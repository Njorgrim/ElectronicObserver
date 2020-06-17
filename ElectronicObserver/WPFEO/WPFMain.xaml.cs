using System;
using System.Linq;
using System.Windows;
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
using System.Threading;
using System.Threading.Tasks;
using ElectronicObserver.WinFormsEO;
using ElectronicObserver.WinFormsEO.Dialog;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using ElectronicObserver.WinFormsEO.Dialog.KancolleProgress;
using ElectronicObserver.WPFEO.Fleet;

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

		private DispatcherTimer UIUpdateTimer { get; }

		#region Forms

		private List<UserControl> SubUserControls { get; set; }

		private WPFFleet[] UserControlFleets { get; set; }
		private WPFFleetOverview UserControlFleetOverview { get; set; }

		private WPFShipGroup UserControlShipGroup { get; set; }
		// public FormXPCalculator ucXPCalculator;

		private WPFDock UserControlDock { get; set; }
		private WPFArsenal UserControlArsenal { get; set; }
		private WPFBaseAirCorps UserControlBaseAirCorps { get; set; }

		private WPFHQ UserControlHeadquarters { get; set; }
		private WPFQuest UserControlQuest { get; set; }
		private WPFInformation UserControlInformation { get; set; }

		private WPFCompass UserControlCompass { get; set; }
		private WPFBattle UserControlBattle { get; set; }

		public WPFBrowserHost UserControlBrowser { get; private set; }

		private WPFLog UserControlLog { get; set; }
		// public FormWindowCapture ucWindowCapture;
		// public FormJson ucJson;

		#endregion


		public WPFMain()
		{
			InitializeComponent();

			Instance = this;

			this.DataContext = this;

			UIUpdateTimer = new DispatcherTimer
			{
				Interval = TimeSpan.FromSeconds(1)
			};

			UIUpdateTimer.Tick += Timer_Tick;
		}

		private void Timer_Tick(object? sender, EventArgs e)
		{
			SystemEvents.OnUpdateTimerTick();
		}

		/* todo 
		private void UIUpdateTimer_Tick(object sender, EventArgs e)
		{

			SystemEvents.OnUpdateTimerTick();

			// 東京標準時
			DateTime now = Utility.Mathematics.DateTimeHelper.GetJapanStandardTimeNow();

			switch (ClockFormat)
			{
				case 0: //時計表示
					var pvpReset = now.Date.AddHours(3);
					while (pvpReset < now)
						pvpReset = pvpReset.AddHours(12);
					var pvpTimer = pvpReset - now;

					var questReset = now.Date.AddHours(5);
					if (questReset < now)
						questReset = questReset.AddHours(24);
					var questTimer = questReset - now;

					DateTime maintDate = now;
					TimeSpan maintTimer = now - now;
					if (SoftwareUpdater.MaintState != 0)
					{
						maintDate = DateTimeHelper.CSVStringToTime(SoftwareUpdater.MaintDate);
						if (maintDate < now)
							maintDate = now;
						maintTimer = maintDate - now;
					}

					string maintState, message;
					switch (SoftwareUpdater.MaintState)
					{
						case 1:
							message = maintDate > now ? "Event starts in" : "Event has started!";
							break;
						case 2:
							message = maintDate > now ? "Event ends in" : "Event period has ended.";
							break;
						case 3:
							message = maintDate > now ? "Maintenance starts in" : "Maintenance has started.";
							break;
						default:
							message = string.Empty;
							break;
					}

					if (maintDate > now)
					{
						var hours = $"{maintTimer.Days}d {maintTimer.Hours}h";
						if ((int)maintTimer.TotalHours < 24)
							hours = $"{maintTimer.Hours}h";
						maintState = $"{message} {hours} {maintTimer.Minutes}m {maintTimer.Seconds}s";
					}
					else
						maintState = message;

					var resetMsg =
						$"Next PVP reset: {(int)pvpTimer.TotalHours:D2}:{pvpTimer.Minutes:D2}:{pvpTimer.Seconds:D2}\r\n" +
						$"Next Quest reset: {(int)questTimer.TotalHours:D2}:{questTimer.Minutes:D2}:{questTimer.Seconds:D2}\r\n" +
						$"{maintState}";

					StripStatus_Clock.Text = now.ToString("HH\\:mm\\:ss");
					StripStatus_Clock.ToolTipText = now.ToString("yyyy\\/MM\\/dd (ddd)\r\n") + resetMsg;

					break;

				case 1: //演習更新まで
					{
						var border = now.Date.AddHours(3);
						while (border < now)
							border = border.AddHours(12);

						var ts = border - now;
						StripStatus_Clock.Text = string.Format("{0:D2}:{1:D2}:{2:D2}", (int)ts.TotalHours, ts.Minutes, ts.Seconds);
						StripStatus_Clock.ToolTipText = now.ToString("yyyy\\/MM\\/dd (ddd) HH\\:mm\\:ss");

					}
					break;

				case 2: //任務更新まで
					{
						var border = now.Date.AddHours(5);
						if (border < now)
							border = border.AddHours(24);

						var ts = border - now;
						StripStatus_Clock.Text = string.Format("{0:D2}:{1:D2}:{2:D2}", (int)ts.TotalHours, ts.Minutes, ts.Seconds);
						StripStatus_Clock.ToolTipText = now.ToString("yyyy\\/MM\\/dd (ddd) HH\\:mm\\:ss");

					}
					break;
			}


			// WMP コントロールによって音量が勝手に変えられてしまうため、前回終了時の音量の再設定を試みる。
			// 10回試行してダメなら諦める(例外によるラグを防ぐため)
			// 起動直後にやらないのはちょっと待たないと音量設定が有効にならないから
			if (_volumeUpdateState != -1 && _volumeUpdateState < 10 && Utility.Configuration.Config.Control.UseSystemVolume)
			{

				try
				{
					uint id = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
					float volume = Utility.Configuration.Config.Control.LastVolume;
					bool mute = Utility.Configuration.Config.Control.LastIsMute;

					BrowserLibCore.VolumeManager.SetApplicationVolume(id, volume);
					BrowserLibCore.VolumeManager.SetApplicationMute(id, mute);

					SyncBGMPlayer.Instance.SetInitialVolume((int)(volume * 100));
					foreach (var not in NotifierManager.Instance.GetNotifiers())
						not.SetInitialVolume((int)(volume * 100));

					_volumeUpdateState = -1;

				}
				catch (Exception)
				{

					_volumeUpdateState++;
				}
			}

		}*/

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
			var activeContent = ((LayoutRoot) sender).ActiveContent;
			if (e.PropertyName == "ActiveContent")
			{
				Debug.WriteLine(string.Format("ActiveContent-> {0}", activeContent));
			}
		}

		private void dockManager_DocumentClosing(object sender, DocumentClosingEventArgs e)
		{
			if (MessageBox.Show("Do you really want to close this tool?", "Electronic Observer (blah)",
				MessageBoxButton.YesNo) == MessageBoxResult.No)
				e.Cancel = true;
		}

		#region AvalonSerialization

		private void LoadLayout(string path)
		{
			if (File.Exists(@".\EODefaultLayout.config"))
			{
				var currentContentsList = dockManager.Layout.Descendents().OfType<LayoutContent>()
					.Where(c => c.ContentId != null).ToArray();

				var serializer = new XmlLayoutSerializer(dockManager);
				serializer.LayoutSerializationCallback += (s, args) =>
				{
					var prevContent = currentContentsList.FirstOrDefault(c => c.ContentId == args.Model.ContentId);
					if (prevContent != null)
						args.Content = prevContent.Content;
				};
				using var stream = new StreamReader(@".\EODefaultLayout.config");
				serializer.Deserialize(stream);

				currentContentsList = dockManager.Layout.Descendents().OfType<LayoutContent>()
					.Where(c => c.ContentId != null).ToArray();

				foreach (LayoutContent lc in currentContentsList)
				{
					lc.Content = lc.ContentId switch
					{
						"fleet1" => UserControlFleets[0],
						"fleet2" => UserControlFleets[1],
						"fleet3" => UserControlFleets[2],
						"fleet4" => UserControlFleets[3],
						"fleets" => UserControlFleetOverview,
						"group" => UserControlShipGroup,

						"dock" => UserControlDock,
						"arsenal" => UserControlArsenal,
						"ab" => UserControlBaseAirCorps,

						"hq" => UserControlHeadquarters,
						"quest" => UserControlQuest,
						"info" => UserControlInformation,

						"compass" => UserControlCompass,
						"battle" => UserControlBattle,

						"cefBrowser" => UserControlBrowser,
						"log" => UserControlLog,
						_ => lc.Content
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


			Configuration.Instance.Load();

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

			Logger.Add(2, SoftwareInformation.SoftwareNameEnglish + " is starting...");

			ResourceManager.Instance.Load();
			RecordManager.Instance.Load();
			KCDatabase.Instance.Load();
			NotifierManager.Instance.Initialize(this);
			SyncBGMPlayer.Instance.ConfigurationChanged();

			APIObserver.Instance.Start(Configuration.Config.Connection.Port, this);

			SubUserControls = new List<UserControl>();

			//form init
			//注：一度全てshowしないとイベントを受け取れないので注意
			UserControlFleets = new WPFFleet[4];
			for (int i = 0; i < UserControlFleets.Length; i++)
			{
				SubUserControls.Add(UserControlFleets[i] = new WPFFleet(this, i + 1));
			}
			SubUserControls.Add(UserControlFleetOverview = new WPFFleetOverview(new FormFleetOverview()));
			SubUserControls.Add(UserControlShipGroup = new WPFShipGroup(new FormShipGroup()));
			//SubUCs.Add(ucXPCalculator = new FormXPCalculator(this));

			SubUserControls.Add(UserControlDock = new WPFDock(new FormDock()));
			SubUserControls.Add(UserControlArsenal = new WPFArsenal(new FormArsenal()));
			SubUserControls.Add(UserControlBaseAirCorps = new WPFBaseAirCorps(new FormBaseAirCorps()));

			SubUserControls.Add(UserControlHeadquarters = new WPFHQ(this));
			SubUserControls.Add(UserControlQuest = new WPFQuest(new FormQuest()));
			SubUserControls.Add(UserControlInformation = new WPFInformation(new FormInformation()));

			SubUserControls.Add(UserControlCompass = new WPFCompass(new FormCompass()));
			SubUserControls.Add(UserControlBattle = new WPFBattle(this));

			SubUserControls.Add(UserControlBrowser = new WPFBrowserHost());
			SubUserControls.Add(UserControlLog = new WPFLog(this));
			//SubUCs.Add(ucWindowCapture = new FormWindowCapture(this));
			//SubUCs.Add(ucJson = new FormJson(this));

			ConfigurationChanged(); //設定から初期化

			LoadLayout(Configuration.Config.Life.LayoutFilePath);


#if false
			// todo reenable auto updates
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

					Activate(); // 上記ロードに時間がかかるとウィンドウが表示されなくなることがあるので
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
			UserControlBrowser.InitializeApiCompleted();

			UIUpdateTimer.Start();


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

							string[] files = Directory.GetFiles(parent,
								string.Format("*{0}@{1}.json", isRequest ? "Q" : "S", line.Replace('/', '@')),
								SearchOption.TopDirectoryOnly);

							if (files.Length == 0)
								continue;

							Array.Sort(files);

							using (StreamReader sr2 = new StreamReader(files[files.Length - 1]))
							{
								if (isRequest)
								{
									Dispatcher.Invoke((Action) (() =>
									{
										APIObserver.Instance.LoadRequest("/kcsapi/" + line, sr2.ReadToEnd());
									}));
								}
								else
								{
									Dispatcher.Invoke((Action) (() =>
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
			if (Configuration.Config.Life.ConfirmOnClosing)
			{
				if (MessageBox.Show("Are you sure you want to exit?", "Electronic Observer", MessageBoxButton.YesNo,
					    MessageBoxImage.Question, MessageBoxResult.No)
				    == MessageBoxResult.No)
				{
					e.Cancel = true;
					return;
				}
			}


			Logger.Add(2, SoftwareInformation.SoftwareNameEnglish + Properties.Resources.IsClosing);

			UIUpdateTimer.Stop();

			UserControlBrowser.CloseBrowser();

			UpdatePlayTime();


			SystemEvents.OnSystemShuttingDown();


			SaveLayout(Configuration.Config.Life.LayoutFilePath);


			// 音量の保存
			{
				try
				{
					uint id = (uint) Process.GetCurrentProcess().Id;
					Configuration.Config.Control.LastVolume = BrowserLib.VolumeManager.GetApplicationVolume(id);
					Configuration.Config.Control.LastIsMute = BrowserLib.VolumeManager.GetApplicationMute(id);
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
			Configuration.Instance.Save();
			RecordManager.Instance.SavePartial();
			KCDatabase.Instance.Save();
			APIObserver.Instance.Stop();


			Logger.Add(2, Properties.Resources.ClosingComplete);

			if (Configuration.Config.Log.SaveLogFlag)
				Logger.Save();

		}

		private void MI_File_Layout_Load_Click(object sender, RoutedEventArgs e)
		{
			LoadLayout("");
		}

		private void MI_File_Layout_Save_Click(object sender, RoutedEventArgs e)
		{
			SaveLayout("");
		}

		private void MI_File_Settings_OnClick(object sender, RoutedEventArgs e)
		{
			using var dialog = new DialogConfiguration(Configuration.Config);

			if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

			dialog.ToConfiguration(Configuration.Config);
			Configuration.Instance.OnConfigurationChanged();
		}

		#region View

		private void MI_View_Fleet1_Click(object sender, RoutedEventArgs e)
		{
			LayoutAnchorable anchorable = new LayoutAnchorable
			{
				Title = "Fleet1",
				Content = UserControlFleets[0],
				ContentId = "fleet1"
			};
			anchorable.AddToLayout(dockManager, AnchorableShowStrategy.Most);
			anchorable.Float();
		}

		private void MI_View_Fleet2_Click(object sender, RoutedEventArgs e)
		{
			LayoutAnchorable anchorable = new LayoutAnchorable
			{
				Title = "Fleet2",
				Content = UserControlFleets[1],
				ContentId = "fleet2"
			};
			anchorable.AddToLayout(dockManager, AnchorableShowStrategy.Most);
			anchorable.Float();
		}

		private void MI_View_Fleet3_Click(object sender, RoutedEventArgs e)
		{
			LayoutAnchorable anchorable = new LayoutAnchorable
			{
				Title = "Fleet3",
				Content = UserControlFleets[2],
				ContentId = "fleet3"
			};
			anchorable.AddToLayout(dockManager, AnchorableShowStrategy.Most);
			anchorable.Float();
		}

		private void MI_View_Fleet4_Click(object sender, RoutedEventArgs e)
		{
			LayoutAnchorable anchorable = new LayoutAnchorable
			{
				Title = "Fleet4",
				Content = UserControlFleets[3],
				ContentId = "fleet4"
			};
			anchorable.AddToLayout(dockManager, AnchorableShowStrategy.Most);
			anchorable.Float();
		}

		private void MI_View_FleetList_OnClick(object sender, RoutedEventArgs e)
		{
			LayoutAnchorable anchorable = new LayoutAnchorable
			{
				Title = "Fleets",
				Content = UserControlFleetOverview,
				ContentId = "fleets"
			};
			anchorable.AddToLayout(dockManager, AnchorableShowStrategy.Most);
			anchorable.Float();
		}

		private void MI_View_ShipGroup_OnClick(object sender, RoutedEventArgs e)
		{
			LayoutAnchorable anchorable = new LayoutAnchorable
			{
				Title = "Group",
				Content = UserControlShipGroup,
				ContentId = "group"
			};
			anchorable.AddToLayout(dockManager, AnchorableShowStrategy.Most);
			anchorable.Float();
		}

		private void MI_View_Dock_OnClick(object sender, RoutedEventArgs e)
		{
			LayoutAnchorable anchorable = new LayoutAnchorable
			{
				Title = "Dock",
				Content = UserControlDock,
				ContentId = "dock"
			};
			anchorable.AddToLayout(dockManager, AnchorableShowStrategy.Most);
			anchorable.Float();
		}

		private void MI_View_Arsenal_OnClick(object sender, RoutedEventArgs e)
		{
			LayoutAnchorable anchorable = new LayoutAnchorable
			{
				Title = "Arsenal",
				Content = UserControlArsenal,
				ContentId = "arsenal"
			};
			anchorable.AddToLayout(dockManager, AnchorableShowStrategy.Most);
			anchorable.Float();
		}

		private void MI_View_LBAS_OnClick(object sender, RoutedEventArgs e)
		{
			LayoutAnchorable anchorable = new LayoutAnchorable
			{
				Title = "AB",
				Content = UserControlBaseAirCorps,
				ContentId = "ab"
			};
			anchorable.AddToLayout(dockManager, AnchorableShowStrategy.Most);
			anchorable.Float();
		}

		private void MI_View_HQ_Click(object sender, RoutedEventArgs e)
		{
			LayoutAnchorable anchorable = new LayoutAnchorable
			{
				Title = "HQ",
				Content = UserControlHeadquarters,
				ContentId = "hq"
			};
			anchorable.AddToLayout(dockManager, AnchorableShowStrategy.Most);
			anchorable.Float();
		}

		private void MI_View_Quest_OnClick(object sender, RoutedEventArgs e)
		{
			LayoutAnchorable anchorable = new LayoutAnchorable
			{
				Title = "Quests",
				Content = UserControlQuest,
				ContentId = "quest"
			};
			anchorable.AddToLayout(dockManager, AnchorableShowStrategy.Most);
			anchorable.Float();
		}

		private void MI_View_Information_OnClick(object sender, RoutedEventArgs e)
		{
			LayoutAnchorable anchorable = new LayoutAnchorable
			{
				Title = "Info",
				Content = UserControlInformation,
				ContentId = "info"
			};
			anchorable.AddToLayout(dockManager, AnchorableShowStrategy.Most);
			anchorable.Float();
		}

		private void MI_View_Compass_OnClick(object sender, RoutedEventArgs e)
		{
			LayoutAnchorable anchorable = new LayoutAnchorable
			{
				Title = "Compass",
				Content = UserControlCompass,
				ContentId = "compass"
			};
			anchorable.AddToLayout(dockManager, AnchorableShowStrategy.Most);
			anchorable.Float();
		}

		private void MI_View_Battle_Click(object sender, RoutedEventArgs e)
		{
			LayoutAnchorable anchorable = new LayoutAnchorable
			{
				Title = "Battle",
				Content = UserControlBattle,
				ContentId = "battle"
			};
			anchorable.AddToLayout(dockManager, AnchorableShowStrategy.Most);
			anchorable.Float();
		}

		private void MI_View_Browser_Click(object sender, RoutedEventArgs e)
		{
			LayoutAnchorable anchorable = new LayoutAnchorable
			{
				Title = "Browser",
				Content = UserControlBrowser,
				ContentId = "cefBrowser"
			};
			anchorable.AddToLayout(dockManager, AnchorableShowStrategy.Most);
			anchorable.Float();
		}

		private void MI_View_Log_Click(object sender, RoutedEventArgs e)
		{
			LayoutAnchorable anchorable = new LayoutAnchorable
			{
				Title = "Log",
				Content = UserControlLog,
				ContentId = "log"
			};
			anchorable.AddToLayout(dockManager, AnchorableShowStrategy.Most);
			anchorable.Float();
		}

		#endregion

		#region Tools

		private void MI_Tools_EquipmentList_OnClick(object sender, RoutedEventArgs e)
		{
			new DialogEquipmentList().Show();
		}

		private void MI_Tools_DropRecord_OnClick(object sender, RoutedEventArgs e)
		{
			if (KCDatabase.Instance.MasterShips.Count == 0)
			{
				MessageBox.Show(GeneralRes.KancolleMustBeLoaded, GeneralRes.NoMasterData, MessageBoxButton.OK,
					MessageBoxImage.Error);
				return;
			}

			if (RecordManager.Instance.ShipDrop.Record.Count == 0)
			{
				MessageBox.Show(GeneralRes.NoDropData, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			new DialogDropRecordViewer().Show();
		}

		private void MI_Tools_DevelopmentRecord_OnClick(object sender, RoutedEventArgs e)
		{
			if (KCDatabase.Instance.MasterShips.Count == 0)
			{
				MessageBox.Show(GeneralRes.KancolleMustBeLoaded, GeneralRes.NoMasterData, MessageBoxButton.OK,
					MessageBoxImage.Error);
				return;
			}

			if (RecordManager.Instance.Development.Record.Count == 0)
			{
				MessageBox.Show(GeneralRes.NoDevData, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			new DialogDevelopmentRecordViewer().Show();
		}

		private void MI_Tools_ConstructionRecord_OnClick(object sender, RoutedEventArgs e)
		{
			if (KCDatabase.Instance.MasterShips.Count == 0)
			{
				MessageBox.Show(GeneralRes.KancolleMustBeLoaded, GeneralRes.NoMasterData, MessageBoxButton.OK,
					MessageBoxImage.Error);
				return;
			}

			if (RecordManager.Instance.Construction.Record.Count == 0)
			{
				MessageBox.Show(GeneralRes.NoBuildData, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			new DialogConstructionRecordViewer().Show();
		}

		private void MI_Tools_ResourceChart_OnClick(object sender, RoutedEventArgs e)
		{
			new DialogResourceChart().Show();
		}

		private void MI_Tools_ShipEncyclopedia_OnClick(object sender, RoutedEventArgs e)
		{
			if (KCDatabase.Instance.MasterShips.Count == 0)
			{
				MessageBox.Show("Ship data is not loaded.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			new DialogAlbumMasterShip().Show();
		}

		private void MI_Tools_EquipmentEncyclopedia_OnClick(object sender, RoutedEventArgs e)
		{
			if (KCDatabase.Instance.MasterEquipments.Count == 0)
			{
				MessageBox.Show("Equipment data is not loaded.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			new DialogAlbumMasterEquipment().Show();
		}

		private void MI_Tools_AADefense_OnClick(object sender, RoutedEventArgs e)
		{
			new DialogAntiAirDefense().Show();
		}

		private void MI_Tools_ExportFleetImage_OnClick(object sender, RoutedEventArgs e)
		{
			new DialogFleetImageGenerator(1).Show();
		}

		private void MI_Tools_LBASSimulator_OnClick(object sender, RoutedEventArgs e)
		{
			new DialogBaseAirCorpsSimulation().Show();
		}

		private void MI_Tools_ExpCalculator_OnClick(object sender, RoutedEventArgs e)
		{
			new DialogExpChecker().Show();
		}

		private void MI_Tools_ExpeditionCheck_OnClick(object sender, RoutedEventArgs e)
		{
			new DialogExpeditionCheck().Show();
		}

		private void MI_Tools_ShipProgressionList_OnClick(object sender, RoutedEventArgs e)
		{
			new DialogKancolleProgressWpf().Show();
		}

		private void MI_Tools_ExtraBrowser_OnClick(object sender, RoutedEventArgs e)
		{
			WPFBrowserHost.Instance.Browser.OpenExtraBrowser();
		}

		#endregion
	}
}