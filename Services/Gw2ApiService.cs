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
                var data = await _gw2ApiManager.Gw2ApiClient.V2.Account.Raids.GetAsync();

                if (data == null)
                    return null;

                return [.. data.Select(d => d)];
            }
            catch (Exception ex)
            {
                _logger.Warn($"Error while getting raid clears : {ex.Message}");
                return null;
            }
        }
    }
}