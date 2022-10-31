using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using yayoCombat;

namespace YayosCombatAddon
{
	[StaticConstructorOnStartup]
	public static class HarmonyPatches
	{
		class JobInfo
		{
			public JobDef Def;
			public JobCondition EndCondition;
			public ThingWithComps PreviousWeapon;
		}
		static readonly ConditionalWeakTable<Pawn, JobInfo> PawnPreviousJobTable = new ConditionalWeakTable<Pawn, JobInfo>();

		static HarmonyPatches()
		{
			Harmony harmony = new Harmony("syrus.yayoscombataddon");

			// patch for reload gizmo
			harmony.Patch(
				typeof(Pawn_DraftController).GetMethod("GetGizmos", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public),
				postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Pawn_DraftController_GetGizmos)));
			// patch for eject ammo gizmo
			harmony.Patch(
				typeof(ThingComp).GetMethod("CompGetGizmosExtra", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public),
				postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.ThingComp_CompGetGizmosExtra)));

			// replace original patches
			harmony.Patch(
				typeof(patch_Pawn_TickRare).GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public),
				transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.YC_Patch_Pawn_TickRare)));
			harmony.Patch(
				typeof(patch_CompReloadable_UsedOnce).GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public), 
				transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.YC_Patch_CompReloadable_UsedOnce)));

			// patch to make original "eject ammo" right click menu only show if there is any ejectable ammo
			harmony.Patch(
				typeof(patch_ThingWithComps_GetFloatMenuOptions).GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public),
				transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.YC_ThingWithComps_GetFloatMenuOptions)));

			// patches to prevent reloading after hunting job fails (usually after timing out after 2h), stops pawns from going back and forth between hunting and reloading
			harmony.Patch(
				typeof(JobGiver_Reload).GetMethod("GetPriority", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public),
				postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.JobGiver_Reload_GetPriority)));
			harmony.Patch(
				typeof(Pawn_JobTracker).GetMethod("EndCurrentJob", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public),
				prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Pawn_JobTracker_EndCurrentJob)));

			// SimpleSidearms compatibility patches
			if (Main.SimpleSidearmsCompatibility)
			{
				// Info: original Yayo's Combat patch to ReloadableUtility.FindSomeReloadableComponent should be reworked as a postfix patch
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


		static IEnumerable<Gizmo> Pawn_DraftController_GetGizmos(IEnumerable<Gizmo> __result, Pawn_DraftController __instance)
		{
			var addReloadGizmo = false;

			var pawn = __instance?.pawn;
			if (yayoCombat.yayoCombat.ammo
				&& pawn != null
				&& pawn.Faction?.IsPlayer == true
				&& pawn.Drafted
				&& !pawn.WorkTagIsDisabled(WorkTags.Violent))
			{
				foreach (var thing in pawn.equipment.AllEquipmentListForReading)
				{
					if (thing.TryGetComp<CompReloadable>() != null)
					{
						addReloadGizmo = true;
						break;
					}
				}
			}

			foreach (var gizmo in __result)
			{
				yield return gizmo;

				if (addReloadGizmo
					&& gizmo is Command command
					&& command.tutorTag == "FireAtWillToggle")
				{
					yield return new Command_ReloadActions(pawn);
					addReloadGizmo = false;
				}
			}

			if (addReloadGizmo)
				Log.ErrorOnce($"{nameof(YayosCombatAddon)}: Failed to add 'Reload' gizmo!", 0x18051848);
		}

		static IEnumerable<Gizmo> ThingComp_CompGetGizmosExtra(IEnumerable<Gizmo> __result, ThingComp __instance)
		{
			if (__instance is CompReloadable reloadable
				&& reloadable.AmmoDef.IsAmmo()
				&& (reloadable.Props.ammoCountToRefill > 0
					|| reloadable.Props.ammoCountPerCharge > 0))
			{
				var thing = reloadable.parent;
				if (thing.Map.designationManager.DesignationOn(thing, YCA_DesignationDefOf.YCA_EjectAmmo) == null)
				{
					yield return new Command_Action
					{
						defaultLabel = "SY_YCA.EjectAmmo_label".Translate(),
						defaultDesc = "SY_YCA.EjectAmmo_desc".Translate(),
						icon = YCA_Textures.AmmoEject,
						disabled = reloadable.EjectableAmmo() <= 0,
						disabledReason = "SY_YCA.NoEjectableAmmo".Translate(),
						action = () => thing.Map.designationManager.AddDesignation(new Designation(thing, YCA_DesignationDefOf.YCA_EjectAmmo)),
						activateSound = YCA_SoundDefOf.YCA_Designate_EjectAmmo,
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


		static float JobGiver_Reload_GetPriority(float __result, Pawn pawn)
		{
			// do not reload if hunting failed "Incompletable", it probably timed out and we don't want pawns running back and forth between reloading & hunting
			if (PawnPreviousJobTable.TryGetValue(pawn, out var jobInfo)
				&& jobInfo.Def == JobDefOf.Hunt
				&& jobInfo.EndCondition == JobCondition.Incompletable)
				__result = -1f;
			return __result;
		}
		static void Pawn_JobTracker_EndCurrentJob(Pawn ___pawn, Job ___curJob, JobCondition condition)
		{
			if (___pawn?.IsColonist == true 
				&& ___curJob != null)
			{
				var jobInfo = PawnPreviousJobTable.GetOrCreateValue(___pawn);
				jobInfo.Def = ___curJob.def;
				jobInfo.EndCondition = condition;

				if (Main.SimpleSidearmsCompatibility
					&& ___curJob.def == JobDefOf.Reload
					&& jobInfo.PreviousWeapon != null)
				{
					// reequip previous weapon
					___pawn.EquipThingFromInventory(jobInfo.PreviousWeapon);
				}
			}
		}


		static void Patch_Pawn_TickRare(Pawn __instance)
		{
			if (!yayoCombat.yayoCombat.ammo
				|| __instance?.Drafted != true
				|| Find.TickManager.TicksGame % 60 != 0 
				|| __instance.equipment == null) 
				return;

			var job = __instance.CurJobDef;
			// if attacking at range, try reloading only primary once it runs out of ammo
			if (job == JobDefOf.AttackStatic)
				ReloadUtility.TryAutoReloadSingle(__instance.GetPrimary().TryGetComp<CompReloadable>());
			// if waiting (drafted), try reloading all weapons that are out of ammo and for which ammo can be found
			else if (job == JobDefOf.Wait_Combat)
				ReloadUtility.TryAutoReloadAll(__instance);
		}

		static void Patch_CompReloadable_UsedOnce(CompReloadable __instance)
		{
			if (!yayoCombat.yayoCombat.ammo || __instance.Wearer == null)
				return;

			// (new) don't try to reload ammo that's not part of Yayo's Combat
			if (__instance.AmmoDef?.IsAmmo() != true)
				return;

			// (replacement) Replaced with new method
			var pawn = __instance.Wearer;
			var drafted = pawn.Drafted;
			if (!ReloadUtility.TryAutoReloadSingle(
					__instance,
					showOutOfAmmoWarning: true,
					ignoreDistance: !drafted,
					returnToStartingPosition: drafted)
				&& pawn.CurJobDef == JobDefOf.Hunt)
				pawn.jobs.StopAll();
		}


		static CompReloadable ReloadableUtility_FindSomeReloadableComponent(CompReloadable __result, Pawn pawn, bool allowForcedReload)
		{
			if (__result == null)
			{
				foreach (var thing in pawn.GetSimpleSidearms())
				{
					// requires secondary patch to JobDriver_Reload.MakeNewToils (must only fail if comp.Wearer is neither pawn nor comp.Parent is in pawn's inventory)
					var comp = thing.TryGetComp<CompReloadable>();
					if (comp?.NeedsReload(allowForcedReload) == true 
						&& comp.AmmoDef.AnyReservableReachableThing(pawn, comp.MinAmmoNeeded(allowForcedReload)))
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

			// remember previous weapon to reequip it after the job ended
			var jobInfo = PawnPreviousJobTable.GetOrCreateValue(pawn);
			jobInfo.PreviousWeapon = pawn.equipment.Primary;

			// thing to reload must be equipped
			pawn.EquipThingFromInventory(thing);
		}
	}
}
