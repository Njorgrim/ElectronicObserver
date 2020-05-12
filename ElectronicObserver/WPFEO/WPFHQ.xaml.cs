using ElectronicObserver.Data;
using ElectronicObserver.Observer;
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
	/// Interaction logic for WPFHQ.xaml
	/// </summary>
	public partial class WPFHQ : UserControl
	{
		public WPFHQ(WPFMain parent)
		{
			InitializeComponent();
		}

		public void Update()
		{
			//Update Player Metadata
			UpdateMetadata();

			//Update Resources
			UpdateResources();
		}

		public void UpdateMetadata()
		{
			OTB_PlayerName.Text = KCDatabase.Instance.Admiral.AdmiralName;
			OTB_PlayerRank.Text = KCDatabase.Instance.Admiral.RankString;
			OTB_PlayerMessage.Text= KCDatabase.Instance.Admiral.Comment;

			OTB_PlayerLevel.Text = KCDatabase.Instance.Admiral.Level.ToString();
			OTB_PlayerExp.Text = KCDatabase.Instance.Admiral.Exp.ToString();

			OTB_PlayerShips.Text = KCDatabase.Instance.Ships.Count.ToString() + "/" + KCDatabase.Instance.Admiral.MaxShipCount.ToString();
			OTB_PlayerEquips.Text = KCDatabase.Instance.Equipments.Count.ToString() + "/" + KCDatabase.Instance.Admiral.MaxEquipmentCount.ToString();
		}

		public void UpdateResources()
		{
			OTB_Buckets.Text = KCDatabase.Instance.Material.InstantRepair.ToString();
			OTB_Flamethrowers.Text = KCDatabase.Instance.Material.InstantConstruction.ToString();
			OTB_Devmats.Text = KCDatabase.Instance.Material.DevelopmentMaterial.ToString();
			OTB_Screws.Text = KCDatabase.Instance.Material.ModdingMaterial.ToString();
			OTB_Coins.Text = KCDatabase.Instance.Admiral.FurnitureCoin.ToString();

			//TODO: display seasonal item amount

			OTB_Fuel.Text = KCDatabase.Instance.Material.Fuel.ToString();
			OTB_Ammo.Text = KCDatabase.Instance.Material.Ammo.ToString();
			OTB_Steel.Text = KCDatabase.Instance.Material.Steel.ToString();
			OTB_Bauxite.Text = KCDatabase.Instance.Material.Bauxite.ToString();
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			APIObserver o = APIObserver.Instance;

			o.APIList["api_req_nyukyo/start"].RequestReceived += Updated;
			o.APIList["api_req_nyukyo/speedchange"].RequestReceived += Updated;
			o.APIList["api_req_kousyou/createship"].RequestReceived += Updated;
			o.APIList["api_req_kousyou/createship_speedchange"].RequestReceived += Updated;
			o.APIList["api_req_kousyou/destroyship"].RequestReceived += Updated;
			o.APIList["api_req_kousyou/destroyitem2"].RequestReceived += Updated;
			o.APIList["api_req_member/updatecomment"].RequestReceived += Updated;

			o.APIList["api_get_member/basic"].ResponseReceived += Updated;
			o.APIList["api_get_member/slot_item"].ResponseReceived += Updated;
			o.APIList["api_port/port"].ResponseReceived += Updated;
			o.APIList["api_get_member/ship2"].ResponseReceived += Updated;
			o.APIList["api_req_kousyou/getship"].ResponseReceived += Updated;
			o.APIList["api_req_hokyu/charge"].ResponseReceived += Updated;
			o.APIList["api_req_kousyou/destroyship"].ResponseReceived += Updated;
			o.APIList["api_req_kousyou/destroyitem2"].ResponseReceived += Updated;
			o.APIList["api_req_kaisou/powerup"].ResponseReceived += Updated;
			o.APIList["api_req_kousyou/createitem"].ResponseReceived += Updated;
			o.APIList["api_req_kousyou/remodel_slot"].ResponseReceived += Updated;
			o.APIList["api_get_member/material"].ResponseReceived += Updated;
			o.APIList["api_get_member/ship_deck"].ResponseReceived += Updated;
			o.APIList["api_req_air_corps/set_plane"].ResponseReceived += Updated;
			o.APIList["api_req_air_corps/supply"].ResponseReceived += Updated;
			o.APIList["api_get_member/useitem"].ResponseReceived += Updated;
		}

		private void Updated(string apiname, dynamic data)
		{
			Update();
		}
	}
}
