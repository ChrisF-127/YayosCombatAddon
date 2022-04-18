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

			// patch for eject ammo gizmo
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
			// patch to make original "eject ammo" right click menu only show if there is any ejectable ammo
			harmony.Patch(
				typeof(patch_ThingWithComps_GetFloatMenuOptions).GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public),
				transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.YC_ThingWithComps_GetFloatMenuOptions)));

			// patch to allow for picking up stacklimit 
			harmony.Patch(
				typeof(Pawn_CarryTracker).GetMethod("MaxStackSpaceEver", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public),
				postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Pawn_CarryTracker_MaxStackSpaceEver)));

			// SimpleSidearms compatibility patches
			if (Main.SimpleSidearmsCompatibility)
			{
				// Info: original Yayo's Combat patch to ReloadableUtility.FindSomeReloadableComponent should be replaced as a postfix patch
				// patch which makes this method also find sidearms in inventory
				harmony.Patch(
					typeof(ReloadableUtility).GetMethod("FindSomeReloadableComponent", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public),
					postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.ReloadableUtility_FindSomeReloadableComponent)));

				// patch to equip thing from inventory so it can be reloaded
				harmony.Patch(
					typeof(JobDriver_Reload).GetMethod("MakeNewToils", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public),
					prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.JobDriver_Reload_MakeNewToils)));
			}
		}


		static IEnumerable<Gizmo> ThingComp_CompGetGizmosExtra(IEnumerable<Gizmo> __result, ThingComp __instance)
		{
			if (__instance is CompReloadable reloadable
				&& reloadable.AmmoDef.IsAmmo()
				&& (reloadable.Props.ammoCountToRefill > 0
					|| reloadable.Props.ammoCountPerCharge > 0))
			{
				var thing = reloadable.parent;
				if (thing.Map.designationManager.DesignationOn(thing, YCA_DesignationDefOf.EjectAmmo) == null)
				{
					yield return new Command_Action
					{
						defaultLabel = "SY_YCA.EjectAmmo_label".Translate(),
						defaultDesc = "SY_YCA.EjectAmmo_desc".Translate(),
						icon = YCA_Textures.AmmoEject,
						disabled = reloadable.EjectableAmmo() <= 0,
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
		static IEnumerable<CodeInstruction> YC_ThingWithComps_GetFloatMenuOptions(IEnumerable<CodeInstruction> codeInstructions)
		{
			foreach (var instruction in codeInstructions)
			{
				if (instruction.opcode == OpCodes.Callvirt && ((MethodInfo)instruction.operand).Name == "get_RemainingCharges")
					yield return new CodeInstruction(OpCodes.Call, typeof(AmmoUtility).GetMethod(nameof(AmmoUtility.EjectableAmmo), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));
				else
					yield return instruction;
			}
		}


		static int Pawn_CarryTracker_MaxStackSpaceEver(int __result, ThingDef td)
		{
			return td.IsAmmo() ? td.stackLimit : __result;
		}


		static void Patch_Pawn_TickRare(Pawn __instance)
		{
			if (!yayoCombat.yayoCombat.ammo) 
				return;
			if (!__instance.Drafted) 
				return;
			if (Find.TickManager.TicksGame % 60 != 0) 
				return;
			if ((__instance.CurJobDef != JobDefOf.Wait_Combat && __instance.CurJobDef != JobDefOf.AttackStatic) || __instance.equipment == null) 
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


		static CompReloadable ReloadableUtility_FindSomeReloadableComponent(CompReloadable __result, Pawn pawn, bool allowForcedReload)
		{
			if (__result == null)
			{
				foreach (var thing in pawn.GetSimpleSidearms())
				{
					// requires secondary patch to JobDriver_Reload.MakeNewToils (must only fail if comp.Wearer is neither pawn nor comp.Parent is in pawn's inventory)
					var comp = thing.TryGetComp<CompReloadable>();
					if (comp?.NeedsReload(allowForcedReload) == true && comp.AmmoDef.AnyReservableReachableThing(pawn, comp.MinAmmoNeeded(allowForcedReload)))
					{
						__result = comp;
						break;
					}
				}
			}
			return __result;
		}

		static void JobDriver_Reload_MakeNewToils(JobDriver_Reload __instance)
		{
			var pawn = __instance.pawn;
			var comp = __instance.Gear?.TryGetComp<CompReloadable>();
			var thing = comp?.parent;
			if (pawn == null
				|| comp == null
				|| comp.Wearer == pawn
				|| thing == null
				|| !pawn.inventory.Contains(thing))
				return;

			// thing to reload must be equipped
			pawn.EquipThingFromInventory(thing);
		}
	}
}
