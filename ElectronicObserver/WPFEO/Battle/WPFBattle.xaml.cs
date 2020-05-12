using ElectronicObserver.Data;
using ElectronicObserver.Data.Battle;
using ElectronicObserver.Data.Battle.Phase;
using ElectronicObserver.Observer;
using ElectronicObserver.WinFormsEO;
using ElectronicObserver.WPFEO.Battle;
using ElectronicObserver.WPFEO.MiscControls;
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

namespace ElectronicObserver.WPFEO
{
	/// <summary>
	/// Interaction logic for WPFBattle.xaml
	/// </summary>
	public partial class WPFBattle : UserControl
	{
		List<WPFBattleShip> PlayerMain = new List<WPFBattleShip>();
		List<WPFBattleShip> PlayerEscort = new List<WPFBattleShip>();
		List<WPFBattleShip> EnemyMain = new List<WPFBattleShip>();
		List<WPFBattleShip> EnemyEscort = new List<WPFBattleShip>();

		public WPFBattle(WPFMain parent)
		{
			InitializeComponent();

			for (int i = 0; i < 6; i++)
			{
				WPFBattleShip pmShip = new WPFBattleShip(this);
				WPFBattleShip peShip = new WPFBattleShip(this);
				WPFBattleShip emShip = new WPFBattleShip(this);
				WPFBattleShip eeShip = new WPFBattleShip(this);

				PlayerMain.Add(pmShip);
				PlayerEscort.Add(peShip);
				EnemyMain.Add(emShip);
				EnemyEscort.Add(eeShip);

				SP_PlayerFleetContainer.Children.Add(pmShip);
				SP_PlayerEscortContainer.Children.Add(peShip);
				SP_EnemyFleetContainer.Children.Add(emShip);
				SP_EnemyEscortContainer.Children.Add(eeShip);
			}
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			APIObserver o = APIObserver.Instance;

			o["api_port/port"].ResponseReceived += Updated;
			o["api_req_map/start"].ResponseReceived += Updated;
			o["api_req_map/next"].ResponseReceived += Updated;
			o["api_req_sortie/battle"].ResponseReceived += Updated;
			o["api_req_sortie/battleresult"].ResponseReceived += Updated;
			o["api_req_battle_midnight/battle"].ResponseReceived += Updated;
			o["api_req_battle_midnight/sp_midnight"].ResponseReceived += Updated;
			o["api_req_sortie/airbattle"].ResponseReceived += Updated;
			o["api_req_sortie/ld_airbattle"].ResponseReceived += Updated;
			o["api_req_sortie/night_to_day"].ResponseReceived += Updated;
			o["api_req_sortie/ld_shooting"].ResponseReceived += Updated;
			o["api_req_combined_battle/battle"].ResponseReceived += Updated;
			o["api_req_combined_battle/midnight_battle"].ResponseReceived += Updated;
			o["api_req_combined_battle/sp_midnight"].ResponseReceived += Updated;
			o["api_req_combined_battle/airbattle"].ResponseReceived += Updated;
			o["api_req_combined_battle/battle_water"].ResponseReceived += Updated;
			o["api_req_combined_battle/ld_airbattle"].ResponseReceived += Updated;
			o["api_req_combined_battle/ec_battle"].ResponseReceived += Updated;
			o["api_req_combined_battle/ec_midnight_battle"].ResponseReceived += Updated;
			o["api_req_combined_battle/ec_night_to_day"].ResponseReceived += Updated;
			o["api_req_combined_battle/each_battle"].ResponseReceived += Updated;
			o["api_req_combined_battle/each_battle_water"].ResponseReceived += Updated;
			o["api_req_combined_battle/ld_shooting"].ResponseReceived += Updated;
			o["api_req_combined_battle/battleresult"].ResponseReceived += Updated;
			o["api_req_practice/battle"].ResponseReceived += Updated;
			o["api_req_practice/midnight_battle"].ResponseReceived += Updated;
			o["api_req_practice/battle_result"].ResponseReceived += Updated;

			Utility.Configuration.Instance.ConfigurationChanged += ConfigurationChanged;

		}

		private void ConfigurationChanged()
		{
			throw new NotImplementedException();
		}

