using HarmonyLib;
using RimWorld;
using SyControlsBuilder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace YayosCombatAddon
{
#warning TODO ammo icons
#warning TODO traders: postFix ThingSetMaker_TraderStock.Generate, add light/heavy if medium available, s. ThingSetMaker_TraderStock_Generate.addAmmo
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
			CreateAmmoSettings();
			CreateWeaponSettings();
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
				float buttonWidth = width / 3f;
				float buttonHeight = ControlsBuilder.SettingsRowHeight / 4f * 3f;
				float subMenButtonOffsetX = 0;
				// General
				CreateSubMenuSelector(
					new Rect(subMenButtonOffsetX + 2, offsetY, buttonWidth - 4, buttonHeight),
					"SY_YCA.SubMenuGeneral".Translate(),
					SettingsSubMenuEnum.General,
					false);
				subMenButtonOffsetX += buttonWidth;
				if (yayoCombat.yayoCombat.ammo)
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

				ControlsBuilder.Begin(inRect);
				try
				{
					switch (_selectedSettingsSubMenu)
					{
						case SettingsSubMenuEnum.General:
							NumberOfAmmoColumns = ControlsBuilder.CreateNumeric(
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
								ref offsetY,
								width,
								"SY_YCA.EjectAmmoOnDowned_title".Translate(),
								"SY_YCA.EjectAmmoOnDowned_desc".Translate(),
								EjectAmmoOnDowned,
								Default_EjectAmmoOnDowned);

							AmmoDroppedOnDownedFactor = ControlsBuilder.CreateNumeric(
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
							break;

						case SettingsSubMenuEnum.Ammo:
							ControlsBuilder.CreateText(ref offsetY, width, "SY_YCA.AmmoRequireRestart".Translate(), Color.red, TextAnchor.MiddleCenter, GameFont.Medium);

							offsetY += ControlsBuilder.SettingsRowHeight * 0.5f;

							foreach (var baseAmmoSetting in AmmoSettings)
							{
								if (baseAmmoSetting is AmmoSetting ammoSetting)
								{
									ammoSetting.IsEnabled = ControlsBuilder.CreateCheckbox(
										ref offsetY,
										width,
										"SY_YCA.Enable".Translate() + " " + ammoSetting.AmmoDef.label,
										"SY_YCA.AmmoEnable_desc".Translate(),
										ammoSetting.IsEnabled,
										false);
									ammoSetting.Parameter = ControlsBuilder.CreateNumeric(
										ref offsetY,
										width,
										ammoSetting.AmmoDef.LabelCap + " " + "SY_YCA.Damage".Translate(),
										$"SY_YCA.AmmoParameter{(ammoSetting.ParameterGreater ? "Greater" : "Less")}_desc".Translate(),
										ammoSetting.Parameter,
										ammoSetting.DefaultParameter,
										$"{ammoSetting.Name}_Parameter",
										unit: ammoSetting.ParameterGreater ? "<" : ">");
								}

								baseAmmoSetting.RecipeAmount = ControlsBuilder.CreateNumeric(
									ref offsetY,
									width,
									baseAmmoSetting.RecipeDef.LabelCap + " " + "SY_YCA.Amount".Translate(),
									"SY_YCA.AmmoRecipeAmount_desc".Translate(),
									baseAmmoSetting.RecipeAmount,
									baseAmmoSetting.DefaultRecipeAmount,
									$"{baseAmmoSetting.Name}_RecipeAmount",
									1,
									1000);

								offsetY += ControlsBuilder.SettingsRowHeight * 0.5f;
							}
							break;

						case SettingsSubMenuEnum.Weapons:
							foreach (var weaponSetting in WeaponSettings)
							{
								CreateWeaponSettingControl(
									ref offsetY,
									width,
									"SY_YCA.WeaponAmmoDef_desc".Translate(),
									"SY_YCA.WeaponMaxCharges_desc".Translate(),
									weaponSetting);
							}
							break;
					}
				}
				finally
				{
					ControlsBuilder.End(offsetY);
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
		private void CreateAmmoSettings()
		{
			var primitive = "primitive";
			var industrial = "industrial";
			var spacer = "spacer";
			var light = "light";
			var heavy = "heavy";

			addAmmoSetting(primitive, light, 12, false);
			addAmmoSetting(primitive);
			addAmmoSetting(primitive, heavy, 18, true);

			addAmmoSetting(industrial, light, 11, false);
			addAmmoSetting(industrial);
			addAmmoSetting(industrial, heavy, 17, true);

			addAmmoSetting(spacer, light, 15, false);
			addAmmoSetting(spacer);
			addAmmoSetting(spacer, heavy, 25, true);

			void addAmmoSetting(string tech, string type = null, int parameter = -1, bool greater = false)
			{
				if (type == null)
					AmmoSettings.Add(new BaseAmmoSetting(tech));
				else
					AmmoSettings.Add(new AmmoSetting(tech, type, parameter, greater));
			}
		}

		private void CreateWeaponSettings()
		{
			foreach (var thingDef in DefDatabase<ThingDef>.AllDefs)
			{
				if (thingDef?.IsRangedWeapon != true)
					continue;
				WeaponSettings.Add(new WeaponSetting(thingDef));
			}
		}

		private void CreateWeaponSettingControl(
			ref float offsetY,
			float viewWidth,
			string tooltipAmmoDef,
			string tooltipMaxCharges,
			WeaponSetting weaponSetting)
		{
			var label = weaponSetting.WeaponDef.LabelCap;

			var maxCharges = weaponSetting.MaxCharges;
			var defaultMaxCharges = weaponSetting.DefaultMaxCharges;

			if (weaponSetting.AmmoDefWrapper == null)
				weaponSetting.AmmoDefWrapper = new TargetWrapper<ThingDef>(weaponSetting.AmmoDef);
			var ammoDefWrapper = weaponSetting.AmmoDefWrapper;
			var defaultAmmoDef = weaponSetting.DefaultAmmoDef;

			var valueBufferKey = weaponSetting.Name + "_MaxCharges";

			var isModified = !maxCharges.Equals(defaultMaxCharges) || !ammoDefWrapper.Value.Equals(defaultAmmoDef);
			var controlWidth = viewWidth / 4 - 4;

			// Label
			if (isModified)
				GUI.color = ControlsBuilder.ModifiedColor;
			Widgets.Label(new Rect(0, offsetY, controlWidth - 8, ControlsBuilder.SettingsRowHeight), label);
			GUI.color = ControlsBuilder.OriColor;

			// AmmoDef Menu Generator
			IEnumerable<Widgets.DropdownMenuElement<ThingDef>> menuGenerator(TargetWrapper<ThingDef> vWrapper)
			{
				foreach (var item in AmmoUtility.AmmoDefs)
				{
					yield return new Widgets.DropdownMenuElement<ThingDef>
					{
						option = new FloatMenuOption(item.LabelCap, () => vWrapper.Value = item),
						payload = item,
					};
				}
			}
			// AmmoDef Dropdown
			var rect = new Rect(controlWidth + 2, offsetY + 2, controlWidth - 4, ControlsBuilder.SettingsRowHeight - 4);
			Widgets.Dropdown(
				rect,
				ammoDefWrapper,
				null,
				menuGenerator,
				ammoDefWrapper.Value.LabelCap);
			// Tooltip
			ControlsBuilder.DrawTooltip(rect, tooltipAmmoDef);

			// Max Charges
			var numericRect = new Rect((controlWidth + 2) * 2, offsetY + 6, controlWidth - 4, ControlsBuilder.SettingsRowHeight - 12);
			var valueBuffer = ControlsBuilder.GetValueBuffer(valueBufferKey, maxCharges); // required for typing decimal points etc.
			Widgets.TextFieldNumeric(numericRect, ref maxCharges, ref valueBuffer.Buffer, 1f, 1e+9f);
			// Tooltip
			ControlsBuilder.DrawTooltip(numericRect, tooltipMaxCharges);

			// Reset button
			var resetRect = new Rect((controlWidth + 2) * 3, offsetY + 2, ControlsBuilder.SettingsRowHeight * 2 - 4, ControlsBuilder.SettingsRowHeight - 4);
			ControlsBuilder.DrawTooltip(resetRect, $"Reset to '{defaultAmmoDef.LabelCap}' x{defaultMaxCharges}");
			if (isModified && Widgets.ButtonText(resetRect, "Reset"))
			{
				maxCharges = defaultMaxCharges;
				ControlsBuilder.ValueBuffers.Remove(valueBufferKey);

				ammoDefWrapper.Value = defaultAmmoDef;
			}

			offsetY += ControlsBuilder.SettingsRowHeight;

			weaponSetting.AmmoDef = ammoDefWrapper.Value;
			weaponSetting.MaxCharges = maxCharges;
			weaponSetting.Apply();
		}
		#endregion
	}
}
