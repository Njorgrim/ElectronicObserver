using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BrowserLibCore;
using CefSharp;
using CefSharp.Wpf;
using CefSharp.Wpf.Internals;
using ElectronicObserver.Observer;
using ElectronicObserver.Utility;
using ElectronicObserver.WPFEO.Browser.CefOp;
using ElectronicObserver.WPFEO.Browser.ExtraBrowser;
using Clipboard = System.Windows.Clipboard;
using Color = System.Drawing.Color;
using MessageBox = System.Windows.MessageBox;
using Orientation = System.Windows.Controls.Orientation;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Size = System.Windows.Size;
using UserControl = System.Windows.Controls.UserControl;

namespace ElectronicObserver.WPFEO.Browser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class WPFBrowser : UserControl
	{
		private Size KanColleSize { get; } = new Size(1200, 720);
		private string BrowserCachePath => BrowserConstants.CachePath;

		private readonly string StyleClassID = Guid.NewGuid().ToString().Substring(0, 8);
		private bool RestoreStyleSheet { get; set; }

		private Configuration.ConfigurationData.ConfigFormBrowser Config => Configuration.Config.FormBrowser;

		private string ProxySettings { get; set; }
		private ChromiumWebBrowser? Browser { get; set; }


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
				else
				{
					SizeAdjuster.Height = double.NaN;
					SizeAdjuster.Width = double.NaN;
				}

				_styleSheetApplied = value;
			}
		}

		private VolumeManager? VolumeManager { get; set; }

		private string? LastScreenShotPath { get; set; }

		public WPFBrowser()
		{
			InitializeComponent();

			// should be handled in EO itself already
            /*CultureInfo c = CultureInfo.CurrentCulture;
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
			Thread.CurrentThread.CurrentUICulture = ui;*/

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
			if (!(sender is MenuItem mi)) return;
			if (!(mi.Tag is string tag)) return;

			if (double.TryParse(tag, out double zoom))
			{
				Config.ZoomRate = zoom;
				Config.ZoomFit = MI_Zoom_Fit.IsChecked = false;
			}
			else
			{
				Config.ZoomFit = MI_Zoom_Fit.IsChecked = true;
			}

			ApplyZoom();
		}

		private void MI_Zoom_InOut_OnClick(object sender, RoutedEventArgs e)
		{
			if (!(sender is MenuItem mi)) return;
			if (!(mi.Tag is string tag)) return;
			if (!double.TryParse(tag, out double zoom)) return;

			Config.ZoomRate = Math.Clamp(Config.ZoomRate + zoom, 0.1, 10);
			Config.ZoomFit = MI_Zoom_Fit.IsChecked = false;
			ApplyZoom();
		}

		#endregion

		private void MI_Mute_OnClick(object sender, RoutedEventArgs e)
		{
			if (VolumeManager == null)
			{
                TryGetVolumeManager();
			}

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
			if (!Config.ConfirmAtRefresh ||
			    MessageBox.Show(Properties.BrowserResources.ReloadDialog, Properties.BrowserResources.Confirmation,
				    MessageBoxButton.OKCancel, MessageBoxImage.Exclamation, MessageBoxResult.Cancel)
			    == MessageBoxResult.OK)
			{
				RefreshBrowser();
			}
		}

		private void MI_GoToLogin_OnClick(object sender, RoutedEventArgs e)
		{
			if (MessageBox.Show(Properties.BrowserResources.LoginDialog, Properties.BrowserResources.Confirmation,
				    MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel)
			    == MessageBoxResult.OK)
			{
				Navigate(Config.LogInPageURL);
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
			if (!Directory.Exists(Config.ScreenShotPath)) return;

			ProcessStartInfo psi = new ProcessStartInfo
			{
				FileName = Config.ScreenShotPath,
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
				SendErrorReport(ex, "Failed to copy screenshot to clipboard.");
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
				
			}
		}

		private void MI_Other_HardRefresh_OnClick(object sender, RoutedEventArgs e)
		{
			if (!Config.ConfirmAtRefresh ||
			    MessageBox.Show(Properties.BrowserResources.ReloadHardDialog, Properties.BrowserResources.Confirmation,
				    MessageBoxButton.OKCancel, MessageBoxImage.Exclamation, MessageBoxResult.Cancel)
			    == MessageBoxResult.OK)
			{
				RefreshBrowser(true);
			}
		}

		private void MI_Other_GoTo_OnClick(object sender, RoutedEventArgs e)
		{
			WPFBrowserHost.Instance.RequestNavigation(Browser.GetMainFrame()?.Url ?? "");
		}

		private void MI_Other_ClearCache_OnClick(object sender, RoutedEventArgs e)
		{
			if (MessageBox.Show("キャッシュをクリアするため、ブラウザを再起動します。\r\nよろしいですか？\r\n※環境によっては本ツールが終了する場合があります。その場合は再起動してください。", "ブラウザ再起動確認",
				    MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes)
			{
				WPFBrowserHost.Instance.ClearCache();
			}
		}

		private void MI_Other_ApplyStyleSheet_OnClick(object sender, RoutedEventArgs e)
		{
			MI_Other_ApplyStyleSheet.IsChecked = !MI_Other_ApplyStyleSheet.IsChecked;
			Config.AppliesStyleSheet = MI_Other_ApplyStyleSheet.IsChecked;
			if (!Config.AppliesStyleSheet)
				RestoreStyleSheet = true;

			ApplyStyleSheet();
			ApplyZoom();
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

				Config.ToolMenuDockStyle = (DockStyle) dockValue;
			}
			else
			{
				ToolMenu.Visibility = Visibility.Collapsed;
				Config.IsToolMenuVisible = false;
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

		// hack it gets called multiple times in wpf, call it only once
		private bool WindowLoaded { get; set; }

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			if (WindowLoaded) return;

			WindowLoaded = true;

			InitializeCef();
			InitializeBrowser();
		}

		public void RestartBrowser()
		{
			InitializeBrowser();
		}

		private string GetDownstreamProxy()
		{
			var config = Configuration.Config.Connection;

			if (!string.IsNullOrEmpty(config.DownstreamProxy))
			{
				return config.DownstreamProxy;

			}
			else if (config.UseSystemProxy)
			{
				return APIObserver.Instance.ProxyPort.ToString();
			}
			else if (config.UseUpstreamProxy)
			{
				return string.Format(
					"http=127.0.0.1:{0};https={1}:{2}",
					APIObserver.Instance.ProxyPort,
					config.UpstreamProxyAddress,
					config.UpstreamProxyPort);
			}
			else
			{
				return string.Format("http=127.0.0.1:{0}", APIObserver.Instance.ProxyPort);
			}
		}

		private void InitializeCef()
		{
			if (Cef.IsInitialized) return;

			CefSettings settings = new CefSettings
			{
				BrowserSubprocessPath = Path.Combine(
					AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
					@"CefSharp.BrowserSubprocess.exe"),
				CachePath = BrowserCachePath,
				Locale = "ja",
				AcceptLanguageList = "ja,en-US,en",        // todo: いる？
				LogSeverity = Config.SavesBrowserLog ? LogSeverity.Error : LogSeverity.Disable,
				LogFile = "BrowserLog.log",
			};

			if (!Config.HardwareAccelerationEnabled)
				settings.DisableGpuAcceleration();

			// this sets ProxySettings
			SetProxy(GetDownstreamProxy());

			settings.CefCommandLineArgs.Add("proxy-server", ProxySettings);
			settings.CefCommandLineArgs.Add("limit-fps", "60");
			// limit browser fps to fix canvas crash
			// causes memory leak after Umikaze k2 update somehow...
			// settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:66.0) Gecko/20100101 Firefox/66.0"; // fix for 206 response from server for bgm
			settings.CefCommandLineArgs.Add("disable-features", "HardwareMediaKeyHandling"); // prevent CEF from taking over media keys
			if (Config.ForceColorProfile)
				settings.CefCommandLineArgs.Add("force-color-profile", "srgb");
			CefSharpSettings.SubprocessExitIfParentProcessClosed = true;
			Cef.Initialize(settings, false, (IBrowserProcessHandler) null);
		}


		/// <summary>
		/// ブラウザを初期化します。
		/// 最初の呼び出しのみ有効です。二回目以降は何もしません。
		/// </summary>
		void InitializeBrowser()
		{

			/*
			 <wpf:ChromiumWebBrowser 
				x:Name="Browser" 
				Address="http://www.dmm.com/netgame/social/-/gadgets/=/app_id=854854/" 
				FontFamily="Microsoft YaHei" />
			 */
			if (Browser != null) return;
			if (ProxySettings == null) return;
			
			Browser = new ChromiumWebBrowser
			{
				Address = Configuration.Config.FormBrowser.LogInPageURL,
				FontFamily = new System.Windows.Media.FontFamily("Microsoft YaHei"),
				RequestHandler = new CefRequestHandler(),
				MenuHandler = new MenuHandler(),
				DragHandler = new DragHandler(),
			};

			Browser.WpfKeyboardHandler = new WpfKeyboardHandler(Browser);

			Browser.LoadingStateChanged += Browser_LoadingStateChanged;
			Browser.IsBrowserInitializedChanged += Browser_IsBrowserInitializedChanged;

			SizeAdjuster.Content = Browser;
		}

		public void CloseBrowser()
		{
			// might need to set cookie again
			Counter = 0;
			Browser?.Dispose();
			Browser = null;
		}

		private void AddLog(int priority, string message)
		{
			Logger.Add(priority, message);
        }

		private void SendErrorReport(Exception ex, string message)
		{
			ErrorReporter.SendErrorReport(ex, message);
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
			if (!Config.AppliesStyleSheet && !RestoreStyleSheet) return;

			try
			{
				IFrame? mainframe = GetMainFrame();
				IFrame? gameframe = GetGameFrame();

				if (mainframe == null || gameframe == null) return;

				if (RestoreStyleSheet)
				{
					mainframe.EvaluateScriptAsync(string.Format(Properties.BrowserResources.RestoreScript, StyleClassID));
					gameframe.EvaluateScriptAsync(string.Format(Properties.BrowserResources.RestoreScript, StyleClassID));
					gameframe.EvaluateScriptAsync("document.body.style.backgroundColor = \"#000000\";");
					StyleSheetApplied = false;
					RestoreStyleSheet = false;
				}
				else
				{
					mainframe.EvaluateScriptAsync(string.Format(Properties.BrowserResources.PageScript, StyleClassID));
					gameframe.EvaluateScriptAsync(string.Format(Properties.BrowserResources.FrameScript, StyleClassID));
					gameframe.EvaluateScriptAsync("document.body.style.backgroundColor = \"#000000\";");

				}

				StyleSheetApplied = true;

			}
			catch (Exception ex)
			{
				SendErrorReport(ex, Properties.BrowserResources.FailedToApplyStylesheet);
			}

		}

		/// <summary>
		/// DMMによるページ更新ダイアログを非表示にします。
		/// </summary>
		private void DestroyDMMreloadDialog()
		{
			if (!IsBrowserInitialized) return;
			if (!Config.IsDMMreloadDialogDestroyable) return;

			try
			{
				IFrame? mainframe = GetMainFrame();

				if (mainframe == null) return;

				mainframe.EvaluateScriptAsync(Properties.BrowserResources.DMMScript);
			}
			catch (Exception ex)
			{
				SendErrorReport(ex, "Failed to hide DMM refresh dialog.");
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
			if (url != Config.LogInPageURL || !Config.AppliesStyleSheet)
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
			if (!Config.AppliesStyleSheet)
				StyleSheetApplied = false;

			Browser.Reload(ignoreCache);
		}

		/// <summary>
		/// ズームを適用します。
		/// </summary>
		private void ApplyZoom()
		{
			if (!IsBrowserInitialized) return;

			double zoomRate = Config.ZoomRate;
			bool fit = Config.ZoomFit && StyleSheetApplied;

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
				true => Properties.BrowserResources.Other_Zoom_Current_Fit,
				false => Properties.BrowserResources.Other_Zoom_Current + $" {zoomRate:p1}"
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

			int savemode = Config.ScreenShotSaveMode;
			int format = Config.ScreenShotFormat;
			string folderPath = Config.ScreenShotPath;
			bool is32bpp = format != 1 && Config.AvoidTwitterDeterioration;

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
						SendErrorReport(ex, "Failed to save screenshot.");
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
						SendErrorReport(ex, "Failed to copy screenshot to clipboard.");
					}
				}
			}
			catch (Exception ex)
			{
				SendErrorReport(ex, Properties.BrowserResources.ScreenshotError);
			}
			finally
			{
				image?.Dispose();
			}

		}

		/// <summary>
		/// todo make private once we delete the winforms version
		/// </summary>
		private void SetProxy(string proxy)
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
		}

		private void SetCookie()
		{
			Browser.ExecuteScriptAsync(Properties.BrowserResources.RegionCookie);
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
				SendErrorReport(e, "Icon failed to load.");
			}

			MI_Other_Volume_Slider.Value = volume;

			// todo volume
			// Config.Volume = volume;
			SyncBGMPlayer.Instance.IsMute = mute;
		}

		void SizeAdjuster_DoubleClick(object sender, EventArgs e)
		{
			ToolMenu.Visibility = Visibility.Visible;
			Config.IsToolMenuVisible = true;
		}

		private void SizeAdjuster_OnSizeChanged(object sender, SizeChangedEventArgs e)
		{
			ApplyZoom();
		}

		public void OpenExtraBrowser()
		{
			new WPFExtraBrowser().Show();
		}
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