		private void Updated(string apiname, dynamic data)
		{
			KCDatabase db = KCDatabase.Instance;
			BattleManager bm = db.Battle;
			bool hideDuringBattle = Utility.Configuration.Config.FormBattle.HideDuringBattle;

			switch (apiname)
			{

				case "api_port/port":
					Grid_BaseLayout.Visibility = Visibility.Collapsed;
					//TODO: ToolTipInfo.RemoveAll();
					break;

				case "api_req_map/start":
				case "api_req_map/next":
					if (!bm.Compass.HasAirRaid)
						goto case "api_port/port";

					SetFormation(bm);
					ClearSearchingResult();
					ClearBaseAirAttack();
					SetAerialWarfare(null, ((BattleBaseAirRaid)bm.BattleDay).BaseAirRaid);
					SetHPBar(bm.BattleDay);
					SetDamageRate(bm);

					Grid_BaseLayout.Visibility = !hideDuringBattle ? Visibility.Collapsed : Visibility.Visible;
					break;


				case "api_req_sortie/battle":
				case "api_req_practice/battle":
				case "api_req_sortie/ld_airbattle":
				case "api_req_sortie/ld_shooting":
					{

						SetFormation(bm);
						SetSearchingResult(bm.BattleDay);
						SetBaseAirAttack(bm.BattleDay.BaseAirAttack);
						SetAerialWarfare(bm.BattleDay.JetAirBattle, bm.BattleDay.AirBattle);
						SetHPBar(bm.BattleDay);
						SetDamageRate(bm);

						Grid_BaseLayout.Visibility = !hideDuringBattle ? Visibility.Collapsed : Visibility.Visible;
					}
					break;

				case "api_req_battle_midnight/battle":
				case "api_req_practice/midnight_battle":
					{

						SetNightBattleEvent(bm.BattleNight.NightInitial);
						SetHPBar(bm.BattleNight);
						SetDamageRate(bm);

						Grid_BaseLayout.Visibility = !hideDuringBattle ? Visibility.Collapsed : Visibility.Visible;
					}
					break;

				case "api_req_battle_midnight/sp_midnight":
					{

						SetFormation(bm);
						ClearBaseAirAttack();
						ClearAerialWarfare();
						ClearSearchingResult();
						SetNightBattleEvent(bm.BattleNight.NightInitial);
						SetHPBar(bm.BattleNight);
						SetDamageRate(bm);

						Grid_BaseLayout.Visibility = !hideDuringBattle ? Visibility.Collapsed : Visibility.Visible;
					}
					break;

				case "api_req_sortie/airbattle":
					{

						SetFormation(bm);
						SetSearchingResult(bm.BattleDay);
						SetBaseAirAttack(bm.BattleDay.BaseAirAttack);
						SetAerialWarfare(bm.BattleDay.JetAirBattle, bm.BattleDay.AirBattle, ((BattleAirBattle)bm.BattleDay).AirBattle2);
						SetHPBar(bm.BattleDay);
						SetDamageRate(bm);

						Grid_BaseLayout.Visibility = !hideDuringBattle ? Visibility.Collapsed : Visibility.Visible;
					}
					break;

				case "api_req_sortie/night_to_day":
					{
						// 暫定
						var battle = bm.BattleNight as BattleDayFromNight;

						SetFormation(bm);
						ClearAerialWarfare();
						ClearSearchingResult();
						ClearBaseAirAttack();
						SetNightBattleEvent(battle.NightInitial);

						if (battle.NextToDay)
						{
							SetSearchingResult(battle);
							SetBaseAirAttack(battle.BaseAirAttack);
							SetAerialWarfare(battle.JetAirBattle, battle.AirBattle);
						}

						SetHPBar(bm.BattleDay);
						SetDamageRate(bm);

						Grid_BaseLayout.Visibility = !hideDuringBattle ? Visibility.Collapsed : Visibility.Visible;
					}
					break;

				case "api_req_combined_battle/battle":
				case "api_req_combined_battle/battle_water":
				case "api_req_combined_battle/ld_airbattle":
				case "api_req_combined_battle/ec_battle":
				case "api_req_combined_battle/each_battle":
				case "api_req_combined_battle/each_battle_water":
				case "api_req_combined_battle/ld_shooting":
					{

						SetFormation(bm);
						SetSearchingResult(bm.BattleDay);
						SetBaseAirAttack(bm.BattleDay.BaseAirAttack);
						SetAerialWarfare(bm.BattleDay.JetAirBattle, bm.BattleDay.AirBattle);
						SetHPBar(bm.BattleDay);
						SetDamageRate(bm);

						Grid_BaseLayout.Visibility = !hideDuringBattle ? Visibility.Collapsed : Visibility.Visible;
					}
					break;

				case "api_req_combined_battle/airbattle":
					{

						SetFormation(bm);
						SetSearchingResult(bm.BattleDay);
						SetBaseAirAttack(bm.BattleDay.BaseAirAttack);
						SetAerialWarfare(bm.BattleDay.JetAirBattle, bm.BattleDay.AirBattle, ((BattleCombinedAirBattle)bm.BattleDay).AirBattle2);
						SetHPBar(bm.BattleDay);
						SetDamageRate(bm);

						Grid_BaseLayout.Visibility = !hideDuringBattle ? Visibility.Collapsed : Visibility.Visible;
					}
					break;

				case "api_req_combined_battle/midnight_battle":
				case "api_req_combined_battle/ec_midnight_battle":
					{

						SetNightBattleEvent(bm.BattleNight.NightInitial);
						SetHPBar(bm.BattleNight);
						SetDamageRate(bm);

						Grid_BaseLayout.Visibility = !hideDuringBattle ? Visibility.Collapsed : Visibility.Visible;
					}
					break;

				case "api_req_combined_battle/sp_midnight":
					{

						SetFormation(bm);
						ClearAerialWarfare();
						ClearSearchingResult();
						ClearBaseAirAttack();
						SetNightBattleEvent(bm.BattleNight.NightInitial);
						SetHPBar(bm.BattleNight);
						SetDamageRate(bm);

						Grid_BaseLayout.Visibility = !hideDuringBattle ? Visibility.Collapsed : Visibility.Visible;
					}
					break;

				case "api_req_combined_battle/ec_night_to_day":
					{
						var battle = bm.BattleNight as BattleDayFromNight;

						SetFormation(bm);
						ClearAerialWarfare();
						ClearSearchingResult();
						ClearBaseAirAttack();
						SetNightBattleEvent(battle.NightInitial);

						if (battle.NextToDay)
						{
							SetSearchingResult(battle);
							SetBaseAirAttack(battle.BaseAirAttack);
							SetAerialWarfare(battle.JetAirBattle, battle.AirBattle);
						}

						SetHPBar(battle);
						SetDamageRate(bm);

						Grid_BaseLayout.Visibility = !hideDuringBattle ? Visibility.Collapsed : Visibility.Visible;
					}
					break;

				case "api_req_sortie/battleresult":
				case "api_req_combined_battle/battleresult":
				case "api_req_practice/battle_result":
					{

						SetMVPShip(bm);

						Grid_BaseLayout.Visibility = Visibility.Visible;
					}
					break;

			}
		}

