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
			var comp = TargetThingA?.TryGetComp<CompApparelReloadable>();

			// check if target thing needs reloading and if we got targets to reload from
			if (comp?.NeedsReload(true) == true && job.targetQueueB?.Count > 0)
			{
				// sneaky way for setting wait duration using comp
				Wait.defaultDuration = comp.Props.baseReloadTicks;

				// get ammo from queue
				var maxStackSpaceEver = pawn.carryTracker.MaxStackSpaceEver(comp.AmmoDef);
				for (int i = job.targetQueueB.Count - 1; i >= 0; i--)
				{
					var targetInfo = job.targetQueueB[i];
					var ammoThing = targetInfo.Thing;
					if (ammoThing.def == comp.AmmoDef)
					{
						// check if ammo thing is valid
						if (ammoThing == null || ammoThing.Destroyed || ammoThing.stackCount <= 0)
							throw new Exception($"{nameof(YayosCombatAddon)}:" +
								$" invalid ammo thing" +
								$" ammoDef: {comp.AmmoDef}" +
								$" ammoThing: {ammoThing}" +
								$" destroyed: {ammoThing?.Destroyed}" +
								$" stackCount: {ammoThing?.stackCount}");

						// get total ammo needed
						var ammoNeeded = comp.MaxAmmoNeeded(false);
						// technically this should never be the case, but if it is, stop
						if (ammoNeeded <= 0)
						{
							output = false;
							goto OUT;
						}

						// check if pawn is already carrying ammo
						var alreadyCarryCount = 0;
						var carriedThing = pawn.carryTracker.CarriedThing;
						if (carriedThing != null)
						{
							// carrying invalid thing instead of ammo
							if (carriedThing.def != comp.AmmoDef)
								throw new Exception($"{nameof(YayosCombatAddon)}: " +
									$"carrying invalid thing while trying to get ammo: '{carriedThing}' ({carriedThing.def} / expected {comp.AmmoDef})");

							// get count of carried thing 
							alreadyCarryCount = carriedThing.stackCount;
						}

						// calculate amount of ammo thing to carry from inventory
						var countToCarry = Mathf.Min(ammoNeeded, maxStackSpaceEver) - alreadyCarryCount;
						// limit amount to carry to available stack size or 0 if pawn is already carrying more ammo than required for some reason
						countToCarry = Mathf.Clamp(countToCarry, 0, ammoThing.stackCount);

						// remove ammo thing from target queue if its being used up
						if (countToCarry == ammoThing.stackCount)
							job.targetQueueB.Remove(targetInfo);

						// start carrying ammo thing from inventory
						if (countToCarry > 0)
							pawn.inventory.innerContainer.TryTransferToContainer(ammoThing, pawn.carryTracker.innerContainer, countToCarry);

						// expected stack count of carried thing
						var totalCarry = alreadyCarryCount + countToCarry;

						// check if pawn is carrying expected stack count
						if (totalCarry != pawn.carryTracker.CarriedThing?.stackCount)
							throw new Exception($"{nameof(YayosCombatAddon)}:" +
								$" not carrying expected amount" +
								$" ammoNeeded {ammoNeeded}" +
								$" alreadyCarriedCount {alreadyCarryCount}" +
								$" countToCarry {countToCarry}" +
								$" totalCarry {totalCarry}" +
								$" stackCount {pawn.carryTracker.CarriedThing?.stackCount}");

						// check if there is enough ammo to reload
						if (totalCarry < comp.MinAmmoNeededChecked())
							continue;

						// success
						output = true;

						// already carrying enough or maximum ammo
						if (countToCarry == 0 || totalCarry == maxStackSpaceEver)
							goto OUT;
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
					if (!carriedThing.Destroyed
						&& carriedThing.stackCount > 0
						&& carriedThing.IsAmmo() 
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
