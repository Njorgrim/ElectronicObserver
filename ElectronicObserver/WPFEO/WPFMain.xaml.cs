using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ElectronicObserver.Properties;
using ElectronicObserver.Data;
using ElectronicObserver.Notifier;
using ElectronicObserver.Observer;
using ElectronicObserver.Resource;
using ElectronicObserver.Resource.Record;
using ElectronicObserver.Utility;
using AvalonDock.Layout;
using System.Diagnostics;
using System.IO;
using AvalonDock.Layout.Serialization;
using AvalonDock;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Interop;
using Titanium.Web.Proxy.StreamExtended;

namespace ElectronicObserver.WPFEO
{
	public partial class WPFMain : Window
	{
		#region Properties

		//public DockPanel MainPanel => MainDockPanel;
		// public FormWindowCapture WindowCapture => fWindowCapture;

		private int ClockFormat;

		/// <summary>
		/// 音量設定用フラグ
		/// -1 = 無効, そうでなければ現在の試行回数
		/// </summary>
		private int _volumeUpdateState = 0;

		private DateTime _prevPlayTimeRecorded = DateTime.MinValue;

		#endregion

		//Singleton
		public static WPFMain Instance;


		#region Forms

		public List<UserControl> SubUCs { get; private set; }

		public WPFFleet[] ucFleet;
		//public FormDock ucDock;
		//public FormArsenal ucArsenal;
		public WPFHQ ucHeadquarters;
		//public FormInformation ucInformation;
		//public FormCompass ucCompass;
		public WPFLog ucLog;
		//public FormQuest ucQuest;
		public WPFBattle ucBattle;
		//public FormFleetOverview ucFleetOverview;
		//public FormShipGroup ucShipGroup;
		public WPFBrowserHost ucBrowser;
		//public FormWindowCapture ucWindowCapture;
		//public FormXPCalculator ucXPCalculator;
		//public FormBaseAirCorps ucBaseAirCorps;
		//public FormJson ucJson;

		#endregion


		public WPFMain()
		{
			InitializeComponent();

			Instance = this;

			this.DataContext = this;
		}

		#region TestBackground
		/*
		/// <summary>
		/// TestBackground Dependency Property
		/// </summary>
		public static readonly DependencyProperty TestBackgroundProperty =
			DependencyProperty.Register("TestBackground", typeof(Brush), typeof(WPFMain),
				new FrameworkPropertyMetadata((Brush)null));

		/// <summary>
		/// Gets or sets the TestBackground property.  This dependency property 
		/// indicates a randomly changing brush (just for testing).
		/// </summary>
		public Brush TestBackground
		{
			get => (Brush)GetValue(TestBackgroundProperty);
			set => SetValue(TestBackgroundProperty, value);
		}
		*/
		#endregion

		#region FocusedElement
		/*
		/// <summary>
		/// FocusedElement Dependency Property
		/// </summary>
		public static readonly DependencyProperty FocusedElementProperty =
			DependencyProperty.Register("FocusedElement", typeof(string), typeof(WPFMain),
				new FrameworkPropertyMetadata((IInputElement)null));
		*/
		#endregion

		private void OnLayoutRootPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			var activeContent = ((LayoutRoot)sender).ActiveContent;
			if (e.PropertyName == "ActiveContent")
			{
				Debug.WriteLine(string.Format("ActiveContent-> {0}", activeContent));
			}
		}

		private void dockManager_DocumentClosing(object sender, DocumentClosingEventArgs e)
		{
			if (MessageBox.Show("Do you really want to close this tool?", "Electronic Observer (blah)", MessageBoxButton.YesNo) == MessageBoxResult.No)
				e.Cancel = true;
		}

