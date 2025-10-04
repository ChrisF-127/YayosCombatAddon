using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace YayosCombatAddon
{
	internal class BaseAmmoSetting
	{
		public string Name { get; }

		public ThingDef AmmoDef { get; }

		public RecipeDef RecipeDef { get; }
		public RecipeDef RecipeDef10 { get; }
		public ThingDefCountClass Product { get; }
		public ThingDefCountClass Product10 { get; }
		public int RecipeAmount
		{
			get => Product?.count ?? -1;
			set
			{
				if (value > 0)
				{
					if (Product != null)
						Product.count = value;
					if (Product10 != null)
						Product10.count = value * 10;
				}
			}
		}
		public int DefaultRecipeAmount { get; }

		public BaseAmmoSetting(string name)
		{
			Name = name;

			var fullName = $"yy_ammo_{name}";
			var ammoDef = (ThingDef)AccessTools.Field(typeof(YCA_DefOf), fullName).GetValue(null);
			AmmoDef = ammoDef;

			var recipeDef = (RecipeDef)AccessTools.Field(typeof(YCA_DefOf), $"Make_{fullName}").GetValue(null);
			RecipeDef = recipeDef;
			Product = recipeDef.products.First(p => p.thingDef == ammoDef);

			var recipeDef10 = (RecipeDef)AccessTools.Field(typeof(YCA_DefOf), $"Make_{fullName}10").GetValue(null);
			RecipeDef10 = recipeDef10;
			Product10 = recipeDef10.products.First(p => p.thingDef == ammoDef);

			DefaultRecipeAmount = RecipeAmount;
		}
	}

	internal class AmmoSetting : BaseAmmoSetting
	{
		public ThingDef BaseAmmoDef { get; }

		public bool IsModifiable =>
			AmmoDef != BaseAmmoDef;
		public bool IsEnabled { get; set; }
		public int Parameter { get; set; }
		public int DefaultParameter { get; }
		public bool ParameterGreater { get; }

		public AmmoSetting(string tech, string type, int parameter, bool parameterGreater) :
			base($"{tech}_{type}")
		{
			var baseName = $"yy_ammo_{tech}";
			BaseAmmoDef = (ThingDef)AccessTools.Field(typeof(YCA_DefOf), baseName).GetValue(null);

			DefaultParameter = Parameter = parameter;
			ParameterGreater = parameterGreater;
		}
	}
}
