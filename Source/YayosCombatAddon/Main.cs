using HugsLib;
using HugsLib.Settings;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace YayosCombatAddon
{
	public class Main : ModBase
	{
		internal static string AmmoCategoryName = "yy_ammo_category";

		public static bool SimpleSidearmsCompatibility { get; private set; } = 
			ModsConfig.IsActive("petetimessix.simplesidearms") || ModsConfig.IsActive("petetimessix.simplesidearms_steam");

		private SettingHandle<int> _numberOfAmmoColumnsSetting;
		public static int NumberOfAmmoColumns = 2;

		private SettingHandle<float> _lowAmmoFactorForReloadWhileWaiting;
		public static float LowAmmoFactorForReloadWhileWaiting = 0.1f;

		private SettingHandle<bool> _ejectAmmoOnDowned;
		public static bool EjectAmmoOnDowned = false;

		private SettingHandle<float> _ammoDroppedOnDownedFactor;
		public static float AmmoDroppedOnDownedFactor = 1.0f;

		private SettingHandle<float> _ammoInWeaponOnDownedFactor;
		public static float AmmoInWeaponOnDownedFactor = 1.0f;

		public Main()
		{
			if (SimpleSidearmsCompatibility)
				Log.Message($"[{nameof(YayosCombatAddon)}] SimpleSidearms found; applied compatibility");
		}

		public override void DefsLoaded()
		{
			base.DefsLoaded();

			// Setting: number of ammo-columns in "Assign"-tab
			_numberOfAmmoColumnsSetting = Settings.GetHandle(
				nameof(NumberOfAmmoColumns),
				"SY_YCA.NumberOfAmmoColumns_title".Translate(),
				"SY_YCA.NumberOfAmmoColumns_desc".Translate(),
				NumberOfAmmoColumns, 
				s => int.TryParse(s, out int result) && result >= 0 && result <= 10);
			NumberOfAmmoColumns = _numberOfAmmoColumnsSetting.Value;

			// Setting: low ammo before reload
			_lowAmmoFactorForReloadWhileWaiting = Settings.GetHandle(
				nameof(LowAmmoFactorForReloadWhileWaiting),
				"SY_YCA.LowAmmoFactorForReloadWhileWaiting_title".Translate(),
				"SY_YCA.LowAmmoFactorForReloadWhileWaiting_desc".Translate(),
				LowAmmoFactorForReloadWhileWaiting * 1e2f, 
				s => float.TryParse(s, out float result) && result >= 0f && result <= 90f);
			_lowAmmoFactorForReloadWhileWaiting.ValueChanged += handle => LowAmmoFactorForReloadWhileWaiting = ((SettingHandle<float>)handle) * 1e-2f;
			LowAmmoFactorForReloadWhileWaiting = _lowAmmoFactorForReloadWhileWaiting.Value * 1e-2f;

			// Setting: eject ammo on downed
			_ejectAmmoOnDowned = Settings.GetHandle(
				nameof(EjectAmmoOnDowned),
				"SY_YCA.EjectAmmoOnDowned_title".Translate(),
				"SY_YCA.EjectAmmoOnDowned_desc".Translate(),
				EjectAmmoOnDowned);
			_ejectAmmoOnDowned.ValueChanged += handle => EjectAmmoOnDowned = (SettingHandle<bool>)handle;
			EjectAmmoOnDowned = _ejectAmmoOnDowned.Value;

			// Setting: ammo dropped on death/downed factor
			_ammoDroppedOnDownedFactor = Settings.GetHandle(
				nameof(AmmoDroppedOnDownedFactor),
				"SY_YCA.AmmoDroppedOnDownedFactor_title".Translate(),
				"SY_YCA.AmmoDroppedOnDownedFactor_desc".Translate(),
				AmmoDroppedOnDownedFactor * 1e2f, 
				s => float.TryParse(s, out float result) && result >= 0f && result <= 100f);
			_ammoDroppedOnDownedFactor.ValueChanged += handle => AmmoDroppedOnDownedFactor = ((SettingHandle<float>)handle) * 1e-2f;
			AmmoDroppedOnDownedFactor = _ammoDroppedOnDownedFactor.Value * 1e-2f;

			// Setting: ammo in weapon on death/downed factor
			_ammoInWeaponOnDownedFactor = Settings.GetHandle(
				nameof(AmmoInWeaponOnDownedFactor),
				"SY_YCA.AmmoInWeaponOnDownedFactor_title".Translate(),
				"SY_YCA.AmmoInWeaponOnDownedFactor_desc".Translate(),
				AmmoInWeaponOnDownedFactor * 1e2f, 
				s => float.TryParse(s, out float result) && result >= 0f && result <= 100f);
			_ammoInWeaponOnDownedFactor.ValueChanged += handle => AmmoInWeaponOnDownedFactor = ((SettingHandle<float>)handle) * 1e-2f;
			AmmoInWeaponOnDownedFactor = _ammoInWeaponOnDownedFactor.Value * 1e-2f;
   
			if (yayoCombat.yayoCombat.ammo)
   				return;
			// Dynamic "Assign"-tab ammo-column initialization
			var assignTableDef = DefDatabase<PawnTableDef>.AllDefs.First(def => def.defName == "Assign");
			var ammoDefs = DefDatabase<ThingDef>.AllDefs.Where(def => def.IsAmmo(true))?.ToList();
			if (ammoDefs?.Count > 0)
			{
				for (int i = 0; i < NumberOfAmmoColumns; i++)
				{
					var name = $"yy_ammo{i + 1}";
					var inventory = new InventoryStockGroupDef
					{
						defName = name,
						modContentPack = ModContentPack,
						defaultThingDef = ammoDefs[0],
						thingDefs = ammoDefs,
						min = 0,
						max = 10000,
					};
					var column = new PawnColumnDef
					{
						defName = name,
						label = "ammo " + (i + 1),
						workerClass = typeof(PawnColumnWorker_CarryAmmo),
						sortable = true,
					};

					ModContentPack.AddDef(inventory);
					ModContentPack.AddDef(column);

					DefGenerator.AddImpliedDef(inventory);
					DefGenerator.AddImpliedDef(column);

					assignTableDef.columns.Insert(assignTableDef.columns.Count - 1, column);
				}
			}
			else
				Log.Error($"{nameof(YayosCombatAddon)}: could not find any things using the '{AmmoCategoryName}'-ThingCategory (no ammo found); assign tab columns could not be created!");
		}
	}
}
