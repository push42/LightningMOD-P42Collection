namespace Turbo.Plugins.Custom.ItemReveal
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;
    using SharpDX.DirectInput;
    using Turbo.Plugins.Default;

    /// <summary>
    /// Item Reveal Plugin - Shows item stats BEFORE identification!
    /// 
    /// TurboHUD can read item affixes even on unidentified items.
    /// This plugin displays the stats so you know immediately if an item is worth keeping.
    /// 
    /// Features:
    /// - Shows stats on unidentified items in inventory
    /// - Shows stats on unidentified items on the ground
    /// - Highlights good rolls (perfection %)
    /// - Ancient/Primal indicator for unidentified items
    /// - Perfection score calculation
    /// 
    /// No more wasting time with the Book of Cain!
    /// </summary>
    public class ItemRevealPlugin : BasePlugin, IInGameTopPainter, IKeyEventHandler, IItemsOnFloorPainter
    {
        #region Settings

        /// <summary>
        /// Key to toggle the reveal panel on/off
        /// </summary>
        public IKeyEvent ToggleKeyEvent { get; set; }

        /// <summary>
        /// Show stats on unidentified items in inventory
        /// </summary>
        public bool ShowInventoryStats { get; set; } = true;

        /// <summary>
        /// Show stats on unidentified items on the ground
        /// </summary>
        public bool ShowGroundStats { get; set; } = true;

        /// <summary>
        /// Show perfection percentage
        /// </summary>
        public bool ShowPerfection { get; set; } = true;

        /// <summary>
        /// Minimum perfection to highlight as "good" (0-100)
        /// </summary>
        public float GoodPerfectionThreshold { get; set; } = 85f;

        /// <summary>
        /// Minimum perfection to highlight as "great" (0-100)
        /// </summary>
        public float GreatPerfectionThreshold { get; set; } = 95f;

        /// <summary>
        /// Show Ancient/Primal status on unidentified items
        /// </summary>
        public bool ShowAncientStatus { get; set; } = true;

        /// <summary>
        /// Only show for legendary items
        /// </summary>
        public bool LegendaryOnly { get; set; } = true;

        /// <summary>
        /// Maximum stats to show in tooltip
        /// </summary>
        public int MaxStatsToShow { get; set; } = 8;

        #endregion

        #region Private Fields

        private bool _enabled = true;
        private IItem _hoveredItem;

        // Fonts
        private IFont _titleFont;
        private IFont _statFont;
        private IFont _perfectionFont;
        private IFont _goodPerfectionFont;
        private IFont _greatPerfectionFont;
        private IFont _ancientFont;
        private IFont _primalFont;
        private IFont _setFont;
        private IFont _hintFont;

        // Brushes
        private IBrush _panelBrush;
        private IBrush _borderBrush;
        private IBrush _legendaryBorderBrush;
        private IBrush _ancientBorderBrush;
        private IBrush _primalBorderBrush;
        private IBrush _unidentifiedBrush;

        // UI
        private IUiElement _inventoryElement;

        #endregion

        public ItemRevealPlugin()
        {
            Enabled = true;
            Order = 50100;
        }

        public override void Load(IController hud)
        {
            base.Load(hud);

            ToggleKeyEvent = Hud.Input.CreateKeyEvent(true, Key.F4, false, false, false);

            // Fonts
            _titleFont = Hud.Render.CreateFont("tahoma", 8, 255, 255, 200, 100, true, false, 200, 0, 0, 0, true);
            _statFont = Hud.Render.CreateFont("tahoma", 7, 255, 220, 220, 220, false, false, 180, 0, 0, 0, true);
            _perfectionFont = Hud.Render.CreateFont("tahoma", 7, 255, 180, 180, 180, false, false, 160, 0, 0, 0, true);
            _goodPerfectionFont = Hud.Render.CreateFont("tahoma", 7, 255, 100, 255, 100, true, false, 160, 0, 0, 0, true);
            _greatPerfectionFont = Hud.Render.CreateFont("tahoma", 7, 255, 255, 200, 50, true, false, 160, 0, 0, 0, true);
            _ancientFont = Hud.Render.CreateFont("tahoma", 7.5f, 255, 255, 150, 50, true, false, 180, 0, 0, 0, true);
            _primalFont = Hud.Render.CreateFont("tahoma", 7.5f, 255, 255, 50, 50, true, false, 180, 0, 0, 0, true);
            _setFont = Hud.Render.CreateFont("tahoma", 7.5f, 255, 0, 255, 0, true, false, 180, 0, 0, 0, true);
            _hintFont = Hud.Render.CreateFont("tahoma", 6.5f, 200, 150, 150, 150, false, false, 140, 0, 0, 0, true);

            // Brushes
            _panelBrush = Hud.Render.CreateBrush(240, 15, 15, 25, 0);
            _borderBrush = Hud.Render.CreateBrush(255, 60, 60, 80, 1.5f);
            _legendaryBorderBrush = Hud.Render.CreateBrush(255, 255, 128, 0, 2f);
            _ancientBorderBrush = Hud.Render.CreateBrush(255, 255, 170, 50, 2f);
            _primalBorderBrush = Hud.Render.CreateBrush(255, 255, 50, 50, 2f);
            _unidentifiedBrush = Hud.Render.CreateBrush(150, 255, 255, 0, 2f);

            _inventoryElement = Hud.Inventory.InventoryMainUiElement;
        }

        public void OnKeyEvent(IKeyEvent keyEvent)
        {
            if (!Hud.Game.IsInGame) return;

            if (ToggleKeyEvent.Matches(keyEvent) && keyEvent.IsPressed)
            {
                _enabled = !_enabled;
            }
        }

        public void PaintItems(IEnumerable<IItem> items, ClipState clipState)
        {
            if (!_enabled) return;
            if (!ShowGroundStats) return;
            if (clipState != ClipState.BeforeClip) return;

            foreach (var item in items)
            {
                if (!ShouldShowStats(item)) continue;
                if (!item.Unidentified) continue;

                // Draw indicator on ground items
                var screenCoord = item.FloorCoordinate.ToScreenCoordinate();
                DrawUnidentifiedIndicator(item, screenCoord.X, screenCoord.Y - 30);
            }
        }

        public void PaintTopInGame(ClipState clipState)
        {
            if (!Hud.Game.IsInGame) return;
            if (!_enabled) return;

            if (clipState == ClipState.BeforeClip)
            {
                // Draw status indicator
                DrawStatusIndicator();
            }

            if (clipState != ClipState.Inventory) return;
            if (!ShowInventoryStats) return;
            if (!_inventoryElement.Visible) return;

            // Find hovered item
            _hoveredItem = null;
            int mouseX = Hud.Window.CursorX;
            int mouseY = Hud.Window.CursorY;

            // Check inventory items
            foreach (var item in Hud.Inventory.ItemsInInventory)
            {
                if (!ShouldShowStats(item)) continue;

                var rect = Hud.Inventory.GetItemRect(item);
                if (rect == RectangleF.Empty) continue;

                // Highlight unidentified items
                if (item.Unidentified)
                {
                    _unidentifiedBrush.DrawRectangle(rect);
                }

                // Check if mouse is over this item
                if (mouseX >= rect.X && mouseX <= rect.X + rect.Width &&
                    mouseY >= rect.Y && mouseY <= rect.Y + rect.Height)
                {
                    _hoveredItem = item;
                }
            }

            // Check stash items if stash is open
            if (Hud.Inventory.StashMainUiElement?.Visible == true)
            {
                foreach (var item in Hud.Inventory.ItemsInStash)
                {
                    if (!ShouldShowStats(item)) continue;

                    var rect = Hud.Inventory.GetItemRect(item);
                    if (rect == RectangleF.Empty) continue;

                    if (item.Unidentified)
                    {
                        _unidentifiedBrush.DrawRectangle(rect);
                    }

                    if (mouseX >= rect.X && mouseX <= rect.X + rect.Width &&
                        mouseY >= rect.Y && mouseY <= rect.Y + rect.Height)
                    {
                        _hoveredItem = item;
                    }
                }
            }

            // Draw stats tooltip for hovered item
            if (_hoveredItem != null)
            {
                DrawItemStatsTooltip(_hoveredItem, mouseX + 20, mouseY);
            }
        }

        private bool ShouldShowStats(IItem item)
        {
            if (item == null || item.SnoItem == null) return false;
            if (LegendaryOnly && !item.IsLegendary) return false;
            return true;
        }

        private void DrawStatusIndicator()
        {
            float x = Hud.Window.Size.Width * 0.005f;
            float y = Hud.Window.Size.Height * 0.70f;
            float w = 110;
            float h = 32;

            _panelBrush.DrawRectangle(x, y, w, h);
            _borderBrush.DrawRectangle(x, y, w, h);

            var titleLayout = _titleFont.GetTextLayout("Item Reveal");
            _titleFont.DrawText(titleLayout, x + 6, y + 4);

            string status = _enabled ? "ON [F4]" : "OFF [F4]";
            var statusFont = _enabled ? _goodPerfectionFont : _perfectionFont;
            var statusLayout = statusFont.GetTextLayout(status);
            statusFont.DrawText(statusLayout, x + 6, y + 4 + titleLayout.Metrics.Height);
        }

        private void DrawUnidentifiedIndicator(IItem item, float x, float y)
        {
            // Show Ancient/Primal status and perfection above ground items
            string text = "";
            IFont font = _statFont;

            if (item.AncientRank == 2)
            {
                text = "⚡ PRIMAL";
                font = _primalFont;
            }
            else if (item.AncientRank == 1)
            {
                text = "★ ANCIENT";
                font = _ancientFont;
            }
            else if (item.SetSno != 0)
            {
                text = "SET";
                font = _setFont;
            }
            else
            {
                text = "LEGENDARY";
                font = _titleFont;
            }

            // Add perfection if available
            if (ShowPerfection && item.Perfection > 0)
            {
                float perf = (float)(item.Perfection * 100);
                text += string.Format(" {0:F0}%", perf);
            }

            var layout = font.GetTextLayout(text);
            font.DrawText(layout, x - layout.Metrics.Width / 2, y);
        }

        private void DrawItemStatsTooltip(IItem item, float x, float y)
        {
            // Build tooltip content
            var lines = new List<TooltipLine>();

            // Item name
            string name = item.SnoItem.NameLocalized ?? item.SnoItem.NameEnglish ?? "Unknown";
            lines.Add(new TooltipLine { Text = name, Font = _titleFont });

            // Ancient/Primal status
            if (ShowAncientStatus)
            {
                if (item.AncientRank == 2)
                {
                    lines.Add(new TooltipLine { Text = "⚡ PRIMAL ANCIENT", Font = _primalFont });
                }
                else if (item.AncientRank == 1)
                {
                    lines.Add(new TooltipLine { Text = "★ ANCIENT", Font = _ancientFont });
                }
            }

            // Set name
            if (item.SetSno != 0)
            {
                lines.Add(new TooltipLine { Text = "[Set Item]", Font = _setFont });
            }

            // Unidentified indicator
            if (item.Unidentified)
            {
                lines.Add(new TooltipLine { Text = "UNIDENTIFIED - Stats revealed:", Font = _hintFont });
            }

            // Overall perfection
            if (ShowPerfection && item.Perfection > 0)
            {
                float perf = (float)(item.Perfection * 100);
                IFont perfFont = _perfectionFont;
                
                if (perf >= GreatPerfectionThreshold)
                    perfFont = _greatPerfectionFont;
                else if (perf >= GoodPerfectionThreshold)
                    perfFont = _goodPerfectionFont;

                lines.Add(new TooltipLine { Text = string.Format("Overall: {0:F1}%", perf), Font = perfFont });
            }

            // Separator
            lines.Add(new TooltipLine { Text = "─────────────", Font = _hintFont });

            // Item stats from Perfections
            if (item.Perfections != null && item.Perfections.Length > 0)
            {
                int count = 0;
                foreach (var perf in item.Perfections)
                {
                    if (count >= MaxStatsToShow) break;
                    if (perf?.Attribute == null) continue;

                    string statName = GetStatName(perf);
                    string statValue = FormatStatValue(perf);
                    string perfStr = "";

                    // Calculate this stat's perfection
                    if (ShowPerfection && perf.Max > perf.Min)
                    {
                        double statPerf = (perf.Cur - perf.Min) / (perf.Max - perf.Min) * 100;
                        perfStr = string.Format(" ({0:F0}%)", statPerf);
                    }

                    IFont statValueFont = _statFont;
                    if (!string.IsNullOrEmpty(perfStr))
                    {
                        double statPerf = (perf.Cur - perf.Min) / (perf.Max - perf.Min) * 100;
                        if (statPerf >= GreatPerfectionThreshold)
                            statValueFont = _greatPerfectionFont;
                        else if (statPerf >= GoodPerfectionThreshold)
                            statValueFont = _goodPerfectionFont;
                    }

                    lines.Add(new TooltipLine { Text = statName + ": " + statValue + perfStr, Font = statValueFont });
                    count++;
                }
            }
            // Fallback to StatList if no Perfections
            else if (item.StatList != null)
            {
                int count = 0;
                foreach (var stat in item.StatList)
                {
                    if (count >= MaxStatsToShow) break;
                    if (stat?.Attribute == null) continue;

                    string statName = stat.Attribute.Code ?? "Unknown";
                    string statValue = stat.DoubleValue.ToString("F0");

                    lines.Add(new TooltipLine { Text = statName + ": " + statValue, Font = _statFont });
                    count++;
                }
            }

            // Affixes (legendary powers)
            if (item.Affixes != null && item.Affixes.Length > 0)
            {
                lines.Add(new TooltipLine { Text = "─────────────", Font = _hintFont });
                
                foreach (var affix in item.Affixes)
                {
                    if (affix == null) continue;
                    string affixName = affix.NameLocalized ?? affix.NameEnglish ?? "";
                    if (!string.IsNullOrEmpty(affixName))
                    {
                        // Truncate long names
                        if (affixName.Length > 40)
                            affixName = affixName.Substring(0, 37) + "...";
                        lines.Add(new TooltipLine { Text = "• " + affixName, Font = _titleFont });
                    }
                }
            }

            // Calculate tooltip size
            float maxWidth = 0;
            float totalHeight = 0;
            float padding = 8;

            foreach (var line in lines)
            {
                var layout = line.Font.GetTextLayout(line.Text);
                line.Layout = layout;
                if (layout.Metrics.Width > maxWidth)
                    maxWidth = layout.Metrics.Width;
                totalHeight += layout.Metrics.Height + 2;
            }

            float tooltipW = maxWidth + padding * 2;
            float tooltipH = totalHeight + padding * 2;

            // Keep tooltip on screen
            if (x + tooltipW > Hud.Window.Size.Width - 10)
                x = Hud.Window.Size.Width - tooltipW - 10;
            if (y + tooltipH > Hud.Window.Size.Height - 10)
                y = Hud.Window.Size.Height - tooltipH - 10;

            // Draw background
            _panelBrush.DrawRectangle(x, y, tooltipW, tooltipH);

            // Draw border based on item type
            IBrush borderBrush = _borderBrush;
            if (item.AncientRank == 2)
                borderBrush = _primalBorderBrush;
            else if (item.AncientRank == 1)
                borderBrush = _ancientBorderBrush;
            else if (item.IsLegendary)
                borderBrush = _legendaryBorderBrush;

            borderBrush.DrawRectangle(x, y, tooltipW, tooltipH);

            // Draw lines
            float lineY = y + padding;
            foreach (var line in lines)
            {
                line.Font.DrawText(line.Layout, x + padding, lineY);
                lineY += line.Layout.Metrics.Height + 2;
            }
        }

        private string GetStatName(IItemPerfection perf)
        {
            if (perf.Attribute == null) return "Unknown";

            string code = perf.Attribute.Code ?? "";
            
            // Common stat translations
            switch (code)
            {
                case "Strength_Item": return "Strength";
                case "Dexterity_Item": return "Dexterity";
                case "Intelligence_Item": return "Intelligence";
                case "Vitality_Item": return "Vitality";
                case "Armor_Item": return "Armor";
                case "Armor_Bonus_Percent": return "Armor %";
                case "Hitpoints_Max_Percent_Bonus_Item": return "Life %";
                case "Hitpoints_Regen_Per_Second": return "Life Regen";
                case "Hitpoints_On_Hit": return "Life on Hit";
                case "Hitpoints_On_Kill": return "Life on Kill";
                case "Resource_Cost_Reduction_Percent_All": return "Resource Cost Reduction";
                case "Cooldown_Reduction_Percent_All": return "Cooldown Reduction";
                case "Attacks_Per_Second_Percent": return "Attack Speed %";
                case "Attacks_Per_Second_Item": return "Attack Speed";
                case "Crit_Percent_Bonus_Capped": return "Crit Chance";
                case "Crit_Damage_Percent": return "Crit Damage";
                case "Damage_Min_Physical": return "Min Damage";
                case "Damage_Delta_Physical": return "Damage Range";
                case "Damage_Weapon_Percent_All": return "Damage %";
                case "Damage_Percent_Bonus_Vs_Elites": return "Elite Damage %";
                case "Resistance_All": return "All Resist";
                case "Resistance_Physical": return "Physical Resist";
                case "Resistance_Fire": return "Fire Resist";
                case "Resistance_Lightning": return "Lightning Resist";
                case "Resistance_Cold": return "Cold Resist";
                case "Resistance_Poison": return "Poison Resist";
                case "Resistance_Arcane": return "Arcane Resist";
                case "Gold_Find": return "Gold Find";
                case "Magic_Find": return "Magic Find";
                case "Experience_Bonus_Percent": return "Experience %";
                case "Movement_Scalar": return "Movement Speed";
                case "Sockets": return "Sockets";
                case "Thorns_Fixed_Physical": return "Thorns";
                case "Power_Damage_Percent_Bonus": return "Skill Damage %";
                case "Area_Damage_Percent": return "Area Damage";
                default: 
                    // Try to make the code more readable
                    return code.Replace("_Item", "").Replace("_", " ");
            }
        }

        private string FormatStatValue(IItemPerfection perf)
        {
            double value = perf.Cur;
            string code = perf.Attribute?.Code ?? "";

            // Format percentages
            if (code.Contains("Percent") || code.Contains("Crit") || 
                code.Contains("Reduction") || code.Contains("Find") ||
                code.Contains("Bonus"))
            {
                return string.Format("{0:F1}%", value * 100);
            }

            // Format whole numbers
            if (value >= 1000)
                return string.Format("{0:N0}", value);

            return string.Format("{0:F0}", value);
        }

        private class TooltipLine
        {
            public string Text;
            public IFont Font;
            public ITextLayout Layout;
        }
    }
}
