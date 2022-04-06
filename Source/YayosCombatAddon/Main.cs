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
        private SettingHandle<bool> showReloadWeaponGizmoSetting;
        public static bool showReloadWeaponGizmo = true;

#warning TODO setting to enable SimpleSidearms compatibility

        public override void DefsLoaded()
        {
            showReloadWeaponGizmoSetting = Settings.GetHandle(
                "showReloadButton", 
                "SY_YCA.ShowReloadWeaponGizmo_title".Translate(), 
                "SY_YCA.ShowReloadWeaponGizmo_desc".Translate(), 
                true);
            showReloadWeaponGizmo = showReloadWeaponGizmoSetting.Value;
        }
    }
}
