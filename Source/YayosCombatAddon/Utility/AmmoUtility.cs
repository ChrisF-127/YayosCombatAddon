using RimWorld;
using RimWorld.Planet;
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
		#region PROPERTIES
		public static ThingDef[] AmmoDefs =
		{
			YCA_DefOf.yy_ammo_primitive_light,
			YCA_DefOf.yy_ammo_primitive,
			YCA_DefOf.yy_ammo_primitive_heavy,

			YCA_DefOf.yy_ammo_industrial_light,
			YCA_DefOf.yy_ammo_industrial,
			YCA_DefOf.yy_ammo_industrial_heavy,

			YCA_DefOf.yy_ammo_spacer_light,
			YCA_DefOf.yy_ammo_spacer,
			YCA_DefOf.yy_ammo_spacer_heavy,
		};
		public static RecipeDef[] AmmoRecipeDefs =
		{
			YCA_DefOf.Make_yy_ammo_primitive_light,
			YCA_DefOf.Make_yy_ammo_primitive_light10,
			YCA_DefOf.Make_yy_ammo_primitive_heavy,
			YCA_DefOf.Make_yy_ammo_primitive_heavy10,

			YCA_DefOf.Make_yy_ammo_industrial_light,
			YCA_DefOf.Make_yy_ammo_industrial_light10,
			YCA_DefOf.Make_yy_ammo_industrial_heavy,
			YCA_DefOf.Make_yy_ammo_industrial_heavy10,

			YCA_DefOf.Make_yy_ammo_spacer_light,
			YCA_DefOf.Make_yy_ammo_spacer_light10,
			YCA_DefOf.Make_yy_ammo_spacer_heavy,
			YCA_DefOf.Make_yy_ammo_spacer_heavy10,
		};
		#endregion

		public static void AdjustAmmoType(this ThingDef weapon)
		{
			//if (weapon?.IsRangedWeapon != true)
			//	return;
			var reloadable = weapon?.GetCompProperties<CompProperties_ApparelReloadable>();
			if (reloadable == null)
				return;
			var ammoDef = reloadable.ammoDef;
			//if (!AmmoDefs.Contains(ammoDef))
			//	return;

			var damage = weapon.verbs.FirstOrDefault(v => typeof(Verb_LaunchProjectile).IsAssignableFrom(v.verbClass))?.defaultProjectile?.projectile?.GetDamageAmount(null);
			foreach (var setting in YayosCombatAddon.Settings.AmmoSettings.Where(s => s is AmmoSetting a && a.BaseAmmoDef == ammoDef).Cast<AmmoSetting>())
			{
				if (setting.IsEnabled && (setting.ParameterGreater ? damage > setting.Parameter : damage < setting.Parameter))
				{
					if (reloadable.ammoDef != setting.AmmoDef)
						reloadable.ammoDef = setting.AmmoDef;
					break;
				}
			}
		}

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

		public static bool IsAmmo(this Thing thing, bool deepCheck = false) =>
			thing?.def.IsAmmo(deepCheck) == true;
		public static bool IsAmmo(this ThingDef def, bool deepCheck = false)
		{
			if (def == null)
				return false;
			if (deepCheck)
				return def.thingCategories?.Contains(ThingCategoryDef.Named(YayosCombatAddon.AmmoCategoryName)) == true;
			return true;
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
			if (comp?.AmmoDef.IsAmmo() == true)
				return comp.MinAmmoNeededChecked();

			throw new Exception($"{nameof(YayosCombatAddon)}: invalid thing for {nameof(MinAmmoNeededForThing)}: '{thing}'");
		}
		public static int MaxAmmoNeeded(this Thing thing, out Def ammoDef)
		{
			var comp = thing?.TryGetComp<CompApparelReloadable>();
			if (comp?.AmmoDef.IsAmmo() == true)
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
			return comp?.AmmoDef.IsAmmo() == true 
				&& ((comp.Props.ammoCountToRefill > 0 && comp.RemainingCharges <= 0) 
					|| (comp.Props.ammoCountPerCharge > 0 && comp.RemainingCharges <= comp.MaxCharges * YayosCombatAddon.Settings.LowAmmoFactorForReloadWhileWaiting * 0.01f))
				&& (!checkAvailable || comp.AnyReservableReachableThing(pawn));
		}
		public static bool AnyAtLowAmmo(this IEnumerable<Thing> things, Pawn pawn, bool checkAvailable)
		{
			foreach (var thing in things)
				if (thing.AtLowAmmo(pawn, checkAvailable))
					return true;
			return false;
		}

		public static void TraderStock_Generate_AddAmmo(TraderKindDef traderKindDef, Faction makingFaction, PlanetTile forTile, List<Thing> outThings)
		{
			const float baseAmount = 400f;
			const float min = 0.25f;
			const float max = 1.50f;
			const float light = 2.0f;
			const float heavy = 0.5f;

			if (!yayoCombat.yayoCombat.ammo)
				return;

			var isWeaponsTrader = false;
			var isExoticTrader = false;
			foreach (var stockGenerator in traderKindDef.stockGenerators)
			{
				switch (stockGenerator)
				{
					case StockGenerator_MarketValue marketValue:
						if (marketValue.tradeTag == "WeaponRanged")
							isWeaponsTrader = true;
						break;
					case StockGenerator_Tag tag:
						if (tag.tradeTag == "ExoticMisc")
							isExoticTrader = true;
						break;
				}
			}

			if (isWeaponsTrader)
				isExoticTrader = false;

			if (!traderKindDef.defName.ToLower().Contains("bulkgoods")
				&& !isWeaponsTrader
				&& !isExoticTrader
				&& Rand.Value > 0.33f)
				return;

			var tech = makingFaction?.def.techLevel ?? TechLevel.Spacer;
			if (tech < TechLevel.Neolithic)
				return;

			// Primitive
			var random = Rand.Value;
			if (!isExoticTrader                                         //   0% for Exotic Traders
				&& (   tech <= TechLevel.Medieval						// 100% for Neolithic & Medieval
					|| tech <= TechLevel.Industrial && random <= 0.2f	//  20% for Industrial
					|| random <= 0.1f))									//  10% for Post-Industrial
			{
				var primitiveAmount = baseAmount;
				if (tech > TechLevel.Medieval)
					primitiveAmount *= 0.5f;

				add(YCA_DefOf.yy_ammo_primitive_light, primitiveAmount * light);
				add(YCA_DefOf.yy_ammo_primitive_heavy, primitiveAmount * heavy);
			}

			// Industrial
			random = Rand.Value;
			if (!isExoticTrader											//   0% for Exotic Traders
				&& (   tech == TechLevel.Industrial						// 100% for Industrial
					|| tech >  TechLevel.Industrial && random <= 0.8f	//  80% for Post-Industrial
					|| random <= 0.2f))									//  20% for Pre-Industrial
			{
				var industrialAmount = baseAmount;
				if (tech < TechLevel.Industrial)
					industrialAmount /= (int)TechLevel.Industrial - (int)tech;

				add(YCA_DefOf.yy_ammo_industrial_light, industrialAmount * light);
				add(YCA_DefOf.yy_ammo_industrial_heavy, industrialAmount * heavy);
			}


			// Spacer
			random = Rand.Value;
			if (isExoticTrader                                          // 100% for Exotic Traders
				|| tech >= TechLevel.Spacer								// 100% for Spacer & Post-Spacer 
				|| tech == TechLevel.Industrial && random <= 0.5f		//  50% for Industrial
				|| random <= 0.1f)										//  10% for Pre-Industrial
			{
				var spacerAmount = baseAmount;
				if (tech < TechLevel.Spacer)
					spacerAmount /= (int)TechLevel.Spacer - (int)tech;

				add(YCA_DefOf.yy_ammo_spacer_light, spacerAmount * light);
				add(YCA_DefOf.yy_ammo_spacer_heavy, spacerAmount * heavy);
			}


			void add(ThingDef ammoDef, float amount)
			{
				if (YayosCombatAddon.Settings.AmmoSettings.First(ws => ws.AmmoDef == ammoDef) is AmmoSetting ammoSetting && ammoSetting.IsEnabled)
				{
					var count = Mathf.RoundToInt(Rand.Range(min, max) * yayoCombat.yayoCombat.ammoGen * amount);
					if (count > 20)
					{
						var thing = ThingMaker.MakeThing(ammoDef);
						thing.stackCount = count;
						thing.PostGeneratedForTrader(traderKindDef, forTile, makingFaction);
						outThings.Add(thing);
					}
				}
			}
		}
	}
}
