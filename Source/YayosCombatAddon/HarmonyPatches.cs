using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace YayosCombatAddon
{
	[StaticConstructorOnStartup]
	public static class HarmonyPatches
	{
		static HarmonyPatches()
		{
			Harmony harmony = new Harmony("syrus.yayoscombataddon");

			harmony.Patch(
				typeof(ThingComp).GetMethod("CompGetGizmosExtra"),
				postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.ThingComp_CompGetGizmosExtra_Postfix)));
		}

		static IEnumerable<Gizmo> ThingComp_CompGetGizmosExtra_Postfix(IEnumerable<Gizmo> __result, ThingComp __instance)
		{
			if (__instance is CompReloadable reloadable)
			{
				var thing = reloadable.parent;
				if (thing.Map.designationManager.DesignationOn(thing, YCA_DesignationDefOf.EjectAmmo) == null)
				{
					yield return new Command_Action
					{
						defaultLabel = "SY_YCA.EjectAmmo_label".Translate(),
						defaultDesc = "SY_YCA.EjectAmmo_desc".Translate(),
						icon = YCA_Textures.AmmoEject,
						action = () => thing.Map.designationManager.AddDesignation(new Designation(thing, YCA_DesignationDefOf.EjectAmmo)),
					};
				}
			}

			foreach (var gizmo in __result)
				yield return gizmo;
		}
	}
}
