using Blish_HUD;
using Blish_HUD.Modules.Managers;
using Newtonsoft.Json;
using RaidWeekPlanner.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LoadingSpinner = RaidWeekPlanner.UI.Controls.LoadingSpinner;

namespace RaidWeekPlanner.Services
{
    public class BusinessService
    {
        private readonly ContentsManager _contentsManager;
        private readonly Gw2ApiService _gw2ApiService;
        private readonly Func<LoadingSpinner> _getSpinner;
        private readonly UI.Controls.CornerIcon _cornerIcon;
        private readonly Logger _logger;

        private string _accountName { get; set; }
        private Data _data;
        private Rotation _rotation;
        private List<string> _raidClears;

        public BusinessService(ContentsManager contentsManager, Gw2ApiService gw2ApiService, Func<LoadingSpinner> getSpinner, UI.Controls.CornerIcon cornerIcon, Logger logger)
        {
            _contentsManager = contentsManager;
            _gw2ApiService = gw2ApiService;
            _getSpinner = getSpinner;
            _cornerIcon = cornerIcon;
            _logger = logger;
        }

        public void LoadData()
        {
            var file = _contentsManager.GetFileStream("data.json");
            using (StreamReader sr = new StreamReader(file))
            {
                string content = sr.ReadToEnd();
                _data = JsonConvert.DeserializeObject<Data>(content);
            }

            var rotationFile = _contentsManager.GetFileStream("rotation.json");
            using (StreamReader sr = new StreamReader(rotationFile))
            {
                string content = sr.ReadToEnd();
                _rotation = JsonConvert.DeserializeObject<Rotation>(content);
            }
        }

        public async Task RefreshBaseData()
        {
            _getSpinner?.Invoke()?.Show();

            //Get accountName
            await RefreshAccountName();

            //Get user raid progression
            await RefreshProgression();

            _getSpinner?.Invoke()?.Hide();
        }

        public async Task<List<string>> GetAccountClears(bool forceRefresh = false)
        {
            if (_raidClears == null || forceRefresh)
            {
                await RefreshBaseData();
            }

            return _raidClears;
        }

        public List<Area> GetAreas()
        {
            return _data.Areas;
        }

        public List<string> GetNeverOnTheMenu()
        {
            return [.. _data.Areas
                .SelectMany(a => a.Encounters)
                .Where(e => e.IsDisabled)
                .Select(e => e.Key)];
        }

        public Dictionary<int, List<string>> GetEventsForCurrentWeek()
        {
            // Trouver le lundi de la semaine en cours
            DateTime monday = GetMonday(DateTime.UtcNow);

            var weekEvents = new Dictionary<int, List<string>>();

            // Parcourir du lundi au dimanche (7 jours)
            for (int dayOffset = 0; dayOffset < 7; dayOffset++)
            {
                DateTime currentDay = monday.AddDays(dayOffset);
                weekEvents.Add(dayOffset, GetEventsForDate(currentDay));
            }

            return weekEvents;
        }

        private async Task<bool> RefreshAccountName()
        {
            _accountName = await _gw2ApiService.GetAccountName();

            _cornerIcon.UpdateWarningState(string.IsNullOrWhiteSpace(_accountName));

            return !string.IsNullOrWhiteSpace(_accountName);
        }

        private async Task RefreshProgression()
        {
            _raidClears = await _gw2ApiService.GetClears();
        }

        private DateTime GetMonday(DateTime date)
        {
            // DayOfWeek.Monday = 1, Sunday = 0
            int daysFromMonday = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            return date.Date.AddDays(-daysFromMonday).AddHours(1);
        }

        private List<string> GetEventsForDate(DateTime targetDate)
        {
            int daysPassed = (targetDate - _rotation.StartDate).Days;

            if (daysPassed < 0)
            {
                throw new ArgumentException("La date cible ne peut pas être antérieure à la date de départ");
            }

            var results = new List<string>();

            var eventLists = _rotation.GetLists();
            for (int i = 0; i < eventLists.Length; i++)
            {
                if (eventLists[i] != null && eventLists[i].Count > 0)
                {
                    int index = daysPassed % eventLists[i].Count;
                    results.Add(eventLists[i][index]);
                }
            }

            return results;
        }
    }
}
