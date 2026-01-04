namespace Turbo.Plugins.Custom.Navigation
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;
    using SharpDX.DirectInput;
    using Turbo.Plugins.Default;

    /// <summary>
    /// God-Tier Navigation Plugin
    /// 
    /// Features:
    /// - Real-time spatial awareness visualization
    /// - A* pathfinding with danger avoidance
    /// - Safe zone detection
    /// - Escape route analysis
    /// - Monster movement prediction
    /// - Density heatmaps
    /// - Click-to-navigate
    /// 
    /// Hotkeys:
    /// - Ctrl+N: Toggle navigation overlay
    /// - Ctrl+Shift+N: Toggle auto-navigation
    /// - Right-click: Set navigation target (when enabled)
    /// </summary>
    public class NavigationPlugin : BasePlugin, IAfterCollectHandler, IInGameTopPainter, IKeyEventHandler
    {
        #region Public Settings

        public IKeyEvent ToggleOverlayKey { get; set; }
        public IKeyEvent ToggleAutoNavKey { get; set; }
        
        public bool ShowOverlay { get; set; } = true;
        public bool AutoNavigate { get; set; } = false;
        
        // Display options
        public bool ShowGrid { get; set; } = false;
        public bool ShowWalkability { get; set; } = false;
        public bool ShowDangerZones { get; set; } = true;
        public bool ShowSafeZones { get; set; } = true;
        public bool ShowEscapeRoutes { get; set; } = true;
        public bool ShowPath { get; set; } = true;
        public bool ShowThreatIndicators { get; set; } = true;
        public bool ShowDensityHeatmap { get; set; } = false;
        public bool ShowWallMarkers { get; set; } = true;
        public bool ShowFrontiers { get; set; } = true;  // NEW: Show exploration frontiers
        public bool ShowPortals { get; set; } = true;    // NEW: Show detected portals
        public bool ShowPanel { get; set; } = true;
        
        // Panel settings
        public float PanelX { get; set; } = 0.005f;
        public float PanelY { get; set; } = 0.72f;

        #endregion

        #region Private Fields

        private SpatialAwarenessEngine _awareness;
        private AStarPathfinder _pathfinder;
        
        // Navigation state
        private IWorldCoordinate _navigationTarget;
        private PathResult _currentPath;
        private int _pathWaypointIndex;
        private IWatch _moveTimer;
        private IWatch _pathRecalcTimer;
        
        // UI
        private IFont _titleFont;
        private IFont _infoFont;
        private IFont _statusFont;
        private IBrush _panelBrush;
        private IBrush _borderBrush;
        private IBrush _accentOnBrush;
        private IBrush _accentOffBrush;
        
        // World brushes
        private IBrush _dangerBrushCritical;
        private IBrush _dangerBrushHigh;
        private IBrush _dangerBrushMedium;
        private IBrush _dangerBrushLow;
        private IBrush _safeBrush;
        private IBrush _pathBrush;
        private IBrush _escapeBrush;
        private IBrush _escapeBlockedBrush;
        private IBrush _targetBrush;
        private IBrush _wallBrush;
        private IBrush _gridBrush;
        private IBrush _gridBlockedBrush;
        private IBrush _threatBrush;
        private IBrush _eliteThreatBrush;
        private IBrush _frontierBrush;  // NEW: For exploration frontiers
        private IBrush _portalBrush;    // NEW: For portals
        
        private const float PATH_RECALC_INTERVAL = 500;
        private const float WAYPOINT_REACH_DIST = 3f;
        private const int MOVE_INTERVAL = 50;

        #endregion

        public NavigationPlugin()
        {
            Enabled = true; // RE-ENABLED
            Order = 60000;
        }

        public override void Load(IController hud)
        {
            base.Load(hud);

            // Initialize systems
            _awareness = new SpatialAwarenessEngine { Hud = hud };
            _pathfinder = _awareness.Pathfinder;

            // Hotkeys
            ToggleOverlayKey = Hud.Input.CreateKeyEvent(true, Key.N, true, false, false); // Ctrl+N
            ToggleAutoNavKey = Hud.Input.CreateKeyEvent(true, Key.N, true, false, true);  // Ctrl+Shift+N

            // Timers
            _moveTimer = Hud.Time.CreateWatch();
            _pathRecalcTimer = Hud.Time.CreateWatch();

            InitializeUI();
        }

        private void InitializeUI()
        {
            _titleFont = Hud.Render.CreateFont("tahoma", 8, 255, 100, 200, 255, true, false, 180, 0, 0, 0, true);
            _infoFont = Hud.Render.CreateFont("tahoma", 7, 200, 180, 180, 180, false, false, 140, 0, 0, 0, true);
            _statusFont = Hud.Render.CreateFont("tahoma", 7.5f, 255, 255, 255, 255, true, false, 160, 0, 0, 0, true);

            _panelBrush = Hud.Render.CreateBrush(235, 15, 20, 35, 0);
            _borderBrush = Hud.Render.CreateBrush(200, 50, 80, 120, 1f);
            _accentOnBrush = Hud.Render.CreateBrush(255, 80, 200, 255, 0);
            _accentOffBrush = Hud.Render.CreateBrush(255, 100, 100, 100, 0);

            // Danger zones by level
            _dangerBrushCritical = Hud.Render.CreateBrush(120, 255, 0, 0, 2f);
            _dangerBrushHigh = Hud.Render.CreateBrush(100, 255, 100, 0, 2f);
            _dangerBrushMedium = Hud.Render.CreateBrush(80, 255, 200, 0, 1.5f);
            _dangerBrushLow = Hud.Render.CreateBrush(60, 200, 200, 0, 1f);

            _safeBrush = Hud.Render.CreateBrush(60, 0, 255, 100, 1.5f);
            _pathBrush = Hud.Render.CreateBrush(200, 100, 200, 255, 3f);
            _escapeBrush = Hud.Render.CreateBrush(100, 0, 255, 200, 1.5f);
            _escapeBlockedBrush = Hud.Render.CreateBrush(80, 255, 50, 50, 1f);
            _targetBrush = Hud.Render.CreateBrush(255, 255, 255, 0, 3f);
            _wallBrush = Hud.Render.CreateBrush(150, 255, 150, 0, 2f);
            _gridBrush = Hud.Render.CreateBrush(40, 50, 200, 50, 0.5f);
            _gridBlockedBrush = Hud.Render.CreateBrush(60, 255, 50, 50, 0.5f);
            _threatBrush = Hud.Render.CreateBrush(80, 255, 150, 150, 1.5f);
            _eliteThreatBrush = Hud.Render.CreateBrush(100, 255, 100, 255, 2f);
            _frontierBrush = Hud.Render.CreateBrush(150, 255, 200, 0, 2f);  // Yellow-orange for frontiers
            _portalBrush = Hud.Render.CreateBrush(200, 0, 255, 255, 3f);    // Cyan for portals
        }

        public void OnKeyEvent(IKeyEvent keyEvent)
        {
            if (!Hud.Game.IsInGame) return;

            if (ToggleOverlayKey.Matches(keyEvent) && keyEvent.IsPressed)
            {
                ShowOverlay = !ShowOverlay;
            }
            else if (ToggleAutoNavKey.Matches(keyEvent) && keyEvent.IsPressed)
            {
                AutoNavigate = !AutoNavigate;
                if (!AutoNavigate)
                {
                    _navigationTarget = null;
                    _currentPath = null;
                }
            }
        }

        public void AfterCollect()
        {
            if (!Hud.Game.IsInGame) return;
            if (Hud.Game.IsInTown) return;

            // Update spatial awareness
            _awareness.Update();

            // Auto-navigation logic
            if (AutoNavigate && _navigationTarget != null)
            {
                UpdateNavigation();
            }
        }

        private void UpdateNavigation()
        {
            var myPos = Hud.Game.Me.FloorCoordinate;

            // Check if reached target
            float distToTarget = myPos.XYDistanceTo(_navigationTarget);
            if (distToTarget < WAYPOINT_REACH_DIST)
            {
                _navigationTarget = null;
                _currentPath = null;
                return;
            }

            // Recalculate path periodically or if no path
            if (_currentPath == null || !_currentPath.IsValid ||
                _pathRecalcTimer.ElapsedMilliseconds > PATH_RECALC_INTERVAL)
            {
                RecalculatePath();
                _pathRecalcTimer.Restart();
            }

            // Follow path
            if (_currentPath != null && _currentPath.IsValid && _pathWaypointIndex < _currentPath.Path.Count)
            {
                FollowPath(myPos);
            }
        }

        private void RecalculatePath()
        {
            if (_navigationTarget == null) return;

            var myPos = Hud.Game.Me.FloorCoordinate;
            _currentPath = _pathfinder.FindSafePath(
                myPos.X, myPos.Y,
                _navigationTarget.X, _navigationTarget.Y
            );
            _pathWaypointIndex = 0;
        }

        private void FollowPath(IWorldCoordinate myPos)
        {
            if (_moveTimer.ElapsedMilliseconds < MOVE_INTERVAL) return;

            // Get current waypoint
            var waypoint = _currentPath.Path[_pathWaypointIndex];

            // Check if reached waypoint
            float dist = (float)Math.Sqrt(
                Math.Pow(myPos.X - waypoint.WorldX, 2) +
                Math.Pow(myPos.Y - waypoint.WorldY, 2)
            );

            if (dist < WAYPOINT_REACH_DIST)
            {
                _pathWaypointIndex++;
                if (_pathWaypointIndex >= _currentPath.Path.Count)
                    return;
                waypoint = _currentPath.Path[_pathWaypointIndex];
            }

            // Move toward waypoint
            var targetCoord = Hud.Window.CreateWorldCoordinate(waypoint.WorldX, waypoint.WorldY, myPos.Z);
            var screenPos = targetCoord.ToScreenCoordinate();

            if (screenPos.X > 10 && screenPos.X < Hud.Window.Size.Width - 10 &&
                screenPos.Y > 10 && screenPos.Y < Hud.Window.Size.Height - 10)
            {
                Hud.Interaction.MouseMove((int)screenPos.X, (int)screenPos.Y, 1, 1);
                Hud.Interaction.MouseDown(MouseButtons.Left);
                Hud.Wait(3);
                Hud.Interaction.MouseUp(MouseButtons.Left);

                _moveTimer.Restart();
            }
        }

        /// <summary>
        /// Set navigation target (can be called externally)
        /// </summary>
        public void NavigateTo(IWorldCoordinate target)
        {
            _navigationTarget = target;
            RecalculatePath();
            AutoNavigate = true;
        }

        /// <summary>
        /// Navigate to nearest safe zone
        /// </summary>
        public void NavigateToSafety()
        {
            var safeZone = _awareness.GetSafestPosition();
            if (safeZone != null)
            {
                var target = Hud.Window.CreateWorldCoordinate(safeZone.X, safeZone.Y, Hud.Game.Me.FloorCoordinate.Z);
                NavigateTo(target);
            }
        }

        public void PaintTopInGame(ClipState clipState)
        {
            if (clipState != ClipState.BeforeClip) return;
            if (!Hud.Game.IsInGame) return;

            // Draw panel only if enabled
            if (ShowPanel)
            {
                DrawStatusPanel();
            }

            if (!ShowOverlay) return;
            if (Hud.Game.IsInTown) return;

            var myPos = Hud.Game.Me.FloorCoordinate;

            if (ShowGrid) DrawGrid(myPos);
            if (ShowWalkability) DrawWalkabilityGrid(myPos);
            if (ShowDangerZones) DrawDangerZones();
            if (ShowThreatIndicators) DrawThreats();
            if (ShowSafeZones) DrawSafeZones(myPos);
            if (ShowEscapeRoutes) DrawEscapeRoutes(myPos);
            if (ShowWallMarkers) DrawWallMarkersFromNavMesh(myPos);
            if (ShowFrontiers) DrawFrontiers(myPos);  // NEW
            if (ShowPortals) DrawPortals(myPos);       // NEW
            if (ShowPath && _currentPath != null) DrawPath(myPos);
            if (_navigationTarget != null) DrawTarget();
        }

        private void DrawGrid(IWorldCoordinate myPos)
        {
            float cellSize = NavGrid.DEFAULT_CELL_SIZE;
            float range = 40f;

            for (float dx = -range; dx <= range; dx += cellSize)
            {
                for (float dy = -range; dy <= range; dy += cellSize)
                {
                    float x = myPos.X + dx;
                    float y = myPos.Y + dy;

                    var coord = Hud.Window.CreateWorldCoordinate(x, y, myPos.Z);
                    if (!coord.IsOnScreen()) continue;

                    var cell = _awareness.Grid.GetCellAtWorld(x, y);
                    if (cell != null && cell.IsWalkable && !cell.IsBlocked)
                    {
                        _gridBrush.DrawWorldEllipse(cellSize / 3, -1, coord);
                    }
                }
            }
        }

        /// <summary>
        /// Draw actual NavMesh walkability - shows real game walkable areas
        /// </summary>
        private void DrawWalkabilityGrid(IWorldCoordinate myPos)
        {
            float cellSize = 2.5f;
            float range = 30f;  // Smaller range for performance

            for (float dx = -range; dx <= range; dx += cellSize)
            {
                for (float dy = -range; dy <= range; dy += cellSize)
                {
                    float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                    if (dist > range) continue;
                    
                    float x = myPos.X + dx;
                    float y = myPos.Y + dy;

                    var coord = Hud.Window.CreateWorldCoordinate(x, y, myPos.Z);
                    if (!coord.IsOnScreen()) continue;

                    // Use actual NavMesh data
                    bool walkable = _awareness.NavMesh.IsWalkable(x, y);
                    
                    var brush = walkable ? _gridBrush : _gridBlockedBrush;
                    brush.DrawWorldEllipse(cellSize / 3f, -1, coord);
                }
            }
        }

        private void DrawDangerZones()
        {
            foreach (var danger in _awareness.DangerZones)
            {
                if (!danger.Position.IsOnScreen()) continue;

                IBrush brush;
                switch (danger.Level)
                {
                    case DangerLevel.Critical:
                        brush = _dangerBrushCritical;
                        break;
                    case DangerLevel.High:
                        brush = _dangerBrushHigh;
                        break;
                    case DangerLevel.Medium:
                        brush = _dangerBrushMedium;
                        break;
                    default:
                        brush = _dangerBrushLow;
                        break;
                }

                brush.DrawWorldEllipse(danger.Radius, -1, danger.Position);
            }
        }

        private void DrawThreats()
        {
            foreach (var threat in _awareness.Threats.Where(t => t.ThreatLevel >= ThreatLevel.Medium))
            {
                if (!threat.Position.IsOnScreen()) continue;

                var brush = threat.IsElite ? _eliteThreatBrush : _threatBrush;
                brush.DrawWorldEllipse(threat.ThreatRadius, -1, threat.Position);

                // Draw movement prediction
                if (threat.IsMovingTowardPlayer && threat.PredictedX != 0)
                {
                    var predictedPos = Hud.Window.CreateWorldCoordinate(
                        threat.PredictedX, threat.PredictedY, threat.Position.Z);
                    if (predictedPos.IsOnScreen())
                    {
                        brush.DrawLineWorld(threat.Position, predictedPos);
                    }
                }
            }
        }

        private void DrawSafeZones(IWorldCoordinate myPos)
        {
            int shown = 0;
            foreach (var safe in _awareness.SafeZones.Take(5))
            {
                var coord = Hud.Window.CreateWorldCoordinate(safe.X, safe.Y, myPos.Z);
                if (!coord.IsOnScreen()) continue;

                float size = 2f + (safe.SafetyScore / 100f) * 2f;
                _safeBrush.DrawWorldEllipse(size, -1, coord);
                shown++;
            }
        }

        /// <summary>
        /// Draw exploration frontiers (unexplored edges to explore)
        /// </summary>
        private void DrawFrontiers(IWorldCoordinate myPos)
        {
            foreach (var frontier in _awareness.NavMesh.Frontiers.Take(5))
            {
                var coord = Hud.Window.CreateWorldCoordinate(frontier.X, frontier.Y, myPos.Z);
                if (!coord.IsOnScreen()) continue;

                // Size based on priority
                float size = 1.5f + (frontier.Priority / 50f);
                _frontierBrush.DrawWorldEllipse(size, -1, coord);
                
                // Draw arrow showing direction
                float arrowLen = 3f;
                float rad = frontier.Direction * (float)Math.PI / 180f;
                float endX = frontier.X + (float)Math.Cos(rad) * arrowLen;
                float endY = frontier.Y + (float)Math.Sin(rad) * arrowLen;
                var endCoord = Hud.Window.CreateWorldCoordinate(endX, endY, myPos.Z);
                
                if (endCoord.IsOnScreen())
                {
                    _frontierBrush.DrawLineWorld(coord, endCoord);
                }
            }
            
            // Highlight best frontier
            var best = _awareness.NavMesh.GetBestFrontier();
            if (best != null)
            {
                var bestCoord = Hud.Window.CreateWorldCoordinate(best.X, best.Y, myPos.Z);
                if (bestCoord.IsOnScreen())
                {
                    _pathBrush.DrawWorldEllipse(3f, -1, bestCoord);
                }
            }
        }

        /// <summary>
        /// Draw detected portals
        /// </summary>
        private void DrawPortals(IWorldCoordinate myPos)
        {
            foreach (var portal in _awareness.NavMesh.DetectedPortals)
            {
                if (!portal.Position.IsOnScreen()) continue;

                // Different sizes for different portal types
                float size = portal.Type == GizmoType.BossPortal ? 4f : 3f;
                
                _portalBrush.DrawWorldEllipse(size, -1, portal.Position);
                
                // Pulsing effect for boss portals
                if (portal.Type == GizmoType.BossPortal)
                {
                    float pulse = (float)(Math.Sin(Hud.Game.CurrentRealTimeMilliseconds / 300.0) + 1) / 2f;
                    _portalBrush.DrawWorldEllipse(size + pulse * 2f, -1, portal.Position);
                }
            }
        }

        private void DrawEscapeRoutes(IWorldCoordinate myPos)
        {
            foreach (var route in _awareness.EscapeRoutes)
            {
                var targetCoord = Hud.Window.CreateWorldCoordinate(route.TargetX, route.TargetY, myPos.Z);
                if (!targetCoord.IsOnScreen()) continue;

                var brush = route.IsBlocked ? _escapeBlockedBrush : _escapeBrush;

                // Size based on safety score
                float size = route.IsBlocked ? 0.8f : 1f + Math.Max(0, route.SafetyScore / 100f);
                brush.DrawWorldEllipse(size, -1, targetCoord);
            }

            // Highlight best escape route
            var best = _awareness.GetBestEscapeRoute();
            if (best != null && !best.IsBlocked)
            {
                var bestCoord = Hud.Window.CreateWorldCoordinate(best.TargetX, best.TargetY, myPos.Z);
                _pathBrush.DrawWorldEllipse(2.5f, -1, bestCoord);
                _pathBrush.DrawLineWorld(myPos, bestCoord);
            }
        }

        /// <summary>
        /// Draw wall markers using actual NavMesh walkability checks
        /// </summary>
        private void DrawWallMarkersFromNavMesh(IWorldCoordinate myPos)
        {
            float checkDist = _awareness.WallDetectionRange;
            int directions = 16;
            
            for (int i = 0; i < directions; i++)
            {
                float angle = i * (360f / directions);
                float radians = angle * (float)Math.PI / 180f;
                
                // Check along this direction for first non-walkable point
                for (float dist = 3f; dist <= checkDist; dist += 2f)
                {
                    float checkX = myPos.X + (float)Math.Cos(radians) * dist;
                    float checkY = myPos.Y + (float)Math.Sin(radians) * dist;
                    
                    if (!_awareness.NavMesh.IsWalkable(checkX, checkY))
                    {
                        // Found a wall/non-walkable area
                        var wallPoint = Hud.Window.CreateWorldCoordinate(checkX, checkY, myPos.Z);
                        if (wallPoint.IsOnScreen())
                        {
                            _wallBrush.DrawWorldEllipse(1.5f, -1, wallPoint);
                        }
                        break; // Only mark the first wall in this direction
                    }
                }
            }
        }

        // Keep old method for fallback
        private void DrawWallMarkers()
        {
            var scene = Hud.Game.Me.Scene;
            if (scene == null) return;

            var myPos = Hud.Game.Me.FloorCoordinate;
            float markerDist = _awareness.WallDetectionRange;

            DrawWallMarkerIfNear(myPos, scene.PosX, myPos.Y, myPos.X - scene.PosX, markerDist);
            DrawWallMarkerIfNear(myPos, scene.MaxX, myPos.Y, scene.MaxX - myPos.X, markerDist);
            DrawWallMarkerIfNear(myPos, myPos.X, scene.PosY, myPos.Y - scene.PosY, markerDist);
            DrawWallMarkerIfNear(myPos, myPos.X, scene.MaxY, scene.MaxY - myPos.Y, markerDist);
        }

        private void DrawWallMarkerIfNear(IWorldCoordinate myPos, float x, float y, float dist, float maxDist)
        {
            if (dist < maxDist && dist > 0)
            {
                var wallPoint = Hud.Window.CreateWorldCoordinate(x, y, myPos.Z);
                if (wallPoint.IsOnScreen())
                    _wallBrush.DrawWorldEllipse(2f, -1, wallPoint);
            }
        }

        private void DrawPath(IWorldCoordinate myPos)
        {
            if (_currentPath == null || !_currentPath.IsValid) return;

            IWorldCoordinate prev = myPos;
            for (int i = _pathWaypointIndex; i < _currentPath.Path.Count; i++)
            {
                var wp = _currentPath.Path[i];
                var coord = Hud.Window.CreateWorldCoordinate(wp.WorldX, wp.WorldY, myPos.Z);

                if (prev.IsOnScreen() || coord.IsOnScreen())
                {
                    _pathBrush.DrawLineWorld(prev, coord);
                    _pathBrush.DrawWorldEllipse(1f, -1, coord);
                }

                prev = coord;
            }
        }

        private void DrawTarget()
        {
            if (_navigationTarget == null || !_navigationTarget.IsOnScreen()) return;

            _targetBrush.DrawWorldEllipse(3f, -1, _navigationTarget);
            
            // Pulsing effect
            float pulse = (float)(Math.Sin(Hud.Game.CurrentRealTimeMilliseconds / 200.0) + 1) / 2f;
            _targetBrush.DrawWorldEllipse(3f + pulse * 2f, -1, _navigationTarget);
        }

        private void DrawStatusPanel()
        {
            float x = Hud.Window.Size.Width * PanelX;
            float y = Hud.Window.Size.Height * PanelY;
            float w = 155;
            float h = ShowOverlay && !Hud.Game.IsInTown ? 135 : 45;
            float pad = 5;

            _panelBrush.DrawRectangle(x, y, w, h);
            _borderBrush.DrawRectangle(x, y, w, h);

            var accentBrush = ShowOverlay ? _accentOnBrush : _accentOffBrush;
            accentBrush.DrawRectangle(x, y, 3, h);

            float tx = x + pad + 3;
            float ty = y + pad;

            var title = _titleFont.GetTextLayout("Navigation");
            _titleFont.DrawText(title, tx, ty);
            ty += title.Metrics.Height + 1;

            string statusStr = ShowOverlay ? (AutoNavigate ? "● Auto-Nav" : "● Overlay") : "OFF";
            var status = _statusFont.GetTextLayout(statusStr);
            _statusFont.DrawText(status, tx, ty);
            ty += status.Metrics.Height;

            var hint = _infoFont.GetTextLayout("[Ctrl+N]");
            _infoFont.DrawText(hint, tx, ty);

            if (ShowOverlay && !Hud.Game.IsInTown)
            {
                ty += hint.Metrics.Height;
                
                // Show tile info and learning
                var navMesh = _awareness.NavMesh;
                var tileInfo = _infoFont.GetTextLayout($"Tiles:{navMesh.MasterTilesLearned} Sc:{navMesh.RuntimeScenesActive}");
                _infoFont.DrawText(tileInfo, tx, ty);
                ty += tileInfo.Metrics.Height;
                
                // Exploration progress
                var expInfo = _infoFont.GetTextLayout($"Explored:{navMesh.ExplorationPercent:F0}% W:{navMesh.WalkableCellsLearned}");
                _infoFont.DrawText(expInfo, tx, ty);
                ty += expInfo.Metrics.Height;
                
                // Floor/Z level
                var floorInfo = _infoFont.GetTextLayout($"Z:{navMesh.PlayerZ:F0} Floor:{navMesh.PlayerFloorLevel}");
                _infoFont.DrawText(floorInfo, tx, ty);
                ty += floorInfo.Metrics.Height;
                
                // Frontiers and portals
                int frontierCount = navMesh.Frontiers.Count;
                int portalCount = navMesh.DetectedPortals.Count;
                var fpInfo = _infoFont.GetTextLayout($"Front:{frontierCount} Port:{portalCount}");
                _infoFont.DrawText(fpInfo, tx, ty);
                ty += fpInfo.Metrics.Height;
                
                // Dangers and threats
                var dangers = _infoFont.GetTextLayout($"D:{_awareness.DangerZones.Count} T:{_awareness.Threats.Count}");
                _infoFont.DrawText(dangers, tx, ty);
                ty += dangers.Metrics.Height;

                int validEscapes = _awareness.EscapeRoutes.Count(r => !r.IsBlocked && r.SafetyScore > 0);
                var escapes = _infoFont.GetTextLayout($"Safe:{_awareness.SafeZones.Count} Esc:{validEscapes}");
                _infoFont.DrawText(escapes, tx, ty);

                if (_currentPath != null && _currentPath.IsValid)
                {
                    ty += escapes.Metrics.Height;
                    var pathInfo = _infoFont.GetTextLayout($"Path:{_currentPath.Path.Count - _pathWaypointIndex}");
                    _infoFont.DrawText(pathInfo, tx, ty);
                }
            }
        }

        /// <summary>
        /// Get the spatial awareness engine for external use
        /// </summary>
        public SpatialAwarenessEngine GetAwareness() => _awareness;
    }
}
