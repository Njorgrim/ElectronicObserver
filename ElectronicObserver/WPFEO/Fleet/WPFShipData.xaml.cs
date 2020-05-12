using ElectronicObserver.Data;
using ElectronicObserver.WPFEO.MiscControls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
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

namespace ElectronicObserver.WPFEO.Fleet
{
	/// <summary>
	/// Interaction logic for ShipData.xaml
	/// </summary>
	public partial class WPFShipData : UserControl
	{
		WPFFleet parent;

		public ShipData ship;
		public string? shipName;
		public int shipID, shipMasterID;
		public int level, expNext, morale;
		public int hpMax, hpCurrent;
		public int fuelMax, ammoMax;
		public int fuelCurrent, ammoCurrent;
		public int fpBase, fpTotal;
		public int tpBase, tpTotal;
		public int aaBase, aaTotal;
		public int armBase, armTotal;
		public int aswBase, aswTotal;
		public int evaBase, evaTotal;
		public int losBase, losTotal;
		public int luckTotal;
		public int range, speed;
		public EquipmentData[]? equipSlot;
		public int equipSlotAmount;
		public bool hasOpenRE, isEscaped;
		public int repairID;
		public WPFShipData(WPFFleet parent)
		{
			InitializeComponent();

			this.parent = parent;
		}

		public void Update(ShipData ship)
		{
			this.ship = ship;
			if (ship == null) return;
			//Value Update
			UpdateParams();

			//GUI update
			UpdateMeta();
			UpdateShipName();

			//HP handling.
			//Scales the HP bar depending on HP percentage (hpp) and colors it accordingly.
			int hpp = (int)((double)hpCurrent / hpMax * 100);
			UpdateHP(hpp);

			//Fuel/Ammo handling. Scales Fuel/Ammo bar according to percentage (fuelp and ammop)
			int fuelp = (int)((double)fuelCurrent / fuelMax * 100);
			int ammop = (int)((double)ammoCurrent / ammoMax * 100);
			UpdateSupply(fuelp, ammop);

			//Morale handling. Colors the bar background according to remaining morale.
			UpdateMorale();

			//Equip handling. Displays correct amount of equip circles and draws icons, plane rank, plane count and upgrades accordingly.
			UpdateEquipment();

			//Exp Bar handling. Displays experience to next level. TODO: Calculate percentage and apply to exp bar fill, to display progress.
			UpdateExperience();
		}

		private void UpdateParams()
		{
			//Value Update
			shipName = ship.MasterShip.NameWithClass;
			shipID = ship.ShipID;
			shipMasterID = ship.ID;
			level = ship.Level;
			expNext = ship.ExpNext;
			morale = ship.Condition;
			hpMax = ship.HPMax;
			hpCurrent = ship.HPCurrent;
			fuelMax = ship.FuelMax;
			ammoMax = ship.AmmoMax;
			fuelCurrent = ship.Fuel;
			ammoCurrent = ship.Ammo;
			fpBase = ship.FirepowerBase;
			fpTotal = ship.FirepowerTotal;
			tpBase = ship.TorpedoBase;
			tpTotal = ship.TorpedoTotal;
			aaBase = ship.AABase;
			aaTotal = ship.AATotal;
			armBase = ship.ArmorBase;
			armTotal = ship.ArmorTotal;
			aswBase = ship.ASWBase;
			aswTotal = ship.ASWTotal;
			evaBase = ship.EvasionBase;
			evaTotal = ship.EvasionTotal;
			losBase = ship.LOSBase;
			losTotal = ship.LOSTotal;
			luckTotal = ship.LuckTotal;
			range = ship.Range;
			speed = ship.Speed;
			hasOpenRE = ship.IsExpansionSlotAvailable;
			isEscaped = KCDatabase.Instance.Fleet[parent.fleetnum].EscapedShipList.Contains(shipMasterID);
			equipSlotAmount = ship.SlotSize;
			repairID = ship.RepairingDockID;
		}

		private void UpdateMeta()
		{
			OTB_Level.Text = level.ToString();
			OTB_ExpNext.Text = expNext.ToString();
		}

		private void UpdateHP(int hpp)
		{
			if (repairID != -1)
			{
				OTB_HP.Text = "Repairing => " + hpCurrent + "/" + hpMax;
				Grid_HP.Background = Brushes.DarkBlue;
				Grid_HP.Width = 192;
			}
			else if (isEscaped)
			{
				OTB_HP.Text = "Retreated => " + hpCurrent + "/" + hpMax;
				Grid_HP.Background = Brushes.Gray;
				Grid_HP.Width = 192;
			}
			else
			{
				OTB_HP.Text = hpCurrent + "/" + hpMax;

				switch (hpp)
				{
					case int n when (n == 100): Grid_HP.Background = Brushes.CornflowerBlue; break;
					case int n when (n < 100 && n > 75): Grid_HP.Background = Brushes.ForestGreen; break;
					case int n when (n <= 75 && n > 50): Grid_HP.Background = Brushes.Gold; break;
					case int n when (n <= 50 && n > 25): Grid_HP.Background = Brushes.Orange; break;
					case int n when (n <= 25 && n > 0): Grid_HP.Background = Brushes.Red; break;
					case int n when (n == 0): Grid_HP.Background = Brushes.Gray; OTB_HP.Text = "Sunk"; break;
				}
				Grid_HP.Width = ((double)192 * hpp/100);
			}
		}

