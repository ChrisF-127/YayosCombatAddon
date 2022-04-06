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
	public static class ReloadUtility
	{
		public static void TryForcedReloadFromInventory(Pawn pawn, IEnumerable<CompReloadable> comps)
		{
			var first = true;
			var noAmmo = true;
			var noWeaponToReload = true;
			foreach (var comp in comps)
			{
				if (comp.RemainingCharges < comp.MaxCharges)
				{
					noWeaponToReload = false;
					var ammo = pawn.inventory.innerContainer.FirstOrDefault((item) => item.def == comp.AmmoDef);
					if (ammo != null)
					{
						noAmmo = false;
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
			if (noWeaponToReload) // nothing to reload
				ShowRejectMessage("SY_YCA.NothingToReload".Translate());
			else if (noAmmo) // no ammo
				ShowRejectMessage("SY_YCA.NoAmmo".Translate());
		}

		public static void TryForcedReloadFromSurrounding(Pawn pawn, IEnumerable<CompReloadable> comps)
		{
			if (yayoCombat.yayoCombat.supplyAmmoDist < 0)
				return;
			
			var first = true;
			var noAmmo = true;
			var noWeaponToReload = true;
			foreach (var comp in comps)
			{
				if (comp.RemainingCharges < comp.MaxCharges)
				{
					noWeaponToReload = false;
					var ammoList = RefuelWorkGiverUtility.FindEnoughReservableThings(
						pawn,
						pawn.Position,
						new IntRange(comp.MinAmmoNeeded(false), comp.MaxAmmoNeeded(false)),
						t => t.def == comp.AmmoDef && IntVec3Utility.DistanceTo(pawn.Position, t.Position) <= yayoCombat.yayoCombat.supplyAmmoDist);

					if (ammoList?.Count > 0)
					{
						noAmmo = false;
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
			if (noWeaponToReload) // nothing to reload
				ShowRejectMessage("SY_YCA.NothingToReload".Translate());
			else if (noAmmo) // no ammo
				ShowRejectMessage("SY_YCA.NoAmmo".Translate());
			else // make pawn go back to where they were
				pawn.jobs.jobQueue.EnqueueLast(JobMaker.MakeJob(JobDefOf.Goto, pawn.Position));
		}


		public static void ShowRejectMessage(string text) =>
			Messages.Message(text, MessageTypeDefOf.RejectInput, historical: false);


		public static bool IsAmmo(this ThingDef def) =>
			def?.thingCategories?.Contains(ThingCategoryDef.Named("yy_ammo_category")) == true;

		public static bool RequiresReloading(this IEnumerable<CompReloadable> comps)
		{
			foreach (var comp in comps)
				if (comp.RemainingCharges < comp.MaxCharges)
					return true;
			return false;
		}
	}
}
