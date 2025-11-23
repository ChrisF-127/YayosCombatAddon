using RimWorld;
using SyControlsBuilder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace YayosCombatAddon
{
	internal class YayosCombatAddonSettings : ModSettings
	{
		#region ENUMS
		public enum SettingsSubMenuEnum
		{
			General,
			Ammo,
			Weapons,
		}
		#endregion

		#region CONSTANTS
		public const int Default_NumberOfAmmoColumns = 2;
		public const int Default_LowAmmoFactorForReloadWhileWaiting = 10;

		public const bool Default_EjectAmmoOnDowned = false;
		public const int Default_AmmoDroppedOnDownedFactor = 100;
		public const int Default_AmmoInWeaponOnDownedFactor = 100;
		#endregion

		#region PROPERTIES
		public int NumberOfAmmoColumns { get; set; } = Default_NumberOfAmmoColumns;
		public int LowAmmoFactorForReloadWhileWaiting { get; set; } = Default_LowAmmoFactorForReloadWhileWaiting;

		public bool EjectAmmoOnDowned { get; set; } = Default_EjectAmmoOnDowned;
		public int AmmoDroppedOnDownedFactor { get; set; } = Default_AmmoDroppedOnDownedFactor;
		public int AmmoInWeaponOnDownedFactor { get; set; } = Default_AmmoInWeaponOnDownedFactor;

		public List<BaseAmmoSetting> AmmoSettings { get; } = new List<BaseAmmoSetting>();
		public List<WeaponSetting> WeaponSettings { get; } = new List<WeaponSetting>();
		#endregion

		#region FIELDS
		private SettingsSubMenuEnum _selectedSettingsSubMenu = SettingsSubMenuEnum.General;
		#endregion

		#region CONSTRUCTORS
		public YayosCombatAddonSettings() :
			base()
		{
			// setup ammo settings
			var primitive = "primitive";
			var industrial = "industrial";
			var spacer = "spacer";
			var light = "light";
			var heavy = "heavy";
			var emp = "_emp";
			var fire = "_fire";

			// using default values
			addAmmoSetting(primitive, light, 12, false);
			addAmmoSetting(primitive);
			addAmmoSetting(primitive, heavy, 18, true);
			addAmmoSetting(primitive + emp);
			addAmmoSetting(primitive + fire);
			addAmmoSetting(industrial, light, 11, false);
			addAmmoSetting(industrial);
			addAmmoSetting(industrial, heavy, 17, true);
			addAmmoSetting(industrial + emp);
			addAmmoSetting(industrial + fire);
			addAmmoSetting(spacer, light, 15, false);
			addAmmoSetting(spacer);
			addAmmoSetting(spacer, heavy, 25, true);
			addAmmoSetting(spacer + emp);
			addAmmoSetting(spacer + fire);

			void addAmmoSetting(string tech, string type = null, int parameter = -1, bool greater = false)
			{
				if (type == null)
					AmmoSettings.Add(new BaseAmmoSetting(tech));
				else
					AmmoSettings.Add(new AmmoSetting(tech, type, parameter, greater));
			}

			// setup weapon settings
			foreach (var thingDef in DefDatabase<ThingDef>.AllDefs)
			{
				if (thingDef?.IsRangedWeapon != true)
					continue;
				WeaponSettings.Add(new WeaponSetting(thingDef));
			}
		}
		#endregion

		#region PUBLIC METHODS
		public void DoSettingsWindowContents(Rect inRect)
		{
			var width = inRect.width;
			var offsetY = 0.0f;

			try
			{
				GUI.BeginGroup(inRect);

				// Sub menu buttons
				var buttonWidth = width / 3f;
				var buttonHeight = ControlsBuilder.SettingsRowHeight / 4f * 3f;
				var subMenButtonOffsetX = 0f;
				// General
				CreateSubMenuSelector(
					new Rect(subMenButtonOffsetX + 2, offsetY, buttonWidth - 4, buttonHeight),
					"SY_YCA.SubMenuGeneral".Translate(),
					SettingsSubMenuEnum.General,
					false);
				subMenButtonOffsetX += buttonWidth;
				if (yayoCombat.YayoCombatCore.ammo)
				{
					// Ammo
					CreateSubMenuSelector(
						new Rect(subMenButtonOffsetX + 2, offsetY, buttonWidth - 4, buttonHeight),
						"SY_YCA.SubMenuAmmo".Translate(),
						SettingsSubMenuEnum.Ammo,
						false);
					subMenButtonOffsetX += buttonWidth;
					// Weapons
					CreateSubMenuSelector(
						new Rect(subMenButtonOffsetX + 2, offsetY, buttonWidth - 4, buttonHeight),
						"SY_YCA.SubMenuWeapons".Translate(),
						SettingsSubMenuEnum.Weapons,
						false);
					subMenButtonOffsetX += buttonWidth;
				}

				inRect.height -= ControlsBuilder.SettingsRowHeight;

				switch (_selectedSettingsSubMenu)
				{
					case SettingsSubMenuEnum.General:
						ControlsBuilder.Begin(inRect);
						try
						{
							NumberOfAmmoColumns = ControlsBuilder.CreateNumeric(
								0f,
								ref offsetY,
								width,
								"SY_YCA.NumberOfAmmoColumns_title".Translate(),
								"SY_YCA.NumberOfAmmoColumns_desc".Translate(),
								NumberOfAmmoColumns,
								Default_NumberOfAmmoColumns,
								nameof(NumberOfAmmoColumns),
								0,
								12);

							offsetY += ControlsBuilder.SettingsRowHeight * 0.5f;

							LowAmmoFactorForReloadWhileWaiting = ControlsBuilder.CreateNumeric(
								0f,
								ref offsetY,
								width,
								"SY_YCA.LowAmmoFactorForReloadWhileWaiting_title".Translate(),
								"SY_YCA.LowAmmoFactorForReloadWhileWaiting_desc".Translate(),
								LowAmmoFactorForReloadWhileWaiting,
								Default_LowAmmoFactorForReloadWhileWaiting,
								nameof(LowAmmoFactorForReloadWhileWaiting),
								0,
								90,
								unit: "%");

							offsetY += ControlsBuilder.SettingsRowHeight * 0.5f;

							EjectAmmoOnDowned = ControlsBuilder.CreateCheckbox(
								0f,
								ref offsetY,
								width,
								"SY_YCA.EjectAmmoOnDowned_title".Translate(),
								"SY_YCA.EjectAmmoOnDowned_desc".Translate(),
								EjectAmmoOnDowned,
								Default_EjectAmmoOnDowned);

							AmmoDroppedOnDownedFactor = ControlsBuilder.CreateNumeric(
								0f,
								ref offsetY,
								width,
								"SY_YCA.AmmoDroppedOnDownedFactor_title".Translate(),
								"SY_YCA.AmmoDroppedOnDownedFactor_desc".Translate(),
								AmmoDroppedOnDownedFactor,
								Default_AmmoDroppedOnDownedFactor,
								nameof(AmmoDroppedOnDownedFactor),
								0,
								100,
								unit: "%");

							AmmoInWeaponOnDownedFactor = ControlsBuilder.CreateNumeric(
								0f,
								ref offsetY,
								width,
								"SY_YCA.AmmoInWeaponOnDownedFactor_title".Translate(),
								"SY_YCA.AmmoInWeaponOnDownedFactor_desc".Translate(),
								AmmoInWeaponOnDownedFactor,
								Default_AmmoInWeaponOnDownedFactor,
								nameof(AmmoInWeaponOnDownedFactor),
								0,
								100,
								unit: "%");
						}
						finally
						{
							ControlsBuilder.End(offsetY);
						}
						break;

					case SettingsSubMenuEnum.Ammo:
						offsetY += ControlsBuilder.SettingsRowHeight * 0.8f;
						ControlsBuilder.CreateText(0f, ref offsetY, width, "SY_YCA.AmmoRequireRestart".Translate(), Color.red, TextAnchor.MiddleCenter, GameFont.Medium);

						ControlsBuilder.Begin(new Rect(inRect.x, inRect.y + ControlsBuilder.SettingsRowHeight * 0.75f, inRect.width, inRect.height));
						try
						{
							offsetY = 0f;
							foreach (var baseAmmoSetting in AmmoSettings)
							{
								CreateAmmoSettingControl(
									ref offsetY,
									width,
									baseAmmoSetting);
							}
						}
						finally
						{
							ControlsBuilder.End(offsetY);
						}
						break;

					case SettingsSubMenuEnum.Weapons:
						ControlsBuilder.Begin(inRect);
						try
						{
							offsetY = 0f;
							foreach (var weaponSetting in WeaponSettings)
							{
								CreateWeaponSettingControl(
									ref offsetY,
									width,
									weaponSetting);
							}
						}
						finally
						{
							ControlsBuilder.End(offsetY);
						}
						break;
				}
			}
			finally
			{
				GUI.EndGroup();
			}
		}
		#endregion

		#region OVERRIDES
		public override void ExposeData()
		{
			base.ExposeData();

			bool boolValue;
			int intValue;

			// General
			intValue = NumberOfAmmoColumns;
			Scribe_Values.Look(ref intValue, nameof(NumberOfAmmoColumns), Default_NumberOfAmmoColumns);
			NumberOfAmmoColumns = intValue;

			intValue = LowAmmoFactorForReloadWhileWaiting;
			Scribe_Values.Look(ref intValue, nameof(LowAmmoFactorForReloadWhileWaiting), Default_LowAmmoFactorForReloadWhileWaiting);
			LowAmmoFactorForReloadWhileWaiting = intValue;

			boolValue = EjectAmmoOnDowned;
			Scribe_Values.Look(ref boolValue, nameof(EjectAmmoOnDowned), Default_EjectAmmoOnDowned);
			EjectAmmoOnDowned = boolValue;

			intValue = AmmoDroppedOnDownedFactor;
			Scribe_Values.Look(ref intValue, nameof(AmmoDroppedOnDownedFactor), Default_AmmoDroppedOnDownedFactor);
			AmmoDroppedOnDownedFactor = intValue;

			intValue = AmmoInWeaponOnDownedFactor;
			Scribe_Values.Look(ref intValue, nameof(AmmoInWeaponOnDownedFactor), Default_AmmoInWeaponOnDownedFactor);
			AmmoInWeaponOnDownedFactor = intValue;

			// Ammo
			foreach (var baseAmmoSetting in AmmoSettings)
			{
				if (baseAmmoSetting is AmmoSetting ammoSetting)
				{
					boolValue = ammoSetting.IsEnabled;
					Scribe_Values.Look(ref boolValue, $"{ammoSetting.Name}_IsEnabled", false);
					ammoSetting.IsEnabled = boolValue;

					intValue = ammoSetting.Parameter;
					Scribe_Values.Look(ref intValue, $"{ammoSetting.Name}_Parameter", ammoSetting.DefaultParameter);
					ammoSetting.Parameter = intValue;
				}

				intValue = baseAmmoSetting.RecipeAmount;
				Scribe_Values.Look(ref intValue, $"{baseAmmoSetting.Name}_RecipeAmount", baseAmmoSetting.DefaultRecipeAmount);
				baseAmmoSetting.RecipeAmount = intValue;
			}

			// Weapons
			foreach (var weaponSetting in WeaponSettings)
			{
				if (Scribe.mode != LoadSaveMode.Saving || weaponSetting.AmmoDef != weaponSetting.DefaultAmmoDef)
				{
					var ammoDef = weaponSetting.AmmoDef;
					Scribe_Defs.Look(ref ammoDef, $"{weaponSetting.Name}_AmmoDef");
					weaponSetting.AmmoDef = ammoDef ?? weaponSetting.DefaultAmmoDef;
				}

				intValue = weaponSetting.MaxCharges;
				Scribe_Values.Look(ref intValue, $"{weaponSetting.Name}_MaxCharges", weaponSetting.DefaultMaxCharges);
				weaponSetting.MaxCharges = intValue;
			}
		}
		#endregion

		#region PRIVATE METHODS
		private void CreateSubMenuSelector(Rect rect, string label, SettingsSubMenuEnum value, bool isModified)
		{
			// Colorize if selected
			if (_selectedSettingsSubMenu == value)
				GUI.color = ControlsBuilder.SelectionColor;
			// Colorize if modified
			else if (isModified)
				GUI.color = ControlsBuilder.ModifiedColor;
			// Draw button
			if (Widgets.ButtonText(rect, label))
				_selectedSettingsSubMenu = value;
			// Reset color
			GUI.color = Color.white;
		}
		#endregion

		#region PRIVATE METHODS
		private void CreateAmmoSettingControl(
			ref float offsetY,
			float viewWidth,
			BaseAmmoSetting baseAmmoSetting)
		{
			var iconSize = ControlsBuilder.SettingsRowHeight - 2f;

			// Icon
			Widgets.DefIcon(new Rect(0f, offsetY, iconSize, iconSize), baseAmmoSetting.AmmoDef);
			var offsetX = iconSize + 8;
			viewWidth -= offsetX;

			if (baseAmmoSetting is AmmoSetting ammoSetting)
			{
				ammoSetting.IsEnabled = ControlsBuilder.CreateCheckbox(
					offsetX,
					ref offsetY,
					viewWidth,
					"SY_YCA.Enable".Translate() + " " + ammoSetting.AmmoDef.label,
					"SY_YCA.AmmoEnable_desc".Translate(),
					ammoSetting.IsEnabled,
					false);
				ammoSetting.Parameter = ControlsBuilder.CreateNumeric(
					offsetX,
					ref offsetY,
					viewWidth,
					ammoSetting.AmmoDef.LabelCap + " " + "SY_YCA.Damage".Translate(),
					$"SY_YCA.AmmoParameter{(ammoSetting.ParameterGreater ? "Greater" : "Less")}_desc".Translate(),
					ammoSetting.Parameter,
					ammoSetting.DefaultParameter,
					$"{ammoSetting.Name}_Parameter",
					unit: ammoSetting.ParameterGreater ? "<" : ">");
			}

			baseAmmoSetting.RecipeAmount = ControlsBuilder.CreateNumeric(
				offsetX,
				ref offsetY,
				viewWidth,
				baseAmmoSetting.RecipeDef.LabelCap + " " + "SY_YCA.Amount".Translate(),
				"SY_YCA.AmmoRecipeAmount_desc".Translate(),
				baseAmmoSetting.RecipeAmount,
				baseAmmoSetting.DefaultRecipeAmount,
				$"{baseAmmoSetting.Name}_RecipeAmount",
				1,
				1000);

			offsetY += ControlsBuilder.SettingsRowHeight * 0.5f;
		}

		private void CreateWeaponSettingControl(
			ref float offsetY,
			float viewWidth,
			WeaponSetting weaponSetting)
		{
			if (weaponSetting.AmmoDef == null)
				return;

			var label = weaponSetting.WeaponDef.LabelCap;

			var maxCharges = weaponSetting.MaxCharges;
			var defaultMaxCharges = weaponSetting.DefaultMaxCharges;

			if (weaponSetting.AmmoDefWrapper == null)
				weaponSetting.AmmoDefWrapper = new TargetWrapper<ThingDef>(weaponSetting.AmmoDef);
			var ammoDefWrapper = weaponSetting.AmmoDefWrapper;
			var defaultAmmoDef = weaponSetting.DefaultAmmoDef;

			var valueBufferKey = weaponSetting.Name + "_MaxCharges";

			var isModified = !maxCharges.Equals(defaultMaxCharges) || !ammoDefWrapper.Value.Equals(defaultAmmoDef);
			var iconSize = ControlsBuilder.SettingsRowHeight - 2f;
			var controlWidth = (viewWidth - iconSize - 4) / 4 - 16;
			var offsetX = 0f;

			// Icon
			Widgets.DefIcon(new Rect(offsetX, offsetY, iconSize, iconSize), weaponSetting.WeaponDef);
			offsetX += iconSize + 8;

			// Label
			if (isModified)
				GUI.color = ControlsBuilder.ModifiedColor;
			Widgets.Label(new Rect(offsetX, offsetY, controlWidth - 8, ControlsBuilder.SettingsRowHeight), label);
			GUI.color = ControlsBuilder.OriColor;
			offsetX += controlWidth;

			// AmmoDef Menu Generator
			IEnumerable<Widgets.DropdownMenuElement<ThingDef>> menuGenerator(TargetWrapper<ThingDef> vWrapper)
			{
				foreach (var ammoDef in AmmoUtility.ActiveAmmoDefs)
				{
					yield return new Widgets.DropdownMenuElement<ThingDef>
					{
						option = new FloatMenuOption(ammoDef.LabelCap, () => vWrapper.Value = ammoDef),
						payload = ammoDef,
					};
				}
			}
			// AmmoDef Dropdown
			var rect = new Rect(offsetX, offsetY + 2, controlWidth - 8, ControlsBuilder.SettingsRowHeight - 4);
			Widgets.Dropdown(
				rect,
				ammoDefWrapper,
				null,
				menuGenerator,
				ammoDefWrapper.Value.LabelCap);
			// Tooltip
			ControlsBuilder.DrawTooltip(rect, "SY_YCA.WeaponAmmoDef_desc".Translate());
			offsetX += controlWidth;

			// Max Charges
			var numericRect = new Rect(offsetX, offsetY + 6, controlWidth - 8, ControlsBuilder.SettingsRowHeight - 12);
			var valueBuffer = ControlsBuilder.GetValueBuffer(valueBufferKey, maxCharges); // required for typing decimal points etc.
			Widgets.TextFieldNumeric(numericRect, ref maxCharges, ref valueBuffer.Buffer, 1f, 1e+9f);
			// Tooltip
			ControlsBuilder.DrawTooltip(numericRect, "SY_YCA.WeaponMaxCharges_desc".Translate());
			offsetX += controlWidth;

			// Reset button
			if (isModified)
			{
				var resetRect = new Rect(offsetX, offsetY + 2, controlWidth - 8, ControlsBuilder.SettingsRowHeight - 4);
				ControlsBuilder.DrawTooltip(resetRect, $"Reset to '{defaultAmmoDef.LabelCap}' x{defaultMaxCharges}");
				if (Widgets.ButtonText(resetRect, "Reset"))
				{
					maxCharges = defaultMaxCharges;
					ControlsBuilder.ValueBuffers.Remove(valueBufferKey);

					ammoDefWrapper.Value = defaultAmmoDef;
				}
				offsetX += controlWidth;
			}

			offsetY += ControlsBuilder.SettingsRowHeight;

			weaponSetting.AmmoDef = ammoDefWrapper.Value;
			weaponSetting.MaxCharges = maxCharges;
			weaponSetting.Apply();
		}
		#endregion
	}
}
