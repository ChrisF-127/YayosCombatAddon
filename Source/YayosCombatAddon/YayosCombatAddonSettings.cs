using RimWorld;
using SyControlsBuilder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace YayosCombatAddon
{
	public class YayosCombatAddonSettings : ModSettings
	{
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
		#endregion

		#region PUBLIC METHODS
		public void DoSettingsWindowContents(Rect inRect)
		{
			var width = inRect.width;
			var offsetY = 0.0f;

			ControlsBuilder.Begin(inRect);
			try
			{
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
			}
			finally
			{
				ControlsBuilder.End(offsetY);
			}
		}
		#endregion

		#region OVERRIDES
		public override void ExposeData()
		{
			base.ExposeData();

			bool boolValue;
			int intValue;

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
		}
		#endregion
	}
}
