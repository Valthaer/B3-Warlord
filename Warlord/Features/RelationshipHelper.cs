using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Warlord
{
	public static class RelationshipHelper
	{
		public static void IncreasePlayerRelation(CharacterObject character, Hero hero2, int amount)
		{
			Hero heroObject = character.HeroObject;
			if (heroObject == null)
			{
				return;
			}
			RelationshipHelper.IncreasePlayerRelation(heroObject, hero2, amount);
		}

		public static void IncreasePlayerRelation(Hero hero1, Hero hero2, int amount)
		{
			if (amount < 1)
			{
				return;
			}
			Hero hero3 = hero1;
			Hero hero4 = hero2;
			if ((hero1.Clan == null || hero1.Clan.Leader != null) && (hero2.Clan == null || hero2.Clan.Leader != null))
			{
				try
				{
					Campaign.Current.Models.DiplomacyModel.GetHeroesForEffectiveRelation(hero1, hero2, out hero3, out hero4);
				}
				catch
				{
				}
			}
			int unadjustedRelationshipValue = RelationshipHelper.GetUnadjustedRelationshipValue(hero3, hero4);
			int value = Math.Min(amount + unadjustedRelationshipValue, 100);
			hero3.SetPersonalRelation(hero4, value);

			MessageManager.DisplayMessage($"Your relationship with {hero2.Name} has improved",ModuleColors.uiNormalPriorityEvent);			
		}

		public static int GetUnadjustedRelationshipValue(Hero hero1, Hero hero2)
		{
			return MBMath.ClampInt(CharacterRelationManager.GetHeroRelation(hero1, hero2), -100, 100);
		}

		public static IEnumerable<Hero> GetLocalHeros(Hero hero)
		{
			Vec3 position = hero.GetPosition();
			List<Hero> affectedHeroes = new List<Hero>();
			IEnumerable<Hero> aliveHeroes = from w in Campaign.Current.AliveHeroes
											where w.IsActive
											where w.Id != hero.Id
											where w.IsNoble || w.IsWanderer
											select w;
			
			foreach (Hero nearHero in aliveHeroes)
			{
				if (nearHero.GetPosition().DistanceSquared(position) < (float)ResourceManager.RadiusToAffectByLordEvents)
				{
					affectedHeroes.Add(nearHero);
				}
			}
			return affectedHeroes;
		}
	}
}