		#region AvalonSerialization
		private void LoadLayout(string path)
		{
			if (File.Exists(@".\EODefaultLayout.config"))
			{
				var currentContentsList = dockManager.Layout.Descendents().OfType<LayoutContent>().Where(c => c.ContentId != null).ToArray();

				var serializer = new XmlLayoutSerializer(dockManager);
				serializer.LayoutSerializationCallback += (s, args) =>
				{
					var prevContent = currentContentsList.FirstOrDefault(c => c.ContentId == args.Model.ContentId);
					if (prevContent != null)
						args.Content = prevContent.Content;
				};
				using var stream = new StreamReader(@".\EODefaultLayout.config");
				serializer.Deserialize(stream);

				currentContentsList = dockManager.Layout.Descendents().OfType<LayoutContent>().Where(c => c.ContentId != null).ToArray();

				foreach (LayoutContent lc in currentContentsList)
				{
					switch (lc.ContentId)
					{
						case "cefBrowser":
							{
								lc.Content = ucBrowser;
								break;
							}
						case "log":
							{
								lc.Content = ucLog;
								break;
							}
						case "fleet1":
							{
								lc.Content = ucFleet[0];
								break;
							}
						case "fleet2":
							{
								lc.Content = ucFleet[1];
								break;
							}
						case "fleet3":
							{
								lc.Content = ucFleet[2];
								break;
							}
						case "fleet4":
							{
								lc.Content = ucFleet[3];
								break;
							}
						case "hq":
							{
								lc.Content = ucHeadquarters;
								break;
							}
						case "battle":
							{
								lc.Content = ucBattle;
								break;
							}
						default: break;
					};
				}
			}
		}

		private void SaveLayout(string path)
		{
			var serializer = new XmlLayoutSerializer(dockManager);
			using var stream = new StreamWriter(@".\EODefaultLayout.config");
			serializer.Serialize(stream);
		}
		#endregion

