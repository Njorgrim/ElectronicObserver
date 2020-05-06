using ElectronicObserver.WinFormsEO;
using System.Windows;
using System.Windows.Controls;

namespace ElectronicObserver.WPFEO
{
	public partial class WPFBrowserHost : UserControl
	{

		private static WPFBrowserHost _instance;
		public static WPFBrowserHost Instance => _instance;

		private FormBrowserHost browserForm;
		public WPFBrowserHost(WPFMain parent)
		{
			InitializeComponent();

			var host = new System.Windows.Forms.Integration.WindowsFormsHost();

			browserForm = new FormBrowserHost(this);
			browserForm.TopLevel = false;

			host.Child = browserForm;
			this.Grid_Root.Children.Add(host);
		}

		public void InitializeApiCompleted()
		{
			browserForm.InitializeApiCompleted();
		}

		public void CloseBrowser()
		{
			browserForm.CloseBrowser();
		}
	}
}
