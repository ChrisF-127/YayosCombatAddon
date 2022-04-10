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
	internal static class YCA_JobUtility
	{
		public static Thing GetPrimary(this Pawn pawn) =>
			pawn?.equipment?.Primary;

		public static Toil DropCarriedThing()
		{
			var toil = new Toil();
			toil.initAction = () =>
			{
				var actor = toil.GetActor();
				if (actor.carryTracker.CarriedThing != null)
					actor.carryTracker.TryDropCarriedThing(actor.Position, ThingPlaceMode.Near, out _);
			};
			return toil;
		}

		public static Toil EquipStaticOrTargetA(Thing staticThing = null)
		{
			var toil = new Toil();
			toil.initAction = () =>
			{
				var actor = toil.GetActor();
				var targetThingA = actor.CurJob.targetA.Thing;

				var thing = staticThing ?? targetThingA;
				var equipment = actor.equipment;
				var primary = equipment.Primary;
				if (thing is ThingWithComps thingWithComps)
				{
					if (thingWithComps != primary)
					{
						if (primary != null && !equipment.TryTransferEquipmentToContainer(primary, actor.inventory.innerContainer))
							Log.Warning($"{nameof(YayosCombatAddon)}: could not move '{primary}' into inventory");
						thingWithComps.holdingOwner?.Remove(thingWithComps);
						equipment.AddEquipment(thingWithComps);
					}
				}
				else
					Log.Warning($"{nameof(YayosCombatAddon)}: '{thing}' is not {nameof(ThingWithComps)}");
			};
			return staticThing == null ? toil.FailOnDestroyedNullOrForbidden(TargetIndex.A) : toil;
		}

		public static Toil ReloadFromCarriedThing()
		{
			var toil = new Toil();
			toil.initAction = () =>
			{
				var actor = toil.GetActor();
				var targetThingA = actor.CurJob.targetA.Thing;

				var comp = targetThingA?.TryGetComp<CompReloadable>();
				if (comp?.NeedsReload(true) == true)
				{
					var carriedThing = actor.carryTracker.CarriedThing;
					if (carriedThing?.def == comp.AmmoDef)
						comp.ReloadFrom(carriedThing);
					else
						Log.Warning($"{nameof(YayosCombatAddon)}: invalid carried thing: '{carriedThing}' (needed: '{comp.AmmoDef}')");
				}
				else
					Log.Warning($"{nameof(YayosCombatAddon)}: failed getting comp / does not need reloading: '{targetThingA}'");
			};
			return toil.FailOnDestroyedNullOrForbidden(TargetIndex.A);
		}
	}
}