		private void SetMVPShip(BattleManager bm)
		{
			bool isCombined = bm.IsCombinedBattle;

			var bd = bm.StartsFromDayBattle ? (BattleData)bm.BattleDay : (BattleData)bm.BattleNight;
			var br = bm.Result;

			var friend = bd.Initial.FriendFleet;
			var escort = !isCombined ? null : bd.Initial.FriendFleetEscort;


			for (int i = 0; i < friend.Members.Count; i++)
			{
				if (friend.EscapedShipList.Contains(friend.Members[i]))
					PlayerMain[i].Background = Brushes.AliceBlue;

				else if (br.MVPIndex == i + 1)
					PlayerMain[i].Background = Brushes.Wheat;

				else
					PlayerMain[i].Background = null;
			}

			if (escort != null)
			{
				for (int i = 0; i < escort.Members.Count; i++)
				{
					if (friend.EscapedShipList.Contains(friend.Members[i]))
						PlayerEscort[i].Background = Brushes.AliceBlue;

					else if (br.MVPIndex == i + 1)
						PlayerEscort[i].Background = Brushes.Wheat;

					else
						PlayerMain[i].Background = null;
				}
			}
		}

		private void SetNightBattleEvent(PhaseNightInitial pd)
		{
			FleetData fleet = pd.FriendFleet;

			//Friendly Searchlight
			{
				int index = pd.SearchlightIndexFriend;

				if (index != -1)
				{
					ShipData ship = fleet.MembersInstance[index];

					OTB_PlayerTotalPlaneLoss.Text = "#" + (index + (pd.IsFriendEscort ? 6 : 0) + 1);
					OTB_PlayerTotalPlaneLoss.Fill = Brushes.White;
					Img_PlayerTotalPlaneLoss.Source = (ImageSource) Application.Current.Resources["Icon_Equipment_Searchlight"];
					OTB_PlayerTotalPlaneLoss.ToolTip = GeneralRes.SearchlightUsed + ": " + ship.NameWithLevel;
				}
				else
				{
					OTB_PlayerScoutSuccess.ToolTip = null;
				}
			}

			//Enemy Searchlight
			{
				int index = pd.SearchlightIndexEnemy;
				if (index != -1)
				{
					OTB_EnemyTotalPlaneLoss.Text = "#" + (index + (pd.IsEnemyEscort ? 6 : 0) + 1);
					OTB_EnemyTotalPlaneLoss.Fill = Brushes.White;
					Img_EnemyTotalPlaneLoss.Source = (ImageSource) Application.Current.Resources["Icon_Equipment_Searchlight"];
					OTB_EnemyTotalPlaneLoss.ToolTip = GeneralRes.SearchlightUsed + ": " + pd.SearchlightEnemyInstance.NameWithClass;
				}
				else
				{
					OTB_EnemyTotalPlaneLoss.ToolTip = null;
				}
			}


			//Friendly Night Scout
			if (pd.TouchAircraftFriend != -1)
			{
				OTB_PlayerScoutSuccess.Text = GeneralRes.NightContact;
				Img_PlayerScouting.Source = (ImageSource) Application.Current.Resources["Icon_Equipment_Seaplane"];
				OTB_PlayerScoutSuccess.ToolTip = GeneralRes.NightContacting + ": " + KCDatabase.Instance.MasterEquipments[pd.TouchAircraftFriend].Name;
			}
			else
			{
				OTB_PlayerScoutSuccess.ToolTip = null;
			}

			//Enemy Night Scout
			if (pd.TouchAircraftEnemy != -1)
			{
				OTB_EnemyScoutSuccess.Text = GeneralRes.NightContact;
				Img_EnemyScouting.Source = (ImageSource) Application.Current.Resources["Icon_Equipment_Seaplane"];
				OTB_EnemyScoutSuccess.ToolTip = GeneralRes.NightContacting + ": " + KCDatabase.Instance.MasterEquipments[pd.TouchAircraftEnemy].Name;
			}
			else
			{
				OTB_EnemyScoutSuccess.ToolTip = null;
			}

			//Friendly Star Shell / Flare
			{
				int index = pd.FlareIndexFriend;

				if (index != -1)
				{
					OTB_PlayerBomberPlaneLoss.Text = "#" + (index + 1);
					OTB_PlayerBomberPlaneLoss.Fill = Brushes.White;
					Img_PlayerBomberPlaneLoss.Source = (ImageSource) Application.Current.Resources["Icon_Equipment_Flare"];
					OTB_PlayerBomberPlaneLoss.ToolTip = GeneralRes.StarShellUsed + ": " + pd.FlareFriendInstance.NameWithLevel;

				}
				else
				{
					OTB_PlayerBomberPlaneLoss.ToolTip = null;
				}
			}

			//Enemy Star Shell / Flare
			{
				int index = pd.FlareIndexEnemy;

				if (index != -1)
				{
					OTB_EnemyBomberPlaneLoss.Text = "#" + (index + 1);
					OTB_EnemyBomberPlaneLoss.Fill = Brushes.White;
					Img_EnemyBomberPlaneLoss.Source = (ImageSource)Application.Current.Resources["Icon_Equipment_Flare"];
					OTB_EnemyBomberPlaneLoss.ToolTip = GeneralRes.StarShellUsed + ": " + pd.FlareEnemyInstance.NameWithClass;
				}
				else
				{
					OTB_EnemyBomberPlaneLoss.ToolTip = null;
				}
			}
		}

