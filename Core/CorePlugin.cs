namespace Turbo.Plugins.Custom.Core
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Windows.Forms;
    using Newtonsoft.Json;
    using SharpDX.DirectInput;
    using Turbo.Plugins.Default;

    public class CorePlugin : BasePlugin, IKeyEventHandler, IInGameTopPainter, IAfterCollectHandler
    {
        private static CorePlugin _instance;
        public static CorePlugin Instance => _instance;

        public IKeyEvent SettingsKey { get; set; }
        public IKeyEvent DebugKey { get; set; }
        public IKeyEvent QuickToggleKey { get; set; }
        public string DataDirectory { get; private set; }

        public IFont FontTitle { get; private set; }
        public IFont FontHeader { get; private set; }
        public IFont FontSubheader { get; private set; }
        public IFont FontBody { get; private set; }
        public IFont FontSmall { get; private set; }
        public IFont FontMicro { get; private set; }
        public IFont FontMono { get; private set; }
        public IFont FontIcon { get; private set; }
        public IFont FontIconLarge { get; private set; }
        public IFont FontSuccess { get; private set; }
        public IFont FontWarning { get; private set; }
        public IFont FontError { get; private set; }
        public IFont FontAccent { get; private set; }
        public IFont FontMuted { get; private set; }
        public IFont FontHighlight { get; private set; }

        public IBrush SurfaceBase { get; private set; }
        public IBrush SurfaceElevated { get; private set; }
        public IBrush SurfaceOverlay { get; private set; }
        public IBrush SurfaceCard { get; private set; }
        public IBrush SurfaceModal { get; private set; }
        public IBrush SurfaceGlass { get; private set; }
        public IBrush BorderDefault { get; private set; }
        public IBrush BorderAccent { get; private set; }
        public IBrush BorderSuccess { get; private set; }
        public IBrush BorderWarning { get; private set; }
        public IBrush BorderError { get; private set; }
        public IBrush BorderFocus { get; private set; }
        public IBrush BorderSubtle { get; private set; }
        public IBrush BtnDefault { get; private set; }
        public IBrush BtnHover { get; private set; }
        public IBrush BtnActive { get; private set; }
        public IBrush BtnDisabled { get; private set; }
        public IBrush BtnPrimary { get; private set; }
        public IBrush BtnDanger { get; private set; }
        public IBrush ToggleOn { get; private set; }
        public IBrush ToggleOff { get; private set; }
        public IBrush ToggleTrack { get; private set; }
        public IBrush ToggleKnob { get; private set; }
        public IBrush StatusSuccess { get; private set; }
        public IBrush StatusWarning { get; private set; }
        public IBrush StatusError { get; private set; }
        public IBrush StatusInfo { get; private set; }
        public IBrush ProgressTrack { get; private set; }
        public IBrush ProgressFill { get; private set; }
        public IBrush ProgressFillWarning { get; private set; }
        public IBrush ProgressFillError { get; private set; }
        public IBrush ScrollTrack { get; private set; }
        public IBrush ScrollThumb { get; private set; }
        public IBrush HighlightPositive { get; private set; }
        public IBrush HighlightNegative { get; private set; }
        public IBrush HighlightSpecial { get; private set; }
        public IBrush HighlightNeutral { get; private set; }
        public IBrush TabActive { get; private set; }
        public IBrush TabInactive { get; private set; }
        public IBrush TabHover { get; private set; }

        public Dictionary<string, PluginEntry> RegisteredPlugins { get; private set; }
        public List<PluginCategory> Categories { get; private set; }

        private bool _showSettings;
        private bool _showDebug;
        private string _activeCategory = "all";
        private string _selectedPluginId = null;
        private string _activeSettingsTab = null;
        private int _scrollOffset;
        private int _settingsScrollOffset;
        private Dictionary<string, RectangleF> _clickAreas;
        private bool _wasMouseDown;
        private IWatch _clickTimer;
        private float _panelSlide;
        private float _settingsPanelSlide;
        private string _statusMessage;
        private StatusType _statusType;
        private IWatch _statusTimer;
        private bool _sidebarExpanded;
        private float _sidebarSlide;
        private IWatch _sidebarHoverTimer;
        private Dictionary<string, RectangleF> _sidebarClickAreas = new Dictionary<string, RectangleF>();
        private bool _sidebarWasMouseDown;
        private IWatch _sidebarClickTimer;
        private Dictionary<string, bool> _pluginStates;
        private string _settingsFilePath;

        private const float SIDEBAR_WIDTH = 40f;
        private const float SIDEBAR_EXPANDED_WIDTH = 180f;
        private const float MAIN_PANEL_WIDTH = 300f;
        private const float MAIN_PANEL_HEIGHT = 540f;
        private const float SETTINGS_PANEL_WIDTH = 400f;
        private const float SETTINGS_PANEL_HEIGHT = 580f;
        private const int MAX_VISIBLE_PLUGINS = 8;

        private IUiElement _inventoryElement;
        private IUiElement _stashElement;

        public CorePlugin() { Enabled = true; Order = 1; }

        public override void Load(IController hud)
        {
            base.Load(hud);
            _instance = this;
            DataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "Custom", "Core");
            if (!Directory.Exists(DataDirectory)) Directory.CreateDirectory(DataDirectory);
            _settingsFilePath = Path.Combine(DataDirectory, "settings.json");
            SettingsKey = Hud.Input.CreateKeyEvent(true, Key.Grave, false, false, false);      // ` (Grave/Tilde) = Settings Panel
            DebugKey = Hud.Input.CreateKeyEvent(true, Key.Grave, true, false, false);          // Ctrl+` = Debug Overlay
            QuickToggleKey = Hud.Input.CreateKeyEvent(true, Key.Grave, false, false, true);    // Shift+` = Quick Toggle All
            RegisteredPlugins = new Dictionary<string, PluginEntry>();
            _pluginStates = new Dictionary<string, bool>();
            Categories = new List<PluginCategory>
            {
                new PluginCategory("all", "All", "📋", "All plugins"),
                new PluginCategory("combat", "Combat", "⚔️", "Combat & evade plugins"),
                new PluginCategory("macro", "Macro", "🎮", "Build-specific macros"),
                new PluginCategory("automation", "Auto", "🤖", "Automation plugins"),
                new PluginCategory("inventory", "Inv", "🎒", "Inventory management"),
                new PluginCategory("visual", "Visual", "👁️", "Visual enhancements"),
                new PluginCategory("utility", "Util", "🔧", "Utility plugins"),
            };
            _clickAreas = new Dictionary<string, RectangleF>();
            _clickTimer = Hud.Time.CreateAndStartWatch();
            _statusTimer = Hud.Time.CreateWatch();
            _sidebarHoverTimer = Hud.Time.CreateAndStartWatch();
            _sidebarClickTimer = Hud.Time.CreateAndStartWatch();
            _inventoryElement = Hud.Inventory.InventoryMainUiElement;
            _stashElement = Hud.Inventory.StashMainUiElement;
            InitializeDesignSystem();
            LoadSettings();
            Log("Core Plugin v3.0 initialized");
        }

        private void InitializeDesignSystem()
        {
            FontTitle = Hud.Render.CreateFont("segoe ui semibold", 12, 255, 255, 220, 120, true, false, 200, 0, 0, 0, true);
            FontHeader = Hud.Render.CreateFont("segoe ui semibold", 9, 255, 240, 240, 250, true, false, 180, 0, 0, 0, true);
            FontSubheader = Hud.Render.CreateFont("segoe ui semibold", 8f, 255, 210, 210, 225, true, false, 160, 0, 0, 0, true);
            FontBody = Hud.Render.CreateFont("segoe ui", 7.5f, 255, 200, 200, 215, false, false, 160, 0, 0, 0, true);
            FontSmall = Hud.Render.CreateFont("segoe ui", 6.5f, 220, 160, 160, 175, false, false, 140, 0, 0, 0, true);
            FontMicro = Hud.Render.CreateFont("segoe ui", 6f, 180, 130, 130, 145, false, false, 120, 0, 0, 0, true);
            FontMono = Hud.Render.CreateFont("consolas", 7f, 255, 180, 190, 200, false, false, 150, 0, 0, 0, true);
            FontIcon = Hud.Render.CreateFont("segoe ui symbol", 9, 255, 255, 255, 255, false, false, 180, 0, 0, 0, true);
            FontIconLarge = Hud.Render.CreateFont("segoe ui symbol", 12, 255, 255, 255, 255, false, false, 200, 0, 0, 0, true);
            FontSuccess = Hud.Render.CreateFont("segoe ui", 7.5f, 255, 100, 220, 130, true, false, 160, 0, 0, 0, true);
            FontWarning = Hud.Render.CreateFont("segoe ui", 7.5f, 255, 250, 190, 90, true, false, 160, 0, 0, 0, true);
            FontError = Hud.Render.CreateFont("segoe ui", 7.5f, 255, 250, 110, 110, true, false, 160, 0, 0, 0, true);
            FontAccent = Hud.Render.CreateFont("segoe ui", 7.5f, 255, 110, 170, 250, true, false, 160, 0, 0, 0, true);
            FontMuted = Hud.Render.CreateFont("segoe ui", 7f, 160, 120, 120, 135, false, false, 130, 0, 0, 0, true);
            FontHighlight = Hud.Render.CreateFont("segoe ui", 7.5f, 255, 250, 220, 140, true, false, 160, 0, 0, 0, true);
            SurfaceBase = Hud.Render.CreateBrush(245, 12, 12, 18, 0);
            SurfaceElevated = Hud.Render.CreateBrush(240, 18, 18, 26, 0);
            SurfaceOverlay = Hud.Render.CreateBrush(235, 24, 24, 34, 0);
            SurfaceCard = Hud.Render.CreateBrush(230, 30, 32, 42, 0);
            SurfaceModal = Hud.Render.CreateBrush(250, 10, 10, 16, 0);
            SurfaceGlass = Hud.Render.CreateBrush(200, 20, 20, 30, 0);
            BorderDefault = Hud.Render.CreateBrush(140, 50, 55, 70, 1f);
            BorderAccent = Hud.Render.CreateBrush(255, 90, 150, 250, 1.5f);
            BorderSuccess = Hud.Render.CreateBrush(255, 70, 190, 110, 1.5f);
            BorderWarning = Hud.Render.CreateBrush(255, 250, 170, 70, 1.5f);
            BorderError = Hud.Render.CreateBrush(255, 250, 90, 90, 1.5f);
            BorderFocus = Hud.Render.CreateBrush(255, 140, 190, 250, 2f);
            BorderSubtle = Hud.Render.CreateBrush(80, 60, 65, 80, 1f);
            BtnDefault = Hud.Render.CreateBrush(200, 40, 45, 58, 0);
            BtnHover = Hud.Render.CreateBrush(220, 55, 62, 82, 0);
            BtnActive = Hud.Render.CreateBrush(240, 70, 85, 120, 0);
            BtnDisabled = Hud.Render.CreateBrush(120, 32, 35, 45, 0);
            BtnPrimary = Hud.Render.CreateBrush(255, 50, 130, 190, 0);
            BtnDanger = Hud.Render.CreateBrush(255, 170, 55, 55, 0);
            ToggleOn = Hud.Render.CreateBrush(255, 60, 180, 100, 0);
            ToggleOff = Hud.Render.CreateBrush(255, 100, 55, 55, 0);
            ToggleTrack = Hud.Render.CreateBrush(180, 40, 45, 55, 0);
            ToggleKnob = Hud.Render.CreateBrush(255, 235, 235, 240, 0);
            StatusSuccess = Hud.Render.CreateBrush(255, 50, 180, 90, 0);
            StatusWarning = Hud.Render.CreateBrush(255, 220, 160, 50, 0);
            StatusError = Hud.Render.CreateBrush(255, 220, 80, 80, 0);
            StatusInfo = Hud.Render.CreateBrush(255, 80, 150, 220, 0);
            ProgressTrack = Hud.Render.CreateBrush(180, 35, 38, 50, 0);
            ProgressFill = Hud.Render.CreateBrush(255, 70, 190, 110, 0);
            ProgressFillWarning = Hud.Render.CreateBrush(255, 220, 160, 50, 0);
            ProgressFillError = Hud.Render.CreateBrush(255, 220, 80, 80, 0);
            ScrollTrack = Hud.Render.CreateBrush(160, 30, 33, 45, 0);
            ScrollThumb = Hud.Render.CreateBrush(200, 70, 75, 95, 0);
            HighlightPositive = Hud.Render.CreateBrush(180, 80, 220, 120, 2f);
            HighlightNegative = Hud.Render.CreateBrush(180, 230, 90, 90, 2f);
            HighlightSpecial = Hud.Render.CreateBrush(180, 180, 120, 255, 2f);
            HighlightNeutral = Hud.Render.CreateBrush(160, 150, 150, 170, 1.5f);
            TabActive = Hud.Render.CreateBrush(240, 45, 50, 68, 0);
            TabInactive = Hud.Render.CreateBrush(180, 28, 30, 40, 0);
            TabHover = Hud.Render.CreateBrush(210, 38, 42, 55, 0);
        }

        private void LoadSettings()
        {
            try { if (File.Exists(_settingsFilePath)) { var d = JsonConvert.DeserializeObject<CoreSettings>(File.ReadAllText(_settingsFilePath)); if (d?.PluginStates != null) _pluginStates = d.PluginStates; } } catch { }
        }

        private void SaveSettings()
        {
            try { foreach (var e in RegisteredPlugins.Values) _pluginStates[e.Id] = e.IsEnabled; File.WriteAllText(_settingsFilePath, JsonConvert.SerializeObject(new CoreSettings { PluginStates = _pluginStates }, Formatting.Indented)); } catch { }
        }

        private class CoreSettings { public Dictionary<string, bool> PluginStates { get; set; } = new Dictionary<string, bool>(); }

        public void RegisterPlugin(ICustomPlugin plugin)
        {
            if (plugin == null) return;
            var entry = new PluginEntry { Id = plugin.PluginId, Name = plugin.PluginName, Description = plugin.PluginDescription, Version = plugin.PluginVersion, Category = plugin.PluginCategory, Icon = plugin.PluginIcon, Plugin = plugin, IsEnabled = plugin.Enabled };
            if (_pluginStates.TryGetValue(entry.Id, out bool s)) { entry.IsEnabled = s; entry.Plugin.Enabled = s; }
            RegisteredPlugins[entry.Id] = entry;
            Log($"Registered: {entry.Name} v{entry.Version}");
        }

        public void UnregisterPlugin(string id) { if (RegisteredPlugins.ContainsKey(id)) RegisteredPlugins.Remove(id); }
        public PluginEntry GetPlugin(string id) => RegisteredPlugins.TryGetValue(id, out var e) ? e : null;

        public void SetPluginEnabled(string id, bool enabled)
        {
            if (RegisteredPlugins.TryGetValue(id, out var e)) { e.IsEnabled = enabled; e.Plugin.Enabled = enabled; _pluginStates[id] = enabled; SetStatus($"{e.Name} {(enabled ? "enabled" : "disabled")}", enabled ? StatusType.Success : StatusType.Warning); SaveSettings(); }
        }

        public void TogglePlugin(string id) { if (RegisteredPlugins.TryGetValue(id, out var e)) SetPluginEnabled(id, !e.IsEnabled); }
        public void EnableAllPlugins() { foreach (var e in RegisteredPlugins.Values) { e.IsEnabled = true; e.Plugin.Enabled = true; _pluginStates[e.Id] = true; } SaveSettings(); SetStatus("All plugins enabled", StatusType.Success); }
        public void DisableAllPlugins() { foreach (var e in RegisteredPlugins.Values) { e.IsEnabled = false; e.Plugin.Enabled = false; _pluginStates[e.Id] = false; } SaveSettings(); SetStatus("All plugins disabled", StatusType.Warning); }
        public int GetEnabledCount() => RegisteredPlugins.Values.Count(p => p.IsEnabled);

        public void OnKeyEvent(IKeyEvent keyEvent)
        {
            if (!Hud.Game.IsInGame) return;
            if (QuickToggleKey.Matches(keyEvent) && keyEvent.IsPressed) { if (RegisteredPlugins.Values.Any(p => p.IsEnabled)) DisableAllPlugins(); else EnableAllPlugins(); return; }
            if (SettingsKey.Matches(keyEvent) && keyEvent.IsPressed) { _showSettings = !_showSettings; _scrollOffset = 0; _settingsScrollOffset = 0; _activeSettingsTab = null; if (!_showSettings) _selectedPluginId = null; return; }
            if (DebugKey.Matches(keyEvent) && keyEvent.IsPressed) { _showDebug = !_showDebug; return; }
        }

        public void AfterCollect() { foreach (var e in RegisteredPlugins.Values) e.IsEnabled = e.Plugin.Enabled; }

        public void PaintTopInGame(ClipState clipState)
        {
            if (!Hud.Game.IsInGame) return;
            bool gameUIOpen = (_inventoryElement?.Visible == true) || (_stashElement?.Visible == true) || Hud.Render.IsAnyBlockingUiElementVisible;
            if (clipState == ClipState.BeforeClip) { _sidebarClickAreas.Clear(); if (!gameUIOpen && !_showSettings) { UpdateSidebarAnimation(); DrawSidebar(); } if (_showDebug && !gameUIOpen) DrawDebugOverlay(); }
            if (clipState == ClipState.AfterClip && _showSettings) { UpdateAnimations(); _clickAreas.Clear(); DrawSettingsPanel(); HandleMouseInput(); }
        }

        private void UpdateAnimations() { _panelSlide += ((_showSettings ? 1f : 0f) - _panelSlide) * 0.18f; _settingsPanelSlide += ((_selectedPluginId != null ? 1f : 0f) - _settingsPanelSlide) * 0.18f; }

        private void UpdateSidebarAnimation()
        {
            int mx = Hud.Window.CursorX; bool near = mx < SIDEBAR_EXPANDED_WIDTH + 20;
            if (near && !_sidebarExpanded && _sidebarHoverTimer.ElapsedMilliseconds > 200) _sidebarExpanded = true;
            else if (!near) { _sidebarExpanded = false; _sidebarHoverTimer.Restart(); }
            _sidebarSlide += ((_sidebarExpanded ? 1f : 0f) - _sidebarSlide) * 0.2f;
        }

        private void DrawSidebar()
        {
            float w = SIDEBAR_WIDTH + (SIDEBAR_EXPANDED_WIDTH - SIDEBAR_WIDTH) * _sidebarSlide;
            float h = Math.Min(RegisteredPlugins.Count * 36 + 50, Hud.Window.Size.Height * 0.6f);
            float x = 0, y = 80;
            Hud.Render.CreateBrush((byte)(200 + 40 * _sidebarSlide), 12, 12, 18, 0).DrawRectangle(x, y, w, h);
            BorderSubtle.DrawLine(x + w, y, x + w, y + h);
            float py = y + 8;
            if (_sidebarSlide > 0.5f) { var l = FontSubheader.GetTextLayout("⚡ Plugins"); FontSubheader.DrawText(l, x + 10, py); }
            else { var l = FontIcon.GetTextLayout("⚡"); FontIcon.DrawText(l, x + (SIDEBAR_WIDTH - 16) / 2, py); }
            py += 26; BorderSubtle.DrawLine(x + 6, py, x + w - 6, py); py += 8;
            foreach (var entry in RegisteredPlugins.Values.OrderByDescending(p => p.IsActive).Take(12)) { DrawSidebarItem(x, py, w, entry); py += 32; if (py > y + h - 20) break; }
            if (_sidebarSlide > 0.5f && py < y + h - 10) { var l = FontMicro.GetTextLayout("[F10] Settings"); FontMuted.DrawText(l, x + 10, y + h - 18); }
            if (_sidebarExpanded) HandleSidebarClicks();
        }

        private void DrawSidebarItem(float x, float y, float width, PluginEntry entry)
        {
            float itemH = 28, pad = 6;
            bool isActive = entry.IsActive;
            bool reqMet = entry.RequirementsMet;
            
            var itemRect = new RectangleF(x, y, width, itemH);
            if (_sidebarExpanded && IsMouseOver(itemRect)) SurfaceOverlay.DrawRectangle(itemRect);
            _sidebarClickAreas[entry.Id] = itemRect;
            
            // Status indicator dot - show different states:
            // Green = active, Red = off, Gray = requirements not met
            IBrush dotBrush;
            if (!reqMet)
                dotBrush = Hud.Render.CreateBrush(120, 80, 80, 90, 0);  // Grayed out
            else
                dotBrush = isActive ? StatusSuccess : StatusError;
            dotBrush.DrawEllipse(x + pad + 4, y + itemH / 2, 4, 4);
            
            // Icon - dim if requirements not met
            var iconLayout = FontIcon.GetTextLayout(entry.Icon);
            if (reqMet)
                FontIcon.DrawText(iconLayout, x + pad + 14, y + (itemH - iconLayout.Metrics.Height) / 2);
            else
                FontMuted.DrawText(FontMuted.GetTextLayout(entry.Icon), x + pad + 14, y + (itemH - iconLayout.Metrics.Height) / 2);
            
            if (_sidebarSlide > 0.3f)
            {
                string name = entry.Name.Length > 10 ? entry.Name.Substring(0, 8) + ".." : entry.Name;
                IFont nameFont;
                if (!reqMet)
                    nameFont = FontMuted;
                else
                    nameFont = isActive ? FontBody : FontMuted;
                var nameLayout = nameFont.GetTextLayout(name);
                nameFont.DrawText(nameLayout, x + pad + 36, y + (itemH - nameLayout.Metrics.Height) / 2);
                
                if (_sidebarSlide > 0.7f)
                {
                    string status;
                    IFont statusFont;
                    if (!reqMet)
                    {
                        // Show required class icon when requirements not met
                        status = entry.Plugin?.RequiredHeroClass?.ToString().Substring(0, 3) ?? "N/A";
                        statusFont = FontMuted;
                    }
                    else
                    {
                        status = entry.StatusText;
                        statusFont = isActive ? FontSuccess : FontError;
                    }
                    var statusLayout = statusFont.GetTextLayout(status);
                    statusFont.DrawText(statusLayout, x + width - statusLayout.Metrics.Width - pad - 4, y + (itemH - statusLayout.Metrics.Height) / 2);
                }
            }
        }

        private void HandleSidebarClicks()
        {
            bool down = Hud.Input.IsKeyDown(Keys.LButton), clicked = !down && _sidebarWasMouseDown && _sidebarClickTimer.ElapsedMilliseconds > 150;
            _sidebarWasMouseDown = down; if (!clicked) return; _sidebarClickTimer.Restart();
            foreach (var kvp in _sidebarClickAreas) if (IsMouseOver(kvp.Value)) { TogglePlugin(kvp.Key); return; }
        }

        private void DrawSettingsPanel()
        {
            float w = MAIN_PANEL_WIDTH, h = MAIN_PANEL_HEIGHT;
            float x = (Hud.Window.Size.Width - w) / 2, y = (Hud.Window.Size.Height - h) / 2;
            if (_selectedPluginId != null && _settingsPanelSlide > 0.1f) x = (Hud.Window.Size.Width - w - SETTINGS_PANEL_WIDTH - 8) / 2;
            y += (1f - _panelSlide) * 50;
            byte alpha = (byte)(_panelSlide * 250);
            Hud.Render.CreateBrush(alpha, 12, 12, 18, 0).DrawRectangle(x, y, w, h);
            Hud.Render.CreateBrush((byte)(_panelSlide * 160), 50, 55, 70, 1f).DrawRectangle(x, y, w, h);
            StatusInfo.DrawRectangle(x, y, 2, h);
            float pad = 14, cx = x + pad + 2, cy = y + pad, cw = w - pad * 2 - 2;
            var titleLayout = FontTitle.GetTextLayout("⚡ Plugin Hub"); FontTitle.DrawText(titleLayout, cx, cy);
            var closeRect = new RectangleF(x + w - 32, y + 12, 22, 22); _clickAreas["close"] = closeRect; DrawIconButton(closeRect, "✕", IsMouseOver(closeRect));
            cy += titleLayout.Metrics.Height + 2;
            var subLayout = FontMuted.GetTextLayout($"{GetEnabledCount()}/{RegisteredPlugins.Count} active"); FontMuted.DrawText(subLayout, cx, cy);
            cy += subLayout.Metrics.Height + 10;
            DrawCategoryTabs(cx, cy, cw); cy += 32;
            float btnW = (cw - 6) / 2;
            var enableRect = new RectangleF(cx, cy, btnW, 24); var disableRect = new RectangleF(cx + btnW + 6, cy, btnW, 24);
            _clickAreas["enable_all"] = enableRect; _clickAreas["disable_all"] = disableRect;
            DrawButton(enableRect, "✓ All On", IsMouseOver(enableRect)); DrawButton(disableRect, "✗ All Off", IsMouseOver(disableRect));
            cy += 30;
            DrawPluginList(cx, cy, cw, h - (cy - y) - pad - 30);
            cy = y + h - pad - 20; DrawStatusMessage(cx, cy, cw);
            if (_selectedPluginId != null && _settingsPanelSlide > 0.1f) DrawPluginSettingsPanel(x + w + 8, y, SETTINGS_PANEL_WIDTH, SETTINGS_PANEL_HEIGHT);
        }

        private void DrawCategoryTabs(float x, float y, float width)
        {
            float tabH = 26, tabW = (width - (Categories.Count - 1) * 2) / Categories.Count;
            for (int i = 0; i < Categories.Count; i++)
            {
                var cat = Categories[i]; var rect = new RectangleF(x + (tabW + 2) * i, y, tabW, tabH);
                _clickAreas[$"cat_{cat.Id}"] = rect;
                bool active = _activeCategory == cat.Id, hover = IsMouseOver(rect);
                (active ? TabActive : (hover ? TabHover : TabInactive)).DrawRectangle(rect);
                var layout = FontSmall.GetTextLayout(cat.Icon);
                (active ? FontBody : FontMuted).DrawText(layout, rect.X + (tabW - layout.Metrics.Width) / 2, rect.Y + (tabH - layout.Metrics.Height) / 2);
            }
        }

        private void DrawPluginList(float x, float y, float width, float height)
        {
            var plugins = GetFilteredPlugins();
            float rowH = 46; int visible = Math.Min(MAX_VISIBLE_PLUGINS, plugins.Count - _scrollOffset);
            for (int i = 0; i < visible && i + _scrollOffset < plugins.Count; i++)
            {
                var plugin = plugins[i + _scrollOffset];
                var rect = new RectangleF(x, y, width - 8, rowH); _clickAreas[$"plugin_{plugin.Id}"] = rect;
                bool hover = IsMouseOver(rect), selected = _selectedPluginId == plugin.Id;
                bool reqMet = plugin.RequirementsMet;
                
                // Background - dimmer if requirements not met
                if (!reqMet)
                    SurfaceBase.DrawRectangle(rect);
                else
                    (selected ? SurfaceOverlay : (hover ? SurfaceCard : SurfaceElevated)).DrawRectangle(rect);
                
                if (selected) BorderAccent.DrawRectangle(rect);
                
                // Status bar on left - gray if requirements not met
                IBrush statusBarBrush;
                if (!reqMet)
                    statusBarBrush = Hud.Render.CreateBrush(100, 60, 60, 70, 0);
                else
                    statusBarBrush = plugin.IsActive ? StatusSuccess : StatusError;
                statusBarBrush.DrawRectangle(x + 2, y + 10, 2, rowH - 20);
                
                // Icon
                var iconLayout = FontIconLarge.GetTextLayout(plugin.Icon);
                if (reqMet)
                    FontIconLarge.DrawText(iconLayout, x + 12, y + (rowH - iconLayout.Metrics.Height) / 2);
                else
                    FontMuted.DrawText(FontMuted.GetTextLayout(plugin.Icon), x + 12, y + (rowH - iconLayout.Metrics.Height) / 2);
                
                float textX = x + 38;
                var nameFont = reqMet ? (plugin.IsActive ? FontBody : FontMuted) : FontMuted;
                var nameLayout = nameFont.GetTextLayout(plugin.Name);
                nameFont.DrawText(nameLayout, textX, y + 7);
                
                // Description or requirements text
                string descText;
                if (!reqMet && plugin.HasRequirements)
                    descText = "⚠ " + plugin.RequirementsText;
                else
                    descText = plugin.Description.Length > 32 ? plugin.Description.Substring(0, 29) + "..." : plugin.Description;
                var descLayout = FontMicro.GetTextLayout(descText);
                FontMuted.DrawText(descLayout, textX, y + 7 + nameLayout.Metrics.Height + 1);
                
                // Toggle - disabled if requirements not met
                float toggleW = 38, toggleH = 18, tx = x + width - 8 - toggleW - 8, ty = y + (rowH - toggleH) / 2;
                var toggleRect = new RectangleF(tx, ty, toggleW, toggleH);
                _clickAreas[$"toggle_{plugin.Id}"] = toggleRect;
                
                if (reqMet)
                {
                    ToggleTrack.DrawRectangle(tx, ty, toggleW, toggleH);
                    float knobSize = toggleH - 4, knobX = plugin.IsEnabled ? tx + toggleW - knobSize - 2 : tx + 2;
                    (plugin.IsEnabled ? ToggleOn : ToggleOff).DrawRectangle(knobX, ty + 2, knobSize, knobSize);
                }
                else
                {
                    // Grayed out toggle
                    Hud.Render.CreateBrush(100, 40, 40, 50, 0).DrawRectangle(tx, ty, toggleW, toggleH);
                }
                
                // Settings button
                if (plugin.Plugin.HasSettings)
                {
                    var settingsRect = new RectangleF(tx - 26, y + (rowH - 20) / 2, 20, 20);
                    _clickAreas[$"settings_{plugin.Id}"] = settingsRect;
                    DrawIconButton(settingsRect, "⚙", IsMouseOver(settingsRect));
                }
                
                y += rowH + 2;
            }
            if (plugins.Count > MAX_VISIBLE_PLUGINS) { float sx = x + width - 4, sy = y - (visible * (rowH + 2)), sh = visible * (rowH + 2); ProgressTrack.DrawRectangle(sx, sy, 4, sh); float thumbH = sh * ((float)MAX_VISIBLE_PLUGINS / plugins.Count), thumbY = sy + (sh - thumbH) * ((float)_scrollOffset / Math.Max(1, plugins.Count - MAX_VISIBLE_PLUGINS)); StatusInfo.DrawRectangle(sx, thumbY, 4, thumbH); }
        }

        private void DrawPluginSettingsPanel(float x, float y, float width, float height)
        {
            var entry = GetPlugin(_selectedPluginId); if (entry == null) return;
            x += (1f - _settingsPanelSlide) * 30; byte alpha = (byte)(_settingsPanelSlide * 245);
            Hud.Render.CreateBrush(alpha, 14, 14, 22, 0).DrawRectangle(x, y, width, height);
            Hud.Render.CreateBrush((byte)(_settingsPanelSlide * 150), 50, 55, 70, 1f).DrawRectangle(x, y, width, height);
            float pad = 14, cx = x + pad, cy = y + pad, cw = width - pad * 2;
            var iconLayout = FontIconLarge.GetTextLayout(entry.Icon); FontIconLarge.DrawText(iconLayout, cx, cy);
            var nameLayout = FontHeader.GetTextLayout(entry.Name); FontHeader.DrawText(nameLayout, cx + iconLayout.Metrics.Width + 8, cy + 2);
            var closeRect = new RectangleF(x + width - 32, y + 12, 22, 22); _clickAreas["close_settings"] = closeRect; DrawIconButton(closeRect, "◀", IsMouseOver(closeRect));
            cy += Math.Max(iconLayout.Metrics.Height, nameLayout.Metrics.Height) + 4;
            var verLayout = FontMuted.GetTextLayout($"v{entry.Version}"); FontMuted.DrawText(verLayout, cx, cy);
            cy += verLayout.Metrics.Height + 10;
            var tabbedPlugin = entry.Plugin as ITabbedSettingsPlugin;
            if (tabbedPlugin?.SettingsTabs?.Count > 0) { cy += DrawSettingsTabs(cx, cy, cw, tabbedPlugin) + 6; }
            BorderSubtle.DrawLine(cx, cy, cx + cw, cy); cy += 10;
            float settingsHeight = Math.Max(80, (y + height) - cy - pad - 44);
            var settingsRect = new RectangleF(cx, cy, cw, settingsHeight);
            if (tabbedPlugin != null && !string.IsNullOrEmpty(_activeSettingsTab)) tabbedPlugin.DrawTabSettings(Hud, settingsRect, _clickAreas, _settingsScrollOffset, _activeSettingsTab);
            else entry.Plugin.DrawSettings(Hud, settingsRect, _clickAreas, _settingsScrollOffset);
            cy = y + height - pad - 32;
            var toggleLabelLayout = (entry.IsEnabled ? FontSuccess : FontError).GetTextLayout(entry.IsEnabled ? "✓ Enabled" : "✗ Disabled");
            (entry.IsEnabled ? FontSuccess : FontError).DrawText(toggleLabelLayout, cx, cy + 6);
            float toggleW = 44, toggleH = 22, tx = cx + cw - toggleW, ty = cy;
            var bigToggleRect = new RectangleF(tx, ty, toggleW, toggleH); _clickAreas["settings_main_toggle"] = bigToggleRect;
            ToggleTrack.DrawRectangle(tx, ty, toggleW, toggleH);
            float knobSize = toggleH - 4, knobX = entry.IsEnabled ? tx + toggleW - knobSize - 2 : tx + 2;
            (entry.IsEnabled ? ToggleOn : ToggleOff).DrawRectangle(knobX, ty + 2, knobSize, knobSize);
        }

        private float DrawSettingsTabs(float x, float y, float width, ITabbedSettingsPlugin plugin)
        {
            var tabs = plugin.SettingsTabs; if (tabs == null || tabs.Count == 0) return 0;
            float tabH = 26, gap = 3, tabW = Math.Min(68, (width - (tabs.Count - 1) * gap) / tabs.Count);
            if (string.IsNullOrEmpty(_activeSettingsTab)) _activeSettingsTab = tabs[0].Id;
            float currentX = x;
            for (int i = 0; i < tabs.Count; i++)
            {
                var tab = tabs[i]; var rect = new RectangleF(currentX, y, tabW, tabH);
                _clickAreas[$"settingstab_{tab.Id}"] = rect;
                bool active = _activeSettingsTab == tab.Id, hover = IsMouseOver(rect);
                (active ? TabActive : (hover ? TabHover : TabInactive)).DrawRectangle(rect);
                if (active) BorderAccent.DrawLine(rect.X, rect.Y + tabH - 1, rect.X + tabW, rect.Y + tabH - 1);
                string label = !string.IsNullOrEmpty(tab.Icon) ? tab.Icon : (tab.Name.Length > 5 ? tab.Name.Substring(0, 4) + ".." : tab.Name);
                var layout = FontSmall.GetTextLayout(label);
                (active ? FontBody : FontMuted).DrawText(layout, rect.X + (tabW - layout.Metrics.Width) / 2, rect.Y + (tabH - layout.Metrics.Height) / 2);
                currentX += tabW + gap;
            }
            return tabH;
        }

        private void DrawDebugOverlay()
        {
            float x = Hud.Window.Size.Width - 200, y = 10, w = 190, h = 180;
            SurfaceBase.DrawRectangle(x, y, w, h); BorderAccent.DrawRectangle(x, y, w, h);
            float pad = 10, cx = x + pad, cy = y + pad;
            var title = FontHeader.GetTextLayout("🔬 Debug"); FontHeader.DrawText(title, cx, cy); cy += title.Metrics.Height + 8;
            DrawDebugLine(ref cy, cx, "Plugins", RegisteredPlugins.Count.ToString());
            DrawDebugLine(ref cy, cx, "Active", GetEnabledCount().ToString());
            DrawDebugLine(ref cy, cx, "Monsters", Hud.Game.AliveMonsters.Count().ToString());
            DrawDebugLine(ref cy, cx, "Items", Hud.Game.Items.Count().ToString());
            var proc = System.Diagnostics.Process.GetCurrentProcess();
            DrawDebugLine(ref cy, cx, "Memory", $"{proc.WorkingSet64 / 1024 / 1024}MB");
            cy += 8; var hint = FontMicro.GetTextLayout("[F11]"); FontMuted.DrawText(hint, cx, cy);
        }

        private void DrawDebugLine(ref float y, float x, string label, string value)
        {
            var labelLayout = FontSmall.GetTextLayout($"{label}:"); var valueLayout = FontMono.GetTextLayout(value);
            FontMuted.DrawText(labelLayout, x, y); FontMono.DrawText(valueLayout, x + 70, y);
            y += Math.Max(labelLayout.Metrics.Height, valueLayout.Metrics.Height) + 2;
        }

        private void DrawStatusMessage(float x, float y, float w)
        {
            if (_statusTimer.IsRunning && _statusTimer.ElapsedMilliseconds < 2500) { var font = GetStatusFont(_statusType); var layout = font.GetTextLayout(_statusMessage ?? ""); font.DrawText(layout, x, y); }
            else { var hint = FontMuted.GetTextLayout("Click plugin for settings"); FontMuted.DrawText(hint, x, y); }
        }

        private void DrawButton(RectangleF rect, string text, bool hovered, bool disabled = false)
        {
            (disabled ? BtnDisabled : (hovered ? BtnHover : BtnDefault)).DrawRectangle(rect);
            var layout = FontSmall.GetTextLayout(text); FontSmall.DrawText(layout, rect.X + (rect.Width - layout.Metrics.Width) / 2, rect.Y + (rect.Height - layout.Metrics.Height) / 2);
        }

        private void DrawIconButton(RectangleF rect, string icon, bool hovered)
        {
            (hovered ? BtnHover : BtnDefault).DrawRectangle(rect);
            var layout = FontSmall.GetTextLayout(icon); FontSmall.DrawText(layout, rect.X + (rect.Width - layout.Metrics.Width) / 2, rect.Y + (rect.Height - layout.Metrics.Height) / 2);
        }

        private List<PluginEntry> GetFilteredPlugins()
        {
            if (_activeCategory == "all") return RegisteredPlugins.Values.OrderByDescending(p => p.IsActive).ThenBy(p => p.Name).ToList();
            return RegisteredPlugins.Values.Where(p => p.Category.Equals(_activeCategory, StringComparison.OrdinalIgnoreCase)).OrderByDescending(p => p.IsActive).ThenBy(p => p.Name).ToList();
        }

        private bool IsMouseOver(RectangleF rect)
        {
            int mx = Hud.Window.CursorX, my = Hud.Window.CursorY;
            return mx >= rect.X && mx <= rect.X + rect.Width && my >= rect.Y && my <= rect.Y + rect.Height;
        }

        public IFont GetStatusFont(StatusType type) { switch (type) { case StatusType.Success: return FontSuccess; case StatusType.Warning: return FontWarning; case StatusType.Error: return FontError; default: return FontAccent; } }
        public IBrush GetStatusBrush(StatusType type) { switch (type) { case StatusType.Success: return StatusSuccess; case StatusType.Warning: return StatusWarning; case StatusType.Error: return StatusError; default: return StatusInfo; } }
        public void SetStatus(string msg, StatusType type) { _statusMessage = msg; _statusType = type; _statusTimer.Restart(); }
        public void Log(string message) => System.Diagnostics.Debug.WriteLine($"[Core] {message}");

        private void HandleMouseInput()
        {
            bool down = Hud.Input.IsKeyDown(Keys.LButton), clicked = !down && _wasMouseDown && _clickTimer.ElapsedMilliseconds > 150;
            _wasMouseDown = down; if (!clicked) return; _clickTimer.Restart();
            foreach (var kvp in _clickAreas) if (IsMouseOver(kvp.Value)) { HandleClick(kvp.Key); return; }
        }

        private void HandleClick(string id)
        {
            if (id == "close") { _showSettings = false; _selectedPluginId = null; _activeSettingsTab = null; return; }
            if (id == "close_settings") { _selectedPluginId = null; _activeSettingsTab = null; return; }
            if (id.StartsWith("cat_")) { _activeCategory = id.Substring(4); _scrollOffset = 0; return; }
            if (id == "enable_all") { EnableAllPlugins(); return; }
            if (id == "disable_all") { DisableAllPlugins(); return; }
            if (id.StartsWith("toggle_")) 
            { 
                var entry = GetPlugin(id.Substring(7));
                if (entry != null && entry.RequirementsMet)
                    TogglePlugin(id.Substring(7)); 
                else if (entry != null && !entry.RequirementsMet)
                    SetStatus($"{entry.Name} requires {entry.RequirementsText}", StatusType.Warning);
                return; 
            }
            if (id.StartsWith("settingstab_")) { _activeSettingsTab = id.Substring(12); _settingsScrollOffset = 0; return; }
            if (id.StartsWith("settings_") && !id.StartsWith("settings_main")) { _selectedPluginId = id.Substring(9); _settingsScrollOffset = 0; _activeSettingsTab = null; return; }
            if (id.StartsWith("plugin_")) { var entry = GetPlugin(id.Substring(7)); if (entry?.Plugin.HasSettings == true) { _selectedPluginId = id.Substring(7); _settingsScrollOffset = 0; _activeSettingsTab = null; } return; }
            if (id == "settings_main_toggle" && _selectedPluginId != null) 
            { 
                var entry = GetPlugin(_selectedPluginId);
                if (entry != null && entry.RequirementsMet)
                    TogglePlugin(_selectedPluginId);
                else if (entry != null && !entry.RequirementsMet)
                    SetStatus($"Requires {entry.RequirementsText}", StatusType.Warning);
                return; 
            }
            if (_selectedPluginId != null) GetPlugin(_selectedPluginId)?.Plugin.HandleSettingsClick(id);
        }
    }

    public enum StatusType { Info, Success, Warning, Error }

    public class PluginEntry
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }
        public string Category { get; set; }
        public string Icon { get; set; }
        public ICustomPlugin Plugin { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsActive => Plugin?.IsActive ?? IsEnabled;
        public string StatusText => Plugin?.StatusText ?? (IsActive ? "ON" : "OFF");
        
        /// <summary>
        /// Whether this plugin has character/build requirements
        /// </summary>
        public bool HasRequirements => Plugin?.RequiredHeroClass != null || !string.IsNullOrEmpty(Plugin?.RequiredBuild);
        
        /// <summary>
        /// Whether the requirements are currently met
        /// </summary>
        public bool RequirementsMet => Plugin?.RequirementsMet ?? true;
        
        /// <summary>
        /// Display string for requirements (e.g., "Necromancer • LoD Death Nova")
        /// </summary>
        public string RequirementsText
        {
            get
            {
                if (Plugin == null) return "";
                var parts = new List<string>();
                if (Plugin.RequiredHeroClass.HasValue)
                    parts.Add(Plugin.RequiredHeroClass.Value.ToString());
                if (!string.IsNullOrEmpty(Plugin.RequiredBuild))
                    parts.Add(Plugin.RequiredBuild);
                return string.Join(" • ", parts);
            }
        }
    }

    public class PluginCategory
    {
        public string Id { get; }
        public string Name { get; }
        public string Icon { get; }
        public string Description { get; }
        public PluginCategory(string id, string name, string icon, string description = "") { Id = id; Name = name; Icon = icon; Description = description; }
    }

    public interface ICustomPlugin
    {
        string PluginId { get; }
        string PluginName { get; }
        string PluginDescription { get; }
        string PluginVersion { get; }
        string PluginCategory { get; }
        string PluginIcon { get; }
        bool Enabled { get; set; }
        bool HasSettings { get; }
        bool IsActive { get; }
        string StatusText { get; }
        
        /// <summary>
        /// Required hero class for this plugin (null = any class)
        /// </summary>
        HeroClass? RequiredHeroClass { get; }
        
        /// <summary>
        /// Required build/set name (e.g., "LoD Death Nova", "Natalya's") for display
        /// </summary>
        string RequiredBuild { get; }
        
        /// <summary>
        /// Whether the plugin requirements are currently met (correct class, skills, etc.)
        /// </summary>
        bool RequirementsMet { get; }
        
        void DrawSettings(IController hud, RectangleF rect, Dictionary<string, RectangleF> clickAreas, int scrollOffset);
        void HandleSettingsClick(string clickId);
    }

    public interface ITabbedSettingsPlugin : ICustomPlugin
    {
        List<SettingsTab> SettingsTabs { get; }
        void DrawTabSettings(IController hud, RectangleF rect, Dictionary<string, RectangleF> clickAreas, int scrollOffset, string tabId);
    }

    public class SettingsTab
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public string Description { get; set; }
        public SettingsTab(string id, string name, string icon = "", string description = "") { Id = id; Name = name; Icon = icon; Description = description; }
    }
}
