using HarmonyLib;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v1;
using MCM.Abstractions.Settings.Base.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
using TaleWorlds.CampaignSystem.SandBox.GameComponents.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace Warlord
{
    
    public class SupplyLines : MBSubModuleBase
    {

        protected override void OnSubModuleLoad()
        {
            Harmony harmony = new Harmony("SupplyLines");
            harmony.Patch(typeof(MobileParty).GetMethod("get_TotalWage"), new HarmonyMethod(typeof(MobilePartyPatch).GetMethod(nameof(MobilePartyPatch.get_TotalWagePrefix))));
            harmony.Patch(typeof(DefaultInventoryCapacityModel).GetMethod("CalculateInventoryCapacity"), new HarmonyMethod(typeof(DefaultInventoryCapacityModelPatch).GetMethod(nameof(DefaultInventoryCapacityModelPatch.CalculateInventoryCapacityPrefix))));
            harmony.Patch(typeof(CaravansCampaignBehavior).GetMethod("SellGoods", BindingFlags.NonPublic | BindingFlags.Instance), new HarmonyMethod(typeof(CaravansCampaignBehaviorPatch).GetMethod(nameof(CaravansCampaignBehaviorPatch.SellGoodsPrefix))));
            harmony.Patch(typeof(CaravansCampaignBehavior).GetMethod("OnMapEventEnded", BindingFlags.NonPublic | BindingFlags.Instance), new HarmonyMethod(typeof(CaravansCampaignBehaviorPatch).GetMethod(nameof(CaravansCampaignBehaviorPatch.OnMapEventEndedPrefix))));
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            if (!(game.GameType is Campaign)) return;

            CampaignGameStarter cgs = gameStarterObject as CampaignGameStarter;
            cgs.AddBehavior(new SupplyLinesCampaignBehavior());
        }
    }

    public class MobilePartyPatch
    {
        public static bool get_TotalWagePrefix(MobileParty __instance, ref int __result)
        {
            string partyID = __instance.StringId;
            if (partyID.EndsWith("_SupplyLine"))
            {
                __result = __instance.Party.NumberOfAllMembers * 10 - 5000;
                return false;
            }

            return true;
        }
    }

    public class DefaultInventoryCapacityModelPatch
    {
        public static bool CalculateInventoryCapacityPrefix(
            ref ExplainedNumber __result,
            TextObject ____textBase,
            TextObject ____textTroops,
            TextObject ____textSpareMounts,
            TextObject ____textPackAnimals,
          MobileParty mobileParty,
          bool includeDescriptions = false,
          int additionalTroops = 0,
          int additionalSpareMounts = 0,
          int additionalPackAnimals = 0,
          bool includeFollowers = false)
        {
            ExplainedNumber explainedNumber = new ExplainedNumber(includeDescriptions: includeDescriptions);
            PartyBase party = mobileParty.Party;
            int numberOfMounts = party.NumberOfMounts;
            int ofHealthyMembers = party.NumberOfHealthyMembers;
            int numberOfPackAnimals = party.NumberOfPackAnimals;
            if (includeFollowers)
            {
                foreach (MobileParty attachedParty in mobileParty.AttachedParties)
                {
                    numberOfMounts += party.NumberOfMounts;
                    ofHealthyMembers += party.NumberOfHealthyMembers;
                    numberOfPackAnimals += party.NumberOfPackAnimals;
                }
            }
            if (mobileParty.HasPerk(DefaultPerks.Steward.ArenicosHorses))
                ofHealthyMembers += MathF.Round((float)ofHealthyMembers * DefaultPerks.Steward.ArenicosHorses.PrimaryBonus);
            if (mobileParty.HasPerk(DefaultPerks.Steward.ForcedLabor))
                ofHealthyMembers += party.PrisonRoster.TotalHealthyCount;
            explainedNumber.Add(10f, ____textBase);
            float num1;
            if (mobileParty.IsCaravan && (!MySettings.Instance?.ApplyTweaksToCaravans ?? false))
            {
                explainedNumber.Add((float)(ofHealthyMembers * 20.0), ____textTroops);
                explainedNumber.Add((float)(numberOfMounts * 20.0), ____textSpareMounts);
                num1 = (float)(numberOfPackAnimals * 100.0);
            }
            else
            {
                explainedNumber.Add((float)(ofHealthyMembers * 20.0 * (MySettings.Instance?.HealthyTroopCarryMultiplier ?? 0.25f)), ____textTroops);
                explainedNumber.Add((float)(numberOfMounts * 20.0 * (MySettings.Instance?.SpareMountCarryMultiplier ?? 0.5f)), ____textSpareMounts);
                num1 = (float)(numberOfPackAnimals * 100.0 * (MySettings.Instance?.PackAnimalCarryMultiplier ?? 0.5f));
            }
            float num2 = 0.0f;
            if (mobileParty.HasPerk(DefaultPerks.Scouting.BeastWhisperer, true))
                num2 += DefaultPerks.Scouting.BeastWhisperer.SecondaryBonus;
            if (mobileParty.HasPerk(DefaultPerks.Riding.FilledToBrim))
                num2 += DefaultPerks.Riding.FilledToBrim.PrimaryBonus;
            if (mobileParty.HasPerk(DefaultPerks.Steward.ArenicosMules))
                num2 += DefaultPerks.Steward.ArenicosMules.PrimaryBonus;
            float num3 = num1 * (num2 + 1f);
            explainedNumber.Add(num3, ____textPackAnimals);
            if (mobileParty.HasPerk(DefaultPerks.Trade.CaravanMaster))
                explainedNumber.AddFactor(DefaultPerks.Trade.CaravanMaster.PrimaryBonus, DefaultPerks.Trade.CaravanMaster.Name);
            explainedNumber.LimitMin(10f);
            __result = explainedNumber;
            return false;
        }
    }

    public class CaravansCampaignBehaviorPatch//Non-caravan parties called causing KeyNotFoundException with a dictionary
    {
        public static bool SellGoodsPrefix(CaravansCampaignBehavior __instance, MobileParty caravanParty, Town town)
        {
            return caravanParty.IsCaravan;
        }
        public static bool OnMapEventEndedPrefix(MapEvent mapEvent)
        {
            foreach (PartyBase m in mapEvent.InvolvedParties)
            {
                if (m.IsMobile && m.MobileParty.IsCaravan && IsWinnerSide(mapEvent, m.Side))
                {
                    if (m.Id.EndsWith("_SupplyLine"))
                    {
                        if (!m.ItemRoster.Any())
                        {
#if DEBUG
                            MessageManager.DisplayDebugMessage(m.ToString() + " had no items on MapEventEnded! Crash averted!");
#endif
                            m.ItemRoster.AddToCounts(DefaultItems.Grain, 10);
                        }
                    }
                }
            }
            return true;
        }

        private static bool IsWinnerSide(MapEvent mapEvent, BattleSideEnum side)
        {
            return (mapEvent.BattleState == BattleState.DefenderVictory && side == BattleSideEnum.Defender) || (mapEvent.BattleState == BattleState.AttackerVictory && side == BattleSideEnum.Attacker);
        }
    }

    public class MySettings : AttributeGlobalSettings<MySettings>
    {
        public override string Id => "SupplyLinesSettings";
        public override string DisplayName => new TextObject("{=SL_TITLE}Supply Lines").ToString();
        public override string FolderName => "SupplyLines";
        public override string FormatType => "json";

        [SettingProperty("{=SL_SETTINGS_NAME_1}Display Party Routes", RequireRestart = false, HintText = "{=SL_SETTINGS_DESC_1}Enabling this option will make food supply parties display their origin and destination.", Order = 1)]
        [SettingPropertyGroup("{=SL_SETTINGS_CAT_1}Supply Parties", GroupOrder = 1)]
        public bool DisplayRoutes { get; set; } = true;
        [SettingProperty("{=SL_SETTINGS_NAME_2}Donation Quantity", 10, 100, RequireRestart = false, HintText = "{=SL_SETTINGS_DESC_2}Amount of food you will donate to your settlement when using the menu option.", Order = 2)]
        [SettingPropertyGroup("{=SL_SETTINGS_CAT_1}Supply Parties", GroupOrder = 1)]
        public int DonationAmount { get; set; } = 20;
        [SettingProperty("{=SL_SETTINGS_NAME_3}Supply Party Quantity", 10, 100, RequireRestart = false, HintText = "{=SL_SETTINGS_DESC_3}Amount of food supply parties will carry when supplying siege armies or settlements. Also when purchasing grain.", Order = 3)]
        [SettingPropertyGroup("{=SL_SETTINGS_CAT_1}Supply Parties", GroupOrder = 1)]
        public int SUPPLY_AMOUNT { get; set; } = 50;
        [SettingProperty("{=SL_SETTINGS_NAME_4}Army Food Limit", 10, 100, RequireRestart = false, HintText = "{=SL_SETTINGS_DESC_4}The number of days in food an army leader carries that triggers a supply party to be sent out.", Order = 4)]
        [SettingPropertyGroup("{=SL_SETTINGS_CAT_1}Supply Parties", GroupOrder = 1)]
        public int ARMY_FOOD_LIMIT { get; set; } = 4;
        [SettingProperty("{=SL_SETTINGS_NAME_5}Supply Siege Armies Only", RequireRestart = false, HintText = "{=SL_SETTINGS_DESC_5}Enabling this option will only send supply parties to armies besieging settlements.", Order = 5)]
        [SettingPropertyGroup("{=SL_SETTINGS_CAT_1}Supply Parties", GroupOrder = 1)]
        public bool SUPPLY_SIEGES_ONLY { get; set; } = true;
        [SettingProperty("{=SL_SETTINGS_NAME_6}Multiple Army Supply Parties", RequireRestart = false, HintText = "{=SL_SETTINGS_DESC_6}Enabling this option will allow your faction to supply armies with more than one party at a time. Each day it checks the army's status and will send a supply party if needed.", Order = 6)]
        [SettingPropertyGroup("{=SL_SETTINGS_CAT_1}Supply Parties", GroupOrder = 1)]
        public bool MULTIPLE_ARMY_SUPPLY { get; set; } = false;

        [SettingProperty("{=SL_SETTINGS_NAME_7}Pack Animal Carry Multiplier", 0f, 5f, RequireRestart = false, HintText = "{=SL_SETTINGS_DESC_7}This will adjust the carry capacity of pack animals by using the product of this multiplier and the current capacity. Use values less than one to reduce. ;)", Order = 1)]
        [SettingPropertyGroup("{=SL_SETTINGS_CAT_3}Tweaks", GroupOrder = 3)]
        public float PackAnimalCarryMultiplier { get; set; } = 0.5f;
        [SettingProperty("{=SL_SETTINGS_NAME_8}Spare Mount Carry Multiplier", 0f, 5f, RequireRestart = false, HintText = "{=SL_SETTINGS_DESC_8}This will adjust the carry capacity of spare mounts by using the product of this multiplier and the current capacity. Use values less than one to reduce. ;)", Order = 2)]
        [SettingPropertyGroup("{=SL_SETTINGS_CAT_3}Tweaks", GroupOrder = 3)]
        public float SpareMountCarryMultiplier { get; set; } = 0.5f;
        [SettingProperty("{=SL_SETTINGS_NAME_9}Healthy Troop Carry Multiplier", 0f, 5f, RequireRestart = false, HintText = "{=SL_SETTINGS_DESC_9}This will adjust the carry capacity of healthy troops by using the product of this multiplier and the current capacity. Use values less than one to reduce. ;) TaleWorlds made mounts and humans carry capacity the same so you should lower this more than the setting for mounts.", Order = 3)]
        [SettingPropertyGroup("{=SL_SETTINGS_CAT_3}Tweaks", GroupOrder = 3)]
        public float HealthyTroopCarryMultiplier { get; set; } = 0.25f;
        [SettingProperty("{=SL_SETTINGS_NAME_10}Caravans", RequireRestart = false, HintText = "{=SL_SETTINGS_DESC_10}Apply multipliers to caravans.", Order = 3)]
        [SettingPropertyGroup("{=SL_SETTINGS_CAT_3}Tweaks", GroupOrder = 3)]
        public bool ApplyTweaksToCaravans { get; set; }
    }

    public class SupplyLinesCampaignBehavior : CampaignBehaviorBase
    {
        [SaveableField(0)]
        public List<MobileParty> supplyParties = new List<MobileParty>();
        [SaveableField(1)]
        private List<Town> destTowns = new List<Town>();
        [SaveableField(2)]
        private List<Town> originTowns = new List<Town>();
        [SaveableField(3)]
        private List<bool> goingHomes = new List<bool>();
        [SaveableField(4)]
        private bool returnAfterDelivery;
        [SaveableField(5)]
        private bool enableAIForPlayer;
        [SaveableField(6)]
        private List<bool> returnAfterDeliveries = new List<bool>();
        [SaveableField(7)]
        private List<MobileParty> siegeParties = new List<MobileParty>();
        [SaveableField(8)]
        private List<bool> isSiegeSupply = new List<bool>();
        [SaveableField(9)]
        private bool currentRoutesSetting;
        [SaveableField(10)]
        private List<TroopRosterElement[]> troopRosterCopies = new List<TroopRosterElement[]>();
        [SaveableField(11)]
        public List<bool> wasInBattles = new List<bool>();
        [SaveableField(12)]
        public List<int> supplyAmounts = new List<int>();


        public static SupplyLinesCampaignBehavior Instance;

        internal SupplyLinesCampaignBehavior()
        {
            Instance = this;
        }

        private bool ListsInSync()
        {
            int count = supplyParties.Count;
            return count == destTowns.Count && count == originTowns.Count && count == goingHomes.Count && count == returnAfterDeliveries.Count && count == siegeParties.Count && count == isSiegeSupply.Count && count == troopRosterCopies.Count && count == wasInBattles.Count && count == supplyAmounts.Count;
        }

        private bool MobilePartyIsFucked(MobileParty m)
        {
            return m == null || !m.IsActive || m.IsDisbanding || m.MemberRoster.TotalManCount == 0;
        }

        private bool TownIsAvailable(Town t)
        {
            if (t.IsUnderSiege) return false;

            int SUPPLY_AMOUNT = MySettings.Instance?.SUPPLY_AMOUNT ?? 50;
            if (t.FoodStocks <= SUPPLY_AMOUNT || (t.FoodStocks < SUPPLY_AMOUNT * 2 && t.FoodChange < -5)) return false;

            return true;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(AddGameMenus));
            CampaignEvents.TickEvent.AddNonSerializedListener(this, (ticker) =>
            {
                int SUPPLY_AMOUNT = MySettings.Instance?.SUPPLY_AMOUNT ?? 50;
                if (supplyParties.Count > 0)
                {
                    bool rSetting = MySettings.Instance?.DisplayRoutes ?? false;
                    if (rSetting != currentRoutesSetting)
                    {
                        currentRoutesSetting = rSetting;
                        foreach (MobileParty m in supplyParties)
                        {
                            TextObject text;
                            if (rSetting)
                            {
                                string id = m.StringId;
                                ParseId(m, out string originID, out string destID);
                                Settlement origin = Settlement.FindFirst(x => x.Town?.StringId == originID);
                                Settlement dest = Settlement.FindFirst(x => x.Town?.StringId == destID);
                                MobileParty destParty = MobileParty.All.ElementAtOrDefault(MobileParty.All.FindIndex(x => x.StringId == destID));
                                text = new TextObject("{=SL_PARTY_NAME}Food Supply: {FROM_NAME} to {TO_NAME}");
                                text.SetTextVariable("FROM_NAME", origin.Name);
                                text.SetTextVariable("TO_NAME", dest == null ? destParty.Name : dest.Name);
                            }
                            else
                            {
                                text = new TextObject("{=SL_PARTY_NAME_2}Food Supply");
                            }

                            m.SetCustomName(text);
                        }
                    }

                    if (ListsInSync())
                    {
                        for (int i = supplyParties.Count - 1; i > -1; i--)
                        {
                            MobileParty m = supplyParties[i];

                            if (MobilePartyIsFucked(m))
                            {
                                RemoveFromListsAtIndex(i);
                                continue;
                            }

                            if (m.MapEvent != null)
                            {
                                wasInBattles[i] = true;
                                continue;
                            }
                            else if (wasInBattles[i])
                            {
                                wasInBattles[i] = false;
                                troopRosterCopies[i] = m.MemberRoster.GetTroopRoster().ToArray();//need to fix
                                continue;
                            }
                            else
                            {
                                TroopRosterElement[] roster = troopRosterCopies.ElementAt(i);
                                TroopRoster r = m.MemberRoster;
                                if (r.TotalManCount != roster.Sum(x => x.Number))
                                {
                                    for (int j = 0; j < roster.Length; j++)
                                    {
                                        TroopRosterElement t = roster.ElementAt(j);
                                        CharacterObject c = t.Character;
                                        int index = r.FindIndexOfTroop(c);
                                        if (index == -1)
                                        {
                                            r.AddToCounts(c, t.Number);
                                        }
                                        else
                                        {
                                            r.AddToCounts(c, t.Number - r.GetElementCopyAtIndex(index).Number);
                                        }
                                    }

#if DEBUG
                                    //MessageManager.DisplayDebugMessage("Reset troop roster for " + m.Name.ToString() + ".");
#endif
                                }
                            }

                            Town destT = destTowns[i];
                            if (isSiegeSupply[i])
                            {
                                //INSERT GOING TO SIEGE PARTY AI AND GOING HOME AFTER
                                MobileParty siegeParty = siegeParties[i];
                                if (MobilePartyIsFucked(siegeParty))// || siegeParty.Army == null || siegeParty.SiegeEvent == null
                                {
                                    goingHomes[i] = true;
                                    isSiegeSupply[i] = false;
                                    continue;
                                }

                                if (m.Position2D.Distance(siegeParty.Position2D) < 1f)
                                {
                                    ItemRoster items = siegeParty.ItemRoster;
                                    if (!items.Contains(new ItemRosterElement(DefaultItems.Grain)))
                                    {
                                        items.Add(new ItemRosterElement(DefaultItems.Grain, supplyAmounts[i]));
                                    }
                                    else
                                    {
                                        items.AddToCounts(DefaultItems.Grain, supplyAmounts[i]);
                                    }

                                    m.ItemRoster.Clear();
                                    m.ItemRoster.Add(new ItemRosterElement(DefaultItems.Grain, 10));
                                    goingHomes[i] = true;
                                    isSiegeSupply[i] = false;
                                    continue;
                                }
                                else
                                {
                                    m.SetMoveGoToPoint(siegeParty.Position2D);
                                }

                                continue;
                            }
                            else if (!goingHomes[i] && destT != null && destT.MapFaction.IsAtWarWith(m.MapFaction))
                            {
                                goingHomes[i] = true;
                            }

                            Town destTown = goingHomes[i] ? originTowns[i] : destT;

                            if (destTown == null)
                            {
                                RemoveFromListsAtIndex(i);
                                continue;
                            }

                            Settlement s = destTown.Settlement;
                            if (m.CurrentSettlement == s)
                            {
                                if (goingHomes[i])
                                {
                                    if (m.ItemRoster.Contains(new ItemRosterElement(DefaultItems.Grain)))
                                    {
                                        int grainOnHand = m.ItemRoster.GetItemNumber(DefaultItems.Grain);
                                        if (grainOnHand > supplyAmounts[i] * 0.8)//no delivery made... siege army gave up siege or some other misfortune
                                        {
                                            destTown.FoodStocks = Math.Min(destTown.FoodStocks + supplyAmounts[i], destTown.FoodStocksUpperLimit());
                                        }
                                    }
                                    if (destTown.GarrisonParty == null) s.AddGarrisonParty();
                                    destTown.GarrisonParty.MemberRoster.Add(m.MemberRoster);//crash??
                                    LeaveSettlementAction.ApplyForParty(m);
                                    m.RemoveParty();
                                    RemoveFromListsAtIndex(i);
                                }
                                else
                                {
                                    destTown.FoodStocks += supplyAmounts[i];
                                    if (returnAfterDeliveries[i])
                                    {
                                        goingHomes[i] = true;
                                        m.ItemRoster.Clear();
                                        m.ItemRoster.Add(new ItemRosterElement(DefaultItems.Grain, 10));
                                    }
                                    else
                                    {
                                        destTown.GarrisonParty.MemberRoster.Add(m.MemberRoster);
                                        LeaveSettlementAction.ApplyForParty(m);
                                        m.RemoveParty();
                                        RemoveFromListsAtIndex(i);
                                    }
                                }
                            }
                            else if (m.TargetSettlement != s)
                            {
                                m.SetMoveGoToSettlement(s);
                                m.Ai.SetDoNotMakeNewDecisions(true);
                            }
                        }
                    }
                    else
                    {
                        TextObject text = new TextObject("{=SL_MESSAGE_1}Supply Lines Warning: Lists required resyncing! This might happen after updates.");
                        MessageManager.DisplayMessage(text, Colors.Yellow);
                        ClearAllLists();
                        FindLostParties();
                    }
                }
            });
            CampaignEvents.DailyTickClanEvent.AddNonSerializedListener(this, (clan) =>
            {
                if (clan == null) return;
                bool isPlayerClan = clan == Clan.PlayerClan;
                if (isPlayerClan && !enableAIForPlayer) return;

                int SUPPLY_AMOUNT = 50, armyFoodLimit = 4;
                bool siegesOnly = true, multipleArmySupply = false;
                if (MySettings.Instance != null)
                {
                    SUPPLY_AMOUNT = MySettings.Instance.SUPPLY_AMOUNT;
                    armyFoodLimit = MySettings.Instance.ARMY_FOOD_LIMIT;
                    siegesOnly = MySettings.Instance.SUPPLY_SIEGES_ONLY;
                    multipleArmySupply = MySettings.Instance.MULTIPLE_ARMY_SUPPLY;
                }
                int DOUBLE_SUPPLY = SUPPLY_AMOUNT * 2;
                List<Town> needsSupport = new List<Town>();
                List<Town> canSupport = new List<Town>();
                foreach (Town t in clan.Fiefs)
                {
                    if (!TownIsAvailable(t)) continue;

                    MobileParty garrisonParty = t.GarrisonParty;
                    if (garrisonParty == null) continue;

                    TroopRoster garrisonRoster = garrisonParty.MemberRoster;
                    if (garrisonRoster == null) continue;

                    if (garrisonRoster.TotalHealthyCount < 60) continue;

                    if (t.FoodStocks < SUPPLY_AMOUNT && t.FoodChange < 0)
                    {
                        needsSupport.Add(t);
                    }
                    else if ((t.FoodStocks > DOUBLE_SUPPLY && t.FoodChange > -15) || (t.FoodStocks > SUPPLY_AMOUNT && t.FoodChange > 0))
                    {
                        canSupport.Add(t);
                    }
                }

                if (needsSupport.Count > 0 && canSupport.Count == 0)
                {
                    for (int i = needsSupport.Count - 1; i > -1; i--)
                    {
                        Town nTown = needsSupport[i];

                        //purchase from nearby clan
                        float shortestDistance = float.MaxValue;
                        Town closestTown = null;
                        int closestCost = 0;
                        foreach (Town t in Town.AllFiefs)
                        {
                            if (t.OwnerClan == clan) continue;
                            if (t.MapFaction.IsAtWarWith(clan.MapFaction)) continue;
                            if (!TownIsAvailable(t)) continue;

                            MobileParty garrisonParty = t.GarrisonParty;
                            if (garrisonParty == null) continue;

                            TroopRoster garrisonRoster = garrisonParty.MemberRoster;
                            if (garrisonRoster == null) continue;

                            if (garrisonRoster.TotalHealthyCount < 60) continue;

                            if ((t.FoodStocks > DOUBLE_SUPPLY && t.FoodChange > -15) || (t.FoodStocks > SUPPLY_AMOUNT && t.FoodChange > 0))
                            {
                                float currentDistance = nTown.Settlement.Position2D.Distance(t.Settlement.Position2D);

                                int cost = (int)currentDistance * 20 + SUPPLY_AMOUNT * t.MarketData.GetPrice(DefaultItems.Grain, isSelling: true);
                                if (nTown.OwnerClan.Leader.Gold >= cost)
                                {
                                    if (currentDistance < shortestDistance)
                                    {
                                        shortestDistance = currentDistance;
                                        closestTown = t;
                                        closestCost = cost;
                                    }
                                }
                            }

                            if (closestTown == null) continue;

                            MobileParty m = InitializeAISupplyParty(closestTown, nTown, null, isPlayerClan);
                            if (m == null) continue;

                            closestCost *= -1;
                            Hero.MainHero.ChangeHeroGold(closestCost);
                            t.OwnerClan.Leader.ChangeHeroGold(closestCost);

                            if (isPlayerClan)
                            {
                                TextObject text = new TextObject("{=SL_MESSAGE_5}{CLOSEST_TOWN} (Food Stocks = {FOOD_STOCKS}; Change = {FOOD_CHANGE}) is supporting {TOWN_NAME} (Food Stocks = {TOWN_FOOD_LEFT}; Change = {TOWN_FOOD_CHANGE})");
                                text.SetTextVariable("CLOSEST_TOWN", closestTown.Name);
                                text.SetTextVariable("FOOD_STOCKS", closestTown.FoodStocks.ToString("n0"));
                                text.SetTextVariable("FOOD_CHANGE", closestTown.FoodChange.ToString("n0"));
                                text.SetTextVariable("TOWN_NAME", nTown.Name);
                                text.SetTextVariable("TOWN_FOOD_LEFT", nTown.FoodStocks.ToString("n0"));
                                text.SetTextVariable("TOWN_FOOD_CHANGE", nTown.FoodChange.ToString("n0"));
                                MessageManager.DisplayMessage(text);
                            }

                            m.SetMoveGoToSettlement(nTown.Settlement);

                            needsSupport.RemoveAt(i);
                            break;
                        }
                    }
                }

                MobileParty[] allParties = MobileParty.All.ToArray();
                foreach (MobileParty m in allParties)
                {
                    if (m == null) continue;
                    if (m.ActualClan != clan) continue;
                    if (siegesOnly && m.SiegeEvent == null) continue;

                    Army a = m.Army;
                    if (a == null) continue;

                    MobileParty leaderParty = a.LeaderParty;
                    if (leaderParty == null) continue;
                    if (leaderParty.ActualClan != clan) continue;

                    if (!multipleArmySupply && siegeParties.Contains(leaderParty)) continue;

                    if (leaderParty.GetNumDaysForFoodToLast() > armyFoodLimit) continue;

                    Town closestTown = ClosestSupportTown(canSupport, leaderParty.Position2D);

                    if (closestTown == null)
                    {
                        if (isPlayerClan)
                        {
                            TextObject text = new TextObject("{=SL_MESSAGE_2}{SIEGE_PARTY} needs food but no fief was able to help. It's up to you now! (Days Left Until No Food: {DAYS_FOOD_LEFT})");
                            text.SetTextVariable("SIEGE_PARTY", leaderParty.Name);
                            text.SetTextVariable("DAYS_FOOD_LEFT", leaderParty.GetNumDaysForFoodToLast());
                            MessageManager.DisplayMessage(text, Colors.Yellow);
                        }

                        continue;
                    }

                    if (isPlayerClan)
                    {
                        TextObject text = new TextObject("{=SL_MESSAGE_3}{CLOSEST_TOWN} (Food Stocks = {FOOD_STOCKS}; Change = {FOOD_CHANGE}) is supporting {SIEGE_PARTY} (Days Left Until No Food: {DAYS_FOOD_LEFT})");
                        text.SetTextVariable("CLOSEST_TOWN", closestTown.Name);
                        text.SetTextVariable("FOOD_STOCKS", closestTown.FoodStocks.ToString("n0"));
                        text.SetTextVariable("FOOD_CHANGE", closestTown.FoodChange.ToString("n0"));
                        text.SetTextVariable("SIEGE_PARTY", leaderParty.Name);
                        text.SetTextVariable("DAYS_FOOD_LEFT", leaderParty.GetNumDaysForFoodToLast());
                        MessageManager.DisplayMessage(text);
                    }

                    MobileParty supplyParty = InitializeAISupplyParty(closestTown, null, leaderParty, isPlayerClan);
                    if (supplyParty == null) continue;

                    supplyParty.SetMoveGoToPoint(leaderParty.Position2D);
                }

                if (clan.Fiefs.Count < 2) return;

                if (needsSupport.Count == 0 || canSupport.Count == 0) return;

                foreach (Town nTown in needsSupport)
                {
                    if (destTowns.Contains(nTown)) continue;

                    Town closestTown = ClosestSupportTown(canSupport, nTown.Settlement.Position2D);

                    if (closestTown == null)
                    {
                        if (isPlayerClan)
                        {
                            TextObject text = new TextObject("{=SL_MESSAGE_4}{TOWN_NAME} needs food but no other fief was able to help. It's up to you now! (Current Stock = {FOOD_STOCKS}, Daily Change = {FOOD_CHANGE})");
                            text.SetTextVariable("TOWN_NAME", nTown.Name);
                            text.SetTextVariable("FOOD_STOCKS", nTown.FoodStocks.ToString("n0"));
                            text.SetTextVariable("FOOD_CHANGE", nTown.FoodChange.ToString("n0"));
                            MessageManager.DisplayMessage(text, Colors.Yellow);
                        }

                        continue;
                    }

                    MobileParty m = InitializeAISupplyParty(closestTown, nTown, null, isPlayerClan);
                    if (m == null) continue;

                    if (isPlayerClan)
                    {
                        TextObject text = new TextObject("{=SL_MESSAGE_5}{CLOSEST_TOWN} (Food Stocks = {FOOD_STOCKS}; Change = {FOOD_CHANGE}) is supporting {TOWN_NAME} (Food Stocks = {TOWN_FOOD_LEFT}; Change = {TOWN_FOOD_CHANGE})");
                        text.SetTextVariable("CLOSEST_TOWN", closestTown.Name);
                        text.SetTextVariable("FOOD_STOCKS", closestTown.FoodStocks.ToString("n0"));
                        text.SetTextVariable("FOOD_CHANGE", closestTown.FoodChange.ToString("n0"));
                        text.SetTextVariable("TOWN_NAME", nTown.Name);
                        text.SetTextVariable("TOWN_FOOD_LEFT", nTown.FoodStocks.ToString("n0"));
                        text.SetTextVariable("TOWN_FOOD_CHANGE", nTown.FoodChange.ToString("n0"));
                        MessageManager.DisplayMessage(text);
                    }

                    m.SetMoveGoToSettlement(nTown.Settlement);
                }
            });
        }

        private Town ClosestSupportTown(List<Town> townList, Vec2 supportPosition)
        {
            float shortestDistance = float.MaxValue;
            Town closestTown = null;

            for (int i = townList.Count - 1; i > -1; i--)
            {
                Town cTown = townList[i];

                float currentDistance = supportPosition.Distance(cTown.Settlement.Position2D);
                if (currentDistance < shortestDistance)
                {
                    shortestDistance = currentDistance;
                    closestTown = cTown;
                }
            }

            return closestTown;
        }

        private MobileParty InitializeAISupplyParty(Town closestTown, Town nTown, MobileParty leaderParty, bool isPlayerClan)
        {
            MobileParty m = CreateParty(closestTown, nTown, leaderParty);

            int totalTroops = 30;

            foreach (TroopRosterElement troop in closestTown.GarrisonParty.MemberRoster.GetTroopRoster())
            {
                CharacterObject c = troop.Character;
                int healthyTroops = troop.Number - troop.WoundedNumber;
                if (healthyTroops > 0)
                {
                    if (c.IsMounted)
                    {
                        if (!m.MemberRoster.Contains(c))
                        {
                            m.AddElementToMemberRoster(c, 0);
                        }

                        int add = Math.Min(totalTroops, healthyTroops);
                        int xp = troop.Xp;
                        m.MemberRoster.AddToCounts(c, add, xpChange: xp);
                        closestTown.GarrisonParty.MemberRoster.RemoveTroop(c, add, xp: xp);
                        totalTroops -= add;
                        if (totalTroops == 0) break;
                    }
                }
            }

            closestTown.GarrisonParty.MemberRoster.RemoveZeroCounts();

            if (totalTroops > 0)
            {
                foreach (TroopRosterElement troop in closestTown.GarrisonParty.MemberRoster.GetTroopRoster())
                {
                    CharacterObject c = troop.Character;
                    int healthyTroops = troop.Number - troop.WoundedNumber;
                    if (healthyTroops > 0)
                    {
                        if (!m.MemberRoster.Contains(c))
                        {
                            m.AddElementToMemberRoster(c, 0);
                        }

                        int add = Math.Min(totalTroops, healthyTroops);
                        int xp = troop.Xp;
                        m.MemberRoster.AddToCounts(c, add, xpChange: xp);
                        closestTown.GarrisonParty.MemberRoster.RemoveTroop(c, add, xp: xp);
                        totalTroops -= add;
                        if (totalTroops == 0) break;
                    }
                }

                closestTown.GarrisonParty.MemberRoster.RemoveZeroCounts();
            }

            if (totalTroops == 30)
            {
                m.RemoveParty();
                closestTown.FoodStocks += MySettings.Instance?.SUPPLY_AMOUNT ?? 50;
                return null;
            }

            if (isPlayerClan)
            {
                TextObject text = new TextObject("{=SL_MESSAGE_6}{CLOSEST_TOWN} food supply party headed out with {TROOP_COUNT} troops at a speed of {PARTY_SPEED}.");
                text.SetTextVariable("CLOSEST_TOWN", closestTown.Name);
                text.SetTextVariable("TROOP_COUNT", m.Party.NumberOfAllMembers);
                text.SetTextVariable("PARTY_SPEED", m.ComputeSpeed().ToString("n1"));
                MessageManager.DisplayMessage(text);
            }

            supplyParties.Add(m);
            destTowns.Add(nTown);
            originTowns.Add(closestTown);
            goingHomes.Add(false);
            returnAfterDeliveries.Add(true);
            siegeParties.Add(leaderParty);
            isSiegeSupply.Add(nTown == null);
            troopRosterCopies.Add(m.MemberRoster.GetTroopRoster().ToArray());//fix
            wasInBattles.Add(false);
            supplyAmounts.Add(MySettings.Instance?.SUPPLY_AMOUNT ?? 50);

            return m;
        }

        private void RemoveFromListsAtIndex(int i)
        {
            supplyParties.RemoveAt(i);
            destTowns.RemoveAt(i);
            originTowns.RemoveAt(i);
            goingHomes.RemoveAt(i);
            returnAfterDeliveries.RemoveAt(i);
            siegeParties.RemoveAt(i);
            isSiegeSupply.RemoveAt(i);
            troopRosterCopies.RemoveAt(i);
            wasInBattles.RemoveAt(i);
            supplyAmounts.RemoveAt(i);
        }

        private void ClearAllLists()
        {
            supplyParties.Clear();
            destTowns.Clear();
            originTowns.Clear();
            goingHomes.Clear();
            returnAfterDeliveries.Clear();
            siegeParties.Clear();
            isSiegeSupply.Clear();
            troopRosterCopies.Clear();
            wasInBattles.Clear();
            supplyAmounts.Clear();
        }

        private void FindLostParties()
        {
            foreach (MobileParty m in MobileParty.All)
            {
                string partyID = m.StringId;
                if (partyID.EndsWith("_SupplyLine") && !supplyParties.Contains(m))
                {
                    ParseId(m, out string originName, out string destName);
                    Settlement origin = Settlement.FindFirst(x => x.Town?.StringId == originName);
                    Settlement dest = Settlement.FindFirst(x => x.Town?.StringId == destName);
                    //MobileParty destParty = MobileParty.All.ElementAtOrDefault(MobileParty.All.FindIndex(x => x.StringId == destName && !siegeParties.Contains(x)));
                    MobileParty destParty = MobileParty.All.ElementAtOrDefault(MobileParty.All.FindIndex(x => x.StringId == destName));

                    supplyParties.Add(m);
                    troopRosterCopies.Add(m.MemberRoster.GetTroopRoster().ToArray());//fix
                    wasInBattles.Add(false);
                    originTowns.Add(origin.Town);
                    ItemRoster items = m.ItemRoster;
                    int grainOnHand = items.GetElementNumber(items.FindIndexOfElement(new EquipmentElement(DefaultItems.Grain)));
                    bool goingHome = grainOnHand <= 10;
                    goingHomes.Add(goingHome);
                    returnAfterDeliveries.Add(true);
                    supplyAmounts.Add(goingHome ? -1 : grainOnHand / 10 * 10);//should round down to nearest multiple of 10. ex 12 / 10 = 1 * 10 = 10

                    if (dest != null)
                    {
                        destTowns.Add(dest.Town);
                        siegeParties.Add(null);
                        isSiegeSupply.Add(false);
                    }
                    else
                    {
                        destTowns.Add(null);
                        siegeParties.Add(destParty);
                        isSiegeSupply.Add(true);
                    }

                    TextObject text = new TextObject("{=SL_MESSAGE_7}{PARTY_ID} put back on course to {DEST_NAME}.");
                    text.SetTextVariable("PARTY_ID", partyID);
                    text.SetTextVariable("DEST_NAME", goingHome ? origin?.Name?.ToString() ?? new TextObject("{=SL_NULL_ORIGIN}NULL ORIGIN").ToString() : dest?.Name?.ToString() ?? destParty?.Name?.ToString() ?? new TextObject("{=SL_NULL_DEST}NULL DESTINATION").ToString());
                    MessageManager.DisplayMessage(text);
                }
            }
        }

        private void ParseId(MobileParty m, out string origin, out string dest)
        {
            string partyID = m.StringId;
            int indexOfCastle2 = partyID.IndexOf("_castle_");
            int indexOfTown2 = partyID.IndexOf("_town_");
            int indexOfParty2 = partyID.IndexOf("_lord_");

            bool destIsCastle = indexOfCastle2 != -1, destIsTown = indexOfTown2 != -1;

            int start = 0;
            int end = destIsCastle ? indexOfCastle2 : destIsTown ? indexOfTown2 : indexOfParty2;
            origin = partyID.Substring(start, end);
            start = end + 1;
            end = partyID.IndexOf("_SupplyLine", start);
            dest = partyID.Substring(start, end - start);
        }

        protected void AddGameMenus(CampaignGameStarter campaignGameSystemStarter)
        {
            if (!ListsInSync())
            {
                TextObject text = new TextObject("{=SL_MESSAGE_1}Supply Lines Warning: Lists required resyncing! This might happen after updates.");
                MessageManager.DisplayMessage(text, Colors.Yellow);
                ClearAllLists();
                FindLostParties();
            }

            campaignGameSystemStarter.AddGameMenuOption("castle", "slcastle", "{=SL_MENU_1}Supply Lines", new GameMenuOption.OnConditionDelegate(MainOnCondition), new GameMenuOption.OnConsequenceDelegate(MainOnConsequence), false, 4, false);
            campaignGameSystemStarter.AddGameMenuOption("town", "sltown", "{=SL_MENU_1}Supply Lines", new GameMenuOption.OnConditionDelegate(MainOnCondition), new GameMenuOption.OnConsequenceDelegate(MainOnConsequence), false, 8, false);

            campaignGameSystemStarter.AddGameMenu("supply_lines_menu", "{=SL_MENU_2}Manage your supply lines.", new OnInitDelegate(MainOnInit), GameOverlays.MenuOverlayType.SettlementWithBoth);
            campaignGameSystemStarter.AddGameMenuOption("supply_lines_menu", "sldonate", "{=SL_MENU_14}Donate Food To Settlement", new GameMenuOption.OnConditionDelegate(DonateOnCondition), new GameMenuOption.OnConsequenceDelegate(DonateOnConsequence), false, 0, false);
            campaignGameSystemStarter.AddGameMenuOption("supply_lines_menu", "slreport", "{=SL_MENU_3}View Food Stocks", new GameMenuOption.OnConditionDelegate(ReportOnCondition), new GameMenuOption.OnConsequenceDelegate(ReportOnConsequence), false, 1, false);
            campaignGameSystemStarter.AddGameMenuOption("supply_lines_menu", "slsend", "{=SL_MENU_4}Send Support", new GameMenuOption.OnConditionDelegate(SendOnCondition), new GameMenuOption.OnConsequenceDelegate(SendOnConsequence), false, 2, false);
            campaignGameSystemStarter.AddGameMenuOption("supply_lines_menu", "slrequest", "{=SL_MENU_5}Request Support", new GameMenuOption.OnConditionDelegate(RequestOnCondition), new GameMenuOption.OnConsequenceDelegate(RequestOnConsequence), false, 3, false);
            campaignGameSystemStarter.AddGameMenuOption("supply_lines_menu", "slpurchase", "{=SL_MENU_13}Purchase Grain", new GameMenuOption.OnConditionDelegate(PurchaseOnCondition), new GameMenuOption.OnConsequenceDelegate(PurchaseOnConsequence), false, 4, false);
            campaignGameSystemStarter.AddGameMenuOption("supply_lines_menu", "slreturn", "{=SL_MENU_6}Escort Party Will Return", new GameMenuOption.OnConditionDelegate(ReturnOnCondition), new GameMenuOption.OnConsequenceDelegate(ReturnOnConsequence), false, 5, false);
            campaignGameSystemStarter.AddGameMenuOption("supply_lines_menu", "slstay", "{=SL_MENU_7}Escort Party Will Stay", new GameMenuOption.OnConditionDelegate(StayOnCondition), new GameMenuOption.OnConsequenceDelegate(StayOnConsequence), false, 6, false);
            campaignGameSystemStarter.AddGameMenuOption("supply_lines_menu", "slautoon", "{=SL_MENU_8}Automatic Supply Lines Enabled", new GameMenuOption.OnConditionDelegate(AutoOnCondition), new GameMenuOption.OnConsequenceDelegate(AutoOnConsequence), false, 7, false);
            campaignGameSystemStarter.AddGameMenuOption("supply_lines_menu", "slautooff", "{=SL_MENU_9}Automatic Supply Lines Disabled", new GameMenuOption.OnConditionDelegate(ManualOnCondition), new GameMenuOption.OnConsequenceDelegate(ManualOnConsequence), false, 8, false);
            campaignGameSystemStarter.AddGameMenuOption("supply_lines_menu", "slexit", "{=SL_MENU_10}Go Back", new GameMenuOption.OnConditionDelegate(ExitOnCondition), new GameMenuOption.OnConsequenceDelegate(ExitOnConsequence), false, 9, false);

            campaignGameSystemStarter.AddGameMenu("supply_lines_send_menu", "{=SL_MENU_11}Select a fief to send support to.", new OnInitDelegate(SendOnInit), GameOverlays.MenuOverlayType.SettlementWithBoth);
            campaignGameSystemStarter.AddGameMenu("supply_lines_request_menu", "{=SL_MENU_12}Select a fief to request support from.", new OnInitDelegate(RequestOnInit), GameOverlays.MenuOverlayType.SettlementWithBoth);
            campaignGameSystemStarter.AddGameMenu("supply_lines_purchase_menu", "{=SL_MENU_12}Select a fief to request support from.", new OnInitDelegate(RequestOnInit), GameOverlays.MenuOverlayType.SettlementWithBoth);
            int i = -1;
            foreach (Town t in Town.AllFiefs)
            {
                i++;
                string townName = t.Name.ToString();
                campaignGameSystemStarter.AddGameMenuOption("supply_lines_send_menu", "sl_s_" + t.StringId, townName,
                    (args) =>
                    {
                        args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                        if (t != Settlement.CurrentSettlement.Town && t.OwnerClan == Clan.PlayerClan)
                        {
                            args.IsEnabled = !t.IsUnderSiege;
                            return true;
                        }

                        return false;
                    },
                    (args) =>
                    {
                        Settlement s = Settlement.CurrentSettlement;
                        TextObject text = new TextObject("{=SL_MESSAGE_8}{CURRENT_TOWN_NAME} is sending support to {TOWN_NAME}.");
                        text.SetTextVariable("CURRENT_TOWN_NAME", s.Name);
                        text.SetTextVariable("TOWN_NAME", townName);
                        MessageManager.DisplayMessage(text, Colors.Cyan);
                        Town origin = s.Town;
                        MobileParty m = CreateParty(origin, t);
                        SelectTroops(m, origin.GarrisonParty);
                        if (m.IsActive)
                        {
                            m.SetMoveGoToSettlement(t.Settlement);
                            supplyParties.Add(m);
                            troopRosterCopies.Add(m.MemberRoster.GetTroopRoster().ToArray());
                            wasInBattles.Add(false);
                            destTowns.Add(t);
                            originTowns.Add(origin);
                            goingHomes.Add(false);
                            returnAfterDeliveries.Add(returnAfterDelivery);
                            siegeParties.Add(null);
                            isSiegeSupply.Add(false);
                            supplyAmounts.Add(MySettings.Instance?.SUPPLY_AMOUNT ?? 50);
                        }
                        GameMenu.SwitchToMenu("supply_lines_menu");
                    },
                    false, i, false);
                campaignGameSystemStarter.AddGameMenuOption("supply_lines_request_menu", "sl_r_" + t.StringId, townName,
                    (args) =>
                    {
                        args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                        if (t != Settlement.CurrentSettlement.Town && t.OwnerClan == Clan.PlayerClan)
                        {
                            args.IsEnabled = !t.IsUnderSiege && t.FoodStocks > (MySettings.Instance?.SUPPLY_AMOUNT ?? 50);
                            return true;
                        }

                        return false;
                    },
                    (args) =>
                    {
                        Settlement s = Settlement.CurrentSettlement;
                        Town dest = s.Town;
                        TextObject text = new TextObject("{=SL_MESSAGE_9}{CURRENT_TOWN_NAME} is requesting support from {TOWN_NAME}.");
                        text.SetTextVariable("CURRENT_TOWN_NAME", s.Name);
                        text.SetTextVariable("TOWN_NAME", townName);
                        MessageManager.DisplayMessage(text, Colors.Cyan);
                        MobileParty m = CreateParty(t, dest);
                        SelectTroops(m, t.GarrisonParty);
                        if (m.IsActive)
                        {
                            m.SetMoveGoToSettlement(s);
                            supplyParties.Add(m);
                            troopRosterCopies.Add(m.MemberRoster.GetTroopRoster().ToArray());
                            wasInBattles.Add(false);
                            destTowns.Add(dest);
                            originTowns.Add(t);
                            goingHomes.Add(false);
                            returnAfterDeliveries.Add(returnAfterDelivery);
                            siegeParties.Add(null);
                            isSiegeSupply.Add(false);
                            supplyAmounts.Add(MySettings.Instance?.SUPPLY_AMOUNT ?? 50);
                        }
                        GameMenu.SwitchToMenu("supply_lines_menu");
                    },
                    false, i, false);
                campaignGameSystemStarter.AddGameMenuOption("supply_lines_purchase_menu", "sl_r_" + t.StringId, string.Format("{0} ({1})", townName, t.MapFaction.Name),
                    (args) =>
                    {
                        args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                        Clan playerClan = Clan.PlayerClan;
                        IFaction playerFaction = playerClan.MapFaction;
                        Settlement currentSettlement = Settlement.CurrentSettlement;
                        Settlement purchaseSettlement = t.Settlement;
                        IFaction purchaseFaction = t.MapFaction;
                        if (purchaseSettlement != currentSettlement && t.OwnerClan != playerClan && (playerFaction == purchaseFaction || !purchaseFaction.IsAtWarWith(playerFaction)))
                        {
                            args.IsEnabled = !t.IsUnderSiege && t.FoodStocks >= 0.5f * t.FoodStocksUpperLimit() && t.FoodChange > 0f && Hero.MainHero.Gold >= (int)purchaseSettlement.Position2D.Distance(currentSettlement.Position2D) * 20 + (MySettings.Instance?.SUPPLY_AMOUNT ?? 50) * t.MarketData.GetPrice(DefaultItems.Grain, isSelling: true);
                            return true;
                        }

                        return false;
                    },
                    (args) =>
                    {
                        Settlement s = Settlement.CurrentSettlement;
                        Town dest = s.Town;
                        Settlement purchaseSettlement = t.Settlement;

                        TextObject text = new TextObject("{=SL_MESSAGE_9}{CURRENT_TOWN_NAME} is requesting support from {TOWN_NAME}.");
                        text.SetTextVariable("CURRENT_TOWN_NAME", s.Name);
                        text.SetTextVariable("TOWN_NAME", townName);
                        MessageManager.DisplayMessage(text, Colors.Cyan);

                        int cost = (int)purchaseSettlement.Position2D.Distance(s.Position2D) * 20 + (MySettings.Instance?.SUPPLY_AMOUNT ?? 50) * t.MarketData.GetPrice(DefaultItems.Grain, isSelling: true);
                        text = new TextObject("{=SL_MESSAGE_19}You were charged {AMOUNT} gold for this delivery.");
                        text.SetTextVariable("AMOUNT", cost);
                        MessageManager.DisplayMessage(text, Colors.Cyan);

                        t.OwnerClan.Leader.ChangeHeroGold(cost);
                        Hero.MainHero.ChangeHeroGold(-cost);

                        MobileParty m = InitializeAISupplyParty(t, dest, null, true);

                        if (m.IsActive)
                        {
                            m.SetMoveGoToSettlement(s);
                        }
                        GameMenu.SwitchToMenu("supply_lines_menu");
                    },
                    false, i, false);
            }
            i++;
            campaignGameSystemStarter.AddGameMenuOption("supply_lines_send_menu", "slsexit", "{=SL_MENU_10}Go Back", new GameMenuOption.OnConditionDelegate(SendExitOnCondition), new GameMenuOption.OnConsequenceDelegate(SendExitOnConsequence), false, i, false);
            campaignGameSystemStarter.AddGameMenuOption("supply_lines_request_menu", "slrexit", "{=SL_MENU_10}Go Back", new GameMenuOption.OnConditionDelegate(RequestExitOnCondition), new GameMenuOption.OnConsequenceDelegate(RequestExitOnConsequence), false, i, false);
            campaignGameSystemStarter.AddGameMenuOption("supply_lines_purchase_menu", "slpexit", "{=SL_MENU_10}Go Back", new GameMenuOption.OnConditionDelegate(PurchaseExitOnCondition), new GameMenuOption.OnConsequenceDelegate(PurchaseExitOnConsequence), false, i, false);

            campaignGameSystemStarter.AddPlayerLine("order_supply_party", "hero_main_options", "order_supply_party_resp", "{=SL_Dialog_1}I need you to send out a supply party to my location at ...",
                () =>
                {
                    Hero h = Hero.OneToOneConversationHero;
                    Town t = h.GovernorOf;
                    return t != null && t.OwnerClan == Clan.PlayerClan;
                },
                null);
            campaignGameSystemStarter.AddDialogLine("order_supply_party_resp", "order_supply_party_resp", "lord_pretalk", "{=SL_Dialog_2}I will send out the supply party right away.",
                () =>
                {
                    Settlement s = Hero.OneToOneConversationHero.CurrentSettlement;
                    int index = originTowns.IndexOf(s.Town);
                    return (index == -1 || siegeParties[index] != MobileParty.MainParty) && Hero.MainHero.CurrentSettlement != s && s.Town.FoodStocks >= (MySettings.Instance?.SUPPLY_AMOUNT ?? 50);
                },
                () =>
                {
                    MobileParty supplyParty = InitializeAISupplyParty(Hero.OneToOneConversationHero.CurrentSettlement.Town, null, MobileParty.MainParty, true);
                    if (supplyParty == null)
                    {
                        MessageManager.DisplayMessage(new TextObject("{=SL_MESSAGE_14}Supply Lines Error: Supply Party Not Created"));
                        return;
                    }

                    supplyParty.SetMoveGoToPoint(MobileParty.MainParty.Position2D);
                });
            campaignGameSystemStarter.AddDialogLine("order_supply_party_resp", "order_supply_party_resp", "lord_pretalk", "{=SL_Dialog_3}I'll have the men at the granary load up some grain before your departure.",
                () =>
                {
                    Settlement s = Hero.OneToOneConversationHero.CurrentSettlement;
                    return Hero.MainHero.CurrentSettlement == s && s.Town.FoodStocks >= (MySettings.Instance?.SUPPLY_AMOUNT ?? 50);
                },
                () =>
                {
                    Hero.OneToOneConversationHero.CurrentSettlement.Town.FoodStocks -= (MySettings.Instance?.SUPPLY_AMOUNT ?? 50);
                    ItemRoster items = MobileParty.MainParty.ItemRoster;
                    if (items.Contains(new ItemRosterElement(DefaultItems.Grain)))
                    {
                        items.AddToCounts(DefaultItems.Grain, (MySettings.Instance?.SUPPLY_AMOUNT ?? 50));
                    }
                    else
                    {
                        items.Add(new ItemRosterElement(DefaultItems.Grain, (MySettings.Instance?.SUPPLY_AMOUNT ?? 50)));
                    }
                });
            campaignGameSystemStarter.AddDialogLine("order_supply_party_resp", "order_supply_party_resp", "lord_pretalk", "{=SL_Dialog_4}We don't have enough food to send you anything right now. The people would riot and have my head.",
                () =>
                {
                    Settlement s = Hero.OneToOneConversationHero.CurrentSettlement;
                    return s.Town.FoodStocks < (MySettings.Instance?.SUPPLY_AMOUNT ?? 50);
                },
                null);
            campaignGameSystemStarter.AddDialogLine("order_supply_party_resp", "order_supply_party_resp", "lord_pretalk", "{=SL_Dialog_5}Your supplies are on their way.",
                () =>
                {
                    Settlement s = Hero.OneToOneConversationHero.CurrentSettlement;
                    int index = originTowns.IndexOf(s.Town);
                    return index != -1 && siegeParties[index] == MobileParty.MainParty;
                },
                null);
        }

        private bool DonateOnCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.ShowMercy;
            foreach (ItemRosterElement ire in MobileParty.MainParty.ItemRoster)
            {
                if (ire.IsEqualTo(ItemRosterElement.Invalid)) continue;
                if (ire.IsEmpty) continue;

                if (ire.EquipmentElement.Item.IsFood)
                {
                    args.IsEnabled = true;
                    return true;
                }
            }
            args.IsEnabled = false;
            return true;
        }

        private void DonateOnConsequence(MenuCallbackArgs args)
        {
            int donateAmount = 0;
            int maxAmount = MySettings.Instance?.DonationAmount ?? 20;
            List<EquipmentElement> itemsAdjusted = new List<EquipmentElement>();
            List<int> amounts = new List<int>();
            foreach (ItemRosterElement ire in MobileParty.MainParty.ItemRoster)
            {
                if (ire.IsEqualTo(ItemRosterElement.Invalid)) continue;
                if (ire.IsEmpty) continue;

                EquipmentElement ee = ire.EquipmentElement;
                if (ee.Item.IsFood)
                {
                    int currentDonation = Math.Min(ire.Amount, maxAmount - donateAmount);
                    amounts.Add(currentDonation);
                    itemsAdjusted.Add(ee);
                    donateAmount += currentDonation;
                }

                if (donateAmount == maxAmount) break;
            }

            for (int i = 0; i < itemsAdjusted.Count; i++)
            {
                EquipmentElement item = itemsAdjusted[i];
                MobileParty.MainParty.ItemRoster.AddToCounts(item, -amounts[i]);
            }
            Settlement.CurrentSettlement.Town.FoodStocks += donateAmount;
            TextObject text = new TextObject("{=SL_MESSAGE_20}You donate {DONATION_AMOUNT} food items to the settlement food stocks.");
            text.SetTextVariable("DONATION_AMOUNT", donateAmount);
            MessageManager.DisplayMessage(text);
        }

        private bool AutoOnCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Mission;
            return enableAIForPlayer;
        }

        private void AutoOnConsequence(MenuCallbackArgs args)
        {
            enableAIForPlayer = false;
            MessageManager.DisplayMessage(new TextObject("{=SL_MESSAGE_10}Player supply lines will now require player to manually send and request shipments."));
            GameMenu.SwitchToMenu("supply_lines_menu");
        }

        private bool ManualOnCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Manage;
            return !enableAIForPlayer;
        }

        private void ManualOnConsequence(MenuCallbackArgs args)
        {
            enableAIForPlayer = true;
            MessageManager.DisplayMessage(new TextObject("{=SL_MESSAGE_11}Player supply lines will now be handled by the AI. Parties will always return home after making the delivery. The AI will prioritise selecting mounted troops when it forms supply parties. You should keep at least 30 cavalry in your garrisons that maintain food surpluses so the AI can form the quickest and most reliable party."));
            GameMenu.SwitchToMenu("supply_lines_menu");
        }

        private bool ReturnOnCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Continue;
            args.Tooltip = new TextObject("{=SL_TOOLTIP_1}This setting only applies to shipments manually sent by the player.");
            return returnAfterDelivery;
        }

        private void ReturnOnConsequence(MenuCallbackArgs args)
        {
            returnAfterDelivery = false;
            MessageManager.DisplayMessage(new TextObject("{=SL_MESSAGE_12}Supply transfer parties will stay at their destination after delivery."));
            GameMenu.SwitchToMenu("supply_lines_menu");
        }

        private bool StayOnCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Wait;
            args.Tooltip = new TextObject("{=SL_TOOLTIP_1}This setting only applies to shipments manually sent by the player.");
            return !returnAfterDelivery;
        }

        private void StayOnConsequence(MenuCallbackArgs args)
        {
            returnAfterDelivery = true;
            MessageManager.DisplayMessage(new TextObject("{=SL_MESSAGE_13}Supply transfer parties will return home after delivery."));
            GameMenu.SwitchToMenu("supply_lines_menu");
        }

        private void ReportOnConsequence(MenuCallbackArgs args)
        {
            StringBuilder stringBuilder = new StringBuilder();

            IEnumerable<Town> fiefs = Clan.PlayerClan.Fiefs;
            int count = fiefs.Count(), lastIndex = count - 1;
            for (int i = 0; i < count; i++)
            {
                Town t = fiefs.ElementAt(i);
                float foodChange = t.FoodChange;
                TextObject text = new TextObject("{=SL_REPORT_LINE}{TOWN_NAME}: {FOOD_STOCKS} food in stock. {CHANGE_DESC}{NEW_LINE}");
                text.SetTextVariable("TOWN_NAME", t.Name);
                text.SetTextVariable("FOOD_STOCKS", t.FoodStocks.ToString("n0"));

                if (foodChange > 0)
                {
                    TextObject text2 = new TextObject("{=SL_SURPLUS_MSG}Surplus of {AMOUNT} daily.");
                    text2.SetTextVariable("AMOUNT", Math.Abs(t.FoodChange).ToString("n0"));
                    text.SetTextVariable("CHANGE_DESC", text2);
                }
                else if (foodChange < 0)
                {
                    TextObject text3 = new TextObject("{=SL_DEFECIT_MSG}Defecit of {AMOUNT} daily.");
                    text3.SetTextVariable("AMOUNT", Math.Abs(t.FoodChange).ToString("n0"));
                    text.SetTextVariable("CHANGE_DESC", text3);
                }
                else
                {
                    text.SetTextVariable("CHANGE_DESC", new TextObject("{=SL_BALANCED_MSG}Balanced."));
                }

                text.SetTextVariable("NEW_LINE", i == lastIndex ? "" : "\n");
                stringBuilder.Append(text.ToString());
            }
            InformationManager.ShowInquiry(new InquiryData(new TextObject("{=SL_REPORT_TITLE}Food Stocks").ToString(), stringBuilder.ToString(), true, false, new TextObject("{=SL_REPORT_OK}OK").ToString(), null, null, null), true);
        }

        private bool ReportOnCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            return true;
        }

        private MobileParty CreateParty(Town origin, Town destination, MobileParty leaderParty = null)
        {
            BindingFlags nonPublicInstanceFlags = BindingFlags.NonPublic | BindingFlags.Instance;
            bool meesajarjarbinks = destination == null;
            PartyTemplateObject defaultPartyTemplate = origin.Culture.CaravanPartyTemplate;//.DefaultPartyTemplate;
            MobileParty mobileParty = MBObjectManager.Instance.CreateObject<MobileParty>(origin.StringId + '_' + (meesajarjarbinks ? leaderParty.StringId : destination.StringId) + "_SupplyLine");
            mobileParty.PartyComponent = (CaravanPartyComponent)typeof(CaravanPartyComponent).GetConstructor(nonPublicInstanceFlags, null, new Type[] { typeof(Settlement), typeof(Hero) }, null).Invoke(new object[] { origin.Settlement, origin.Owner.Owner });
            mobileParty.InitializeMobileParty(defaultPartyTemplate, origin.Settlement.GatePosition, 0.0f, troopNumberLimit: 0);
            typeof(PartyComponent).GetMethod("SetMobilePartyInternal", nonPublicInstanceFlags).Invoke(mobileParty.PartyComponent, new object[] { mobileParty });
            mobileParty.Party.Visuals.SetMapIconAsDirty();

            TextObject text;
            if (MySettings.Instance?.DisplayRoutes ?? false)
            {
                text = new TextObject("{=SL_PARTY_NAME}Food Supply: {FROM_NAME} to {TO_NAME}");
                text.SetTextVariable("FROM_NAME", origin.Name);
                text.SetTextVariable("TO_NAME", meesajarjarbinks ? leaderParty.Name : destination.Name);
            }
            else
            {
                text = new TextObject("{=SL_PARTY_NAME_2}Food Supply");
            }
            mobileParty.SetCustomName(text);
            //mobileParty.IsCaravan = true;
            //mobileParty.IsMilitia = false;
            mobileParty.SetCustomHomeSettlement(origin.Settlement);
            mobileParty.Party.SetCustomOwner(origin.Owner.Owner);
            mobileParty.SetInititave(0f, 1f, float.MaxValue);

            mobileParty.ShouldJoinPlayerBattles = false;
            mobileParty.Aggressiveness = 0f;
            int SUPPLY_AMOUNT = MySettings.Instance?.SUPPLY_AMOUNT ?? 50;
            origin.FoodStocks -= SUPPLY_AMOUNT;
            ItemRoster itemRoster = mobileParty.ItemRoster;
            itemRoster.Add(new ItemRosterElement(DefaultItems.Grain, SUPPLY_AMOUNT, null));
            float weightAvailable = mobileParty.InventoryCapacity - itemRoster.TotalWeight;
            if (weightAvailable > 0f)
            {
                int addGrain = (int)(weightAvailable / 10f);
                itemRoster.Add(new ItemRosterElement(DefaultItems.Grain, Math.Min(10, addGrain), null));
            }
            mobileParty.Ai.SetDoNotMakeNewDecisions(true);

            typeof(CampaignObjectManager).GetMethod("AddMobileParty", nonPublicInstanceFlags).Invoke(Campaign.Current.CampaignObjectManager, new object[] { mobileParty });
            CampaignEventDispatcher.Instance.OnMobilePartyCreated(mobileParty);

            return mobileParty;
        }

        private void SelectTroops(MobileParty supplyParty, MobileParty garrisonParty)
        {
            PartyScreenLogic partyScreenLogic = new PartyScreenLogic();
            Dictionary<TroopRosterElement, int> listOfTroopsWithInitialExp = new Dictionary<TroopRosterElement, int>();
            for (int index = 0; index < garrisonParty.MemberRoster.Count; ++index)
            {
                listOfTroopsWithInitialExp.Add(garrisonParty.MemberRoster.GetElementCopyAtIndex(index), garrisonParty.MemberRoster.GetElementXp(index));
                garrisonParty.MemberRoster.SetElementXp(index, 0);
            }
            partyScreenLogic.Initialize(supplyParty.Party, garrisonParty, supplyParty.Name, (leftMemberRoster, leftPrisonRoster, rightMemberRoster, rightPrisonRoster, e, f, isForced, leftParties, rigthParties) =>
            {
                if (listOfTroopsWithInitialExp.Count > 0)
                {
                    foreach (TroopRosterElement key in garrisonParty.MemberRoster.GetTroopRoster())
                    {
                        int indexOfTroop = garrisonParty.MemberRoster.FindIndexOfTroop(key.Character);
                        if (indexOfTroop > 0)
                        {
                            int number = listOfTroopsWithInitialExp[key];
                            garrisonParty.MemberRoster.SetElementXp(indexOfTroop, number);
                        }
                    }
                }

                FinalizeShipment(supplyParty, garrisonParty);
                return true;
            });
            partyScreenLogic.InitializeTrade(PartyScreenLogic.TransferState.Transferable, PartyScreenLogic.TransferState.NotTransferable, PartyScreenLogic.TransferState.NotTransferable);

            partyScreenLogic.SetCancelActivateHandler(() =>
            {
                if (listOfTroopsWithInitialExp.Count > 0)
                {
                    foreach (TroopRosterElement key in garrisonParty.MemberRoster.GetTroopRoster())
                    {
                        int indexOfTroop = garrisonParty.MemberRoster.FindIndexOfTroop(key.Character);
                        if (indexOfTroop >= 0)
                        {
                            int number = listOfTroopsWithInitialExp[key];
                            garrisonParty.MemberRoster.SetElementXp(indexOfTroop, number);
                        }
                    }
                }

                FinalizeShipment(supplyParty, garrisonParty);
                return true;
            });

            partyScreenLogic.SetDoneConditionHandler((leftMemberRoster, leftPrisonRoster, rightMemberRoster, rightPrisonRoster, leftLimitNum, rightLimitNum) => new Tuple<bool, TextObject>(true, new TextObject("")));
            partyScreenLogic.SetTroopTransferableDelegate(new PartyScreenLogic.IsTroopTransferableDelegate(PartyScreenManager.TroopTransferableDelegate));
            PartyScreenManager.Instance.GetType().GetField("_partyScreenLogic", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(PartyScreenManager.Instance, partyScreenLogic);
            PartyScreenManager.Instance.GetType().GetField("_currentMode", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(PartyScreenManager.Instance, PartyScreenMode.TroopsManage);
            PartyState state = Game.Current.GameStateManager.CreateState<PartyState>();
            state.InitializeLogic(partyScreenLogic);
            Game.Current.GameStateManager.PushState(state);
        }

        private void FinalizeShipment(MobileParty supplyParty, MobileParty garrisonParty)
        {
            if (supplyParty.Party.NumberOfAllMembers <= 0)
            {
                supplyParty.RemoveParty();
                garrisonParty.CurrentSettlement.Town.FoodStocks += MySettings.Instance?.SUPPLY_AMOUNT ?? 50;
            }
        }

        private void MainOnInit(MenuCallbackArgs args)
        {

        }

        private void SendOnInit(MenuCallbackArgs args)
        {

        }

        private void RequestOnInit(MenuCallbackArgs args)
        {

        }

        private static bool MainOnCondition(MenuCallbackArgs args)
        {
            if (Settlement.CurrentSettlement.OwnerClan != Hero.MainHero.Clan)
                return false;

            args.Tooltip = new TextObject("{=SL_TOOLTIP_2}Create food supply shipments to support your fiefs in need. Requires troops from your garrison.");
            args.optionLeaveType = GameMenuOption.LeaveType.Trade;
            args.IsEnabled = true;
            return true;
        }

        private void MainOnConsequence(MenuCallbackArgs args)
        {
            GameMenu.SwitchToMenu("supply_lines_menu");
        }

        private bool SendOnCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Manage;
            return Settlement.CurrentSettlement.Town.FoodStocks > (MySettings.Instance?.SUPPLY_AMOUNT ?? 50);
        }

        private void SendOnConsequence(MenuCallbackArgs args)
        {
            GameMenu.SwitchToMenu("supply_lines_send_menu");
        }

        private bool RequestOnCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
            return Hero.MainHero.Clan.Fiefs.Count > 1;
        }

        private void RequestOnConsequence(MenuCallbackArgs args)
        {
            GameMenu.SwitchToMenu("supply_lines_request_menu");
        }

        private bool PurchaseOnCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
            args.Tooltip = new TextObject("{=SL_TOOLTIP_3}You will be charged 10 gold per unit of distance both ways for the delivery plus the cost of the grain. It's that or starve!");
            return true;
        }

        private void PurchaseOnConsequence(MenuCallbackArgs args)
        {
            GameMenu.SwitchToMenu("supply_lines_purchase_menu");
        }

        private bool ExitOnCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return true;
        }

        private void ExitOnConsequence(MenuCallbackArgs args)
        {
            if (Settlement.CurrentSettlement.IsCastle) GameMenu.SwitchToMenu("castle");
            else GameMenu.SwitchToMenu("town");
        }

        private bool RequestExitOnCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return true;
        }

        private void RequestExitOnConsequence(MenuCallbackArgs args)
        {
            GameMenu.SwitchToMenu("supply_lines_menu");
        }

        private bool PurchaseExitOnCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return true;
        }

        private void PurchaseExitOnConsequence(MenuCallbackArgs args)
        {
            GameMenu.SwitchToMenu("supply_lines_menu");
        }

        private bool SendExitOnCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return true;
        }

        private void SendExitOnConsequence(MenuCallbackArgs args)
        {
            GameMenu.SwitchToMenu("supply_lines_menu");
        }

        public override void SyncData(IDataStore dataStore)
        {
            try
            {
                dataStore.SyncData("supplyParties", ref supplyParties);
                dataStore.SyncData("destTowns", ref destTowns);
                dataStore.SyncData("originTowns", ref originTowns);
                dataStore.SyncData("goingHomes", ref goingHomes);
                dataStore.SyncData("returnAfterDelivery", ref returnAfterDelivery);
                dataStore.SyncData("enableAIForPlayer", ref enableAIForPlayer);
                dataStore.SyncData("returnAfterDeliveries", ref returnAfterDeliveries);
                dataStore.SyncData("siegeParties", ref siegeParties);
                dataStore.SyncData("isSiegeSupply", ref isSiegeSupply);
                dataStore.SyncData("currentRoutesSetting", ref currentRoutesSetting);
                dataStore.SyncData("troopRosterCopies", ref troopRosterCopies);
                dataStore.SyncData("wasInBattles", ref wasInBattles);
                dataStore.SyncData("supplyAmounts", ref supplyAmounts);
            }
            catch (Exception)
            {

            }
        }
    }

    public class MySaveDefiner : SaveableTypeDefiner
    {
        public MySaveDefiner() : base((0x913847 << 8) | 123)
        {

        }

        protected override void DefineContainerDefinitions()
        {
            ConstructContainerDefinition(typeof(List<TroopRosterElement[]>));
        }
    }
    
}
