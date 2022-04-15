using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace YayosCombatAddon
{
    public class JobDriver_EjectAmmo : JobDriver
    {
        private ThingWithComps Gear => (ThingWithComps)job.GetTarget(TargetIndex.A).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            pawn.Reserve(Gear, job);
            return true;
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            var comp = Gear?.TryGetComp<CompReloadable>();
            job.count = Gear.stackCount;

            this.FailOn(() => comp == null);
            this.FailOn(() => comp.RemainingCharges <= 0);
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnIncapable(PawnCapacityDefOf.Manipulation);

            var done = Toils_General.Label();

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.A).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            yield return Toils_Jump.JumpIf(done, () => comp.RemainingCharges <= 0);
            yield return Toils_General.Wait(comp.Props.baseReloadTicks / 2).WithProgressBarToilDelay(TargetIndex.A);
            yield return new Toil()
            {
                initAction = () =>
                {
                    AmmoUtility.EjectAmmo(GetActor(), comp);
                    Map.designationManager.DesignationOn(comp.parent, YCA_DesignationDefOf.EjectAmmo)?.Delete();
                }
            };
            yield return done;
        }
    }
}
