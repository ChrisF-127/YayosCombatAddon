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
				var weapons = new List<Thing>(pawn.equipment.AllEquipmentListForReading);
#warning TODO SimpleSidearms compatibility

				foreach (var thing in weapons)
				{
					var comp = thing.TryGetComp<CompReloadable>();
					if (comp != null)
					{
						bool disabled = false;
						string disableReason = null;

						if (pawn.Downed) // should actually never happen, since pawns can't be drafted when downed
						{
							disabled = true;
							disableReason = "pawnDowned".Translate();
						}
						else if (comp.RemainingCharges >= comp.MaxCharges)
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

							action = () => ReloadUtility.TryForcedReloadFromInventory(comp),
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

								action = () => ReloadUtility.TryForcedReloadFromSurrounding(comp),
							};
						}
					}
					break;
				}
			}

			foreach (var gizmo in __result)
				yield return gizmo;
		}
	}
}
