using RimWorld;
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
			int count = comp.EjectableAmmo();
			if (count > 0)
			{
				do
				{
					var ammo = ThingMaker.MakeThing(comp.AmmoDef);
					ammo.stackCount = Mathf.Min(ammo.def.stackLimit, count);
					count -= ammo.stackCount;
					GenPlace.TryPlaceThing(ammo, pawn.Position, pawn.Map, ThingPlaceMode.Near);
				}
				while (count > 0);
				comp.Props.soundReload.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
				comp.remainingCharges = 0;
			}
			else
			{
				GeneralUtility.ShowRejectMessage(
					comp.parent,
					"SY_YCA.NoAmmoToEject".Translate(
						new NamedArgument(pawn, "pawn"),
						new NamedArgument(comp.parent, "thing")));
			}
		}

		public static int EjectableAmmo(this CompReloadable comp)
		{
			if (comp.Props.ammoCountToRefill > 0)
				return comp.RemainingCharges == comp.MaxCharges ? comp.Props.ammoCountToRefill : 0;
			if (comp.Props.ammoCountPerCharge > 0)
				return comp.RemainingCharges * comp.Props.ammoCountPerCharge;
			return -1;
		}

		public static bool IsAmmo(this Thing thing) =>
			thing?.def?.IsAmmo() == true;
		public static bool IsAmmo(this ThingDef def) => true; // ammo check disabled
			//def?.thingCategories?.Contains(ThingCategoryDef.Named("yy_ammo_category")) == true;

		public static int CountAmmoInInventory(this Pawn pawn, CompReloadable comp)
		{
			var count = 0;
			foreach (var thing in pawn.inventory.innerContainer)
				if (thing.def == comp.AmmoDef)
					count += thing.stackCount;
			return count;
		}

		public static int MinAmmoNeededChecked(this CompReloadable comp)
		{
			var minAmmoNeeded = comp.MinAmmoNeeded(false);
			if (minAmmoNeeded <= 0)
				throw new Exception($"{nameof(YayosCombatAddon)}: " +
					$"thing does not require reloading: '{comp}' (" +
					$"minAmmoNeeded: {minAmmoNeeded} / " +
					$"remainingCharges: {comp.RemainingCharges} / " +
					$"maxCharges: {comp.MaxCharges})");
			return minAmmoNeeded;
		}
		public static int MinAmmoNeededForThing(this Thing thing)
		{
			var comp = thing?.TryGetComp<CompReloadable>();
			if (comp?.AmmoDef?.IsAmmo() == true)
				return comp.MinAmmoNeededChecked();

			throw new Exception($"{nameof(YayosCombatAddon)}: invalid thing for {nameof(MinAmmoNeededForThing)}: '{thing}'");
		}
		public static int MaxAmmoNeeded(this Thing thing, out Def ammoDef)
		{
			var comp = thing?.TryGetComp<CompReloadable>();
			if (comp?.AmmoDef?.IsAmmo() == true)
			{
				ammoDef = comp.AmmoDef;
				return comp.MaxAmmoNeeded(false);
			}
			ammoDef = null;
			return 0;
		}
		public static bool IsOutOfAmmo(this Thing thing)
		{
			var comp = thing?.TryGetComp<CompReloadable>();
			return comp?.AmmoDef?.IsAmmo() == true && comp.RemainingCharges <= 0;
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
