using HugsLib;
using HugsLib.Settings;
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
		public static bool SimpleSidearmsCompatibility { get; private set; } = ModsConfig.IsActive("PeteTimesSix.SimpleSidearms");
    }
}
