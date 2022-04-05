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
		public static readonly Texture2D AmmoReloadInventory = ContentFinder<Texture2D>.Get("yy_ammo_reload_inventory", true);
		public static readonly Texture2D AmmoReloadSurrounding = ContentFinder<Texture2D>.Get("yy_ammo_reload_surrounding", true);
	}

	[HarmonyPatch(typeof(Pawn_DraftController), "GetGizmos")]
	internal class Pawn_DraftController_GetGizmos
	{
		[HarmonyPostfix]
		static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn_DraftController __instance)
		{
			if (yayoCombat.yayoCombat.ammo
				&& Main.showReloadWeaponGizmo
				&& __instance?.pawn is Pawn pawn
				&& pawn.Faction?.IsPlayer == true
				&& pawn.Drafted
				&& !pawn.WorkTagIsDisabled(WorkTags.Violent))
			{
				var comps = new List<CompReloadable>();
				var weapons = pawn.equipment.AllEquipmentListForReading;

#warning TODO SimpleSidearms compatibility: get all weapons in inventory

				foreach (var thing in weapons)
				{
					var comp = thing.TryGetComp<CompReloadable>();
					if (comp != null)
						comps.Add(comp);
				}

				if (comps.Count > 0)
				{
					bool disabled = false;
					string disableReason = null;

					if (pawn.Downed) // should actually never happen, since pawns can't be drafted when downed
					{
						disabled = true;
						disableReason = "pawnDowned".Translate();
					}
					else if (!AnyWeaponRequiresReloading(comps))
					{
						disabled = true;
						disableReason = "ammoFull".Translate();
					}

					yield return new Command_Action()
					{
						defaultLabel = "reloadWeaponInventory_title".Translate(),
						defaultDesc = "reloadWeaponInventory_desc".Translate(),
						disabled = disabled,
						disabledReason = disableReason,
						icon = GizmoTexture.AmmoReloadInventory,

						action = () => ReloadUtility.TryForcedReloadFromInventory(pawn, comps),
					};

					if (yayoCombat.yayoCombat.supplyAmmoDist > 0)
					{
						yield return new Command_Action()
						{
							defaultLabel = "reloadWeaponSurrounding_title".Translate(),
							defaultDesc = "reloadWeaponSurrounding_desc".Translate(),
							disabled = disabled,
							disabledReason = disableReason,
							icon = GizmoTexture.AmmoReloadSurrounding,

							action = () => ReloadUtility.TryForcedReloadFromSurrounding(pawn, comps),
						};
					}
				}
			}

			foreach (var gizmo in __result)
				yield return gizmo;

			bool AnyWeaponRequiresReloading(IEnumerable<CompReloadable> comps)
			{
				foreach (var comp in comps)
					if (comp.RemainingCharges < comp.MaxCharges)
						return true;
				return false;
			}
		}
	}
}
