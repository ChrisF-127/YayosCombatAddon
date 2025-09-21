using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace YayosCombatAddon
{
	[StaticConstructorOnStartup]
	internal class YCA_Textures
	{
		public static readonly Texture2D AmmoEject = ContentFinder<Texture2D>.Get("YCA_AmmoEject", true);
		public static readonly Texture2D AmmoReload = ContentFinder<Texture2D>.Get("YCA_AmmoReload", true);
	}
}
