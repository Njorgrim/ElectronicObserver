using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ElectronicObserver.WPFEO
{
	/// <summary>
	/// Interaction logic for WPFLog.xaml
	/// </summary>
	public partial class WPFLog : UserControl
	{
		public WPFLog(WPFMain parent)
		{
			InitializeComponent();
		}

		private bool IsLoaded { get; set; }

		private void WPFLog_Loaded(object sender, RoutedEventArgs e)
		{
			if (IsLoaded) return;

			IsLoaded = true;

			foreach (var log in Utility.Logger.Log)
			{
				if (log.Priority >= Utility.Configuration.Config.Log.LogLevel)
					Logger_LogAdded(log.ToString());
			}
			//TB_Log.TopIndex = TB_Log.Items.Count - 1;

			Utility.Logger.Instance.LogAdded += new Utility.LogAddedEventHandler((Utility.Logger.LogData data) =>
			{
				if (Dispatcher.CheckAccess())
				{
					// Invokeはメッセージキューにジョブを投げて待つので、別のBeginInvokeされたジョブが既にキューにあると、
					// それを実行してしまい、BeginInvokeされたジョブの順番が保てなくなる
					// GUIスレッドによる処理は、順番が重要なことがあるので、GUIスレッドからInvokeを呼び出してはいけない
					Dispatcher.Invoke(new Utility.LogAddedEventHandler(Logger_LogAdded), data);
				}
				else
				{
					Logger_LogAdded(data);
				}
			});

			Utility.Configuration.Instance.ConfigurationChanged += ConfigurationChanged;

			//Icon = ResourceManager.ImageToIcon(ResourceManager.Instance.Icons.Images[(int)ResourceManager.IconContent.FormLog]);
		}

		void Logger_LogAdded(Utility.Logger.LogData data)
		{
			TB_Log.Text += data.ToString() + "\n";
		}

		void Logger_LogAdded(string msg)
		{
			TB_Log.Text += msg.ToString() + "\n";
		}

		void ConfigurationChanged()
		{

			//TB_Log.FontFamily = Utility.Configuration.Config.UI.MainFont;
			var Fcolor = Utility.Configuration.Config.UI.ForeColor;
			TB_Log.Foreground = new SolidColorBrush(Color.FromArgb(Fcolor.A, Fcolor.R, Fcolor.G, Fcolor.B));
			var Bcolor = Utility.Configuration.Config.UI.BackColor;
			TB_Log.Background = new SolidColorBrush(Color.FromArgb(Bcolor.A, Bcolor.R, Bcolor.G, Bcolor.B));
		}
	}
}