		private void UpdateSupply(int fuelp, int ammop)
		{
			OTB_Fuel.Text = fuelp.ToString() + "%";
			Grid_Fuel.Width = ((double)46 * fuelp/100);
			OTB_Ammo.Text = ammop.ToString() + "%";
			Grid_Ammo.Width = ((double)45 * ammop/100);
		}

		private void UpdateMorale()
		{
			OTB_Morale.Text = morale.ToString();
			switch (morale)
			{
				case int n when (n > 49): Grid_Morale.Background = Brushes.Gold; break;
				case int n when (n >= 30): Grid_Morale.Background = Brushes.White; break;
				case int n when (n >= 20): Grid_Morale.Background = Brushes.Orange; break;
				case int n when (n >= 0): Grid_Morale.Background = Brushes.Red; break;
			}
		}

		private void UpdateEquipment()
		{
			/*
			equipSlot = new EquipmentData[equipSlotAmount];
			for (int i = 0; i < equipSlotAmount; i++)
			{
				equipSlot[i] = ship.SlotInstance[i];
			}
			*/
			//Hide all equip slots
			Grid_Equip1.Visibility = Visibility.Hidden;
			Grid_Equip2.Visibility = Visibility.Hidden;
			Grid_Equip3.Visibility = Visibility.Hidden;
			Grid_Equip4.Visibility = Visibility.Hidden;
			Grid_Equip5.Visibility = Visibility.Hidden;
			Grid_EquipRE.Visibility = Visibility.Hidden;
			Img_Equip1_PlaneRank.Visibility = Visibility.Collapsed;
			Img_Equip2_PlaneRank.Visibility = Visibility.Collapsed;
			Img_Equip3_PlaneRank.Visibility = Visibility.Collapsed;
			Img_Equip4_PlaneRank.Visibility = Visibility.Collapsed;
			Img_Equip5_PlaneRank.Visibility = Visibility.Collapsed;
			Img_EquipRE_PlaneRank.Visibility = Visibility.Collapsed;
			OTB_Equip1_Level.Visibility = Visibility.Collapsed;
			OTB_Equip2_Level.Visibility = Visibility.Collapsed;
			OTB_Equip3_Level.Visibility = Visibility.Collapsed;
			OTB_Equip4_Level.Visibility = Visibility.Collapsed;
			OTB_Equip5_Level.Visibility = Visibility.Collapsed;
			OTB_EquipRE_Level.Visibility = Visibility.Collapsed;
			OTB_Equip1_PlaneCount.Visibility = Visibility.Collapsed;
			OTB_Equip2_PlaneCount.Visibility = Visibility.Collapsed;
			OTB_Equip3_PlaneCount.Visibility = Visibility.Collapsed;
			OTB_Equip4_PlaneCount.Visibility = Visibility.Collapsed;
			OTB_Equip5_PlaneCount.Visibility = Visibility.Collapsed;
			OTB_EquipRE_PlaneCount.Visibility = Visibility.Collapsed;

			//Show available equip slots (including RE, if applicable)
			switch (equipSlotAmount)
			{
				case 5: Grid_Equip5.Visibility = Visibility.Visible; goto case 4;
				case 4: Grid_Equip4.Visibility = Visibility.Visible; goto case 3;
				case 3: Grid_Equip3.Visibility = Visibility.Visible; goto case 2;
				case 2: Grid_Equip2.Visibility = Visibility.Visible; goto case 1;
				case 1: Grid_Equip1.Visibility = Visibility.Visible; goto case 0;
				case 0: break;
				default: break;
			}

			if (hasOpenRE)
			{
				Grid_EquipRE.Visibility = Visibility.Visible;
			}

			int equippedSlotAmount = 0;
			foreach (EquipmentData eq in ship.SlotInstance)
			{
				if (eq != null)
					equippedSlotAmount++;
			}
			//Get Planecounts
			WritePlaneCount(OTB_Equip1_PlaneCount, ship.Aircraft[0]);
			WritePlaneCount(OTB_Equip2_PlaneCount, ship.Aircraft[1]);
			WritePlaneCount(OTB_Equip3_PlaneCount, ship.Aircraft[2]);
			WritePlaneCount(OTB_Equip4_PlaneCount, ship.Aircraft[3]);
			WritePlaneCount(OTB_Equip5_PlaneCount, ship.Aircraft[4]);

			//Get Equip Improvement Levels
			switch (equippedSlotAmount)
			{
				case 5: WriteImprovementLevel(OTB_Equip5_Level, ship.SlotInstance[4].Level); goto case 4;
				case 4: WriteImprovementLevel(OTB_Equip4_Level, ship.SlotInstance[3].Level); goto case 3;
				case 3: WriteImprovementLevel(OTB_Equip3_Level, ship.SlotInstance[2].Level); goto case 2;
				case 2: WriteImprovementLevel(OTB_Equip2_Level, ship.SlotInstance[1].Level); goto case 1;
				case 1: WriteImprovementLevel(OTB_Equip1_Level, ship.SlotInstance[0].Level); goto case 0;
				case 0: break;
				default: break;
			}

			if (hasOpenRE)
			{
				WriteImprovementLevel(OTB_EquipRE_Level, ship.ExpansionSlotInstance.Level);
			}

			//Get Plane Rank
			switch (equippedSlotAmount)
			{
				case 5: Img_Equip5_PlaneRank.Visibility = Visibility.Visible; goto case 4;
				case 4: Img_Equip4_PlaneRank.Visibility = Visibility.Visible; goto case 3;
				case 3: Img_Equip3_PlaneRank.Visibility = Visibility.Visible; goto case 2;
				case 2: Img_Equip2_PlaneRank.Visibility = Visibility.Visible; goto case 1;
				case 1: Img_Equip1_PlaneRank.Visibility = Visibility.Visible; goto case 0;
				case 0: break;
				default: break;
			}

			switch (equippedSlotAmount)
			{
				case 5: WritePlaneRank(Img_Equip5_PlaneRank, ship.SlotInstance[4].AircraftLevel); goto case 4;
				case 4: WritePlaneRank(Img_Equip4_PlaneRank, ship.SlotInstance[3].AircraftLevel); goto case 3;
				case 3: WritePlaneRank(Img_Equip3_PlaneRank, ship.SlotInstance[2].AircraftLevel); goto case 2;
				case 2: WritePlaneRank(Img_Equip2_PlaneRank, ship.SlotInstance[1].AircraftLevel); goto case 1;
				case 1: WritePlaneRank(Img_Equip1_PlaneRank, ship.SlotInstance[0].AircraftLevel); goto case 0;
				case 0: break;
				default: break;
			}

			switch (equippedSlotAmount)
			{
				case 5: SetEquipIcon(Img_Equip5, ship.SlotInstance[4].MasterEquipment.IconType); goto case 4;
				case 4: SetEquipIcon(Img_Equip4, ship.SlotInstance[3].MasterEquipment.IconType); goto case 3;
				case 3: SetEquipIcon(Img_Equip3, ship.SlotInstance[2].MasterEquipment.IconType); goto case 2;
				case 2: SetEquipIcon(Img_Equip2, ship.SlotInstance[1].MasterEquipment.IconType); goto case 1;
				case 1: SetEquipIcon(Img_Equip1, ship.SlotInstance[0].MasterEquipment.IconType); goto case 0;
				case 0: break;
				default: break;
			}

			if (hasOpenRE)
			{
				SetEquipIcon(Img_EquipRE, ship.ExpansionSlotInstance.MasterEquipment.IconType);
			}

		}

