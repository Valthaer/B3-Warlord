using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine.Screens;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v1;
using MCM.Abstractions.Settings.Base.Global;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
using TaleWorlds.CampaignSystem.SandBox.GameComponents.Party;
using System.Reflection;
using ModLib;

namespace Warlord
{
    public class Main : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            try
            {
                Harmony harmony = new Harmony("Warlord");
                
                //Method retriever
                //var method1 = typeof(MobileParty).GetMethod("FillPartyStacks", BindingFlags.NonPublic | BindingFlags.Instance);
                //var method2 = typeof(SpawnLordParty).GetMethod(nameof(SpawnLordParty.FillPartyStacksPrefix));

                harmony.Patch(typeof(MobileParty).GetMethod("FillPartyStacks", BindingFlags.NonPublic | BindingFlags.Instance),
                    new HarmonyMethod(typeof(SpawnLordParty).GetMethod(nameof(SpawnLordParty.FillPartyStacksPrefix))));
                harmony.Patch(typeof(HeroSpawnCampaignBehavior).GetMethod("CalculateScoreToCreateParty", BindingFlags.NonPublic | BindingFlags.Instance),
                    new HarmonyMethod(typeof(SpawnLordParty).GetMethod(nameof(SpawnLordParty.CalculateScoreToCreatePartyPrefix))));
                harmony.Patch(typeof(HeroSpawnCampaignBehavior).GetMethod("GetHeroPartyCommandScore", BindingFlags.NonPublic | BindingFlags.Instance),
                    new HarmonyMethod(typeof(SpawnLordParty).GetMethod(nameof(SpawnLordParty.GetHeroPartyCommandScorePrefix))));
                

            }
            catch (Exception e)
            {
                MessageBox.Show("Couldn't apply Harmony due to: " + e.ToString());
            }
        }

        public override void OnGameLoaded(Game game, object initializerObject)
        {
            try
            {
                RenownGainedEventListener renownGainedEventListener = new RenownGainedEventListener();
                CampaignEvents.RenownGained.AddNonSerializedListener(renownGainedEventListener, new Action<Hero, int, bool>(renownGainedEventListener.RenownGainedListener));
                CampaignEvents.TournamentFinished.AddNonSerializedListener(renownGainedEventListener, new Action<CharacterObject, MBReadOnlyList<CharacterObject>, Town, ItemObject>(renownGainedEventListener.PleaseTheCrowd));
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage("An error has occurred during OnGameLoaded for BattleBuddies: " + ex.Message.ToString(), Color.FromUint(4282569842U)));
            }
        }
        
        protected override void OnGameStart(Game game, IGameStarter starterObject)
        {
            base.OnGameStart(game, starterObject);
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            try
            {
                MessageManager.DisplayMessage("Loaded Warlord 0.2.0", Colors.Green);
                //ModLib.FileDatabase.Initialise("Warlord");
                new WarlordModuleSettings();
                //this.ThrowWarningIfGameErrorDoesntMatchModVersion();
            }
            catch (Exception ex)
            {
                MessageManager.DisplayMessage("Could not Initialise ModLib for Warlord: " + ex.Message.ToString(),ModuleColors.uiColorWarning);
            }            
        }
    }
}
