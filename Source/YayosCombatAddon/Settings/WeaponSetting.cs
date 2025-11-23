using RimWorld;
using SyControlsBuilder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace YayosCombatAddon
{
	internal class WeaponSetting
	{
		public string Name =>
			WeaponDef.defName;

		public ThingDef WeaponDef { get; }
		public CompProperties_ApparelReloadable Reloadable =>
			WeaponDef.GetCompProperties<CompProperties_ApparelReloadable>();

		public ThingDef AmmoDef { get; set; } = null;
		public ThingDef DefaultAmmoDef { get; private set; }

		public int MaxCharges { get; set; } = 0;
		public int DefaultMaxCharges { get; private set; }

		public TargetWrapper<ThingDef> AmmoDefWrapper { get; set; } = null;

		public WeaponSetting(ThingDef weapon)
		{
			WeaponDef = weapon;
		}

		public void DefsLoaded(ThingDef ammoDef, int maxCharges)
		{
			DefaultAmmoDef = ammoDef;
			if (AmmoDef != null)
			{
				if (YayosCombatAddon.Settings.AmmoSettings.FirstOrDefault(s => s.AmmoDef == AmmoDef) is BaseAmmoSetting baseAmmoSetting
					&& (!(baseAmmoSetting is AmmoSetting ammoSetting) || ammoSetting.IsEnabled))
					Reloadable.ammoDef = AmmoDef;
				else
				{
					Log.Message($"[{nameof(YayosCombatAddon)}] {WeaponDef}: ammo type '{AmmoDef}' not enabled/found, defaulting to '{ammoDef}'");
					AmmoDef = ammoDef;
				}
			}
			else
				AmmoDef = ammoDef;

			DefaultMaxCharges = maxCharges;
			if (MaxCharges > 0)
				Reloadable.maxCharges = MaxCharges;
			else
				MaxCharges = maxCharges;
		}

		public void Apply()
		{
			Reloadable.ammoDef = AmmoDef;
			Reloadable.maxCharges = MaxCharges;
		}
	}
}
