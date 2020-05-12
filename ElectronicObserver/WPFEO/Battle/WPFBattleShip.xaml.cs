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

namespace ElectronicObserver.WPFEO.Battle
{
	/// <summary>
	/// Interaction logic for WPFBattleShip.xaml
	/// </summary>
	public partial class WPFBattleShip : UserControl
	{
		public int HP { get; set; }
		public int PrevHP { get; set; }
		public int MaxHP { get; set; }
		public string ShipType { get; set; }
		public WPFBattleShip(WPFBattle parent)
		{
			InitializeComponent();
		}

		internal void Update()
		{
			OTB_HPCurrent.Fill = Brushes.White;
			OTB_ShipClass.Text = ShipType;

			OTB_HPCurrent.Text = HP.ToString();
			OTB_HPLost.Text = "-" + (PrevHP - HP).ToString();

			int hpp = (int)((double)HP / MaxHP * 100);
			int hpplost = (int)((double)PrevHP / MaxHP * 100);

			switch (hpp)
			{
				case int n when (n == 100): Grid_ShipHP.Background = Brushes.CornflowerBlue; break;
				case int n when (n < 100 && n > 75): Grid_ShipHP.Background = Brushes.ForestGreen; break;
				case int n when (n <= 75 && n > 50): Grid_ShipHP.Background = Brushes.Gold; break;
				case int n when (n <= 50 && n > 25): Grid_ShipHP.Background = Brushes.Orange; break;
				case int n when (n <= 25 && n > 0): Grid_ShipHP.Background = Brushes.Red; break;
				case int n when (n == 0): Grid_ShipHP.Background = Brushes.Gray; break;
			}

			Grid_ShipHP.Width = Math.Max(((double)75 * hpp/100), 0);
			Grid_ShipHPLost.Width = Math.Max(((double)75 * hpplost/100), 0);
		}
	}
}
