using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace YayosCombatAddon
{
	internal class JobDriver_ReloadFromInventory : JobDriver
	{
		private Toil Wait { get; } = Toils_General.Wait(1).WithProgressBarToilDelay(TargetIndex.A);

		public override bool TryMakePreToilReservations(bool errorOnFailed) =>
			true;

		protected override IEnumerable<Toil> MakeNewToils()
		{
			Log.Message($"'{pawn}'");
			this.FailOn(() => pawn == null);
			this.FailOn(() => pawn.Downed);
			this.FailOnIncapable(PawnCapacityDefOf.Manipulation);

			var next = Toils_General.Label();
			var done = Toils_General.Label();

			// save currently equipped weapon
			// -- NEXT
			// move carried thing into inventory
			// get next weapon to reload out of Target A queue
			// if not reloadable or no ammo -> goto DONE
			// equip weapon
			// take ammo out of inventory as carried thing
			// wait
			// reload
			// if more things in queue -> goto NEXT
			// -- DONE
			// put carried thing back into inventory
			// switch back to original weapon

			var primary = GetPrimary();
			yield return next;
			yield return Toils_JobTransforms.ExtractNextTargetFromQueue(TargetIndex.A);
			yield return Toils_Jump.JumpIf(done, () => !CheckReloadableAmmo());
			yield return Toils_General.PutCarriedThingInInventory();
			yield return Equip();
			yield return Ammo();
			yield return Wait;
			yield return Reload();
			yield return Toils_Jump.JumpIf(next, () => !job.GetTargetQueue(TargetIndex.A).NullOrEmpty());
			yield return done;
			yield return Toils_General.PutCarriedThingInInventory();
			yield return Equip(primary);
		}

		private bool CheckReloadableAmmo()
		{
			Log.Message($"CheckReloadableAmmo '{TargetThingA}'");
			var comp = TargetThingA?.TryGetComp<CompReloadable>();
			if (comp?.NeedsReload(true) == true)
			{
				// sneaky way for setting wait duration using comp
				Wait.defaultDuration = comp.Props.baseReloadTicks;

				if (pawn.carryTracker.CarriedThing?.def == comp.AmmoDef)
					return true;
				foreach (var thing in pawn.inventory.innerContainer)
					if (thing?.def == comp.AmmoDef)
						return true;

				Log.Warning($"Yayo's Combat Addon: could not find ammo for '{TargetThingA}' in inventory (ammo: '{comp.AmmoDef}')");
			}
			else
				Log.Warning($"Yayo's Combat Addon: '{TargetThingA}' does not need reloading");
			return false;
		}

		private Thing GetPrimary() => 
			pawn?.equipment?.Primary;

		private Toil Ammo()
		{
			return new Toil
			{
				initAction = () =>
				{
					Log.Message($"Ammo '{TargetThingA}'");
					var comp = TargetThingA?.TryGetComp<CompReloadable>();
					if (comp?.NeedsReload(true) == true)
					{
						var innerContainer = pawn.inventory.innerContainer;
						for (int i = innerContainer.Count - 1; i >= 0; i--)
						{
							var thing = innerContainer[i];
							if (thing.def == comp.AmmoDef && !pawn.inventory.innerContainer.TryTransferToContainer(thing, pawn.carryTracker.innerContainer))
								Log.Warning($"Yayo's Combat Addon: could not move/merge '{thing}' into CarriedThing (carrying: '{pawn.carryTracker.CarriedThing}')");
						}
					}
				},
				defaultCompleteMode = ToilCompleteMode.Instant,
			};
		}

		private Toil Equip(Thing staticThing = null)
		{
			return new Toil
			{
				initAction = () =>
				{
					var thing = staticThing ?? TargetThingA;
					Log.Message($"Equip '{thing}'");
					var equipment = pawn.equipment;
					var inventory = pawn.inventory;
					var primary = equipment.Primary;
					if (thing is ThingWithComps thingWithComps)
					{
						if (thingWithComps != primary)
						{
							if (primary != null && !equipment.TryTransferEquipmentToContainer(primary, pawn.inventory.innerContainer))
								Log.Warning($"Yayo's Combat Addon: could not move '{primary}' into inventory");
							thingWithComps.holdingOwner?.Remove(thingWithComps);
							equipment.AddEquipment(thingWithComps);
						}
					}
					else
						Log.Warning($"Yayo's Combat Addon: '{thing}' is not {nameof(ThingWithComps)}");
				},
				defaultCompleteMode = ToilCompleteMode.Instant,
			};
		}

		private Toil Reload()
		{
			return new Toil
			{
				initAction = () =>
				{
					Log.Message($"Reload '{TargetThingA}'");
					var comp = TargetThingA?.TryGetComp<CompReloadable>();
					if (comp?.NeedsReload(true) == true)
					{
						var carriedThing = pawn.carryTracker.CarriedThing;
						if (carriedThing?.def == comp.AmmoDef)
							comp.ReloadFrom(carriedThing);
						else
							Log.Warning($"Yayo's Combat Addon: invalid carried thing: '{carriedThing}' (needed: '{comp.AmmoDef}')");
					}
					else
						Log.Warning($"Yayo's Combat Addon: failed getting comp / does not need reloading: '{TargetThingA}'");
				},
				defaultCompleteMode = ToilCompleteMode.Instant,
			};
		}
	}
}
