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
using ElectronicObserver.Utility.Data;

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

		private WPFEquipmentData[] EquipmentSlots { get; }

		public WPFShipData(WPFFleet parent)
		{
			InitializeComponent();

			this.parent = parent;

			EquipmentSlots = new[] {Grid_Equip1, Grid_Equip2, Grid_Equip3, Grid_Equip4, Grid_Equip5};
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
			OTB_Level.Text = "Lv. " + level;
			OTB_ExpNext.Text = expNext.ToString();
		}

		private void UpdateHP(int hpp)
		{
			if (repairID != -1)
			{
				OTB_HP.Text = "Repairing => " + hpCurrent + "/" + hpMax;
				Grid_HP.Background = Brushes.DarkBlue;
			}
			else if (isEscaped)
			{
				OTB_HP.Text = "Retreated => " + hpCurrent + "/" + hpMax;
				Grid_HP.Background = Brushes.Gray;
			}
			else
			{
				OTB_HP.Text = hpp switch
				{
					int i when i > 0 => hpCurrent + "/" + hpMax,
					_ => "Sunk"
				};

				Grid_HP.Foreground = PercentegeBarColor(hpp);
				Grid_HP.Value = hpp;
			}
		}

		private void UpdateSupply(int fuelp, int ammop)
		{
			ProgressBarFuel.Foreground = PercentegeBarColor(fuelp);
			ProgressBarFuel.Value = fuelp;

			ProgressBarAmmo.Foreground = PercentegeBarColor(ammop);
			ProgressBarAmmo.Value = ammop;
		}

		private Brush PercentegeBarColor(int percentage) => percentage switch
		{
			int n when n == 100 => (Brush) FindResource("EOBlue"),
			int n when n > 75 => Brushes.ForestGreen,
			int n when n > 50 => Brushes.Gold,
			int n when n > 25 => Brushes.Orange,
			int n when n > 0 => Brushes.Red,
			_ => Brushes.Gray,
		};

		private void UpdateMorale()
		{
			OTB_Morale.Text = morale.ToString();
			MoraleIcon.Source = (ImageSource?) (morale switch
			{
				int n when (n > 49) => FindResource("Icon_Condition_Sparkle"),
				int n when (n > 39) => null,
				int n when (n > 29) => FindResource("Icon_Condition_LittleTired"),
				int n when (n > 19) => FindResource("Icon_Condition_Tired"),
				_ => FindResource("Icon_Condition_VeryTired")
			});
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

			foreach (WPFEquipmentData equip in EquipmentSlots)
			{
				equip.Visibility = Visibility.Collapsed;
				equip.PlaneRank.Visibility = Visibility.Collapsed;
				equip.Level.Visibility = Visibility.Collapsed;
				equip.PlaneRank.Visibility = Visibility.Collapsed;
			}

			Grid_EquipRE.Visibility = Visibility.Hidden;
			Grid_EquipRE.PlaneRank.Visibility = Visibility.Collapsed;
			Grid_EquipRE.Level.Visibility = Visibility.Collapsed;
			Grid_EquipRE.PlaneCount.Visibility = Visibility.Collapsed;

			// Show available equip slots (including RE, if applicable)
			for (int i = 0; i < equipSlotAmount; i++)
			{
				EquipmentSlots[i].Visibility = Visibility.Visible;
			}

			int equippedSlotAmount = ship.SlotInstance.Count(eq => eq != null);

			// Get Planecounts
			for (int i = 0; i < 5; i++)
			{
				WritePlaneCount(EquipmentSlots[i].PlaneCount, ship.Aircraft[i]);
			}

			for (int i = 0; i < equippedSlotAmount; i++)
			{
				WriteImprovementLevel(EquipmentSlots[i].Level, ship.SlotInstance[i].Level);
				EquipmentSlots[i].PlaneRank.Visibility = Visibility.Visible;
				WritePlaneRank(EquipmentSlots[i].PlaneRank, ship.SlotInstance[i].AircraftLevel);
				EquipmentSlots[i].Image.Source = GetEquipIcon(ship.SlotInstance[i].MasterEquipment.IconType);
			}

			if (hasOpenRE)
			{
				Grid_EquipRE.Visibility = Visibility.Visible;
			}

			if (hasOpenRE && ship.ExpansionSlotInstance != null)
			{
				WriteImprovementLevel(Grid_EquipRE.Level, ship.ExpansionSlotInstance.Level);
				Grid_EquipRE.Image.Source = GetEquipIcon(ship.ExpansionSlotInstance.MasterEquipment.IconType);
			}

		}

		private ImageSource GetEquipIcon(int type) => (ImageSource) (type switch
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
		});

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
				otb.Text = otb.Text = "+" + level;
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

			ProgressBarExp.Minimum = ExpTable.ShipExp[ship.Level].Total;
			ProgressBarExp.Maximum = ExpTable.ShipExp[ship.Level].Total + ExpTable.ShipExp[ship.Level].Next;
			ProgressBarExp.Value = ProgressBarExp.Maximum - ship.ExpNext;
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