		public void SetEquipIcon(Image img, int type)
		{
			var icon = type switch
			{
				1 => Application.Current.Resources["Icon_Equipment_MainGunS"],
				2 => Application.Current.Resources["Icon_Equipment_MainGunM"],
				3 => Application.Current.Resources["Icon_Equipment_MainGunL"],
				4 => Application.Current.Resources["Icon_Equipment_SecondaryGun"],
				5 => Application.Current.Resources["Icon_Equipment_Torpedo"],
				6 => Application.Current.Resources["Icon_Equipment_CarrierBasedFighter"],
				7 => Application.Current.Resources["Icon_Equipment_CarrierBasedBomber"],
				8 => Application.Current.Resources["Icon_Equipment_CarrierBasedTorpedo"],
				9 => Application.Current.Resources["Icon_Equipment_CarrierBasedRecon"],
				10 => Application.Current.Resources["Icon_Equipment_Seaplane"],
				11 => Application.Current.Resources["Icon_Equipment_RADAR"],
				12 => Application.Current.Resources["Icon_Equipment_AAShell"],
				13 => Application.Current.Resources["Icon_Equipment_APShell"],
				14 => Application.Current.Resources["Icon_Equipment_DamageControl"],
				15 => Application.Current.Resources["Icon_Equipment_AAGun"],
				16 => Application.Current.Resources["Icon_Equipment_HighAngleGun"],
				17 => Application.Current.Resources["Icon_Equipment_DepthCharge"],
				18 => Application.Current.Resources["Icon_Equipment_SONAR"],
				19 => Application.Current.Resources["Icon_Equipment_Engine"],
				20 => Application.Current.Resources["Icon_Equipment_LandingCraft"],
				21 => Application.Current.Resources["Icon_Equipment_Autogyro"],
				22 => Application.Current.Resources["Icon_Equipment_ASWPatrol"],
				23 => Application.Current.Resources["Icon_Equipment_Bulge"],
				24 => Application.Current.Resources["Icon_Equipment_Searchlight"],
				25 => Application.Current.Resources["Icon_Equipment_DrumCanister"],
				26 => Application.Current.Resources["Icon_Equipment_RepairFacility"],
				27 => Application.Current.Resources["Icon_Equipment_Flare"],
				28 => Application.Current.Resources["Icon_Equipment_CommandFacility"],
				29 => Application.Current.Resources["Icon_Equipment_MaintenanceTeam"],
				30 => Application.Current.Resources["Icon_Equipment_AADirector"],
				31 => Application.Current.Resources["Icon_Equipment_RocketArtillery"],
				32 => Application.Current.Resources["Icon_Equipment_PicketCrew"],
				33 => Application.Current.Resources["Icon_Equipment_FlyingBoat"],
				34 => Application.Current.Resources["Icon_Equipment_Ration"],
				35 => Application.Current.Resources["Icon_Equipment_Supplies"],
				36 => Application.Current.Resources["Icon_Equipment_AmpibiousVehicle"],
				37 => Application.Current.Resources["Icon_Equipment_LandAttacker"],
				38 => Application.Current.Resources["Icon_Equipment_Interceptor"],
				39 => Application.Current.Resources["Icon_Equipment_JetFightingBomberKeiun"],
				40 => Application.Current.Resources["Icon_Equipment_JetFightingBomberKikka"],
				41 => Application.Current.Resources["Icon_Equipment_TransportMaterial"],
				42 => Application.Current.Resources["Icon_Equipment_SubmarineEquipment"],
				43 => Application.Current.Resources["Icon_Equipment_SeaplaneFighter"],
				44 => Application.Current.Resources["Icon_Equipment_ArmyInterceptor"],
				45 => Application.Current.Resources["Icon_Equipment_NightFighter"],
				46 => Application.Current.Resources["Icon_Equipment_NightAttacker"],
				47 => Application.Current.Resources["Icon_Equipment_LandASPatrol"],
				_ => Application.Current.Resources["Icon_Equipment_Unknown"],
			};

			img.Source = (ImageSource)icon;
		}

