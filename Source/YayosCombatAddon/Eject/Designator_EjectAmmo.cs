using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace YayosCombatAddon
{
    public class Designator_EjectAmmo : Designator
    {
        public Designator_EjectAmmo()
        {
            defaultLabel = "SY_YCA.EjectAmmo_label".Translate();
            defaultDesc = "SY_YCA.EjectAmmo_desc".Translate();
            icon = YCA_Textures.AmmoEject;
            soundDragSustain = SoundDefOf.Designate_DragStandard;
            soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
            useMouseIcon = true;
            soundSucceeded = YCA_SoundDefOf.YCA_Designate_EjectAmmo;
            hotKey = KeyBindingDefOf.Misc2;
        }

		#region OVERRIDES
		public override DesignationDef Designation => 
            YCA_DesignationDefOf.YCA_EjectAmmo;

		public override AcceptanceReport CanDesignateThing(Thing thing)
        {
            if (Map.designationManager.DesignationOn(thing, Designation) != null)
                return false;

            return CanEjectAmmo(thing);
        }

		public override AcceptanceReport CanDesignateCell(IntVec3 cell)
        {
            if (!cell.InBounds(Map) || cell.Fogged(Map))
                return false;

            var things = GetEjectableWeapons(cell, Map);
            if (things?.Count() > 0)
			{
                foreach (var thing in things)
                    if (CanDesignateThing(thing))
                        return true;
            }

            return false;
        }

        public override void DesignateSingleCell(IntVec3 cell)
        {
            foreach (var thing in GetEjectableWeapons(cell, Map))
                DesignateThing(thing);
        }

		public override void DesignateThing(Thing thing) =>
            Map.designationManager.AddDesignation(new Designation(thing, Designation));

		public override void SelectedUpdate() => 
            GenUI.RenderMouseoverBracket();
        #endregion

        #region PRIVATE METHODS
        private static IEnumerable<Thing> GetEjectableWeapons(IntVec3 cell, Map map)
        {
            foreach (Thing thing in cell.GetThingList(map))
            {
                if (CanEjectAmmo(thing))
                    yield return thing;
            }
        }

        private static bool CanEjectAmmo(Thing thing)
		{
            var reloadable = thing?.TryGetComp<CompApparelReloadable>();
			return reloadable != null
				&& reloadable.AmmoDef.IsAmmo()
				&& reloadable.EjectableAmmo() > 0;
		}
		#endregion
	}
}
