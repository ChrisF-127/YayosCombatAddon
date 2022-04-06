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
								Log.Warning($"Could not move/merge '{thing}' into CarriedThing (carrying: '{pawn.carryTracker.CarriedThing}')");
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
						Log.Warning($"Invalid carried thing: '{carriedThing}' (needed: '{comp.AmmoDef}')");
				},
				defaultCompleteMode = ToilCompleteMode.Instant,
			};
			yield return done;

			yield return Toils_General.PutCarriedThingInInventory();
		}
	}


	//public class JobDriver_Reload : JobDriver
	//{
	//	private const TargetIndex GearInd = TargetIndex.A;

	//	private const TargetIndex AmmoInd = TargetIndex.B;

	//	private Thing Gear => job.GetTarget(TargetIndex.A).Thing;

	//	public override bool TryMakePreToilReservations(bool errorOnFailed)
	//	{
	//		pawn.ReserveAsManyAsPossible(job.GetTargetQueue(TargetIndex.B), job);
	//		return true;
	//	}

	//	protected override IEnumerable<Toil> MakeNewToils()
	//	{
	//		CompReloadable comp = Gear?.TryGetComp<CompReloadable>();
	//		this.FailOn(() => comp == null);
	//		this.FailOn(() => comp.Wearer != pawn);
	//		this.FailOn(() => !comp.NeedsReload(allowForcedReload: true));
	//		this.FailOnDestroyedOrNull(TargetIndex.A);
	//		this.FailOnIncapable(PawnCapacityDefOf.Manipulation);
	//		Toil getNextIngredient = Toils_General.Label();
	//		yield return getNextIngredient;
	//		foreach (Toil item in ReloadAsMuchAsPossible(comp))
	//		{
	//			yield return item;
	//		}
	//		yield return Toils_JobTransforms.ExtractNextTargetFromQueue(TargetIndex.B);
	//		yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
	//		yield return Toils_Haul.StartCarryThing(TargetIndex.B, putRemainderInQueue: false, subtractNumTakenFromJobCount: true).FailOnDestroyedNullOrForbidden(TargetIndex.B);
	//		yield return Toils_Jump.JumpIf(getNextIngredient, () => !job.GetTargetQueue(TargetIndex.B).NullOrEmpty());
	//		foreach (Toil item2 in ReloadAsMuchAsPossible(comp))
	//		{
	//			yield return item2;
	//		}
	//		Toil toil = new Toil();
	//		toil.initAction = delegate
	//		{
	//			Thing carriedThing = pawn.carryTracker.CarriedThing;
	//			if (carriedThing != null && !carriedThing.Destroyed)
	//			{
	//				pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out var _);
	//			}
	//		};
	//		toil.defaultCompleteMode = ToilCompleteMode.Instant;
	//		yield return toil;
	//	}

	//	private IEnumerable<Toil> ReloadAsMuchAsPossible(CompReloadable comp)
	//	{
	//		Toil done = Toils_General.Label();
	//		yield return Toils_Jump.JumpIf(done, () => pawn.carryTracker.CarriedThing == null || pawn.carryTracker.CarriedThing.stackCount < comp.MinAmmoNeeded(allowForcedReload: true));
	//		yield return Toils_General.Wait(comp.Props.baseReloadTicks).WithProgressBarToilDelay(TargetIndex.A);
	//		Toil toil = new Toil();
	//		toil.initAction = delegate
	//		{
	//			Thing carriedThing = pawn.carryTracker.CarriedThing;
	//			comp.ReloadFrom(carriedThing);
	//		};
	//		toil.defaultCompleteMode = ToilCompleteMode.Instant;
	//		yield return toil;
	//		yield return done;
	//	}
	//}
}
