using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
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

		public override bool TryMakePreToilReservations(bool errorOnFailed) =>
			true;

		public override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOn(() => pawn == null);
			this.FailOn(() => pawn.Downed);
			this.FailOnIncapable(PawnCapacityDefOf.Manipulation);

			var next = Toils_General.Label();
			var repeat = Toils_General.Label();
			var done = Toils_General.Label();

			// save currently equipped weapon
			// NEXT:
			// drop carried thing
			// if no more items in queue -> goto DONE: (job ends)
			// get next weapon out of TargetA queue
			// REPEAT:
			// if no ammo found or not reloadable -> goto NEXT: (get next weapon)
			// get next ammo out of TargetB queue
			// go to ammo
			// equip weapon
			// start carrying ammo
			// wait (progress bar)
			// reload weapon from carried ammo
			// switch to original weapon (in case job is interrupted)
			// goto REPEAT: (make sure weapon is fully loaded)
			// DONE:

			StartingPosition = pawn.Position;
			var primary = pawn.GetPrimary();
			yield return next;
			yield return YCA_JobUtility.DropCarriedThing();
			yield return Toils_Jump.JumpIf(done, () => job.GetTargetQueue(TargetIndex.A).NullOrEmpty());
			yield return Toils_JobTransforms.ExtractNextTargetFromQueue(TargetIndex.A);
			yield return repeat;
			yield return Toils_Jump.JumpIf(next, () => !CheckReloadableAmmo() || job.GetTargetQueue(TargetIndex.B).NullOrEmpty());
			yield return Toils_JobTransforms.ExtractNextTargetFromQueue(TargetIndex.B);
			yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
			//yield return Toils_Haul.StartCarryThing(TargetIndex.B, subtractNumTakenFromJobCount: true).FailOnDestroyedNullOrForbidden(TargetIndex.B);
			yield return StartCarryAmmoFromGround(); // custom method instead of Toils_Haul.StartCarryThing because it allows picking up full stacks
			yield return YCA_JobUtility.EquipStaticOrTargetA();
			yield return Wait;
			yield return YCA_JobUtility.ReloadFromCarriedThing();
			yield return YCA_JobUtility.EquipStaticOrTargetA(primary);
			yield return Toils_Jump.Jump(repeat);
			yield return done;
		}

		private bool CheckReloadableAmmo()
		{
			var comp = TargetThingA?.TryGetComp<CompReloadable>();
			if (comp?.NeedsReload(true) == true)
			{
				// sneaky way for setting wait duration using comp
				Wait.defaultDuration = comp.Props.baseReloadTicks;

				var ammoList = RefuelWorkGiverUtility.FindEnoughReservableThings(
					pawn,
					pawn.Position,
					new IntRange(comp.MinAmmoNeeded(true), comp.MaxAmmoNeeded(true)),
					t => t.def == comp.AmmoDef 
						&& (!pawn.Drafted // non-drafted pawns will look everywhere
							|| IntVec3Utility.DistanceTo(pawn.Position, t.Position) <= yayoCombat.yayoCombat.supplyAmmoDist
							|| IntVec3Utility.DistanceTo(StartingPosition, t.Position) <= yayoCombat.yayoCombat.supplyAmmoDist));

				job.targetQueueB?.Clear();
				if (ammoList?.Count > 0)
				{
					job.count = comp.MaxCharges - comp.RemainingCharges;
					foreach (var thing in ammoList)
						job.AddQueuedTarget(TargetIndex.B, thing);
					pawn.ReserveAsManyAsPossible(job.GetTargetQueue(TargetIndex.B), job);
					return true;
				}

				if (job.overeat) // used to decide whether to show messages or not
				{
					Log.Message($"ReloadFromSurrounding forced: {job.overeat}");
					ReloadUtility.ShowRejectMessage("SY_YCA.NoAmmoNearby".Translate(new NamedArgument(pawn.Name, "pawn"), new NamedArgument(comp.parent.LabelCap, "weapon")));
				}
			}
			return false;
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
	}
}
