using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using RaidWeekPlanner.Domain;
using RaidWeekPlanner.Ressources;
using RaidWeekPlanner.Services;
using RaidWeekPlanner.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Threading.Tasks;
using Color = Microsoft.Xna.Framework.Color;
using FlowPanel = RaidWeekPlanner.UI.Controls.FlowPanel;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace RaidWeekPlanner.UI.Views
{
    public class RaidWeekPlannerWindow : StandardWindow
    {
        // Components
        private LoadingSpinner _loadingSpinner;
        private FlowPanel _tableContainer;
        private List<(Panel, Label, string)> _tablePanels = new();
        private List<Label> _labels = new();
        private readonly List<StandardButton> _buttons = new();
        private StandardButton _toggleEncounterDrawModeBtn;
        private StandardButton _toggleTableDrawModeBtn;

        // Data
        private List<Area> _areas;
        private Dictionary<int, List<string>> _bounties;
        private List<string> _neverOnTheMenu;
        private List<string> _accountClears;

        // Params
        private TableDrawMode _tableDrawMode = TableDrawMode.Week;
        private EncounterDrawMode _encounterDrawMode = EncounterDrawMode.Progression;

        // DI
        private readonly BusinessService _businessService;
        private ResourceManager _stringsResx;
        private ResourceManager _areasResx;
        private ResourceManager _encountersResx;

        public RaidWeekPlannerWindow(AsyncTexture2D background, Rectangle windowRegion, Rectangle contentRegion,
            AsyncTexture2D cornerIconTexture, BusinessService businessService
           ) : base(background, windowRegion, contentRegion)
        {
            Parent = GameService.Graphics.SpriteScreen;
            Title = "RaidWeekPlanner";
            Emblem = cornerIconTexture;
            Location = new Point(300, 300);
            SavesPosition = true;

            _businessService = businessService;
            _stringsResx = strings.ResourceManager;
            _areasResx = areas.ResourceManager;
            _encountersResx = encounters.ResourceManager;

            LoadData();
        }

        public void BuildUi()
        {
            FlowPanel mainContainer = new()
            {
                Parent = this,
                WidthSizingMode = SizingMode.Fill,
                HeightSizingMode = SizingMode.Fill,
                ControlPadding = new(3, 3),
                CanScroll = true,
            };

            mainContainer.Resized += (s, e) =>
            {
                var newWidth = mainContainer.Width - 20;
                _tableContainer.Width = newWidth;
            };

            #region Notifications
            _tableContainer = new()
            {
                Parent = mainContainer,
                WidthSizingMode = SizingMode.Standard,
                HeightSizingMode = SizingMode.AutoSize,
                ShowBorder = true,
                OuterControlPadding = new(5),
                ControlPadding = new(5),
            };
            _tableContainer.ContentResized += TableContainer_ContentResized;

            #endregion Notifications

            #region Actions
            Controls.FlowPanel actionContainer = new()
            {
                Parent = mainContainer,
                WidthSizingMode = SizingMode.Fill,
                Height = 35,
                OuterControlPadding = new(5),
                ControlPadding = new(5),
            };
            actionContainer.ContentResized += ActionContainer_ContentResized;

            StandardButton button;
            _buttons.Add(button = new Controls.StandardButton()
            {
                SetLocalizedText = () => strings.MainWindow_Button_Refresh_Label,
                SetLocalizedTooltip = () => strings.MainWindow_Button_Refresh_Tooltip,
                Parent = actionContainer
            });
            button.Click += async (s, e) => await RefreshData();

            _buttons.Add(_toggleEncounterDrawModeBtn = new Controls.StandardButton()
            {
                SetLocalizedText = () => GetToggleEncounterDrawModeBtnLabel(),
                SetLocalizedTooltip = () => strings.MainWindow_Button_ToggleEncounterDrawMode_Tooltip,
                Parent = actionContainer
            });
            _toggleEncounterDrawModeBtn.Click += (s, e) => ToggleEncounterDrawMode();

            _buttons.Add(_toggleTableDrawModeBtn = new Controls.StandardButton()
            {
                SetLocalizedText = () => GetToggleTableDrawModeBtnLabel(),
                SetLocalizedTooltip = () => strings.MainWindow_Button_ToggleTableDrawMode_Tooltip,
                Parent = actionContainer
            });
            _toggleTableDrawModeBtn.Click += (s, e) => ToggleTableDrawMode();

            #endregion Actions

            #region Spinner
            _loadingSpinner = new LoadingSpinner()
            {
                Parent = actionContainer,
                Size = new Point(29, 29),
                Visible = false,
            };
            #endregion Spinner

            #region Legend
            Controls.FlowPanel legendContainer = new()
            {
                Parent = mainContainer,
                WidthSizingMode = SizingMode.Fill,
                HeightSizingMode = SizingMode.AutoSize,
                OuterControlPadding = new(5),
                ControlPadding = new(5),
            };

            #endregion Legend

            DrawLegend(legendContainer);

            DrawTable();
        }

        public void InjectData(List<string> accountClears)
        {
            _accountClears = accountClears;

            UpdateColors();
        }

        private void LoadData()
        {
            _areas = _businessService.GetAreas();
            _bounties = _businessService.GetEventsForCurrentWeek();
            _neverOnTheMenu = _businessService.GetNeverOnTheMenu();
        }

        #region Labels
        public string GetToggleEncounterDrawModeBtnLabel() => _encounterDrawMode == EncounterDrawMode.Neutral ?
                strings.MainWindow_Button_ToggleEncounterDrawMode_Neutral : strings.MainWindow_Button_ToggleEncounterDrawMode_Progression;

        public string GetToggleTableDrawModeBtnLabel() => _tableDrawMode == TableDrawMode.Week ?
                strings.MainWindow_Button_ToggleTableDrawMode_Week : strings.MainWindow_Button_ToggleTableDrawMode_Areas;
        #endregion Labels

        #region Draw
        private void DrawLegend(FlowPanel container)
        {
            var legend = UiUtils.CreateLabel(() => strings.Legend_Title, () => "", container, amount: 12);

            legend = UiUtils.CreateLabel(() => strings.Legend_None, () => "", container, amount: 11);
            legend.panel.BackgroundColor = Colors.None;

            legend = UiUtils.CreateLabel(() => strings.Legend_Todo, () => "", container, amount: 11);
            legend.panel.BackgroundColor = Colors.Todo;

            legend = UiUtils.CreateLabel(() => strings.Legend_Planned, () => "", container, amount: 11);
            legend.panel.BackgroundColor = Colors.Planned;

            legend = UiUtils.CreateLabel(() => strings.Legend_Done, () => "", container, amount: 11);
            legend.panel.BackgroundColor = Colors.Done;
        }

        private void DrawTable()
        {
            if (_tableDrawMode == TableDrawMode.Week)
            {
                DrawTableAsWeek();
            }
            else if (_tableDrawMode == TableDrawMode.Areas)
            {
                DrawTableAsAreas();
            }
        }

        private void DrawTableAsWeek()
        {
            AddHeadersAsWeek(_tableContainer);
            DrawLinesAsWeek();
        }

        private void AddHeadersAsWeek(FlowPanel container)
        {
            List<int> days = [0, 1, 2, 3, 4, 5, 6];
            foreach (var day in days)
            {
                _labels.Add(UiUtils.CreateLabel(() => _stringsResx.GetString($"day{day}"), () => "", container, amount: 7).label);
            }
        }

        private void DrawLinesAsWeek()
        {
            // Workaround for localization change out of range bug
            List<int> rows = [0, 1, 2, 3];
            foreach (var row in rows)
            {
                foreach (var bounty in _bounties)
                {
                    var encounter = bounty.Value[row];

                    var label = UiUtils.CreateLabel(() => _encountersResx.GetString($"{encounter}Label"), () => GetTooltip(encounter), _tableContainer, amount: 7);
                    label.panel.BackgroundColor = GetBackgroundColor(encounter);
                    _tablePanels.Add((label.panel, label.label, encounter));
                }
            }
        }

        private void DrawTableAsAreas()
        {
            AddHeadersAsAreas(_tableContainer);
            DrawLinesAsAreas();
        }

        private void AddHeadersAsAreas(FlowPanel container)
        {
            foreach (var area in _areas)
            {
                _labels.Add(UiUtils.CreateLabel(() => _areasResx.GetString($"{area.Key}Label"), () => _areasResx.GetString($"{area.Key}Tooltip"), container).label);
            }
        }

        private void DrawLinesAsAreas()
        {
            //Lines
            bool keepDrawing = true;
            int count = 1;

            while (keepDrawing)
            {
                keepDrawing = false;

                foreach (var area in _areas)
                {
                    if (area.Encounters.Count >= count)
                    {
                        keepDrawing = true;
                        Encounter currentEncounter = area.Encounters[count - 1];

                        var label = UiUtils.CreateLabel(() => _encountersResx.GetString($"{currentEncounter.Key}Label"), () => GetTooltip(currentEncounter.Key), _tableContainer);
                        label.panel.BackgroundColor = GetBackgroundColor(currentEncounter.Key);
                        _tablePanels.Add((label.panel, label.label, currentEncounter.Key));
                    }
                    else
                    {
                        var (panel, label) = UiUtils.CreateLabel(() => "", () => "", _tableContainer);
                        _tablePanels.Add((panel, label, string.Empty));
                    }
                }
                count++;
            }
        }

        private void UpdateColors()
        {
            foreach (var tablePanel in _tablePanels)
            {
                tablePanel.Item1.BackgroundColor = GetBackgroundColor(tablePanel.Item3);
            }
        }

        #endregion Draw

        #region Clear
        private void ClearTable()
        {
            _labels = [];
            _tablePanels = [];

            _tableContainer.ClearChildren();
        }

        #endregion Clear

        #region Utils
        private bool IsCleared(string key) => _accountClears?.Contains(key) == true;
        private bool IsPlanned(string key) => _bounties?.SelectMany(b => b.Value).Contains(key) == true;

        private Color GetBackgroundColor(string encounterKey)
        {
            if (string.IsNullOrEmpty(encounterKey))
                return Colors.Empty;

            return (_tableDrawMode, _encounterDrawMode) switch
            {
                (TableDrawMode.Week, EncounterDrawMode.Neutral)
                    => Colors.Planned,

                (TableDrawMode.Week, EncounterDrawMode.Progression) when IsCleared(encounterKey)
                    => Colors.Done,
                (TableDrawMode.Week, EncounterDrawMode.Progression)
                    => Colors.Planned,

                (TableDrawMode.Areas, EncounterDrawMode.Neutral) when _neverOnTheMenu.Contains(encounterKey)
                    => Colors.None,
                (TableDrawMode.Areas, EncounterDrawMode.Neutral) when IsPlanned(encounterKey)
                    => Colors.Planned,
                (TableDrawMode.Areas, EncounterDrawMode.Neutral)
                    => Colors.Todo,

                (TableDrawMode.Areas, EncounterDrawMode.Progression) when IsCleared(encounterKey)
                    => Colors.Done,
                (TableDrawMode.Areas, EncounterDrawMode.Progression) when IsPlanned(encounterKey)
                    => Colors.Planned,
                (TableDrawMode.Areas, EncounterDrawMode.Progression)
                    => Colors.Todo,

                _ => Colors.Todo
            };
        }

        private string GetTooltip(string currentEncounter)
        {
            string cplTootlip = string.Empty;
            if (_bounties != null && _bounties.SelectMany(b => b.Value).Contains(currentEncounter))
            {
                var allDays = _bounties
                    .Where(b => b.Value.Contains(currentEncounter))
                    .Select(b => _stringsResx.GetString($"day{b.Key}"));

                cplTootlip = $" - {string.Join(", ", allDays)}";
            }

            return $"{_encountersResx.GetString($"{currentEncounter}Tooltip")}{cplTootlip}";
        }
        #endregion Utils

        #region Actions
        private async Task RefreshData()
        {
            _loadingSpinner.Visible = true;

            _accountClears = await _businessService.GetAccountClears(true);

            UpdateColors();

            _loadingSpinner.Visible = false;
        }

        private void ToggleEncounterDrawMode()
        {
            _encounterDrawMode = _encounterDrawMode == EncounterDrawMode.Neutral ?
                EncounterDrawMode.Progression : EncounterDrawMode.Neutral;

            UpdateColors();

            _toggleEncounterDrawModeBtn.Text = GetToggleEncounterDrawModeBtnLabel();
        }

        private void ToggleTableDrawMode()
        {
            _tableDrawMode = _tableDrawMode == TableDrawMode.Week ?
                TableDrawMode.Areas : TableDrawMode.Week;

            ClearTable();
            DrawTable();

            _toggleTableDrawModeBtn.Text = GetToggleTableDrawModeBtnLabel();
        }
        
        #endregion Actions

        #region Layout
        private void TableContainer_ContentResized(object sender, RegionChangedEventArgs e)
        {
            if (_labels?.Count >= 0)
            {
                int columns = 12;
                var parent = _labels.FirstOrDefault()?.Parent as FlowPanel;
                int width = (parent?.ContentRegion.Width - (int)(parent?.OuterControlPadding.X ?? 0) - ((int)(parent?.ControlPadding.X ?? 0) * (columns - 1))) / columns ?? 100;

                foreach (var label in _labels)
                {
                    label.Width = width - 10;
                }
            }
        }

        private void ActionContainer_ContentResized(object sender, RegionChangedEventArgs e)
        {
            if (_buttons?.Count >= 0)
            {
                int columns = 9;
                var parent = _buttons.FirstOrDefault()?.Parent as FlowPanel;
                int width = (parent?.ContentRegion.Width - (int)parent.OuterControlPadding.X - ((int)parent.ControlPadding.X * (columns - 1))) / columns ?? 100;

                foreach (var button in _buttons)
                {
                    button.Width = width;
                }
            }
        }
        #endregion Layout
    }
}