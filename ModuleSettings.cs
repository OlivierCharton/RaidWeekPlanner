using Blish_HUD.Settings;

namespace RaidWeekPlanner
{
    public class ModuleSettings
    {
        public ModuleSettings(SettingCollection settings)
        {
            SettingCollection internalSettings = settings.AddSubCollection("Internal");
        }
    }
}
