using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace YayosCombatAddon
{
	class ReloadUtility
	{
		public static void TryForcedReloadFromInventory(Pawn pawn, IEnumerable<CompReloadable> comps)
		{
			var first = true;
			foreach (var comp in comps)
			{
				if (comp.RemainingCharges < comp.MaxCharges)
				{
					var ammo = pawn.inventory.innerContainer.FirstOrDefault((item) => item.def == comp.AmmoDef);
					if (ammo != null)
					{
						var job = JobMaker.MakeJob(YCA_JobDefOf.ReloadFromInventory, comp.parent);
						if (first)
						{
							pawn.jobs.TryTakeOrderedJob(job);
							first = false;
						}
						else
							pawn.jobs.jobQueue.EnqueueFirst(job);
					}
				}
			}
		}

		public static void TryForcedReloadFromSurrounding(Pawn pawn, IEnumerable<CompReloadable> comps)
		{
			if (yayoCombat.yayoCombat.supplyAmmoDist < 0)
				return;
			
			var first = true;
			foreach (var comp in comps)
			{
				if (comp.RemainingCharges < comp.MaxCharges)
				{
					var ammoList = RefuelWorkGiverUtility.FindEnoughReservableThings(
						pawn,
						pawn.Position,
						new IntRange(comp.MinAmmoNeeded(false), comp.MaxAmmoNeeded(false)),
						t => t.def == comp.AmmoDef && IntVec3Utility.DistanceTo(pawn.Position, t.Position) <= yayoCombat.yayoCombat.supplyAmmoDist);

					if (ammoList?.Count > 0)
					{
						var job = JobGiver_Reload.MakeReloadJob(comp, ammoList);
						if (first)
						{
							pawn.jobs.TryTakeOrderedJob(job);
							first = false;
						}
						else
							pawn.jobs.jobQueue.EnqueueLast(job);
					}
				}
			}

			// go back
			pawn.jobs.jobQueue.EnqueueLast(JobMaker.MakeJob(JobDefOf.Goto, pawn.Position));
		}

		public static bool IsAmmo(ThingDef def) =>
			def?.thingCategories?.Contains(ThingCategoryDef.Named("yy_ammo_category")) == true;
	}
}
