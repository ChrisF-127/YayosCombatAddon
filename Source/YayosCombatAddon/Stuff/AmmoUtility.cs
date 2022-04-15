using RimWorld;
using SimpleSidearms.rimworld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace YayosCombatAddon
{
	public static class AmmoUtility
	{
		public static void EjectAmmo(Pawn pawn, CompReloadable comp)
		{
			var charges = comp.remainingCharges;
			if (charges > 0)
			{
				do
				{
					var ammo = ThingMaker.MakeThing(comp.AmmoDef);
					ammo.stackCount = Mathf.Min(ammo.def.stackLimit, charges);
					charges -= ammo.stackCount;
					GenPlace.TryPlaceThing(ammo, pawn.Position, pawn.Map, ThingPlaceMode.Near);
				}
				while (charges > 0);
				comp.Props.soundReload.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
				comp.remainingCharges = 0;
			}
		}

		public static bool IsAmmo(this ThingDef def) =>
			def?.thingCategories?.Contains(ThingCategoryDef.Named("yy_ammo_category")) == true;

		public static int CountAmmoInInventory(this Pawn pawn, CompReloadable comp)
		{
			var count = 0;
			foreach (var thing in pawn.inventory.innerContainer)
				if (thing.def == comp.AmmoDef)
					count += thing.stackCount;
			return count;
		}

		public static int AmmoNeeded(this Thing thing)
		{
			var comp = thing?.TryGetComp<CompReloadable>();
			if (comp?.AmmoDef?.IsAmmo() == true)
				return comp.MaxAmmoNeeded(true);
			return 0;
		}
		public static bool IsOutOfAmmo(this Thing thing)
		{
			var comp = thing?.TryGetComp<CompReloadable>();
			if (comp?.AmmoDef?.IsAmmo() == true)
				return comp.RemainingCharges == 0;
			return false;
		}
		public static bool AnyOutOfAmmo(this IEnumerable<Thing> things)
		{
			foreach (var thing in things)
				if (thing.IsOutOfAmmo())
					return true;
			return false;
		}
	}
}
