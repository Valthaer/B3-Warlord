using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Warlord
{
	[HarmonyPatch(typeof(MobileParty), "FillPartyStacks")]
	public class SpawnLordPartyInternalPatch
	{
		private static void Prefix(MobileParty __instance, PartyTemplateObject pt, ref int troopNumberLimit)
		{
			bool isLordParty = __instance.IsLordParty;
			if (isLordParty)
			{
#if DEBUG
				MessageManager.DisplayDebugMessage($"Lord Army going to respawn with troop number = {troopNumberLimit}");
#endif
				troopNumberLimit = 1;
#if DEBUG
				MessageManager.DisplayDebugMessage($"Lord Army respawned with troop number = {troopNumberLimit}");				
#endif
			}
		}
	}
}
