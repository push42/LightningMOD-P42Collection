namespace Turbo.Plugins.Custom.Navigation
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using Newtonsoft.Json;
    using SharpDX.DirectInput;
    using Turbo.Plugins.Custom.Core;
    using Turbo.Plugins.Custom.Navigation.NavMesh;
    using Turbo.Plugins.Default;

    public class EnhancedNavigationPlugin : TabbedCustomPluginBase, IKeyEventHandler, IAfterCollectHandler, INewAreaHandler
    {
        public override string PluginId => "enhanced-navigation";
        public override string PluginName => "Navigation";
        public override string PluginDescription => "Auto-records paths for community database";
        public override string PluginVersion => "1.1.0";
        public override string PluginCategory => "utility";
        public override string PluginIcon => "🧭";
        public override bool HasSettings => true;

        public override List<SettingsTab> SettingsTabs => new List<SettingsTab>
        {
            new SettingsTab("general", "General", "⚙️", "General navigation settings"),
            new SettingsTab("recording", "Recording", "📍", "Auto-recording settings"),
            new SettingsTab("paths", "Paths", "🛤️", "View and manage saved paths"),
            new SettingsTab("visuals", "Visuals", "👁️", "Visual overlay options")
        };

        // Keys
        public IKeyEvent ToggleKey { get; set; }
        public IKeyEvent ManualWaypointKey { get; set; }
        
        // Core settings
        public bool IsActive { get; set; } = true;
        public bool AutoNavigate { get; set; } = false;
        
        // Auto-recording settings (ALWAYS ON when playing)
        public bool AutoRecordEnabled { get; set; } = true;
        public float RecordInterval { get; set; } = 0.3f; // Faster for better paths
        public float RecordDistanceThreshold { get; set; } = 8f; // Smaller for smoother paths
        public float AfkTimeoutSeconds { get; set; } = 5f; // Consider AFK after 5 seconds no movement
        public int MinWaypointsToSave { get; set; } = 10; // Don't save tiny paths
        
        // Visuals
        public bool ShowWaypoints { get; set; } = true;
        public bool ShowPath { get; set; } = true;
        public bool ShowPanel { get; set; } = false;
        public float WaypointSize { get; set; } = 2.5f;

        // Internal state
        private WaypointDatabase _database;
        private WaypointPath _currentPath;
        private List<Waypoint> _sessionWaypoints; // Current session recording
        private IWorldCoordinate _lastRecordedPos;
        private IWorldCoordinate _lastMovementPos;
        private IWatch _recordTimer;
        private IWatch _afkTimer;
        private IWatch _sessionTimer;
        private int _currentWaypointIndex;
        private string _currentActivity;
        private string _currentMapId;
        private string _sessionMapId;
        private string _statusText = "Idle";
        private uint _lastAreaSno;
        private uint _lastWorldId;
        private IWatch _mapCheckTimer;
        private bool _wasInTown;
        private bool _wasLoading;
        private bool _needsMapUpdate;
        private bool _isAfk;
        private int _totalSessionPoints;
        
        // Fonts and brushes
        private IFont _titleFont;
        private IFont _statusFont;
        private IFont _infoFont;
        private IBrush _panelBrush;
        private IBrush _borderBrush;
        private IBrush _waypointBrush;
        private IBrush _sessionBrush;
        private IBrush _pathBrush;

        public EnhancedNavigationPlugin() { Enabled = true; Order = 5000; }

        public override void Load(IController hud)
        {
            base.Load(hud);
            ToggleKey = Hud.Input.CreateKeyEvent(true, Key.N, false, false, false);
            ManualWaypointKey = Hud.Input.CreateKeyEvent(true, Key.N, true, false, false);
            
            _database = new WaypointDatabase { DataDirectory = Path.Combine(DataDirectory, "waypoints") };
            _database.Load();
            _sessionWaypoints = new List<Waypoint>();
            _recordTimer = Hud.Time.CreateAndStartWatch();
            _afkTimer = Hud.Time.CreateAndStartWatch();
            _sessionTimer = Hud.Time.CreateAndStartWatch();
            _mapCheckTimer = Hud.Time.CreateAndStartWatch();
            
            _titleFont = Hud.Render.CreateFont("tahoma", 7, 255, 100, 200, 255, true, false, 160, 0, 0, 0, true);
            _statusFont = Hud.Render.CreateFont("tahoma", 6.5f, 255, 255, 255, 255, true, false, 140, 0, 0, 0, true);
            _infoFont = Hud.Render.CreateFont("tahoma", 6, 200, 180, 180, 180, false, false, 120, 0, 0, 0, true);
            _panelBrush = Hud.Render.CreateBrush(220, 15, 15, 25, 0);
            _borderBrush = Hud.Render.CreateBrush(180, 60, 60, 80, 1f);
            _waypointBrush = Hud.Render.CreateBrush(180, 100, 200, 255, 2f);
            _sessionBrush = Hud.Render.CreateBrush(200, 255, 180, 80, 1.5f); // Yellow for session
            _pathBrush = Hud.Render.CreateBrush(160, 100, 255, 150, 1.5f);
        }

        public override void DrawSettings(IController hud, RectangleF rect, Dictionary<string, RectangleF> clickAreas, int scrollOffset)
        {
            DrawTabSettings(hud, rect, clickAreas, scrollOffset, "general");
        }

        public override void DrawTabSettings(IController hud, RectangleF rect, Dictionary<string, RectangleF> clickAreas, int scrollOffset, string tabId)
        {
            float x = rect.X, y = rect.Y, w = rect.Width;
            switch (tabId)
            {
                case "general":
                    DrawGeneralTab(x, ref y, w, clickAreas);
                    break;
                case "recording":
                    DrawRecordingTab(x, ref y, w, clickAreas);
                    break;
                case "paths":
                    DrawPathsTab(x, ref y, w, clickAreas);
                    break;
                case "visuals":
                    DrawVisualsTab(x, ref y, w, clickAreas);
                    break;
            }
        }

        private void DrawGeneralTab(float x, ref float y, float w, Dictionary<string, RectangleF> clickAreas)
        {
            // Status header
            string status = _isAfk ? "⏸️ AFK" : (AutoRecordEnabled ? "🔴 RECORDING" : "⏹️ PAUSED");
            var statusFont = _isAfk ? (HasCore ? Core.FontWarning : _statusFont) : 
                            (AutoRecordEnabled ? (HasCore ? Core.FontSuccess : _statusFont) : (HasCore ? Core.FontMuted : _infoFont));
            var statusLayout = statusFont.GetTextLayout(status);
            statusFont.DrawText(statusLayout, x, y);
            
            // Session stats on same line
            string sessionInfo = $" | Session: {_totalSessionPoints} pts";
            var sessionFont = HasCore ? Core.FontMuted : _infoFont;
            var sessionLayout = sessionFont.GetTextLayout(sessionInfo);
            sessionFont.DrawText(sessionLayout, x + statusLayout.Metrics.Width, y);
            y += statusLayout.Metrics.Height + 10;
            
            y += DrawSettingsHeader(x, y, "Navigation"); y += 6;
            y += DrawToggleSetting(x, y, w, "Enable Navigation", IsActive, clickAreas, "nav_active");
            y += DrawToggleSetting(x, y, w, "Auto-Navigate Paths", AutoNavigate, clickAreas, "nav_autonav");
            
            y += 12;
            y += DrawSettingsHeader(x, y, "Current Location"); y += 6;
            
            string activityDisplay = (_currentActivity ?? "") == "greater-rift" ? "⚡ Greater Rift" :
                                    (_currentActivity ?? "") == "nephalem-rift" ? "🌀 Nephalem Rift" : "🗺️ Adventure";
            var activityFont = (_currentActivity ?? "").Contains("rift") ? (HasCore ? Core.FontAccent : _statusFont) : (HasCore ? Core.FontMuted : _infoFont);
            var activityLayout = activityFont.GetTextLayout(activityDisplay);
            activityFont.DrawText(activityLayout, x, y);
            y += activityLayout.Metrics.Height + 4;
            
            y += DrawSettingsHint(x, y, $"Map: {_statusText}");
            
            int pathCount = _database?.GetPathsForMap(_currentMapId)?.Count ?? 0;
            int totalPaths = _database?.GetAllPaths().Count ?? 0;
            int totalWaypoints = _database?.GetTotalWaypointCount() ?? 0;
            y += DrawSettingsHint(x, y, $"Paths: {pathCount} local, {totalPaths} total");
            y += DrawSettingsHint(x, y, $"Database: {totalWaypoints} waypoints");
            
            if (AutoNavigate && _currentPath != null)
            {
                y += 8;
                var okFont = HasCore ? Core.FontSuccess : _statusFont;
                var okLayout = okFont.GetTextLayout($"✓ Following: {_currentPath.Name}");
                okFont.DrawText(okLayout, x, y);
                y += okLayout.Metrics.Height;
            }
            
            y += 8;
            y += DrawSettingsHint(x, y, "[N] Toggle Nav • [Ctrl+N] Manual Point");
        }

        private void DrawRecordingTab(float x, ref float y, float w, Dictionary<string, RectangleF> clickAreas)
        {
            // Recording status
            string recStatus = AutoRecordEnabled ? "🔴 AUTO-RECORDING ACTIVE" : "⏹️ Recording Paused";
            var recFont = AutoRecordEnabled ? (HasCore ? Core.FontSuccess : _statusFont) : (HasCore ? Core.FontMuted : _infoFont);
            var recLayout = recFont.GetTextLayout(recStatus);
            recFont.DrawText(recLayout, x, y);
            y += recLayout.Metrics.Height + 8;
            
            y += DrawSettingsHeader(x, y, "Auto-Recording"); y += 6;
            y += DrawToggleSetting(x, y, w, "Auto-Record Paths", AutoRecordEnabled, clickAreas, "nav_autorecord");
            y += DrawSettingsHint(x, y, "Records while playing (not in town)");
            
            y += 10;
            y += DrawSettingsHeader(x, y, "Recording Quality"); y += 6;
            y += DrawSelectorSetting(x, y, w, "Interval", $"{RecordInterval:F1}s", clickAreas, "sel_interval");
            y += DrawSelectorSetting(x, y, w, "Distance", $"{RecordDistanceThreshold:F0}", clickAreas, "sel_distance");
            y += DrawSelectorSetting(x, y, w, "AFK Timeout", $"{AfkTimeoutSeconds:F0}s", clickAreas, "sel_afk");
            y += DrawSelectorSetting(x, y, w, "Min Points", $"{MinWaypointsToSave}", clickAreas, "sel_minpts");
            
            y += 10;
            y += DrawSettingsHeader(x, y, "Current Session"); y += 6;
            y += DrawSettingsHint(x, y, $"Recording for: {_sessionMapId ?? "None"}");
            y += DrawSettingsHint(x, y, $"Points this session: {_sessionWaypoints.Count}");
            y += DrawSettingsHint(x, y, $"Total session points: {_totalSessionPoints}");
            y += DrawSettingsHint(x, y, $"AFK Status: {(_isAfk ? "Yes" : "No")}");
            
            if (_sessionWaypoints.Count >= MinWaypointsToSave)
            {
                y += 8;
                y += DrawButtonSetting(x, y, w, "Save Now", "Save Session", clickAreas, "btn_save_session", primary: true);
            }
            
            if (_sessionWaypoints.Count > 0)
            {
                y += DrawButtonSetting(x, y, w, "Discard", "Discard Session", clickAreas, "btn_discard_session", danger: true);
            }
        }

        private void DrawPathsTab(float x, ref float y, float w, Dictionary<string, RectangleF> clickAreas)
        {
            y += DrawSettingsHeader(x, y, "Database Statistics"); y += 6;
            
            int totalPaths = _database?.GetAllPaths().Count ?? 0;
            int totalWaypoints = _database?.GetTotalWaypointCount() ?? 0;
            int uniqueMaps = _database?.GetUniqueMapsCount() ?? 0;
            
            y += DrawSettingsHint(x, y, $"Total Paths: {totalPaths}");
            y += DrawSettingsHint(x, y, $"Total Waypoints: {totalWaypoints}");
            y += DrawSettingsHint(x, y, $"Unique Maps: {uniqueMaps}");
            
            y += 10;
            y += DrawSettingsHeader(x, y, "Current Map Paths"); y += 6;
            
            var paths = _database?.GetPathsForMap(_currentMapId);
            if (paths != null && paths.Count > 0)
            {
                foreach (var path in paths.Take(5))
                {
                    string pathInfo = $"• {path.Name} ({path.Waypoints.Count} pts)";
                    y += DrawSettingsHint(x, y, pathInfo);
                }
                if (paths.Count > 5)
                    y += DrawSettingsHint(x, y, $"  ... and {paths.Count - 5} more");
            }
            else
            {
                y += DrawSettingsHint(x, y, "No paths for this map yet");
            }
            
            y += 10;
            y += DrawSettingsHeader(x, y, "Management"); y += 6;
            y += DrawButtonSetting(x, y, w, "Export", "Export All", clickAreas, "btn_export", primary: true);
            y += DrawButtonSetting(x, y, w, "Import", "Import", clickAreas, "btn_import");
            y += DrawButtonSetting(x, y, w, "Clear Map", "Clear This Map", clickAreas, "btn_clear_map", danger: true);
        }

        private void DrawVisualsTab(float x, ref float y, float w, Dictionary<string, RectangleF> clickAreas)
        {
            y += DrawSettingsHeader(x, y, "Overlays"); y += 6;
            y += DrawToggleSetting(x, y, w, "Show Saved Waypoints", ShowWaypoints, clickAreas, "nav_waypoints");
            y += DrawToggleSetting(x, y, w, "Show Current Path", ShowPath, clickAreas, "nav_path");
            y += DrawToggleSetting(x, y, w, "Show Mini Panel", ShowPanel, clickAreas, "nav_panel");
            
            y += 10;
            y += DrawSettingsHeader(x, y, "Waypoint Size"); y += 6;
            y += DrawSelectorSetting(x, y, w, "Size", $"{WaypointSize:F1}", clickAreas, "sel_size");
            
            y += 8;
            y += DrawSettingsHint(x, y, "Blue = Saved paths");
            y += DrawSettingsHint(x, y, "Yellow = Current session");
        }

        public override void HandleSettingsClick(string clickId)
        {
            switch (clickId)
            {
                case "nav_active": IsActive = !IsActive; break;
                case "nav_autonav": 
                    AutoNavigate = !AutoNavigate;
                    if (AutoNavigate) RefreshCurrentPath();
                    break;
                case "nav_autorecord": AutoRecordEnabled = !AutoRecordEnabled; break;
                case "nav_waypoints": ShowWaypoints = !ShowWaypoints; break;
                case "nav_path": ShowPath = !ShowPath; break;
                case "nav_panel": ShowPanel = !ShowPanel; break;
                
                case "sel_interval_prev": RecordInterval = Math.Max(0.1f, RecordInterval - 0.1f); break;
                case "sel_interval_next": RecordInterval = Math.Min(2.0f, RecordInterval + 0.1f); break;
                case "sel_distance_prev": RecordDistanceThreshold = Math.Max(2f, RecordDistanceThreshold - 2f); break;
                case "sel_distance_next": RecordDistanceThreshold = Math.Min(30f, RecordDistanceThreshold + 2f); break;
                case "sel_afk_prev": AfkTimeoutSeconds = Math.Max(2f, AfkTimeoutSeconds - 1f); break;
                case "sel_afk_next": AfkTimeoutSeconds = Math.Min(30f, AfkTimeoutSeconds + 1f); break;
                case "sel_minpts_prev": MinWaypointsToSave = Math.Max(5, MinWaypointsToSave - 5); break;
                case "sel_minpts_next": MinWaypointsToSave = Math.Min(100, MinWaypointsToSave + 5); break;
                case "sel_size_prev": WaypointSize = Math.Max(1f, WaypointSize - 0.5f); break;
                case "sel_size_next": WaypointSize = Math.Min(5f, WaypointSize + 0.5f); break;
                
                case "btn_save_session": SaveSessionPath(); break;
                case "btn_discard_session": DiscardSession(); break;
                case "btn_export": ExportPaths(); break;
                case "btn_import": ImportPaths(); break;
                case "btn_clear_map": ClearCurrentMapPaths(); break;
            }
            SavePluginSettings();
        }

        public void OnKeyEvent(IKeyEvent keyEvent)
        {
            if (!Hud.Game.IsInGame || !Enabled) return;
            
            if (ToggleKey.Matches(keyEvent) && keyEvent.IsPressed)
            {
                IsActive = !IsActive;
                SetCoreStatus($"Navigation {(IsActive ? "ON" : "OFF")}", IsActive ? StatusType.Success : StatusType.Warning);
            }
            
            if (ManualWaypointKey.Matches(keyEvent) && keyEvent.IsPressed)
            {
                AddManualWaypoint();
            }
        }

        public void OnNewArea(bool newGame, ISnoArea area)
        {
            if (!Enabled) return;
            
            // Save current session before changing maps
            if (_sessionWaypoints.Count >= MinWaypointsToSave)
            {
                SaveSessionPath();
            }
            
            _lastAreaSno = 0;
            _lastWorldId = 0;
            _needsMapUpdate = true;
            _sessionWaypoints.Clear();
            _sessionMapId = null;
        }

        public void AfterCollect()
        {
            if (!Hud.Game.IsInGame || !Enabled) return;
            
            CheckForMapChange();
            
            // Don't do anything in town
            if (Hud.Game.IsInTown) 
            { 
                _wasInTown = true;
                // Save session when entering town
                if (_sessionWaypoints.Count >= MinWaypointsToSave)
                {
                    SaveSessionPath();
                }
                return; 
            }
            
            if (Hud.Game.IsLoading) { _wasLoading = true; return; }
            
            if (_wasInTown || _wasLoading) 
            { 
                _wasInTown = false; 
                _wasLoading = false; 
                _needsMapUpdate = true;
                _sessionWaypoints.Clear();
                _lastRecordedPos = null;
                _lastMovementPos = null;
            }
            
            if (_needsMapUpdate) 
            { 
                _needsMapUpdate = false; 
                UpdateCurrentMap(); 
            }
            
            // Auto-recording logic
            if (AutoRecordEnabled && IsActive)
            {
                UpdateAutoRecording();
            }
            
            // Auto-navigation
            if (IsActive && AutoNavigate && _currentPath != null)
            {
                UpdateNavigation();
            }
        }

        private void CheckForMapChange()
        {
            if (_mapCheckTimer.ElapsedMilliseconds < 200) return;
            _mapCheckTimer.Restart();
            
            var me = Hud.Game.Me;
            if (me == null) return;
            
            bool mapChanged = false;
            uint currentWorldId = me.WorldId;
            if (_lastWorldId != 0 && _lastWorldId != currentWorldId) mapChanged = true;
            _lastWorldId = currentWorldId;
            
            var scene = me.Scene;
            if (scene != null)
            {
                uint currentAreaSno = scene.SnoArea?.Sno ?? 0;
                if (_lastAreaSno != 0 && _lastAreaSno != currentAreaSno) mapChanged = true;
                _lastAreaSno = currentAreaSno;
            }
            
            if (mapChanged) 
            { 
                // Save before map change
                if (_sessionWaypoints.Count >= MinWaypointsToSave)
                {
                    SaveSessionPath();
                }
                
                _needsMapUpdate = true; 
                _currentWaypointIndex = 0;
                _sessionWaypoints.Clear();
            }
        }

        private void UpdateCurrentMap()
        {
            var me = Hud.Game.Me;
            if (me == null) return;
            var scene = me.Scene;
            if (scene == null) return;
            
            var snoArea = scene.SnoArea;
            string areaName = snoArea?.NameLocalized ?? "Unknown";
            uint areaSno = snoArea?.Sno ?? 0;
            
            bool isGreaterRift = me.InGreaterRiftRank > 0;
            bool isNephalemRift = Hud.Game.RiftPercentage > 0 && !isGreaterRift;
            
            if (isGreaterRift)
            {
                _currentMapId = $"grift_{me.InGreaterRiftRank}";
                _currentActivity = "greater-rift";
                _statusText = $"GR{me.InGreaterRiftRank}";
            }
            else if (isNephalemRift)
            {
                _currentMapId = $"nrift_{me.WorldId}";
                _currentActivity = "nephalem-rift";
                _statusText = "Nephalem Rift";
            }
            else
            {
                _currentMapId = $"area_{areaSno}";
                _currentActivity = "general";
                _statusText = areaName.Length > 18 ? areaName.Substring(0, 15) + "..." : areaName;
            }
            
            _sessionMapId = _currentMapId;
            RefreshCurrentPath();
        }

        private void RefreshCurrentPath()
        {
            var paths = _database.GetPathsForMap(_currentMapId);
            // Get the best path (most waypoints = most coverage)
            _currentPath = paths?.OrderByDescending(p => p.Waypoints.Count).FirstOrDefault();
            _currentWaypointIndex = 0;
        }

        private void UpdateAutoRecording()
        {
            var me = Hud.Game.Me;
            if (me == null) return;
            
            var myPos = me.FloorCoordinate;
            
            // AFK detection - check if player has moved
            if (_lastMovementPos != null)
            {
                float moveDist = myPos.XYDistanceTo(_lastMovementPos);
                if (moveDist > 1f) // Any significant movement
                {
                    _afkTimer.Restart();
                    _isAfk = false;
                }
                else if (_afkTimer.ElapsedMilliseconds > (AfkTimeoutSeconds * 1000))
                {
                    _isAfk = true;
                }
            }
            _lastMovementPos = myPos;
            
            // Don't record if AFK
            if (_isAfk) return;
            
            // Check recording conditions
            bool timeElapsed = _recordTimer.ElapsedMilliseconds >= (RecordInterval * 1000);
            bool distanceMoved = _lastRecordedPos == null || myPos.XYDistanceTo(_lastRecordedPos) >= RecordDistanceThreshold;
            
            if (timeElapsed && distanceMoved)
            {
                _sessionWaypoints.Add(new Waypoint 
                { 
                    X = myPos.X, 
                    Y = myPos.Y, 
                    Z = myPos.Z, 
                    Type = WaypointType.Auto, 
                    Timestamp = DateTime.UtcNow 
                });
                _lastRecordedPos = myPos;
                _recordTimer.Restart();
                _totalSessionPoints++;
                
                // Auto-save every 100 points to prevent data loss
                if (_sessionWaypoints.Count > 0 && _sessionWaypoints.Count % 100 == 0)
                {
                    SaveSessionPath();
                    SetCoreStatus($"Auto-saved {_sessionWaypoints.Count} waypoints", StatusType.Success);
                }
            }
        }

        private void UpdateNavigation()
        {
            if (_currentPath == null || _currentWaypointIndex >= _currentPath.Waypoints.Count) return;
            
            var target = _currentPath.Waypoints[_currentWaypointIndex];
            var myPos = Hud.Game.Me.FloorCoordinate;
            float dist = (float)Math.Sqrt(Math.Pow(myPos.X - target.X, 2) + Math.Pow(myPos.Y - target.Y, 2));
            
            if (dist < 5f)
            {
                _currentWaypointIndex++;
                if (_currentWaypointIndex >= _currentPath.Waypoints.Count) 
                { 
                    _currentPath = null; 
                }
            }
        }

        private void AddManualWaypoint()
        {
            var myPos = Hud.Game.Me.FloorCoordinate;
            _sessionWaypoints.Add(new Waypoint 
            { 
                X = myPos.X, 
                Y = myPos.Y, 
                Z = myPos.Z, 
                Type = WaypointType.Manual, 
                Timestamp = DateTime.UtcNow 
            });
            _lastRecordedPos = myPos;
            _totalSessionPoints++;
            SetCoreStatus($"Manual waypoint ({_sessionWaypoints.Count} session pts)", StatusType.Success);
        }

        private void SaveSessionPath()
        {
            if (_sessionWaypoints.Count < MinWaypointsToSave) return;
            if (string.IsNullOrEmpty(_sessionMapId)) return;
            
            var path = new WaypointPath 
            { 
                Id = Guid.NewGuid().ToString(), 
                MapId = _sessionMapId, 
                Activity = _currentActivity, 
                Name = $"{_statusText} {DateTime.Now:HH:mm:ss}", 
                Waypoints = new List<Waypoint>(_sessionWaypoints), 
                CreatedAt = DateTime.UtcNow 
            };
            
            _database.AddPath(path); 
            _database.Save();
            
            int savedCount = _sessionWaypoints.Count;
            _sessionWaypoints.Clear();
            _lastRecordedPos = null;
            
            RefreshCurrentPath();
            SetCoreStatus($"Saved {savedCount} pts to {_sessionMapId}", StatusType.Success);
        }

        private void DiscardSession()
        {
            int count = _sessionWaypoints.Count;
            _sessionWaypoints.Clear();
            _lastRecordedPos = null;
            SetCoreStatus($"Discarded {count} session points", StatusType.Warning);
        }

        private void ClearCurrentMapPaths()
        {
            if (string.IsNullOrEmpty(_currentMapId)) return;
            _database.ClearPathsForMap(_currentMapId);
            _database.Save();
            _currentPath = null;
            SetCoreStatus($"Cleared paths for {_currentMapId}", StatusType.Warning);
        }

        private void ExportPaths() 
        { 
            try
            {
                string exportDir = Path.Combine(_database.DataDirectory, "exports");
                if (!Directory.Exists(exportDir)) Directory.CreateDirectory(exportDir);
                string filename = $"paths_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                string filepath = Path.Combine(exportDir, filename);
                var allPaths = _database.GetAllPaths();
                File.WriteAllText(filepath, JsonConvert.SerializeObject(allPaths, Formatting.Indented));
                SetCoreStatus($"Exported {allPaths.Count} paths", StatusType.Success);
            }
            catch (Exception ex)
            {
                SetCoreStatus($"Export failed: {ex.Message}", StatusType.Error);
            }
        }
        
        private void ImportPaths() 
        { 
            SetCoreStatus("Place JSON files in imports folder", StatusType.Info); 
        }

        public override void PaintTopInGame(ClipState clipState)
        {
            base.PaintTopInGame(clipState);
            if (clipState != ClipState.BeforeClip || !Hud.Game.IsInGame || !Enabled || Hud.Game.IsInTown || !IsActive) return;
            
            // Draw saved path waypoints
            if (ShowWaypoints && _currentPath != null) 
            {
                foreach (var wp in _currentPath.Waypoints) 
                { 
                    var coord = Hud.Window.CreateWorldCoordinate(wp.X, wp.Y, wp.Z); 
                    if (coord.IsOnScreen()) _waypointBrush.DrawWorldEllipse(WaypointSize, -1, coord); 
                }
            }
            
            // Draw current session waypoints (in yellow)
            if (_sessionWaypoints.Count > 0)
            {
                // Only draw last 50 to not clutter screen
                var recentPoints = _sessionWaypoints.Skip(Math.Max(0, _sessionWaypoints.Count - 50));
                foreach (var wp in recentPoints) 
                { 
                    var coord = Hud.Window.CreateWorldCoordinate(wp.X, wp.Y, wp.Z); 
                    if (coord.IsOnScreen()) _sessionBrush.DrawWorldEllipse(WaypointSize * 0.7f, -1, coord); 
                }
            }
            
            if (ShowPanel) DrawStatusPanel();
        }

        private void DrawStatusPanel()
        {
            float w = 130, h = 36;
            float x = Hud.Window.Size.Width - w - 10;
            float y = Hud.Window.Size.Height - h - 140; // Above skill bar
            float pad = 4;
            
            _panelBrush.DrawRectangle(x, y, w, h); 
            _borderBrush.DrawRectangle(x, y, w, h);
            
            var accentBrush = _isAfk ? _borderBrush : (_sessionWaypoints.Count > 0 ? _sessionBrush : _waypointBrush);
            accentBrush.DrawRectangle(x, y, 2, h);
            
            float tx = x + pad + 2, ty = y + pad;
            string title = _isAfk ? "🧭 AFK" : $"🧭 {_sessionWaypoints.Count} pts";
            var titleLayout = _titleFont.GetTextLayout(title); 
            _titleFont.DrawText(titleLayout, tx, ty); 
            ty += titleLayout.Metrics.Height + 1;
            
            var hintLayout = _infoFont.GetTextLayout(_statusText);
            _infoFont.DrawText(hintLayout, tx, ty);
        }

        protected override object GetSettingsObject() => new NavSettings 
        { 
            IsActive = IsActive, 
            AutoNavigate = AutoNavigate,
            AutoRecordEnabled = AutoRecordEnabled,
            RecordInterval = RecordInterval,
            RecordDistanceThreshold = RecordDistanceThreshold,
            AfkTimeoutSeconds = AfkTimeoutSeconds,
            MinWaypointsToSave = MinWaypointsToSave,
            ShowWaypoints = ShowWaypoints, 
            ShowPath = ShowPath, 
            ShowPanel = ShowPanel, 
            WaypointSize = WaypointSize 
        };
        
        protected override void ApplySettingsObject(object settings) 
        { 
            if (settings is NavSettings s) 
            { 
                IsActive = s.IsActive; 
                AutoNavigate = s.AutoNavigate;
                AutoRecordEnabled = s.AutoRecordEnabled;
                RecordInterval = s.RecordInterval;
                RecordDistanceThreshold = s.RecordDistanceThreshold;
                AfkTimeoutSeconds = s.AfkTimeoutSeconds;
                MinWaypointsToSave = s.MinWaypointsToSave;
                ShowWaypoints = s.ShowWaypoints; 
                ShowPath = s.ShowPath; 
                ShowPanel = s.ShowPanel; 
                WaypointSize = s.WaypointSize; 
            } 
        }
        
        private class NavSettings : PluginSettingsBase 
        { 
            public bool IsActive { get; set; } = true;
            public bool AutoNavigate { get; set; }
            public bool AutoRecordEnabled { get; set; } = true;
            public float RecordInterval { get; set; } = 0.3f;
            public float RecordDistanceThreshold { get; set; } = 8f;
            public float AfkTimeoutSeconds { get; set; } = 5f;
            public int MinWaypointsToSave { get; set; } = 10;
            public bool ShowWaypoints { get; set; } = true;
            public bool ShowPath { get; set; } = true;
            public bool ShowPanel { get; set; }
            public float WaypointSize { get; set; } = 2.5f;
        }
        
        public WaypointDatabase GetDatabase() => _database;
    }

    public class WaypointDatabase
    {
        public string DataDirectory { get; set; }
        private Dictionary<string, List<WaypointPath>> _pathsByMap = new Dictionary<string, List<WaypointPath>>();
        private string DatabaseFile => Path.Combine(DataDirectory, "waypoint_db.json");
        
        public void Load() 
        { 
            try 
            { 
                if (!Directory.Exists(DataDirectory)) Directory.CreateDirectory(DataDirectory); 
                if (File.Exists(DatabaseFile)) 
                { 
                    var data = JsonConvert.DeserializeObject<WaypointDatabaseData>(File.ReadAllText(DatabaseFile)); 
                    if (data?.Paths != null) 
                    {
                        _pathsByMap.Clear();
                        foreach (var path in data.Paths) 
                        { 
                            if (!_pathsByMap.ContainsKey(path.MapId)) 
                                _pathsByMap[path.MapId] = new List<WaypointPath>(); 
                            _pathsByMap[path.MapId].Add(path); 
                        } 
                    }
                } 
            } 
            catch { } 
        }
        
        public void Save() 
        { 
            try 
            { 
                var data = new WaypointDatabaseData { Paths = GetAllPaths(), LastUpdated = DateTime.UtcNow }; 
                File.WriteAllText(DatabaseFile, JsonConvert.SerializeObject(data, Formatting.Indented)); 
            } 
            catch { } 
        }
        
        public void AddPath(WaypointPath path) 
        { 
            if (!_pathsByMap.ContainsKey(path.MapId)) 
                _pathsByMap[path.MapId] = new List<WaypointPath>(); 
            _pathsByMap[path.MapId].Add(path); // Don't replace, add - we want multiple paths!
        }
        
        public void ClearPathsForMap(string mapId)
        {
            if (_pathsByMap.ContainsKey(mapId))
                _pathsByMap[mapId].Clear();
        }
        
        public List<WaypointPath> GetPathsForMap(string mapId) => string.IsNullOrEmpty(mapId) ? null : _pathsByMap.TryGetValue(mapId, out var paths) ? paths : null;
        public List<WaypointPath> GetAllPaths() => _pathsByMap.Values.SelectMany(p => p).ToList();
        public int GetTotalWaypointCount() => _pathsByMap.Values.SelectMany(p => p).Sum(p => p.Waypoints.Count);
        public int GetUniqueMapsCount() => _pathsByMap.Count;
    }
    
    public class WaypointDatabaseData { public List<WaypointPath> Paths { get; set; } = new List<WaypointPath>(); public DateTime LastUpdated { get; set; } }
    public class WaypointPath { public string Id { get; set; } public string MapId { get; set; } public string Activity { get; set; } public string Name { get; set; } public string Author { get; set; } public List<Waypoint> Waypoints { get; set; } = new List<Waypoint>(); public DateTime CreatedAt { get; set; } public int UsageCount { get; set; } public float Rating { get; set; } }
    public class Waypoint { public float X { get; set; } public float Y { get; set; } public float Z { get; set; } public WaypointType Type { get; set; } public string Tag { get; set; } public DateTime Timestamp { get; set; } }
    public enum WaypointType { Auto, Manual, Objective, Hazard, Interest }
}
