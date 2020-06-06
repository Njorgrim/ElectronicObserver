using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Browser.CefOp;
using Browser.ExtraBrowser;
using BrowserLibCore;
using CefSharp;
using CefSharp.Wpf;
using CefSharp.Wpf.Internals;
using Grpc.Core;
using MagicOnion.Client;
using Clipboard = System.Windows.Clipboard;
using Color = System.Drawing.Color;
using MessageBox = System.Windows.MessageBox;
using Orientation = System.Windows.Controls.Orientation;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Size = System.Windows.Size;

namespace Browser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, BrowserLibCore.IBrowser
	{
		private Size KanColleSize { get; } = new Size(1200, 720);
		private string BrowserCachePath => BrowserConstants.CachePath;

		private readonly string StyleClassID = Guid.NewGuid().ToString().Substring(0, 8);
		private bool RestoreStyleSheet { get; set; }

		private string Host { get; }
		private int Port { get; }
		
		private BrowserLibCore.IBrowserHost BrowserHost { get; set; }

		private BrowserConfiguration Configuration { get; set; }

		// 親プロセスが生きているか定期的に確認するためのタイマー
		private System.Timers.Timer HeartbeatTimer { get; } = new System.Timers.Timer();

		private string ProxySettings { get; set; }


		private bool _styleSheetApplied;
		/// <summary>
		/// スタイルシートの変更が適用されているか
		/// </summary>
		private bool StyleSheetApplied
		{
			get => _styleSheetApplied;
			set
			{
				if (value)
				{
					ApplyZoom();
				}

				_styleSheetApplied = value;
			}
		}

		private VolumeManager? VolumeManager { get; set; }

		private string? LastScreenShotPath { get; set; }

		public MainWindow(string host, int port)
		{
			// Debugger.Launch();

			Host = host;
			Port = port;

			OpenEOChannel();
			InitializeCef();

            InitializeComponent();

            CultureInfo c = CultureInfo.CurrentCulture;
			CultureInfo ui = CultureInfo.CurrentUICulture;
			if (c.Name != "en-US" && c.Name != "ja-JP" && c.Name != "ko-KR")
			{
				c = new CultureInfo("en-US");
			}
			if (ui.Name != "en-US" && ui.Name != "ja-JP" && ui.Name != "ko-KR")
			{
				ui = new CultureInfo("en-US");
			}
			Thread.CurrentThread.CurrentCulture = c;
			Thread.CurrentThread.CurrentUICulture = ui;

			StyleSheetApplied = false;
		}

		#region Click Events

		private async void MI_Screenshot_OnClick(object sender, RoutedEventArgs e)
		{
			await SaveScreenShot();
		}

		#region Zoom

		private void MI_Zoom_OnClick(object sender, RoutedEventArgs e)
		{
			if (!(sender is System.Windows.Controls.MenuItem mi)) return;
			if (!(mi.Tag is string tag)) return;

			if (double.TryParse(tag, out double zoom))
			{
				Configuration.ZoomRate = zoom;
				Configuration.ZoomFit = MI_Zoom_Fit.IsChecked = false;
			}
			else
			{
				Configuration.ZoomFit = MI_Zoom_Fit.IsChecked = true;
			}

			ApplyZoom();
			ConfigurationUpdated();
		}

		private void MI_Zoom_InOut_OnClick(object sender, RoutedEventArgs e)
		{
			if (!(sender is System.Windows.Controls.MenuItem mi)) return;
			if (!(mi.Tag is string tag)) return;
			if (!double.TryParse(tag, out double zoom)) return;

			Configuration.ZoomRate = Math.Clamp(Configuration.ZoomRate + zoom, 0.1, 10);
			Configuration.ZoomFit = MI_Zoom_Fit.IsChecked = false;
			ApplyZoom();
			ConfigurationUpdated();
		}

		#endregion

		private void MI_Mute_OnClick(object sender, RoutedEventArgs e)
		{
			try
			{
				VolumeManager.ToggleMute();
			}
			catch (Exception)
			{
				System.Media.SystemSounds.Beep.Play();
			}

			SetVolumeState();
		}

		private void MI_Refresh_OnClick(object sender, RoutedEventArgs e)
		{
			if (!Configuration.ConfirmAtRefresh ||
			    MessageBox.Show(Properties.Resources.ReloadDialog, Properties.Resources.Confirmation,
				    MessageBoxButton.OKCancel, MessageBoxImage.Exclamation, MessageBoxResult.Cancel)
			    == MessageBoxResult.OK)
			{
				RefreshBrowser();
			}
		}

		private void MI_GoToLogin_OnClick(object sender, RoutedEventArgs e)
		{
			if (MessageBox.Show(Properties.Resources.LoginDialog, Properties.Resources.Confirmation,
				    MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel)
			    == MessageBoxResult.OK)
			{
				Navigate(Configuration.LogInPageURL);
			}
		}

		#region Other

		private void MI_Other_PreviousScreenshot_Preview_OnClick(object sender, RoutedEventArgs e)
		{
			if (LastScreenShotPath == null || !File.Exists(LastScreenShotPath)) return;

			ProcessStartInfo psi = new ProcessStartInfo
			{
				FileName = LastScreenShotPath,
				UseShellExecute = true
			};
			Process.Start(psi);
			
		}

		private void MI_Other_PreviousScreenshot_OpenScreenshotFolder_OnClick(object sender, RoutedEventArgs e)
		{
			if (!Directory.Exists(Configuration.ScreenShotPath)) return;

			ProcessStartInfo psi = new ProcessStartInfo
			{
				FileName = Configuration.ScreenShotPath,
				UseShellExecute = true
			};
			Process.Start(psi);
		}

		private void MI_Other_PreviousScreenshot_Copy_OnClick(object sender, RoutedEventArgs e)
		{
			if (LastScreenShotPath == null || !File.Exists(LastScreenShotPath)) return;

			try
			{
				Clipboard.SetImage(new BitmapImage(new Uri(Path.IsPathRooted(LastScreenShotPath) switch
				{
					true => LastScreenShotPath,
					// when can this be null?
					false => Path.Combine(AppDomain.CurrentDomain.BaseDirectory!, LastScreenShotPath)
				})));
				AddLog(2, string.Format("Screenshot {0} copied to clipboard.", LastScreenShotPath));

			}
			catch (Exception ex)
			{
				SendErrorReport(ex.Message, "Failed to copy screenshot to clipboard.");
			}
		}

		private void RangeBase_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (VolumeManager == null)
			{
				TryGetVolumeManager();
			}

			try
			{
				VolumeManager.Volume = (float) e.NewValue / 100;
			}
			catch (Exception)
			{
				//control.BackColor = Color.MistyRose;
			}
		}

		private void MI_Other_HardRefresh_OnClick(object sender, RoutedEventArgs e)
		{
			if (!Configuration.ConfirmAtRefresh ||
			    MessageBox.Show(Properties.Resources.ReloadHardDialog, Properties.Resources.Confirmation,
				    MessageBoxButton.OKCancel, MessageBoxImage.Exclamation, MessageBoxResult.Cancel)
			    == MessageBoxResult.OK)
			{
				RefreshBrowser(true);
			}
		}

		private void MI_Other_GoTo_OnClick(object sender, RoutedEventArgs e)
		{
			BrowserHost.RequestNavigation(Browser.GetMainFrame()?.Url ?? "");
		}

		private void MI_Other_ClearCache_OnClick(object sender, RoutedEventArgs e)
		{
			if (MessageBox.Show("キャッシュをクリアするため、ブラウザを再起動します。\r\nよろしいですか？\r\n※環境によっては本ツールが終了する場合があります。その場合は再起動してください。", "ブラウザ再起動確認",
				    MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes)
			{
				BrowserHost.ClearCache();
			}
		}

		private void MI_Other_ApplyStyleSheet_OnClick(object sender, RoutedEventArgs e)
		{
			MI_Other_ApplyStyleSheet.IsChecked = !MI_Other_ApplyStyleSheet.IsChecked;
			Configuration.AppliesStyleSheet = MI_Other_ApplyStyleSheet.IsChecked;
			if (!Configuration.AppliesStyleSheet)
				RestoreStyleSheet = true;

			ApplyStyleSheet();
			ApplyZoom();
			ConfigurationUpdated();
		}

		private void MI_Other_Alignment_OnClick(object sender, RoutedEventArgs e)
		{
			if (!(sender is System.Windows.Controls.MenuItem mi)) return;
			if (!(mi.Tag is string tag)) return;

			if (int.TryParse(tag, out int dockValue))
			{
				Dock dock = (Dock) dockValue;

				DockPanel.SetDock(ToolMenu, dock);

				FrameworkElementFactory factoryPanel = new FrameworkElementFactory(typeof(StackPanel));
				factoryPanel.SetValue(StackPanel.OrientationProperty, dock switch
				{
					Dock.Top => Orientation.Horizontal,
					Dock.Bottom => Orientation.Horizontal,
					Dock.Left => Orientation.Vertical,
					Dock.Right => Orientation.Vertical,

					_ => Orientation.Horizontal
				});

				ToolMenu.ItemsPanel = new ItemsPanelTemplate { VisualTree = factoryPanel };

				Configuration.ToolMenuDockStyle = dockValue;
				ConfigurationUpdated();
			}
			else
			{
				ToolMenu.Visibility = Visibility.Collapsed;
				Configuration.IsToolMenuVisible = false;
				ConfigurationUpdated();
			}
		}

		private void MI_Other_DeveloperTools_OnClick(object sender, RoutedEventArgs e)
		{
			if (!IsBrowserInitialized)
				return;

			Browser.GetBrowser().ShowDevTools();
		}

		#endregion

		#endregion

		private void OpenEOChannel()
		{
			// ホストプロセスに接続
			Channel grpChannel = new Channel(Host, Port, ChannelCredentials.Insecure);
			BrowserHost = StreamingHubClient.Connect<BrowserLibCore.IBrowserHost, BrowserLibCore.IBrowser>(grpChannel, this);
		}

		// hack it gets called multiple times in wpf, call it only once
		private bool WindowLoaded { get; set; }

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			if (WindowLoaded) return;

			WindowLoaded = true;

			IntPtr handle = new WindowInteropHelper(this).Handle;
			SetWindowLong(handle, GWL_STYLE, WS_CHILD);

			ConfigurationChanged();

			// ウィンドウの親子設定＆ホストプロセスから接続してもらう
			Task.Run(async () => await BrowserHost.ConnectToBrowser((long)handle)).Wait();

			// 親ウィンドウが生きているか確認
			HeartbeatTimer.Elapsed += async (sender2, e2) =>
			{
				try
				{
					await BrowserHost.IsServerAlive();
				}
				catch (Exception e)
				{
					Debug.WriteLine("host died");
					Exit();
				}
			};
			HeartbeatTimer.Interval = 2000; // 2秒ごと　
			HeartbeatTimer.Start();

			InitializeBrowser();
		}

		private void InitializeCef()
		{
			if (Cef.IsInitialized) return;

			Configuration = BrowserHost.Configuration().Result;

			CefSettings settings = new CefSettings
			{
				BrowserSubprocessPath = Path.Combine(
					AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
					@"CefSharp.BrowserSubprocess.exe"),
				CachePath = BrowserCachePath,
				Locale = "ja",
				AcceptLanguageList = "ja,en-US,en",        // todo: いる？
				LogSeverity = Configuration.SavesBrowserLog ? LogSeverity.Error : LogSeverity.Disable,
				LogFile = "BrowserLog.log",
			};

			if (!Configuration.HardwareAccelerationEnabled)
				settings.DisableGpuAcceleration();

			// this sets ProxySettings
			SetProxy(BrowserHost.GetDownstreamProxy().Result);

			settings.CefCommandLineArgs.Add("proxy-server", ProxySettings);
			settings.CefCommandLineArgs.Add("limit-fps", "60");
			// limit browser fps to fix canvas crash
			// causes memory leak after Umikaze k2 update somehow...
			// settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:66.0) Gecko/20100101 Firefox/66.0"; // fix for 206 response from server for bgm
			settings.CefCommandLineArgs.Add("disable-features", "HardwareMediaKeyHandling"); // prevent CEF from taking over media keys
			if (Configuration.ForceColorProfile)
				settings.CefCommandLineArgs.Add("force-color-profile", "srgb");
			CefSharpSettings.SubprocessExitIfParentProcessClosed = true;
			Cef.Initialize(settings, false, (IBrowserProcessHandler)null);
		}


		/// <summary>
		/// ブラウザを初期化します。
		/// 最初の呼び出しのみ有効です。二回目以降は何もしません。
		/// </summary>
		void InitializeBrowser()
		{
			// it's never null in the wpf version
			// if (Browser != null) return;
			if (ProxySettings == null) return;

			Browser.RequestHandler = new CefRequestHandler();
			Browser.MenuHandler = new MenuHandler();
			Browser.WpfKeyboardHandler = new WpfKeyboardHandler(Browser);
			// todo Browser.KeyboardHandler = new KeyboardHandler();
			Browser.DragHandler = new DragHandler();

			// Fixes text rendering position too high
			Browser.LoadingStateChanged += Browser_LoadingStateChanged;
			// todo
			Browser.IsBrowserInitializedChanged += Browser_IsBrowserInitializedChanged;
		}

		void Exit()
		{
			Dispatcher.Invoke(() =>
			{
				HeartbeatTimer.Stop();
				Task.Run(async () => await BrowserHost.DisposeAsync()).Wait();
				Cef.Shutdown();
				Application.Current.Shutdown();
			});
		}

		void BrowserHostChannel_Faulted(Exception e)
		{
			// 親と通信できなくなったら終了する
			Exit();
		}

		public void CloseBrowser()
		{
			HeartbeatTimer.Stop();
			// リモートコールでClose()呼ぶのばヤバそうなので非同期にしておく
			Dispatcher.BeginInvoke((Action)(() => Exit()));
		}

		public async void ConfigurationChanged()
		{
			Configuration = await BrowserHost.Configuration();

			MI_Zoom_Fit.IsChecked = Configuration.ZoomFit;
			ApplyZoom();
			MI_Other_ApplyStyleSheet.IsChecked = Configuration.AppliesStyleSheet;
			MI_Other_Volume_Slider.Value = Configuration.Volume;
			DockPanel.SetDock(ToolMenu, (Dock)Configuration.ToolMenuDockStyle);
			ToolMenu.Visibility = Configuration.IsToolMenuVisible switch
			{
				true => Visibility.Visible,
				false => Visibility.Collapsed
			};
		}

		private void ConfigurationUpdated()
		{
			BrowserHost.ConfigurationUpdated(Configuration);
		}

		private void AddLog(int priority, string message)
		{
			BrowserHost.AddLog(priority, message);
		}

		private void SendErrorReport(string exceptionName, string message)
		{
			BrowserHost.SendErrorReport(exceptionName, message);
		}

		public void InitialAPIReceived()
		{
			//ロード直後の適用ではレイアウトがなぜか崩れるのでこのタイミングでも適用
			ApplyStyleSheet();
			ApplyZoom();
			DestroyDMMreloadDialog();

			//起動直後はまだ音声が鳴っていないのでミュートできないため、この時点で有効化
			SetVolumeState();
		}

		// hack: it makes an infinite loop in the wpf version for some reason
		private int Counter { get; set; }

		private void Browser_LoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
		{
			// DocumentCompleted に相当?
			// note: 非 UI thread からコールされるので、何かしら UI に触る場合は適切な処置が必要

			if (e.IsLoading) return;

			Dispatcher.Invoke(() =>
			{
				if (Counter > 0) return;
				if (!Browser.Address.Contains("redirect")) return;

				Counter++;

				SetCookie();
				Browser.Reload();
			});

			Dispatcher.BeginInvoke((Action)(() =>
			{
				ApplyStyleSheet();

				ApplyZoom();
				DestroyDMMreloadDialog();
			}));
		}


		private bool IsBrowserInitialized =>
			Browser != null &&
			Browser.IsBrowserInitialized;

		private IFrame? GetMainFrame()
		{
			if (!IsBrowserInitialized) return null;

			var browser = Browser.GetBrowser();
			var frame = browser.MainFrame;

			if (frame?.Url?.Contains(@"http://www.dmm.com/netgame/social/") ?? false)
				return frame;

			return null;
		}

		private IFrame? GetGameFrame()
		{
			if (!IsBrowserInitialized) return null;

			var browser = Browser.GetBrowser();
			var frames = browser.GetFrameIdentifiers()
						.Select(id => browser.GetFrame(id));

			return frames.FirstOrDefault(f => f?.Url?.Contains(@"http://osapi.dmm.com/gadgets/") ?? false);
		}

		private IFrame? GetKanColleFrame()
		{
			if (!IsBrowserInitialized) return null;

			var browser = Browser.GetBrowser();
			var frames = browser.GetFrameIdentifiers()
					.Select(id => browser.GetFrame(id));

			return frames.FirstOrDefault(f => f?.Url?.Contains(@"/kcs2/index.php") ?? false);
		}


		/// <summary>
		/// スタイルシートを適用します。
		/// </summary>
		private void ApplyStyleSheet()
		{
			if (!IsBrowserInitialized) return;
			if (!Configuration.AppliesStyleSheet && !RestoreStyleSheet) return;

			try
			{
				IFrame? mainframe = GetMainFrame();
				IFrame? gameframe = GetGameFrame();

				if (mainframe == null || gameframe == null) return;

				if (RestoreStyleSheet)
				{
					mainframe.EvaluateScriptAsync(string.Format(Properties.Resources.RestoreScript, StyleClassID));
					gameframe.EvaluateScriptAsync(string.Format(Properties.Resources.RestoreScript, StyleClassID));
					gameframe.EvaluateScriptAsync("document.body.style.backgroundColor = \"#000000\";");
					StyleSheetApplied = false;
					RestoreStyleSheet = false;
				}
				else
				{
					mainframe.EvaluateScriptAsync(string.Format(Properties.Resources.PageScript, StyleClassID));
					gameframe.EvaluateScriptAsync(string.Format(Properties.Resources.FrameScript, StyleClassID));
					gameframe.EvaluateScriptAsync("document.body.style.backgroundColor = \"#000000\";");

				}

				StyleSheetApplied = true;

			}
			catch (Exception ex)
			{
				SendErrorReport(ex.ToString(), Properties.Resources.FailedToApplyStylesheet);
			}

		}

		/// <summary>
		/// DMMによるページ更新ダイアログを非表示にします。
		/// </summary>
		private void DestroyDMMreloadDialog()
		{
			if (!IsBrowserInitialized) return;
			if (!Configuration.IsDMMreloadDialogDestroyable) return;

			try
			{
				IFrame? mainframe = GetMainFrame();

				if (mainframe == null) return;

				mainframe.EvaluateScriptAsync(Properties.Resources.DMMScript);
			}
			catch (Exception ex)
			{
				SendErrorReport(ex.ToString(), "Failed to hide DMM refresh dialog.");
			}

		}



		// タイミングによっては(特に起動時)、ブラウザの初期化が完了する前に Navigate() が呼ばれることがある
		// その場合ロードに失敗してブラウザが白画面でスタートしてしまう（手動でログインページを開けば続行は可能だが）
		// 応急処置として失敗したとき後で再試行するようにしてみる
		private string? NavigateCache { get; set; }
		private void Browser_IsBrowserInitializedChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (!IsBrowserInitialized || NavigateCache == null) return;

			// ロードが完了したので再試行
			string url = NavigateCache;            // 非同期コールするのでコピーを取っておく必要がある
			Dispatcher.BeginInvoke((Action)(() => Navigate(url)));
			NavigateCache = null;
		}

		/// <summary>
		/// 指定した URL のページを開きます。
		/// </summary>
		public void Navigate(string url)
		{
			if (url != Configuration.LogInPageURL || !Configuration.AppliesStyleSheet)
				StyleSheetApplied = false;

			if (IsBrowserInitialized)
			{
				// bug
				// System.Exception: 'The browser has not been initialized.
				// Load can only be called after the underlying CEF browser is initialized
				// (CefLifeSpanHandler::OnAfterCreated).'
				Browser.Load(url);
			}
			else
			{
				// 大方ロードできないのであとで再試行する
				NavigateCache = url;
			}
		}

		/// <summary>
		/// ブラウザを再読み込みします。
		/// </summary>
		/// <param name="ignoreCache">キャッシュを無視するか。</param>
		private void RefreshBrowser(bool ignoreCache = false)
		{
			if (!Configuration.AppliesStyleSheet)
				StyleSheetApplied = false;

			Browser.Reload(ignoreCache);
		}

		/// <summary>
		/// ズームを適用します。
		/// </summary>
		private void ApplyZoom()
		{
			if (!IsBrowserInitialized) return;

			double zoomRate = Configuration.ZoomRate;
			bool fit = Configuration.ZoomFit && StyleSheetApplied;

			double zoomFactor;

			if (fit)
			{
				double rateX = SizeAdjuster.ActualWidth / KanColleSize.Width;
				double rateY = SizeAdjuster.ActualHeight / KanColleSize.Height;
				zoomFactor = Math.Min(rateX, rateY);
			}
			else
			{
				zoomFactor = Math.Clamp(zoomRate, 0.1, 10);
			}

			Browser.SetZoomLevel(Math.Log(zoomFactor, 1.2));

			if (StyleSheetApplied)
			{
				Browser.Height = (int) (KanColleSize.Height * zoomFactor);
				Browser.Width = (int) (KanColleSize.Width * zoomFactor);
			}

			MI_Zoom_Current.Header = fit switch
			{
				true => Properties.Resources.Other_Zoom_Current_Fit,
				false => Properties.Resources.Other_Zoom_Current + $" {zoomRate:p1}"
			};
		}

		/// <summary>
		/// スクリーンショットを撮影します。
		/// </summary>
		private async Task<Bitmap?> TakeScreenShot()
		{
			var kancolleFrame = GetKanColleFrame();
			if (kancolleFrame == null)
			{
				AddLog(3, "KanColle is not loaded, unable to take screenshots.");
				System.Media.SystemSounds.Beep.Play();
				return null;
			}


			Task<ScreenShotPacket> InternalTakeScreenShot()
			{
				var request = new ScreenShotPacket();

				if (Browser == null || !Browser.IsBrowserInitialized)
					return request.TaskSource.Task;

				string script = $@"
(async function()
{{
	await CefSharp.BindObjectAsync('{request.ID}');

	let canvas = document.querySelector('canvas');
	requestAnimationFrame(() =>
	{{
		let dataurl = canvas.toDataURL('image/png');
		{request.ID}.complete(dataurl);
	}});
}})();
";

				Browser.JavascriptObjectRepository.Register(request.ID, request, true);
				kancolleFrame.ExecuteJavaScriptAsync(script);

				return request.TaskSource.Task;
			}

			var result = await InternalTakeScreenShot();

			// ごみ掃除
			Browser.JavascriptObjectRepository.UnRegister(result.ID);
			kancolleFrame.ExecuteJavaScriptAsync($@"delete {result.ID}");

			return result.GetImage();
		}



		/// <summary>
		/// スクリーンショットを撮影し、設定で指定された保存先に保存します。
		/// </summary>
		private async Task SaveScreenShot()
		{

			int savemode = Configuration.ScreenShotSaveMode;
			int format = Configuration.ScreenShotFormat;
			string folderPath = Configuration.ScreenShotPath;
			bool is32bpp = format != 1 && Configuration.AvoidTwitterDeterioration;

			Bitmap? image = null;
			try
			{
				image = await TakeScreenShot();

				if (image == null) return;

				if (is32bpp)
				{
					if (image.PixelFormat != PixelFormat.Format32bppArgb)
					{
						var imgalt = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb);
						using (var g = Graphics.FromImage(imgalt))
						{
							g.DrawImage(image, new System.Drawing.Rectangle(0, 0, imgalt.Width, imgalt.Height));
						}

						image.Dispose();
						image = imgalt;
					}

					// 不透明ピクセルのみだと jpeg 化されてしまうため、1px だけわずかに透明にする
					Color temp = image.GetPixel(image.Width - 1, image.Height - 1);
					image.SetPixel(image.Width - 1, image.Height - 1, Color.FromArgb(252, temp.R, temp.G, temp.B));
				}
				else
				{
					if (image.PixelFormat != PixelFormat.Format24bppRgb)
					{
						var imgalt = new Bitmap(image.Width, image.Height, PixelFormat.Format24bppRgb);
						using (var g = Graphics.FromImage(imgalt))
						{
							g.DrawImage(image, new Rectangle(0, 0, imgalt.Width, imgalt.Height));
						}

						image.Dispose();
						image = imgalt;
					}
				}


				// to file
				if ((savemode & 1) != 0)
				{
					try
					{
						if (!Directory.Exists(folderPath))
							Directory.CreateDirectory(folderPath);

						string ext;
						ImageFormat imgFormat;

						switch (format)
						{
							case 1:
								ext = "jpg";
								imgFormat = ImageFormat.Jpeg;
								break;
							case 2:
							default:
								ext = "png";
								imgFormat = ImageFormat.Png;
								break;
						}

						string path = $"{folderPath}\\{DateTime.Now:yyyyMMdd_HHmmssff}.{ext}";
						image.Save(path, imgFormat);
						LastScreenShotPath = path;
						MI_Other_PreviousScreenshot_Preview_Image.Source = 
							new BitmapImage(new Uri(Path.IsPathRooted(path) switch
							{
								true => path,
								false => Path.Combine(AppDomain.CurrentDomain.BaseDirectory!, path)
							}));

						AddLog(2, $"Screenshot saved to {path}.");
					}
					catch (Exception ex)
					{
						SendErrorReport(ex.ToString(), "Failed to save screenshot.");
					}
				}


				// to clipboard
				if ((savemode & 2) != 0)
				{
					try
					{
						Clipboard.SetImage(image.ToBitmapSource());

						if ((savemode & 3) != 3)
							AddLog(2, "Screenshot copied to clipboard.");
					}
					catch (Exception ex)
					{
						SendErrorReport(ex.ToString(), "Failed to copy screenshot to clipboard.");
					}
				}
			}
			catch (Exception ex)
			{
				SendErrorReport(ex.ToString(), Properties.Resources.ScreenshotError);
			}
			finally
			{
				image?.Dispose();
			}

		}

		/// <summary>
		/// todo make private once we delete the winforms version
		/// </summary>
		public void SetProxy(string proxy)
		{
			if (ushort.TryParse(proxy, out ushort port))
			{
				// WinInetUtil.SetProxyInProcessForNekoxy(port);
				ProxySettings = "http=127.0.0.1:" + port;           // todo: 動くには動くが正しいかわからない
			}
			else
			{
				// WinInetUtil.SetProxyInProcess(proxy, "local");
				ProxySettings = proxy;
			}

			BrowserHost.SetProxyCompleted();
		}


		private void SetCookie()
		{
			Browser.ExecuteScriptAsync(Properties.Resources.RegionCookie);
		}

		private void TryGetVolumeManager()
		{
			VolumeManager = VolumeManager.CreateInstanceByProcessName("CefSharp.BrowserSubprocess");
		}

		private void SetVolumeState()
		{
			bool mute;
			float volume;

			try
			{
				if (VolumeManager == null)
				{
					TryGetVolumeManager();
				}

				mute = VolumeManager.IsMute;
				volume = VolumeManager.Volume * 100;
			}
			catch (Exception e)
			{
				// 音量データ取得不能時
				VolumeManager = null;
				mute = false;
				volume = 100;
			}

			try
			{
				MI_Mute.Icon = new System.Windows.Controls.Image
				{
					Source = (ImageSource) FindResource(mute switch
					{
						true => "Icon_Browser_Mute",
						false => "Icon_Browser_Unmute"
					})
				};
			}
			catch (Exception e)
			{
				SendErrorReport(e.ToString(), "Icon failed to load.");
			}

			MI_Other_Volume_Slider.Value = volume;

			Configuration.Volume = volume;
			Configuration.IsMute = mute;
			ConfigurationUpdated();
		}

		void SizeAdjuster_DoubleClick(object sender, EventArgs e)
		{
			// double click size adjuster
			ToolMenu.Visibility = Visibility.Visible;
			Configuration.IsToolMenuVisible = true;
			ConfigurationUpdated();
		}

		private void SizeAdjuster_OnSizeChanged(object sender, SizeChangedEventArgs e)
		{
			ApplyZoom();
		}

		public void OpenExtraBrowser()
		{
			new WPFExtraBrowser().Show();
		}

		#region 呪文

		[DllImport("user32.dll", EntryPoint = "GetWindowLongA", SetLastError = true)]
		private static extern uint GetWindowLong(IntPtr hwnd, int nIndex);

		[DllImport("user32.dll", EntryPoint = "SetWindowLongA", SetLastError = true)]
		private static extern uint SetWindowLong(IntPtr hwnd, int nIndex, uint dwNewLong);

		private const int GWL_STYLE = (-16);
		private const uint WS_CHILD = 0x40000000;
		private const uint WS_VISIBLE = 0x10000000;
		private const int WM_ERASEBKGND = 0x14;

		#endregion
	}

	public static class CustomCommands
    {
	    public static readonly RoutedUICommand Screenshot = new RoutedUICommand
	    (
		    "",
		    "Screenshot",
		    typeof(CustomCommands),
		    new InputGestureCollection
		    {
			    new KeyGesture(Key.F2)
		    }
	    );

	    public static readonly RoutedUICommand Refresh = new RoutedUICommand
	    (
		    "",
			"Refresh",
		    typeof(CustomCommands),
		    new InputGestureCollection
		    {
			    new KeyGesture(Key.F5)
		    }
	    );

	    public static readonly RoutedUICommand HardRefresh = new RoutedUICommand
	    (
		    "",
			"HardRefresh",
		    typeof(CustomCommands),
		    new InputGestureCollection
		    {
			    new KeyGesture(Key.F5, ModifierKeys.Control)
		    }
	    );

		public static readonly RoutedUICommand Mute = new RoutedUICommand
		(
			"",
			"Mute",
			typeof(CustomCommands),
			new InputGestureCollection
			{
				new KeyGesture(Key.F7)
			}
		);

		public static readonly RoutedUICommand OpenDeveloperTools = new RoutedUICommand
		(
			"",
			"OpenDeveloperTools",
			typeof(CustomCommands),
			new InputGestureCollection
			{
				new KeyGesture(Key.F12)
			}
		);
	}
}
