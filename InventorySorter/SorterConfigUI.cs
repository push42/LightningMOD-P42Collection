namespace Turbo.Plugins.Custom.InventorySorter
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Windows.Forms;
    using Turbo.Plugins.Default;

    /// <summary>
    /// Advanced in-game configuration UI for Inventory Sorter
    /// God-tier design with proper click blocking and smooth interactions
    /// </summary>
    public class SorterConfigUI
    {
        #region UI State

        public bool IsVisible { get; set; }
        public bool IsMouseOverUI { get; private set; }

        private int _activeTab = 0;
        private int _scrollOffset = 0;
        private int _maxVisibleItems = 6;
        private string _hoveredElement;
        private float _animProgress = 0f;

        // Click handling
        private IWatch _clickTimer;
        private IWatch _hoverTimer;
        private bool _wasMouseDown;
        private IController _hud;

        #endregion

        #region UI Dimensions

        private RectangleF _panelRect;
        private RectangleF _headerRect;
        private RectangleF _contentRect;
        private Dictionary<string, RectangleF> _buttonRects;
        private Dictionary<string, RectangleF> _tabRects;
        private Dictionary<string, RectangleF> _toggleRects;
        private Dictionary<string, RectangleF> _sliderRects;
        private RectangleF _scrollUpRect;
        private RectangleF _scrollDownRect;

        // Tabs
        private readonly string[] _tabNames = { "Presets", "Sort Rules", "Zones", "Settings" };
        private readonly string[] _tabIcons = { "◈", "⚙", "▦", "☰" };

        #endregion

        #region Fonts and Brushes

        // Fonts
        private IFont _titleFont;
        private IFont _headerFont;
        private IFont _labelFont;
        private IFont _valueFont;
        private IFont _smallFont;
        private IFont _tinyFont;
        private IFont _tabFont;
        private IFont _tabActiveFont;
        private IFont _iconFont;
        private IFont _buttonFont;

        // Brushes - Modern dark theme
        private IBrush _panelBrush;
        private IBrush _panelBorderBrush;
        private IBrush _headerBrush;
        private IBrush _headerGradientBrush;
        private IBrush _contentBrush;
        private IBrush _itemBrush;
        private IBrush _itemHoverBrush;
        private IBrush _itemSelectedBrush;
        private IBrush _buttonBrush;
        private IBrush _buttonHoverBrush;
        private IBrush _buttonActiveBrush;
        private IBrush _toggleOnBrush;
        private IBrush _toggleOffBrush;
        private IBrush _toggleHandleBrush;
        private IBrush _sliderTrackBrush;
        private IBrush _sliderFillBrush;
        private IBrush _sliderHandleBrush;
        private IBrush _accentBrush;
        private IBrush _accentDimBrush;
        private IBrush _tabBrush;
        private IBrush _tabActiveBrush;
        private IBrush _tabHoverBrush;
        private IBrush _scrollbarBrush;
        private IBrush _closeBrush;
        private IBrush _closeHoverBrush;
        private IBrush _dividerBrush;
        private IBrush _shadowBrush;

        // Colors for zone preview
        private IBrush _zoneHighValueBrush;
        private IBrush _zoneGrabOftenBrush;
        private IBrush _zoneRunStartersBrush;
        private IBrush _zoneToDecideBrush;
        private IBrush _zoneMainBrush;

        #endregion

        #region References

        private InventorySorterPlugin _plugin;
        private PresetManager _presetManager;
        private SorterConfiguration _config;

        #endregion

        public SorterConfigUI(IController hud, InventorySorterPlugin plugin)
        {
            _hud = hud;
            _plugin = plugin;
            _buttonRects = new Dictionary<string, RectangleF>();
            _tabRects = new Dictionary<string, RectangleF>();
            _toggleRects = new Dictionary<string, RectangleF>();
            _sliderRects = new Dictionary<string, RectangleF>();

            _clickTimer = hud.Time.CreateAndStartWatch();
            _hoverTimer = hud.Time.CreateAndStartWatch();

            InitializeFonts(hud);
            InitializeBrushes(hud);
        }

        public void SetReferences(PresetManager presetManager, SorterConfiguration config)
        {
            _presetManager = presetManager;
            _config = config;
        }

        private void InitializeFonts(IController hud)
        {
            // Modern, clean fonts
            _titleFont = hud.Render.CreateFont("segoe ui", 11, 255, 255, 255, 255, true, false, 255, 0, 0, 0, true);
            _headerFont = hud.Render.CreateFont("segoe ui", 9, 255, 230, 230, 230, true, false, 200, 0, 0, 0, true);
            _labelFont = hud.Render.CreateFont("segoe ui", 8, 255, 210, 210, 210, false, false, 180, 0, 0, 0, true);
            _valueFont = hud.Render.CreateFont("segoe ui", 8, 255, 130, 190, 255, false, false, 180, 0, 0, 0, true);
            _smallFont = hud.Render.CreateFont("segoe ui", 7, 220, 170, 170, 170, false, false, 160, 0, 0, 0, true);
            _tinyFont = hud.Render.CreateFont("segoe ui", 6.5f, 200, 140, 140, 140, false, false, 140, 0, 0, 0, true);
            _tabFont = hud.Render.CreateFont("segoe ui", 7.5f, 220, 180, 180, 180, false, false, 170, 0, 0, 0, true);
            _tabActiveFont = hud.Render.CreateFont("segoe ui", 7.5f, 255, 255, 220, 130, true, false, 170, 0, 0, 0, true);
            _iconFont = hud.Render.CreateFont("segoe ui symbol", 10, 255, 255, 200, 100, false, false, 200, 0, 0, 0, true);
            _buttonFont = hud.Render.CreateFont("segoe ui", 7.5f, 255, 255, 255, 255, true, false, 170, 0, 0, 0, true);
        }

        private void InitializeBrushes(IController hud)
        {
            // Modern dark theme with subtle gradients
            _panelBrush = hud.Render.CreateBrush(250, 18, 18, 24, 0);
            _panelBorderBrush = hud.Render.CreateBrush(255, 50, 50, 60, 1.5f);
            _headerBrush = hud.Render.CreateBrush(255, 25, 25, 35, 0);
            _headerGradientBrush = hud.Render.CreateBrush(255, 35, 35, 50, 0);
            _contentBrush = hud.Render.CreateBrush(240, 14, 14, 20, 0);
            _itemBrush = hud.Render.CreateBrush(220, 28, 28, 38, 0);
            _itemHoverBrush = hud.Render.CreateBrush(240, 40, 40, 55, 0);
            _itemSelectedBrush = hud.Render.CreateBrush(250, 45, 60, 80, 0);
            _buttonBrush = hud.Render.CreateBrush(230, 45, 65, 95, 0);
            _buttonHoverBrush = hud.Render.CreateBrush(250, 60, 90, 130, 0);
            _buttonActiveBrush = hud.Render.CreateBrush(255, 70, 110, 160, 0);
            _toggleOnBrush = hud.Render.CreateBrush(255, 50, 180, 90, 0);
            _toggleOffBrush = hud.Render.CreateBrush(255, 70, 70, 80, 0);
            _toggleHandleBrush = hud.Render.CreateBrush(255, 240, 240, 245, 0);
            _sliderTrackBrush = hud.Render.CreateBrush(220, 35, 35, 45, 0);
            _sliderFillBrush = hud.Render.CreateBrush(255, 80, 160, 220, 0);
            _sliderHandleBrush = hud.Render.CreateBrush(255, 220, 220, 230, 0);
            _accentBrush = hud.Render.CreateBrush(255, 255, 200, 100, 0);
            _accentDimBrush = hud.Render.CreateBrush(150, 255, 200, 100, 0);
            _tabBrush = hud.Render.CreateBrush(200, 22, 22, 30, 0);
            _tabActiveBrush = hud.Render.CreateBrush(255, 35, 35, 50, 0);
            _tabHoverBrush = hud.Render.CreateBrush(230, 30, 30, 42, 0);
            _scrollbarBrush = hud.Render.CreateBrush(180, 60, 60, 75, 0);
            _closeBrush = hud.Render.CreateBrush(200, 180, 60, 60, 0);
            _closeHoverBrush = hud.Render.CreateBrush(255, 220, 70, 70, 0);
            _dividerBrush = hud.Render.CreateBrush(100, 60, 60, 70, 0);
            _shadowBrush = hud.Render.CreateBrush(80, 0, 0, 0, 0);

            // Zone colors (semi-transparent)
            _zoneHighValueBrush = hud.Render.CreateBrush(120, 255, 200, 50, 0);
            _zoneGrabOftenBrush = hud.Render.CreateBrush(120, 80, 180, 255, 0);
            _zoneRunStartersBrush = hud.Render.CreateBrush(120, 255, 100, 200, 0);
            _zoneToDecideBrush = hud.Render.CreateBrush(120, 255, 160, 80, 0);
            _zoneMainBrush = hud.Render.CreateBrush(80, 120, 120, 140, 0);
        }

        #region Main Render

        public void Render(RectangleF inventoryRect)
        {
            if (!IsVisible) return;

            // Position panel between character and inventory
            float panelW = 320;
            float panelH = 440;
            
            // Calculate position - to the left of inventory, but not too far
            float panelX = inventoryRect.X - panelW - 12;
            float panelY = inventoryRect.Y - 20;

            // Ensure panel stays on screen
            if (panelX < 10) panelX = 10;
            if (panelY < 10) panelY = 10;
            if (panelY + panelH > _hud.Window.Size.Height - 10)
                panelH = _hud.Window.Size.Height - panelY - 10;

            _panelRect = new RectangleF(panelX, panelY, panelW, panelH);

            // Check if mouse is over our UI (to block game input)
            IsMouseOverUI = IsMouseOver(_panelRect);

            // Draw shadow
            _shadowBrush.DrawRectangle(panelX + 4, panelY + 4, panelW, panelH);

            // Draw panel
            DrawPanel(panelX, panelY, panelW, panelH);
        }

        private void DrawPanel(float x, float y, float w, float h)
        {
            _buttonRects.Clear();
            _tabRects.Clear();
            _toggleRects.Clear();
            _sliderRects.Clear();
            _hoveredElement = null;

            // Main panel background
            _panelBrush.DrawRectangle(x, y, w, h);
            _panelBorderBrush.DrawRectangle(x, y, w, h);

            float pad = 12;
            float contentX = x + pad;
            float contentY = y;
            float contentW = w - pad * 2;

            // === HEADER ===
            float headerH = 40;
            _headerRect = new RectangleF(x, y, w, headerH);
            _headerBrush.DrawRectangle(x, y, w, headerH);

            // Accent line under header
            _accentBrush.DrawRectangle(x, y + headerH - 2, w, 2);

            // Icon + Title
            var iconLayout = _iconFont.GetTextLayout("⚙");
            _iconFont.DrawText(iconLayout, x + 12, y + (headerH - iconLayout.Metrics.Height) / 2);

            var titleLayout = _titleFont.GetTextLayout("Inventory Sorter");
            _titleFont.DrawText(titleLayout, x + 12 + iconLayout.Metrics.Width + 8, y + (headerH - titleLayout.Metrics.Height) / 2);

            // Close button (X)
            float closeSize = 24;
            float closePad = 8;
            var closeRect = new RectangleF(x + w - closeSize - closePad, y + (headerH - closeSize) / 2, closeSize, closeSize);
            _buttonRects["close"] = closeRect;

            bool closeHovered = IsMouseOver(closeRect);
            var closeBrush = closeHovered ? _closeHoverBrush : _closeBrush;
            closeBrush.DrawRectangle(closeRect.X + 2, closeRect.Y + 2, closeSize - 4, closeSize - 4);

            var closeLayout = _labelFont.GetTextLayout("✕");
            _labelFont.DrawText(closeLayout, closeRect.X + (closeSize - closeLayout.Metrics.Width) / 2, closeRect.Y + (closeSize - closeLayout.Metrics.Height) / 2);

            contentY = y + headerH + 8;

            // === TAB BAR ===
            float tabH = 32;
            float tabW = (contentW + 4) / _tabNames.Length;
            float tabY = contentY;

            for (int i = 0; i < _tabNames.Length; i++)
            {
                float tabX = contentX + i * tabW;
                var tabRect = new RectangleF(tabX, tabY, tabW - 4, tabH);
                _tabRects[_tabNames[i]] = tabRect;

                bool isActive = (i == _activeTab);
                bool isHovered = IsMouseOver(tabRect);

                // Tab background
                var tabBrush = isActive ? _tabActiveBrush : (isHovered ? _tabHoverBrush : _tabBrush);
                tabBrush.DrawRectangle(tabRect);

                // Active indicator
                if (isActive)
                    _accentBrush.DrawRectangle(tabX, tabY + tabH - 3, tabW - 4, 3);

                // Tab content (icon + text)
                var tabFont = isActive ? _tabActiveFont : _tabFont;
                string tabText = _tabIcons[i] + " " + _tabNames[i];
                var tabLayout = tabFont.GetTextLayout(tabText);
                tabFont.DrawText(tabLayout, tabX + (tabW - 4 - tabLayout.Metrics.Width) / 2, tabY + (tabH - tabLayout.Metrics.Height) / 2);
            }

            contentY = tabY + tabH + 10;

            // === CONTENT AREA ===
            float contentH = h - (contentY - y) - pad;
            _contentRect = new RectangleF(contentX, contentY, contentW, contentH);
            _contentBrush.DrawRectangle(contentX - 2, contentY, contentW + 4, contentH);

            // Draw content based on active tab
            float innerPad = 8;
            float innerX = contentX + innerPad;
            float innerY = contentY + innerPad;
            float innerW = contentW - innerPad * 2;
            float innerH = contentH - innerPad * 2;

            switch (_activeTab)
            {
                case 0: DrawPresetsTab(innerX, innerY, innerW, innerH); break;
                case 1: DrawSortRulesTab(innerX, innerY, innerW, innerH); break;
                case 2: DrawZonesTab(innerX, innerY, innerW, innerH); break;
                case 3: DrawSettingsTab(innerX, innerY, innerW, innerH); break;
            }
        }

        #endregion

        #region Tab Content

        private void DrawPresetsTab(float x, float y, float w, float h)
        {
            if (_presetManager == null) return;

            // Section header
            DrawSectionHeader(x, ref y, w, "Stash Organization Presets");
            y += 4;

            // Description
            var descLayout = _smallFont.GetTextLayout("Choose a layout or create your own");
            _smallFont.DrawText(descLayout, x, y);
            y += descLayout.Metrics.Height + 12;

            // Preset list
            var presets = _presetManager.GetAllPresets();
            float rowH = 56;
            float maxY = _contentRect.Bottom - 50;

            foreach (var preset in presets)
            {
                if (y + rowH > maxY) break;

                var rowRect = new RectangleF(x, y, w, rowH);
                bool isActive = preset.Id == _presetManager.ActivePresetId;
                bool isHovered = IsMouseOver(rowRect);

                _toggleRects["preset_" + preset.Id] = rowRect;
                if (isHovered) _hoveredElement = "preset_" + preset.Id;

                // Background with selection state
                var rowBrush = isActive ? _itemSelectedBrush : (isHovered ? _itemHoverBrush : _itemBrush);
                rowBrush.DrawRectangle(rowRect);

                // Left accent bar for active
                if (isActive)
                    _accentBrush.DrawRectangle(x, y, 3, rowH);

                // Radio button
                float radioSize = 16;
                float radioX = x + 12;
                float radioY = y + (rowH - radioSize) / 2;

                // Radio outer circle
                _panelBorderBrush.DrawEllipse(radioX + radioSize / 2, radioY + radioSize / 2, radioSize / 2, radioSize / 2);
                
                // Radio inner fill if active
                if (isActive)
                    _accentBrush.DrawEllipse(radioX + radioSize / 2, radioY + radioSize / 2, radioSize / 2 - 4, radioSize / 2 - 4);

                // Preset name
                float textX = radioX + radioSize + 12;
                var nameFont = isActive ? _headerFont : _labelFont;
                var nameLayout = nameFont.GetTextLayout(preset.Name);
                nameFont.DrawText(nameLayout, textX, y + 10);

                // Description
                string desc = preset.Description ?? "";
                if (desc.Length > 45) desc = desc.Substring(0, 42) + "...";
                var presetDescLayout = _smallFont.GetTextLayout(desc);
                _smallFont.DrawText(presetDescLayout, textX, y + 10 + nameLayout.Metrics.Height + 3);

                // Built-in badge
                if (preset.IsBuiltIn)
                {
                    var badge = _tinyFont.GetTextLayout("BUILT-IN");
                    _tinyFont.DrawText(badge, x + w - badge.Metrics.Width - 8, y + 8);
                }

                y += rowH + 4;
            }

            // Bottom buttons
            y = _contentRect.Bottom - 40;
            float btnW = (w - 8) / 2;
            float btnH = 30;

            // Apply button
            var applyRect = new RectangleF(x, y, btnW, btnH);
            _buttonRects["apply_preset"] = applyRect;
            DrawButton(applyRect, "✓ Apply", IsMouseOver(applyRect));

            // New preset button
            var newRect = new RectangleF(x + btnW + 8, y, btnW, btnH);
            _buttonRects["new_preset"] = newRect;
            DrawButton(newRect, "+ New Preset", IsMouseOver(newRect), true);
        }

        private void DrawSortRulesTab(float x, float y, float w, float h)
        {
            // Section: Sort Mode
            DrawSectionHeader(x, ref y, w, "Primary Sort Mode");
            y += 8;

            // Mode buttons (2 rows of 3)
            string[] modes = { "Category", "Quality", "Type", "Size", "A-Z", "Row-Lock" };
            string[] modeDescs = { "By item tier", "Best first", "By slot", "Big → small", "Alphabetical", "Fixed rows" };
            float btnW = (w - 8) / 3;
            float btnH = 36;

            for (int i = 0; i < modes.Length; i++)
            {
                int row = i / 3;
                int col = i % 3;
                float bx = x + col * (btnW + 4);
                float by = y + row * (btnH + 4);

                var modeRect = new RectangleF(bx, by, btnW, btnH);
                _buttonRects["mode_" + i] = modeRect;

                bool isActive = (int)_plugin.CurrentMode == i;
                bool isHovered = IsMouseOver(modeRect);

                // Button background
                var modeBrush = isActive ? _buttonActiveBrush : (isHovered ? _buttonHoverBrush : _itemBrush);
                modeBrush.DrawRectangle(modeRect);

                if (isActive)
                    _accentBrush.DrawRectangle(bx, by + btnH - 2, btnW, 2);

                // Mode name
                var modeLayout = _labelFont.GetTextLayout(modes[i]);
                _labelFont.DrawText(modeLayout, bx + (btnW - modeLayout.Metrics.Width) / 2, by + 6);

                // Mode description
                var descLayout = _tinyFont.GetTextLayout(modeDescs[i]);
                _tinyFont.DrawText(descLayout, bx + (btnW - descLayout.Metrics.Width) / 2, by + 6 + modeLayout.Metrics.Height + 2);
            }

            y += (btnH + 4) * 2 + 16;

            // Section: Sorting Options
            DrawSectionHeader(x, ref y, w, "Sorting Options");
            y += 8;

            DrawToggleRow(x, ref y, w, "sort_quality_first", "Sort by quality first", _config?.SortByQualityFirst ?? true, "Higher quality items appear earlier");
            DrawToggleRow(x, ref y, w, "group_sets", "Group set items together", _config?.GroupSets ?? true, "Keep set pieces next to each other");
            DrawToggleRow(x, ref y, w, "gems_by_color", "Group gems by color", _config?.GroupGemsByColor ?? true, "Rubies, Emeralds, etc. together");
            DrawToggleRow(x, ref y, w, "primals_first", "Primals always first", _config?.PrimalsFirst ?? true, "Primal items at the top");

            y += 12;

            // Section: Protection
            DrawSectionHeader(x, ref y, w, "Item Protection");
            y += 8;

            DrawToggleRow(x, ref y, w, "protect_locked", "Respect inventory lock", _config?.RespectInventoryLock ?? true, "Don't move locked items");
            DrawToggleRow(x, ref y, w, "protect_armory", "Protect armory items", _config?.ProtectArmoryItems ?? true, "Items in armory sets stay");
            DrawToggleRow(x, ref y, w, "protect_enchanted", "Protect enchanted", _config?.ProtectEnchantedItems ?? false, "Enchanted items don't move");
            DrawToggleRow(x, ref y, w, "protect_socketed", "Protect socketed", _config?.ProtectSocketedItems ?? false, "Socketed items don't move");
        }

        private void DrawZonesTab(float x, float y, float w, float h)
        {
            DrawSectionHeader(x, ref y, w, "Stash Zone Preview");
            y += 4;

            var descLayout = _smallFont.GetTextLayout("Visual layout of the active preset's zones");
            _smallFont.DrawText(descLayout, x, y);
            y += descLayout.Metrics.Height + 12;

            // Zone visualization (7x10 grid preview)
            float cellSize = Math.Min(24, (w - 20) / 7);
            float gridW = 7 * cellSize;
            float gridH = 10 * cellSize;
            float gridX = x + (w - gridW) / 2;
            float gridY = y;

            // Grid background
            _itemBrush.DrawRectangle(gridX - 2, gridY - 2, gridW + 4, gridH + 4);

            // Draw cells
            for (int gx = 0; gx < 7; gx++)
            {
                for (int gy = 0; gy < 10; gy++)
                {
                    var cellRect = new RectangleF(gridX + gx * cellSize, gridY + gy * cellSize, cellSize - 1, cellSize - 1);
                    _contentBrush.DrawRectangle(cellRect);
                }
            }

            // Draw zones from active preset
            var activePreset = _presetManager?.GetActivePreset();
            if (activePreset?.TabConfigs?.Count > 0)
            {
                var tabConfig = activePreset.TabConfigs[0];
                foreach (var zone in tabConfig.Zones)
                {
                    var zoneRect = new RectangleF(
                        gridX + zone.StartX * cellSize,
                        gridY + zone.StartY * cellSize,
                        zone.Width * cellSize - 1,
                        zone.Height * cellSize - 1
                    );

                    IBrush zoneBrush = _zoneMainBrush;
                    switch (zone.ZoneType)
                    {
                        case StashZoneType.HighValue: zoneBrush = _zoneHighValueBrush; break;
                        case StashZoneType.GrabOften: zoneBrush = _zoneGrabOftenBrush; break;
                        case StashZoneType.RunStarters: zoneBrush = _zoneRunStartersBrush; break;
                        case StashZoneType.ToDecide: zoneBrush = _zoneToDecideBrush; break;
                    }

                    zoneBrush.DrawRectangle(zoneRect);

                    // Zone name (if fits)
                    if (zone.Width >= 2 && zone.Height >= 2)
                    {
                        var zoneLayout = _tinyFont.GetTextLayout(zone.Name);
                        if (zoneLayout.Metrics.Width < zoneRect.Width - 4)
                            _tinyFont.DrawText(zoneLayout, zoneRect.X + 3, zoneRect.Y + 2);
                    }
                }
            }

            y += gridH + 16;

            // Legend
            DrawSectionHeader(x, ref y, w, "Zone Types");
            y += 8;

            float legendX = x;
            float legendW = w / 2 - 4;

            DrawLegendItem(legendX, y, legendW, "High Value", _zoneHighValueBrush, "Primals, jewelry");
            DrawLegendItem(legendX + legendW + 8, y, legendW, "Grab Often", _zoneGrabOftenBrush, "Keys, swaps");
            y += 24;
            DrawLegendItem(legendX, y, legendW, "Run Starters", _zoneRunStartersBrush, "Puzzle Rings");
            DrawLegendItem(legendX + legendW + 8, y, legendW, "To Decide", _zoneToDecideBrush, "Evaluate later");
        }

        private void DrawSettingsTab(float x, float y, float w, float h)
        {
            // Section: Speed
            DrawSectionHeader(x, ref y, w, "Sorting Speed");
            y += 8;

            DrawSlider(x, ref y, w, "move_delay", "Move Delay", 20, 150, _config?.MoveDelayMs ?? 50, "ms");
            y += 8;
            DrawSlider(x, ref y, w, "click_delay", "Click Delay", 10, 100, _config?.ClickDelayMs ?? 30, "ms");
            y += 16;

            // Section: UI
            DrawSectionHeader(x, ref y, w, "User Interface");
            y += 8;

            DrawToggleRow(x, ref y, w, "show_highlights", "Show item highlights", _config?.ShowHighlights ?? true, "Color-coded item borders");
            DrawToggleRow(x, ref y, w, "show_progress", "Show progress bar", _config?.ShowProgress ?? true, "Progress during sorting");
            DrawToggleRow(x, ref y, w, "confirm_sort", "Confirm before sort", _config?.ConfirmBeforeSort ?? false, "Ask before starting");

            y += 16;

            // Section: Hotkeys
            DrawSectionHeader(x, ref y, w, "Hotkeys Reference");
            y += 8;

            var hotkeyBg = new RectangleF(x, y, w, 80);
            _itemBrush.DrawRectangle(hotkeyBg);

            float hkX = x + 12;
            float hkY = y + 8;

            DrawHotkeyRow(hkX, ref hkY, w - 24, "K", "Start/Cancel sorting");
            DrawHotkeyRow(hkX, ref hkY, w - 24, "Shift+K", "Cycle sort mode");
            DrawHotkeyRow(hkX, ref hkY, w - 24, "Ctrl+K", "Open/Close this panel");
            DrawHotkeyRow(hkX, ref hkY, w - 24, "ESC", "Cancel / Close");
        }

        #endregion

        #region UI Components

        private void DrawSectionHeader(float x, ref float y, float w, string text)
        {
            var layout = _headerFont.GetTextLayout(text);
            _headerFont.DrawText(layout, x, y);
            y += layout.Metrics.Height + 2;

            // Underline
            _dividerBrush.DrawLine(x, y, x + w, y);
            y += 6;
        }

        private void DrawButton(RectangleF rect, string text, bool hovered, bool secondary = false)
        {
            var brush = hovered ? _buttonHoverBrush : (secondary ? _itemBrush : _buttonBrush);
            brush.DrawRectangle(rect);

            if (hovered)
                _accentDimBrush.DrawRectangle(rect.X, rect.Y, rect.Width, 2);

            var layout = _buttonFont.GetTextLayout(text);
            _buttonFont.DrawText(layout, rect.X + (rect.Width - layout.Metrics.Width) / 2, rect.Y + (rect.Height - layout.Metrics.Height) / 2);
        }

        private void DrawToggleRow(float x, ref float y, float w, string id, string label, bool value, string tooltip = null)
        {
            float rowH = 32;
            float toggleW = 44;
            float toggleH = 22;
            float toggleX = x + w - toggleW;
            float toggleY = y + (rowH - toggleH) / 2;

            var rowRect = new RectangleF(x, y, w, rowH);
            var toggleRect = new RectangleF(toggleX, toggleY, toggleW, toggleH);
            _toggleRects[id] = toggleRect;

            bool isHovered = IsMouseOver(rowRect);
            if (isHovered) _hoveredElement = id;

            // Row hover highlight
            if (isHovered)
                _itemHoverBrush.DrawRectangle(x - 4, y, w + 8, rowH);

            // Label
            var labelLayout = _labelFont.GetTextLayout(label);
            _labelFont.DrawText(labelLayout, x, y + (rowH - labelLayout.Metrics.Height) / 2 - (tooltip != null ? 4 : 0));

            // Tooltip/description
            if (tooltip != null)
            {
                var tipLayout = _tinyFont.GetTextLayout(tooltip);
                _tinyFont.DrawText(tipLayout, x, y + (rowH - labelLayout.Metrics.Height) / 2 + labelLayout.Metrics.Height - 2);
            }

            // Toggle background (pill shape approximation)
            var bgBrush = value ? _toggleOnBrush : _toggleOffBrush;
            bgBrush.DrawRectangle(toggleX, toggleY, toggleW, toggleH);

            // Toggle handle
            float handleSize = toggleH - 4;
            float handleX = value ? toggleX + toggleW - handleSize - 2 : toggleX + 2;
            _toggleHandleBrush.DrawRectangle(handleX, toggleY + 2, handleSize, handleSize);

            y += rowH + 2;
        }

        private void DrawSlider(float x, ref float y, float w, string id, string label, int min, int max, int value, string unit)
        {
            float rowH = 44;

            // Label and value
            var labelLayout = _labelFont.GetTextLayout(label);
            _labelFont.DrawText(labelLayout, x, y);

            var valueLayout = _valueFont.GetTextLayout(value + unit);
            _valueFont.DrawText(valueLayout, x + w - valueLayout.Metrics.Width, y);

            y += labelLayout.Metrics.Height + 6;

            // Slider track
            float trackH = 8;
            float trackW = w;
            var trackRect = new RectangleF(x, y, trackW, trackH);
            _sliderTrackBrush.DrawRectangle(trackRect);
            _sliderRects[id] = trackRect;

            // Slider fill
            float pct = (float)(value - min) / (max - min);
            _sliderFillBrush.DrawRectangle(x, y, trackW * pct, trackH);

            // Slider handle
            float handleSize = 16;
            float handleX = x + trackW * pct - handleSize / 2;
            _sliderHandleBrush.DrawRectangle(handleX, y - 4, handleSize, trackH + 8);

            y += trackH + 12;
        }

        private void DrawLegendItem(float x, float y, float w, string label, IBrush colorBrush, string desc)
        {
            float colorSize = 14;
            colorBrush.DrawRectangle(x, y + 2, colorSize, colorSize);

            var labelLayout = _smallFont.GetTextLayout(label);
            _smallFont.DrawText(labelLayout, x + colorSize + 6, y);

            var descLayout = _tinyFont.GetTextLayout(desc);
            _tinyFont.DrawText(descLayout, x + colorSize + 6, y + labelLayout.Metrics.Height);
        }

        private void DrawHotkeyRow(float x, ref float y, float w, string key, string action)
        {
            float keyW = 70;

            // Key badge
            var keyBg = new RectangleF(x, y, keyW, 16);
            _buttonBrush.DrawRectangle(keyBg);

            var keyLayout = _smallFont.GetTextLayout(key);
            _smallFont.DrawText(keyLayout, x + (keyW - keyLayout.Metrics.Width) / 2, y + 1);

            // Action text
            var actionLayout = _smallFont.GetTextLayout(action);
            _smallFont.DrawText(actionLayout, x + keyW + 12, y + 1);

            y += 18;
        }

        #endregion

        #region Input Handling

        public bool HandleMouseInput()
        {
            if (!IsVisible) return false;

            bool isMouseDown = _hud.Input.IsKeyDown(Keys.LButton);
            bool clicked = !isMouseDown && _wasMouseDown && _clickTimer.ElapsedMilliseconds > 100;
            _wasMouseDown = isMouseDown;

            if (!clicked) return IsMouseOverUI;

            _clickTimer.Restart();

            // Close button
            if (_buttonRects.TryGetValue("close", out var closeRect) && IsMouseOver(closeRect))
            {
                IsVisible = false;
                return true;
            }

            // Tab clicks
            for (int i = 0; i < _tabNames.Length; i++)
            {
                if (_tabRects.TryGetValue(_tabNames[i], out var tabRect) && IsMouseOver(tabRect))
                {
                    _activeTab = i;
                    _scrollOffset = 0;
                    return true;
                }
            }

            // Preset selection
            foreach (var kvp in _toggleRects)
            {
                if (kvp.Key.StartsWith("preset_") && IsMouseOver(kvp.Value))
                {
                    string presetId = kvp.Key.Substring("preset_".Length);
                    _presetManager?.SetActivePreset(presetId);
                    return true;
                }
            }

            // Mode buttons
            for (int i = 0; i < 6; i++)
            {
                if (_buttonRects.TryGetValue("mode_" + i, out var modeRect) && IsMouseOver(modeRect))
                {
                    _plugin.SetSortMode((SortMode)i);
                    return true;
                }
            }

            // Toggle clicks
            foreach (var kvp in _toggleRects)
            {
                if (IsMouseOver(kvp.Value) && !kvp.Key.StartsWith("preset_"))
                {
                    HandleToggleClick(kvp.Key);
                    return true;
                }
            }

            return IsMouseOverUI;
        }

        private void HandleToggleClick(string id)
        {
            if (_config == null) return;

            switch (id)
            {
                case "sort_quality_first": _config.SortByQualityFirst = !_config.SortByQualityFirst; break;
                case "group_sets": _config.GroupSets = !_config.GroupSets; break;
                case "gems_by_color": _config.GroupGemsByColor = !_config.GroupGemsByColor; break;
                case "primals_first": _config.PrimalsFirst = !_config.PrimalsFirst; break;
                case "protect_locked": _config.RespectInventoryLock = !_config.RespectInventoryLock; break;
                case "protect_armory": _config.ProtectArmoryItems = !_config.ProtectArmoryItems; break;
                case "protect_enchanted": _config.ProtectEnchantedItems = !_config.ProtectEnchantedItems; break;
                case "protect_socketed": _config.ProtectSocketedItems = !_config.ProtectSocketedItems; break;
                case "show_highlights": _config.ShowHighlights = !_config.ShowHighlights; break;
                case "show_progress": _config.ShowProgress = !_config.ShowProgress; break;
                case "confirm_sort": _config.ConfirmBeforeSort = !_config.ConfirmBeforeSort; break;
            }
        }

        private bool IsMouseOver(RectangleF rect)
        {
            int mx = _hud.Window.CursorX;
            int my = _hud.Window.CursorY;
            return mx >= rect.X && mx <= rect.X + rect.Width &&
                   my >= rect.Y && my <= rect.Y + rect.Height;
        }

        #endregion
    }
}
