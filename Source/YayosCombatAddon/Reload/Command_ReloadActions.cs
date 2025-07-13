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

			action = () => ReloadUtility.TryReloadFromInventory(pawn, ReloadableThings, true);
		}

		public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
		{
			get
			{
				string inventory_label, inventory_tooltip;
				string surrounding_label, surrounding_tooltip;

				if (YayosCombatAddon.SimpleSidearmsCompatibility)
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
					() => ReloadUtility.TryReloadFromInventory(Pawn, ReloadableThings, true))
				{
					tooltip = inventory_tooltip.Translate(),
				};
				yield return new FloatMenuOption(
					surrounding_label.Translate(),
					() => ReloadUtility.TryReloadFromSurrounding(Pawn, ReloadableThings, true, true))
				{
					tooltip = surrounding_tooltip.Translate(),
				};

				yield return new FloatMenuOption(
					"SY_YCA.RestockAmmoFromSurrounding_label".Translate(),
					() => InventoryUtility.RestockInventoryFromSurrounding(Pawn)) 
				{ 
					tooltip = "SY_YCA.RestockAmmoFromSurrounding_tooltip".Translate(), 
				};
			}
		}
	}
}
