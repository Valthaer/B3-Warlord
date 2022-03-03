using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Warlord
{
	public class SpawnLordParty
	{
		public static void FillPartyStacksPrefix(MobileParty __instance, PartyTemplateObject pt, ref int troopNumberLimit)
		{
			bool isLordParty = __instance.IsLordParty;
			if (isLordParty && WarlordModuleSettings.Instance.LordPartySpawnSizeReducedEnabled)
			{
#if DEBUG
				//MessageManager.DisplayDebugMessage($"Lord {__instance.LeaderHero.Name} Army of the {__instance.LeaderHero.Clan.Name} going to respawn with troop number = {troopNumberLimit}");
#endif
				troopNumberLimit = 1;
#if DEBUG
				//MessageManager.DisplayDebugMessage($"Lord {__instance.LeaderHero.Name} Army of the {__instance.LeaderHero.Clan.Name} respawned with troop number = {troopNumberLimit}");				
#endif
			}
		}

		public static bool CalculateScoreToCreatePartyPrefix(ref float __result, Clan clan) // See also ConsiderSpawningLordParties
		{
            if (!WarlordModuleSettings.Instance.LordPartySpawnSizeReducedEnabled)
            {
				return false;
            }

			float clanScoreToCreateParty = (float)(clan.Fiefs.Count * 100 - clan.WarPartyComponents.Count<WarPartyComponent>() * 100) + (float)clan.Gold * 0.01f + (clan.IsMinorFaction ? 200f : 0f) + (clan.WarPartyComponents.Any<WarPartyComponent>() ? 0f : 200f);
#if DEBUG
			//MessageManager.DisplayDebugMessage($"Clan {clan.Name} score to create party: {clanScoreToCreateParty}");
#endif
			if (clanScoreToCreateParty > 100)
            {
				Random rnd = new Random();
				int rndResult = rnd.Next(1, 5); //20% of original rate
                if (rndResult != 1)
                {
					clanScoreToCreateParty = -2000;			
				}
#if DEBUG
				//MessageManager.DisplayDebugMessage($"Clan {clan.Name} FINAL score to create party: {clanScoreToCreateParty} with rndResult: {rndResult}");
#endif				
			}
			__result = clanScoreToCreateParty;
			return false; // make sure you only skip if really necessary (we are skiping to run the original method)
		}

		public static bool GetHeroPartyCommandScorePrefix(ref float __result,Hero hero)
		{
			float heroScore = 3f * (float)hero.GetSkillValue(DefaultSkills.Tactics) + 2f * (float)hero.GetSkillValue(DefaultSkills.Leadership) + (float)hero.GetSkillValue(DefaultSkills.Scouting) + (float)hero.GetSkillValue(DefaultSkills.Steward) + (float)hero.GetSkillValue(DefaultSkills.OneHanded) + (float)hero.GetSkillValue(DefaultSkills.TwoHanded) + (float)hero.GetSkillValue(DefaultSkills.Polearm) + (float)hero.GetSkillValue(DefaultSkills.Riding) + ((hero.Clan.Leader == hero) ? 1000f : 0f);

#if DEBUG
			//MessageManager.DisplayDebugMessage($"Hero {hero.Name} of {hero.Clan.Name} command score to create party: {heroScore}");
#endif
			__result = heroScore;
			return false; // make sure you only skip if really necessary (we are skiping to run the original method)
		}
	}
}
