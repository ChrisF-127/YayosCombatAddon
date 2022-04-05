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

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return true;
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			CompReloadable comp = Gear?.TryGetComp<CompReloadable>();
			Log.Message($"Comp: '{comp}'");
			this.FailOn(() => comp == null);
			this.FailOn(() => comp.Wearer != pawn);
			this.FailOn(() => !comp.NeedsReload(true));

			//Toil done = Toils_General.Label();
			//yield return Toils_Jump.JumpIf(done, () => pawn.carryTracker.CarriedThing == null || pawn.carryTracker.CarriedThing.stackCount < comp.MinAmmoNeeded(allowForcedReload: true));
			yield return Toils_General.Wait(comp.Props.baseReloadTicks).WithProgressBarToilDelay(TargetIndex.A);

			Log.Message($"pawn.carryTracker.CarriedThing: '{pawn.carryTracker.CarriedThing}'");
			if (pawn.carryTracker.CarriedThing != null)
				yield return Toils_General.PutCarriedThingInInventory();

			yield return new Toil
			{
				initAction = delegate
				{
					Log.Message($"DoToil: '{comp}' {comp.NeedsReload(false)}");
					while (comp.NeedsReload(false))
					{
						Thing carriedThing = null;
						foreach (var thing in pawn.inventory.innerContainer)
						{
							if (thing.def == comp.AmmoDef && pawn.inventory.innerContainer.TryTransferToContainer(thing, pawn.carryTracker.innerContainer))
							{
								carriedThing = thing;
								break;
							}
						}

						if (pawn.carryTracker.CarriedThing != carriedThing)
							Log.Error($"{pawn.carryTracker.CarriedThing} != {carriedThing}");

						if (carriedThing != null && carriedThing.def == comp.AmmoDef)
							comp.ReloadFrom(carriedThing);
						else
							return;
					}
				},
				defaultCompleteMode = ToilCompleteMode.Instant
			};
			//yield return done;

			yield return new Toil
			{
				initAction = delegate
				{
					Log.Message($"EndToil: '{comp}' {comp.NeedsReload(false)}");
					Thing carriedThing = pawn.carryTracker.CarriedThing;
					if (carriedThing != null && !carriedThing.Destroyed)
					{
						if (!pawn.carryTracker.innerContainer.TryTransferToContainer(carriedThing, pawn.inventory.innerContainer))
							pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out var _);
					}
				},
				defaultCompleteMode = ToilCompleteMode.Instant
			};
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
