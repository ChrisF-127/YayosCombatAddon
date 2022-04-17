using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace YayosCombatAddon
{
	internal class JobDriver_ReloadFromSurrounding : JobDriver
	{
		private Toil Wait { get; } = Toils_General.Wait(1).WithProgressBarToilDelay(TargetIndex.A);
		private IntVec3 StartingPosition { get; set; }

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			pawn.ReserveAsManyAsPossible(job.GetTargetQueue(TargetIndex.B), job);
			return true;
		}

		public override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOn(() => pawn == null);
			this.FailOn(() => pawn.Downed);
			this.FailOnIncapable(PawnCapacityDefOf.Manipulation);

			var next = Toils_General.Label();
			var repeat = Toils_General.Label();
			var done = Toils_General.Label();

			StartingPosition = pawn.Position;
			var primary = pawn.GetPrimary();
			yield return YCA_JobUtility.DropCarriedThing();
			yield return next;
			yield return Toils_Jump.JumpIf(done, () => job.GetTargetQueue(TargetIndex.A).NullOrEmpty());
			yield return Toils_JobTransforms.ExtractNextTargetFromQueue(TargetIndex.A);
			yield return repeat;
			yield return Toils_Jump.JumpIf(next, () => !TryMoveAmmoToCarriedThing());
			yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
			yield return Toils_Haul.StartCarryThing(TargetIndex.B, subtractNumTakenFromJobCount: true).FailOnDestroyedNullOrForbidden(TargetIndex.B);
			yield return YCA_JobUtility.EquipStaticOrTargetA();
			yield return Wait;
			yield return YCA_JobUtility.ReloadFromCarriedThing();
			yield return DropCarriedAmmoAndReaddToQueue();
			yield return YCA_JobUtility.EquipStaticOrTargetA(primary);
			yield return Toils_Jump.Jump(repeat);
			yield return done;
		}

		private bool TryMoveAmmoToCarriedThing()
		{
			var output = false;
			var comp = TargetThingA?.TryGetComp<CompReloadable>();

			// check if target thing needs reloading and if we got targets to reload from
			if (comp?.NeedsReload(true) == true && job.targetQueueB?.Count > 0)
			{
				// sneaky way for setting wait duration using comp
				Wait.defaultDuration = comp.Props.baseReloadTicks;

				// get ammo from queue
				foreach (var targetInfo in job.targetQueueB)
				{
					var ammoThing = targetInfo.Thing;
					if (ammoThing.def == comp.AmmoDef)
					{
						// check if comp needs reloading - this should always be the case at this point
						var minAmmoNeeded = comp.MinAmmoNeededChecked();

						// check if the stack is big enough to reload thing
						if (ammoThing.stackCount < minAmmoNeeded)
							continue;

						// set ammoThing as new target
						job.targetB = targetInfo;
						// set total count of stuff we wish to pick up, this can be more than ammoThing's stack actually holds
						job.count = (comp.MaxCharges - comp.RemainingCharges) * minAmmoNeeded;

						// if the stack is picked up completely, remove it from target queue
						if (ammoThing.stackCount <= job.count)
							job.targetQueueB.Remove(targetInfo);

						// we got something to pick up, so let's go there
						output = true;
						goto OUT;
					}
				}
			}
			OUT:
			return output;
		}

		private Toil DropCarriedAmmoAndReaddToQueue()
		{
			return new Toil
			{
				initAction = () =>
				{
					// if pawn is carrying ammo, drop it, reserve it and add it back to target queue, we might need it later for a different weapon
					var carriedThing = pawn.carryTracker.CarriedThing;
					if (carriedThing != null
						&& pawn.carryTracker.TryDropCarriedThing(pawn.Position, pawn.carryTracker.CarriedThing.stackCount, ThingPlaceMode.Near, out var _)
						&& carriedThing.IsAmmo())
					{
						pawn.Reserve(carriedThing, job);
						job.targetQueueB.Add(carriedThing);
					}
				}
			};
		}
	}
}
