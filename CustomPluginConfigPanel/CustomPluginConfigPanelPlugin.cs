namespace Turbo.Plugins.Custom.CustomPluginConfigPanel
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using SharpDX.DirectInput;
    using Turbo.Plugins.Default;

    /// <summary>
    /// In-Game Configuration Panel for Custom Plugins
    /// 
    /// Since TurboHUD's config UI is protected and cannot be extended,
    /// this plugin provides an in-game overlay for configuring custom plugins.
    /// 
    /// Press Ctrl+Shift+C to toggle the config panel
    /// </summary>
    public class CustomPluginConfigPanelPlugin : BasePlugin, IInGameTopPainter, IKeyEventHandler
    {
        #region Settings

        public IKeyEvent TogglePanelKey { get; set; }
        public bool ShowPanel { get; private set; } = false;

        // Panel positioning
        public float PanelX { get; set; } = 0.25f;  // 25% from left
        public float PanelY { get; set; } = 0.15f;  // 15% from top
        public float PanelWidth { get; set; } = 400f;
        public float PanelHeight { get; set; } = 500f;

        #endregion

        #region Private Fields

        // Categories and plugins
        private List<PluginCategory> _categories = new List<PluginCategory>();
        private int _selectedCategoryIndex = 0;
        private int _selectedPluginIndex = 0;
        private int _scrollOffset = 0;
        private const int MAX_VISIBLE_ITEMS = 12;

        // UI elements
        private IFont _titleFont;
        private IFont _categoryFont;
        private IFont _categorySelectedFont;
        private IFont _pluginFont;
        private IFont _pluginEnabledFont;
        private IFont _pluginDisabledFont;
        private IFont _settingFont;
        private IFont _valueFont;
        private IFont _hintFont;
        private IBrush _panelBrush;
        private IBrush _headerBrush;
        private IBrush _categoryBrush;
        private IBrush _selectedBrush;
        private IBrush _enabledBrush;
        private IBrush _disabledBrush;
        private IBrush _borderBrush;

        // Navigation keys
        private IKeyEvent _keyUp;
        private IKeyEvent _keyDown;
        private IKeyEvent _keyLeft;
        private IKeyEvent _keyRight;
        private IKeyEvent _keyToggle;
        private IKeyEvent _keyEscape;

        private bool _initialized = false;

        #endregion

        public CustomPluginConfigPanelPlugin()
        {
            Enabled = true;
            Order = 99999; // Draw on top
        }

        public override void Load(IController hud)
        {
            base.Load(hud);

            // Ctrl+Shift+C to toggle panel
            TogglePanelKey = Hud.Input.CreateKeyEvent(true, Key.C, true, false, true);

            // Navigation keys (only active when panel is open)
            _keyUp = Hud.Input.CreateKeyEvent(true, Key.Up, false, false, false);
            _keyDown = Hud.Input.CreateKeyEvent(true, Key.Down, false, false, false);
            _keyLeft = Hud.Input.CreateKeyEvent(true, Key.Left, false, false, false);
            _keyRight = Hud.Input.CreateKeyEvent(true, Key.Right, false, false, false);
            _keyToggle = Hud.Input.CreateKeyEvent(true, Key.Return, false, false, false);
            _keyEscape = Hud.Input.CreateKeyEvent(true, Key.Escape, false, false, false);

            // UI Styling
            _titleFont = Hud.Render.CreateFont("tahoma", 11, 255, 255, 200, 100, true, false, 200, 0, 0, 0, true);
            _categoryFont = Hud.Render.CreateFont("tahoma", 9, 255, 180, 180, 180, false, false, 160, 0, 0, 0, true);
            _categorySelectedFont = Hud.Render.CreateFont("tahoma", 9, 255, 255, 220, 100, true, false, 160, 0, 0, 0, true);
            _pluginFont = Hud.Render.CreateFont("tahoma", 8.5f, 255, 200, 200, 200, false, false, 140, 0, 0, 0, true);
            _pluginEnabledFont = Hud.Render.CreateFont("tahoma", 8.5f, 255, 100, 255, 100, false, false, 140, 0, 0, 0, true);
            _pluginDisabledFont = Hud.Render.CreateFont("tahoma", 8.5f, 255, 255, 100, 100, false, false, 140, 0, 0, 0, true);
            _settingFont = Hud.Render.CreateFont("tahoma", 8, 255, 150, 150, 150, false, false, 120, 0, 0, 0, true);
            _valueFont = Hud.Render.CreateFont("tahoma", 8, 255, 200, 200, 255, false, false, 120, 0, 0, 0, true);
            _hintFont = Hud.Render.CreateFont("tahoma", 7, 200, 120, 120, 120, false, false, 100, 0, 0, 0, true);

            _panelBrush = Hud.Render.CreateBrush(240, 20, 20, 30, 0);
            _headerBrush = Hud.Render.CreateBrush(255, 40, 35, 50, 0);
            _categoryBrush = Hud.Render.CreateBrush(200, 30, 30, 45, 0);
            _selectedBrush = Hud.Render.CreateBrush(180, 60, 60, 100, 0);
            _enabledBrush = Hud.Render.CreateBrush(255, 50, 120, 50, 0);
            _disabledBrush = Hud.Render.CreateBrush(255, 120, 50, 50, 0);
            _borderBrush = Hud.Render.CreateBrush(200, 80, 70, 100, 1.5f);
        }

        private void InitializePluginList()
        {
            if (_initialized) return;
            _initialized = true;

            _categories.Clear();

            // === MACROS CATEGORY ===
            var macrosCategory = new PluginCategory("Macros");

            // LoD Death Nova
            var lodPlugin = Hud.AllPlugins.FirstOrDefault(p => p.GetType().Name == "LoDDeathNovaMacroPlugin");
            if (lodPlugin != null)
            {
                macrosCategory.Plugins.Add(new ConfigurablePlugin
                {
                    Name = "LoD Death Nova",
                    Description = "Necromancer Blood Nova macro with CoE sync",
                    Plugin = lodPlugin,
                    Settings = new List<PluginSetting>
                    {
                        new PluginSetting { Name = "Running", PropertyName = "Running", Type = SettingType.ReadOnly },
                        new PluginSetting { Name = "Push Mode", PropertyName = "IsPushMode", Type = SettingType.Boolean },
                        new PluginSetting { Name = "Hide Tip", PropertyName = "IsHideTip", Type = SettingType.Boolean }
                    }
                });
            }

            // Natalya Spike Trap
            var natPlugin = Hud.AllPlugins.FirstOrDefault(p => p.GetType().Name == "NatalyaSpikeTrapMacroPlugin");
            if (natPlugin != null)
            {
                macrosCategory.Plugins.Add(new ConfigurablePlugin
                {
                    Name = "N6 Spike Trap",
                    Description = "Demon Hunter Natalya Spike Trap macro",
                    Plugin = natPlugin,
                    Settings = new List<PluginSetting>
                    {
                        new PluginSetting { Name = "Running", PropertyName = "Running", Type = SettingType.ReadOnly },
                        new PluginSetting { Name = "Damage Mode", PropertyName = "IsDamageMode", Type = SettingType.Boolean },
                        new PluginSetting { Name = "Hide Tip", PropertyName = "IsHideTip", Type = SettingType.Boolean }
                    }
                });
            }

            // Wizard Star Pact
            var wizPlugin = Hud.AllPlugins.FirstOrDefault(p => p.GetType().Name == "WizardStarPactMacroPlugin");
            if (wizPlugin != null)
            {
                macrosCategory.Plugins.Add(new ConfigurablePlugin
                {
                    Name = "Star Pact",
                    Description = "Wizard Star Pact Meteor macro",
                    Plugin = wizPlugin,
                    Settings = new List<PluginSetting>
                    {
                        new PluginSetting { Name = "Running", PropertyName = "Running", Type = SettingType.ReadOnly },
                        new PluginSetting { Name = "Hide Tip", PropertyName = "IsHideTip", Type = SettingType.Boolean }
                    }
                });
            }

            if (macrosCategory.Plugins.Count > 0)
                _categories.Add(macrosCategory);

            // === EVADE CATEGORY ===
            var evadeCategory = new PluginCategory("Evade");

            // Smart Evade v2
            var evadePlugin = Hud.AllPlugins.FirstOrDefault(p => p.GetType().Name == "SmartEvadePlugin");
            if (evadePlugin != null)
            {
                evadeCategory.Plugins.Add(new ConfigurablePlugin
                {
                    Name = "Smart Evade v2",
                    Description = "Auto-evade with wall awareness",
                    Plugin = evadePlugin,
                    Settings = new List<PluginSetting>
                    {
                        new PluginSetting { Name = "Active", PropertyName = "IsActive", Type = SettingType.Boolean },
                        new PluginSetting { Name = "Safe Distance", PropertyName = "SafeDistance", Type = SettingType.Float },
                        new PluginSetting { Name = "Wall Detection", PropertyName = "WallDetectionRange", Type = SettingType.Float },
                        new PluginSetting { Name = "Show Debug", PropertyName = "ShowDebugCircles", Type = SettingType.Boolean },
                        new PluginSetting { Name = "Show Walls", PropertyName = "ShowWallMarkers", Type = SettingType.Boolean },
                        new PluginSetting { Name = "Show Routes", PropertyName = "ShowEscapeRoutes", Type = SettingType.Boolean }
                    }
                });
            }

            // Smart Evade Lite
            var evadeLitePlugin = Hud.AllPlugins.FirstOrDefault(p => p.GetType().Name == "SmartEvadeLitePlugin");
            if (evadeLitePlugin != null)
            {
                evadeCategory.Plugins.Add(new ConfigurablePlugin
                {
                    Name = "Evade Lite",
                    Description = "Human-like delayed evade",
                    Plugin = evadeLitePlugin,
                    Settings = new List<PluginSetting>
                    {
                        new PluginSetting { Name = "Active", PropertyName = "IsActive", Type = SettingType.Boolean },
                        new PluginSetting { Name = "Show Debug", PropertyName = "ShowDebugCircles", Type = SettingType.Boolean }
                    }
                });
            }

            if (evadeCategory.Plugins.Count > 0)
                _categories.Add(evadeCategory);

            // === UTILITIES CATEGORY ===
            var utilCategory = new PluginCategory("Utilities");

            // Auto Pickup
            var pickupPlugin = Hud.AllPlugins.FirstOrDefault(p => p.GetType().Name == "AutoMasterPlugin");
            if (pickupPlugin != null)
            {
                utilCategory.Plugins.Add(new ConfigurablePlugin
                {
                    Name = "Auto Pickup",
                    Description = "Auto-pickup items and globes",
                    Plugin = pickupPlugin,
                    Settings = new List<PluginSetting>
                    {
                        new PluginSetting { Name = "Active", PropertyName = "IsActive", Type = SettingType.Boolean }
                    }
                });
            }

            // Smart Salvage
            var salvagePlugin = Hud.AllPlugins.FirstOrDefault(p => p.GetType().Name == "SmartSalvagePlugin");
            if (salvagePlugin != null)
            {
                utilCategory.Plugins.Add(new ConfigurablePlugin
                {
                    Name = "Smart Salvage",
                    Description = "Auto-salvage with blacklists",
                    Plugin = salvagePlugin,
                    Settings = new List<PluginSetting>
                    {
                        new PluginSetting { Name = "Running", PropertyName = "IsSalvaging", Type = SettingType.ReadOnly }
                    }
                });
            }

            // Inventory Sorter
            var sorterPlugin = Hud.AllPlugins.FirstOrDefault(p => p.GetType().Name == "InventorySorterPlugin");
            if (sorterPlugin != null)
            {
                utilCategory.Plugins.Add(new ConfigurablePlugin
                {
                    Name = "Inventory Sorter",
                    Description = "Sort inventory and stash",
                    Plugin = sorterPlugin,
                    Settings = new List<PluginSetting>
                    {
                        new PluginSetting { Name = "Sorting", PropertyName = "IsSorting", Type = SettingType.ReadOnly }
                    }
                });
            }

            if (utilCategory.Plugins.Count > 0)
                _categories.Add(utilCategory);
        }

        public void OnKeyEvent(IKeyEvent keyEvent)
        {
            if (!Hud.Game.IsInGame) return;

            if (TogglePanelKey.Matches(keyEvent) && keyEvent.IsPressed)
            {
                ShowPanel = !ShowPanel;
                if (ShowPanel)
                {
                    InitializePluginList();
                    _selectedCategoryIndex = 0;
                    _selectedPluginIndex = 0;
                    _scrollOffset = 0;
                }
                return;
            }

            if (!ShowPanel) return;

            // Navigation when panel is open
            if (_keyEscape.Matches(keyEvent) && keyEvent.IsPressed)
            {
                ShowPanel = false;
            }
            else if (_keyUp.Matches(keyEvent) && keyEvent.IsPressed)
            {
                NavigateUp();
            }
            else if (_keyDown.Matches(keyEvent) && keyEvent.IsPressed)
            {
                NavigateDown();
            }
            else if (_keyLeft.Matches(keyEvent) && keyEvent.IsPressed)
            {
                NavigateLeft();
            }
            else if (_keyRight.Matches(keyEvent) && keyEvent.IsPressed)
            {
                NavigateRight();
            }
            else if (_keyToggle.Matches(keyEvent) && keyEvent.IsPressed)
            {
                ToggleSelectedSetting();
            }
        }

        private void NavigateUp()
        {
            if (_selectedPluginIndex > 0)
            {
                _selectedPluginIndex--;
                if (_selectedPluginIndex < _scrollOffset)
                    _scrollOffset = _selectedPluginIndex;
            }
        }

        private void NavigateDown()
        {
            if (_categories.Count == 0) return;
            var category = _categories[_selectedCategoryIndex];
            if (_selectedPluginIndex < category.Plugins.Count - 1)
            {
                _selectedPluginIndex++;
                if (_selectedPluginIndex >= _scrollOffset + MAX_VISIBLE_ITEMS)
                    _scrollOffset = _selectedPluginIndex - MAX_VISIBLE_ITEMS + 1;
            }
        }

        private void NavigateLeft()
        {
            if (_selectedCategoryIndex > 0)
            {
                _selectedCategoryIndex--;
                _selectedPluginIndex = 0;
                _scrollOffset = 0;
            }
        }

        private void NavigateRight()
        {
            if (_selectedCategoryIndex < _categories.Count - 1)
            {
                _selectedCategoryIndex++;
                _selectedPluginIndex = 0;
                _scrollOffset = 0;
            }
        }

        private void ToggleSelectedSetting()
        {
            if (_categories.Count == 0) return;
            var category = _categories[_selectedCategoryIndex];
            if (_selectedPluginIndex >= category.Plugins.Count) return;

            var plugin = category.Plugins[_selectedPluginIndex];
            
            // Toggle the plugin's Enabled state
            plugin.Plugin.Enabled = !plugin.Plugin.Enabled;
        }

        public void PaintTopInGame(ClipState clipState)
        {
            if (clipState != ClipState.AfterClip) return;
            if (!ShowPanel) return;

            InitializePluginList();
            if (_categories.Count == 0) return;

            float x = Hud.Window.Size.Width * PanelX;
            float y = Hud.Window.Size.Height * PanelY;
            float w = PanelWidth;
            float h = PanelHeight;
            float pad = 8;
            float lineHeight = 22;

            // Main panel background
            _panelBrush.DrawRectangle(x, y, w, h);
            _borderBrush.DrawRectangle(x, y, w, h);

            // Header
            float headerH = 35;
            _headerBrush.DrawRectangle(x, y, w, headerH);
            var titleLayout = _titleFont.GetTextLayout("Custom Plugins Config");
            _titleFont.DrawText(titleLayout, x + (w - titleLayout.Metrics.Width) / 2, y + (headerH - titleLayout.Metrics.Height) / 2);

            float contentY = y + headerH + pad;

            // Category tabs
            float tabX = x + pad;
            float tabY = contentY;
            float tabHeight = 25;

            for (int i = 0; i < _categories.Count; i++)
            {
                var cat = _categories[i];
                var font = i == _selectedCategoryIndex ? _categorySelectedFont : _categoryFont;
                var layout = font.GetTextLayout(cat.Name);
                float tabWidth = layout.Metrics.Width + 20;

                if (i == _selectedCategoryIndex)
                {
                    _selectedBrush.DrawRectangle(tabX, tabY, tabWidth, tabHeight);
                }
                else
                {
                    _categoryBrush.DrawRectangle(tabX, tabY, tabWidth, tabHeight);
                }

                font.DrawText(layout, tabX + 10, tabY + (tabHeight - layout.Metrics.Height) / 2);
                tabX += tabWidth + 5;
            }

            contentY += tabHeight + pad;

            // Plugin list for selected category
            var selectedCategory = _categories[_selectedCategoryIndex];
            float listY = contentY;

            for (int i = _scrollOffset; i < Math.Min(selectedCategory.Plugins.Count, _scrollOffset + MAX_VISIBLE_ITEMS); i++)
            {
                var plugin = selectedCategory.Plugins[i];
                bool isSelected = i == _selectedPluginIndex;
                bool isEnabled = plugin.Plugin.Enabled;

                float itemH = lineHeight * 2 + 5;

                // Selection highlight
                if (isSelected)
                {
                    _selectedBrush.DrawRectangle(x + pad, listY, w - pad * 2, itemH);
                }

                // Status indicator
                var statusBrush = isEnabled ? _enabledBrush : _disabledBrush;
                statusBrush.DrawRectangle(x + pad, listY, 4, itemH);

                // Plugin name
                var nameFont = isEnabled ? _pluginEnabledFont : _pluginDisabledFont;
                var nameLayout = nameFont.GetTextLayout(plugin.Name);
                nameFont.DrawText(nameLayout, x + pad + 10, listY + 2);

                // Enabled/Disabled text
                string statusText = isEnabled ? "[ON]" : "[OFF]";
                var statusLayout = nameFont.GetTextLayout(statusText);
                nameFont.DrawText(statusLayout, x + w - pad - statusLayout.Metrics.Width - 5, listY + 2);

                // Description
                var descLayout = _settingFont.GetTextLayout(plugin.Description);
                _settingFont.DrawText(descLayout, x + pad + 10, listY + lineHeight);

                listY += itemH + 3;
            }

            // Footer hints
            float footerY = y + h - 25;
            var hintLayout = _hintFont.GetTextLayout("↑↓ Navigate  ←→ Category  Enter Toggle  Esc Close  Ctrl+Shift+C Toggle Panel");
            _hintFont.DrawText(hintLayout, x + (w - hintLayout.Metrics.Width) / 2, footerY);
        }
    }

    #region Helper Classes

    internal class PluginCategory
    {
        public string Name { get; set; }
        public List<ConfigurablePlugin> Plugins { get; set; } = new List<ConfigurablePlugin>();

        public PluginCategory(string name)
        {
            Name = name;
        }
    }

    internal class ConfigurablePlugin
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public IPlugin Plugin { get; set; }
        public List<PluginSetting> Settings { get; set; } = new List<PluginSetting>();
    }

    internal class PluginSetting
    {
        public string Name { get; set; }
        public string PropertyName { get; set; }
        public SettingType Type { get; set; }
    }

    internal enum SettingType
    {
        Boolean,
        Float,
        Int,
        String,
        ReadOnly
    }

    #endregion
}
