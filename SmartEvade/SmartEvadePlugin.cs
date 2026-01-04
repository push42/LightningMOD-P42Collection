namespace Turbo.Plugins.Custom.SmartEvade
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;
    using SharpDX.DirectInput;
    using Turbo.Plugins.Default;
    using Turbo.Plugins.Custom.Core;
    using Turbo.Plugins.Custom.Navigation;
    using Turbo.Plugins.Custom.Navigation.NavMesh;

    /// <summary>
    /// Smart Evade v3.1 - Core Integrated God-Tier Auto-Evade
    /// 
    /// NOW INTEGRATED WITH CORE PLUGIN FRAMEWORK!
    /// - Settings panel in F10 hub
    /// - Centralized enable/disable
    /// - Shared design system
    /// 
    /// MAJOR FEATURES:
    /// - Full integration with Navigation system
    /// - Uses SpatialAwarenessEngine for threat detection
    /// - A* pathfinding for escape routes
    /// - NavMesh-based walkability checking
    /// - Monster movement prediction
    /// - Safe zone identification
    /// 
    /// PRIVATE VERSION - Not for sharing
    /// </summary>
    public class SmartEvadePlugin : CustomPluginBase, IKeyEventHandler, IInGameTopPainter, IAfterCollectHandler
    {
        #region Plugin Metadata

        public override string PluginId => "smart-evade";
        public override string PluginName => "Smart Evade";
        public override string PluginDescription => "God-tier auto-evade with navigation integration";
        public override string PluginVersion => "3.1.0";
        public override string PluginCategory => "combat";
        public override string PluginIcon => "🛡️";
        public override bool HasSettings => true;

        #endregion

        #region Runtime State (for Core sidebar)

        // Override to report our actual IsActive state to Core
        public override bool IsActive => _isActiveInternal;
        
        // Override to show detailed status in sidebar
        public override string StatusText => !_isActiveInternal ? "OFF" : (_isEvading ? _currentAction : "Ready");

        #endregion

        #region Public Settings

        public IKeyEvent ToggleKey { get; set; }
        private bool _isActiveInternal;
        
        /// <summary>
        /// Set the active state (for customizer use)
        /// </summary>
        public void SetActive(bool active) => _isActiveInternal = active;
        
        // Distance settings
        public float SafeDistance { get; set; } = 15f;
        public float MinKiteDistance { get; set; } = 8f;
        public float MaxEnemyRange { get; set; } = 50f;
        public float DangerRadiusMultiplier { get; set; } = 1.3f;
        
        // Wall awareness settings
        public float WallDetectionRange { get; set; } = 12f;
        public float WallAvoidanceWeight { get; set; } = 3.0f;
        public float CornerDetectionAngle { get; set; } = 90f;
        public float MinWallClearance { get; set; } = 5f;
        
        // Movement settings
        public int EscapeDirections { get; set; } = 16;
        public float EscapeDistance { get; set; } = 10f;
        public float MovementSmoothing { get; set; } = 0.7f;
        public int ActionCooldownMs { get; set; } = 35;
        
        // Combat settings
        public float EvadeHealthThreshold { get; set; } = 100f;
        public bool PrioritizeGroundEffects { get; set; } = true;
        public bool EnablePredictivePathing { get; set; } = true;
        
        // Navigation integration (v3.0)
        public bool UseNavigationSystem { get; set; } = true;
        public bool UseNavMeshWalkability { get; set; } = true;
        public bool UseAStarForEscape { get; set; } = true;
        public bool UseMonsterPrediction { get; set; } = true;
        
        // Debug/UI settings
        public bool ShowDebugCircles { get; set; } = true;
        public bool ShowWallMarkers { get; set; } = true;
        public bool ShowEscapeRoutes { get; set; } = true;
        public bool ShowMovementIndicator { get; set; } = true;
        public bool ShowSafeZones { get; set; } = true;
        public bool ShowPredictedPaths { get; set; } = true;
        public bool ShowPanel { get; set; } = false;  // DISABLED - Core sidebar shows status
        public float PanelX { get; set; } = 0.005f;
        public float PanelY { get; set; } = 0.28f;

        #endregion

        #region Private Fields

        // Navigation system (v3.0)
        private SpatialAwarenessEngine _awareness;
        private NavMeshManager _navMesh;
        private AStarPathfinder _pathfinder;

        private Dictionary<ActorSnoEnum, DangerZone> _dangerZones;

        // Fallback UI elements (when Core not available)
        private IFont _titleFont;
        private IFont _statusFont;
        private IFont _infoFont;
        private IBrush _panelBrush;
        private IBrush _borderBrush;
        private IBrush _accentOnBrush;
        private IBrush _accentOffBrush;
        private IBrush _dangerBrush;
        private IBrush _dangerBrushHigh;
        private IBrush _dangerBrushMedium;
        private IBrush _safeBrush;
        private IBrush _safeZoneBrush;
        private IBrush _moveBrush;
        private IBrush _enemyBrush;
        private IBrush _wallBrush;
        private IBrush _escapeBrush;
        private IBrush _blockedBrush;
        private IBrush _predictionBrush;

        // State tracking
        private IWatch _lastMoveTime;
        private IWatch _stuckDetectionTimer;
        private IWorldCoordinate _lastTargetPos;
        private IWorldCoordinate _lastPlayerPos;
        private bool _isEvading;
        private string _currentAction = "Ready";
        private int _dangerCount;
        private int _enemyCount;
        private int _wallsNearby;
        private bool _isCornerTrapped;
        private int _safeZoneCount;
        private int _threatsMovingToward;
        
        // Escape route analysis
        private List<EscapeRoute> _escapeRoutes = new List<EscapeRoute>();
        private EscapeRoute _bestRoute;
        private Vector2D _smoothedDirection = new Vector2D { X = 0, Y = 0, Length = 0 };
        
        // A* escape path (v3.0)
        private PathResult _escapePath;
        private int _escapePathIndex;
        
        // Anti-stuck detection
        private Queue<IWorldCoordinate> _positionHistory = new Queue<IWorldCoordinate>();
        private const int POSITION_HISTORY_SIZE = 10;
        private int _stuckCounter = 0;
        private const int STUCK_THRESHOLD = 5;

        #endregion

        #region Initialization

        public SmartEvadePlugin()
        {
            Enabled = true;
            Order = 50000;
        }

        public override void Load(IController hud)
        {
            base.Load(hud);

            ToggleKey = Hud.Input.CreateKeyEvent(true, Key.J, false, false, true); // Shift+J
            _isActiveInternal = false;

            // Initialize Navigation system (v3.0)
            _awareness = new SpatialAwarenessEngine { Hud = hud };
            _navMesh = new NavMeshManager { Hud = hud };
            _navMesh.Initialize();
            _pathfinder = _awareness.Pathfinder;

            InitializeDangerZones();
            InitializeFallbackUI();

            _lastMoveTime = Hud.Time.CreateWatch();
            _stuckDetectionTimer = Hud.Time.CreateWatch();
            _stuckDetectionTimer.Start();

            Log("Smart Evade v3.1 loaded");
        }

        private void InitializeFallbackUI()
        {
            // Fallback fonts (used when Core not available)
            _titleFont = Hud.Render.CreateFont("tahoma", 8, 255, 220, 180, 100, true, false, 180, 0, 0, 0, true);
            _statusFont = Hud.Render.CreateFont("tahoma", 7.5f, 255, 255, 255, 255, true, false, 160, 0, 0, 0, true);
            _infoFont = Hud.Render.CreateFont("tahoma", 7, 200, 180, 180, 180, false, false, 140, 0, 0, 0, true);
            
            _panelBrush = Hud.Render.CreateBrush(235, 15, 15, 25, 0);
            _borderBrush = Hud.Render.CreateBrush(200, 60, 60, 80, 1f);
            _accentOnBrush = Hud.Render.CreateBrush(255, 80, 200, 80, 0);
            _accentOffBrush = Hud.Render.CreateBrush(255, 200, 80, 80, 0);

            // World brushes
            _dangerBrush = Hud.Render.CreateBrush(120, 255, 0, 0, 2f);
            _dangerBrushHigh = Hud.Render.CreateBrush(100, 255, 100, 0, 2f);
            _dangerBrushMedium = Hud.Render.CreateBrush(80, 255, 200, 0, 1.5f);
            _safeBrush = Hud.Render.CreateBrush(80, 50, 255, 50, 2f);
            _safeZoneBrush = Hud.Render.CreateBrush(60, 0, 255, 100, 1.5f);
            _moveBrush = Hud.Render.CreateBrush(200, 255, 255, 0, 3f);
            _enemyBrush = Hud.Render.CreateBrush(60, 255, 100, 100, 1.5f);
            _wallBrush = Hud.Render.CreateBrush(150, 255, 150, 0, 2f);
            _escapeBrush = Hud.Render.CreateBrush(100, 0, 255, 150, 1.5f);
            _blockedBrush = Hud.Render.CreateBrush(100, 255, 0, 0, 1.5f);
            _predictionBrush = Hud.Render.CreateBrush(80, 255, 0, 255, 1.5f);
        }

        private void InitializeDangerZones()
        {
            _dangerZones = new Dictionary<ActorSnoEnum, DangerZone>
            {
                // High priority - instant death or massive damage
                { ActorSnoEnum._monsteraffix_frozen_iceclusters, new DangerZone(16f, DangerType.IceBall, 100) },
                { ActorSnoEnum._monsteraffix_molten_deathstart_proxy, new DangerZone(14f, DangerType.MoltenExplosion, 100) },
                { ActorSnoEnum._monsteraffix_molten_deathexplosion_proxy, new DangerZone(14f, DangerType.Molten, 90) },
                { ActorSnoEnum._monsteraffix_molten_firering, new DangerZone(14f, DangerType.Molten, 85) },
                { ActorSnoEnum._monsteraffix_arcaneenchanted_petsweep, new DangerZone(42f, DangerType.Arcane, 95) },
                { ActorSnoEnum._monsteraffix_arcaneenchanted_petsweep_reverse, new DangerZone(42f, DangerType.Arcane, 95) },
                { ActorSnoEnum._arcaneenchanteddummy_spawn, new DangerZone(8f, DangerType.ArcaneSpawn, 80) },
                
                // Medium priority
                { ActorSnoEnum._monsteraffix_desecrator_damage_aoe, new DangerZone(9f, DangerType.Desecrator, 75) },
                { ActorSnoEnum._x1_monsteraffix_thunderstorm_impact, new DangerZone(18f, DangerType.Thunderstorm, 70) },
                { ActorSnoEnum._monsteraffix_plagued_endcloud, new DangerZone(14f, DangerType.Plagued, 60) },
                { ActorSnoEnum._creepmobarm, new DangerZone(14f, DangerType.Plagued, 60) },
                { ActorSnoEnum._x1_monsteraffix_frozenpulse_monster, new DangerZone(16f, DangerType.FrozenPulse, 65) },
                { ActorSnoEnum._gluttony_gascloud_proxy, new DangerZone(22f, DangerType.GhomGas, 55) },
                
                // Lower priority
                { ActorSnoEnum._x1_monsteraffix_teleportmines, new DangerZone(5f, DangerType.Wormhole, 40) },
                { ActorSnoEnum._x1_monsteraffix_corpsebomber_projectile, new DangerZone(6f, DangerType.PoisonEnchanted, 35) },
                { ActorSnoEnum._x1_monsteraffix_orbiter_projectile, new DangerZone(4f, DangerType.Orbiter, 30) },
            };
        }

        #endregion

        #region Settings Panel

        public override void DrawSettings(IController hud, RectangleF rect, Dictionary<string, RectangleF> clickAreas, int scrollOffset)
        {
            float x = rect.X, y = rect.Y, w = rect.Width;

            // Status indicator
            string statusText = _isActiveInternal ? (_isEvading ? "● EVADING" : "● READY") : "○ OFF";
            var statusFont = _isActiveInternal ? (HasCore ? Core.FontSuccess : _statusFont) : (HasCore ? Core.FontError : _statusFont);
            var statusLayout = statusFont.GetTextLayout(statusText);
            statusFont.DrawText(statusLayout, x, y);
            y += statusLayout.Metrics.Height + 10;

            // Navigation section
            y += DrawSettingsHeader(x, y, "Navigation System");
            y += 8;

            y += DrawToggleSetting(x, y, w, "Use Navigation", UseNavigationSystem, clickAreas, "toggle_nav");
            y += DrawToggleSetting(x, y, w, "NavMesh Walkability", UseNavMeshWalkability, clickAreas, "toggle_navmesh");
            y += DrawToggleSetting(x, y, w, "A* Escape Paths", UseAStarForEscape, clickAreas, "toggle_astar");
            y += DrawToggleSetting(x, y, w, "Monster Prediction", UseMonsterPrediction, clickAreas, "toggle_predict");

            y += 12;

            // Distance section
            y += DrawSettingsHeader(x, y, "Distances");
            y += 8;

            y += DrawSelectorSetting(x, y, w, "Safe Distance", SafeDistance.ToString("F0"), clickAreas, "sel_safe");
            y += DrawSelectorSetting(x, y, w, "Escape Distance", EscapeDistance.ToString("F0"), clickAreas, "sel_escape");
            y += DrawSelectorSetting(x, y, w, "Wall Detection", WallDetectionRange.ToString("F0"), clickAreas, "sel_wall");

            y += 12;

            // Debug section
            y += DrawSettingsHeader(x, y, "Debug Visuals");
            y += 8;

            y += DrawToggleSetting(x, y, w, "Show Danger Circles", ShowDebugCircles, clickAreas, "toggle_circles");
            y += DrawToggleSetting(x, y, w, "Show Escape Routes", ShowEscapeRoutes, clickAreas, "toggle_routes");
            y += DrawToggleSetting(x, y, w, "Show Safe Zones", ShowSafeZones, clickAreas, "toggle_zones");
            y += DrawToggleSetting(x, y, w, "Show Panel", ShowPanel, clickAreas, "toggle_panel");

            y += 16;
            y += DrawSettingsHint(x, y, "[Shift+J] Toggle");
        }

        public override void HandleSettingsClick(string clickId)
        {
            switch (clickId)
            {
                case "toggle_nav": UseNavigationSystem = !UseNavigationSystem; break;
                case "toggle_navmesh": UseNavMeshWalkability = !UseNavMeshWalkability; break;
                case "toggle_astar": UseAStarForEscape = !UseAStarForEscape; break;
                case "toggle_predict": UseMonsterPrediction = !UseMonsterPrediction; break;
                case "toggle_circles": ShowDebugCircles = !ShowDebugCircles; break;
                case "toggle_routes": ShowEscapeRoutes = !ShowEscapeRoutes; break;
                case "toggle_zones": ShowSafeZones = !ShowSafeZones; break;
                case "toggle_panel": ShowPanel = !ShowPanel; break;
                case "sel_safe_prev": SafeDistance = Math.Max(5, SafeDistance - 2); break;
                case "sel_safe_next": SafeDistance = Math.Min(30, SafeDistance + 2); break;
                case "sel_escape_prev": EscapeDistance = Math.Max(5, EscapeDistance - 2); break;
                case "sel_escape_next": EscapeDistance = Math.Min(25, EscapeDistance + 2); break;
                case "sel_wall_prev": WallDetectionRange = Math.Max(5, WallDetectionRange - 2); break;
                case "sel_wall_next": WallDetectionRange = Math.Min(25, WallDetectionRange + 2); break;
            }
            SavePluginSettings();
        }

        protected override object GetSettingsObject() => new EvadeSettings
        {
            IsActive = this._isActiveInternal,
            UseNavigationSystem = this.UseNavigationSystem,
            UseNavMeshWalkability = this.UseNavMeshWalkability,
            UseAStarForEscape = this.UseAStarForEscape,
            UseMonsterPrediction = this.UseMonsterPrediction,
            SafeDistance = this.SafeDistance,
            EscapeDistance = this.EscapeDistance,
            WallDetectionRange = this.WallDetectionRange,
            ShowDebugCircles = this.ShowDebugCircles,
            ShowEscapeRoutes = this.ShowEscapeRoutes,
            ShowSafeZones = this.ShowSafeZones,
            ShowPanel = this.ShowPanel
        };

        protected override void ApplySettingsObject(object settings)
        {
            if (settings is EvadeSettings s)
            {
                _isActiveInternal = s.IsActive;
                UseNavigationSystem = s.UseNavigationSystem;
                UseNavMeshWalkability = s.UseNavMeshWalkability;
                UseAStarForEscape = s.UseAStarForEscape;
                UseMonsterPrediction = s.UseMonsterPrediction;
                SafeDistance = s.SafeDistance;
                EscapeDistance = s.EscapeDistance;
                WallDetectionRange = s.WallDetectionRange;
                ShowDebugCircles = s.ShowDebugCircles;
                ShowEscapeRoutes = s.ShowEscapeRoutes;
                ShowSafeZones = s.ShowSafeZones;
                ShowPanel = s.ShowPanel;
            }
        }

        private class EvadeSettings : PluginSettingsBase
        {
            public bool IsActive { get; set; }
            public bool UseNavigationSystem { get; set; }
            public bool UseNavMeshWalkability { get; set; }
            public bool UseAStarForEscape { get; set; }
            public bool UseMonsterPrediction { get; set; }
            public float SafeDistance { get; set; }
            public float EscapeDistance { get; set; }
            public float WallDetectionRange { get; set; }
            public bool ShowDebugCircles { get; set; }
            public bool ShowEscapeRoutes { get; set; }
            public bool ShowSafeZones { get; set; }
            public bool ShowPanel { get; set; }
        }

        #endregion

        #region Key Handler

        public void OnKeyEvent(IKeyEvent keyEvent)
        {
            if (!Hud.Game.IsInGame) return;
            if (!Enabled) return;

            if (ToggleKey.Matches(keyEvent) && keyEvent.IsPressed)
            {
                _isActiveInternal = !_isActiveInternal;
                _currentAction = _isActiveInternal ? "Ready" : "OFF";
                _stuckCounter = 0;
                _positionHistory.Clear();
                _escapePath = null;
                
                SetCoreStatus($"Smart Evade {(_isActiveInternal ? "ON" : "OFF")}", 
                             _isActiveInternal ? StatusType.Success : StatusType.Warning);
            }
        }

        #endregion

        #region Main Logic

        public void AfterCollect()
        {
            if (!Hud.Game.IsInGame) return;
            if (!Enabled) return;
            if (!_isActiveInternal) return;
            if (Hud.Game.IsInTown) return;
            if (!Hud.Window.IsForeground) return;
            if (Hud.Game.Me.IsDead) return;

            var myPos = Hud.Game.Me.FloorCoordinate;

            // Update Navigation system ONLY when needed (lazy update)
            if (UseNavigationSystem && _awareness != null)
            {
                _awareness.Update();
                // Don't update NavMesh every frame - it's expensive
            }

            // Track position history for stuck detection
            UpdatePositionHistory(myPos);

            // === KEY FIX: Only evade when actually in danger! ===
            // Check if we're actually in danger before doing anything
            bool inImmediateDanger = CheckImmediateDanger(myPos);
            
            if (!inImmediateDanger)
            {
                // NOT IN DANGER - do nothing, let player control
                _isEvading = false;
                _currentAction = "Ready";
                _escapePath = null;
                return;
            }

            // Health threshold check (optional extra condition)
            // Only evade if health is below threshold OR always evade dangers
            if (EvadeHealthThreshold < 100f && Hud.Game.Me.Defense.HealthPct > EvadeHealthThreshold)
            {
                _isEvading = false;
                _currentAction = "HP OK";
                return;
            }

            // WE ARE IN DANGER - now evade
            _isEvading = true;

            if (UseNavigationSystem)
            {
                PerformNavigationBasedEvade(myPos);
            }
            else
            {
                PerformLegacyEvade(myPos);
            }
        }

        /// <summary>
        /// Check if player is in IMMEDIATE danger that requires evasion
        /// This is the key check - only evade when truly necessary
        /// </summary>
        private bool CheckImmediateDanger(IWorldCoordinate myPos)
        {
            // Check ground effects / danger zones
            foreach (var actor in Hud.Game.Actors)
            {
                if (_dangerZones.TryGetValue(actor.SnoActor.Sno, out var zone))
                {
                    float distance = myPos.XYDistanceTo(actor.FloorCoordinate);
                    float effectiveRadius = zone.Radius * DangerRadiusMultiplier;
                    
                    // Only trigger if actually INSIDE or very close to danger
                    if (distance < effectiveRadius * 0.9f) // 90% of radius = inside danger
                    {
                        return true;
                    }
                }
            }

            // Check avoidables
            foreach (var avoid in Hud.Game.Me.AvoidablesInRange)
            {
                float distance = myPos.XYDistanceTo(avoid.FloorCoordinate);
                float effectiveRadius = avoid.AvoidableDefinition.Radius * DangerRadiusMultiplier;
                
                if (distance < effectiveRadius * 0.9f)
                {
                    return true;
                }
            }

            // Check navigation awareness if enabled
            if (UseNavigationSystem && _awareness != null)
            {
                // Only trigger on immediate danger (actually inside danger zone)
                foreach (var danger in _awareness.DangerZones)
                {
                    if (danger.Distance < danger.Radius * 0.85f)
                    {
                        return true;
                    }
                }
            }

            // NOT in immediate danger
            return false;
        }

        /// <summary>
        /// v3.0 - Navigation-based evasion with full spatial awareness
        /// ONLY called when already confirmed in danger
        /// </summary>
        private void PerformNavigationBasedEvade(IWorldCoordinate myPos)
        {
            // Get data from awareness engine
            _dangerCount = _awareness?.DangerZones.Count ?? 0;
            _safeZoneCount = _awareness?.SafeZones.Count ?? 0;
            _threatsMovingToward = _awareness?.Threats.Count(t => t.IsMovingTowardPlayer) ?? 0;
            
            // Get wall analysis
            var wallData = AnalyzeWallsFromAwareness(myPos);
            _wallsNearby = wallData.WallsDetected;
            _isCornerTrapped = wallData.IsCornerTrapped;
            
            // Get enemy count
            _enemyCount = _awareness?.Threats.Count ?? 0;

            // Try A* pathfinding for escape (v3.0)
            if (UseAStarForEscape && _awareness != null)
            {
                var bestEscape = _awareness.GetBestEscapeRoute();
                if (bestEscape != null && !bestEscape.IsBlocked && bestEscape.SafetyScore > 30)
                {
                    _escapePath = _pathfinder?.FindSafePath(
                        myPos.X, myPos.Y,
                        bestEscape.TargetX, bestEscape.TargetY
                    );

                    if (_escapePath != null && _escapePath.IsValid)
                    {
                        _currentAction = "A* Escape";
                        ExecutePathMove(myPos);
                        return;
                    }
                }
            }

            // Fall back to direct escape route calculation
            var dangers = CollectDangersFromAwareness();
            var enemies = CollectEnemiesFromAwareness();
            
            _escapeRoutes = CalculateEscapeRoutes(myPos, dangers, enemies, wallData);
            _bestRoute = SelectBestRoute(_escapeRoutes);

            if (_bestRoute != null && _bestRoute.Score > 0)
            {
                var targetDirection = new Vector2D 
                { 
                    X = _bestRoute.Direction.X, 
                    Y = _bestRoute.Direction.Y, 
                    Length = 1f 
                };
                
                _smoothedDirection = SmoothDirection(_smoothedDirection, targetDirection);
                
                if (IsStuck())
                {
                    _currentAction = "UNSTUCK!";
                    ExecuteUnstuckManeuver(myPos, wallData);
                }
                else
                {
                    _currentAction = _bestRoute.Reason;
                    ExecuteMove(_smoothedDirection, myPos);
                }
            }
            else
            {
                // No good escape - try safe zone
                var safeZone = _awareness?.GetSafestPosition();
                if (safeZone != null)
                {
                    _currentAction = "To Safe Zone";
                    MoveToward(safeZone.X, safeZone.Y, myPos);
                }
                else
                {
                    _currentAction = "No Escape!";
                }
            }
        }

        /// <summary>
        /// Legacy evade for when Navigation system is disabled
        /// ONLY called when already confirmed in danger
        /// </summary>
        private void PerformLegacyEvade(IWorldCoordinate myPos)
        {
            var dangers = CollectDangers();
            var enemies = CollectEnemies();
            var wallData = AnalyzeWalls(myPos);
            
            _dangerCount = dangers.Count;
            _enemyCount = enemies.Count;
            _wallsNearby = wallData.WallsDetected;
            _isCornerTrapped = wallData.IsCornerTrapped;

            _escapeRoutes = CalculateEscapeRoutes(myPos, dangers, enemies, wallData);
            _bestRoute = SelectBestRoute(_escapeRoutes);

            if (_bestRoute != null && _bestRoute.Score > 0)
            {
                var targetDirection = new Vector2D 
                { 
                    X = _bestRoute.Direction.X, 
                    Y = _bestRoute.Direction.Y, 
                    Length = 1f 
                };
                
                _smoothedDirection = SmoothDirection(_smoothedDirection, targetDirection);
                
                if (IsStuck())
                {
                    _currentAction = "UNSTUCK!";
                    ExecuteUnstuckManeuver(myPos, wallData);
                }
                else
                {
                    _currentAction = _bestRoute.Reason;
                    ExecuteMove(_smoothedDirection, myPos);
                }
            }
            else
            {
                _currentAction = "Evading...";
                _smoothedDirection = new Vector2D { X = 0, Y = 0, Length = 0 };
            }
        }

        private void ExecutePathMove(IWorldCoordinate myPos)
        {
            if (_escapePath == null || !_escapePath.IsValid) return;
            
            // Find current waypoint
            if (_escapePathIndex >= _escapePath.Path.Count)
            {
                _escapePath = null;
                return;
            }
            
            var wp = _escapePath.Path[_escapePathIndex];
            float dist = (float)Math.Sqrt(
                Math.Pow(myPos.X - wp.WorldX, 2) + 
                Math.Pow(myPos.Y - wp.WorldY, 2));
            
            if (dist < 3f)
            {
                _escapePathIndex++;
                if (_escapePathIndex >= _escapePath.Path.Count)
                {
                    _escapePath = null;
                    return;
                }
                wp = _escapePath.Path[_escapePathIndex];
            }
            
            MoveToward(wp.WorldX, wp.WorldY, myPos);
        }

        private void MoveToward(float x, float y, IWorldCoordinate myPos)
        {
            if (_lastMoveTime.IsRunning && _lastMoveTime.ElapsedMilliseconds < ActionCooldownMs)
                return;

            var targetCoord = Hud.Window.CreateWorldCoordinate(x, y, myPos.Z);
            var screenPos = targetCoord.ToScreenCoordinate();

            if (screenPos.X > 10 && screenPos.X < Hud.Window.Size.Width - 10 &&
                screenPos.Y > 10 && screenPos.Y < Hud.Window.Size.Height - 10)
            {
                _lastTargetPos = targetCoord;
                _lastPlayerPos = myPos;
                
                Hud.Interaction.MouseMove((int)screenPos.X, (int)screenPos.Y, 1, 1);
                Hud.Interaction.MouseDown(MouseButtons.Left);
                Hud.Wait(3);
                Hud.Interaction.MouseUp(MouseButtons.Left);

                _lastMoveTime.Restart();
            }
        }

        #endregion

        #region Navigation Integration (v3.0)

        private WallAnalysis AnalyzeWallsFromAwareness(IWorldCoordinate myPos)
        {
            var analysis = new WallAnalysis();
            var scene = Hud.Game.Me.Scene;
            
            if (scene == null) return analysis;

            // Use NavMesh for more accurate walkability if available
            if (UseNavMeshWalkability)
            {
                // Check walkability in 8 directions
                float checkDist = WallDetectionRange;
                int blockedDirections = 0;
                
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * 45f * (float)Math.PI / 180f;
                    float checkX = myPos.X + (float)Math.Cos(angle) * checkDist;
                    float checkY = myPos.Y + (float)Math.Sin(angle) * checkDist;
                    
                    if (!_navMesh.IsWalkable(checkX, checkY))
                        blockedDirections++;
                }
                
                analysis.WallsDetected = blockedDirections;
                analysis.IsCornerTrapped = blockedDirections >= 5;
            }

            // Scene boundaries (fallback)
            analysis.SceneMinX = scene.PosX;
            analysis.SceneMinY = scene.PosY;
            analysis.SceneMaxX = scene.MaxX;
            analysis.SceneMaxY = scene.MaxY;

            float distToLeft = myPos.X - scene.PosX;
            float distToRight = scene.MaxX - myPos.X;
            float distToBottom = myPos.Y - scene.PosY;
            float distToTop = scene.MaxY - myPos.Y;

            if (distToLeft < WallDetectionRange) analysis.LeftWallDist = distToLeft;
            if (distToRight < WallDetectionRange) analysis.RightWallDist = distToRight;
            if (distToBottom < WallDetectionRange) analysis.BottomWallDist = distToBottom;
            if (distToTop < WallDetectionRange) analysis.TopWallDist = distToTop;

            return analysis;
        }

        private List<DangerPoint> CollectDangersFromAwareness()
        {
            return _awareness.DangerZones.Select(d => new DangerPoint
            {
                Position = d.Position,
                Radius = d.Radius,
                Type = MapDangerLevel(d.Level),
                Priority = d.Priority,
                Distance = d.Distance
            }).ToList();
        }

        private List<EnemyPoint> CollectEnemiesFromAwareness()
        {
            return _awareness.Threats.Select(t => new EnemyPoint
            {
                Position = t.Position,
                Distance = t.Distance,
                Priority = t.Priority,
                ThreatRadius = t.ThreatRadius,
                IsElite = t.IsElite,
                IsMovingToward = t.IsMovingTowardPlayer,
                PredictedX = t.PredictedX,
                PredictedY = t.PredictedY
            }).ToList();
        }

        private DangerType MapDangerLevel(DangerLevel level)
        {
            switch (level)
            {
                case DangerLevel.Critical:
                    return DangerType.IceBall;
                case DangerLevel.High:
                    return DangerType.Arcane;
                case DangerLevel.Medium:
                    return DangerType.Desecrator;
                default:
                    return DangerType.Plagued;
            }
        }

        #endregion

        #region Legacy Methods (kept for fallback)

        private void UpdatePositionHistory(IWorldCoordinate pos)
        {
            if (_positionHistory.Count >= POSITION_HISTORY_SIZE)
                _positionHistory.Dequeue();
            _positionHistory.Enqueue(Hud.Window.CreateWorldCoordinate(pos.X, pos.Y, pos.Z));
        }

        private bool IsStuck()
        {
            if (_positionHistory.Count < POSITION_HISTORY_SIZE) return false;
            
            var positions = _positionHistory.ToArray();
            var first = positions[0];
            var last = positions[positions.Length - 1];
            
            float totalMovement = first.XYDistanceTo(last);
            
            if (totalMovement < 2f && _isEvading)
            {
                _stuckCounter++;
                return _stuckCounter >= STUCK_THRESHOLD;
            }
            
            _stuckCounter = Math.Max(0, _stuckCounter - 1);
            return false;
        }

        private WallAnalysis AnalyzeWalls(IWorldCoordinate myPos)
        {
            var analysis = new WallAnalysis();
            var scene = Hud.Game.Me.Scene;
            
            if (scene == null) return analysis;

            float sceneMinX = scene.PosX;
            float sceneMinY = scene.PosY;
            float sceneMaxX = scene.MaxX;
            float sceneMaxY = scene.MaxY;

            float distToLeft = myPos.X - sceneMinX;
            float distToRight = sceneMaxX - myPos.X;
            float distToBottom = myPos.Y - sceneMinY;
            float distToTop = sceneMaxY - myPos.Y;

            if (distToLeft < WallDetectionRange)
            {
                analysis.WallsDetected++;
                analysis.WallDirections.Add(new Vector2D { X = -1, Y = 0, Length = distToLeft });
                analysis.LeftWallDist = distToLeft;
            }
            if (distToRight < WallDetectionRange)
            {
                analysis.WallsDetected++;
                analysis.WallDirections.Add(new Vector2D { X = 1, Y = 0, Length = distToRight });
                analysis.RightWallDist = distToRight;
            }
            if (distToBottom < WallDetectionRange)
            {
                analysis.WallsDetected++;
                analysis.WallDirections.Add(new Vector2D { X = 0, Y = -1, Length = distToBottom });
                analysis.BottomWallDist = distToBottom;
            }
            if (distToTop < WallDetectionRange)
            {
                analysis.WallsDetected++;
                analysis.WallDirections.Add(new Vector2D { X = 0, Y = 1, Length = distToTop });
                analysis.TopWallDist = distToTop;
            }

            int closeWalls = 0;
            if (distToLeft < MinWallClearance * 2) closeWalls++;
            if (distToRight < MinWallClearance * 2) closeWalls++;
            if (distToBottom < MinWallClearance * 2) closeWalls++;
            if (distToTop < MinWallClearance * 2) closeWalls++;
            
            analysis.IsCornerTrapped = closeWalls >= 2;

            analysis.SceneMinX = sceneMinX;
            analysis.SceneMinY = sceneMinY;
            analysis.SceneMaxX = sceneMaxX;
            analysis.SceneMaxY = sceneMaxY;

            return analysis;
        }

        private bool IsPositionWalkable(float x, float y, WallAnalysis wallData)
        {
            if (UseNavMeshWalkability && _navMesh != null)
            {
                return _navMesh.IsWalkable(x, y);
            }
            
            if (x < wallData.SceneMinX + MinWallClearance) return false;
            if (x > wallData.SceneMaxX - MinWallClearance) return false;
            if (y < wallData.SceneMinY + MinWallClearance) return false;
            if (y > wallData.SceneMaxY - MinWallClearance) return false;
            
            return true;
        }

        private List<EscapeRoute> CalculateEscapeRoutes(IWorldCoordinate myPos, 
            List<DangerPoint> dangers, List<EnemyPoint> enemies, WallAnalysis wallData)
        {
            var routes = new List<EscapeRoute>();
            float angleStep = 360f / EscapeDirections;

            for (int i = 0; i < EscapeDirections; i++)
            {
                float angle = i * angleStep;
                float radians = angle * (float)Math.PI / 180f;
                
                float dirX = (float)Math.Cos(radians);
                float dirY = (float)Math.Sin(radians);
                
                float targetX = myPos.X + dirX * EscapeDistance;
                float targetY = myPos.Y + dirY * EscapeDistance;
                
                var route = new EscapeRoute
                {
                    Direction = new Vector2D { X = dirX, Y = dirY, Length = 1f },
                    Angle = angle,
                    TargetX = targetX,
                    TargetY = targetY
                };

                float score = 100f;
                string reason = "Clear";

                if (!IsPositionWalkable(targetX, targetY, wallData))
                {
                    score = -1000f;
                    route.IsBlocked = true;
                    reason = "Blocked";
                }
                else
                {
                    // Wall penalty
                    float wallPenalty = CalculateWallPenalty(targetX, targetY, wallData);
                    score -= wallPenalty;

                    // Danger avoidance
                    float dangerScore = CalculateDangerScore(myPos, targetX, targetY, dangers, ref reason);
                    score += dangerScore;

                    // Enemy kiting (with prediction for v3.0)
                    float enemyScore = CalculateEnemyScore(myPos, targetX, targetY, enemies, ref reason);
                    score += enemyScore;

                    // Predictive pathing
                    if (EnablePredictivePathing && score > 0)
                    {
                        float lookAheadX = targetX + dirX * EscapeDistance;
                        float lookAheadY = targetY + dirY * EscapeDistance;
                        
                        if (!IsPositionWalkable(lookAheadX, lookAheadY, wallData))
                            score -= 30f;
                    }

                    // Corner escape bonus
                    if (wallData.IsCornerTrapped)
                    {
                        float openness = GetOpenness(targetX, targetY, wallData);
                        score += openness * 2f;
                    }
                }

                route.Score = score;
                route.Reason = reason;
                routes.Add(route);
            }

            return routes;
        }

        private float CalculateWallPenalty(float targetX, float targetY, WallAnalysis wallData)
        {
            float penalty = 0;
            float distFromLeftWall = targetX - wallData.SceneMinX;
            float distFromRightWall = wallData.SceneMaxX - targetX;
            float distFromBottomWall = targetY - wallData.SceneMinY;
            float distFromTopWall = wallData.SceneMaxY - targetY;
            
            if (distFromLeftWall < WallDetectionRange)
                penalty += (WallDetectionRange - distFromLeftWall) * WallAvoidanceWeight;
            if (distFromRightWall < WallDetectionRange)
                penalty += (WallDetectionRange - distFromRightWall) * WallAvoidanceWeight;
            if (distFromBottomWall < WallDetectionRange)
                penalty += (WallDetectionRange - distFromBottomWall) * WallAvoidanceWeight;
            if (distFromTopWall < WallDetectionRange)
                penalty += (WallDetectionRange - distFromTopWall) * WallAvoidanceWeight;
            
            return penalty;
        }

        private float CalculateDangerScore(IWorldCoordinate myPos, float targetX, float targetY, 
            List<DangerPoint> dangers, ref string reason)
        {
            float bonus = 0;
            foreach (var danger in dangers)
            {
                float currentDist = myPos.XYDistanceTo(danger.Position);
                float newDist = (float)Math.Sqrt(
                    Math.Pow(targetX - danger.Position.X, 2) + 
                    Math.Pow(targetY - danger.Position.Y, 2));
                
                if (newDist > currentDist)
                {
                    bonus += (newDist - currentDist) * danger.Priority * 0.5f;
                    if (reason == "Clear") reason = "Evade: " + danger.Type;
                }
                else if (newDist < danger.Radius)
                {
                    bonus -= danger.Priority * 2f;
                }
            }
            return bonus;
        }

        private float CalculateEnemyScore(IWorldCoordinate myPos, float targetX, float targetY, 
            List<EnemyPoint> enemies, ref string reason)
        {
            float bonus = 0;
            foreach (var enemy in enemies)
            {
                if (enemy.Distance < SafeDistance)
                {
                    float currentDist = enemy.Distance;
                    
                    // Use predicted position if monster is moving toward us (v3.0)
                    float checkX = enemy.Position.X;
                    float checkY = enemy.Position.Y;
                    if (UseMonsterPrediction && enemy.IsMovingToward && enemy.PredictedX != 0)
                    {
                        checkX = enemy.PredictedX;
                        checkY = enemy.PredictedY;
                    }
                    
                    float newDist = (float)Math.Sqrt(
                        Math.Pow(targetX - checkX, 2) + 
                        Math.Pow(targetY - checkY, 2));
                    
                    if (newDist > currentDist)
                    {
                        float b = (newDist - currentDist) * enemy.Priority;
                        if (enemy.IsElite) b *= 1.5f;
                        if (enemy.IsMovingToward) b *= 1.3f; // Extra bonus for escaping approaching monsters
                        bonus += b;
                        if (reason == "Clear") reason = "Kiting";
                    }
                }
            }
            return bonus;
        }

        private float GetOpenness(float x, float y, WallAnalysis wallData)
        {
            float distFromLeftWall = x - wallData.SceneMinX;
            float distFromRightWall = wallData.SceneMaxX - x;
            float distFromBottomWall = y - wallData.SceneMinY;
            float distFromTopWall = wallData.SceneMaxY - y;
            
            return Math.Min(
                Math.Min(distFromLeftWall, distFromRightWall),
                Math.Min(distFromBottomWall, distFromTopWall));
        }

        private EscapeRoute SelectBestRoute(List<EscapeRoute> routes)
        {
            if (routes == null || routes.Count == 0) return null;
            
            var validRoutes = routes.Where(r => !r.IsBlocked && r.Score > 0)
                                   .OrderByDescending(r => r.Score)
                                   .ToList();
            
            if (validRoutes.Count == 0)
                return routes.OrderByDescending(r => r.Score).FirstOrDefault();
            
            return validRoutes.First();
        }

        private Vector2D SmoothDirection(Vector2D current, Vector2D target)
        {
            if (current.Length < 0.01f) return target;
            
            float smoothing = MovementSmoothing;
            return new Vector2D
            {
                X = current.X * smoothing + target.X * (1f - smoothing),
                Y = current.Y * smoothing + target.Y * (1f - smoothing),
                Length = 1f
            };
        }

        private void ExecuteMove(Vector2D direction, IWorldCoordinate myPos)
        {
            if (_lastMoveTime.IsRunning && _lastMoveTime.ElapsedMilliseconds < ActionCooldownMs)
                return;

            float targetX = myPos.X + direction.X * EscapeDistance;
            float targetY = myPos.Y + direction.Y * EscapeDistance;

            var targetCoord = Hud.Window.CreateWorldCoordinate(targetX, targetY, myPos.Z);
            var screenPos = targetCoord.ToScreenCoordinate();

            if (screenPos.X > 10 && screenPos.X < Hud.Window.Size.Width - 10 &&
                screenPos.Y > 10 && screenPos.Y < Hud.Window.Size.Height - 10)
            {
                _lastTargetPos = targetCoord;
                _lastPlayerPos = myPos;
                
                Hud.Interaction.MouseMove((int)screenPos.X, (int)screenPos.Y, 1, 1);
                Hud.Interaction.MouseDown(MouseButtons.Left);
                Hud.Wait(3);
                Hud.Interaction.MouseUp(MouseButtons.Left);

                _lastMoveTime.Restart();
            }
        }

        private void ExecuteUnstuckManeuver(IWorldCoordinate myPos, WallAnalysis wallData)
        {
            float bestOpenness = -1;
            Vector2D bestDir = new Vector2D { X = 0, Y = 1, Length = 1 };
            
            float angleStep = 360f / 8f;
            
            for (int i = 0; i < 8; i++)
            {
                float angle = i * angleStep;
                float radians = angle * (float)Math.PI / 180f;
                float dirX = (float)Math.Cos(radians);
                float dirY = (float)Math.Sin(radians);
                
                float targetX = myPos.X + dirX * EscapeDistance * 1.5f;
                float targetY = myPos.Y + dirY * EscapeDistance * 1.5f;
                
                if (IsPositionWalkable(targetX, targetY, wallData))
                {
                    float openness = GetOpenness(targetX, targetY, wallData);
                    
                    if (openness > bestOpenness)
                    {
                        bestOpenness = openness;
                        bestDir = new Vector2D { X = dirX, Y = dirY, Length = 1 };
                    }
                }
            }
            
            _stuckCounter = 0;
            ExecuteMove(bestDir, myPos);
        }

        private List<DangerPoint> CollectDangers()
        {
            var dangers = new List<DangerPoint>();
            var myPos = Hud.Game.Me.FloorCoordinate;

            foreach (var actor in Hud.Game.Actors)
            {
                if (_dangerZones.TryGetValue(actor.SnoActor.Sno, out var zone))
                {
                    float distance = myPos.XYDistanceTo(actor.FloorCoordinate);
                    float effectiveRadius = zone.Radius * DangerRadiusMultiplier;

                    if (distance < effectiveRadius + 15f)
                    {
                        dangers.Add(new DangerPoint
                        {
                            Position = actor.FloorCoordinate,
                            Radius = effectiveRadius,
                            Type = zone.Type,
                            Priority = zone.Priority,
                            Distance = distance
                        });
                    }
                }
            }

            foreach (var avoid in Hud.Game.Me.AvoidablesInRange)
            {
                float distance = myPos.XYDistanceTo(avoid.FloorCoordinate);
                dangers.Add(new DangerPoint
                {
                    Position = avoid.FloorCoordinate,
                    Radius = avoid.AvoidableDefinition.Radius * DangerRadiusMultiplier,
                    Type = DangerType.Avoidable,
                    Priority = avoid.AvoidableDefinition.InstantDeath ? 100 : 60,
                    Distance = distance
                });
            }

            return dangers;
        }

        private List<EnemyPoint> CollectEnemies()
        {
            var enemies = new List<EnemyPoint>();
            var myPos = Hud.Game.Me.FloorCoordinate;

            foreach (var monster in Hud.Game.AliveMonsters)
            {
                float distance = myPos.XYDistanceTo(monster.FloorCoordinate);
                if (distance > MaxEnemyRange) continue;

                int priority = 1;
                float threatRadius = monster.RadiusBottom + 5f;

                if (monster.Rarity == ActorRarity.Boss) 
                { 
                    priority = 10; 
                    threatRadius += 8f; 
                }
                else if (monster.Rarity == ActorRarity.Champion || monster.Rarity == ActorRarity.Rare) 
                { 
                    priority = 5; 
                    threatRadius += 4f; 
                }
                else if (monster.Rarity == ActorRarity.RareMinion) 
                { 
                    priority = 3; 
                    threatRadius += 2f;
                }

                enemies.Add(new EnemyPoint
                {
                    Position = monster.FloorCoordinate,
                    Distance = distance,
                    Priority = priority,
                    ThreatRadius = threatRadius,
                    IsElite = monster.Rarity != ActorRarity.Normal
                });
            }

            return enemies;
        }

        #endregion

        #region UI Rendering

        public override void PaintTopInGame(ClipState clipState)
        {
            // Call base for Core registration
            base.PaintTopInGame(clipState);
            
            if (clipState != ClipState.BeforeClip) return;
            if (!Hud.Game.IsInGame) return;
            if (!Enabled) return;

            // Draw panel only if enabled AND Core is not showing our status in sidebar
            // When registered with Core, the sidebar shows our status - no need for separate panel
            if (ShowPanel && !HasCore)
            {
                DrawStatusPanel();
            }

            if (_isActiveInternal && !Hud.Game.IsInTown)
            {
                if (ShowDebugCircles) DrawDebugCircles();
                if (ShowWallMarkers) DrawWallMarkers();
                if (ShowEscapeRoutes) DrawEscapeRoutes();
                if (ShowSafeZones && UseNavigationSystem) DrawSafeZones();
                if (ShowPredictedPaths && UseNavigationSystem) DrawPredictedPaths();
                if (ShowMovementIndicator && _isEvading && _lastTargetPos != null) 
                    DrawMovementIndicator();
            }
        }

        private void DrawDebugCircles()
        {
            var myPos = Hud.Game.Me.FloorCoordinate;
            _safeBrush.DrawWorldEllipse(SafeDistance, -1, myPos);

            if (UseNavigationSystem && _awareness != null)
            {
                // Draw danger zones from awareness
                foreach (var danger in _awareness.DangerZones)
                {
                    if (!danger.Position.IsOnScreen()) continue;
                    
                    IBrush brush;
                    switch (danger.Level)
                    {
                        case DangerLevel.Critical:
                            brush = _dangerBrush;
                            break;
                        case DangerLevel.High:
                            brush = _dangerBrushHigh;
                            break;
                        default:
                            brush = _dangerBrushMedium;
                            break;
                    }
                    
                    brush.DrawWorldEllipse(danger.Radius, -1, danger.Position);
                }
            }
            else
            {
                foreach (var actor in Hud.Game.Actors)
                {
                    if (_dangerZones.TryGetValue(actor.SnoActor.Sno, out var zone))
                    {
                        float effectiveRadius = zone.Radius * DangerRadiusMultiplier;
                        _dangerBrush.DrawWorldEllipse(effectiveRadius, -1, actor.FloorCoordinate);
                    }
                }
            }

            foreach (var monster in Hud.Game.AliveMonsters.Where(m => m.Rarity != ActorRarity.Normal))
            {
                float distance = myPos.XYDistanceTo(monster.FloorCoordinate);
                if (distance < MaxEnemyRange)
                    _enemyBrush.DrawWorldEllipse(monster.RadiusBottom + 3f, -1, monster.FloorCoordinate);
            }
        }

        private void DrawSafeZones()
        {
            if (_awareness == null) return;
            var myPos = Hud.Game.Me.FloorCoordinate;
            
            foreach (var safe in _awareness.SafeZones.Take(5))
            {
                var coord = Hud.Window.CreateWorldCoordinate(safe.X, safe.Y, myPos.Z);
                if (!coord.IsOnScreen()) continue;
                
                float size = 2f + (safe.SafetyScore / 100f) * 2f;
                _safeZoneBrush.DrawWorldEllipse(size, -1, coord);
            }
        }

        private void DrawPredictedPaths()
        {
            if (_awareness == null) return;
            
            foreach (var threat in _awareness.Threats.Where(t => t.IsMovingTowardPlayer && t.PredictedX != 0))
            {
                if (!threat.Position.IsOnScreen()) continue;
                
                var predicted = Hud.Window.CreateWorldCoordinate(
                    threat.PredictedX, threat.PredictedY, threat.Position.Z);
                
                if (predicted.IsOnScreen())
                {
                    _predictionBrush.DrawLineWorld(threat.Position, predicted);
                    _predictionBrush.DrawWorldEllipse(1.5f, -1, predicted);
                }
            }
        }

        private void DrawWallMarkers()
        {
            var scene = Hud.Game.Me.Scene;
            if (scene == null) return;

            var myPos = Hud.Game.Me.FloorCoordinate;
            float markerDist = WallDetectionRange;
            
            if (myPos.X - scene.PosX < markerDist)
            {
                var wallPoint = Hud.Window.CreateWorldCoordinate(scene.PosX, myPos.Y, myPos.Z);
                if (wallPoint.IsOnScreen())
                    _wallBrush.DrawWorldEllipse(2f, -1, wallPoint);
            }
            if (scene.MaxX - myPos.X < markerDist)
            {
                var wallPoint = Hud.Window.CreateWorldCoordinate(scene.MaxX, myPos.Y, myPos.Z);
                if (wallPoint.IsOnScreen())
                    _wallBrush.DrawWorldEllipse(2f, -1, wallPoint);
            }
            if (myPos.Y - scene.PosY < markerDist)
            {
                var wallPoint = Hud.Window.CreateWorldCoordinate(myPos.X, scene.PosY, myPos.Z);
                if (wallPoint.IsOnScreen())
                    _wallBrush.DrawWorldEllipse(2f, -1, wallPoint);
            }
            if (scene.MaxY - myPos.Y < markerDist)
            {
                var wallPoint = Hud.Window.CreateWorldCoordinate(myPos.X, scene.MaxY, myPos.Z);
                if (wallPoint.IsOnScreen())
                    _wallBrush.DrawWorldEllipse(2f, -1, wallPoint);
            }
        }

        private void DrawEscapeRoutes()
        {
            if (_escapeRoutes == null || _escapeRoutes.Count == 0) return;

            var myPos = Hud.Game.Me.FloorCoordinate;

            foreach (var route in _escapeRoutes)
            {
                var targetCoord = Hud.Window.CreateWorldCoordinate(route.TargetX, route.TargetY, myPos.Z);
                if (!targetCoord.IsOnScreen()) continue;

                IBrush brush;
                if (route.IsBlocked)
                    brush = _blockedBrush;
                else if (_bestRoute != null && route == _bestRoute)
                    brush = _moveBrush;
                else
                    brush = _escapeBrush;

                brush.DrawWorldEllipse(0.8f, -1, targetCoord);
            }
        }

        private void DrawMovementIndicator()
        {
            if (_lastTargetPos == null) return;
            var myPos = Hud.Game.Me.FloorCoordinate;
            _moveBrush.DrawLineWorld(myPos, _lastTargetPos);
            _moveBrush.DrawWorldEllipse(1.5f, -1, _lastTargetPos);
        }

        private void DrawStatusPanel()
        {
            float x = Hud.Window.Size.Width * PanelX;
            float y = Hud.Window.Size.Height * PanelY;
            float w = 145;
            float h = _isActiveInternal ? 95 : 48;
            float pad = 6;

            _panelBrush.DrawRectangle(x, y, w, h);
            _borderBrush.DrawRectangle(x, y, w, h);

            var accentBrush = _isActiveInternal ? _accentOnBrush : _accentOffBrush;
            accentBrush.DrawRectangle(x, y, 3, h);

            float tx = x + pad + 3;
            float ty = y + pad;

            var title = _titleFont.GetTextLayout("Smart Evade v3");
            _titleFont.DrawText(title, tx, ty);
            ty += title.Metrics.Height + 2;

            string statusStr = _isActiveInternal ? (_isEvading ? "● " + _currentAction : "● Ready") : "OFF";
            var statusLayout = _statusFont.GetTextLayout(statusStr);
            _statusFont.DrawText(statusLayout, tx, ty);
            ty += statusLayout.Metrics.Height + 1;

            var hint = _infoFont.GetTextLayout("[Shift+J] Toggle");
            _infoFont.DrawText(hint, tx, ty);

            if (_isActiveInternal)
            {
                ty += hint.Metrics.Height + 1;
                string wallInfo = _isCornerTrapped ? "⚠ CORNER!" : $"W:{_wallsNearby}";
                var stats = _infoFont.GetTextLayout($"D:{_dangerCount} M:{_enemyCount} {wallInfo}");
                _infoFont.DrawText(stats, tx, ty);
                
                ty += stats.Metrics.Height;
                string navInfo = UseNavigationSystem ? $"Safe:{_safeZoneCount} →:{_threatsMovingToward}" : "Nav: OFF";
                var navStats = _infoFont.GetTextLayout(navInfo);
                _infoFont.DrawText(navStats, tx, ty);
            }
        }

        #endregion

        /// <summary>
        /// Get the spatial awareness engine for external use
        /// </summary>
        public SpatialAwarenessEngine GetAwareness() => _awareness;
    }

    #region Helper Classes

    internal class DangerZone
    {
        public float Radius { get; set; }
        public DangerType Type { get; set; }
        public int Priority { get; set; }
        
        public DangerZone(float radius, DangerType type, int priority)
        {
            Radius = radius;
            Type = type;
            Priority = priority;
        }
    }

    internal class DangerPoint
    {
        public IWorldCoordinate Position { get; set; }
        public float Radius { get; set; }
        public DangerType Type { get; set; }
        public int Priority { get; set; }
        public float Distance { get; set; }
    }

    internal class EnemyPoint
    {
        public IWorldCoordinate Position { get; set; }
        public float Distance { get; set; }
        public int Priority { get; set; }
        public float ThreatRadius { get; set; }
        public bool IsElite { get; set; }
        public bool IsMovingToward { get; set; }
        public float PredictedX { get; set; }
        public float PredictedY { get; set; }
    }

    internal class WallAnalysis
    {
        public int WallsDetected { get; set; }
        public bool IsCornerTrapped { get; set; }
        public List<Vector2D> WallDirections { get; set; } = new List<Vector2D>();
        public float LeftWallDist { get; set; } = float.MaxValue;
        public float RightWallDist { get; set; } = float.MaxValue;
        public float TopWallDist { get; set; } = float.MaxValue;
        public float BottomWallDist { get; set; } = float.MaxValue;
        public float SceneMinX { get; set; }
        public float SceneMinY { get; set; }
        public float SceneMaxX { get; set; }
        public float SceneMaxY { get; set; }
    }

    internal class EscapeRoute
    {
        public Vector2D Direction { get; set; }
        public float Angle { get; set; }
        public float TargetX { get; set; }
        public float TargetY { get; set; }
        public float Score { get; set; }
        public bool IsBlocked { get; set; }
        public string Reason { get; set; }
    }

    internal struct Vector2D
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Length { get; set; }
    }

    internal enum DangerType
    {
        Unknown,
        IceBall,
        MoltenExplosion,
        Molten,
        Desecrator,
        Thunderstorm,
        Plagued,
        Arcane,
        ArcaneSpawn,
        FrozenPulse,
        Wormhole,
        PoisonEnchanted,
        Orbiter,
        GhomGas,
        Avoidable
    }

    #endregion
}
