using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace YayosCombatAddon
{
	[DefOf]
	internal static class YCA_DefOf
	{
#pragma warning disable 0649 // disable "never assigned to" warning
		// AmmoDefs
		public static ThingDef yy_ammo_primitive_light;
		public static ThingDef yy_ammo_primitive;
		public static ThingDef yy_ammo_primitive_heavy;

		public static ThingDef yy_ammo_industrial_light;
		public static ThingDef yy_ammo_industrial;
		public static ThingDef yy_ammo_industrial_heavy;

		public static ThingDef yy_ammo_spacer_light;
		public static ThingDef yy_ammo_spacer;
		public static ThingDef yy_ammo_spacer_heavy;

		// RecipeDefs
		public static RecipeDef Make_yy_ammo_primitive_light;
		public static RecipeDef Make_yy_ammo_primitive_light10;
		public static RecipeDef Make_yy_ammo_primitive;
		public static RecipeDef Make_yy_ammo_primitive10;
		public static RecipeDef Make_yy_ammo_primitive_heavy;
		public static RecipeDef Make_yy_ammo_primitive_heavy10;

		public static RecipeDef Make_yy_ammo_industrial_light;
		public static RecipeDef Make_yy_ammo_industrial_light10;
		public static RecipeDef Make_yy_ammo_industrial;
		public static RecipeDef Make_yy_ammo_industrial10;
		public static RecipeDef Make_yy_ammo_industrial_heavy;
		public static RecipeDef Make_yy_ammo_industrial_heavy10;

		public static RecipeDef Make_yy_ammo_spacer_light;
		public static RecipeDef Make_yy_ammo_spacer_light10;
		public static RecipeDef Make_yy_ammo_spacer;
		public static RecipeDef Make_yy_ammo_spacer10;
		public static RecipeDef Make_yy_ammo_spacer_heavy;
		public static RecipeDef Make_yy_ammo_spacer_heavy10;
#pragma warning restore 0649
	}
}
