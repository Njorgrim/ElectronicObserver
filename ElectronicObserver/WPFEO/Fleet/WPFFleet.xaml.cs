using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ElectronicObserver.Data;
using ElectronicObserver.Observer;
using ElectronicObserver.Utility.Data;
using ElectronicObserver.WinFormsEO;
using ElectronicObserver.WinFormsEO.Dialog;

namespace ElectronicObserver.WPFEO.Fleet
{
	/// <summary>
	/// Interaction logic for WPFFleet.xaml
	/// </summary>
	public partial class WPFFleet : UserControl
	{
		private List<WPFShipData> ShipList { get; set; }
		private IEnumerable<ShipData> Ships => KCDatabase.Instance.Fleet[fleetnum].MembersInstance
			.Where(s => s != null);

		// null before KC loads
		private FleetData? Fleet => KCDatabase.Instance.Fleet[fleetnum];


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
					ShipList.Add(new WPFShipData());
			}
			//Update all ships' datasets
			for (int i = 0; i < ShipList.Count; i++)
			{
				ShipList[i].Update(fleet.MembersInstance[i], fleet);
			}
			//Update fleet dataset

			//Apply To GUI
			SP_ShipList.Children.Clear();
			foreach (WPFShipData ship in ShipList)
			{
				SP_ShipList.Children.Add(ship);
			}


			UpdateFleetName();

			//Update Fleet Condition TODO
			UpdateFleetCondition();

			UpdateAirPower();

			UpdateLOS();

