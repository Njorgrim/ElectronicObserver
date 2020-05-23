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
using ElectronicObserver.WinFormsEO;

namespace ElectronicObserver.WPFEO
{
	/// <summary>
	/// Interaction logic for WPFFleetOverview.xaml
	/// </summary>
	public partial class WPFFleetOverview : UserControl
	{
		public WPFFleetOverview(FormFleetOverview formFleetOverview)
		{
			InitializeComponent();

			formFleetOverview.TopLevel = false;
			WindowsFormsHost.Child = formFleetOverview;
		}
	}
}
