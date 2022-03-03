using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace Warlord
{
	public class RenownGainedEventListener
	{
		private static readonly int TOURNAMENT_RELATIONSHIP_INCREASE_AMOUNT = WarlordModuleSettings.Instance.TournamentRelationshipIncrease;

		private static readonly int BATTLE_RELATIONSHIP_INCREASE_AMOUNT = WarlordModuleSettings.Instance.BattleRelationshipIncrease;

		private static readonly int RADIUS = WarlordModuleSettings.Instance.Radius;

		public void PleaseTheCrowd(CharacterObject winner, MBReadOnlyList<CharacterObject> characters, Town town, ItemObject item)
		{
			if (!winner.IsPlayerCharacter)
			{
				return;
			}
			foreach (Hero hero in (from s in town.Workshops
								   select s.Owner).Distinct<Hero>())
			{
				this.IncreasePlayerRelation(winner, hero, RenownGainedEventListener.TOURNAMENT_RELATIONSHIP_INCREASE_AMOUNT);
			}
			//foreach (Hero hero2 in this.GetLocalHeros(winner.HeroObject, 1))
			//{
			//	this.IncreasePlayerRelation(winner, hero2, RenownGainedEventListener.TOURNAMENT_RELATIONSHIP_INCREASE_AMOUNT);
			//}
		}

		private void IncreasePlayerRelation(CharacterObject character, Hero hero2, int amount)
		{
			Hero heroObject = character.HeroObject;
			if (heroObject == null)
			{
				return;
			}
			RenownGainedEventListener.IncreasePlayerRelation(heroObject, hero2, amount);
		}
		public static void IncreasePlayerRelation(Hero hero1, Hero hero2, int amount)
		{
			int relation = hero1.GetRelation(hero2);
			if (relation < 100)
			{
				int value = Math.Min(amount + relation, 100);
				hero1.SetPersonalRelation(hero2, value);
				if (hero1.GetRelation(hero2) == relation)
				{
					CharacterRelationManager.SetHeroRelation(hero1, hero2, value);
					if (hero1.GetRelation(hero2) == relation)
					{
						ChangeRelationAction.ApplyRelationChangeBetweenHeroes(hero1, hero2, 1, false);
					}
				}
				InformationManager.DisplayMessage(new InformationMessage(string.Format("Your relationship with {0} has improved.", hero2.Name)));
			}
		}
		public void RenownGainedListener(Hero hero, int renownAmount, bool _showNotification)
		{
			//if (!hero.IsHumanPlayerCharacter || renownAmount < 1)
			//{
			//	return;
			//}
			//int amount = Math.Min(renownAmount, RenownGainedEventListener.BATTLE_RELATIONSHIP_INCREASE_AMOUNT);
			//foreach (Hero hero2 in this.GetLocalHeros(hero, RenownGainedEventListener.RADIUS).DistinctBy((Hero d) => d.Id))
			//{
			//	RenownGainedEventListener.IncreasePlayerRelation(hero, hero2, amount);
			//}
		}
		//private IEnumerable<Hero> GetLocalHeros(Hero hero, int radius = 1)
		//{
		//	Vec3 position = hero.GetPosition();
		//	IEnumerable<Hero> source = from w in Campaign.Current.AliveHeroes
		//							   where w.IsActive
		//							   select w;
		//	Func<Hero, bool> <> 9__1;
		//	Func<Hero, bool> predicate;
		//	if ((predicate = <> 9__1) == null)
		//	{
		//		predicate = (<> 9__1 = ((Hero w) => w.Id != hero.Id));
		//	}
		//	IEnumerable<Hero> source2 = from w in source.Where(predicate)
		//								where w.IsNoble || w.IsWanderer
		//								select w;
		//	Func<Hero, bool> <> 9__3;
		//	Func<Hero, bool> predicate2;
		//	if ((predicate2 = <> 9__3) == null)
		//	{
		//		predicate2 = (<> 9__3 = ((Hero w) => !w.IsEnemy(hero)));
		//	}
		//	foreach (Hero hero2 in source2.Where(predicate2))
		//	{
		//		if (hero2.GetPosition().DistanceSquared(position) < (float)radius)
		//		{
		//			yield return hero2;
		//		}
		//	}
		//	IEnumerator<Hero> enumerator = null;
		//	yield break;
		//	yield break;
		//}
	}
}
