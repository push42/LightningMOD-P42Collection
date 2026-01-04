namespace Turbo.Plugins.Custom.Core
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using Newtonsoft.Json;
    using Turbo.Plugins.Default;

    /// <summary>
    /// Base class for custom plugins that integrate with the Core system
    /// 
    /// Provides:
    /// - Automatic registration with Core
    /// - Access to shared design system
    /// - Settings panel framework (with optional tabs support)
    /// - Settings persistence
    /// - Common utilities
    /// - Consistent logging
    /// </summary>
    public abstract class CustomPluginBase : BasePlugin, ICustomPlugin, IInGameTopPainter
    {
        #region ICustomPlugin Implementation (Abstract - must override)

        /// <summary>
        /// Unique identifier for this plugin (e.g., "smart-salvage", "lod-nova-macro")
        /// </summary>
        public abstract string PluginId { get; }

        /// <summary>
        /// Display name for the plugin
        /// </summary>
        public abstract string PluginName { get; }

        /// <summary>
        /// Short description of what the plugin does
        /// </summary>
        public abstract string PluginDescription { get; }

        /// <summary>
        /// Version string (e.g., "1.0.0")
        /// </summary>
        public abstract string PluginVersion { get; }

        /// <summary>
        /// Category for organization (combat, automation, inventory, visual, utility, debug)
        /// </summary>
        public abstract string PluginCategory { get; }

        /// <summary>
        /// Icon emoji for the plugin
        /// </summary>
        public abstract string PluginIcon { get; }

        #endregion

        #region Runtime State (Virtual - override to customize)

        /// <summary>
        /// Returns the current active/running state of the plugin.
        /// Override this in your plugin to return your IsActive property.
        /// Default: returns Enabled
        /// </summary>
        public virtual bool IsActive => Enabled;

        /// <summary>
        /// Short status text for sidebar display.
        /// Override this to provide custom status (e.g., "Ready", "Evading", "5 items")
        /// Default: "ON" or "OFF" based on IsActive
        /// </summary>
        public virtual string StatusText => IsActive ? "ON" : "OFF";

        #endregion

        #region Requirements (Virtual - override for class/build specific plugins)

        /// <summary>
        /// Required hero class for this plugin.
        /// Override and return a HeroClass value for class-specific plugins.
        /// Default: null (works with any class)
        /// </summary>
        public virtual HeroClass? RequiredHeroClass => null;

        /// <summary>
        /// Required build/set name for display (e.g., "LoD Death Nova", "Natalya's Set")
        /// Override to show build requirement in UI.
        /// Default: null (no specific build required)
        /// </summary>
        public virtual string RequiredBuild => null;

        /// <summary>
        /// Whether the plugin's requirements are currently met.
        /// Override to implement custom requirement checking (skills, sets, etc.)
        /// Default: checks only hero class if RequiredHeroClass is set
        /// </summary>
        public virtual bool RequirementsMet
        {
            get
            {
                if (RequiredHeroClass == null) return true;
                if (!Hud.Game.IsInGame) return false;
                return Hud.Game.Me?.HeroClassDefinition?.HeroClass == RequiredHeroClass.Value;
            }
        }

        #endregion

        #region Settings Panel (Virtual - can override)

        /// <summary>
        /// Whether this plugin has a settings panel
        /// Override and return true to enable settings
        /// </summary>
        public virtual bool HasSettings => false;

        /// <summary>
        /// Draw the plugin's settings UI
        /// Override to provide custom settings UI
        /// </summary>
        public virtual void DrawSettings(IController hud, RectangleF rect, Dictionary<string, RectangleF> clickAreas, int scrollOffset)
        {
            // Default: draw "No settings available"
            if (HasCore && Core.FontMuted != null)
            {
                var layout = Core.FontMuted.GetTextLayout("No settings available");
                Core.FontMuted.DrawText(layout, rect.X + 10, rect.Y + 10);
            }
        }

        /// <summary>
        /// Handle clicks in the settings panel
        /// Override to handle custom click interactions
        /// </summary>
        public virtual void HandleSettingsClick(string clickId)
        {
            // Default: no-op
        }

        #endregion

        #region Core Access

        /// <summary>
        /// Access to the Core plugin (fonts, brushes, etc.)
        /// </summary>
        protected CorePlugin Core => CorePlugin.Instance;

        /// <summary>
        /// Check if Core is available
        /// </summary>
        protected bool HasCore => CorePlugin.Instance != null;

        /// <summary>
        /// Get the data directory for this plugin
        /// </summary>
        protected string DataDirectory { get; private set; }

        /// <summary>
        /// Track if we've registered with Core
        /// </summary>
        private bool _isRegisteredWithCore = false;

        #endregion

        #region Lifecycle

        public override void Load(IController hud)
        {
            base.Load(hud);

            // Setup data directory
            string pluginFolder = GetType().Name.Replace("Plugin", "");
            DataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "Custom", pluginFolder);
            if (!Directory.Exists(DataDirectory))
            {
                try { Directory.CreateDirectory(DataDirectory); }
                catch { /* Ignore */ }
            }

            // Load saved settings
            LoadPluginSettings();

            // Try to register with Core immediately (may fail if Core hasn't loaded yet)
            TryRegisterWithCore();
        }

        /// <summary>
        /// IInGameTopPainter implementation - used to ensure registration
        /// This is called every frame when in game, so we can use it to retry registration
        /// </summary>
        public virtual void PaintTopInGame(ClipState clipState)
        {
            // Only check once per frame, and only before clip
            if (clipState == ClipState.BeforeClip && !_isRegisteredWithCore)
            {
                TryRegisterWithCore();
            }
        }

        /// <summary>
        /// Try to register this plugin with the Core system
        /// </summary>
        private void TryRegisterWithCore()
        {
            if (_isRegisteredWithCore) return;
            
            if (CorePlugin.Instance != null)
            {
                CorePlugin.Instance.RegisterPlugin(this);
                _isRegisteredWithCore = true;
                Log($"Registered with Core");
            }
        }

        #endregion

        #region Settings Persistence

        private string SettingsFilePath => Path.Combine(DataDirectory, "settings.json");

        /// <summary>
        /// Override to define your settings class
        /// </summary>
        protected virtual object GetSettingsObject() => null;

        /// <summary>
        /// Override to apply loaded settings
        /// </summary>
        protected virtual void ApplySettingsObject(object settings) { }

        /// <summary>
        /// Save plugin settings to file
        /// </summary>
        protected void SavePluginSettings()
        {
            try
            {
                var settings = GetSettingsObject();
                if (settings == null) return;

                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(SettingsFilePath, json);
                Log($"Settings saved");
            }
            catch (Exception ex)
            {
                LogError($"Failed to save settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Load plugin settings from file
        /// </summary>
        protected void LoadPluginSettings()
        {
            try
            {
                if (!File.Exists(SettingsFilePath)) return;

                string json = File.ReadAllText(SettingsFilePath);
                var settingsType = GetSettingsObject()?.GetType();
                if (settingsType == null) return;

                var settings = JsonConvert.DeserializeObject(json, settingsType);
                if (settings != null)
                {
                    ApplySettingsObject(settings);
                    Log($"Settings loaded");
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to load settings: {ex.Message}");
            }
        }

        #endregion

        #region Logging

        /// <summary>
        /// Log a message (simple console/debug output)
        /// </summary>
        protected void Log(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[{PluginId}] {message}");
        }

        /// <summary>
        /// Log with level indicator
        /// </summary>
        protected void LogInfo(string message) => Log($"[INFO] {message}");
        protected void LogWarn(string message) => Log($"[WARN] {message}");
        protected void LogError(string message) => Log($"[ERROR] {message}");

        #endregion

        #region Status

        /// <summary>
        /// Set a status message in the Core panel
        /// </summary>
        protected void SetCoreStatus(string message, StatusType type = StatusType.Info)
        {
            CorePlugin.Instance?.SetStatus(message, type);
        }

        #endregion

        #region Settings UI Helpers

        /// <summary>
        /// IMPORTANT: Click ID Prefixes
        /// The following prefixes are RESERVED by CorePlugin and should NOT be used
        /// in plugin settings:
        /// - "toggle_" - Used by Core for plugin enable/disable toggles
        /// - "plugin_" - Used by Core for plugin selection
        /// - "settings_" - Used by Core for opening plugin settings
        /// - "cat_" - Used by Core for category tabs
        /// - "settingstab_" - Used by Core for settings tabs
        /// 
        /// Recommended prefixes for plugin settings:
        /// - "opt_" - Options/toggles
        /// - "sel_" - Selectors
        /// - "btn_" - Buttons
        /// - "act_" - Actions
        /// </summary>

        /// <summary>
        /// Draw a settings section header
        /// </summary>
        protected float DrawSettingsHeader(float x, float y, string text)
        {
            if (!HasCore) return 20f;
            
            var layout = Core.FontSubheader.GetTextLayout(text);
            Core.FontSubheader.DrawText(layout, x, y);
            return layout.Metrics.Height + 4;
        }

        /// <summary>
        /// Draw a toggle setting
        /// IMPORTANT: Do NOT use "toggle_" prefix for clickId - it conflicts with Core.
        /// Use "opt_" prefix instead.
        /// </summary>
        protected float DrawToggleSetting(float x, float y, float width, string label, bool value, 
            Dictionary<string, RectangleF> clickAreas, string clickId)
        {
            if (!HasCore) return 28f;

            // Warn if using reserved prefix
            if (clickId.StartsWith("toggle_"))
            {
                LogWarn($"DrawToggleSetting: '{clickId}' uses reserved 'toggle_' prefix - will not work! Use 'opt_' instead.");
            }

            float rowH = 26f;
            var rect = new RectangleF(x, y, width - 14, rowH);
            clickAreas[clickId] = rect;

            bool hovered = IsMouseOver(rect);
            if (hovered)
                Core.SurfaceOverlay.DrawRectangle(rect);

            var labelLayout = Core.FontBody.GetTextLayout(label);
            Core.FontBody.DrawText(labelLayout, x + 8, y + (rowH - labelLayout.Metrics.Height) / 2);

            // Toggle on right
            float toggleW = 40, toggleH = 18;
            float tx = x + width - 14 - toggleW - 8, ty = y + (rowH - toggleH) / 2;
            Core.ToggleTrack.DrawRectangle(tx, ty, toggleW, toggleH);
            
            float knobSize = toggleH - 4;
            float knobX = value ? tx + toggleW - knobSize - 2 : tx + 2;
            var knobBrush = value ? Core.ToggleOn : Core.ToggleOff;
            knobBrush.DrawRectangle(knobX, ty + 2, knobSize, knobSize);

            return rowH + 4;
        }

        /// <summary>
        /// Draw a selector setting (with prev/next buttons)
        /// </summary>
        protected float DrawSelectorSetting(float x, float y, float width, string label, string value,
            Dictionary<string, RectangleF> clickAreas, string baseClickId)
        {
            if (!HasCore) return 28f;

            float rowH = 26f;

            var labelLayout = Core.FontBody.GetTextLayout(label);
            Core.FontBody.DrawText(labelLayout, x + 8, y + (rowH - labelLayout.Metrics.Height) / 2);

            // Selector on right
            float selW = 80, selH = 22;
            float sx = x + width - 14 - selW - 8, sy = y + (rowH - selH) / 2;

            // Prev button
            var prevRect = new RectangleF(sx, sy, 20, selH);
            clickAreas[$"{baseClickId}_prev"] = prevRect;
            Core.BtnDefault.DrawRectangle(prevRect);
            var prevLayout = Core.FontSmall.GetTextLayout("◀");
            Core.FontSmall.DrawText(prevLayout, sx + (20 - prevLayout.Metrics.Width) / 2, sy + (selH - prevLayout.Metrics.Height) / 2);

            // Value display
            var valLayout = Core.FontMono.GetTextLayout(value);
            Core.FontMono.DrawText(valLayout, sx + 20 + (selW - 40 - valLayout.Metrics.Width) / 2, sy + (selH - valLayout.Metrics.Height) / 2);

            // Next button
            var nextRect = new RectangleF(sx + selW - 20, sy, 20, selH);
            clickAreas[$"{baseClickId}_next"] = nextRect;
            Core.BtnDefault.DrawRectangle(nextRect);
            var nextLayout = Core.FontSmall.GetTextLayout("▶");
            Core.FontSmall.DrawText(nextLayout, sx + selW - 20 + (20 - nextLayout.Metrics.Width) / 2, sy + (selH - nextLayout.Metrics.Height) / 2);

            return rowH + 4;
        }

        /// <summary>
        /// Draw a slider setting
        /// </summary>
        protected float DrawSliderSetting(float x, float y, float width, string label, float value, float min, float max,
            Dictionary<string, RectangleF> clickAreas, string clickId)
        {
            if (!HasCore) return 28f;

            float rowH = 26f;
            var labelLayout = Core.FontBody.GetTextLayout(label);
            Core.FontBody.DrawText(labelLayout, x + 8, y + (rowH - labelLayout.Metrics.Height) / 2);

            // Slider track
            float sliderW = 100, sliderH = 6;
            float sx = x + width - 14 - sliderW - 60, sy = y + (rowH - sliderH) / 2;
            
            var trackRect = new RectangleF(sx, sy, sliderW, sliderH);
            clickAreas[clickId] = trackRect;
            Core.ProgressTrack.DrawRectangle(sx, sy, sliderW, sliderH);
            
            // Fill
            float pct = (value - min) / (max - min);
            Core.ProgressFill.DrawRectangle(sx, sy, sliderW * pct, sliderH);
            
            // Knob
            float knobX = sx + sliderW * pct - 4;
            Core.ToggleKnob.DrawRectangle(knobX, sy - 3, 8, sliderH + 6);

            // Value label
            var valLayout = Core.FontMono.GetTextLayout($"{value:F1}");
            Core.FontMono.DrawText(valLayout, sx + sliderW + 8, y + (rowH - valLayout.Metrics.Height) / 2);

            return rowH + 4;
        }

        /// <summary>
        /// Draw a button setting
        /// </summary>
        protected float DrawButtonSetting(float x, float y, float width, string label, string buttonText,
            Dictionary<string, RectangleF> clickAreas, string clickId, bool primary = false, bool danger = false)
        {
            if (!HasCore) return 32f;

            float rowH = 28f;
            
            var labelLayout = Core.FontBody.GetTextLayout(label);
            Core.FontBody.DrawText(labelLayout, x + 8, y + (rowH - labelLayout.Metrics.Height) / 2);

            float btnW = 80, btnH = 22;
            float bx = x + width - 14 - btnW - 8, by = y + (rowH - btnH) / 2;
            
            var btnRect = new RectangleF(bx, by, btnW, btnH);
            clickAreas[clickId] = btnRect;
            
            bool hovered = IsMouseOver(btnRect);
            IBrush brush;
            if (danger) brush = Core.BtnDanger;
            else if (primary) brush = Core.BtnPrimary;
            else brush = hovered ? Core.BtnHover : Core.BtnDefault;
            
            brush.DrawRectangle(btnRect);
            
            var btnLayout = Core.FontSmall.GetTextLayout(buttonText);
            Core.FontSmall.DrawText(btnLayout, bx + (btnW - btnLayout.Metrics.Width) / 2, by + (btnH - btnLayout.Metrics.Height) / 2);

            return rowH + 4;
        }

        /// <summary>
        /// Draw a hint/description text
        /// </summary>
        protected float DrawSettingsHint(float x, float y, string text)
        {
            if (!HasCore) return 16f;
            
            var layout = Core.FontMuted.GetTextLayout(text);
            Core.FontMuted.DrawText(layout, x, y);
            return layout.Metrics.Height + 4;
        }

        /// <summary>
        /// Draw a horizontal separator line
        /// </summary>
        protected void DrawSettingsSeparator(float x, float y, float width)
        {
            if (HasCore)
                Core.BorderDefault.DrawLine(x, y, x + width, y);
        }

        /// <summary>
        /// Draw a progress bar
        /// </summary>
        protected float DrawProgressBar(float x, float y, float width, string label, float progress, StatusType type = StatusType.Success)
        {
            if (!HasCore) return 24f;

            float rowH = 22f;
            
            var labelLayout = Core.FontSmall.GetTextLayout(label);
            Core.FontSmall.DrawText(labelLayout, x + 8, y);

            float barH = 8;
            float barY = y + labelLayout.Metrics.Height + 2;
            
            Core.ProgressTrack.DrawRectangle(x + 8, barY, width - 16, barH);
            
            var fillBrush = Core.GetStatusBrush(type);
            fillBrush.DrawRectangle(x + 8, barY, (width - 16) * Math.Min(1f, Math.Max(0f, progress)), barH);

            return rowH + labelLayout.Metrics.Height;
        }

        #endregion

        #region UI Convenience Wrappers

        /// <summary>
        /// Check if mouse is over a rectangle
        /// </summary>
        protected bool IsMouseOver(RectangleF rect)
        {
            int mx = Hud.Window.CursorX, my = Hud.Window.CursorY;
            return mx >= rect.X && mx <= rect.X + rect.Width &&
                   my >= rect.Y && my <= rect.Y + rect.Height;
        }

        #endregion
    }

    /// <summary>
    /// Base class for plugins with tabbed settings panels
    /// Use this when your plugin has complex settings that need organization
    /// </summary>
    public abstract class TabbedCustomPluginBase : CustomPluginBase, ITabbedSettingsPlugin
    {
        /// <summary>
        /// Define the tabs for your settings panel
        /// Override in your plugin
        /// </summary>
        public virtual List<SettingsTab> SettingsTabs => new List<SettingsTab>();

        /// <summary>
        /// Draw settings content for a specific tab
        /// Override this to provide tab-specific settings UI
        /// </summary>
        public virtual void DrawTabSettings(IController hud, RectangleF rect, Dictionary<string, RectangleF> clickAreas, int scrollOffset, string tabId)
        {
            // Default: draw the main settings
            DrawSettings(hud, rect, clickAreas, scrollOffset);
        }
    }

    #region Settings Base Class

    /// <summary>
    /// Base class for plugin settings
    /// Inherit from this to define your plugin's settings
    /// </summary>
    public abstract class PluginSettingsBase
    {
        /// <summary>
        /// When these settings were last modified
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.Now;

        /// <summary>
        /// Settings version for migration support
        /// </summary>
        public int Version { get; set; } = 1;
    }

    #endregion
}
