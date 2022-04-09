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
	internal class JobDriver_ReloadFromInventory : JobDriver
	{
		private Toil Wait { get; } = Toils_General.Wait(1).WithProgressBarToilDelay(TargetIndex.A);

		public override bool TryMakePreToilReservations(bool errorOnFailed) =>
			true;

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOn(() => pawn == null);
			this.FailOn(() => pawn.Downed);
			this.FailOnIncapable(PawnCapacityDefOf.Manipulation);

			var next = Toils_General.Label();
			var repeat = Toils_General.Label();
			var done = Toils_General.Label();

			// save currently equipped weapon
			// NEXT:
			// put carried thing into inventory
			// if no more items in queue -> goto DONE: (job ends)
			// get next weapon out of TargetA queue
			// REPEAT:
			// if no ammo found or not reloadable -> goto NEXT: (get next weapon)
			// equip weapon
			// take ammo out of inventory as carried thing
			// wait (progress bar)
			// reload weapon from carried ammo
			// goto REPEAT: (make sure weapon is fully loaded)
			// DONE:
			// switch to original weapon

			var primary = GetPrimary();
			yield return YCA_JobUtility.DropCarriedThing();
			yield return next;
			yield return Toils_General.PutCarriedThingInInventory();
			yield return Toils_Jump.JumpIf(done, () => job.GetTargetQueue(TargetIndex.A).NullOrEmpty());
			yield return Toils_JobTransforms.ExtractNextTargetFromQueue(TargetIndex.A);
			yield return repeat;
			yield return Toils_Jump.JumpIf(next, () => !CheckReloadableAmmo());
			yield return Ammo();
			yield return Equip();
			yield return Wait;
			yield return Reload();
			yield return Toils_Jump.Jump(repeat);
			yield return done;
			yield return Equip(primary);
		}

		private bool CheckReloadableAmmo()
		{
			var comp = TargetThingA?.TryGetComp<CompReloadable>();
			if (comp?.NeedsReload(true) == true)
			{
				// sneaky way for setting wait duration using comp
				Wait.defaultDuration = comp.Props.baseReloadTicks;

				if (pawn.carryTracker.CarriedThing?.def == comp.AmmoDef)
					return true;
				foreach (var thing in pawn.inventory.innerContainer)
					if (thing?.def == comp.AmmoDef)
						return true;

				ReloadUtility.ShowRejectMessage("SY_YCA.NoAmmoInventory".Translate(new NamedArgument(pawn.Name, "pawn"), new NamedArgument(comp.parent.LabelCap, "weapon")));
			}
			return false;
		}

		private Thing GetPrimary() => 
			pawn?.equipment?.Primary;

		private Toil Ammo()
		{
			var toil = new Toil
			{
				initAction = () =>
				{
					var comp = TargetThingA?.TryGetComp<CompReloadable>();
					if (comp?.NeedsReload(true) == true)
					{
						var innerContainer = pawn.inventory.innerContainer;
						for (int i = innerContainer.Count - 1; i >= 0; i--)
						{
							var thing = innerContainer[i];
							if (thing.def == comp.AmmoDef)
							{
								var carriedThing = pawn.carryTracker.CarriedThing;
								if (carriedThing != null && carriedThing.def != comp.AmmoDef) // carrying invalid thing instead of ammo
								{
									Log.Warning($"{nameof(YayosCombatAddon)}: carrying invalid thing while trying to get ammo: '{pawn.carryTracker.CarriedThing}'");
									break;
								}

								var prevCount = carriedThing?.stackCount ?? 0;
								var count = Mathf.Min(comp.AmmoDef.stackLimit - prevCount, thing.stackCount);
								if (count < 0)
									throw new Exception($"{nameof(YayosCombatAddon)}: count should never be less than 0: {count}");
								if (count == 0) // already carrying max amount of ammo
									break;
								
								pawn.inventory.innerContainer.TryTransferToContainer(thing, pawn.carryTracker.innerContainer, count, true);
								carriedThing = pawn.carryTracker.CarriedThing;
								if (carriedThing?.stackCount != prevCount + count)
								{
									Log.Warning($"{nameof(YayosCombatAddon)}: failed to move/merge '{thing}' ({thing.stackCount}) into CarriedThing " +
										$"(carrying: '{carriedThing}' ({carriedThing?.stackCount} / {comp.AmmoDef.stackLimit}; expected: {prevCount + count} ({prevCount} + {count}))");
									break;
								}
							}
						}
					}
				},
			};
			return toil.FailOnDestroyedNullOrForbidden(TargetIndex.A);
		}

		private Toil Equip(Thing staticThing = null)
		{
			var toil = new Toil
			{
				initAction = () =>
				{
					var thing = staticThing ?? TargetThingA;
					var equipment = pawn.equipment;
					var primary = equipment.Primary;
					if (thing is ThingWithComps thingWithComps)
					{
						if (thingWithComps != primary)
						{
							if (primary != null && !equipment.TryTransferEquipmentToContainer(primary, pawn.inventory.innerContainer))
								Log.Warning($"{nameof(YayosCombatAddon)}: could not move '{primary}' into inventory");
							thingWithComps.holdingOwner?.Remove(thingWithComps);
							equipment.AddEquipment(thingWithComps);
						}
					}
					else
						Log.Warning($"{nameof(YayosCombatAddon)}: '{thing}' is not {nameof(ThingWithComps)}");
				},
			};
			return staticThing == null ? toil.FailOnDestroyedNullOrForbidden(TargetIndex.A) : toil;
		}

		private Toil Reload()
		{
			var toil = new Toil
			{
				initAction = () =>
				{
					var comp = TargetThingA?.TryGetComp<CompReloadable>();
					if (comp?.NeedsReload(true) == true)
					{
						var carriedThing = pawn.carryTracker.CarriedThing;
						if (carriedThing?.def == comp.AmmoDef)
							comp.ReloadFrom(carriedThing);
						else
							Log.Warning($"{nameof(YayosCombatAddon)}: invalid carried thing: '{carriedThing}' (needed: '{comp.AmmoDef}')");
					}
					else
						Log.Warning($"{nameof(YayosCombatAddon)}: failed getting comp / does not need reloading: '{TargetThingA}'");
				},
			};
			return toil.FailOnDestroyedNullOrForbidden(TargetIndex.A);
		}
	}
}
