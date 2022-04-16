using RimWorld;
using SimpleSidearms.rimworld;
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
		public static void TryAutoReloadSingle(CompReloadable comp)
		{
			if (comp?.RemainingCharges <= 0)
			{
				var pawn = comp.Wearer;
				var thing = comp.parent;

				if (pawn != null && thing != null)
				{
					var ammoCount = pawn.CountAmmoInInventory(comp);

					// add ammo to inventory if pawn is not humanlike; for example a mech or a llama wielding a shotgun
					if (ammoCount == 0 && !pawn.RaceProps.Humanlike && yayoCombat.yayoCombat.refillMechAmmo)
					{
						Thing ammo = ThingMaker.MakeThing(comp.AmmoDef);
						ammo.stackCount = comp.MaxAmmoNeeded(true);
						if (pawn.inventory.innerContainer.TryAdd(ammo))
							ammoCount = ammo.stackCount;
					}

					// only reload equipped weapon from inventory
					if (ammoCount > 0)
						ReloadFromInventory(pawn, new Thing[] { thing }, false);
					// reload all weapons from surrounding
					else if (yayoCombat.yayoCombat.supplyAmmoDist >= 0)
						ReloadFromSurrounding(pawn, pawn.GetAllReloadableThings(), false, false);
				}
			}
		}
		public static void TryAutoReloadAll(Pawn pawn)
		{
			var things = pawn?.GetAllReloadableThings()?.ToArray();
			if (things?.AnyOutOfAmmo() == true)
			{
				var reloadFromInventory = false;
				foreach (var thing in things)
				{
					var comp = thing.TryGetComp<CompReloadable>();
					var ammoInInventory = pawn.CountAmmoInInventory(comp);

					// add ammo to inventory if pawn is not humanlike; for example a mech or a llama wielding a shotgun
					if (ammoInInventory == 0 && !pawn.RaceProps.Humanlike && yayoCombat.yayoCombat.refillMechAmmo)
					{
						Thing ammo = ThingMaker.MakeThing(comp.AmmoDef);
						ammo.stackCount = comp.MaxAmmoNeeded(true);
						if (pawn.inventory.innerContainer.TryAdd(ammo))
							ammoInInventory = ammo.stackCount;
					}

					if (ammoInInventory > 0) 
						reloadFromInventory = true;
				}

				// reload from inventory
				if (reloadFromInventory)
					ReloadFromInventory(pawn, things, false);
				// reload from surrounding
				else if (yayoCombat.yayoCombat.supplyAmmoDist >= 0)
					ReloadFromSurrounding(pawn, things, false, false);
			}
		}


		public static void ReloadFromInventory(Pawn pawn, IEnumerable<Thing> things, bool showMessages)
		{
			if (things.Count() > 0)
			{
				var job = JobMaker.MakeJob(YCA_JobDefOf.ReloadFromInventory);

				// set attached variables
				var variables = JobDriver_ReloadFromInventory.AttachedVariables.GetOrCreateValue(job);
				variables.ShowMessages = showMessages;

				// fill job queue
				foreach (var thing in things)
					job.AddQueuedTarget(TargetIndex.A, thing);

				pawn.jobs.StartJob(job, JobCondition.InterruptForced, resumeCurJobAfterwards: true, canReturnCurJobToPool: true);
				//pawn.jobs.TryTakeOrderedJob(job);
			}
			else if (showMessages) // nothing to reload
				GeneralUtility.ShowRejectMessage("SY_YCA.NothingToReload".Translate());
		}

		public static void ReloadFromSurrounding(Pawn pawn, IEnumerable<Thing> things, bool showMessages, bool ignoreDistance, bool returnToStartingPosition = true)
		{
			if (!ignoreDistance && yayoCombat.yayoCombat.supplyAmmoDist < 0)
				return;

			if (things.Count() > 0)
			{
				var job = JobMaker.MakeJob(YCA_JobDefOf.ReloadFromSurrounding);

				// set attached variables
				var variables = JobDriver_ReloadFromSurrounding.AttachedVariables.GetOrCreateValue(job);
				variables.ShowMessages = showMessages;
				variables.IgnoreDistance = ignoreDistance;

				// fill job queue
				foreach (var thing in things)
					job.AddQueuedTarget(TargetIndex.A, thing);

				pawn.jobs.StartJob(job, JobCondition.InterruptForced, resumeCurJobAfterwards: true, canReturnCurJobToPool: true);
				//pawn.jobs.TryTakeOrderedJob(job);

				// make pawn go back to where they were
				if (returnToStartingPosition)
					pawn.jobs.jobQueue.EnqueueFirst(JobMaker.MakeJob(JobDefOf.Goto, pawn.Position));
			}
			else if (showMessages) // nothing to reload
				GeneralUtility.ShowRejectMessage("SY_YCA.NothingToReload".Translate());
		}

		public static void RestockInventoryFromSurrounding(Pawn pawn)
		{
			var required = new Dictionary<Def, int>();
			for (int i = 0; i < pawn.drugs.CurrentPolicy.Count; i++)
			{
				var entry = pawn.drugs.CurrentPolicy[i];
				var def = entry.drug;
				var count = entry.takeToInventory;
				if (def.IsAmmo() && count > 0)
					required.IncreaseOrAdd(def, count);
			}
			foreach (var entry in pawn.inventoryStock.stockEntries)
			{
				var def = entry.Value.thingDef;
				var count = entry.Value.count;
				if (def.IsAmmo() && count > 0)
					required.IncreaseOrAdd(def, count);
			}

			foreach (var thing in pawn.inventory.innerContainer)
			{
				if (thing.def.IsAmmo() && required.ContainsKey(thing.def))
					required.DecreaseOrRemove(thing.def, thing.stackCount);
			}

			if (required.Count > 0)
			{
				var enqueue = false;
				foreach (var entry in required)
				{
					var def = entry.Key;
					var count = entry.Value;
					var ammoList = RefuelWorkGiverUtility.FindEnoughReservableThings(
						pawn,
						pawn.Position,
						new IntRange(1, count),
						t => t.def == def);

					if (ammoList?.Count > 0)
					{
						foreach (var ammo in ammoList)
						{
							var job = JobMaker.MakeJob(JobDefOf.TakeCountToInventory, ammo);
							job.count = Mathf.Min(ammo.stackCount, count);
							count -= job.count;
							pawn.jobs.TryTakeOrderedJob(job, requestQueueing: enqueue);

							enqueue = true;
							if (count == 0)
								break;
						}
					}

					if (count > 0)
					{
						GeneralUtility.ShowRejectMessage("SY_YCA.NoAmmoRestock".Translate(
							new NamedArgument(pawn, "pawn"),
							new NamedArgument(def.label, "ammo"),
							new NamedArgument(count, "count"),
							new NamedArgument(entry.Value, "max")));
					}
				}

				if (enqueue)
					pawn.jobs.jobQueue.EnqueueLast(JobMaker.MakeJob(JobDefOf.Goto, pawn.Position));
			}
			else
			{
				GeneralUtility.ShowRejectMessage("SY_YCA.NothingToRestock".Translate());
			}
		}


		public static IEnumerable<Thing> GetAllReloadableThings(this Pawn pawn)
		{
			if (pawn == null)
				yield break;

			foreach (var thing in pawn.equipment.AllEquipmentListForReading)
				if (thing?.AmmoNeeded() > 0)
					yield return thing;

			if (Main.SimpleSidearmsCompatibility)
			{
				var memory = CompSidearmMemory.GetMemoryCompForPawn(pawn);
				foreach (var thing in pawn.inventory.innerContainer)
				{
					if (thing != null
						&& memory.RememberedWeapons.Contains(new ThingDefStuffDefPair(thing.def))
						&& thing.AmmoNeeded() > 0)
						yield return thing;
				}
			}
		}
	}
}
