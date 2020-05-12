using ElectronicObserver.Data;
using ElectronicObserver.Observer;
using ElectronicObserver.Utility.Data;
using ElectronicObserver.WPFEO.Fleet;
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
	/// Interaction logic for WPFFleet.xaml
	/// </summary>
	public partial class WPFFleet : UserControl
	{
		List<WPFShipData> ShipList;
		public int fleetnum;

		public WPFFleet(WPFMain parent, int fleetnum)
		{
			InitializeComponent();

			ShipList = new List<WPFShipData>();
			this.fleetnum = fleetnum;

		}



		public void Update(FleetData fleet)
		{
			if (fleet == null)
			{
				//Clear list
				ShipList.Clear();
				return;
			}

			//Clear list
			ShipList.Clear();
			//Add all ships in respective fleet to list
			foreach (ShipData ship in KCDatabase.Instance.Fleet[fleetnum].MembersInstance)
			{
				if (ship != null)
					ShipList.Add(new WPFShipData(this));
			}
			//Update all ships' datasets
			for (int i = 0; i < ShipList.Count; i++)
			{
				ShipList[i].Update(fleet.MembersInstance[i]);
			}
			//Update fleet dataset

			//Apply To GUI
			SP_ShipList.Children.Clear();
			foreach (WPFShipData ship in ShipList)
			{
				SP_ShipList.Children.Add(ship);
			}

			//Update Fleet Condition TODO
			UpdateFleetCondition();

			UpdateAirPower();

			UpdateLOS();
		}

		private void UpdateLOS()
		{
			//TODO: Display LoS depending on equipment weight.
			double los = Calculator.GetSearchingAbility_New33(KCDatabase.Instance.Fleet[fleetnum], /* TODO: Weight */ 1, KCDatabase.Instance.Admiral.Level);
			OTB_FleetLOS.Text = (Math.Truncate(100*los)/100).ToString();
		}

		private void UpdateAirPower()
		{
			OTB_FleetAirPower.Text = ((int)Calculator.GetAirSuperiority(KCDatabase.Instance.Fleet[fleetnum])).ToString();
		}

		private void UpdateFleetCondition()
		{
			bool isResupplied = true;
			bool isSparkled = true;
			bool isHeavilyDamaged = false;
			bool isOnExpedition = (KCDatabase.Instance.Fleet[fleetnum].ExpeditionState != 0);
			bool isOnSortie = (KCDatabase.Instance.Fleet[fleetnum].IsInSortie);
			foreach (WPFShipData ship in ShipList)
			{
				if (!ship.isFullySupplied())
					isResupplied = false;

				if (!(ship.morale >= 50))
					isSparkled = false;

				if ((double)ship.hpCurrent / ship.hpMax <= 0.25)
					isHeavilyDamaged=true;
			}

			if (fleetnum == 1 && ((double)ShipList[0].hpCurrent / ShipList[0].hpMax <= 0.5) && KCDatabase.Instance.Fleet.CombinedFlag != 0)
			{
				Img_FleetCondition.Source = (ImageSource)Application.Current.Resources["Icon_Unused_ShipState_damageM"];
				OTB_FleetCondition.Text = "Flag Damaged!";
			}
			else if (isHeavilyDamaged && !isOnExpedition)
			{
				Img_FleetCondition.Source = (ImageSource)Application.Current.Resources["Icon_Fleet_Damaged"];
				OTB_FleetCondition.Text = "Heavy Damage!";
			}
			else if (!isResupplied && !isHeavilyDamaged && !isOnExpedition && !isOnSortie)
			{
				Img_FleetCondition.Source = (ImageSource)Application.Current.Resources["Icon_Fleet_NotReplenished"];
				OTB_FleetCondition.Text = "Need Supplies!";
			}
			else if (isResupplied && !isHeavilyDamaged && isSparkled && !isOnExpedition)
			{
				Img_FleetCondition.Source = (ImageSource)Application.Current.Resources["Icon_Condition_Sparkle"];
				OTB_FleetCondition.Text = "Sparkled!";
			}
			else if (isResupplied && !isHeavilyDamaged && !isSparkled && !isOnExpedition)
			{
				Img_FleetCondition.Source = (ImageSource)Application.Current.Resources["Icon_Fleet_Ready"];
				OTB_FleetCondition.Text = "Idle";
			}
			else if (isOnExpedition)
			{
				Img_FleetCondition.Source = (ImageSource)Application.Current.Resources["Icon_Fleet_Expedition"];
				switch (KCDatabase.Instance.Fleet[fleetnum].ExpeditionState)
				{
					case 3:
						OTB_FleetCondition.Text = "Retreated!";
						break;
					case 2:
						OTB_FleetCondition.Text = "Returned!";
						break;
					case 1:
						OTB_FleetCondition.Text = "On Expedition";
						break;
					default: break;
				}
			}
			else if (isOnSortie && !isHeavilyDamaged)
			{
				Img_FleetCondition.Source = (ImageSource)Application.Current.Resources["Icon_Fleet_Sortie"];
				OTB_FleetCondition.Text = "Good Luck!";
			}
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			APIObserver o = APIObserver.Instance;

			o["api_req_nyukyo/start"].RequestReceived += Updated;
			o["api_req_nyukyo/speedchange"].RequestReceived += Updated;
			o["api_req_hensei/change"].RequestReceived += Updated;
			o["api_req_kousyou/destroyship"].RequestReceived += Updated;
			o["api_req_member/updatedeckname"].RequestReceived += Updated;
			o["api_req_kaisou/remodeling"].RequestReceived += Updated;
			o["api_req_map/start"].RequestReceived += Updated;
			o["api_req_hensei/combined"].RequestReceived += Updated;
			o["api_req_kaisou/open_exslot"].RequestReceived += Updated;
			o["api_port/port"].ResponseReceived += Updated;
			o["api_get_member/ship2"].ResponseReceived += Updated;
			o["api_get_member/ndock"].ResponseReceived += Updated;
			o["api_req_kousyou/getship"].ResponseReceived += Updated;
			o["api_req_hokyu/charge"].ResponseReceived += Updated;
			o["api_req_kousyou/destroyship"].ResponseReceived += Updated;
			o["api_get_member/ship3"].ResponseReceived += Updated;
			o["api_req_kaisou/powerup"].ResponseReceived += Updated;        //requestのほうは面倒なのでこちらでまとめてやる
			o["api_get_member/deck"].ResponseReceived += Updated;
			o["api_get_member/slot_item"].ResponseReceived += Updated;
			o["api_req_map/start"].ResponseReceived += Updated;
			o["api_req_map/next"].ResponseReceived += Updated;
			o["api_get_member/ship_deck"].ResponseReceived += Updated;
			o["api_req_hensei/preset_select"].ResponseReceived += Updated;
			o["api_req_kaisou/slot_exchange_index"].ResponseReceived += Updated;
			o["api_get_member/require_info"].ResponseReceived += Updated;
			o["api_req_kaisou/slot_deprive"].ResponseReceived += Updated;
			o["api_req_kaisou/marriage"].ResponseReceived += Updated;
			o["api_req_map/anchorage_repair"].ResponseReceived += Updated;

			Update(KCDatabase.Instance.Fleet.Fleets[fleetnum]);
			//Utility.Configuration.Instance.ConfigurationChanged += ConfigurationChanged;
		}

		private void Updated(string apiname, dynamic data)
		{
			KCDatabase db = KCDatabase.Instance;

			if (db.Ships.Count == 0) return;

			FleetData fleet = db.Fleet.Fleets[fleetnum];
			if (fleet == null) return;

			Update(fleet);
		}
	}
}