		private async void Window_Loaded(object sender, RoutedEventArgs e)
		{
			if (!Directory.Exists("Settings"))
				Directory.CreateDirectory("Settings");


			//Utility.Configuration.Instance.Load(this);

			/*
			this.MainDockPanel.Styles = Configuration.Config.UI.DockPanelSuiteStyles;
			this.MainDockPanel.Theme = new WeifenLuo.WinFormsUI.Docking.VS2012Theme();
			this.BackColor = this.StripMenu.BackColor = Utility.Configuration.Config.UI.BackColor;
			this.ForeColor = this.StripMenu.ForeColor = Utility.Configuration.Config.UI.ForeColor;
			this.StripStatus.BackColor = Utility.Configuration.Config.UI.StatusBarBackColor;
			this.StripStatus.ForeColor = Utility.Configuration.Config.UI.StatusBarForeColor;
			*/

			Utility.Logger.Instance.LogAdded += new Utility.LogAddedEventHandler((Utility.Logger.LogData data) =>
			{
				if (Dispatcher.CheckAccess())
				{
					// Invokeはメッセージキューにジョブを投げて待つので、別のBeginInvokeされたジョブが既にキューにあると、
					// それを実行してしまい、BeginInvokeされたジョブの順番が保てなくなる
					// GUIスレッドによる処理は、順番が重要なことがあるので、GUIスレッドからInvokeを呼び出してはいけない
					//Dispatcher.Invoke(new Utility.LogAddedEventHandler(Logger_LogAdded), data);
				}
				else
				{
					//Logger_LogAdded(data);
				}
			});

			//Utility.Configuration.Instance.ConfigurationChanged += ConfigurationChanged;

			Utility.Logger.Add(2, SoftwareInformation.SoftwareNameEnglish + " is starting...");

			ResourceManager.Instance.Load();
			RecordManager.Instance.Load();
			KCDatabase.Instance.Load();
			//NotifierManager.Instance.Initialize(this);
			SyncBGMPlayer.Instance.ConfigurationChanged();

			APIObserver.Instance.Start(Utility.Configuration.Config.Connection.Port, this);

			SubUCs = new List<UserControl>();

			//form init
			//注：一度全てshowしないとイベントを受け取れないので注意
			ucFleet = new WPFFleet[4];
			for (int i = 0; i < ucFleet.Length; i++)
			{
				SubUCs.Add(ucFleet[i] = new WPFFleet(this, i + 1));
			}

			//SubUCs.Add(ucDock = new FormDock(this));
			//SubUCs.Add(ucArsenal = new FormArsenal(this));
			SubUCs.Add(ucHeadquarters = new WPFHQ(this));
			//SubUCs.Add(ucInformation = new FormInformation(this));
			//SubUCs.Add(ucCompass = new FormCompass(this));
			SubUCs.Add(ucLog = new WPFLog(this));
			//SubUCs.Add(ucQuest = new FormQuest(this));
			SubUCs.Add(ucBattle = new WPFBattle(this));
			//SubUCs.Add(ucFleetOverview = new FormFleetOverview(this));
			//SubUCs.Add(ucShipGroup = new FormShipGroup(this));
			SubUCs.Add(ucBrowser = new WPFBrowserHost(this));
			//SubUCs.Add(ucWindowCapture = new FormWindowCapture(this));
			//SubUCs.Add(ucXPCalculator = new FormXPCalculator(this));
			//SubUCs.Add(ucBaseAirCorps = new FormBaseAirCorps(this));
			//SubUCs.Add(ucJson = new FormJson(this));

			ConfigurationChanged();     //設定から初期化

			LoadLayout(Configuration.Config.Life.LayoutFilePath);


			#if (!DEBUG)
			SoftwareInformation.CheckUpdate();
			
			CancellationTokenSource cts = new CancellationTokenSource();
			Task.Run(async () => await SoftwareUpdater.PeriodicUpdateCheckAsync(cts.Token));
			#endif

			// デバッグ: 開始時にAPIリストを読み込む
			if (Configuration.Config.Debug.LoadAPIListOnLoad)
			{

				try
				{

					await Task.Factory.StartNew(() => LoadAPIList(Configuration.Config.Debug.APIListPath));

					Activate();     // 上記ロードに時間がかかるとウィンドウが表示されなくなることがあるので
				}
				catch (Exception ex)
				{

					Utility.Logger.Add(3, LoggerRes.FailedLoadAPI + ex.Message);
				}
			}

			APIObserver.Instance.ResponseReceived += (a, b) => UpdatePlayTime();


			// 🎃
			if (DateTime.Now.Month == 10 && DateTime.Now.Day == 31)
			{
				APIObserver.Instance.APIList["api_port/port"].ResponseReceived += CallPumpkinHead;
			}

			// 完了通知（ログインページを開く）
			ucBrowser.InitializeApiCompleted();

			//UIUpdateTimer.Start();


			Utility.Logger.Add(3, Properties.Resources.StartupComplete);

		}

		private void LoadAPIList(string path)
		{

			string parent = Path.GetDirectoryName(path);

			using (StreamReader sr = new StreamReader(path))
			{
				string line;
				while ((line = sr.ReadLine()) != null)
				{

					bool isRequest = false;
					{
						int slashindex = line.IndexOf('/');
						if (slashindex != -1)
						{

							switch (line.Substring(0, slashindex).ToLower())
							{
								case "q":
								case "request":
									isRequest = true;
									goto case "s";
								case "":
								case "s":
								case "response":
									line = line.Substring(Math.Min(slashindex + 1, line.Length));
									break;
							}

						}
					}

					if (APIObserver.Instance.APIList.ContainsKey(line))
					{
						APIBase api = APIObserver.Instance.APIList[line];

						if (isRequest ? api.IsRequestSupported : api.IsResponseSupported)
						{

							string[] files = Directory.GetFiles(parent, string.Format("*{0}@{1}.json", isRequest ? "Q" : "S", line.Replace('/', '@')), SearchOption.TopDirectoryOnly);

							if (files.Length == 0)
								continue;

							Array.Sort(files);

							using (StreamReader sr2 = new StreamReader(files[files.Length - 1]))
							{
								if (isRequest)
								{
									Dispatcher.Invoke((Action)(() =>
									{
										APIObserver.Instance.LoadRequest("/kcsapi/" + line, sr2.ReadToEnd());
									}));
								}
								else
								{
									Dispatcher.Invoke((Action)(() =>
									{
										APIObserver.Instance.LoadResponse("/kcsapi/" + line, sr2.ReadToEnd());
									}));
								}
							}

							//System.Diagnostics.Debug.WriteLine( "APIList Loader: API " + line + " File " + files[files.Length-1] + " Loaded." );
						}
					}
				}

			}

		}

