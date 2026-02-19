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
        private readonly BusinessService _businessService;

        private LoadingSpinner _loadingSpinner;
        private FlowPanel _tableContainer;

        private List<Label> _labels = new();
        private readonly List<StandardButton> _buttons = new();

        private List<Area> _areas;

        private List<string> _accountClears;
        private Dictionary<int, List<string>> _bounties;
        private List<string> _neverOnTheMenu;
        private bool _showClears = true;
        private bool _showWeek = false;

        private List<(Panel, Label, string)> _tablePanels = new();

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

            _areas = businessService.GetAreas();
            _bounties = businessService.GetEventsForCurrentWeek();
            _neverOnTheMenu = businessService.GetNeverOnTheMenu();

            _stringsResx = strings.ResourceManager;
            _areasResx = areas.ResourceManager;
            _encountersResx = encounters.ResourceManager;
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

            AddHeaders(_tableContainer);

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
                Parent = actionContainer
            });
            button.Click += async (s, e) => await RefreshData();

            StandardButton toggleButton;
            _buttons.Add(toggleButton = new Controls.StandardButton()
            {
                SetLocalizedText = () => strings.MainWindow_Button_Toggle_Label,
                SetLocalizedTooltip = () => strings.MainWindow_Button_Toggle_Tooltip,
                Parent = actionContainer
            });
            toggleButton.Click += (s, e) => ToggleClears();

            StandardButton drawWeeklyClearsButton;
            _buttons.Add(drawWeeklyClearsButton = new Controls.StandardButton()
            {
                SetLocalizedText = () => strings.MainWindow_Button_Toggle_Week_Label,
                SetLocalizedTooltip = () => strings.MainWindow_Button_Toggle_Week_Tooltip,
                Parent = actionContainer
            });
            drawWeeklyClearsButton.Click += (s, e) => ToggleWeeklyClears();

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

            DrawLegend(legendContainer);

            #endregion Legend

            DrawData();
        }

        public void InjectData(List<string> accountClears)
        {
            _accountClears = accountClears;

            if (!_showWeek)
            {
                DrawData();
            }
        }

        private void AddHeaders(FlowPanel container)
        {
            foreach (var area in _areas)
            {
                try
                {
                    _labels.Add(UiUtils.CreateLabel(() => _areasResx.GetString($"{area.Key}Label"), () => _areasResx.GetString($"{area.Key}Tooltip"), container).label);
                }
                catch (System.Exception)
                {
                }
            }
        }

        private void AddWeekHeaders(FlowPanel container)
        {
            List<int> days = [0, 1, 2, 3, 4, 5, 6];
            foreach (var day in days)
            {
                _labels.Add(UiUtils.CreateLabel(() => _stringsResx.GetString($"day{day}"), () => "", container, amount: 7).label);
            }
        }

        private void DrawData()
        {
            ClearLines();
            DrawLines();
        }

        private void ToggleWeeklyClears()
        {
            ClearHeaders();
            ClearLines();

            if (_showWeek)
            {
                AddHeaders(_tableContainer);
                DrawLines();
            }
            else
            {
                AddWeekHeaders(_tableContainer);
                DrawWeeklyClears();
            }

            _showWeek = !_showWeek;
        }

        private void DrawWeeklyClears()
        {
            // Workaround for localization change out of range bug
            List<int> rows = [0, 1, 2, 3];
            foreach (var row in rows)
            {
                foreach (var bounty in _bounties)
                {
                    var encounter = bounty.Value[row];

                    var label = UiUtils.CreateLabel(() => _encountersResx.GetString($"{encounter}Label"), () => GetTooltip(encounter), _tableContainer, amount: 7);
                    label.panel.BackgroundColor = Colors.Todo;
                    _tablePanels.Add((label.panel, label.label, encounter));
                }
            }
        }

        private void ClearHeaders()
        {
            for (int i = _labels.Count - 1; i >= 0; i--)
            {
                _labels.ElementAt(i).Dispose();
            }

            _labels = [];
        }

        private void ClearLines()
        {
            for (int i = _tablePanels.Count - 1; i >= 0; i--)
            {
                _tablePanels.ElementAt(i).Item1.Dispose();
            }

            _tablePanels = [];
        }

        private void DrawLines()
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

                        try
                        {
                            var label = UiUtils.CreateLabel(() => _encountersResx.GetString($"{currentEncounter.Key}Label"), () => GetTooltip(currentEncounter.Key), _tableContainer);
                            label.panel.BackgroundColor = GetBackgroundColor(currentEncounter.Key);
                            _tablePanels.Add((label.panel, label.label, currentEncounter.Key));
                        }
                        catch (System.Exception)
                        {
                        }
                    }
                    else
                    {
                        var emptyLabel = UiUtils.CreateLabel(() => "", () => "", _tableContainer);
                        _tablePanels.Add((emptyLabel.panel, emptyLabel.label, string.Empty));
                    }
                }
                count++;
            }
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

        private async Task RefreshData()
        {
            _loadingSpinner.Visible = true;

            _accountClears = await _businessService.GetAccountClears(true);

            DrawData();

            _loadingSpinner.Visible = false;
        }

        private void ToggleClears()
        {
            _showClears = !_showClears;

            foreach (var tablePanel in _tablePanels)
            {
                tablePanel.Item1.BackgroundColor = GetBackgroundColor(tablePanel.Item3);
            }
        }

        private Color GetBackgroundColor(string encounterKey)
        {
            if (string.IsNullOrEmpty(encounterKey))
            {
                return Colors.Empty;
            }

            if (_showClears && _accountClears != null && _accountClears.Contains(encounterKey))
            {
                return Colors.Done;
            }
            else if (_bounties != null && _bounties.SelectMany(b => b.Value).Contains(encounterKey))
            {
                return Colors.Planned;
            }
            else if (!_showClears && _neverOnTheMenu.Contains(encounterKey))
            {
                return Colors.None;
            }

            return Colors.Todo;
        }

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
    }
}