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
			yield return StartCarryAmmoFromGround(); // custom method instead of Toils_Haul.StartCarryThing because it allows picking up full stacks
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
			if (comp?.NeedsReload(true) == true && job.targetQueueB?.Count > 0)
			{
				// sneaky way for setting wait duration using comp
				Wait.defaultDuration = comp.Props.baseReloadTicks;

				// sort by distance
				job.targetQueueB = job.targetQueueB.OrderBy(t => IntVec3Utility.DistanceTo(pawn.Position, t.Thing.Position)).ToList();

				// get ammo from queue
				foreach (var targetInfo in job.targetQueueB)
				{
					var ammoThing = targetInfo.Thing;
					if (ammoThing.def == comp.AmmoDef)
					{
						job.targetB = targetInfo;
						job.count = comp.MaxCharges - comp.RemainingCharges;

						if (ammoThing.stackCount < job.count)
							job.targetQueueB.Remove(targetInfo);

						output = true;
						goto OUT;
					}
				}
			}
			OUT:
			return output;
		}

		private Toil StartCarryAmmoFromGround()
		{
			var toil = new Toil
			{
				initAction = () =>
				{
					var comp = TargetThingA?.TryGetComp<CompReloadable>();
					if (comp?.NeedsReload(true) == true)
					{
						var thing = TargetThingB;
						if (thing != null && !Toils_Haul.ErrorCheckForCarry(pawn, thing))
						{
							var carriedThing = pawn.carryTracker.CarriedThing;
							if (carriedThing != null && carriedThing.def != comp.AmmoDef) // carrying invalid thing instead of ammo
							{
								Log.Warning($"{nameof(YayosCombatAddon)}: carrying invalid thing while trying to pick up ammo: '{carriedThing}'");
								return;
							}

							var prevCount = carriedThing?.stackCount ?? 0;
							var count = Mathf.Min(comp.AmmoDef.stackLimit - prevCount, thing.stackCount, job.count);
							if (count < 0)
								throw new Exception($"{nameof(YayosCombatAddon)}: count should never be less than 0: {count}");
							if (count == 0) // already carrying max amount of ammo
								return;

							int num = pawn.carryTracker.innerContainer.TryAdd(thing.SplitOff(count), count);
							thing.def.soundPickup.PlayOneShot(new TargetInfo(thing.Position, pawn.Map));

							carriedThing = pawn.carryTracker.CarriedThing;
							pawn.Reserve(carriedThing, job);

							if (carriedThing?.stackCount != prevCount + count || num != count)
							{
								Log.Warning($"{nameof(YayosCombatAddon)}: failed to move/merge '{thing}' ({thing.stackCount}) into CarriedThing " +
									$"(carrying: '{carriedThing}' ({carriedThing?.stackCount} / {comp.AmmoDef.stackLimit}; expected: {prevCount + count} ({prevCount} + {count} // {num}))");
								return;
							}
						}
					}
				},
			};
			return toil.FailOnDestroyedNullOrForbidden(TargetIndex.A).FailOnDestroyedNullOrForbidden(TargetIndex.B);
		}

		private Toil DropCarriedAmmoAndReaddToQueue()
		{
			var toil = new Toil();
			toil.initAction = () =>
			{
				var actor = toil.GetActor();
				var carriedThing = actor.carryTracker.CarriedThing;
				if (carriedThing != null 
					&& actor.carryTracker.TryDropCarriedThing(actor.Position, actor.carryTracker.CarriedThing.stackCount, ThingPlaceMode.Near, out var _)
					&& carriedThing.IsAmmo())
					job.targetQueueB.Add(carriedThing);
			};
			return toil;
		}
	}
}
