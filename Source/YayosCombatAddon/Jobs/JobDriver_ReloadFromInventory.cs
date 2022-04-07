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
			// carried thing -> inventory
			// -- label: NEXT
			// get next weapon to reload out of Target A queue
			// if not reloadable or no ammo -> goto done
			// equip weapon
			// take ammo out of inventory -> CarriedThing
			// "wait"
			// reload
			// -> goto NEXT
			// -- label: DONE
			// put carried thing into inventory
			// switch back to original weapon

			var primary = GetPrimary();
			yield return Toils_General.PutCarriedThingInInventory();
			yield return next;
			yield return Toils_JobTransforms.ExtractNextTargetFromQueue(TargetIndex.A);
			yield return Toils_Jump.JumpIf(done, () => !CheckReloadableAmmo());
			yield return Equip(TargetThingA);
			yield return AmmoToCarriedThing();
			yield return Wait;
			yield return Reload();
			yield return Toils_Jump.JumpIf(next, () => !job.GetTargetQueue(TargetIndex.A).NullOrEmpty());
			yield return done;
			yield return Toils_General.PutCarriedThingInInventory();
			yield return Equip(primary);
		}

		private bool CheckReloadableAmmo()
		{
			var comp = TargetThingA?.TryGetComp<CompReloadable>();
			if (comp?.NeedsReload(true) == true)
			{
				// sneaky way for setting wait duration using comp
				Wait.defaultDuration = comp.Props.baseReloadTicks;
				foreach (var thing in pawn.inventory.innerContainer)
					if (thing?.def == comp.AmmoDef)
						return true;
			}
			return false;
		}

		private Thing GetPrimary() => 
			pawn?.equipment?.Primary;

		private Toil AmmoToCarriedThing()
		{
			return new Toil
			{
				initAction = () =>
				{
					Log.Message("AmmoToCarriedThing");
					var comp = TargetThingA?.TryGetComp<CompReloadable>();
					if (comp?.NeedsReload(true) == true)
					{
						var innerContainer = pawn.inventory.innerContainer;
						for (int i = innerContainer.Count - 1; i >= 0; i--)
						{
							var thing = innerContainer[i];
							if (thing.def == comp.AmmoDef)
							{
								if (!pawn.inventory.innerContainer.TryTransferToContainer(thing, pawn.carryTracker.innerContainer))
									Log.Warning($"Yayo's Combat Addon: Could not move/merge '{thing}' into CarriedThing (carrying: '{pawn.carryTracker.CarriedThing}')");
							}
						}
					}
				},
				defaultCompleteMode = ToilCompleteMode.Instant,
			};
		}

		private Toil Equip(Thing thing)
		{
			return new Toil
			{
				initAction = () =>
				{
					Log.Message("Equip");
					// TODO:
					//  check if thing != primary
					//  move primary to inventory
					//  move thing to primary
				},
				defaultCompleteMode = ToilCompleteMode.Instant,
			};
		}

		private Toil Reload()
		{
			return new Toil
			{
				initAction = delegate
				{
					Log.Message("Reload");
					var comp = TargetThingA?.TryGetComp<CompReloadable>();
					if (comp?.NeedsReload(true) == true)
					{
						var carriedThing = pawn.carryTracker.CarriedThing;
						if (carriedThing?.def == comp.AmmoDef)
							comp.ReloadFrom(carriedThing);
						else
							Log.Warning($"Yayo's Combat Addon: Invalid carried thing: '{carriedThing}' (needed: '{comp.AmmoDef}')");
					}
					else
						Log.Warning($"Yayo's Combat Addon: failed getting comp / does not need reloading: '{TargetThingA}'");
				},
				defaultCompleteMode = ToilCompleteMode.Instant,
			};
		}
	}
}
