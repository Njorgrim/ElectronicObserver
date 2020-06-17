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
using ElectronicObserver.Utility.Mathematics;
using ElectronicObserver.WinFormsEO;
using ElectronicObserver.WinFormsEO.Dialog;

namespace ElectronicObserver.WPFEO.Fleet
{
	/// <summary>
	/// Interaction logic for ShipData.xaml
	/// </summary>
	public partial class WPFShipData : UserControl
	{
		private ShipData Ship { get; set; }
		private FleetData Fleet { get; set; }
		private string ShipName => Ship.MasterShip.NameWithClass;
		private int Level => Ship.Level;
		private int ExpNext => Ship.ExpNext;
		public int Morale => Ship.Condition;
		public int HpMax => Ship.HPMax;
		public int HpCurrent => Ship.HPCurrent;
		private int FuelMax => Ship.FuelMax;
		private int AmmoMax => Ship.AmmoMax;
		private int FuelCurrent => Ship.Fuel;
		private int AmmoCurrent => Ship.Ammo;
		private int EquipSlotAmount => Ship.SlotSize;
		private bool HasOpenRE => Ship.IsExpansionSlotAvailable;
		private bool IsEscaped => Fleet.EscapedShipList.Contains(Ship.ID);
		private int RepairID => Ship.RepairingDockID;

		private WPFEquipmentData[] EquipmentSlots { get; }

		public WPFShipData()
		{
			InitializeComponent();

			EquipmentSlots = new[] {Equip1, Equip2, Equip3, Equip4, Equip5};
		}

		public void Update(ShipData ship, FleetData fleet)
		{
			Ship = ship;
			Fleet = fleet;
			if (ship == null) return;

			//GUI update
			UpdateMeta();
			UpdateShipName();

			//HP handling.
			//Scales the HP bar depending on HP percentage (hpp) and colors it accordingly.
			UpdateHP();

			//Fuel/Ammo handling. Scales Fuel/Ammo bar according to percentage (fuelp and ammop)
			
			UpdateSupply();

			//Morale handling. Colors the bar background according to remaining morale.
			UpdateMorale();

			//Equip handling. Displays correct amount of equip circles and draws icons, plane rank, plane count and upgrades accordingly.
			UpdateEquipment();

			//Exp Bar handling. Displays experience to next level. TODO: Calculate percentage and apply to exp bar fill, to display progress.
			UpdateExperience();
		}

		private void UpdateMeta()
		{
			OTB_Level.Text = "Lv. " + Level;
			OTB_ExpNext.Text = ExpNext.ToString();
		}

		private void UpdateHP()
		{
			int hpp = (int)((double)HpCurrent / HpMax * 100);

			if (RepairID != -1)
			{
				OTB_HP.Text = "Repairing => " + HpCurrent + "/" + HpMax;
				Grid_HP.Background = Brushes.DarkBlue;
			}
			else if (IsEscaped)
			{
				OTB_HP.Text = "Retreated => " + HpCurrent + "/" + HpMax;
				Grid_HP.Background = Brushes.Gray;
			}
			else
			{
				OTB_HP.Text = hpp switch
				{
					int i when i > 0 => HpCurrent + "/" + HpMax,
					_ => "Sunk"
				};

				Grid_HP.Foreground = PercentageBarColor(hpp);
				Grid_HP.Value = hpp;
			}

			StringBuilder sb = new StringBuilder();
			double hprate = (double)Ship.HPCurrent / Ship.HPMax;

			sb.AppendFormat("HP: {0:0.0}% [{1}]\n", hprate * 100, Constants.GetDamageState(hprate));
			if (IsEscaped)
			{
				sb.AppendLine(GeneralRes.Retreating);
			}
			else if (hprate > 0.50)
			{
				sb.AppendFormat(GeneralRes.ToMidAndHeavy + "\n", Ship.HPCurrent - Ship.HPMax / 2, Ship.HPCurrent - Ship.HPMax / 4);
			}
			else if (hprate > 0.25)
			{
				sb.AppendFormat(GeneralRes.ToHeavy + "\n", Ship.HPCurrent - Ship.HPMax / 4);
			}
			else
			{
				sb.AppendLine(GeneralRes.IsTaiha);
			}

			if (Ship.RepairTime > 0)
			{
				var span = DateTimeHelper.FromAPITimeSpan(Ship.RepairTime);
				sb.AppendFormat(GeneralRes.DockTime + ": {0} @ {1}",
					DateTimeHelper.ToTimeRemainString(span),
					DateTimeHelper.ToTimeRemainString(Calculator.CalculateDockingUnitTime(Ship)));
			}

			HPToolTip.Text = sb.ToString();
		}

