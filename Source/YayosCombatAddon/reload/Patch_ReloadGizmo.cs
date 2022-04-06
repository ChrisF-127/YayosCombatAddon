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
					yield return new Command_ReloadActions(pawn, comps);
			}

			foreach (var gizmo in __result)
				yield return gizmo;
		}
	}

	internal class Command_ReloadActions : Command_Action
	{
		private readonly Pawn Pawn = null;
		private readonly IEnumerable<CompReloadable> Comps;

		public Command_ReloadActions(Pawn pawn, IEnumerable<CompReloadable> comps)
		{
			Pawn = pawn;
			Comps = comps;

			defaultLabel = "SY_YCA.ReloadGizmo_title".Translate();
			defaultDesc = "SY_YCA.ReloadGizmo_desc".Translate();
			icon = GizmoTexture.AmmoReload;

			action = () => ReloadUtility.TryForcedReloadFromInventory(pawn, comps);
		}

		public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
		{
			get
			{
				yield return new FloatMenuOption(
					"SY_YCA.ReloadWeaponFromInventory_label".Translate(),
					() => ReloadUtility.TryForcedReloadFromInventory(Pawn, Comps))
				{
					tooltip = "SY_YCA.ReloadWeaponFromInventory_tooltip".Translate(),
				};

				if (yayoCombat.yayoCombat.supplyAmmoDist >= 0)
				{
					yield return new FloatMenuOption(
						"SY_YCA.ReloadWeaponFromSurrounding_label".Translate(),
						() => ReloadUtility.TryForcedReloadFromSurrounding(Pawn, Comps))
					{ 
						tooltip = "SY_YCA.ReloadWeaponFromSurrounding_tooltip".Translate(),
					};
				}

				yield return new FloatMenuOption(
					"SY_YCA.RestockAmmoFromSurrounding_label".Translate(),
					() => Log.Warning("NOT IMPLEMENTED")) 
				{ 
					tooltip = "SY_YCA.RestockAmmoFromSurrounding_tooltip".Translate(), 
				};
			}
		}
	}
}
