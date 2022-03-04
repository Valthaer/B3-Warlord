using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Warlord
{
	public class RelationShipListeners
	{
		public void PleaseTheCrowd(CharacterObject winner, MBReadOnlyList<CharacterObject> compeditors, Town town, ItemObject item)
		{
			if (!winner.IsPlayerCharacter || !WarlordModuleSettings.Instance.RelationByEventsEnabled)
			{
				return;
			}
			MessageManager.DisplayDebugMessage("Relationship increased after tournament");

			foreach (Hero hero in (from s in town.Workshops select s.Owner).Distinct<Hero>())
			{
				RelationshipHelper.IncreasePlayerRelation(winner, hero, ResourceManager.TournamentRelationIncreaseValue);
			}
			foreach (Hero hero2 in RelationshipHelper.GetLocalHeros(winner.HeroObject))
			{
				RelationshipHelper.IncreasePlayerRelation(winner, hero2, ResourceManager.TournamentRelationIncreaseValue);
			}
		}

		public void OnBattleEndEvent(MapEvent m)
		{
            if (!WarlordModuleSettings.Instance.RelationByEventsEnabled)
            {
				return;
            }
			MessageManager.DisplayDebugMessage("Relationship increased after battle");

			int num = (int)Math.Min(m.GetRenownValue(m.WinningSide), (float)ResourceManager.BattleRelationIncreaseValue);
			if (num < 1)
			{
				return;
			}
			if (m.PlayerSide != m.WinningSide)
			{
				num *= -1;
			}
			MapEventSide mapEventSide = (m.WinningSide == BattleSideEnum.Attacker) ? m.AttackerSide : m.DefenderSide;
			IEnumerable<Hero> enumerable;
			if (mapEventSide == null)
			{
				enumerable = null;
			}
			else
			{
				enumerable = (from w in mapEventSide.Parties
							  where w.Party.LeaderHero != null
							  where w.Party.LeaderHero.IsActive
							  select w).Where(delegate (MapEventParty w)
							  {
								  PartyBase party = w.Party;
								  return ((party != null) ? party.LeaderHero : null) != Hero.MainHero;
							  }).Select(delegate (MapEventParty s)
							  {
								  PartyBase party = s.Party;
								  if (party == null)
								  {
									  return null;
								  }
								  return party.LeaderHero;
							  });
			}
			foreach (Hero hero in enumerable)
			{
				RelationshipHelper.IncreasePlayerRelation(Hero.MainHero, hero, num);
			}
			foreach (Hero hero2 in RelationshipHelper.GetLocalHeros(Hero.MainHero))
			{
				if (hero2.PartyBelongedTo == Hero.MainHero.PartyBelongedTo)
				{
					RelationshipHelper.IncreasePlayerRelation(Hero.MainHero, hero2, num);
				}
			}
		}
	}
}
