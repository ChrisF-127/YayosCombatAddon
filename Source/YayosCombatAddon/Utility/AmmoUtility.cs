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
		public static void EjectAmmo(Pawn pawn, CompApparelReloadable comp)
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
			else if (pawn.IsColonist)
			{
				GeneralUtility.ShowRejectMessage(
					comp.parent,
					"SY_YCA.NoAmmoToEject".Translate(
						new NamedArgument(pawn, "pawn"),
						new NamedArgument(comp.parent, "thing")));
			}
		}

		public static int EjectableAmmo(this CompApparelReloadable comp)
		{
			if (comp.Props.ammoCountToRefill > 0)
				return comp.RemainingCharges == comp.MaxCharges ? comp.Props.ammoCountToRefill : 0;
			if (comp.Props.ammoCountPerCharge > 0)
				return comp.RemainingCharges * comp.Props.ammoCountPerCharge;
			return -1;
		}

		public static bool IsAmmo(this Thing thing, bool forceCheck = false) =>
			thing?.def?.IsAmmo(forceCheck) == true;
		public static bool IsAmmo(this ThingDef def, bool forceCheck = false)
		{
#if !ALWAYS_CHECK_ISAMMO
			if (!forceCheck)
				return true;
#endif
			return def?.thingCategories?.Contains(ThingCategoryDef.Named(Main.AmmoCategoryName)) == true;
		}

		public static int CountAmmoInInventory(this Pawn pawn, CompApparelReloadable comp)
		{
			var count = 0;
			foreach (var thing in pawn.inventory.innerContainer)
				if (thing.def == comp.AmmoDef)
					count += thing.stackCount;
			return count;
		}

		public static int MinAmmoNeededChecked(this CompApparelReloadable comp)
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
			var comp = thing?.TryGetComp<CompApparelReloadable>();
			if (comp?.AmmoDef?.IsAmmo() == true)
				return comp.MinAmmoNeededChecked();

			throw new Exception($"{nameof(YayosCombatAddon)}: invalid thing for {nameof(MinAmmoNeededForThing)}: '{thing}'");
		}
		public static int MaxAmmoNeeded(this Thing thing, out Def ammoDef)
		{
			var comp = thing?.TryGetComp<CompApparelReloadable>();
			if (comp?.AmmoDef?.IsAmmo() == true)
			{
				ammoDef = comp.AmmoDef;
				return comp.MaxAmmoNeeded(false);
			}
			ammoDef = null;
			return 0;
		}
		public static bool AtLowAmmo(this Thing thing, Pawn pawn, bool checkAvailable)
		{
			var comp = thing?.TryGetComp<CompApparelReloadable>();
			return comp?.AmmoDef?.IsAmmo() == true 
				&& ((comp.Props.ammoCountToRefill > 0 && comp.RemainingCharges <= 0) 
					|| (comp.Props.ammoCountPerCharge > 0 && comp.RemainingCharges <= comp.MaxCharges * Main.LowAmmoFactorForReloadWhileWaiting))
				&& (!checkAvailable || comp.AnyReservableReachableThing(pawn));
		}
		public static bool AnyAtLowAmmo(this IEnumerable<Thing> things, Pawn pawn, bool checkAvailable)
		{
			foreach (var thing in things)
				if (thing.AtLowAmmo(pawn, checkAvailable))
					return true;
			return false;
		}
	}
}
