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
	/// Interaction logic for WPFInformation.xaml
	/// </summary>
	public partial class WPFInformation : UserControl
	{
		public WPFInformation(FormInformation formInformation)
		{
			InitializeComponent();

			formInformation.TopLevel = false;
			WindowsFormsHost.Child = formInformation;
		}
	}
}