		private void SetDamageRate(BattleManager bm)
		{
			int rank = bm.PredictWinRank(out double friendrate, out double enemyrate);

			OTB_PlayerDamageTaken.Text = friendrate.ToString("p1");
			OTB_EnemyDamageTaken.Text = enemyrate.ToString("p1");

			if (bm.IsBaseAirRaid)
			{
				int kind = bm.Compass.AirRaidDamageKind;
				OTB_PlayerBattleRank.Text = Constants.GetAirRaidDamageShort(kind);
				OTB_PlayerBattleRank.Fill = (1 <= kind && kind <= 3) ? Brushes.Red : Brushes.White;
			}
			else
			{
				OTB_PlayerBattleRank.Text = Constants.GetWinRank(rank);
				OTB_PlayerBattleRank.Fill = rank >= 4 ? Brushes.White : Brushes.Red;
			}
		}

		private void SetHPBar(BattleData bd)
		{
			KCDatabase db = KCDatabase.Instance;
			bool isPractice = bd.IsPractice;
			bool isFriendCombined = bd.IsFriendCombined;
			bool isEnemyCombined = bd.IsEnemyCombined;
			bool isBaseAirRaid = bd.IsBaseAirRaid;
			bool hasFriend7thShip = bd.Initial.FriendMaxHPs.Count(hp => hp > 0) == 7;

			var initial = bd.Initial;
			var resultHPs = bd.ResultHPs;
			var attackDamages = bd.AttackDamages;


			void EnableHPBar(int fleetIndex, int shipIndex, int initialHP, int resultHP, int maxHP)
			{
				if (shipIndex >= 6)
					return;
				switch (fleetIndex)
				{
					case 1:
						{
							PlayerMain[shipIndex].HP = resultHP;
							PlayerMain[shipIndex].PrevHP = initialHP;
							PlayerMain[shipIndex].MaxHP = maxHP;
							PlayerMain[shipIndex].Background = null;
							PlayerMain[shipIndex].Visibility = Visibility.Visible;
							break;
						}

					case 2:
						{
							PlayerEscort[shipIndex].HP = resultHP;
							PlayerEscort[shipIndex].PrevHP = initialHP;
							PlayerEscort[shipIndex].MaxHP = maxHP;
							PlayerEscort[shipIndex].Background = null;
							PlayerEscort[shipIndex].Visibility = Visibility.Visible;
							break;
						}
					case 3:
						{
							EnemyEscort[shipIndex].HP = resultHP;
							EnemyEscort[shipIndex].PrevHP = initialHP;
							EnemyEscort[shipIndex].MaxHP = maxHP;
							EnemyEscort[shipIndex].Background = null;
							EnemyEscort[shipIndex].Visibility = Visibility.Visible;
							break;
						}
					case 4:
						{
							EnemyMain[shipIndex].HP = resultHP;
							EnemyMain[shipIndex].PrevHP = initialHP;
							EnemyMain[shipIndex].MaxHP = maxHP;
							EnemyMain[shipIndex].Background = null;
							EnemyMain[shipIndex].Visibility = Visibility.Visible;
							break;
						}
				}
			}

			void DisableHPBar(int fleetIndex, int shipIndex)
			{
				if (shipIndex >=6)
					return;
				switch (fleetIndex)
				{
					case 1: PlayerMain[shipIndex].Visibility = Visibility.Collapsed; break;
					case 2: PlayerEscort[shipIndex].Visibility = Visibility.Collapsed; break;
					case 3: EnemyEscort[shipIndex].Visibility = Visibility.Collapsed; break;
					case 4: EnemyMain[shipIndex].Visibility = Visibility.Collapsed; break;
				}
			}



			// friend main
			for (int i = 0; i < initial.FriendInitialHPs.Length; i++)
			{
				int refindex = BattleIndex.Get(BattleSides.FriendMain, i);

				if (initial.FriendInitialHPs[i] != -1)
				{
					EnableHPBar(1, refindex, initial.FriendInitialHPs[i], resultHPs[refindex], initial.FriendMaxHPs[i]);

					string name;
					bool isEscaped;
					bool isLandBase;

					var bar = PlayerMain[refindex];

					if (isBaseAirRaid)
					{
						name = string.Format("Base {0}", i + 1);
						isEscaped = false;
						isLandBase = true;
						bar.ShipType = "LB";        //note: Land Base (Landing Boat もあるらしいが考えつかなかったので)

					}
					else
					{
						ShipData ship = bd.Initial.FriendFleet.MembersInstance[i];
						name = ship.NameWithLevel;
						isEscaped = bd.Initial.FriendFleet.EscapedShipList.Contains(ship.MasterID);
						isLandBase = ship.MasterShip.IsLandBase;
						bar.ShipType = Constants.GetShipClassClassification(ship.MasterShip.ShipType);
					}
					bar.ToolTip = string.Format
						("{0}\r\nHP: ({1} → {2})/{3} ({4}) [{5}]\r\n" + GeneralRes.DamageDone + ": {6}\r\n\r\n{7}",
						name,
						Math.Max(bar.PrevHP, 0),
						Math.Max(bar.HP, 0),
						bar.MaxHP,
						bar.HP - bar.PrevHP,
						Constants.GetDamageState((double)bar.HP / bar.MaxHP, isPractice, isLandBase, isEscaped),
						attackDamages[refindex],
						bd.GetBattleDetail(refindex)
						);

					if (isEscaped) bar.Background = Brushes.AliceBlue;
					else bar.Background = null;
					bar.Update();
				}
				else
				{
					DisableHPBar(1, refindex);
				}
			}


			// enemy main
			for (int i = 0; i < initial.EnemyInitialHPs.Length; i++)
			{
				int refindex = BattleIndex.Get(BattleSides.EnemyMain, i);

				if (initial.EnemyInitialHPs[i] != -1)
				{
					EnableHPBar(4, refindex-12, initial.EnemyInitialHPs[i], resultHPs[refindex], initial.EnemyMaxHPs[i]);
					ShipDataMaster ship = bd.Initial.EnemyMembersInstance[i];

					var bar = EnemyMain[refindex-12];
					bar.ShipType = Constants.GetShipClassClassification(ship.ShipType);

					bar.ToolTip = string.Format("{0} Lv. {1}\r\nHP: ({2} → {3})/{4} ({5}) [{6}]\r\n\r\n{7}",
							ship.NameWithClass,
							initial.EnemyLevels[i],
							Math.Max(bar.PrevHP, 0),
							Math.Max(bar.HP, 0),
							bar.MaxHP,
							bar.HP - bar.PrevHP,
							Constants.GetDamageState((double)bar.HP / bar.MaxHP, isPractice, ship.IsLandBase),
							bd.GetBattleDetail(refindex)
							);
					bar.Update();
				}
				else
				{
					DisableHPBar(4, refindex-12);
				}
			}


			// friend escort
			if (isFriendCombined)
			{
				SP_PlayerEscortContainer.Visibility = Visibility.Visible;

				for (int i = 0; i < initial.FriendInitialHPsEscort.Length; i++)
				{
					int refindex = BattleIndex.Get(BattleSides.FriendEscort, i);

					if (initial.FriendInitialHPsEscort[i] != -1)
					{
						EnableHPBar(2, refindex-6, initial.FriendInitialHPsEscort[i], resultHPs[refindex], initial.FriendMaxHPsEscort[i]);

						ShipData ship = bd.Initial.FriendFleetEscort.MembersInstance[i];
						bool isEscaped = bd.Initial.FriendFleetEscort.EscapedShipList.Contains(ship.MasterID);

						var bar = PlayerEscort[refindex-6];
						bar.ShipType = Constants.GetShipClassClassification(ship.MasterShip.ShipType);

						bar.ToolTip = string.Format(
							"{0} Lv. {1}\r\nHP: ({2} → {3})/{4} ({5}) [{6}]\r\n" + GeneralRes.DamageDone + ": {7}\r\n\r\n{8}",
							ship.MasterShip.NameWithClass,
							ship.Level,
							Math.Max(bar.PrevHP, 0),
							Math.Max(bar.HP, 0),
							bar.MaxHP,
							bar.HP - bar.PrevHP,
							Constants.GetDamageState((double)bar.HP / bar.MaxHP, isPractice, ship.MasterShip.IsLandBase, isEscaped),
							attackDamages[refindex],
							bd.GetBattleDetail(refindex)
							);

						if (isEscaped) bar.Background = Brushes.AliceBlue;
						else bar.Background = null;
						bar.Update();
					}
					else
					{
						DisableHPBar(2, refindex-6);
					}
				}

			}
			else
			{
				SP_PlayerEscortContainer.Visibility = Visibility.Collapsed;
				foreach (var i in BattleIndex.FriendEscort.Skip(Math.Max(bd.Initial.FriendFleet.Members.Count - 6, 0)))
					DisableHPBar(2, i);
			}

			//MoveHPBar(hasFriend7thShip);



			// enemy escort
			if (isEnemyCombined)
			{
				SP_EnemyEscortContainer.Visibility = Visibility.Visible;

				for (int i = 0; i < 6; i++)
				{
					int refindex = BattleIndex.Get(BattleSides.EnemyEscort, i);

					if (initial.EnemyInitialHPsEscort[i] != -1)
					{
						EnableHPBar(3, refindex-18, initial.EnemyInitialHPsEscort[i], resultHPs[refindex], initial.EnemyMaxHPsEscort[i]);

						ShipDataMaster ship = bd.Initial.EnemyMembersEscortInstance[i];

						var bar = EnemyEscort[refindex-18];
						bar.ShipType = Constants.GetShipClassClassification(ship.ShipType);
						bar.ToolTip = string.Format("{0} Lv. {1}\r\nHP: ({2} → {3})/{4} ({5}) [{6}]\r\n\r\n{7}",
								ship.NameWithClass,
								bd.Initial.EnemyLevelsEscort[i],
								Math.Max(bar.PrevHP, 0),
								Math.Max(bar.HP, 0),
								bar.MaxHP,
								bar.HP - bar.PrevHP,
								Constants.GetDamageState((double)bar.HP / bar.MaxHP, isPractice, ship.IsLandBase),
								bd.GetBattleDetail(refindex)
								);
						bar.Update();
					}
					else
					{
						DisableHPBar(3, refindex-18);
					}
				}

			}
			else
			{
				SP_EnemyEscortContainer.Visibility = Visibility.Collapsed;
				foreach (var i in BattleIndex.EnemyEscort)
					DisableHPBar(3, i);
			}




			if (isFriendCombined && isEnemyCombined)
			{
				OTB_EngagementType.Width = 150;
				OTB_PlayerLBASStats.Width = 150;
				SP_AirState.Width = 150;
				Grid_FleetSeparator.Width = 0;
				OTB_PlayerBattleRank.Width = 150;
			} else if (isFriendCombined && !isEnemyCombined)
			{
				OTB_EngagementType.Width = 75;
				OTB_PlayerLBASStats.Width = 75;
				SP_AirState.Width = 75;
				Grid_FleetSeparator.Width = 0;
				OTB_PlayerBattleRank.Width = 75;
			} else if (!isFriendCombined && isEnemyCombined)
			{
				OTB_EngagementType.Width = 75;
				OTB_PlayerLBASStats.Width = 75;
				SP_AirState.Width = 75;
				Grid_FleetSeparator.Width = 0;
				OTB_PlayerBattleRank.Width = 75;
			} else if (!isFriendCombined && !isEnemyCombined)
			{
				OTB_EngagementType.Width = 75;
				OTB_PlayerLBASStats.Width = 75;
				SP_AirState.Width = 75;
				Grid_FleetSeparator.Width = 75;
				OTB_PlayerBattleRank.Width = 75;
			}


			{   // support
				/*
				PhaseSupport? support = null;

				if (bd is BattleDayFromNight bddn)
				{
					if (bddn.NightSupport?.IsAvailable ?? false)
						support = bddn.NightSupport;
				}
				if (support == null)
					support = bd.Support;

				if (support?.IsAvailable ?? false)
				{

					switch (support.SupportFlag)
					{
						case 1:
							FleetFriend.ImageIndex = (int)ResourceManager.EquipmentContent.CarrierBasedTorpedo;
							break;
						case 2:
							FleetFriend.ImageIndex = (int)ResourceManager.EquipmentContent.MainGunL;
							break;
						case 3:
							FleetFriend.ImageIndex = (int)ResourceManager.EquipmentContent.Torpedo;
							break;
						case 4:
							FleetFriend.ImageIndex = (int)ResourceManager.EquipmentContent.DepthCharge;
							break;
						default:
							FleetFriend.ImageIndex = (int)ResourceManager.EquipmentContent.Unknown;
							break;
					}

					FleetFriend.ImageAlign = ContentAlignment.MiddleLeft;
					ToolTipInfo.SetToolTip(FleetFriend, "Support Expedition\r\n" + support.GetBattleDetail());

					if ((isFriendCombined || hasFriend7thShip) && isEnemyCombined)
						FleetFriend.Text = "Friendly";
					else
						FleetFriend.Text = "Friendly";

				}
				else
				{
					FleetFriend.ImageIndex = -1;
					FleetFriend.ImageAlign = ContentAlignment.MiddleCenter;
					FleetFriend.Text = "Friendly";
					ToolTipInfo.SetToolTip(FleetFriend, null);

				} */
			}


			if (bd.Initial.IsBossDamaged)
			{
				EnemyMain[0].Background = Brushes.DarkRed;
			}

			if (!isBaseAirRaid)
			{
				foreach (int i in bd.MVPShipIndexes)
				{
					PlayerMain[BattleIndex.Get(BattleSides.FriendMain, i)].Background = Brushes.Wheat;
					PlayerMain[BattleIndex.Get(BattleSides.FriendMain, i)].OTB_HPCurrent.Fill = Brushes.Black;
				}

				if (isFriendCombined)
				{
					foreach (int i in bd.MVPShipCombinedIndexes)
					{
						PlayerEscort[BattleIndex.Get(BattleSides.FriendEscort, i)].Background = Brushes.Wheat;
						PlayerEscort[BattleIndex.Get(BattleSides.FriendEscort, i)].OTB_HPCurrent.Fill = Brushes.Black;
					}
				}
			}
		}

