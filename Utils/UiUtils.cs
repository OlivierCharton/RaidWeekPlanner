using Blish_HUD.Controls;
using System;
using FlowPanel = RaidWeekPlanner.UI.Controls.FlowPanel;
using Label = RaidWeekPlanner.UI.Controls.Label;

namespace RaidWeekPlanner.Utils
{
    public static class UiUtils
    {
        public static (FlowPanel panel, Label label) CreateLabel(Func<string> labelText, Func<string> tooltipText, FlowPanel parent, int amount = 12, HorizontalAlignment alignment = HorizontalAlignment.Center)
        {
            FlowPanel panel = new()
            {
                Parent = parent,
                FlowDirection = ControlFlowDirection.SingleLeftToRight,
                ControlPadding = new(5),
                SetLocalizedTooltip = tooltipText,
                HeightSizingMode = SizingMode.AutoSize,
            };

            Label label = new()
            {
                Parent = panel,
                SetLocalizedText = labelText,
                Height = 25,
                VerticalAlignment = VerticalAlignment.Middle,
                HorizontalAlignment = alignment,
                SetLocalizedTooltip = tooltipText,
            };

            void FitToPanel(object sender, RegionChangedEventArgs e)
            {
                panel.Invalidate();
            }

            void FitToParent(object sender, RegionChangedEventArgs e)
            {
                int width = (parent.ContentRegion.Width - (int)(parent.ControlPadding.X * (amount - 1))) / amount;
                panel.Width = width;
                label.Width = width;
                panel.Invalidate();
            }

            panel.ContentResized += FitToPanel;
            parent.ContentResized += FitToParent;

            return new(panel, label);
        }
    }
}
