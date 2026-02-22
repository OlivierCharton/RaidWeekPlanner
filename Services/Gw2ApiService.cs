using Blish_HUD;
using Blish_HUD.Modules.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RaidWeekPlanner.Services
{
    public class Gw2ApiService
    {
        private readonly Gw2ApiManager _gw2ApiManager;
        private readonly Logger _logger;

        public Gw2ApiService(Gw2ApiManager gw2ApiManager, Logger logger)
        {
            _gw2ApiManager = gw2ApiManager;
            _logger = logger;
        }

        public async Task<string> GetAccountName()
        {
            if (_gw2ApiManager.HasPermissions(_gw2ApiManager.Permissions) == false)
            {
                _logger.Warn("Permissions not granted.");
                return string.Empty;
            }

            try
            {
                var account = await _gw2ApiManager.Gw2ApiClient.V2.Account.GetAsync();
                return account?.Name;
            }
            catch (Exception ex)
            {
                _logger.Warn($"Error while getting account name : {ex.Message}");
                return null;
            }
        }

        public async Task<List<string>> GetClears()
        {
            if (_gw2ApiManager.HasPermissions(_gw2ApiManager.Permissions) == false)
            {
                _logger.Warn("Permissions not granted.");
                return null;
            }

            try
            {
                var raidData = await _gw2ApiManager.Gw2ApiClient.V2.Account.Raids.GetAsync();
                var strikeData = await GetStrikeClear();

                if (raidData == null && strikeData == null)
                    return null;

                var clears = raidData?.Select(d => d)?.ToList() ?? [];
                clears.AddRange(strikeData ?? []);

                return clears;
            }
            catch (Exception ex)
            {
                _logger.Warn($"Error while getting raid clears : {ex.Message}");
                return null;
            }
        }

        private async Task<List<string>> GetStrikeClear()
        {
            var accountAchievements = await _gw2ApiManager.Gw2ApiClient.V2.Account.Achievements.GetAsync();
            var strikeWeeklyClearAchievement = accountAchievements?.FirstOrDefault(a => a.Id == 9125);

            if (strikeWeeklyClearAchievement == null || strikeWeeklyClearAchievement.Bits == null)
                return null;

            List<string> res = [.. strikeWeeklyClearAchievement.Bits.Select(GetStrikeName)];

            return res;
        }

        private string GetStrikeName(int bit)
        {
            return bit switch
            {
                0 => "shiverpeaks_pass",
                1 => "fraenir_of_jormag",
                2 => "voice_and_claw",
                3 => "whisper_of_jormag",
                4 => "boneskinner",
                5 => "cold_war",
                6 => "aetherblade_hideout",
                7 => "xunlai_jade_junkyard",
                8 => "kaineng_overlook",
                9 => "harvest_temple",
                10 => "cosmic_observatory",
                11 => "temple_of_febe",
                12 => "old_lion_court",
                13 => "kela",
                _ => string.Empty
            };
        }
    }
}