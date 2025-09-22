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
				foreach (var baseAmmoSetting in Settings.AmmoSettings)
				{
					if (baseAmmoSetting is AmmoSetting ammoSetting && !ammoSetting.IsEnabled)
					{
						var ammoDef = ammoSetting.AmmoDef;
						ammoDef.tradeability = Tradeability.None;
						ammoDef.tradeTags = null;

						ammoSetting.RecipeDef.recipeUsers?.Clear();
						ammoSetting.RecipeDef10.recipeUsers?.Clear();
					}
				}

				var weaponSettings = Settings.WeaponSettings;
				for (int i = weaponSettings.Count - 1; i >= 0; i--)
				{
					var weaponSetting = weaponSettings[i];
					var reloadable = weaponSetting.Reloadable;
					if (reloadable?.ammoDef == null || !AmmoUtility.AmmoDefs.Contains(reloadable.ammoDef))
					{
						weaponSettings.Remove(weaponSetting);
						continue;
					}

					weaponSetting.DefsLoaded(reloadable.ammoDef, reloadable.maxCharges);
					weaponSetting.WeaponDef.AdjustAmmoType();
				}

				weaponSettings.Sort((x, y) => x.WeaponDef.label.CompareTo(y.WeaponDef.label));
			}
			else
			{
				foreach (var ammoDef in AmmoUtility.AmmoDefs)
				{
					ammoDef.tradeability = Tradeability.None;
					ammoDef.tradeTags = null;
				}
				foreach (var recipeDef in AmmoUtility.AmmoRecipeDefs)
					recipeDef.recipeUsers?.Clear();
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
