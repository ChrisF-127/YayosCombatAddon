using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace YayosCombatAddon.Jobs
{
    public class JobDriver_EjectAmmo : JobDriver
    {
        private ThingWithComps Gear => (ThingWithComps)job.GetTarget(TargetIndex.A).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            pawn.Reserve(Gear, job);
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            JobDriver_EjectAmmo f = this;
            Thing gear = f.Gear;
            Pawn actor = GetActor();
            CompReloadable comp = gear != null ? gear.TryGetComp<CompReloadable>() : null;
            f.FailOn(() => comp == null);
            //f.FailOn<JobDriver_EjectAmmo>((Func<bool>)(() => comp.AmmoDef == null));
            //f.FailOn<JobDriver_EjectAmmo>((Func<bool>)(() => comp.Props.destroyOnEmpty));
            f.FailOn(() => comp.RemainingCharges <= 0);
            f.FailOnDestroyedOrNull(TargetIndex.A);
            f.FailOnIncapable(PawnCapacityDefOf.Manipulation);
            Toil getNextIngredient = Toils_General.Label();

            yield return getNextIngredient;
            foreach (Toil toil in f.EjectAsMuchAsPossible(comp))
                yield return toil;
            yield return Toils_JobTransforms.ExtractNextTargetFromQueue(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.A).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            yield return Toils_Haul.StartCarryThing(TargetIndex.A, subtractNumTakenFromJobCount: true).FailOnDestroyedNullOrForbidden(TargetIndex.A);
            yield return Toils_Jump.JumpIf(getNextIngredient, () => !job.GetTargetQueue(TargetIndex.A).NullOrEmpty());
            foreach (Toil toil in f.EjectAsMuchAsPossible(comp))
                yield return toil;
            yield return new Toil()
            {
                initAction = () =>
				{
					Thing carriedThing = pawn.carryTracker.CarriedThing;
					if (carriedThing == null || carriedThing.Destroyed)
						return;
					pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out _);
				},
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }

        private IEnumerable<Toil> EjectAsMuchAsPossible(CompReloadable comp)
        {
            Toil done = Toils_General.Label();
            yield return Toils_Jump.JumpIf(done, () => pawn.carryTracker.CarriedThing == null || pawn.carryTracker.CarriedThing.stackCount < comp.MinAmmoNeeded(true));
            yield return Toils_General.Wait(comp.Props.baseReloadTicks).WithProgressBarToilDelay(TargetIndex.A);
            yield return new Toil()
            {
                initAction = (Action)(() => reloadUtility.EjectAmmoAction(GetActor(), comp)),
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return done;
        }
    }
}
