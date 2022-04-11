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
        private SettingHandle<bool> _showReloadWeaponGizmoSetting;
        public static bool ShowReloadWeaponGizmo = true;

        private SettingHandle<bool> _reloadAllWeaponsInInventoryOption;
        public static bool ReloadAllWeaponsInInventoryOption = false;

#warning TODO replace standard reload operations from Yayo's - as setting! "Replace reloading from Yayo's with improved job?"

        public override void DefsLoaded()
        {
            _showReloadWeaponGizmoSetting = Settings.GetHandle(
                "showReloadButton", 
                "SY_YCA.ShowReloadWeaponGizmo_title".Translate(), 
                "SY_YCA.ShowReloadWeaponGizmo_desc".Translate(), 
                true);
            _showReloadWeaponGizmoSetting.ValueChanged += 
                value => ShowReloadWeaponGizmo = (SettingHandle<bool>)value;
            ShowReloadWeaponGizmo = _showReloadWeaponGizmoSetting.Value;

            _reloadAllWeaponsInInventoryOption = Settings.GetHandle(
                "reloadAllWeaponsInInventoryOption",
                "SY_YCA.ReloadAllWeaponsInInventoryOption_title".Translate(),
                "SY_YCA.ReloadAllWeaponsInInventoryOption_desc".Translate(),
                false);
            _reloadAllWeaponsInInventoryOption.ValueChanged += 
                value => ReloadAllWeaponsInInventoryOption = (SettingHandle<bool>)value;
            ReloadAllWeaponsInInventoryOption = _reloadAllWeaponsInInventoryOption.Value;
        }
    }
}
