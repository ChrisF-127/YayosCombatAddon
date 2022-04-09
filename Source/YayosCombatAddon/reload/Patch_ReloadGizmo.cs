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
	[StaticConstructorOnStartup]
	internal static class GizmoTexture
	{
		public static readonly Texture2D AmmoReload = ContentFinder<Texture2D>.Get("YCA_AmmoReload", true);
	}

	[HarmonyPatch(typeof(Pawn_DraftController), "GetGizmos")]
	internal class Pawn_DraftController_GetGizmos
	{
		[HarmonyPostfix]
#pragma warning disable IDE0051 // Remove unused private members
		static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn_DraftController __instance)
#pragma warning restore IDE0051 // Remove unused private members
		{
			if (yayoCombat.yayoCombat.ammo
				&& Main.ShowReloadWeaponGizmo
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
		private readonly List<CompReloadable> EquippedComps;
		private readonly List<CompReloadable> EquippedAndInventoryComps;

		public Command_ReloadActions(Pawn pawn)
		{
			Pawn = pawn;

			EquippedComps = new List<CompReloadable>();
			foreach (var thing in pawn.equipment.AllEquipmentListForReading)
			{
				var comp = thing.TryGetComp<CompReloadable>();
				if (comp != null)
					EquippedComps.Add(comp);
			}

			if (Main.ReloadAllWeaponsInInventoryOption)
			{
				EquippedAndInventoryComps = new List<CompReloadable>(EquippedComps);
				foreach (var thing in pawn.inventory.innerContainer)
				{
					var comp = thing.TryGetComp<CompReloadable>();
					if (comp != null)
						EquippedAndInventoryComps.Add(comp);
				}
			}

			defaultLabel = "SY_YCA.ReloadGizmo_title".Translate();
			defaultDesc = "SY_YCA.ReloadGizmo_desc".Translate();
			icon = GizmoTexture.AmmoReload;

			action = () => ReloadUtility.TryForcedReloadFromInventory(pawn, EquippedComps);
		}

		public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
		{
			get
			{
				if (Main.ReloadAllWeaponsInInventoryOption)
				{
					yield return new FloatMenuOption(
						"SY_YCA.ReloadAllWeaponFromInventory_label".Translate(),
						() => ReloadUtility.TryForcedReloadFromInventory(Pawn, EquippedAndInventoryComps))
					{
						tooltip = "SY_YCA.ReloadAllWeaponFromInventory_tooltip".Translate(),
					};
					if (yayoCombat.yayoCombat.supplyAmmoDist >= 0)
					{
						yield return new FloatMenuOption(
							"SY_YCA.ReloadAllWeaponFromSurrounding_label".Translate(),
							() => ReloadUtility.TryForcedReloadFromSurrounding(Pawn, EquippedAndInventoryComps))
						{
							tooltip = "SY_YCA.ReloadAllWeaponFromSurrounding_tooltip".Translate(),
						};
					}
				}
				else
				{
					yield return new FloatMenuOption(
					"SY_YCA.ReloadWeaponFromInventory_label".Translate(),
					() => ReloadUtility.TryForcedReloadFromInventory(Pawn, EquippedComps))
					{
						tooltip = "SY_YCA.ReloadWeaponFromInventory_tooltip".Translate(),
					};
					if (yayoCombat.yayoCombat.supplyAmmoDist >= 0)
					{
						yield return new FloatMenuOption(
							"SY_YCA.ReloadWeaponFromSurrounding_label".Translate(),
							() => ReloadUtility.TryForcedReloadFromSurrounding(Pawn, EquippedComps))
						{
							tooltip = "SY_YCA.ReloadWeaponFromSurrounding_tooltip".Translate(),
						};
					}
				}

				yield return new FloatMenuOption(
					"SY_YCA.RestockAmmoFromSurrounding_label".Translate(),
					() => ReloadUtility.TryRestockInventoryFromSurrounding(Pawn)) 
				{ 
					tooltip = "SY_YCA.RestockAmmoFromSurrounding_tooltip".Translate(), 
				};
			}
		}
	}
}
