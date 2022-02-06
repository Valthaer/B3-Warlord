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

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            MessageManager.DisplayMessage("Loaded Warlord 0.1.3",Colors.Green);
            //this.ThrowWarningIfGameErrorDoesntMatchModVersion();
        }
    }
}
