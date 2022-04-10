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
			Log.Message($"PotentialWorkThingsGlobal");
			var designations = pawn.Map.designationManager.allDesignations;
			for (int i = 0; i < designations.Count; i++)
			{
				if (designations[i].def == YCA_DesignationDefOf.EjectAmmo)
					yield return designations[i].target.Thing;
			}
		}

		public override bool ShouldSkip(Pawn pawn, bool forced = false)
		{
			var x = !pawn.Map.designationManager.AnySpawnedDesignationOfDef(YCA_DesignationDefOf.EjectAmmo);
			Log.Message($"ShouldSkip {x}");
			return x;
		}

		public override bool HasJobOnThing(Pawn pawn, Thing thing, bool forced = false)
		{
			Log.Message("HasJobOnThing");
			if (!pawn.CanReserve(thing, ignoreOtherReservations: forced))
				return false;

			Log.Message("-> CanReserve");
			if (pawn.Map.designationManager.DesignationOn(thing, YCA_DesignationDefOf.EjectAmmo) == null)
				return false;

			Log.Message("-> DesignationOn");
			return true;
		}

		public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
		{
			var job = JobMaker.MakeJob(yayoCombat_Defs.JobDefOf.EjectAmmo, thing);
			Log.Message($"JobOnThing {thing}");
			return job;
		}
	}
}
