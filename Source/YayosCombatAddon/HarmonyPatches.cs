using HarmonyLib;
using RimWorld;
using RimWorld.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using yayoCombat;
using yayoCombat.HarmonyPatches;

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
				AccessTools.Method(typeof(Pawn_DraftController), nameof(Pawn_DraftController.GetGizmos)),
				postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Pawn_DraftController_GetGizmos_Postfix)));
			// patch for eject ammo gizmo
			harmony.Patch(
				AccessTools.Method(typeof(ThingComp), nameof(ThingComp.CompGetGizmosExtra)),
				postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(ThingComp_CompGetGizmosExtra_Postfix)));

			// replace original patches
			harmony.Patch(
				AccessTools.Method(typeof(Pawn_Tick), nameof(Pawn_Tick.Postfix)),
				transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(YC_Patch_Pawn_TickRare_Transpiler)));
			harmony.Patch(
				AccessTools.Method(typeof(CompApparelReloadable_UsedOnce), nameof(CompApparelReloadable_UsedOnce.Postfix)), 
				transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(YC_Patch_CompApparelReloadable_UsedOnce_Transpiler)));

			// patch to make original "eject ammo" right click menu only show if there is any ejectable ammo
			harmony.Patch(
				AccessTools.Method(typeof(ThingWithComps_GetFloatMenuOptions), nameof(ThingWithComps_GetFloatMenuOptions.Postfix)),
				transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(YC_ThingWithComps_GetFloatMenuOptions_Transpiler)));

			// patch to stop ejecting ammo on death
			harmony.Patch(
				AccessTools.Method(typeof(Pawn_EquipmentTracker_DropAllEquipment), nameof(Pawn_EquipmentTracker_DropAllEquipment.Prefix)),
				prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(YC_Pawn_EquipmentTracker_DropAllEquipment_Prefix)));

			// patch to reduce ammo in dropped weapons
			harmony.Patch(
				AccessTools.Method(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.DropAllEquipment)),
				prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Pawn_EquipmentTracker_DropAllEquipment_Prefix)));
			// patch to reduce dropped ammo amount
			harmony.Patch(
				AccessTools.Method(typeof(Pawn_InventoryTracker), nameof(Pawn_InventoryTracker.DropAllNearPawn)),
				prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Pawn_InventoryTracker_DropAllNearPawn_Prefix)));

			// patches to generate ammo in inventory instead of filling the weapon with more ammo than it can carry
			harmony.Patch(
				AccessTools.Method(typeof(PawnGenerator_GenerateGearFor), nameof(PawnGenerator_GenerateGearFor.Postfix)),
				transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(YC_Patch_PawnGenerator_GenerateGearFor_Transpiler)));
			harmony.Patch(
				AccessTools.Method(typeof(CompApparelVerbOwner_Charged_PostPostMake), nameof(CompApparelVerbOwner_Charged_PostPostMake.Postfix)),
				transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(YC_Patch_CompApparelVerbOwner_Charged_PostPostMake_Transpiler)));

			// patches to prevent reloading after hunting job fails (times out after 2h), stops pawns from going back and forth between hunting and reloading
			harmony.Patch(
				AccessTools.Method(typeof(JobGiver_Reload), nameof(JobGiver_Reload.GetPriority)),
				postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(JobGiver_Reload_GetPriority_Postfix)));
			harmony.Patch(
				AccessTools.Method(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.EndCurrentJob)),
				prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Pawn_JobTracker_EndCurrentJob_Prefix)));

			// SimpleSidearms compatibility patches
			if (YayosCombatAddon.SimpleSidearmsCompatibility)
			{
				// Info: original Yayo's Combat patch to ReloadableUtility.FindSomeReloadableComponent should be reworked as a postfix patch
				// patch which makes this method also find sidearms in inventory
				harmony.Patch(
					AccessTools.Method(typeof(ReloadableUtility), nameof(ReloadableUtility.FindSomeReloadableComponent)),
					postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(ReloadableUtility_FindSomeReloadableComponent_Postfix)));

				// patch to equip thing from inventory so it can be reloaded
				harmony.Patch(
					AccessTools.Method(typeof(JobDriver_Reload), nameof(JobDriver_Reload.MakeNewToils)),
					prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(JobDriver_Reload_MakeNewToils_Prefix)));
			}
		}


		static IEnumerable<Gizmo> Pawn_DraftController_GetGizmos_Postfix(IEnumerable<Gizmo> __result, Pawn_DraftController __instance)
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
					if (thing.TryGetComp<CompApparelReloadable>() != null)
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

		static IEnumerable<Gizmo> ThingComp_CompGetGizmosExtra_Postfix(IEnumerable<Gizmo> __result, ThingComp __instance)
		{
			if (__instance is CompApparelReloadable reloadable
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


		static IEnumerable<CodeInstruction> YC_Patch_Pawn_TickRare_Transpiler(IEnumerable<CodeInstruction> _)
		{
			yield return new CodeInstruction(OpCodes.Ldarg_0);
			yield return new CodeInstruction(OpCodes.Call, typeof(HarmonyPatches).GetMethod(nameof(Patch_Pawn_TickRare), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));
			yield return new CodeInstruction(OpCodes.Ret);
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
				ReloadUtility.TryAutoReloadSingle(__instance.GetPrimary().TryGetComp<CompApparelReloadable>());
			// if waiting (drafted), try reloading all weapons that are out of ammo and for which ammo can be found
			else if (job == JobDefOf.Wait_Combat)
				ReloadUtility.TryAutoReloadAll(__instance);
		}

		static IEnumerable<CodeInstruction> YC_Patch_CompApparelReloadable_UsedOnce_Transpiler(IEnumerable<CodeInstruction> _)
		{
			yield return new CodeInstruction(OpCodes.Ldarg_0);
			yield return new CodeInstruction(OpCodes.Call, typeof(HarmonyPatches).GetMethod(nameof(Patch_CompReloadable_UsedOnce), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));
			yield return new CodeInstruction(OpCodes.Ret);
		}
		static void Patch_CompReloadable_UsedOnce(CompApparelReloadable __instance)
		{
			if (!yayoCombat.yayoCombat.ammo || __instance.Wearer == null)
				return;
			// (new) only reload equippable weapons
			if (__instance.ReloadableThing?.def?.equipmentType != EquipmentType.Primary)
				return;
			// (new) only reload valid ammo
			if (__instance.AmmoDef.IsAmmo() != true)
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

		static IEnumerable<CodeInstruction> YC_ThingWithComps_GetFloatMenuOptions_Transpiler(IEnumerable<CodeInstruction> codeInstructions)
		{
			foreach (var instruction in codeInstructions)
			{
				if (instruction.opcode == OpCodes.Callvirt && ((MethodInfo)instruction.operand).Name == "get_RemainingCharges")
					yield return new CodeInstruction(OpCodes.Call, typeof(AmmoUtility).GetMethod(nameof(AmmoUtility.EjectableAmmo), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));
				else
					yield return instruction;
			}
		}

		static bool YC_Pawn_EquipmentTracker_DropAllEquipment_Prefix()
		{
			// do not eject ammo from weapons dropped on death/downed if eject ammo disabled
			return YayosCombatAddon.Settings.EjectAmmoOnDowned;
		}

		static void Pawn_EquipmentTracker_DropAllEquipment_Prefix(Pawn_EquipmentTracker __instance)
		{
			// only non-player pawns
			if (__instance?.pawn?.IsPlayerControlled != false)
				return;
			// skip if all ammo is dropped
			if (YayosCombatAddon.Settings.AmmoInWeaponOnDownedFactor == 100)
				return;

			// reduce ammo in weapon
			foreach (var thing in __instance.equipment)
				TryReduceWeaponAmmo(thing);
		}

		static void Pawn_InventoryTracker_DropAllNearPawn_Prefix(Pawn_InventoryTracker __instance)
		{
			// only non-player pawns
			if (__instance?.pawn?.IsPlayerControlled != false)
				return;
			// skip if all ammo is dropped
			if (YayosCombatAddon.Settings.AmmoDroppedOnDownedFactor == 100
				&& YayosCombatAddon.Settings.AmmoInWeaponOnDownedFactor == 100)
				return;

			// iterate through inventory
			for (int i = __instance.innerContainer.Count - 1; i >= 0; i--)
			{
				var thing = __instance.innerContainer[i];
				switch (TryReduceAmmoStackCount(thing))
				{
					case -1:	// thing is not ammo, check if it is a reloadable weapon
						TryReduceWeaponAmmo(thing);
						break;
					case 0:		// remove empty ammo stack from inventory
						__instance.innerContainer.Remove(thing);
						break;
				}
			}
		}
		static int TryReduceAmmoStackCount(Thing thing)
		{
			// only reduce dropped ammo
			if (!thing.IsAmmo(true))
				return -1;
			// reduce dropped ammo
			var count = Mathf.RoundToInt(thing.stackCount * YayosCombatAddon.Settings.AmmoDroppedOnDownedFactor * 0.01f);
			if (count > 0)
				thing.stackCount = count;
			return count;
		}
		static bool TryReduceWeaponAmmo(Thing thing)
		{
			var comp = thing?.TryGetComp<CompApparelReloadable>();
			// only reduce ammo if applicable
			if (comp?.AmmoDef.IsAmmo() != true)
				return false;
			// reduce ammo in dropped weapon
			comp.remainingCharges = Mathf.RoundToInt(comp.remainingCharges * YayosCombatAddon.Settings.AmmoInWeaponOnDownedFactor * 0.01f);
			return true;
		}

		static IEnumerable<CodeInstruction> YC_Patch_PawnGenerator_GenerateGearFor_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var previous = default(OpCode);
			foreach (var instruction in instructions)
			{
				if (previous == OpCodes.Ldloc_0
					&& instruction.opcode == OpCodes.Callvirt && instruction.operand is MethodInfo mi && mi.Name == "GetEnumerator")
				{
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HarmonyPatches), nameof(Patch_PawnGenerator_GenerateGearFor)));
					yield return new CodeInstruction(OpCodes.Ret);
					yield break;
				}
				yield return instruction;
				previous = instruction.opcode;
			}
			Log.Error($"{nameof(YayosCombatAddon)}: failed to apply '{nameof(YC_Patch_PawnGenerator_GenerateGearFor_Transpiler)}'");
		}
		static void Patch_PawnGenerator_GenerateGearFor(List<CompApparelReloadable> allWeaponsComps, Pawn pawn)
		{
			if (!(allWeaponsComps?.Count > 0) || pawn == null)
				return;

			// add ammo to equipped/carried weapons
			var ammoDict = new Dictionary<ThingDef, int>();
			foreach (var comp in allWeaponsComps)
			{
				var ammo = Mathf.RoundToInt(comp.MaxCharges * yayoCombat.yayoCombat.s_enemyAmmo * Rand.Range(0.7f, 1.3f));
				var charges = Math.Min(ammo, comp.MaxCharges);
				comp.remainingCharges = charges;

				var rest = ammo - charges;
				if (rest > 0)
					ammoDict.IncreaseOrAdd(comp.AmmoDef, rest);
			}
			// add ammo to inventory
			//if (pawn?.Faction?.IsPlayer != true)
			{
				foreach (var item in ammoDict)
				{
					var ammoThing = ThingMaker.MakeThing(item.Key);
					ammoThing.stackCount = item.Value;
					pawn.inventory.innerContainer.TryAdd(ammoThing, item.Value);
				}
			}
		}

		static IEnumerable<CodeInstruction> YC_Patch_CompApparelVerbOwner_Charged_PostPostMake_Transpiler(IEnumerable<CodeInstruction> _)
		{
			// overwrite original functionality
			yield return new CodeInstruction(OpCodes.Ldarg_0);
			yield return new CodeInstruction(OpCodes.Ldarg_1);
			yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HarmonyPatches), nameof(Patch_CompApparelVerbOwner_Charged_PostPostMake)));
			yield return new CodeInstruction(OpCodes.Ret);
		}
		static void Patch_CompApparelVerbOwner_Charged_PostPostMake(CompApparelVerbOwner_Charged __instance, ref int ___remainingCharges)
		{
			if (!yayoCombat.yayoCombat.ammo || !__instance.parent.def.IsWeapon)
				return;

			// when first generating the world, at most add "max charges" to a weapon, not more, otherwise keep the weapon empty
			___remainingCharges = GenTicks.TicksGame > 5 ? 0 : Math.Min(Mathf.RoundToInt(__instance.Props.maxCharges * yayoCombat.yayoCombat.s_enemyAmmo), __instance.Props.maxCharges);
		}


		static float JobGiver_Reload_GetPriority_Postfix(float __result, Pawn pawn)
		{
			// do not reload if hunting failed "Incompletable", it probably timed out and we don't want pawns running back and forth between reloading & hunting
			if (PawnPreviousJobTable.TryGetValue(pawn, out var jobInfo)
				&& jobInfo.Def == JobDefOf.Hunt
				&& jobInfo.EndCondition == JobCondition.Incompletable)
				__result = -1f;
			return __result;
		}
		static void Pawn_JobTracker_EndCurrentJob_Prefix(Pawn ___pawn, Job ___curJob, JobCondition condition)
		{
			if (___pawn?.IsColonist == true 
				&& ___curJob != null)
			{
				var jobInfo = PawnPreviousJobTable.GetOrCreateValue(___pawn);
				jobInfo.Def = ___curJob.def;
				jobInfo.EndCondition = condition;

				if (YayosCombatAddon.SimpleSidearmsCompatibility
					&& ___curJob.def == JobDefOf.Reload
					&& jobInfo.PreviousWeapon != null)
				{
					// reequip previous weapon
					___pawn.EquipThingFromInventory(jobInfo.PreviousWeapon);
				}
			}
		}

		static CompApparelReloadable ReloadableUtility_FindSomeReloadableComponent_Postfix(CompApparelReloadable __result, Pawn pawn, bool allowForcedReload)
		{
			if (__result == null)
			{
				foreach (var thing in pawn.GetSimpleSidearms())
				{
					if (thing.def.equipmentType != EquipmentType.Primary)
						continue;
					// requires secondary patch to JobDriver_Reload.MakeNewToils (must only fail if comp.Wearer is neither pawn nor comp.Parent is in pawn's inventory)
					var comp = thing.TryGetComp<CompApparelReloadable>();
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
		static void JobDriver_Reload_MakeNewToils_Prefix(JobDriver_Reload __instance)
		{
			var pawn = __instance.pawn;
			var comp = __instance.Gear?.TryGetComp<CompApparelReloadable>();
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
