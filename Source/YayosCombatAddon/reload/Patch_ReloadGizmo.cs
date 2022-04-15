using HarmonyLib;
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
	[HarmonyPatch(typeof(Pawn_DraftController), "GetGizmos")]
	internal class Pawn_DraftController_GetGizmos
	{
		[HarmonyPostfix]
#pragma warning disable IDE0051 // Remove unused private members
		static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn_DraftController __instance)
#pragma warning restore IDE0051 // Remove unused private members
		{
			if (yayoCombat.yayoCombat.ammo
				&& __instance?.pawn is Pawn pawn
				&& pawn.Faction?.IsPlayer == true
				&& pawn.Drafted
				&& !pawn.WorkTagIsDisabled(WorkTags.Violent))
			{
				var comps = new List<CompReloadable>();
				foreach (var thing in pawn.equipment.AllEquipmentListForReading)
				{
					var comp = thing.TryGetComp<CompReloadable>();
					if (comp != null)
						comps.Add(comp);
				}

				if (comps.Count > 0)
					yield return new Command_ReloadActions(pawn);
			}

			foreach (var gizmo in __result)
				yield return gizmo;
		}
	}

	internal class Command_ReloadActions : Command_Action
	{
		private readonly Pawn Pawn = null;
		private readonly IEnumerable<Thing> ReloadableThings;

		public Command_ReloadActions(Pawn pawn)
		{
			defaultLabel = "SY_YCA.ReloadGizmo_title".Translate();
			defaultDesc = "SY_YCA.ReloadGizmo_desc".Translate();
			icon = YCA_Textures.AmmoReload;

			Pawn = pawn;
			ReloadableThings = pawn.GetAllReloadableThings();

			action = () => ReloadUtility.ReloadFromInventory(pawn, ReloadableThings, true);
		}

		public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
		{
			get
			{
				string inventory_label, inventory_tooltip;
				string surrounding_label, surrounding_tooltip;

				if (Main.SimpleSidearmsCompatibility)
				{
					inventory_label = "SY_YCA.ReloadAllWeaponFromInventory_label";
					inventory_tooltip = "SY_YCA.ReloadAllWeaponFromInventory_tooltip";

					surrounding_label = "SY_YCA.ReloadAllWeaponFromSurrounding_label";
					surrounding_tooltip = "SY_YCA.ReloadAllWeaponFromSurrounding_tooltip";
				}
				else
				{
					inventory_label = "SY_YCA.ReloadWeaponFromInventory_label";
					inventory_tooltip = "SY_YCA.ReloadWeaponFromInventory_tooltip";

					surrounding_label = "SY_YCA.ReloadWeaponFromSurrounding_label";
					surrounding_tooltip = "SY_YCA.ReloadWeaponFromSurrounding_tooltip";
				}

				yield return new FloatMenuOption(
					inventory_label.Translate(),
					() => ReloadUtility.ReloadFromInventory(Pawn, ReloadableThings, true))
				{
					tooltip = inventory_tooltip.Translate(),
				};
				if (yayoCombat.yayoCombat.supplyAmmoDist >= 0)
				{
					yield return new FloatMenuOption(
						surrounding_label.Translate(),
						() => ReloadUtility.ReloadFromSurrounding(Pawn, ReloadableThings, true, false))
					{
						tooltip = surrounding_tooltip.Translate(),
					};
				}

				yield return new FloatMenuOption(
					"SY_YCA.RestockAmmoFromSurrounding_label".Translate(),
					() => ReloadUtility.RestockInventoryFromSurrounding(Pawn)) 
				{ 
					tooltip = "SY_YCA.RestockAmmoFromSurrounding_tooltip".Translate(), 
				};
			}
		}
	}
}
