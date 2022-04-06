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
		private Thing Gear => job.GetTarget(TargetIndex.A).Thing;

		public override bool TryMakePreToilReservations(bool errorOnFailed) =>
			true;

		protected override IEnumerable<Toil> MakeNewToils()
		{
			var comp = Gear?.TryGetComp<CompReloadable>();
			this.FailOn(() => pawn == null);
			this.FailOn(() => pawn.Downed);
			this.FailOn(() => comp == null);
			this.FailOn(() => comp.Wearer != pawn);

			yield return Toils_General.PutCarriedThingInInventory();

			var done = Toils_General.Label();
			yield return Toils_Jump.JumpIf(done, () =>
			{
				var output = true;
				if (comp.NeedsReload(false))
				{
					var innerContainer = pawn.inventory.innerContainer;
					for (int i = innerContainer.Count - 1; i >= 0; i--)
					{
						var thing = innerContainer[i];
						if (thing.def == comp.AmmoDef)
						{
							if (pawn.inventory.innerContainer.TryTransferToContainer(thing, pawn.carryTracker.innerContainer, true))
								output = false;
							else
								Log.Warning($"Yayo's Combat Addon: Could not move/merge '{thing}' into CarriedThing (carrying: '{pawn.carryTracker.CarriedThing}')");
						}
					}
				}
				return output;
			});
			yield return Toils_General.Wait(comp.Props.baseReloadTicks).WithProgressBarToilDelay(TargetIndex.A);

			yield return new Toil
			{
				initAction = () =>
				{
					var carriedThing = pawn.carryTracker.CarriedThing;
					if (carriedThing?.def == comp.AmmoDef)
						comp.ReloadFrom(carriedThing);
					else
						Log.Warning($"Yayo's Combat Addon: Invalid carried thing: '{carriedThing}' (needed: '{comp.AmmoDef}')");
				},
				defaultCompleteMode = ToilCompleteMode.Instant,
			};
			yield return done;

			yield return Toils_General.PutCarriedThingInInventory();
		}
	}
}
