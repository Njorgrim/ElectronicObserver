using System.Windows;

namespace WPFBrowser.ExtraBrowser
{
    public partial class WPFExtraBrowser : Window
    {
        public WPFExtraBrowser()
        {
            InitializeComponent();
        }

        private void GoToDmm(object sender, RoutedEventArgs e)
        {
	        Browser.Load("https://point.dmm.com/choice/pay");
        }

        private void GoToAkashiList(object sender, RoutedEventArgs e)
        {
			Browser.Load("https://akashi-list.me/");
		}
    }
}
