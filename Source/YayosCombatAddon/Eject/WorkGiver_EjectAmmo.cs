using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace YayosCombatAddon
{
    public class WorkGiver_EjectAmmo : WorkGiver_Scanner
	{
		public override PathEndMode PathEndMode => PathEndMode.Touch;

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
		{
			var designations = pawn.Map.designationManager.AllDesignations;
			for (int i = 0; i < designations.Count; i++)
			{
				if (designations[i].def == YCA_DesignationDefOf.YCA_EjectAmmo)
					yield return designations[i].target.Thing;
			}
		}

		public override bool ShouldSkip(Pawn pawn, bool forced = false) => 
			!pawn.Map.designationManager.AnySpawnedDesignationOfDef(YCA_DesignationDefOf.YCA_EjectAmmo);

		public override bool HasJobOnThing(Pawn pawn, Thing thing, bool forced = false)
		{
			if (!pawn.CanReserve(thing, ignoreOtherReservations: forced))
				return false;

			if (pawn.Map.designationManager.DesignationOn(thing, YCA_DesignationDefOf.YCA_EjectAmmo) == null)
				return false;

			return true;
		}

		public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false) =>
			JobMaker.MakeJob(yayoCombat_Defs.JobDefOf.EjectAmmo, thing);
	}
}
