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
		public static Toil DropCarriedThing()
		{
			Toil toil = new Toil();
			toil.initAction = () =>
			{
				Pawn actor = toil.GetActor();
				if (actor.carryTracker.CarriedThing != null)
					actor.carryTracker.TryDropCarriedThing(actor.Position, ThingPlaceMode.Near, out _);
			};
			return toil;
		}
	}
}
