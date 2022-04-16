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
					var ammoInInventory = pawn.CountAmmoInInventory(comp);

					// add ammo to inventory if pawn is not humanlike; for example a mech or a llama wielding a shotgun
					if (ammoInInventory == 0 && !pawn.RaceProps.Humanlike && yayoCombat.yayoCombat.refillMechAmmo)
					{
						Thing ammo = ThingMaker.MakeThing(comp.AmmoDef);
						ammo.stackCount = comp.MaxAmmoNeeded(true);
						if (pawn.inventory.innerContainer.TryAdd(ammo))
							ammoInInventory = ammo.stackCount;
					}

					// only reload equipped weapon from inventory
					if (ammoInInventory > 0)
						ReloadFromInventory(pawn, new Thing[] { thing }, false);
					// reload all weapons from surrounding
					else
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
				else
					ReloadFromSurrounding(pawn, things, false, false);
			}
		}


		public static void ReloadFromInventory(Pawn pawn, IEnumerable<Thing> reloadables, bool showMessages)
		{
			var ammoDefDict = reloadables.GetRequiredAmmo();
			if (ammoDefDict.Count() > 0)
			{
				var ammoThings = pawn.FindAmmoThingsInventory(ammoDefDict);
				if (ammoThings.Count > 0)
				{
					var job = JobMaker.MakeJob(YCA_JobDefOf.ReloadFromInventory);

					// fill job queue
					foreach (var thing in reloadables)
						job.AddQueuedTarget(TargetIndex.A, thing);
					foreach (var thing in ammoThings)
						job.AddQueuedTarget(TargetIndex.B, thing);

					pawn.jobs.StartJob(job, JobCondition.InterruptForced, resumeCurJobAfterwards: true, canReturnCurJobToPool: true);
					//pawn.jobs.TryTakeOrderedJob(job);
				}
#warning TODO no ammo found message?
			}
			else if (showMessages) // nothing to reload
				GeneralUtility.ShowRejectMessage("SY_YCA.NothingToReload".Translate());
		}

		public static void ReloadFromSurrounding(Pawn pawn, IEnumerable<Thing> reloadables, bool showMessages, bool ignoreDistance, bool returnToStartingPosition = true)
		{
			if (!ignoreDistance && yayoCombat.yayoCombat.supplyAmmoDist < 0)
				return;

			// check for things requiring reloading
			var ammoDefDict = reloadables.GetRequiredAmmo();
			if (ammoDefDict.Count() > 0)
			{
				// find ammo for reloading
				var ammoThings = pawn.FindAmmoThingsSurrounding(ammoDefDict, ignoreDistance);
				if (ammoThings.Count > 0)
				{
					var job = JobMaker.MakeJob(YCA_JobDefOf.ReloadFromSurrounding);

					// fill job queues
					foreach (var thing in reloadables)
						job.AddQueuedTarget(TargetIndex.A, thing);
					foreach (var thing in ammoThings)
						job.AddQueuedTarget(TargetIndex.B, thing);

					pawn.jobs.StartJob(job, JobCondition.InterruptForced, resumeCurJobAfterwards: true, canReturnCurJobToPool: true);
					//pawn.jobs.TryTakeOrderedJob(job);

					// make pawn go back to where they were
					if (returnToStartingPosition)
						pawn.jobs.jobQueue.EnqueueFirst(JobMaker.MakeJob(JobDefOf.Goto, pawn.Position));
				}
#warning TODO no ammo found message?
			}
			else if (showMessages) // nothing to reload
				GeneralUtility.ShowRejectMessage("SY_YCA.NothingToReload".Translate());
		}


		public static IEnumerable<Thing> GetAllReloadableThings(this Pawn pawn)
		{
			if (pawn == null)
				yield break;

			foreach (var thing in pawn.equipment.AllEquipmentListForReading)
				if (thing.AmmoNeeded(out _) > 0)
					yield return thing;

			if (Main.SimpleSidearmsCompatibility)
			{
				var memory = CompSidearmMemory.GetMemoryCompForPawn(pawn);
				foreach (var thing in pawn.inventory.innerContainer)
				{
					if (thing != null
						&& memory.RememberedWeapons.Contains(new ThingDefStuffDefPair(thing.def))
						&& thing.AmmoNeeded(out _) > 0)
						yield return thing;
				}
			}
		}
		public static Dictionary<Def, int> GetRequiredAmmo(this IEnumerable<Thing> things)
		{
			var output = new Dictionary<Def, int>();
			if (things != null)
			{
				foreach (var thing in things)
				{
					var count = thing.AmmoNeeded(out Def def);
					if (count > 0)
						output.IncreaseOrAdd(def, count);
				}
			}
			return output;
		}

		public static List<Thing> FindAmmoThingsInventory(this Pawn pawn, Dictionary<Def, int> ammoDefDict)
		{
			var ammoThings = new List<Thing>();
			if (pawn != null && ammoDefDict != null)
			{
				foreach (var entry in ammoDefDict)
				{
					var ammoDef = entry.Key;
					var count = entry.Value;
					foreach (var thing in pawn.inventory.innerContainer)
					{
						if (thing.def == ammoDef)
						{
							if (count <= 0)
								break;
							ammoThings.Add(thing);
							count -= thing.stackCount;
						}
					}
#warning TODO no ammo found message?
				}
			}
			return ammoThings;
		}
		public static List<Thing> FindAmmoThingsSurrounding(this Pawn pawn, Dictionary<Def, int> ammoDefDict, bool ignoreDistance)
		{
			var ammoThings = new List<Thing>();
			if (pawn != null && ammoDefDict != null)
			{
				foreach (var entry in ammoDefDict)
				{
					var ammoDef = entry.Key;
					var count = entry.Value;
					var things = RefuelWorkGiverUtility.FindEnoughReservableThings(
						pawn,
						pawn.Position,
						new IntRange(1, count),
						t => t.def == ammoDef && (ignoreDistance || IntVec3Utility.DistanceTo(pawn.Position, t.Position) <= yayoCombat.yayoCombat.supplyAmmoDist));
					foreach (var thing in things)
						ammoThings.Add(thing);
#warning TODO no ammo found message?
				}
			}
			return ammoThings;
		}
	}
}