		private void ClearAerialWarfare()
		{
			OTB_AirState.Text = "-";
			OTB_AirState.ToolTip = null;

			ClearAircraftLabel(OTB_PlayerTotalPlaneLoss);
			ClearAircraftLabel(OTB_EnemyTotalPlaneLoss);
			ClearAircraftLabel(OTB_PlayerBomberPlaneLoss);
			ClearAircraftLabel(OTB_EnemyBomberPlaneLoss);

			OTB_AADefense.Text = "-";
			OTB_AADefense.ToolTip = null;
		}

		private void SetAerialWarfare(PhaseAirBattleBase phaseJet, PhaseAirBattleBase phase1) => SetAerialWarfare(phaseJet, phase1, null);

		private void SetAerialWarfare(PhaseAirBattleBase phaseJet, PhaseAirBattleBase phase1, PhaseAirBattleBase phase2)
		{
			Img_PlayerTotalPlaneLoss.Source = (ImageSource) Application.Current.Resources["Icon_Equipment_CarrierBasedAircraft"];
			Img_EnemyTotalPlaneLoss.Source = (ImageSource) Application.Current.Resources["Icon_Equipment_CarrierBasedAircraft"];
			Img_PlayerBomberPlaneLoss.Source = (ImageSource) Application.Current.Resources["Icon_Equipment_CarrierBasedBomber"];
			Img_EnemyBomberPlaneLoss.Source = (ImageSource) Application.Current.Resources["Icon_Equipment_CarrierBasedBomber"];

			var phases = new[] {
				new AerialWarfareFormatter( phaseJet, "Jet: " ),
				new AerialWarfareFormatter( phase1, "1st: "),
				new AerialWarfareFormatter( phase2, "2nd: "),
			};

			if (!phases[0].Enabled && !phases[2].Enabled)
				phases[1].PhaseName = "";


			void SetShootdown(OutlinedTextBlock otb, int stage, bool isFriend, bool needAppendInfo)
			{
				var phasesEnabled = phases.Where(p => p.GetEnabled(stage));

				if (needAppendInfo)
				{
					otb.Text = string.Join(",", phasesEnabled.Select(p => "-" + p.GetAircraftLost(stage, isFriend)));
					otb.ToolTip = string.Join("", phasesEnabled.Select(p => $"{p.PhaseName}-{p.GetAircraftLost(stage, isFriend)}/{p.GetAircraftTotal(stage, isFriend)}\r\n"));
				}
				else
				{
					otb.Text = $"-{phases[1].GetAircraftLost(stage, isFriend)}/{phases[1].GetAircraftTotal(stage, isFriend)}";
					otb.ToolTip = null;
				}

				if (phasesEnabled.Any(p =>
					p.GetAircraftTotal(stage, isFriend) > 0 &&
					p.GetAircraftLost(stage, isFriend) == p.GetAircraftTotal(stage, isFriend)))
					otb.Fill = Brushes.Red;
				else
					otb.Fill = Brushes.White;
			}

			void ClearAADefenseLabel()
			{
				OTB_AADefense.Text = "AA Defense";
				OTB_AADefense.ToolTip = null;
			}



			if (phases[1].Stage1Enabled)
			{
				bool needAppendInfo = phases[0].Stage1Enabled || phases[2].Stage1Enabled;
				var phases1 = phases.Where(p => p.Stage1Enabled);
				
				OTB_AirState.Text = Constants.GetAirSuperiority(phases[1].Air.AirSuperiority);
				switch (phases[1].Air.AirSuperiority)
				{
					case 1: //AS+
					case 2: //AS
						OTB_AirState.Fill = Brushes.ForestGreen;
						break;
					case 3: //AI
					case 4: //AI-
						OTB_AirState.Fill = Brushes.Red;
						break;
					default: //AP
						OTB_AirState.Fill = Brushes.White;
						break;
				}
				OTB_AirState.ToolTip = needAppendInfo ? string.Join("", phases1.Select(p => $"{p.PhaseName}{Constants.GetAirSuperiority(p.Air.AirSuperiority)}\r\n")) : null;


				SetShootdown(OTB_PlayerTotalPlaneLoss, 1, true, needAppendInfo);
				SetShootdown(OTB_EnemyTotalPlaneLoss, 1, false, needAppendInfo);

				void SetTouch(OutlinedTextBlock otb, bool isFriend)
				{
					if (phases1.Any(p => p.GetTouchAircraft(isFriend) > 0))
					{
						otb.ToolTip += "Contact\r\n" + string.Join("\r\n", phases1.Select(p => $"{p.PhaseName}{(KCDatabase.Instance.MasterEquipments[p.GetTouchAircraft(isFriend)]?.Name ?? "(none)")}"));
					}
				}
				SetTouch(OTB_PlayerTotalPlaneLoss, true);
				SetTouch(OTB_EnemyTotalPlaneLoss, false);
			}
			else
			{
				OTB_AirState.Text = Constants.GetAirSuperiority(-1);
				OTB_AirState.ToolTip = null;

				ClearAircraftLabel(OTB_PlayerTotalPlaneLoss);
				ClearAircraftLabel(OTB_EnemyTotalPlaneLoss);
			}


			if (phases[1].Stage2Enabled)
			{
				bool needAppendInfo = phases[0].Stage2Enabled || phases[2].Stage2Enabled;
				var phases2 = phases.Where(p => p.Stage2Enabled);

				SetShootdown(OTB_PlayerBomberPlaneLoss, 2, true, needAppendInfo);
				SetShootdown(OTB_EnemyBomberPlaneLoss, 2, false, needAppendInfo);


				if (phases2.Any(p => p.Air.IsAACutinAvailable))
				{
					OTB_AADefense.Text = "#" + string.Join("/", phases2.Select(p => p.Air.IsAACutinAvailable ? (p.Air.AACutInIndex + 1).ToString() : "-"));
					OTB_AADefense.Fill = Brushes.Green;
					OTB_AADefense.ToolTip = "AACI\r\n" +
						string.Join("\r\n", phases2.Select(p => p.PhaseName + (p.Air.IsAACutinAvailable ? $"{p.Air.AACutInShip.NameWithLevel}\r\nAACI type: {p.Air.AACutInKind} ({Constants.GetAACutinKind(p.Air.AACutInKind)})" : "(did not activate)")));
				}
				else
				{
					ClearAADefenseLabel();
				}
			}
			else
			{
				ClearAircraftLabel(OTB_PlayerBomberPlaneLoss);
				ClearAircraftLabel(OTB_EnemyBomberPlaneLoss);
				ClearAADefenseLabel();
			}
		}
		void ClearAircraftLabel(OutlinedTextBlock otb)
		{
			otb.Text = "-";
			otb.Fill = Brushes.White;
			otb.ToolTip = null;
		}

