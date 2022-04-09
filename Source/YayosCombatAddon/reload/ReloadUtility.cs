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
			var reloads = new List<Thing>();

			foreach (var comp in comps)
			{
				if (comp.RemainingCharges < comp.MaxCharges)
					reloads.Add(comp.parent);
			}

			if (reloads.Count > 0)
			{
				var job = JobMaker.MakeJob(YCA_JobDefOf.ReloadFromInventory);
				foreach (var thing in reloads)
					job.AddQueuedTarget(TargetIndex.A, thing);
				pawn.jobs.TryTakeOrderedJob(job);
			}
			else // nothing to reload
				ShowRejectMessage("SY_YCA.NothingToReload".Translate());
		}

		public static void TryForcedReloadFromSurrounding(Pawn pawn, IEnumerable<CompReloadable> comps)
		{
			if (yayoCombat.yayoCombat.supplyAmmoDist < 0)
				return;

			var reloads = new List<Thing>();

			var noWeaponToReload = true;
			foreach (var comp in comps)
			{
				if (comp.RemainingCharges < comp.MaxCharges)
				{
					noWeaponToReload = false;
					reloads.Add(comp.parent);
				}
			}

			if (reloads.Count > 0)
			{
				var job = JobMaker.MakeJob(YCA_JobDefOf.ReloadFromSurrounding);
				foreach (var thing in reloads)
					job.AddQueuedTarget(TargetIndex.A, thing);
				pawn.jobs.TryTakeOrderedJob(job);
			}

			if (noWeaponToReload) // nothing to reload
				ShowRejectMessage("SY_YCA.NothingToReload".Translate());
			else // make pawn go back to where they were
				pawn.jobs.jobQueue.EnqueueLast(JobMaker.MakeJob(JobDefOf.Goto, pawn.Position));
		}

		public static void TryRestockInventoryFromSurrounding(Pawn pawn)
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
				var hasJob = false;
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
							pawn.jobs.TryTakeOrderedJob(job, requestQueueing: hasJob);

							hasJob = true;
							if (count == 0)
								break;
						}
					}

					if (count > 0)
					{
						ShowRejectMessage("SY_YCA.NoAmmoRestock".Translate(
							new NamedArgument(pawn, "pawn"),
							new NamedArgument(def.label, "ammo"),
							new NamedArgument(count, "count"),
							new NamedArgument(entry.Value, "max")));
					}
				}

				if (hasJob)
					pawn.jobs.jobQueue.EnqueueLast(JobMaker.MakeJob(JobDefOf.Goto, pawn.Position));
			}
			else
			{
				ShowRejectMessage("SY_YCA.NothingToRestock".Translate());
			}
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

		public static void IncreaseOrAdd<T>(this Dictionary<T, int> dictionary, T t, int count)
		{
			if (dictionary.ContainsKey(t))
				dictionary[t] += count;
			else
				dictionary.Add(t, count);
		}
		public static void DecreaseOrRemove<T>(this Dictionary<T, int> dictionary, T t, int count)
		{
			if (dictionary.ContainsKey(t))
			{
				var value = dictionary[t] - count;
				if (value > 0)
					dictionary[t] = value;
				else
					dictionary.Remove(t);
			}
		}
	}
}