		private void UpdateSupply()
		{
			int fuelp = (int) Math.Ceiling(100.0 * FuelCurrent / FuelMax);
			int ammop = (int)Math.Ceiling(100.0 * AmmoCurrent / AmmoMax);

			ProgressBarFuel.Foreground = PercentageBarColor(fuelp);
			ProgressBarFuel.Value = fuelp;

			ProgressBarAmmo.Foreground = PercentageBarColor(ammop);
			ProgressBarAmmo.Value = ammop;

			FuelAmmoToolTip.Text = string.Format("Fuel: {0}/{1} ({2}%)\r\nAmmo: {3}/{4} ({5}%)",
				FuelCurrent, FuelMax, fuelp,
				AmmoCurrent, AmmoMax, ammop);
		}

		private Brush PercentageBarColor(int percentage) => percentage switch
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
			OTB_Morale.Text = Morale.ToString();
			MoraleIcon.Source = (ImageSource?) (Morale switch
			{
				int n when n > 49 => FindResource("Icon_Condition_Sparkle"),
				int n when n > 39 => null,
				int n when n > 29 => FindResource("Icon_Condition_LittleTired"),
				int n when n > 19 => FindResource("Icon_Condition_Tired"),
				_ => FindResource("Icon_Condition_VeryTired")
			});

			if (Ship.Condition < 49)
			{
				TimeSpan ts = new TimeSpan(0, (int)Math.Ceiling((49 - Ship.Condition) / 3.0) * 3, 0);
				MoraleToolTip.Text = string.Format(GeneralRes.FatigueRestoreTime, (int)ts.TotalMinutes, (int)ts.Seconds);
			}
			else
			{
				MoraleToolTip.Text = string.Format(GeneralRes.RemainingExpeds, (int)Math.Ceiling((Ship.Condition - 49) / 3.0));
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

			foreach (WPFEquipmentData equip in EquipmentSlots)
			{
				equip.Visibility = Visibility.Collapsed;
				equip.PlaneRank.Visibility = Visibility.Collapsed;
				equip.Level.Visibility = Visibility.Collapsed;
				equip.PlaneRank.Visibility = Visibility.Collapsed;
			}

			EquipRE.Visibility = Visibility.Hidden;
			EquipRE.PlaneRank.Visibility = Visibility.Collapsed;
			EquipRE.Level.Visibility = Visibility.Collapsed;
			EquipRE.PlaneCount.Visibility = Visibility.Collapsed;

			// Show available equip slots (including RE, if applicable)
			for (int i = 0; i < EquipSlotAmount; i++)
			{
				EquipmentSlots[i].Visibility = Visibility.Visible;
			}

			int equippedSlotAmount = Ship.SlotInstance.Count(eq => eq != null);

			// Get Planecounts
			for (int i = 0; i < 5; i++)
			{
				WritePlaneCount(EquipmentSlots[i].PlaneCount, Ship.Aircraft[i]);
			}

			for (int i = 0; i < equippedSlotAmount; i++)
			{
				WriteImprovementLevel(EquipmentSlots[i].Level, Ship.SlotInstance[i].Level);
				EquipmentSlots[i].PlaneRank.Visibility = Visibility.Visible;
				WritePlaneRank(EquipmentSlots[i].PlaneRank, Ship.SlotInstance[i].AircraftLevel);
				EquipmentSlots[i].Image.Source = GetEquipIcon(Ship.SlotInstance[i].MasterEquipment.IconType);
			}

			if (HasOpenRE)
			{
				EquipRE.Visibility = Visibility.Visible;
			}

			if (HasOpenRE && Ship.ExpansionSlotInstance != null)
			{
				WriteImprovementLevel(EquipRE.Level, Ship.ExpansionSlotInstance.Level);
				EquipRE.Image.Source = GetEquipIcon(Ship.ExpansionSlotInstance.MasterEquipment.IconType);
			}

			EquipmentToolTip.Text = GetEquipmentString(Ship);
		}