		private void SetBaseAirAttack(PhaseBaseAirAttack baseAirAttack)
		{
			if (baseAirAttack != null && baseAirAttack.IsAvailable)
			{

				OTB_PlayerLBASStats.Text = "LBAS";

				var sb = new StringBuilder();
				int index = 1;

				foreach (var phase in baseAirAttack.AirAttackUnits)
				{

					sb.AppendFormat(GeneralRes.BaseWave + " - " + GeneralRes.BaseAirCorps + " :\r\n",
						index, phase.AirUnitID);

					if (phase.IsStage1Available)
					{
						sb.AppendFormat("　St1: " + GeneralRes.FriendlyAir + " -{0}/{1} | " + GeneralRes.EnemyAir + " -{2}/{3} | {4}\r\n",
							phase.AircraftLostStage1Friend, phase.AircraftTotalStage1Friend,
							phase.AircraftLostStage1Enemy, phase.AircraftTotalStage1Enemy,
							Constants.GetAirSuperiority(phase.AirSuperiority));
					}
					if (phase.IsStage2Available)
					{
						sb.AppendFormat("　St2: " + GeneralRes.FriendlyAir + " -{0}/{1} | " + GeneralRes.EnemyAir + " -{2}/{3}\r\n",
							phase.AircraftLostStage2Friend, phase.AircraftTotalStage2Friend,
							phase.AircraftLostStage2Enemy, phase.AircraftTotalStage2Enemy);
					}

					index++;
				}

				OTB_PlayerLBASStats.ToolTip = sb.ToString();

			}
			else
			{
				ClearBaseAirAttack();
			}
		}

