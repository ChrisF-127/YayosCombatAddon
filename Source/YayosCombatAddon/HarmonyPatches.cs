using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Verse;
using yayoCombat;

namespace YayosCombatAddon
{
	[StaticConstructorOnStartup]
	public static class HarmonyPatches
	{
		static HarmonyPatches()
		{
			Harmony harmony = new Harmony("syrus.yayoscombataddon");

			// gizmo patch for reloadable weapons
			harmony.Patch(
				typeof(ThingComp).GetMethod("CompGetGizmosExtra"),
				postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.ThingComp_CompGetGizmosExtra)));

			// replace original patches
			harmony.Patch(
				typeof(patch_Pawn_TickRare).GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public),
				transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.YC_Patch_Pawn_TickRare)));
			harmony.Patch(
				typeof(patch_CompReloadable_UsedOnce).GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public),
				transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.YC_Patch_CompReloadable_UsedOnce)));

#warning TODO patch reload via right click

			if (Main.SimpleSidearmsCompatibility)
			{
				harmony.Patch(
					typeof(ReloadableUtility).GetMethod("FindSomeReloadableComponent", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public),
					postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.ReloadableUtility_FindSomeReloadableComponent)));
			}
		}


		static CompReloadable ReloadableUtility_FindSomeReloadableComponent(CompReloadable __result, Pawn pawn, bool allowForcedReload)
		{
			Log.Message($"ReloadableUtility_FindSomeReloadableComponent {__result} {pawn} {allowForcedReload}");
			if (__result == null)
			{
				foreach (var thing in pawn.GetSimpleSidearms())
				{
					var compReloadable = thing.TryGetComp<CompReloadable>();
					if (compReloadable?.NeedsReload(allowForcedReload) == true && pawn.EquipThingFromInventory(thing))
						return compReloadable;
				}
			}
			return __result;
		}


		static IEnumerable<Gizmo> ThingComp_CompGetGizmosExtra(IEnumerable<Gizmo> __result, ThingComp __instance)
		{
			if (__instance is CompReloadable reloadable && reloadable.AmmoDef.IsAmmo())
			{
				var thing = reloadable.parent;
				if (thing.Map.designationManager.DesignationOn(thing, YCA_DesignationDefOf.EjectAmmo) == null)
				{
					yield return new Command_Action
					{
						defaultLabel = "SY_YCA.EjectAmmo_label".Translate(),
						defaultDesc = "SY_YCA.EjectAmmo_desc".Translate(),
						icon = YCA_Textures.AmmoEject,
						disabled = reloadable.RemainingCharges == 0,
						disabledReason = "SY_YCA.NoEjectableAmmo".Translate(),
						action = () => thing.Map.designationManager.AddDesignation(new Designation(thing, YCA_DesignationDefOf.EjectAmmo)),
						activateSound = YCA_SoundDefOf.Designate_EjectAmmo,
					};
				}
			}

			foreach (var gizmo in __result)
				yield return gizmo;
		}


		static IEnumerable<CodeInstruction> YC_Patch_Pawn_TickRare(IEnumerable<CodeInstruction> instructions)
		{
			yield return new CodeInstruction(OpCodes.Ldarg_0);
			yield return new CodeInstruction(OpCodes.Call, typeof(HarmonyPatches).GetMethod(nameof(Patch_Pawn_TickRare), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));
			yield return new CodeInstruction(OpCodes.Ret);
		}
		static IEnumerable<CodeInstruction> YC_Patch_CompReloadable_UsedOnce(IEnumerable<CodeInstruction> instructions)
		{
			yield return new CodeInstruction(OpCodes.Ldarg_0);
			yield return new CodeInstruction(OpCodes.Call, typeof(HarmonyPatches).GetMethod(nameof(Patch_CompReloadable_UsedOnce), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));
			yield return new CodeInstruction(OpCodes.Ret);
		}

		static void Patch_Pawn_TickRare(Pawn __instance)
		{
			if (!yayoCombat.yayoCombat.ammo) 
				return;
			if (!__instance.Drafted) 
				return;
			if (Find.TickManager.TicksGame % 60 != 0) 
				return;
			if (__instance.CurJobDef != JobDefOf.Wait_Combat && __instance.CurJobDef != JobDefOf.AttackStatic || __instance.equipment == null) 
				return;

			ReloadUtility.TryAutoReloadAll(__instance);
		}
		static bool Patch_CompReloadable_UsedOnce(CompReloadable __instance)
		{
			if (!yayoCombat.yayoCombat.ammo) 
				return true;

			// (base) decrease number of charges
			__instance.remainingCharges--;

			// (base) destroy item if it is empty and supposed to be destroyed when empty
			if (__instance.Props.destroyOnEmpty && __instance.remainingCharges == 0 && !__instance.parent.Destroyed)
				__instance.parent.Destroy(DestroyMode.Vanish);

			// (yayo) guess it's better to make sure the wearer isn't null
			var pawn = __instance.Wearer;
			if (pawn == null) 
				return false;

			// (new) don't try to reload ammo that's not part of Yayo's Combat
			if (__instance.AmmoDef?.IsAmmo() != true)
				return false;

			// (replacement) Replaced with new method
			if (!ReloadUtility.TryAutoReloadSingle(__instance, true) && pawn.CurJobDef == JobDefOf.Hunt)
				pawn.jobs.StopAll();
			return false;
		}
	}
}
