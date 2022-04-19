using RimWorld;
using SimpleSidearms.rimworld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace YayosCombatAddon
{
	internal static class InventoryUtility
	{
		public static bool EquipThingFromInventory(this Pawn pawn, Thing thing)
		{
			var success = false;
			var equipment = pawn.equipment;
			var primary = equipment.Primary;
			if (thing is ThingWithComps thingWithComps)
			{
				if (thingWithComps != primary)
				{
					if (primary != null && !equipment.TryTransferEquipmentToContainer(primary, pawn.inventory.innerContainer))
						Log.Warning($"{nameof(YayosCombatAddon)}: could not move '{primary}' into inventory");
					thingWithComps.holdingOwner?.Remove(thingWithComps);
					equipment.Primary = thingWithComps;
					success = true;
				}
			}
			else
				Log.Warning($"{nameof(YayosCombatAddon)}: '{thing}' is not {nameof(ThingWithComps)}");
			return success;
		}

		/// <summary>
		/// Remember to check for <see cref="Main.SimpleSidearmsCompatibility"/> before executing; probably due to optimization, it can't be checked inside the method
		/// </summary>
		/// <param name="pawn"></param>
		/// <returns></returns>
		public static List<Thing> GetSimpleSidearms(this Pawn pawn)
		{
			var things = new List<Thing>();
			if (pawn != null)
			{
				Log.Message("Test");
				var memory = CompSidearmMemory.GetMemoryCompForPawn(pawn);
				foreach (var thing in pawn.inventory.innerContainer)
				{
					if (thing != null && memory.RememberedWeapons.Contains(new ThingDefStuffDefPair(thing.def)))
						things.Add(thing);
				}
			}
			return things;
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
						GeneralUtility.ShowRejectMessage(
							pawn, 
							"SY_YCA.NoAmmoRestock".Translate(
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
				GeneralUtility.ShowRejectMessage(pawn, "SY_YCA.NothingToRestock".Translate());
			}
		}
	}
}