		private void ClearBaseAirAttack()
		{
			OTB_PlayerLBASStats.Text = "-";
			OTB_PlayerLBASStats.ToolTip = null;
		}

		private void SetSearchingResult(BattleData bd)
		{
			void SetResult(Image img, OutlinedTextBlock otb, int search)
			{
				otb.Text = Constants.GetSearchingResultShort(search);
				img.Source = search > 0 
						? (ImageSource)(search < 4 
							? Application.Current.Resources["Icon_Equipment_Seaplane"] 
							: Application.Current.Resources["Icon_Equipment_RADAR"]) 
						: (ImageSource)Application.Current.Resources["Icon_Nothing"];
				otb.ToolTip = null;
			}

			SetResult(Img_PlayerScouting, OTB_PlayerScoutSuccess, bd.Searching.SearchingFriend);
			SetResult(Img_EnemyScouting, OTB_EnemyScoutSuccess, bd.Searching.SearchingEnemy);
		}

		private void ClearSearchingResult()
		{
			void ClearResult(Image img, OutlinedTextBlock otb)
			{
				otb.Text = "-";
				img.Source = (ImageSource)Application.Current.Resources["Icon_Nothing"];
				otb.ToolTip = null;
			}

			ClearResult(Img_PlayerScouting, OTB_PlayerScoutSuccess);
			ClearResult(Img_EnemyScouting, OTB_EnemyScoutSuccess);
		}

