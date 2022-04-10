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
            soundSucceeded = YCA_SoundDefOf.Designate_EjectAmmo;
            hotKey = KeyBindingDefOf.Misc2;
        }

		#region OVERRIDES
		public override int DraggableDimensions => 2;

        protected override DesignationDef Designation => 
            YCA_DesignationDefOf.EjectAmmo;

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

            var thing = GetEjectableWeapon(cell, Map);
            if (thing == null)
                return false;

            return CanDesignateThing(thing);
        }

        public override void DesignateSingleCell(IntVec3 cell) => 
            DesignateThing(GetEjectableWeapon(cell, Map));

		public override void DesignateThing(Thing thing) =>
            Map.designationManager.AddDesignation(new Designation(thing, Designation));

		public override void SelectedUpdate() => 
            GenUI.RenderMouseoverBracket();
        #endregion

        #region PRIVATE METHODS
        private static Thing GetEjectableWeapon(IntVec3 cell, Map map)
        {
            foreach (Thing thing in cell.GetThingList(map))
            {
                if (thing.TryGetComp<CompReloadable>()?.RemainingCharges > 0)
                    return thing;
            }
            return null;
        }

        private static bool CanEjectAmmo(Thing thing) =>
            thing?.TryGetComp<CompReloadable>()?.RemainingCharges > 0;
        #endregion
    }
}
