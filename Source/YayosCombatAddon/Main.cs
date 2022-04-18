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
        public static float LowAmmoFactorForReloadWhileWaiting { get; private set; } = 0.1f;

        public static bool SimpleSidearmsCompatibility { get; private set; } = 
            ModsConfig.IsActive("petetimessix.simplesidearms") || ModsConfig.IsActive("petetimessix.simplesidearms_steam");

        public Main()
		{
            if (SimpleSidearmsCompatibility)
                Log.Message($"[{nameof(YayosCombatAddon)}] SimpleSidearms found; applied compatibility");
		}
    }
}
