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

			// Setting: 
			_lowAmmoFactorForReloadWhileWaiting = Settings.GetHandle(
				nameof(LowAmmoFactorForReloadWhileWaiting),
				"SY_YCA.LowAmmoFactorForReloadWhileWaiting_title".Translate(),
				"SY_YCA.LowAmmoFactorForReloadWhileWaiting_desc".Translate(),
				LowAmmoFactorForReloadWhileWaiting * 1e2f, 
				s => float.TryParse(s, out float result) && result >= 0f && result <= 90f);
			_lowAmmoFactorForReloadWhileWaiting.ValueChanged += handle => LowAmmoFactorForReloadWhileWaiting = ((SettingHandle<float>)handle) * 1e-2f;
			LowAmmoFactorForReloadWhileWaiting = _lowAmmoFactorForReloadWhileWaiting.Value * 1e-2f;


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

					assignTableDef.columns.Add(column);
				}
			}
			else
				Log.Error($"{nameof(YayosCombatAddon)}: could not find any things using the '{AmmoCategoryName}'-ThingCategory (no ammo found); assign tab columns could not be created!");
		}
	}
}
