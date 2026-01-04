namespace Turbo.Plugins.Custom.ItemReveal
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;
    using SharpDX.DirectInput;
    using Turbo.Plugins.Custom.Core;
    using Turbo.Plugins.Default;

    /// <summary>
    /// Item Reveal Plugin v2.3 - Core Edition
    /// 
    /// Now fully integrated with the Core Plugin Framework!
    /// 
    /// Features:
    /// - Ancient/Primal detection on unidentified items
    /// - Stat ranges preview
    /// - Full stats for identified items
    /// - Settings panel in Core Hub
    /// </summary>
    public class ItemRevealPlugin : CustomPluginBase, IInGameTopPainter, IKeyEventHandler, IInGameWorldPainter
    {
        #region Plugin Metadata

        public override string PluginId => "item-reveal";
        public override string PluginName => "Item Reveal";
        public override string PluginDescription => "See Ancient/Primal status on unidentified items";
        public override string PluginVersion => "2.3.0";
        public override string PluginCategory => "inventory";
        public override string PluginIcon => "🔍";
        public override bool HasSettings => true;

        #endregion

        #region Settings

        public IKeyEvent ToggleKeyEvent { get; set; }
        public IKeyEvent DebugKeyEvent { get; set; }
        public bool ShowInventoryStats { get; set; } = true;
        public bool ShowGroundStats { get; set; } = true;
        public bool ShowPerfection { get; set; } = true;
        public float GoodPerfectionThreshold { get; set; } = 85f;
        public float GreatPerfectionThreshold { get; set; } = 95f;
        public bool LegendaryOnly { get; set; } = true;
        public int MaxStatsToShow { get; set; } = 12;

        #endregion

        #region Private Fields

        private bool _debugMode;
        private IItem _hoveredItem;
        private IUiElement _inventoryElement;

        // Fallback fonts
        private IFont _titleFont;
        private IFont _statFont;
        private IFont _ancientFont;
        private IFont _primalFont;
        private IFont _setFont;
        private IFont _hintFont;
        private IFont _debugFont;

        // Fallback brushes
        private IBrush _panelBrush;
        private IBrush _borderBrush;
        private IBrush _ancientBrush;
        private IBrush _primalBrush;
        private IBrush _unidBrush;

        #endregion

        #region Initialization

        public ItemRevealPlugin()
        {
            Enabled = true;
            Order = 50100;
        }

        public override void Load(IController hud)
        {
            base.Load(hud);

            ToggleKeyEvent = Hud.Input.CreateKeyEvent(true, Key.F4, false, false, false);
            DebugKeyEvent = Hud.Input.CreateKeyEvent(true, Key.F5, false, false, false);

            _inventoryElement = Hud.Inventory.InventoryMainUiElement;

            InitializeFallbackResources();
            Log("Item Reveal loaded");
        }

        private void InitializeFallbackResources()
        {
            _titleFont = Hud.Render.CreateFont("tahoma", 8, 255, 255, 200, 100, true, false, 200, 0, 0, 0, true);
            _statFont = Hud.Render.CreateFont("tahoma", 7, 255, 220, 220, 220, false, false, 180, 0, 0, 0, true);
            _ancientFont = Hud.Render.CreateFont("tahoma", 8, 255, 255, 150, 50, true, false, 200, 0, 0, 0, true);
            _primalFont = Hud.Render.CreateFont("tahoma", 8, 255, 255, 50, 50, true, false, 200, 0, 0, 0, true);
            _setFont = Hud.Render.CreateFont("tahoma", 7.5f, 255, 0, 255, 0, true, false, 180, 0, 0, 0, true);
            _hintFont = Hud.Render.CreateFont("tahoma", 6.5f, 200, 150, 150, 150, false, false, 140, 0, 0, 0, true);
            _debugFont = Hud.Render.CreateFont("consolas", 6f, 255, 150, 200, 255, false, false, 140, 0, 0, 0, true);

            _panelBrush = Hud.Render.CreateBrush(245, 12, 12, 20, 0);
            _borderBrush = Hud.Render.CreateBrush(255, 60, 60, 80, 1.5f);
            _ancientBrush = Hud.Render.CreateBrush(150, 255, 170, 50, 3f);
            _primalBrush = Hud.Render.CreateBrush(180, 255, 50, 50, 3f);
            _unidBrush = Hud.Render.CreateBrush(120, 255, 255, 0, 2f);
        }

        #endregion

        #region Settings Panel

        public override void DrawSettings(IController hud, RectangleF rect, Dictionary<string, RectangleF> clickAreas, int scrollOffset)
        {
            float x = rect.X, y = rect.Y, w = rect.Width;

            // Display Settings
            y += DrawSettingsHeader(x, y, "Display Settings");
            y += 8;

            y += DrawToggleSetting(x, y, w, "Show in Inventory", ShowInventoryStats, clickAreas, "toggle_inventory");
            y += DrawToggleSetting(x, y, w, "Show on Ground", ShowGroundStats, clickAreas, "toggle_ground");
            y += DrawToggleSetting(x, y, w, "Show Perfection %", ShowPerfection, clickAreas, "toggle_perfection");
            y += DrawToggleSetting(x, y, w, "Legendary Only", LegendaryOnly, clickAreas, "toggle_legonly");

            y += 12;

            // Thresholds
            y += DrawSettingsHeader(x, y, "Perfection Thresholds");
            y += 8;

            y += DrawSelectorSetting(x, y, w, "Good", $"{GoodPerfectionThreshold:F0}%", clickAreas, "sel_good");
            y += DrawSelectorSetting(x, y, w, "Great", $"{GreatPerfectionThreshold:F0}%", clickAreas, "sel_great");

            y += 12;

            // Stats
            y += DrawSettingsHeader(x, y, "Stat Display");
            y += 8;

            y += DrawSelectorSetting(x, y, w, "Max Stats", MaxStatsToShow.ToString(), clickAreas, "sel_maxstats");

            y += 16;
            y += DrawSettingsHint(x, y, "[F4] Toggle • [F5] Debug Mode");

            if (_debugMode)
            {
                y += 8;
                y += DrawSettingsHint(x, y, "Debug mode is ON");
            }
        }

        public override void HandleSettingsClick(string clickId)
        {
            switch (clickId)
            {
                case "toggle_inventory": ShowInventoryStats = !ShowInventoryStats; break;
                case "toggle_ground": ShowGroundStats = !ShowGroundStats; break;
                case "toggle_perfection": ShowPerfection = !ShowPerfection; break;
                case "toggle_legonly": LegendaryOnly = !LegendaryOnly; break;
                case "sel_good_prev": GoodPerfectionThreshold = Math.Max(50, GoodPerfectionThreshold - 5); break;
                case "sel_good_next": GoodPerfectionThreshold = Math.Min(100, GoodPerfectionThreshold + 5); break;
                case "sel_great_prev": GreatPerfectionThreshold = Math.Max(50, GreatPerfectionThreshold - 5); break;
                case "sel_great_next": GreatPerfectionThreshold = Math.Min(100, GreatPerfectionThreshold + 5); break;
                case "sel_maxstats_prev": MaxStatsToShow = Math.Max(4, MaxStatsToShow - 2); break;
                case "sel_maxstats_next": MaxStatsToShow = Math.Min(20, MaxStatsToShow + 2); break;
            }
            SavePluginSettings();
        }

        protected override object GetSettingsObject() => new RevealSettings
        {
            ShowInventoryStats = this.ShowInventoryStats,
            ShowGroundStats = this.ShowGroundStats,
            ShowPerfection = this.ShowPerfection,
            GoodPerfectionThreshold = this.GoodPerfectionThreshold,
            GreatPerfectionThreshold = this.GreatPerfectionThreshold,
            LegendaryOnly = this.LegendaryOnly,
            MaxStatsToShow = this.MaxStatsToShow
        };

        protected override void ApplySettingsObject(object settings)
        {
            if (settings is RevealSettings s)
            {
                ShowInventoryStats = s.ShowInventoryStats;
                ShowGroundStats = s.ShowGroundStats;
                ShowPerfection = s.ShowPerfection;
                GoodPerfectionThreshold = s.GoodPerfectionThreshold;
                GreatPerfectionThreshold = s.GreatPerfectionThreshold;
                LegendaryOnly = s.LegendaryOnly;
                MaxStatsToShow = s.MaxStatsToShow;
            }
        }

        private class RevealSettings : PluginSettingsBase
        {
            public bool ShowInventoryStats { get; set; }
            public bool ShowGroundStats { get; set; }
            public bool ShowPerfection { get; set; }
            public float GoodPerfectionThreshold { get; set; }
            public float GreatPerfectionThreshold { get; set; }
            public bool LegendaryOnly { get; set; }
            public int MaxStatsToShow { get; set; }
        }

        #endregion

        #region Font/Brush Access

        private IFont TitleFont => HasCore ? Core.FontTitle : _titleFont;
        private IFont BodyFont => HasCore ? Core.FontBody : _statFont;
        private IFont SmallFont => HasCore ? Core.FontSmall : _hintFont;
        private IFont MonoFont => HasCore ? Core.FontMono : _debugFont;
        private IBrush PanelBg => HasCore ? Core.SurfaceBase : _panelBrush;
        private IBrush PanelBorder => HasCore ? Core.BorderDefault : _borderBrush;

        #endregion

        #region Key Handler

        public void OnKeyEvent(IKeyEvent keyEvent)
        {
            if (!Hud.Game.IsInGame) return;

            if (ToggleKeyEvent.Matches(keyEvent) && keyEvent.IsPressed)
            {
                Enabled = !Enabled;
                SetCoreStatus($"Item Reveal {(Enabled ? "ON" : "OFF")}", 
                             Enabled ? StatusType.Success : StatusType.Warning);
            }
            
            if (DebugKeyEvent.Matches(keyEvent) && keyEvent.IsPressed)
                _debugMode = !_debugMode;
        }

        #endregion

        #region Rendering

        public void PaintWorld(WorldLayer layer)
        {
            if (!Enabled || !ShowGroundStats || layer != WorldLayer.Ground) return;

            foreach (var item in Hud.Game.Items.Where(i => i.Location == ItemLocation.Floor))
            {
                if (!ShouldShowStats(item)) continue;
                var screenCoord = item.FloorCoordinate.ToScreenCoordinate();
                DrawGroundLabel(item, screenCoord.X, screenCoord.Y - 35);
            }
        }

        public override void PaintTopInGame(ClipState clipState)
        {
            // Call base to ensure Core registration
            base.PaintTopInGame(clipState);
            
            if (!Hud.Game.IsInGame || !Enabled) return;

            if (clipState == ClipState.BeforeClip)
                DrawStatusIndicator();

            if (clipState != ClipState.Inventory || !ShowInventoryStats || !_inventoryElement.Visible) return;

            _hoveredItem = null;
            int mouseX = Hud.Window.CursorX;
            int mouseY = Hud.Window.CursorY;

            foreach (var item in Hud.Inventory.ItemsInInventory)
            {
                if (!ShouldShowStats(item)) continue;
                var rect = Hud.Inventory.GetItemRect(item);
                if (rect == RectangleF.Empty) continue;

                DrawItemHighlight(item, rect);
                if (mouseX >= rect.X && mouseX <= rect.X + rect.Width &&
                    mouseY >= rect.Y && mouseY <= rect.Y + rect.Height)
                    _hoveredItem = item;
            }

            if (Hud.Inventory.StashMainUiElement?.Visible == true)
            {
                foreach (var item in Hud.Inventory.ItemsInStash)
                {
                    if (!ShouldShowStats(item)) continue;
                    var rect = Hud.Inventory.GetItemRect(item);
                    if (rect == RectangleF.Empty) continue;

                    DrawItemHighlight(item, rect);
                    if (mouseX >= rect.X && mouseX <= rect.X + rect.Width &&
                        mouseY >= rect.Y && mouseY <= rect.Y + rect.Height)
                        _hoveredItem = item;
                }
            }

            if (_hoveredItem != null)
                DrawItemTooltip(_hoveredItem, mouseX + 20, mouseY);
        }

        private void DrawStatusIndicator()
        {
            // Don't draw standalone panel when Core sidebar is available
            if (HasCore) return;
            
            float x = Hud.Window.Size.Width * 0.005f;
            float y = Hud.Window.Size.Height * 0.70f;
            float w = 145, h = 28;

            PanelBg.DrawRectangle(x, y, w, h);
            PanelBorder.DrawRectangle(x, y, w, h);

            string text = $"🔍 Reveal {(Enabled ? "ON" : "OFF")}{(_debugMode ? " [DBG]" : "")}";
            var layout = SmallFont.GetTextLayout(text);
            SmallFont.DrawText(layout, x + 8, y + (h - layout.Metrics.Height) / 2);
        }

        private void DrawItemHighlight(IItem item, RectangleF rect)
        {
            if (!item.Unidentified) return;
            
            if (item.AncientRank == 2)
                _primalBrush.DrawRectangle(rect);
            else if (item.AncientRank == 1)
                _ancientBrush.DrawRectangle(rect);
            else
                _unidBrush.DrawRectangle(rect);
        }

        private void DrawGroundLabel(IItem item, float x, float y)
        {
            string text;
            IFont font;
            
            if (item.AncientRank == 2) { text = "!! PRIMAL !!"; font = _primalFont; }
            else if (item.AncientRank == 1) { text = "* ANCIENT *"; font = _ancientFont; }
            else if (item.SetSno != 0) { text = "[SET]"; font = _setFont; }
            else return;

            var layout = font.GetTextLayout(text);
            font.DrawText(layout, x - layout.Metrics.Width / 2, y);
        }

        private void DrawItemTooltip(IItem item, float x, float y)
        {
            var lines = new List<TooltipLine>();

            // Item name
            string name = item.SnoItem.NameLocalized ?? item.SnoItem.NameEnglish ?? "Unknown";
            lines.Add(new TooltipLine { Text = name, Font = TitleFont });

            // Ancient/Primal
            if (item.AncientRank == 2)
                lines.Add(new TooltipLine { Text = "!! PRIMAL ANCIENT !!", Font = _primalFont });
            else if (item.AncientRank == 1)
                lines.Add(new TooltipLine { Text = "* ANCIENT *", Font = _ancientFont });

            if (item.SetSno != 0)
                lines.Add(new TooltipLine { Text = "[Set Item]", Font = _setFont });

            lines.Add(new TooltipLine { Text = "────────────────────", Font = SmallFont });

            if (item.Unidentified)
            {
                lines.Add(new TooltipLine { Text = "⚠ UNIDENTIFIED", Font = HasCore ? Core.FontWarning : _ancientFont });
                lines.Add(new TooltipLine { Text = "", Font = SmallFont });
                
                lines.Add(new TooltipLine { Text = "✓ Confirmed:", Font = HasCore ? Core.FontSuccess : _setFont });
                string quality = item.AncientRank == 2 ? "PRIMAL" : item.AncientRank == 1 ? "ANCIENT" : "Normal";
                lines.Add(new TooltipLine { Text = $"  Quality: {quality}", Font = BodyFont });
                lines.Add(new TooltipLine { Text = $"  Set: {(item.SetSno != 0 ? "Yes" : "No")}", Font = BodyFont });
                
                // Possible stats
                if (item.Perfections != null && item.Perfections.Length > 0)
                {
                    lines.Add(new TooltipLine { Text = "", Font = SmallFont });
                    lines.Add(new TooltipLine { Text = "📊 Possible Stats:", Font = HasCore ? Core.FontAccent : _titleFont });
                    
                    int count = 0;
                    foreach (var perf in item.Perfections)
                    {
                        if (count >= 6) break;
                        if (perf?.Attribute == null) continue;
                        
                        string statName = GetStatName(perf);
                        string range = FormatStatRange(perf);
                        lines.Add(new TooltipLine { Text = $"  {statName}: {range}", Font = MonoFont });
                        count++;
                    }
                }
                
                lines.Add(new TooltipLine { Text = "", Font = SmallFont });
                lines.Add(new TooltipLine { Text = "Actual values hidden until ID", Font = SmallFont });
            }
            else
            {
                // Identified item - show full stats
                if (item.Perfection > 0 && ShowPerfection)
                {
                    float perf = (float)(item.Perfection * 100);
                    var perfFont = perf >= GreatPerfectionThreshold ? (HasCore ? Core.FontSuccess : _setFont) :
                                   perf >= GoodPerfectionThreshold ? (HasCore ? Core.FontSuccess : _setFont) : BodyFont;
                    lines.Add(new TooltipLine { Text = $"Perfection: {perf:F1}%", Font = perfFont });
                }

                if (item.Perfections != null)
                {
                    int count = 0;
                    foreach (var perf in item.Perfections)
                    {
                        if (count >= MaxStatsToShow) break;
                        if (perf?.Attribute == null) continue;

                        string statName = GetStatName(perf);
                        string statValue = FormatStatValue(perf);
                        lines.Add(new TooltipLine { Text = $"{statName}: {statValue}", Font = BodyFont });
                        count++;
                    }
                }
            }

            // Debug mode
            if (_debugMode)
            {
                lines.Add(new TooltipLine { Text = "── DEBUG ──", Font = MonoFont });
                lines.Add(new TooltipLine { Text = $"Seed: {item.Seed}", Font = MonoFont });
                lines.Add(new TooltipLine { Text = $"AncientRank: {item.AncientRank}", Font = MonoFont });
                lines.Add(new TooltipLine { Text = $"Perfections: {item.Perfections?.Length ?? 0}", Font = MonoFont });
            }

            // Draw tooltip
            DrawTooltipBox(lines, x, y, item);
        }

        private void DrawTooltipBox(List<TooltipLine> lines, float x, float y, IItem item)
        {
            float maxWidth = 0, totalHeight = 0, padding = 8;
            foreach (var line in lines)
            {
                var layout = line.Font.GetTextLayout(line.Text);
                line.Width = layout.Metrics.Width;
                line.Height = layout.Metrics.Height;
                if (line.Width > maxWidth) maxWidth = line.Width;
                totalHeight += line.Height + 1;
            }

            float tooltipW = maxWidth + padding * 2;
            float tooltipH = totalHeight + padding * 2;

            // Keep on screen
            if (x + tooltipW > Hud.Window.Size.Width - 10) x = Hud.Window.Size.Width - tooltipW - 10;
            if (y + tooltipH > Hud.Window.Size.Height - 10) y = Hud.Window.Size.Height - tooltipH - 10;
            if (x < 10) x = 10;
            if (y < 10) y = 10;

            PanelBg.DrawRectangle(x, y, tooltipW, tooltipH);
            
            // Border based on ancient rank
            IBrush border = item.AncientRank == 2 ? _primalBrush :
                           item.AncientRank == 1 ? _ancientBrush : PanelBorder;
            border.DrawRectangle(x, y, tooltipW, tooltipH);

            float lineY = y + padding;
            foreach (var line in lines)
            {
                var layout = line.Font.GetTextLayout(line.Text);
                line.Font.DrawText(layout, x + padding, lineY);
                lineY += line.Height + 1;
            }
        }

        #endregion

        #region Helpers

        private bool ShouldShowStats(IItem item)
        {
            if (item == null || item.SnoItem == null) return false;
            if (LegendaryOnly && !item.IsLegendary) return false;
            return true;
        }

        private string GetStatName(IItemPerfection perf)
        {
            if (perf.Attribute == null) return "?";
            string code = perf.Attribute.Code ?? "";
            
            switch (code)
            {
                case "Strength_Item": return "STR";
                case "Dexterity_Item": return "DEX";
                case "Intelligence_Item": return "INT";
                case "Vitality_Item": return "VIT";
                case "Armor_Item": return "Armor";
                case "Hitpoints_Max_Percent_Bonus_Item": return "Life%";
                case "Hitpoints_On_Hit": return "LoH";
                case "Resource_Cost_Reduction_Percent_All": return "RCR";
                case "Cooldown_Reduction_Percent_All": return "CDR";
                case "Attacks_Per_Second_Percent": return "IAS%";
                case "Crit_Percent_Bonus_Capped": return "CHC";
                case "Crit_Damage_Percent": return "CHD";
                case "Damage_Percent_Bonus_Vs_Elites": return "Elite%";
                case "Resistance_All": return "AllRes";
                case "Sockets": return "Sockets";
                case "Area_Damage_Percent": return "AD";
                default:
                    string clean = code.Replace("_Item", "").Replace("_Percent", "%").Replace("_", " ");
                    return clean.Length > 12 ? clean.Substring(0, 10) + ".." : clean;
            }
        }

        private string FormatStatValue(IItemPerfection perf)
        {
            double value = perf.Cur;
            string code = perf.Attribute?.Code ?? "";

            if (code.Contains("Percent") || code.Contains("Crit") || code.Contains("Reduction"))
                return $"{value * 100:F1}%";
            if (value >= 1000) return $"{value:N0}";
            return $"{value:F0}";
        }

        private string FormatStatRange(IItemPerfection perf)
        {
            string code = perf.Attribute?.Code ?? "";
            bool isPercent = code.Contains("Percent") || code.Contains("Crit") || code.Contains("Reduction");
            
            if (isPercent)
            {
                double min = perf.Min * 100, max = perf.Max * 100;
                return Math.Abs(min - max) < 0.01 ? $"{min:F1}%" : $"{min:F1}-{max:F1}%";
            }
            return Math.Abs(perf.Min - perf.Max) < 0.01 ? $"{perf.Min:F0}" : $"{perf.Min:F0}-{perf.Max:F0}";
        }

        private class TooltipLine
        {
            public string Text;
            public IFont Font;
            public float Width;
            public float Height;
        }

        #endregion
    }
}
