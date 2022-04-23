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
		public static float LowAmmoFactorForReloadWhileWaiting { get; private set; } = 0.1f;

		public static bool SimpleSidearmsCompatibility { get; private set; } = 
			ModsConfig.IsActive("petetimessix.simplesidearms") || ModsConfig.IsActive("petetimessix.simplesidearms_steam");

		private SettingHandle<int> _numberOfAmmoColumnsSetting;
		public static int NumberOfAmmoColumns = 2;

		public Main()
		{
			if (SimpleSidearmsCompatibility)
				Log.Message($"[{nameof(YayosCombatAddon)}] SimpleSidearms found; applied compatibility");
		}

		public override void DefsLoaded()
		{
			base.DefsLoaded();

			_numberOfAmmoColumnsSetting = Settings.GetHandle(
				nameof(NumberOfAmmoColumns),
				"SY_YCA.NumberOfAmmoColumns_title".Translate(),
				"SY_YCA.NumberOfAmmoColumns_desc".Translate(), 
				2, 
				s => true);
			NumberOfAmmoColumns = _numberOfAmmoColumnsSetting.Value;

			var ammoCategory = "yy_ammo_category";

			var assignTableDef = DefDatabase<PawnTableDef>.AllDefs.First(def => def.defName == "Assign");
			var ammoDefs = DefDatabase<ThingDef>.AllDefs.Where(def => def.thingCategories?.Contains(ThingCategoryDef.Named(ammoCategory)) == true)?.ToList();
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
				Log.Error($"{nameof(YayosCombatAddon)}: could not find any things using the '{ammoCategory}'-ThingCategory (no ammo found); assign tab columns could not be created!");
		}
	}
}
