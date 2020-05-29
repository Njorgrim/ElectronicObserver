using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using BrowserLibCore;
using MessageBox = System.Windows.MessageBox;
using UserControl = System.Windows.Controls.UserControl;

namespace ElectronicObserver.WPFEO
{
	public partial class WPFBrowserHost : UserControl
	{

		private static WPFBrowserHost _instance;
		public static WPFBrowserHost Instance => _instance;


		public WPFBrowserHost()
		{
			InitializeComponent();

			_instance = this;
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			// this can get called multiple times...
		}

		public void RequestNavigation(string baseurl)
		{
			using var dialog =
				new WinFormsEO.Dialog.DialogTextInput(Properties.Resources.AskNavTitle, Properties.Resources.AskNavText)
				{
					InputtedText = baseurl
				};

			if (dialog.ShowDialog() == DialogResult.OK)
			{
				Browser.Navigate(dialog.InputtedText);
			}
		}


		public async void ClearCache()
		{
			Utility.Logger.Add(2, "キャッシュの削除を開始するため、ブラウザを終了しています…");

			Browser.CloseBrowser();

			await ClearCacheAsync();

			Utility.Logger.Add(2, "キャッシュの削除処理が終了しました。ブラウザを再起動しています…");

			try
			{
				Browser.RestartBrowser();
			}
			catch (Exception ex)
			{
				Utility.ErrorReporter.SendErrorReport(ex, "ブラウザの再起動に失敗しました。");
				MessageBox.Show("ブラウザプロセスの再起動に失敗しました。\r\n申し訳ありませんが本ツールを一旦終了してください。", ":(", MessageBoxButton.OK,
					MessageBoxImage.Error);
			}
		}

		private async Task ClearCacheAsync()
		{
			int trial;
			Exception? lastException = null;
			DirectoryInfo dir = new DirectoryInfo(BrowserConstants.CachePath);

			for (trial = 0; trial < 4; trial++)
			{
				try
				{
					dir.Refresh();

					if (dir.Exists)
						dir.Delete(true);
					else
						break;

					for (int i = 0; i < 10; i++)
					{
						dir.Refresh();
						if (dir.Exists)
						{
							await Task.Delay(50);
						}
						else break;
					}

					if (!dir.Exists)
					{
						break;
					}
				}
				catch (Exception ex)
				{
					lastException = ex;
					await Task.Delay(500);
				}
			}

			if (trial == 4)
			{
				Utility.ErrorReporter.SendErrorReport(lastException!, "キャッシュの削除に失敗しました。");
			}
		}

		// todo this should be useless
		public void CloseBrowser()
		{
			try
			{
				if (Browser == null)
				{
					// ブラウザを開いていない場合はnullなので
					return;
				}

				Browser.CloseBrowser();
			}
			catch (Exception ex)
			{
				//ブラウザプロセスが既に終了していた場合など
				Utility.ErrorReporter.SendErrorReport(ex, Properties.Resources.BrowserCloseError);
			}

		}
	}
}