		private void UpdatePlayTime()
		{
			var c = Utility.Configuration.Config.Log;
			DateTime now = DateTime.Now;

			double span = (now - _prevPlayTimeRecorded).TotalSeconds;
			if (span < c.PlayTimeIgnoreInterval)
			{
				c.PlayTime += span;
			}

			_prevPlayTimeRecorded = now;
		}

		private void CallPumpkinHead(string apiname, dynamic data)
		{
			//new DialogHalloween().Show(this);
			APIObserver.Instance.APIList["api_port/port"].ResponseReceived -= CallPumpkinHead;
		}

		private void ConfigurationChanged()
		{
			/*
			var c = Utility.Configuration.Config;

			StripMenu_Debug.Enabled = StripMenu_Debug.Visible =
			StripMenu_View_Json.Enabled = StripMenu_View_Json.Visible =
				c.Debug.EnableDebugMenu;

			StripStatus.Visible = c.Life.ShowStatusBar;

			Load で TopMost を変更するとバグるため(前述)
			if (UIUpdateTimer.Enabled)
				TopMost = c.Life.TopMost;

			ClockFormat = c.Life.ClockFormat;

			Font = c.UI.MainFont;
			StripMenu.Font = Font;
			StripStatus.Font = Font;
			MainDockPanel.Skin.AutoHideStripSkin.TextFont = Font;
			MainDockPanel.Skin.DockPaneStripSkin.TextFont = Font;

			foreach (var uc in SubUCs)
			{
				uc.BackColor = this.BackColor;
				uc.ForeColor = this.ForeColor;
				if (uc is FormShipGroup)
				{ // 暂时不对舰队编成窗口应用主题
					uc.BackColor = SystemColors.Control;
					uc.ForeColor = SystemColors.ControlText;
				}
			}
			*/

			//StripStatus_Information.BackColor = System.Drawing.Color.Transparent;
			//StripStatus_Information.Margin = new Padding(-1, 1, -1, 0);

			/*
			if (c.Life.LockLayout)
			{
				MainDockPanel.AllowChangeLayout = false;
				FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			}
			else
			{
				MainDockPanel.AllowChangeLayout = true;
				FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
			}

			StripMenu_File_Layout_LockLayout.Checked = c.Life.LockLayout;
			MainDockPanel.CanCloseFloatWindowInLock = c.Life.CanCloseFloatWindowInLock;

			StripMenu_File_Layout_TopMost.Checked = c.Life.TopMost;

			StripMenu_File_Notification_MuteAll.Checked = Notifier.NotifierManager.Instance.GetNotifiers().All(n => n.IsSilenced);

			if (!c.Control.UseSystemVolume)
				_volumeUpdateState = -1;
			*/
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (Utility.Configuration.Config.Life.ConfirmOnClosing)
			{
				if (MessageBox.Show("Are you sure you want to exit?", "Electronic Observer", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No)
					== MessageBoxResult.No)
				{
					e.Cancel = true;
					return;
				}
			}


			Utility.Logger.Add(2, SoftwareInformation.SoftwareNameEnglish + Properties.Resources.IsClosing);

			//UIUpdateTimer.Stop();

			ucBrowser.CloseBrowser();

			UpdatePlayTime();


			SystemEvents.OnSystemShuttingDown();


			SaveLayout(Configuration.Config.Life.LayoutFilePath);


			// 音量の保存
			{
				try
				{
					uint id = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
					Utility.Configuration.Config.Control.LastVolume = BrowserLibCore.VolumeManager.GetApplicationVolume(id);
					Utility.Configuration.Config.Control.LastIsMute = BrowserLibCore.VolumeManager.GetApplicationMute(id);

				}
				catch (Exception)
				{
					/* ぷちっ */
				}

			}
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			//NotifierManager.Instance.ApplyToConfiguration();
			Utility.Configuration.Instance.Save();
			RecordManager.Instance.SavePartial();
			KCDatabase.Instance.Save();
			APIObserver.Instance.Stop();


			Utility.Logger.Add(2, Properties.Resources.ClosingComplete);

			if (Utility.Configuration.Config.Log.SaveLogFlag)
				Utility.Logger.Save();

		}

