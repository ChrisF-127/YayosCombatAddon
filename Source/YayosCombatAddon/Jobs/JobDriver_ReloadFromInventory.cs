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

namespace YayosCombatAddon
{
	internal class JobDriver_ReloadFromInventory : JobDriver
	{
		private Toil Wait { get; } = Toils_General.Wait(1).WithProgressBarToilDelay(TargetIndex.A);

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

			var primary = pawn.GetPrimary();
			yield return YCA_JobUtility.DropCarriedThing();
			yield return next;
			yield return Toils_Jump.JumpIf(done, () => job.GetTargetQueue(TargetIndex.A).NullOrEmpty());
			yield return Toils_JobTransforms.ExtractNextTargetFromQueue(TargetIndex.A);
			yield return repeat;
			yield return Toils_Jump.JumpIf(next, () => !TryMoveAmmoToCarriedThing());
			yield return YCA_JobUtility.EquipStaticOrTargetA();
			yield return Wait;
			yield return YCA_JobUtility.ReloadFromCarriedThing();
			yield return StowCarriedAmmoAndReaddToQueue();
			yield return Toils_Jump.Jump(repeat);
			yield return done;
			yield return YCA_JobUtility.EquipStaticOrTargetA(primary);
		}

		private bool TryMoveAmmoToCarriedThing()
		{
			var output = false;
			var comp = TargetThingA?.TryGetComp<CompReloadable>();
			if (comp?.NeedsReload(true) == true && job.targetQueueB?.Count > 0)
			{
				// sneaky way for setting wait duration using comp
				Wait.defaultDuration = comp.Props.baseReloadTicks;

				// get ammo from queue
				for (int i = job.targetQueueB.Count - 1; i >= 0; i--)
				{
					var targetInfo = job.targetQueueB[i];
					var ammoThing = targetInfo.Thing;
					if (ammoThing.def == comp.AmmoDef)
					{
						var prevCount = 0;
						var carriedThing = pawn.carryTracker.CarriedThing;
						if (carriedThing != null)
						{
							// carrying invalid thing instead of ammo
							if (carriedThing.def != comp.AmmoDef)
								throw new Exception($"{nameof(YayosCombatAddon)}: " +
									$"carrying invalid thing while trying to get ammo: '{carriedThing}' ({carriedThing.def} / expected {comp.AmmoDef})");

							prevCount = carriedThing.stackCount;
						}

						var maxReq = comp.AmmoDef.stackLimit - prevCount;

						// count should never be less than 0, neither should the stackCount be 0
						if (maxReq < 0 || ammoThing.stackCount == 0)
							throw new Exception($"{nameof(YayosCombatAddon)}: " +
								$"count less than 0 or empty stack: count: {maxReq} stackLimit: {comp.AmmoDef.stackLimit} prevCount: {prevCount} stackCount: {ammoThing.stackCount}");

						// already carrying max amount of ammo
						if (maxReq == 0)
						{
							output = true;
							goto OUT;
						}

						var count = Mathf.Min(maxReq, ammoThing.stackCount);

						// remove from queue if used up
						if (count >= ammoThing.stackCount)
							job.targetQueueB.Remove(targetInfo);

						// start carrying thing from inventory
						pawn.inventory.innerContainer.TryTransferToContainer(ammoThing, pawn.carryTracker.innerContainer, count, true);

						// check carried thing
						carriedThing = pawn.carryTracker.CarriedThing;
						if (carriedThing?.stackCount != prevCount + count)
						{
							Log.Warning($"{nameof(YayosCombatAddon)}: failed to move/merge '{ammoThing}' ({ammoThing.stackCount}) into CarriedThing " +
								$"(carrying: '{carriedThing}' ({carriedThing?.stackCount} / {comp.AmmoDef.stackLimit}; expected: {prevCount + count} ({prevCount} + {count}))");
							break;
						}

						// success
						output = true;
					}
				}
			}
			OUT:
			return output;
		}

		private Toil StowCarriedAmmoAndReaddToQueue()
		{
			var toil = new Toil();
			toil.initAction = () =>
			{
				var actor = toil.GetActor();
				var carriedThing = actor.carryTracker.CarriedThing;
				if (carriedThing != null)
				{
					if (carriedThing.IsAmmo() 
						&& actor.carryTracker.innerContainer.TryTransferToContainer(carriedThing, actor.inventory.innerContainer))
						job.targetQueueB.Add(carriedThing);
					else
						actor.carryTracker.TryDropCarriedThing(actor.Position, actor.carryTracker.CarriedThing.stackCount, ThingPlaceMode.Near, out var _);
				}
			};
			return toil;
		}
	}
}