		private void WritePlaneCount(OutlinedTextBlock otb, int count)
		{
			if (count != 0)
			{
				otb.Text = count.ToString();
				otb.Visibility = Visibility.Visible;
			}
		}
		private void WriteImprovementLevel(OutlinedTextBlock otb, int level)
		{
			if (level != 0 && level != 10)
			{
				otb.Text = level.ToString();
				otb.Visibility = Visibility.Visible;
			}
			else if (level == 10)
			{
				otb.Text = "+★";
			}
		}
		private void WritePlaneRank(Image img, int rank)
		{
			if (level != 0)
			{
				var icon = rank switch
				{
					1 => Application.Current.Resources["Icon_Level_AircraftLevelTop1"],
					2 => Application.Current.Resources["Icon_Level_AircraftLevelTop2"],
					3 => Application.Current.Resources["Icon_Level_AircraftLevelTop3"],
					4 => Application.Current.Resources["Icon_Level_AircraftLevelTop4"],
					5 => Application.Current.Resources["Icon_Level_AircraftLevelTop5"],
					6 => Application.Current.Resources["Icon_Level_AircraftLevelTop6"],
					7 => Application.Current.Resources["Icon_Level_AircraftLevelTop7"],
					_ => Application.Current.Resources["Icon_Level_AircraftLevelTop0"],
				};
				img.Source = (ImageSource)icon;
			}
		}

		private void UpdateExperience()
		{
			OTB_ExpNext.Text = expNext.ToString();
		}

		private void UpdateShipName()
		{
			if (shipName != null)
				OTB_ShipName.Text = shipName;
		}

		public bool isFullySupplied()
		{
			bool hasUnsuppliedAircraft = ship.Aircraft
						.Zip(ship.MasterShip.Aircraft, (current, max) => (current, max))
						.Zip(ship.SlotInstance, (planes, equip) => (planes.current, planes.max, equip))
						.Any(slot => slot.current != slot.max &&
									 (slot.current != 1 || slot.equip.MasterEquipment.CategoryType != EquipmentTypes.FlyingBoat));

			return fuelCurrent==fuelMax && ammoCurrent==ammoMax && !hasUnsuppliedAircraft;
		}
	}
}