		private void SetFormation(BattleManager bm)
		{
			OTB_PlayerFormation.Text = Constants.GetFormationShort(bm.FirstBattle.Searching.FormationFriend);
			OTB_EnemyFormation.Text = Constants.GetFormationShort(bm.FirstBattle.Searching.FormationEnemy);
			OTB_EngagementType.Text = Constants.GetEngagementForm(bm.FirstBattle.Searching.EngagementForm);

			if (bm.Compass != null && bm.Compass.EventID == 5)
			{
				OTB_EnemyHeader.Fill = Brushes.Red;
				OTB_EnemyEscortHeader.Fill = Brushes.Red;
			}
			else
			{
				OTB_EnemyHeader.Fill = Brushes.White;
				OTB_EnemyEscortHeader.Fill = Brushes.White;
			}

			if (bm.IsEnemyCombined && bm.StartsFromDayBattle)
			{
				bool willMain = bm.WillNightBattleWithMainFleet();
				SP_EnemyFleetContainer.Background = willMain ? Brushes.LightSteelBlue : null;
				SP_EnemyEscortContainer.Background = willMain ? null : Brushes.LightSteelBlue;
			}
			else
			{
				SP_EnemyFleetContainer.Background =
				SP_EnemyEscortContainer.Background = null;
			}

			switch (bm.FirstBattle.Searching.EngagementForm)
			{
				case 3:
					OTB_EngagementType.Fill = Brushes.ForestGreen;
					break;
				case 4:
					OTB_EngagementType.Fill = Brushes.Red;
					break;
				default:
					OTB_EngagementType.Fill = Brushes.White;
					break;
			}
		}

		private class AerialWarfareFormatter
		{
			public readonly PhaseAirBattleBase Air;
			public string PhaseName;

			public AerialWarfareFormatter(PhaseAirBattleBase air, string phaseName)
			{
				Air = air;
				PhaseName = phaseName;
			}

			public bool Enabled => Air != null && Air.IsAvailable;
			public bool Stage1Enabled => Enabled && Air.IsStage1Available;
			public bool Stage2Enabled => Enabled && Air.IsStage2Available;

			public bool GetEnabled(int stage)
			{
				if (stage == 1)
					return Stage1Enabled;
				else if (stage == 2)
					return Stage2Enabled;
				else
					throw new ArgumentOutOfRangeException();
			}

			public int GetAircraftLost(int stage, bool isFriend)
			{
				if (stage == 1)
					return isFriend ? Air.AircraftLostStage1Friend : Air.AircraftLostStage1Enemy;
				else if (stage == 2)
					return isFriend ? Air.AircraftLostStage2Friend : Air.AircraftLostStage2Enemy;
				else
					throw new ArgumentOutOfRangeException();
			}

			public int GetAircraftTotal(int stage, bool isFriend)
			{
				if (stage == 1)
					return isFriend ? Air.AircraftTotalStage1Friend : Air.AircraftTotalStage1Enemy;
				else if (stage == 2)
					return isFriend ? Air.AircraftTotalStage2Friend : Air.AircraftTotalStage2Enemy;
				else
					throw new ArgumentOutOfRangeException();
			}

			public int GetTouchAircraft(bool isFriend) => isFriend ? Air.TouchAircraftFriend : Air.TouchAircraftEnemy;

		}
	}
}
