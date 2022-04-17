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
		public static bool TryAutoReloadSingle(
			CompReloadable comp,
			bool showOutOfAmmoWarning = false,
			bool showJobWarnings = false,
			bool ignoreDistance = false, 
			bool returnToStartingPosition = true)
		{
			bool success = true;
			if (comp?.RemainingCharges <= 0)
			{
				var pawn = comp.Wearer;
				var thing = comp.parent;

				if (pawn != null && thing != null)
				{
					var ammoInInventory = pawn.CountAmmoInInventory(comp);

					// check if comp needs reloading - this should always be the case at this point
					var minAmmoNeeded = comp.MinAmmoNeededChecked();

					// add ammo to inventory if pawn is not humanlike; for example a mech or a llama wielding a shotgun
					if (ammoInInventory < minAmmoNeeded && !pawn.RaceProps.Humanlike && yayoCombat.yayoCombat.refillMechAmmo)
					{
						Thing ammo = ThingMaker.MakeThing(comp.AmmoDef);
						ammo.stackCount = comp.MaxAmmoNeeded(true);
						if (pawn.inventory.innerContainer.TryAdd(ammo))
							ammoInInventory = ammo.stackCount;
					}

					// only reload equipped weapon from inventory
					if (ammoInInventory >= minAmmoNeeded)
						success = TryReloadFromInventory(pawn, new Thing[] { thing }, showJobWarnings);
					// reload all weapons from surrounding
					else
						success = TryReloadFromSurrounding(pawn, pawn.GetAllReloadableThings(), showJobWarnings, ignoreDistance, returnToStartingPosition);
					// show out of ammo warning if reloading failed
					if (showOutOfAmmoWarning && !success)
						GeneralUtility.ShowRejectMessage(pawn, "SY_YCA.OutOfAmmo".Translate( new NamedArgument(pawn, "pawn")));
				}
			}
			return success;
		}
		public static bool TryAutoReloadAll(
			Pawn pawn,
			bool showOutOfAmmoWarning = false,
			bool showJobWarnings = false,
			bool ignoreDistance = false, 
			bool returnToStartingPosition = true)
		{
			bool success = true;
			var things = pawn?.GetAllReloadableThings()?.ToArray();
			if (things?.AnyOutOfAmmo() == true)
			{
				var reloadFromInventory = false;
				foreach (var thing in things)
				{
					var comp = thing.TryGetComp<CompReloadable>();
					var ammoInInventory = pawn.CountAmmoInInventory(comp);

					// check if comp needs reloading - this should always be the case at this point
					var minAmmoNeeded = comp.MinAmmoNeededChecked();

					// add ammo to inventory if pawn is not humanlike; for example a mech or a llama wielding a shotgun
					if (ammoInInventory < minAmmoNeeded && !pawn.RaceProps.Humanlike && yayoCombat.yayoCombat.refillMechAmmo)
					{
						Thing ammo = ThingMaker.MakeThing(comp.AmmoDef);
						ammo.stackCount = comp.MaxAmmoNeeded(true);
						if (pawn.inventory.innerContainer.TryAdd(ammo))
							ammoInInventory = ammo.stackCount;
					}

					// reload from inventory is there is anything that can be reloaded from inventory
					if (ammoInInventory >= minAmmoNeeded) 
						reloadFromInventory = true;
				}

				// reload from inventory
				if (reloadFromInventory)
					success = TryReloadFromInventory(pawn, things, showJobWarnings);
				// reload from surrounding
				else
					success = TryReloadFromSurrounding(pawn, things, showJobWarnings, ignoreDistance, returnToStartingPosition);
				// show out of ammo warning if reloading failed
				if (showOutOfAmmoWarning && !success)
					GeneralUtility.ShowRejectMessage(pawn, "SY_YCA.OutOfAmmo".Translate(new NamedArgument(pawn, "pawn")));
			}
			return success;
		}


		public static bool TryReloadFromInventory(Pawn pawn, IEnumerable<Thing> reloadables, bool showWarnings)
		{
			bool success = false;

			// check for things requiring reloading
			var ammoDefDict = reloadables.GetRequiredAmmo();
			if (ammoDefDict.Count() > 0)
			{
				// find ammo for reloading in inventory
				var ammoThings = pawn.FindAmmoThingsInventory(ammoDefDict, showWarnings);
				if (ammoThings.Count > 0)
				{
					// make job
					var job = JobMaker.MakeJob(YCA_JobDefOf.ReloadFromInventory);

					// fill job queue
					foreach (var thing in reloadables)
						job.AddQueuedTarget(TargetIndex.A, thing);
					foreach (var thing in ammoThings)
						job.AddQueuedTarget(TargetIndex.B, thing);

					// start reload job and try to resume previous job after reloading
					pawn.jobs.StartJob(job, JobCondition.InterruptForced, resumeCurJobAfterwards: true, canReturnCurJobToPool: true);

					success = true;
				}
			}
			else if (showWarnings) // nothing to reload
				GeneralUtility.ShowRejectMessage(pawn, "SY_YCA.NothingToReload".Translate());

			return success;
		}

		public static bool TryReloadFromSurrounding(Pawn pawn, IEnumerable<Thing> reloadables, bool showWarnings, bool ignoreDistance, bool returnToStartingPosition = true)
		{
			bool success = false;
			if (!ignoreDistance && yayoCombat.yayoCombat.supplyAmmoDist < 0)
				return success;

			// check for things requiring reloading
			var ammoDefDict = reloadables.GetRequiredAmmo();
			if (ammoDefDict.Count() > 0)
			{
				// find ammo for reloading
				var ammoThings = pawn.FindAmmoThingsSurrounding(ammoDefDict, showWarnings, ignoreDistance);
				if (ammoThings.Count > 0)
				{
					// make job
					var job = JobMaker.MakeJob(YCA_JobDefOf.ReloadFromSurrounding);

					// fill job queues
					foreach (var thing in reloadables)
						job.AddQueuedTarget(TargetIndex.A, thing);
					foreach (var thing in ammoThings)
						job.AddQueuedTarget(TargetIndex.B, thing);

					// start reload job and try to resume previous job after reloading
					pawn.jobs.StartJob(job, JobCondition.InterruptForced, resumeCurJobAfterwards: true, canReturnCurJobToPool: true);

					// make pawn go back to where they were
					if (returnToStartingPosition)
						pawn.jobs.jobQueue.EnqueueFirst(JobMaker.MakeJob(JobDefOf.Goto, pawn.Position));

					success = true;
				}
			}
			else if (showWarnings) // nothing to reload
				GeneralUtility.ShowRejectMessage(pawn, "SY_YCA.NothingToReload".Translate());

			return success;
		}


		public static List<Thing> GetAllReloadableThings(this Pawn pawn)
		{
			var things = new List<Thing>();
			if (pawn != null)
			{
				foreach (var thing in pawn.equipment.AllEquipmentListForReading)
					if (thing.MaxAmmoNeeded(out _) > 0)
						things.Add(thing);

				foreach (var thing in pawn.GetSimpleSidearms())
					if (thing.MaxAmmoNeeded(out _) > 0)
						things.Add(thing);
			}
			return things;
		}
		public static Dictionary<Def, AmmoInfo> GetRequiredAmmo(this IEnumerable<Thing> things)
		{
			var output = new Dictionary<Def, AmmoInfo>();
			if (things != null)
			{
				foreach (var thing in things)
				{
					var count = thing.MaxAmmoNeeded(out Def def);
					if (count > 0)
					{
						var minAmmoNeeded = thing.MinAmmoNeededForThing();
						if (output.ContainsKey(def))
						{
							output[def].Count += count;
							if (output[def].MinAmmoNeeded > minAmmoNeeded)
								output[def].MinAmmoNeeded = minAmmoNeeded;
						}
						else
						{
							output.Add(def, new AmmoInfo
							{
								Count = count,
								MinAmmoNeeded = minAmmoNeeded,
							});
						}
					}
				}
			}
			return output;
		}

		public static List<Thing> FindAmmoThingsInventory(this Pawn pawn, Dictionary<Def, AmmoInfo> ammoDefDict, bool showWarnings)
		{
			var output = new List<Thing>();
			if (pawn != null && ammoDefDict != null)
			{
				foreach (var entry in ammoDefDict)
				{
					var ammoDef = entry.Key;
					var count = entry.Value.Count;
					var minAmmoNeeded = entry.Value.MinAmmoNeeded;

					// find things for this ammoDef in inventory
					foreach (var thing in pawn.inventory.innerContainer)
					{
						if (thing.def == ammoDef)
						{
							if (count <= 0)
								break;
							output.Add(thing);
							count -= thing.stackCount;
						}
					}
					// if less than minAmmoNeeded was found, remove all things of this ammoDef from output
					if ((entry.Value.Count - count) < minAmmoNeeded)
					{
						for (int i = output.Count - 1; i >= 0; i--)
							if (output[i].def == ammoDef)
								output.RemoveAt(i);
					}
					// show warning if ammo not found
					if (showWarnings)
					{
						GeneralUtility.ShowRejectMessage(
							pawn, 
							"SY_YCA.NoAmmoInventory".Translate(
								new NamedArgument(pawn, "pawn"),
								new NamedArgument(ammoDef.label, "ammo"),
								new NamedArgument(count, "count"),
								new NamedArgument(minAmmoNeeded, "minAmmoNeeded")));
					}
				}
			}
			return output;
		}
		public static List<Thing> FindAmmoThingsSurrounding(this Pawn pawn, Dictionary<Def, AmmoInfo> ammoDefDict, bool showWarnings, bool ignoreDistance)
		{
			var output = new List<Thing>();
			if (pawn != null && ammoDefDict != null)
			{
				foreach (var entry in ammoDefDict)
				{
					var ammoDef = entry.Key;
					var count = entry.Value.Count;
					var minAmmoNeeded = entry.Value.MinAmmoNeeded;

					// find things for this ammoDef nearby
					var things = RefuelWorkGiverUtility.FindEnoughReservableThings(
						pawn,
						pawn.Position,
						new IntRange(minAmmoNeeded, count),
						t => t.def == ammoDef && (ignoreDistance || IntVec3Utility.DistanceTo(pawn.Position, t.Position) <= yayoCombat.yayoCombat.supplyAmmoDist));

					// add found things to output
					if (things?.Count > 0)
					{
						foreach (var thing in things)
							output.Add(thing);
					}
					// show warning if ammo not found
					else if (showWarnings)
					{
						GeneralUtility.ShowRejectMessage(
							pawn,
							"SY_YCA.NoAmmoNearby".Translate(
								new NamedArgument(pawn, "pawn"),
								new NamedArgument(ammoDef.label, "ammo"),
								new NamedArgument(count, "count"),
								new NamedArgument(minAmmoNeeded, "minAmmoNeeded")));
					}
				}
			}
			return output;
		}

		// Helper class for picking up ammo
		public class AmmoInfo
		{
			public int Count;
			public int MinAmmoNeeded;
		}
	}
}