			UpdateFleetAA();
		}

		private void UpdateFleetName()
		{
			FleetName.Text = Fleet.Name;

			string supportType = Fleet.SupportType switch
			{
				1 => "Aerial Support",
				2 => "Support Shelling",
				3 => "Long-range Torpedo Attack",
				_ => "n/a"
			};

			IEnumerable<int> drumCounts = Ships.Select(s => s.AllSlotInstance
				.Count(e => e?.MasterEquipment.CategoryType == EquipmentTypes.TransportContainer));

			IEnumerable<int> daihatsuCounts = Ships.Select(s => s.AllSlotInstanceMaster
				.Count(eq => eq?.CategoryType == EquipmentTypes.LandingCraft || 
				             eq?.CategoryType == EquipmentTypes.SpecialAmphibiousTank));

			int tp = Calculator.GetTPDamage(Fleet);

			FleetNameToolTip.Text = string.Format(
				"Lv sum: {0} / avg: {1:0.00}\r\n" +
				"{2} fleet\r\n" +
				"Support Expedition: {3}\r\n" +
				"Total FP {4} / Torp {5} / AA {6} / ASW {7} / LOS {8}\r\n" +
				"Drum: {9} ({10} ships)\r\n" +
				"Daihatsu: {11} ({12} ships, +{13:p1})\r\n" +
				"TP: S {14} / A {15}\r\n" +
				"Consumption: {16} fuel / {17} ammo\r\n" +
				"({18} fuel / {19} ammo per battle)",
				Ships.Sum(s => s.Level),
				Ships.Average(s => s.Level),
				Constants.GetSpeed(Ships.Min(s => s.Speed)),
				supportType,
				Ships.Sum(s => s.FirepowerTotal),
				Ships.Sum(s => s.TorpedoTotal),
				Ships.Sum(s => s.AATotal),
				Ships.Sum(s => s.ASWTotal),
				Ships.Sum(s => s.LOSTotal),
				drumCounts.Sum(),
				drumCounts.Count(i => i > 0),
				daihatsuCounts.Sum(),
				daihatsuCounts.Count(i => i > 0),
				Calculator.GetExpeditionBonus(Fleet),
				tp,
				Math.Floor(tp*0.7),
				Ships.Sum(s => Math.Max((int)Math.Floor(s.FuelMax * (s.IsMarried ? 0.85 : 1.00)), 1)),
				Ships.Sum(s => Math.Max((int)Math.Floor(s.AmmoMax * (s.IsMarried ? 0.85 : 1.00)), 1)),
				Ships.Sum(s => Math.Max((int)Math.Floor(s.FuelMax * 0.2 * (s.IsMarried ? 0.85 : 1.00)), 1)),
				Ships.Sum(s => Math.Max((int)Math.Floor(s.AmmoMax * 0.2 * (s.IsMarried ? 0.85 : 1.00)), 1))
			);
		}

		private void UpdateFleetCondition()
		{
			bool isResupplied = true;
			bool isSparkled = true;
			bool isHeavilyDamaged = false;
			bool isOnExpedition = KCDatabase.Instance.Fleet[fleetnum].ExpeditionState != 0;
			bool isOnSortie = KCDatabase.Instance.Fleet[fleetnum].IsInSortie;
			foreach (WPFShipData ship in ShipList)
			{
				if (!ship.isFullySupplied())
					isResupplied = false;

				if (!(ship.Morale >= 50))
					isSparkled = false;

				if ((double)ship.HpCurrent / ship.HpMax <= 0.25)
					isHeavilyDamaged = true;
			}

			if (fleetnum == 1 && ((double)ShipList[0].HpCurrent / ShipList[0].HpMax <= 0.5) && KCDatabase.Instance.Fleet.CombinedFlag != 0)
			{
				FleetCondition.Source = (ImageSource)Application.Current.Resources["Icon_Unused_ShipState_damageM"];
				FleetCondition.Text = "Flag Damaged!";
			}
			else if (isHeavilyDamaged && !isOnExpedition)
			{
				FleetCondition.Source = (ImageSource)Application.Current.Resources["Icon_Fleet_Damaged"];
				FleetCondition.Text = "Heavy Damage!";
			}
			else if (!isResupplied && !isHeavilyDamaged && !isOnExpedition && !isOnSortie)
			{
				FleetCondition.Source = (ImageSource)Application.Current.Resources["Icon_Fleet_NotReplenished"];
				FleetCondition.Text = "Need Supplies!";
			}
			else if (isResupplied && !isHeavilyDamaged && isSparkled && !isOnExpedition)
			{
				FleetCondition.Source = (ImageSource)Application.Current.Resources["Icon_Condition_Sparkle"];
				FleetCondition.Text = "Sparkled!";
			}
			else if (isResupplied && !isHeavilyDamaged && !isSparkled && !isOnExpedition)
			{
				FleetCondition.Source = (ImageSource)Application.Current.Resources["Icon_Fleet_Ready"];
				FleetCondition.Text = "Idle";
			}
			else if (isOnExpedition)
			{
				FleetCondition.Source = (ImageSource)Application.Current.Resources["Icon_Fleet_Expedition"];
				FleetCondition.Text = KCDatabase.Instance.Fleet[fleetnum].ExpeditionState switch
				{
					3 => "Retreated!",
					2 => "Returned!",
					1 => "On Expedition",
					_ => FleetCondition.Text
				};
			}
			else if (isOnSortie && !isHeavilyDamaged)
			{
				FleetCondition.Source = (ImageSource)Application.Current.Resources["Icon_Fleet_Sortie"];
				FleetCondition.Text = "Good Luck!";
			}
		}

		private void UpdateAirPower()
		{
			int airSuperiority = Fleet.GetAirSuperiority();
			bool includeLevel = Utility.Configuration.Config.FormFleet.AirSuperiorityMethod == 1;

			FleetAirPower.Text = Fleet.GetAirSuperiorityString();

			FleetAirPower.ToolTipText.Text = string.Format(GeneralRes.ASTooltip,
				(int)(airSuperiority / 3.0),
				(int)(airSuperiority / 1.5),
				Math.Max((int)(airSuperiority * 1.5 - 1), 0),
				Math.Max((int)(airSuperiority * 3.0 - 1), 0),
				includeLevel ? "w/o Proficiency" : "w/ Proficiency",
				includeLevel ? Calculator.GetAirSuperiorityIgnoreLevel(Fleet) : Calculator.GetAirSuperiority(Fleet));
		}

		private void UpdateLOS()
		{
			if (Fleet == null) return;

			FleetLoS.Text = Fleet.GetSearchingAbilityString(BranchWeight);
			StringBuilder sb = new StringBuilder();
			double probStart = Fleet.GetContactProbability();
			var probSelect = Fleet.GetContactSelectionProbability();

			sb.AppendFormat(
				"Formula 33 (n={0})\r\n　(Click to switch between weighting)\r\n\r\nContact:\r\n　AS+ {1:p1} / AS {2:p1}\r\n",
				BranchWeight,
				probStart,
				probStart * 0.6);

			if (probSelect.Count > 0)
			{
				sb.AppendLine("Selection:");

				foreach (var p in probSelect.OrderBy(p => p.Key))
				{
					sb.AppendFormat("・Acc+{0}: {1:p1}\r\n", p.Key, p.Value);
				}
			}

			FleetLoS.ToolTipText.Text = sb.ToString();
		}

		private int BranchWeight { get; set; }

		private void ChangeBranchWeight(object sender, ExecutedRoutedEventArgs e)
		{
			BranchWeight--;
			if (BranchWeight <= 0)
				BranchWeight = 4;

			UpdateLOS();
		}

		private void UpdateFleetAA()
		{
			if (Fleet == null) return;

			var sb = new StringBuilder();
			double lineahead = Calculator.GetAdjustedFleetAAValue(Fleet, 1);

			FleetAA.Text = lineahead.ToString("0.0");

			sb.AppendFormat(GeneralRes.AntiAirPower,
				lineahead,
				Calculator.GetAdjustedFleetAAValue(Fleet, 2),
				Calculator.GetAdjustedFleetAAValue(Fleet, 3));

			FleetAA.ToolTipText.Text = sb.ToString();
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

			FleetData? fleet = db.Fleet.Fleets[fleetnum];
			if (fleet == null) return;

			Update(fleet);
		}

		private void ContextMenuFleet_CopyFleet_Click(object sender, RoutedEventArgs e)
		{
			StringBuilder sb = new StringBuilder();
			KCDatabase db = KCDatabase.Instance;
			if (Fleet == null) return;

			sb.AppendFormat("{0}\tAS: {1} / LOS: {2} / TP: {3}\r\n", Fleet.Name, Fleet.GetAirSuperiority(), Fleet.GetSearchingAbilityString(BranchWeight), Calculator.GetTPDamage(Fleet));
			for (int i = 0; i < Fleet.Members.Count; i++)
			{
				if (Fleet[i] == -1)
					continue;

				ShipData ship = db.Ships[Fleet[i]];

				sb.AppendFormat("{0}/Lv{1}\t", ship.MasterShip.Name, ship.Level);

				var eq = ship.AllSlotInstance;


				if (eq != null)
				{
					for (int j = 0; j < eq.Count; j++)
					{

						if (eq[j] == null) continue;

						int count = 1;
						for (int k = j + 1; k < eq.Count; k++)
						{
							if (eq[k] != null && eq[k].EquipmentID == eq[j].EquipmentID && eq[k].Level == eq[j].Level && eq[k].AircraftLevel == eq[j].AircraftLevel)
							{
								count++;
							}
							else
							{
								break;
							}
						}

						if (count == 1)
						{
							sb.AppendFormat("{0}{1}", j == 0 ? "" : ", ", eq[j].NameWithLevel);
						}
						else
						{
							sb.AppendFormat("{0}{1}x{2}", j == 0 ? "" : ", ", eq[j].NameWithLevel, count);
						}

						j += count - 1;
					}
				}

				sb.AppendLine();
			}

			Clipboard.SetText(sb.ToString());
		}

		private void ContextMenuFleet_CopyFleetDeckBuilder_Click(object sender, RoutedEventArgs e)
		{
			StringBuilder sb = new StringBuilder();
			KCDatabase db = KCDatabase.Instance;

			// 手書き json の悲しみ

			sb.Append(@"{""version"":4,");

			foreach (var fleet in db.Fleet.Fleets.Values)
			{
				if (fleet == null || fleet.MembersInstance.All(m => m == null)) continue;

				sb.AppendFormat(@"""f{0}"":{{", fleet.FleetID);

				int shipcount = 1;
				foreach (var ship in fleet.MembersInstance)
				{
					if (ship == null) break;

					sb.AppendFormat(@"""s{0}"":{{""id"":{1},""lv"":{2},""luck"":{3},""items"":{{",
						shipcount,
						ship.ShipID,
						ship.Level,
						ship.LuckBase);

					int eqcount = 1;
					foreach (var eq in ship.AllSlotInstance.Where(eq => eq != null))
					{
						if (eq == null) break;

						sb.AppendFormat(@"""i{0}"":{{""id"":{1},""rf"":{2},""mas"":{3}}},", eqcount >= 6 ? "x" : eqcount.ToString(), eq.EquipmentID, eq.Level, eq.AircraftLevel);

						eqcount++;
					}

					if (eqcount > 1)
						sb.Remove(sb.Length - 1, 1);        // remove ","
					sb.Append(@"}},");

					shipcount++;
				}

				if (shipcount > 0)
					sb.Remove(sb.Length - 1, 1);        // remove ","
				sb.Append(@"},");

			}

			sb.Remove(sb.Length - 1, 1);        // remove ","
			sb.Append(@"}");

			Clipboard.SetText(sb.ToString());
		}

		private void ContextMenuFleet_CopyKanmusuList_Click(object sender, RoutedEventArgs e)
		{
			StringBuilder sb = new StringBuilder();
			KCDatabase db = KCDatabase.Instance;

			// version
			sb.Append(".2");

			// <たね艦娘(完全未改造時)のID, 艦娘リスト>　に分類
			Dictionary<int, List<ShipData>> shiplist = new Dictionary<int, List<ShipData>>();

			foreach (var ship in db.Ships.Values.Where(s => s.IsLocked))
			{
				var master = ship.MasterShip;
				while (master.RemodelBeforeShip != null)
					master = master.RemodelBeforeShip;

				if (!shiplist.ContainsKey(master.ShipID))
				{
					shiplist.Add(master.ShipID, new List<ShipData>() { ship });
				}
				else
				{
					shiplist[master.ShipID].Add(ship);
				}
			}

			// 上で作った分類の各項を文字列化
			foreach (var sl in shiplist)
			{
				sb.Append("|").Append(sl.Key).Append(":");

				foreach (var ship in sl.Value.OrderByDescending(s => s.Level))
				{
					sb.Append(ship.Level);

					// 改造レベルに達しているのに未改造の艦は ".<たね=1, 改=2, 改二=3, ...>" を付加
					if (ship.MasterShip.RemodelAfterShipID != 0 && ship.ExpNextRemodel == 0)
					{
						sb.Append(".");
						int count = 1;
						var master = ship.MasterShip;
						while (master.RemodelBeforeShip != null)
						{
							master = master.RemodelBeforeShip;
							count++;
						}
						sb.Append(count);
					}
					sb.Append(",");
				}

				// 余った "," を削除
				sb.Remove(sb.Length - 1, 1);
			}

			Clipboard.SetText(sb.ToString());
		}

		private void ContextMenuFleet_CopyFleetAnalysis_Click(object sender, RoutedEventArgs e)
		{
			KCDatabase db = KCDatabase.Instance;
			List<string> ships = new List<string>();

			foreach (ShipData ship in db.Ships.Values.Where(s => s.IsLocked))
			{
				int[] apiKyouka =
				{
					ship.FirepowerModernized,
					ship.TorpedoModernized,
					ship.AAModernized,
					ship.ArmorModernized,
					ship.LuckModernized,
					ship.HPMaxModernized,
					ship.ASWModernized
				};

				int expProgress = 0;
				if (ExpTable.ShipExp.ContainsKey(ship.Level + 1) && ship.Level != 99)
				{
					expProgress = (ExpTable.ShipExp[ship.Level].Next - ship.ExpNext)
					              / ExpTable.ShipExp[ship.Level].Next;
				}

				int[] apiExp = { ship.ExpTotal, ship.ExpNext, expProgress };

				string shipId = $"\"api_ship_id\":{ship.ShipID}";
				string level = $"\"api_lv\":{ship.Level}";
				string kyouka = $"\"api_kyouka\":[{string.Join(",", apiKyouka)}]";
				string exp = $"\"api_exp\":[{string.Join(",", apiExp)}]";
				// ship.SallyArea defaults to -1 if it doesn't exist on api 
				// which breaks the app, changing the default to 0 would be 
				// easier but I'd prefer not to mess with that
				string sallyArea = $"\"api_sally_area\":{(ship.SallyArea >= 0 ? ship.SallyArea : 0)}";

				string[] analysisData = { shipId, level, kyouka, exp, sallyArea };

				ships.Add($"{{{string.Join(",", analysisData)}}}");
			}

			string json = $"[{string.Join(",", ships)}]";

			Clipboard.SetText(json);
		}

		private void GenerateEquipList(bool allEquipment)
		{
			StringBuilder sb = new StringBuilder();
			KCDatabase db = KCDatabase.Instance;

			// 手書き json の悲しみ
			// pain and suffering

			sb.Append("[");

			foreach (EquipmentData equip in db.Equipments.Values.Where(eq => allEquipment || eq.IsLocked))
			{
				sb.Append($"{{\"api_slotitem_id\":{equip.EquipmentID},\"api_level\":{equip.Level}}},");
			}

			sb.Remove(sb.Length - 1, 1);        // remove ","
			sb.Append("]");

			Clipboard.SetText(sb.ToString());
		}

		private void ContextMenuFleet_CopyFleetAnalysisLockedEquip_Click(object sender, RoutedEventArgs e)
		{
			GenerateEquipList(false);
		}

		private void ContextMenuFleet_CopyFleetAnalysisAllEquip_Click(object sender, RoutedEventArgs e)
		{
			GenerateEquipList(true);
		}

		private void ContextMenuFleet_AntiAirDetails_Click(object sender, RoutedEventArgs e)
		{
			if (Fleet == null) return;

			var dialog = new DialogAntiAirDefense();

			dialog.SetFleetID(Fleet.ID);
			dialog.Show();
		}

		private void ContextMenuFleet_OutputFleetImage_Click(object sender, RoutedEventArgs e)
		{
			if (Fleet == null) return;

			using var dialog = new DialogFleetImageGenerator(Fleet.ID);
			dialog.ShowDialog();
		}
	}

	public static class CustomCommands
	{
		public static readonly RoutedUICommand ChangeBranchWeight = new RoutedUICommand
		(
			nameof(ChangeBranchWeight),
			nameof(ChangeBranchWeight),
			typeof(CustomCommands)
		);

		public static readonly RoutedUICommand OpenShipEncyclopedia = new RoutedUICommand
		(
			nameof(OpenShipEncyclopedia),
			nameof(OpenShipEncyclopedia),
			typeof(CustomCommands)
		);

		public static readonly RoutedUICommand OpenExpCalculator = new RoutedUICommand
		(
			nameof(OpenExpCalculator),
			nameof(OpenExpCalculator),
			typeof(CustomCommands)
		);
	}
}