		private string GetEquipmentString(ShipData ship)
		{
			StringBuilder sb = new StringBuilder();

			for (int i = 0; i < ship.Slot.Count; i++)
			{
				var eq = ship.SlotInstance[i];
				if (eq != null)
					sb.AppendFormat("[{0}/{1}] {2}\r\n", ship.Aircraft[i], ship.MasterShip.Aircraft[i], eq.NameWithLevel);
			}

			{
				var exslot = ship.ExpansionSlotInstance;
				if (exslot != null)
					sb.AppendFormat(GeneralRes.Expansion + ": {0}\r\n", exslot.NameWithLevel);
			}

			int[] slotmaster = ship.AllSlotMaster.ToArray();

			sb.AppendFormat("\r\n" + GeneralRes.DayBattle + ": {0}", Constants.GetDayAttackKind(Calculator.GetDayAttackKind(slotmaster, ship.ShipID, -1)));
			{
				int shelling = ship.ShellingPower;
				int aircraft = ship.AircraftPower;
				if (shelling > 0)
				{
					if (aircraft > 0)
						sb.AppendFormat(" - " + GeneralRes.Shelling + ": {0} / " + GeneralRes.Bombing + ": {1}", shelling, aircraft);
					else
						sb.AppendFormat(" - " + GeneralRes.Power + ": {0}", shelling);
				}
				else if (aircraft > 0)
					sb.AppendFormat(" - " + GeneralRes.Power + ": {0}", aircraft);
			}
			sb.AppendLine();

			if (ship.CanAttackAtNight)
			{
				sb.AppendFormat(GeneralRes.NightBattle + ": {0}", Constants.GetNightAttackKind(Calculator.GetNightAttackKind(slotmaster, ship.ShipID, -1)));
				{
					int night = ship.NightBattlePower;
					if (night > 0)
					{
						sb.AppendFormat(" - " + GeneralRes.Power + ": {0}", night);
					}
				}
				sb.AppendLine();
			}

			{
				int torpedo = ship.TorpedoPower;
				int asw = ship.AntiSubmarinePower;

				if (torpedo > 0)
				{
					sb.AppendFormat(ConstantsRes.TorpedoAttack + ": {0}", torpedo);
				}
				if (asw > 0)
				{
					if (torpedo > 0)
						sb.Append(" / ");

					sb.AppendFormat("ASW: {0}", asw);

					if (ship.CanOpeningASW)
						sb.Append(" (OASW)");
				}
				if (torpedo > 0 || asw > 0)
					sb.AppendLine();
			}

			{
				int aacutin = Calculator.GetAACutinKind(ship.ShipID, slotmaster);
				if (aacutin != 0)
				{
					sb.AppendFormat(GeneralRes.AntiAir + ": {0}\r\n", Constants.GetAACutinKind(aacutin));
				}
				double adjustedaa = Calculator.GetAdjustedAAValue(ship);
				sb.AppendFormat(GeneralRes.ShipAADefense + "\r\n",
					adjustedaa,
					Calculator.GetProportionalAirDefense(adjustedaa)
					);

				double aarbRate = Calculator.GetAarbRate(ship, adjustedaa);
				if (aarbRate > 0)
				{
					sb.Append($"AARB: {aarbRate.ToString("#0.##%")}\r\n");
					sb.Append("Ise class and multiple rocket bonus is probably wrong!\r\n");
				}

			}

			{
				int airsup_min;
				int airsup_max;
				if (Utility.Configuration.Config.FormFleet.AirSuperiorityMethod == 1)
				{
					airsup_min = Calculator.GetAirSuperiority(ship, false);
					airsup_max = Calculator.GetAirSuperiority(ship, true);
				}
				else
				{
					airsup_min = airsup_max = Calculator.GetAirSuperiorityIgnoreLevel(ship);
				}

				int airbattle = ship.AirBattlePower;
				if (airsup_min > 0)
				{

					string airsup_str;
					if (Utility.Configuration.Config.FormFleet.ShowAirSuperiorityRange && airsup_min < airsup_max)
					{
						airsup_str = string.Format("{0} ～ {1}", airsup_min, airsup_max);
					}
					else
					{
						airsup_str = airsup_min.ToString();
					}

					if (airbattle > 0)
						sb.AppendFormat(GeneralRes.AirPower + ": {0} / Airstrike Power: {1}\r\n", airsup_str, airbattle);
					else
						sb.AppendFormat(GeneralRes.AirPower + ": {0}\r\n", airsup_str);
				}
				else if (airbattle > 0)
					sb.AppendFormat("Airstrike Power: {0}\r\n", airbattle);
			}

			return sb.ToString();
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
			if (Level != 0)
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
			OTB_ExpNext.Text = ExpNext.ToString();

			ProgressBarExp.Minimum = ExpTable.ShipExp[Ship.Level].Total;
			ProgressBarExp.Maximum = ExpTable.ShipExp[Ship.Level].Total + ExpTable.ShipExp[Ship.Level].Next;
			ProgressBarExp.Value = ProgressBarExp.Maximum - Ship.ExpNext;

			StringBuilder tip = new StringBuilder();
			tip.AppendFormat("Total: {0} exp.\r\n", Ship.ExpTotal);

			if (!Utility.Configuration.Config.FormFleet.ShowNextExp)
				tip.AppendFormat(GeneralRes.ToNextLevel + " exp.\r\n", Ship.ExpNext);

			if (Ship.MasterShip.RemodelAfterShipID != 0 && Ship.Level < Ship.MasterShip.RemodelAfterLevel)
			{
				tip.AppendFormat(GeneralRes.ToRemodel + "\r\n", Ship.MasterShip.RemodelAfterLevel - Ship.Level, Ship.ExpNextRemodel);
			}
			else if (Ship.Level <= 99)
			{
				tip.AppendFormat(GeneralRes.To99 + " exp.\r\n", Math.Max(ExpTable.GetExpToLevelShip(Ship.ExpTotal, 99), 0));
			}
			else
			{
				tip.AppendFormat(GeneralRes.ToX + " exp.\r\n", ExpTable.ShipMaximumLevel, Math.Max(ExpTable.GetExpToLevelShip(Ship.ExpTotal, ExpTable.ShipMaximumLevel), 0));
			}

			tip.AppendLine("(right click to calculate exp)");

			ExpToolTip.Text = tip.ToString();
		}

		private void UpdateShipName()
		{
			if (Ship == null) return;

			OTB_ShipName.Text = ShipName;

			ShipNameToolTip.Text = string.Format(
				"{0} {1}\r\nFP: {2}/{3}\r\nTorp: {4}/{5}\r\nAA: {6}/{7}\r\nArmor: {8}/{9}\r\nASW: {10}/{11}\r\nEvasion: {12}/{13}\r\nLOS: {14}/{15}\r\nLuck: {16}\r\nAccuracy: {17:+#;-#;+0}\r\nBombing: {18:+#;-#;+0}\r\nRange: {19} / Speed: {20}\r\n(right click to open encyclopedia)\n",
				Ship.MasterShip.ShipTypeName, Ship.NameWithLevel,
				Ship.FirepowerBase, Ship.FirepowerTotal,
				Ship.TorpedoBase, Ship.TorpedoTotal,
				Ship.AABase, Ship.AATotal,
				Ship.ArmorBase, Ship.ArmorTotal,
				Ship.ASWBase, Ship.ASWTotal,
				Ship.EvasionBase, Ship.EvasionTotal,
				Ship.LOSBase, Ship.LOSTotal,
				Ship.LuckTotal,
				Ship.AllSlotInstance.Where(eq => eq != null).Sum(eq => eq.MasterEquipment.Accuracy),
				Ship.AllSlotInstance.Where(eq => eq != null).Sum(eq => eq.MasterEquipment.Bomber),
				Constants.GetRange(Ship.Range),
				Constants.GetSpeed(Ship.Speed)
			);
		}

		public bool isFullySupplied()
		{
			bool hasUnsuppliedAircraft = Ship.Aircraft
						.Zip(Ship.MasterShip.Aircraft, (current, max) => (current, max))
						.Zip(Ship.SlotInstance, (planes, equip) => (planes.current, planes.max, equip))
						.Any(slot => slot.current != slot.max &&
									 (slot.current != 1 || slot.equip.MasterEquipment.CategoryType != EquipmentTypes.FlyingBoat));

			return FuelCurrent==FuelMax && AmmoCurrent==AmmoMax && !hasUnsuppliedAircraft;
		}

		private void ShipName_RightClick(object sender, ExecutedRoutedEventArgs e)
		{
			if (Ship is null) return;

			new DialogAlbumMasterShip(Ship.ShipID).Show();
		}

		private void ShipLevel_RightClick(object sender, ExecutedRoutedEventArgs e)
		{
			if (Ship is null) return;

			new DialogExpChecker(Ship.ID).Show();
		}

		private void HelpOnHover(object sender, MouseEventArgs e)
		{
			Mouse.OverrideCursor = Cursors.Help;
		}

		private void ResetCursor(object sender, MouseEventArgs e)
		{
			Mouse.OverrideCursor = Cursors.Arrow;
		}
	}
}