		private void MI_File_Layout_Load_Click(object sender, RoutedEventArgs e)
		{
			LoadLayout("");
		}

		private void MI_File_Layout_Save_Click(object sender, RoutedEventArgs e)
		{
			SaveLayout("");
		}

		private void MI_View_Browser_Click(object sender, RoutedEventArgs e)
		{
			var anchorable = new LayoutAnchorable()
			{
				Title = "Browser",
				Content = ucBrowser,
				ContentId = "cefBrowser"
			};
			anchorable.AddToLayout(dockManager, AnchorableShowStrategy.Most);
			anchorable.Float();
		}

		private void MI_View_Log_Click(object sender, RoutedEventArgs e)
		{
			var anchorable = new LayoutAnchorable()
			{
				Title = "Log",
				Content = ucLog,
				ContentId = "log"
			};
			anchorable.AddToLayout(dockManager, AnchorableShowStrategy.Most);
			anchorable.Float();
		}

		private void MI_View_Fleet1_Click(object sender, RoutedEventArgs e)
		{
			var anchorable = new LayoutAnchorable()
			{
				Title = "Fleet1",
				Content = ucFleet[0],
				ContentId = "fleet1"
			};
			anchorable.AddToLayout(dockManager, AnchorableShowStrategy.Most);
			anchorable.Float();
		}

		private void MI_View_Fleet2_Click(object sender, RoutedEventArgs e)
		{
			var anchorable = new LayoutAnchorable()
			{
				Title = "Fleet2",
				Content = ucFleet[1],
				ContentId = "fleet2"
			};
			anchorable.AddToLayout(dockManager, AnchorableShowStrategy.Most);
			anchorable.Float();
		}

		private void MI_View_Fleet3_Click(object sender, RoutedEventArgs e)
		{
			var anchorable = new LayoutAnchorable()
			{
				Title = "Fleet3",
				Content = ucFleet[2],
				ContentId = "fleet3"
			};
			anchorable.AddToLayout(dockManager, AnchorableShowStrategy.Most);
			anchorable.Float();
		}

		private void MI_View_Fleet4_Click(object sender, RoutedEventArgs e)
		{
			var anchorable = new LayoutAnchorable()
			{
				Title = "Fleet4",
				Content = ucFleet[3],
				ContentId = "fleet4"
			};
			anchorable.AddToLayout(dockManager, AnchorableShowStrategy.Most);
			anchorable.Float();
		}

		private void MI_View_HQ_Click(object sender, RoutedEventArgs e)
		{
			var anchorable = new LayoutAnchorable()
			{
				Title = "HQ",
				Content = ucHeadquarters,
				ContentId = "hq"
			};
			anchorable.AddToLayout(dockManager, AnchorableShowStrategy.Most);
			anchorable.Float();
		}

		private void MI_View_Battle_Click(object sender, RoutedEventArgs e)
		{
			var anchorable = new LayoutAnchorable()
			{
				Title = "Battle",
				Content = ucBattle,
				ContentId = "battle"
			};
			anchorable.AddToLayout(dockManager, AnchorableShowStrategy.Most);
			anchorable.Float();
		}
	}
}