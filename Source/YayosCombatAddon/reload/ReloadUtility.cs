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

					// reload from inventory
					if (ammoCount > 0)
						ReloadFromInventory(pawn, false, thing);
					// reload from surrounding
					else if (yayoCombat.yayoCombat.supplyAmmoDist >= 0)
						ReloadFromSurrounding(pawn, false, thing);
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
					var comp = thing?.TryGetComp<CompReloadable>();
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
					ReloadFromInventory(pawn, false, things);
				// reload from surrounding
				else if (yayoCombat.yayoCombat.supplyAmmoDist >= 0)
					ReloadFromSurrounding(pawn, false, things);
			}
		}


		public static void ReloadFromInventory(Pawn pawn, bool showMessages, IEnumerable<CompReloadable> comps)
		{
			var reloads = new List<Thing>();
			foreach (var comp in comps)
				if (comp.RemainingCharges < comp.MaxCharges)
					reloads.Add(comp.parent);

			if (reloads.Count > 0)
				ReloadFromInventory(pawn, showMessages, reloads.ToArray());
			else if (showMessages) // nothing to reload
				ShowRejectMessage("SY_YCA.NothingToReload".Translate());
		}
		public static void ReloadFromInventory(Pawn pawn, bool showMessages, params Thing[] things)
		{
			var job = JobMaker.MakeJob(YCA_JobDefOf.ReloadFromInventory);

			// just needed a variable for the job to tell it not to show messages
			job.overeat = showMessages;
			Log.Message($"ReloadFromInventory forced: {job.overeat}");

			foreach (var thing in things)
				job.AddQueuedTarget(TargetIndex.A, thing);
			pawn.jobs.TryTakeOrderedJob(job);
		}
		public static void ReloadFromSurrounding(Pawn pawn, bool showMessages, IEnumerable<CompReloadable> comps)
		{
			if (yayoCombat.yayoCombat.supplyAmmoDist < 0)
				return;

			var reloads = new List<Thing>();
			foreach (var comp in comps)
				if (comp.RemainingCharges < comp.MaxCharges)
					reloads.Add(comp.parent);

			if (reloads.Count > 0)
			{
				ReloadFromSurrounding(pawn, showMessages, reloads.ToArray());

				// make pawn go back to where they were
				pawn.jobs.jobQueue.EnqueueLast(JobMaker.MakeJob(JobDefOf.Goto, pawn.Position));
			}
			else if (showMessages) // nothing to reload
				ShowRejectMessage("SY_YCA.NothingToReload".Translate());
		}
		public static void ReloadFromSurrounding(Pawn pawn, bool showMessages, params Thing[] things)
		{
			var job = JobMaker.MakeJob(YCA_JobDefOf.ReloadFromSurrounding);

			// just needed a variable for the job to tell it not to show messages
			job.overeat = showMessages;
			Log.Message($"ReloadFromSurrounding forced: {job.overeat}");

			foreach (var thing in things)
				job.AddQueuedTarget(TargetIndex.A, thing);
			pawn.jobs.TryTakeOrderedJob(job);
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
						ShowRejectMessage("SY_YCA.NoAmmoRestock".Translate(
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
				ShowRejectMessage("SY_YCA.NothingToRestock".Translate());
			}
		}


		public static IEnumerable<CompReloadable> GetCompReloadables(this IEnumerable<Thing> things)
		{
			foreach (var thing in things)
			{
				var comp = thing?.TryGetComp<CompReloadable>();
				if (comp != null)
					yield return comp;
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
		public static int AmmoNeeded(this Thing thing)
		{
			var comp = thing?.TryGetComp<CompReloadable>();
			if (comp?.AmmoDef?.IsAmmo() == true)
				return comp.MaxAmmoNeeded(true);
			return 0;
		}
		public static bool IsOutOfAmmo(this Thing thing)
		{
			var comp = thing?.TryGetComp<CompReloadable>();
			if (comp?.AmmoDef?.IsAmmo() == true)
				return comp.RemainingCharges == 0;
			return false;
		}
		public static bool AnyOutOfAmmo(this IEnumerable<Thing> things)
		{
			foreach (var thing in things)
				if (thing.IsOutOfAmmo())
					return true;
			return false;
		}
		public static void EjectAmmo(Pawn pawn, CompReloadable comp)
		{
			var charges = comp.remainingCharges;
			if (charges > 0)
			{
				do
				{
					var ammo = ThingMaker.MakeThing(comp.AmmoDef);
					ammo.stackCount = Mathf.Min(ammo.def.stackLimit, charges);
					charges -= ammo.stackCount;
					GenPlace.TryPlaceThing(ammo, pawn.Position, pawn.Map, ThingPlaceMode.Near);
				}
				while (charges > 0);
				comp.Props.soundReload.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
				comp.remainingCharges = 0;
			}
		}


		public static bool IsAmmo(this ThingDef def) =>
			def?.thingCategories?.Contains(ThingCategoryDef.Named("yy_ammo_category")) == true;

		public static int CountAmmoInInventory(this Pawn pawn, CompReloadable comp)
		{
			var count = 0;
			foreach (var thing in pawn.inventory.innerContainer)
				if (thing.def == comp.AmmoDef)
					count += thing.stackCount;
			return count;
		}


		public static void ShowRejectMessage(string text) =>
			Messages.Message(text, MessageTypeDefOf.RejectInput, historical: false);


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
