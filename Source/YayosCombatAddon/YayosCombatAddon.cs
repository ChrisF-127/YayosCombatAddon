using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace YayosCombatAddon
{
	internal class YayosCombatAddon : Mod
	{
		#region PROPERTIES
		public static YayosCombatAddon Instance { get; private set; }
		public static YayosCombatAddonSettings Settings { get; private set; }

		public static bool SimpleSidearmsCompatibility { get; } =
			ModsConfig.IsActive("petetimessix.simplesidearms") || ModsConfig.IsActive("petetimessix.simplesidearms_steam");
		#endregion

		#region FIELDS
		internal static string AmmoCategoryName = "yy_ammo_category";
		#endregion

		#region CONSTRUCTORS
		public YayosCombatAddon(ModContentPack content) : base(content)
		{
			Instance = this;

			LongEventHandler.ExecuteWhenFinished(Initialize);
		}
		#endregion

		#region PUBLIC METHODS
		public static void DefsLoaded()
		{
			if (yayoCombat.yayoCombat.ammo)
			{
				// setup ammo defs
				var recipeDefsToRemove = new List<RecipeDef>();
				foreach (var baseAmmoSetting in Settings.AmmoSettings)
				{
					if (baseAmmoSetting is AmmoSetting ammoSetting && !ammoSetting.IsEnabled)
					{
						// set ammo to non-tradeable
						var ammoDef = ammoSetting.AmmoDef;
						ammoDef.tradeability = Tradeability.None;
						ammoDef.tradeTags = null;

						// remove recipe users from recipes
						ammoSetting.RecipeDef.recipeUsers?.Clear();
						ammoSetting.RecipeDef10.recipeUsers?.Clear();

						// remember recipes to remove from thingDefs
						recipeDefsToRemove.Add(ammoSetting.RecipeDef);
						recipeDefsToRemove.Add(ammoSetting.RecipeDef10);
					}
				}
				// remove recipes from thingDefs
				Parallel.ForEach(
					DefDatabase<ThingDef>.AllDefs,
					thingDef =>
					{
						if (!thingDef.IsWorkTable)
							return; // skip
						thingDef.recipes?.RemoveAll(recipeDef => recipeDefsToRemove.Contains(recipeDef));
						thingDef.allRecipesCached?.RemoveAll(recipeDef => recipeDefsToRemove.Contains(recipeDef));
					});


				// setup weapon settings, removing ones without or incorrect ammo defs
				var weaponSettings = Settings.WeaponSettings;
				for (int i = weaponSettings.Count - 1; i >= 0; i--)
				{
					var weaponSetting = weaponSettings[i];
					var reloadable = weaponSetting.Reloadable;
					if (reloadable?.ammoDef == null || !AmmoUtility.AllAmmoDefs.Contains(reloadable.ammoDef))
					{
						// remove weapon settings for weapons without or incorrect ammo defs
						weaponSettings.Remove(weaponSetting);
						continue;
					}

					// adjust and apply ammo defs for weapon settings
					weaponSetting.WeaponDef.AdjustAmmoType();
					weaponSetting.DefsLoaded(reloadable.ammoDef, reloadable.maxCharges);
				}

				// sort weapon settings by label
				weaponSettings.Sort((x, y) => x.WeaponDef.label.CompareTo(y.WeaponDef.label));
			}
			else
			{
				// set ammo to non-tradeable
				foreach (var ammoDef in AmmoUtility.AllAmmoDefs)
				{
					ammoDef.tradeability = Tradeability.None;
					ammoDef.tradeTags = null;
				}
				// remove recipe users from recipes
				foreach (var recipeDef in AmmoUtility.AllAmmoRecipeDefs)
					recipeDef.recipeUsers?.Clear();
				// remove recipes from thingDefs
				Parallel.ForEach(
					DefDatabase<ThingDef>.AllDefs,
					thingDef =>
					{
						if (!thingDef.IsWorkTable)
							return; // skip
						thingDef.recipes?.RemoveAll(recipeDef => AmmoUtility.AllAmmoRecipeDefs.Contains(recipeDef));
						thingDef.allRecipesCached?.RemoveAll(recipeDef => AmmoUtility.AllAmmoRecipeDefs.Contains(recipeDef));
					});
			}
		}
		#endregion

		#region OVERRIDES
		public override string SettingsCategory() =>
			"Yayo's Combat 3 - Addon";

		public override void DoSettingsWindowContents(Rect inRect)
		{
			base.DoSettingsWindowContents(inRect);

			Settings.DoSettingsWindowContents(inRect);
		}
		#endregion

		#region PRIVATE METHODS
		private void Initialize()
		{
			if (SimpleSidearmsCompatibility)
				Log.Message($"[{nameof(YayosCombatAddon)}] SimpleSidearms active, compatibility will be applied");

			Settings = GetSettings<YayosCombatAddonSettings>();

			// Dynamic "Assign"-tab ammo-column initialization
			var assignTableDef = DefDatabase<PawnTableDef>.AllDefs.First(def => def.defName == "Assign");
			var ammoDefs = DefDatabase<ThingDef>.AllDefs.Where(def => def.IsAmmo(true))?.ToList();
			if (ammoDefs?.Count > 0)
			{
				for (int i = 0; i < Settings.NumberOfAmmoColumns; i++)
				{
					var name = $"yy_ammo{i + 1}";
					var inventory = new InventoryStockGroupDef
					{
						defName = name,
						modContentPack = Content,
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

					Content.AddDef(inventory);
					Content.AddDef(column);

					DefGenerator.AddImpliedDef(inventory);
					DefGenerator.AddImpliedDef(column);

					assignTableDef.columns.Insert(assignTableDef.columns.Count - 1, column);
				}
			}
			else
				Log.Error($"{nameof(YayosCombatAddon)}: could not find any things using the '{AmmoCategoryName}'-ThingCategory (no ammo found); assign tab columns could not be created!");
		}
		#endregion
	}
}
