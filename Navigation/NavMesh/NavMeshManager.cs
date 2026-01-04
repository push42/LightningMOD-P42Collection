namespace Turbo.Plugins.Custom.Navigation.NavMesh
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Turbo.Plugins.Default;
    using Newtonsoft.Json;

    /// <summary>
    /// GOD-TIER NavMesh Manager - Advanced runtime navigation learning system
    /// 
    /// Features:
    /// - Runtime walkability learning from player movement
    /// - Scene/tile type recognition for GR maps
    /// - Multi-floor Z-level tracking
    /// - Exploration frontier detection
    /// - Portal and exit tracking
    /// - Blocked area detection via movement failure
    /// - Scene connection mapping
    /// - Optimized persistent caching by SceneSnoId (tile type)
    /// - Stuck detection and area blacklisting
    /// - Exploration coverage tracking
    /// </summary>
    public class NavMeshManager
    {
        public IController Hud { get; set; }
        
        // Runtime navigation data per scene instance
        private Dictionary<uint, RuntimeSceneNav> _sceneNavs = new Dictionary<uint, RuntimeSceneNav>();
        
        // Learned data cache by SceneSnoId (tile type, not instance)
        // This allows learning to transfer across same tile types
        private Dictionary<uint, MasterNavData> _masterNavData = new Dictionary<uint, MasterNavData>();
        
        // Scene connections (which scenes connect to which)
        private Dictionary<uint, HashSet<uint>> _sceneConnections = new Dictionary<uint, HashSet<uint>>();
        
        // Current world tracking
        public uint CurrentWorldSno { get; private set; }
        public bool IsInRift { get; private set; }
        
        // Player tracking for learning
        private Queue<PlayerSnapshot> _playerHistory = new Queue<PlayerSnapshot>();
        private const int MAX_HISTORY = 200;
        private float _lastRecordedX, _lastRecordedY, _lastRecordedZ;
        private uint _lastSceneId;
        private const float RECORD_DISTANCE = 1.5f; // Record every 1.5 units
        
        // Stuck detection
        private IWorldCoordinate _lastMoveTarget;
        private int _stuckCounter;
        private HashSet<(int, int, uint)> _blacklistedCells = new HashSet<(int, int, uint)>();
        
        // Performance stats
        public int RuntimeScenesActive => _sceneNavs.Count;
        public int MasterTilesLearned => _masterNavData.Count;
        public int TotalCellsLearned { get; private set; }
        public int WalkableCellsLearned { get; private set; }
        public float ExplorationPercent { get; private set; }
        
        // Current player info
        public float PlayerZ { get; private set; }
        public int PlayerFloorLevel { get; private set; }
        
        // Exploration tracking
        public List<ExplorationFrontier> Frontiers { get; private set; } = new List<ExplorationFrontier>();
        public List<PortalInfo> DetectedPortals { get; private set; } = new List<PortalInfo>();
        
        // Debug info
        public string LastUpdateInfo { get; private set; } = "Not initialized";
        
        // Cache paths
        private string _cachePath;
        private string _masterCachePath;

        public NavMeshManager()
        {
            var docsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            _cachePath = Path.Combine(docsPath, "TurboHUD", "NavCache");
            _masterCachePath = Path.Combine(docsPath, "TurboHUD", "NavCache", "Master");
        }

        /// <summary>
        /// Initialize the manager
        /// </summary>
        public void Initialize()
        {
            _sceneNavs.Clear();
            _sceneConnections.Clear();
            TotalCellsLearned = 0;
            WalkableCellsLearned = 0;
            _blacklistedCells.Clear();
            
            // Create cache directories
            try
            {
                Directory.CreateDirectory(_cachePath);
                Directory.CreateDirectory(_masterCachePath);
            }
            catch { }
            
            // Load master nav data (tile type learning)
            LoadMasterNavData();
        }

        /// <summary>
        /// Update runtime navigation data based on current game state
        /// </summary>
        public void Update()
        {
            if (Hud?.Game?.Me == null || !Hud.Game.IsInGame) 
            {
                LastUpdateInfo = "Not in game";
                return;
            }

            var player = Hud.Game.Me;
            var currentScene = player.Scene;
            if (currentScene == null) 
            {
                LastUpdateInfo = "No current scene";
                return;
            }

            // Update player position info
            var myPos = player.FloorCoordinate;
            PlayerZ = myPos.Z;
            PlayerFloorLevel = (int)(myPos.Z / 10f);

            uint worldSno = currentScene.WorldSno;
            
            // Check world change
            if (worldSno != CurrentWorldSno)
            {
                OnWorldChange(worldSno);
            }

            // Detect if in rift
            IsInRift = IsRiftWorld(worldSno);

            // Get or create scene nav
            if (!_sceneNavs.TryGetValue(currentScene.SceneId, out var sceneNav))
            {
                sceneNav = CreateRuntimeSceneNav(currentScene);
                _sceneNavs[currentScene.SceneId] = sceneNav;
            }

            // Track scene transitions
            if (_lastSceneId != 0 && _lastSceneId != currentScene.SceneId)
            {
                RecordSceneConnection(_lastSceneId, currentScene.SceneId);
            }
            _lastSceneId = currentScene.SceneId;

            // Learn from player movement
            LearnFromPlayerPosition(myPos, currentScene, sceneNav);
            
            // Update dynamic obstacles
            sceneNav.UpdateDynamicObstacles(Hud, myPos);
            
            // Detect portals
            UpdatePortalDetection();
            
            // Update exploration frontiers
            UpdateExplorationFrontiers(myPos, sceneNav);
            
            // Calculate exploration percentage
            CalculateExplorationPercent(sceneNav);

            int walkable = sceneNav.WalkableCellCount;
            int total = sceneNav.TotalCellCount;
            LastUpdateInfo = $"Tile:{sceneNav.SceneSnoId} Walk:{walkable}/{total} Z:{myPos.Z:F0} Exp:{ExplorationPercent:F0}%";
        }

        private void OnWorldChange(uint newWorldSno)
        {
            // Save current learning before clearing
            SaveAllLearnedData();
            
            CurrentWorldSno = newWorldSno;
            _sceneNavs.Clear();
            _sceneConnections.Clear();
            _lastSceneId = 0;
            Frontiers.Clear();
            DetectedPortals.Clear();
        }

        private bool IsRiftWorld(uint worldSno)
        {
            // GR world SNOs - these are the rift worlds
            return worldSno >= 288482 && worldSno <= 288684; // Approximate GR world range
        }

        private RuntimeSceneNav CreateRuntimeSceneNav(IScene scene)
        {
            var runtimeNav = new RuntimeSceneNav
            {
                SceneId = scene.SceneId,
                SceneSnoId = scene.SnoScene?.Sno ?? 0,
                NavMeshId = scene.NavMeshId,
                MinX = scene.PosX,
                MinY = scene.PosY,
                MaxX = scene.MaxX,
                MaxY = scene.MaxY,
                Width = scene.W,
                Height = scene.H,
                BaseZ = scene.Z,
                SceneCode = scene.SnoScene?.Code ?? "Unknown"
            };

            // Initialize grid
            runtimeNav.InitializeGrid();
            
            // Apply master nav data for this tile type (learned from previous runs)
            if (runtimeNav.SceneSnoId != 0 && _masterNavData.TryGetValue(runtimeNav.SceneSnoId, out var master))
            {
                runtimeNav.ApplyMasterData(master);
            }

            return runtimeNav;
        }

        /// <summary>
        /// Learn walkability from player's actual movement
        /// </summary>
        private void LearnFromPlayerPosition(IWorldCoordinate pos, IScene scene, RuntimeSceneNav sceneNav)
        {
            float dist = (float)Math.Sqrt(
                Math.Pow(pos.X - _lastRecordedX, 2) + 
                Math.Pow(pos.Y - _lastRecordedY, 2));
            
            if (dist < RECORD_DISTANCE) return;
            
            // Record as walkable
            _lastRecordedX = pos.X;
            _lastRecordedY = pos.Y;
            _lastRecordedZ = pos.Z;
            
            // Mark walkable with radius (player has some width)
            sceneNav.MarkWalkableArea(pos.X, pos.Y, pos.Z, radius: 1.5f);
            
            // Mark path from last position
            if (_playerHistory.Count > 0)
            {
                var last = _playerHistory.Last();
                if (last.SceneId == scene.SceneId)
                {
                    MarkPathWalkable(sceneNav, last.X, last.Y, pos.X, pos.Y, pos.Z);
                }
            }
            
            // Add to history
            _playerHistory.Enqueue(new PlayerSnapshot 
            { 
                X = pos.X, 
                Y = pos.Y, 
                Z = pos.Z,
                SceneId = scene.SceneId,
                Tick = Hud.Game.CurrentGameTick
            });
            
            while (_playerHistory.Count > MAX_HISTORY)
                _playerHistory.Dequeue();
            
            WalkableCellsLearned = sceneNav.WalkableCellCount;
            TotalCellsLearned = _sceneNavs.Values.Sum(s => s.WalkableCellCount);
        }

        private void MarkPathWalkable(RuntimeSceneNav sceneNav, float x1, float y1, float x2, float y2, float z)
        {
            float dist = (float)Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
            int steps = Math.Max(1, (int)(dist / 0.8f));
            
            float dx = (x2 - x1) / steps;
            float dy = (y2 - y1) / steps;
            
            for (int i = 0; i <= steps; i++)
            {
                float x = x1 + dx * i;
                float y = y1 + dy * i;
                sceneNav.MarkWalkableArea(x, y, z, radius: 1f);
            }
        }

        private void RecordSceneConnection(uint fromScene, uint toScene)
        {
            if (!_sceneConnections.TryGetValue(fromScene, out var connections))
            {
                connections = new HashSet<uint>();
                _sceneConnections[fromScene] = connections;
            }
            connections.Add(toScene);
            
            // Bidirectional
            if (!_sceneConnections.TryGetValue(toScene, out connections))
            {
                connections = new HashSet<uint>();
                _sceneConnections[toScene] = connections;
            }
            connections.Add(fromScene);
        }

        private void UpdatePortalDetection()
        {
            DetectedPortals.Clear();
            
            foreach (var actor in Hud.Game.Actors)
            {
                if (actor.GizmoType == GizmoType.Portal ||
                    actor.GizmoType == GizmoType.BossPortal ||
                    actor.GizmoType == GizmoType.DungeonPortal ||
                    actor.GizmoType == GizmoType.ReturnPortal)
                {
                    DetectedPortals.Add(new PortalInfo
                    {
                        Position = actor.FloorCoordinate,
                        Type = actor.GizmoType,
                        Name = actor.SnoActor?.NameLocalized ?? "Portal",
                        SceneId = Hud.Game.Me.Scene?.SceneId ?? 0
                    });
                }
            }
        }

        private void UpdateExplorationFrontiers(IWorldCoordinate myPos, RuntimeSceneNav sceneNav)
        {
            Frontiers.Clear();
            
            // Find cells at the edge of explored area that might lead somewhere
            var frontierCells = sceneNav.GetFrontierCells(myPos.Z);
            
            foreach (var cell in frontierCells.Take(8)) // Limit to top 8 frontiers
            {
                var (worldX, worldY) = sceneNav.GridToWorld(cell.gx, cell.gy);
                float dist = (float)Math.Sqrt(Math.Pow(worldX - myPos.X, 2) + Math.Pow(worldY - myPos.Y, 2));
                
                Frontiers.Add(new ExplorationFrontier
                {
                    X = worldX,
                    Y = worldY,
                    Z = cell.z,
                    Distance = dist,
                    Priority = cell.priority,
                    Direction = (float)Math.Atan2(worldY - myPos.Y, worldX - myPos.X) * 180f / (float)Math.PI
                });
            }
            
            // Sort by priority (weighted by distance)
            Frontiers.Sort((a, b) => (b.Priority / (1 + b.Distance * 0.1f))
                .CompareTo(a.Priority / (1 + a.Distance * 0.1f)));
        }

        private void CalculateExplorationPercent(RuntimeSceneNav sceneNav)
        {
            int explored = sceneNav.WalkableCellCount;
            int estimated = sceneNav.EstimatedWalkableCells;
            
            if (estimated > 0)
            {
                ExplorationPercent = Math.Min(100f, (explored * 100f) / estimated);
            }
            else
            {
                ExplorationPercent = 0;
            }
        }

        #region Public API

        /// <summary>
        /// Check if a position is walkable
        /// </summary>
        public bool IsWalkable(float x, float y)
        {
            // Check blacklist first
            var scene = GetSceneNavAt(x, y);
            if (scene != null)
            {
                var (gx, gy) = scene.WorldToGrid(x, y);
                if (_blacklistedCells.Contains((gx, gy, scene.SceneId)))
                    return false;
            }
            
            if (scene == null)
                return false;
            return scene.IsWalkable(x, y, PlayerZ);
        }

        /// <summary>
        /// Check if a position is walkable at specific Z level
        /// </summary>
        public bool IsWalkable(float x, float y, float z)
        {
            var scene = GetSceneNavAt(x, y);
            if (scene == null)
                return false;
            return scene.IsWalkable(x, y, z);
        }

        /// <summary>
        /// Report that movement to a position failed (stuck detection)
        /// </summary>
        public void ReportMovementFailed(float x, float y)
        {
            var scene = GetSceneNavAt(x, y);
            if (scene != null)
            {
                var (gx, gy) = scene.WorldToGrid(x, y);
                scene.MarkBlocked(x, y);
                _blacklistedCells.Add((gx, gy, scene.SceneId));
            }
        }

        /// <summary>
        /// Get the RuntimeSceneNav containing the given position
        /// </summary>
        public RuntimeSceneNav GetSceneNavAt(float x, float y)
        {
            foreach (var nav in _sceneNavs.Values)
            {
                if (x >= nav.MinX && x <= nav.MaxX && y >= nav.MinY && y <= nav.MaxY)
                    return nav;
            }
            return null;
        }

        /// <summary>
        /// Get all walkable positions in a radius
        /// </summary>
        public IEnumerable<(float x, float y)> GetWalkablePositions(float centerX, float centerY, float radius, float step = 2.5f)
        {
            for (float dx = -radius; dx <= radius; dx += step)
            {
                for (float dy = -radius; dy <= radius; dy += step)
                {
                    if (dx * dx + dy * dy > radius * radius) continue;
                    
                    float x = centerX + dx;
                    float y = centerY + dy;
                    
                    if (IsWalkable(x, y))
                        yield return (x, y);
                }
            }
        }

        /// <summary>
        /// Check if there's a clear walkable path between two points
        /// </summary>
        public bool HasClearPath(float x1, float y1, float x2, float y2)
        {
            float dist = (float)Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
            int steps = Math.Max(3, (int)(dist / 1.5f));
            
            float dx = (x2 - x1) / steps;
            float dy = (y2 - y1) / steps;
            
            for (int i = 1; i <= steps; i++)
            {
                float checkX = x1 + dx * i;
                float checkY = y1 + dy * i;
                
                if (!IsWalkable(checkX, checkY))
                    return false;
            }
            
            return true;
        }

        /// <summary>
        /// Find nearest walkable position
        /// </summary>
        public (float x, float y)? FindNearestWalkable(float x, float y, float maxRadius = 20f)
        {
            if (IsWalkable(x, y)) return (x, y);

            float bestDist = float.MaxValue;
            (float x, float y)? best = null;

            for (float r = 2f; r <= maxRadius; r += 2f)
            {
                foreach (var pos in GetWalkablePositions(x, y, r, 2f))
                {
                    float dist = (float)Math.Sqrt(Math.Pow(pos.x - x, 2) + Math.Pow(pos.y - y, 2));
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        best = pos;
                    }
                }
                
                if (best.HasValue) break;
            }

            return best;
        }

        /// <summary>
        /// Get best exploration frontier
        /// </summary>
        public ExplorationFrontier GetBestFrontier()
        {
            return Frontiers.FirstOrDefault();
        }

        /// <summary>
        /// Get nearest portal
        /// </summary>
        public PortalInfo GetNearestPortal(float x, float y)
        {
            return DetectedPortals
                .OrderBy(p => Math.Sqrt(Math.Pow(p.Position.X - x, 2) + Math.Pow(p.Position.Y - y, 2)))
                .FirstOrDefault();
        }

        #endregion

        #region Data Persistence

        /// <summary>
        /// Save all learned data
        /// </summary>
        public void SaveAllLearnedData()
        {
            try
            {
                // Save master data (by tile type) for transfer learning
                foreach (var kvp in _sceneNavs)
                {
                    var sceneSnoId = kvp.Value.SceneSnoId;
                    if (sceneSnoId == 0) continue;
                    
                    var masterData = kvp.Value.ExtractMasterData();
                    
                    // Merge with existing master data
                    if (_masterNavData.TryGetValue(sceneSnoId, out var existing))
                    {
                        existing.MergeWith(masterData);
                    }
                    else
                    {
                        _masterNavData[sceneSnoId] = masterData;
                    }
                }
                
                // Save to disk
                SaveMasterNavData();
            }
            catch { }
        }

        private void SaveMasterNavData()
        {
            try
            {
                foreach (var kvp in _masterNavData)
                {
                    if (kvp.Value.WalkableCells.Count == 0) continue;
                    
                    var path = Path.Combine(_masterCachePath, $"tile_{kvp.Key}.json");
                    var json = JsonConvert.SerializeObject(kvp.Value, Formatting.None);
                    File.WriteAllText(path, json);
                }
            }
            catch { }
        }

        private void LoadMasterNavData()
        {
            _masterNavData.Clear();
            
            try
            {
                if (!Directory.Exists(_masterCachePath)) return;
                
                foreach (var file in Directory.GetFiles(_masterCachePath, "tile_*.json"))
                {
                    var json = File.ReadAllText(file);
                    var data = JsonConvert.DeserializeObject<MasterNavData>(json);
                    if (data != null && data.SceneSnoId != 0)
                    {
                        _masterNavData[data.SceneSnoId] = data;
                    }
                }
            }
            catch { }
        }

        #endregion
    }

    /// <summary>
    /// Runtime navigation data for a single scene instance
    /// </summary>
    public class RuntimeSceneNav
    {
        public uint SceneId { get; set; }
        public uint SceneSnoId { get; set; }
        public uint NavMeshId { get; set; }
        public string SceneCode { get; set; }
        
        public float MinX { get; set; }
        public float MinY { get; set; }
        public float MaxX { get; set; }
        public float MaxY { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public float BaseZ { get; set; }

        // Grid settings
        private const float CELL_SIZE = 2.0f; // Smaller cells for better accuracy
        private const float Z_TOLERANCE = 12f;
        
        private int _gridWidth;
        private int _gridHeight;
        
        // Cell data
        private Dictionary<(int, int), NavCellData> _cells = new Dictionary<(int, int), NavCellData>();
        private HashSet<(int, int)> _dynamicBlocked = new HashSet<(int, int)>();
        private HashSet<(int, int)> _exploredCells = new HashSet<(int, int)>();

        public int WalkableCellCount => _cells.Count(c => c.Value.IsWalkable && c.Value.IsConfirmed);
        public int BlockedCellCount => _cells.Count(c => !c.Value.IsWalkable);
        public int TotalCellCount => _gridWidth * _gridHeight;
        public int EstimatedWalkableCells => (int)(TotalCellCount * 0.6f); // Estimate 60% of scene is walkable

        public void InitializeGrid()
        {
            _gridWidth = Math.Max(1, (int)Math.Ceiling(Width / CELL_SIZE));
            _gridHeight = Math.Max(1, (int)Math.Ceiling(Height / CELL_SIZE));
            _cells.Clear();
            _exploredCells.Clear();
            
            // Mark edge cells as blocked
            float edgeBuffer = 3f;
            for (int gx = 0; gx < _gridWidth; gx++)
            {
                for (int gy = 0; gy < _gridHeight; gy++)
                {
                    var (worldX, worldY) = GridToWorld(gx, gy);
                    
                    float distToLeft = worldX - MinX;
                    float distToRight = MaxX - worldX;
                    float distToBottom = worldY - MinY;
                    float distToTop = MaxY - worldY;
                    
                    bool isEdge = distToLeft < edgeBuffer || distToRight < edgeBuffer ||
                                  distToBottom < edgeBuffer || distToTop < edgeBuffer;
                    
                    if (isEdge)
                    {
                        _cells[(gx, gy)] = new NavCellData 
                        { 
                            IsWalkable = false, 
                            IsEdge = true,
                            Z = BaseZ 
                        };
                    }
                }
            }
        }

        /// <summary>
        /// Mark an area as walkable
        /// </summary>
        public void MarkWalkableArea(float worldX, float worldY, float z, float radius)
        {
            int cellRadius = (int)Math.Ceiling(radius / CELL_SIZE);
            var (centerGX, centerGY) = WorldToGrid(worldX, worldY);
            
            for (int dx = -cellRadius; dx <= cellRadius; dx++)
            {
                for (int dy = -cellRadius; dy <= cellRadius; dy++)
                {
                    int gx = centerGX + dx;
                    int gy = centerGY + dy;
                    
                    if (gx < 0 || gx >= _gridWidth || gy < 0 || gy >= _gridHeight) continue;
                    
                    float dist = (float)Math.Sqrt(dx * dx + dy * dy) * CELL_SIZE;
                    if (dist <= radius)
                    {
                        MarkWalkable(gx, gy, z);
                    }
                }
            }
        }

        private void MarkWalkable(int gx, int gy, float z)
        {
            if (!_cells.TryGetValue((gx, gy), out var cell))
            {
                cell = new NavCellData();
                _cells[(gx, gy)] = cell;
            }
            
            if (!cell.IsEdge)
            {
                cell.IsWalkable = true;
                cell.IsConfirmed = true;
                cell.Z = z;
                cell.LastSeen = DateTime.UtcNow.Ticks;
                cell.VisitCount++;
                _exploredCells.Add((gx, gy));
            }
        }

        public void MarkBlocked(float worldX, float worldY)
        {
            var (gx, gy) = WorldToGrid(worldX, worldY);
            
            if (gx < 0 || gx >= _gridWidth || gy < 0 || gy >= _gridHeight) return;
            
            if (!_cells.TryGetValue((gx, gy), out var cell))
            {
                cell = new NavCellData();
                _cells[(gx, gy)] = cell;
            }
            
            cell.IsWalkable = false;
            cell.IsConfirmed = true;
        }

        public bool IsWalkable(float worldX, float worldY, float playerZ)
        {
            if (worldX < MinX || worldX > MaxX || worldY < MinY || worldY > MaxY)
                return false;
            
            var (gx, gy) = WorldToGrid(worldX, worldY);
            
            if (gx < 0 || gx >= _gridWidth || gy < 0 || gy >= _gridHeight)
                return false;

            if (_dynamicBlocked.Contains((gx, gy)))
                return false;
            
            if (_cells.TryGetValue((gx, gy), out var cell))
            {
                if (!cell.IsWalkable) return false;
                if (cell.IsConfirmed && Math.Abs(cell.Z - playerZ) > Z_TOLERANCE)
                    return false;
                return true;
            }
            
            // Unknown cell - check if near edge
            float distToLeft = worldX - MinX;
            float distToRight = MaxX - worldX;
            float distToBottom = worldY - MinY;
            float distToTop = MaxY - worldY;
            
            bool nearEdge = distToLeft < 5f || distToRight < 5f || 
                           distToBottom < 5f || distToTop < 5f;
            
            return !nearEdge;
        }

        /// <summary>
        /// Get frontier cells (unexplored cells adjacent to explored cells)
        /// </summary>
        public IEnumerable<(int gx, int gy, float z, float priority)> GetFrontierCells(float playerZ)
        {
            var frontiers = new List<(int gx, int gy, float z, float priority)>();
            var visited = new HashSet<(int, int)>();
            
            foreach (var explored in _exploredCells)
            {
                // Check neighbors
                int[] dx = { -1, 1, 0, 0, -1, 1, -1, 1 };
                int[] dy = { 0, 0, -1, 1, -1, -1, 1, 1 };
                
                for (int i = 0; i < 8; i++)
                {
                    int nx = explored.Item1 + dx[i];
                    int ny = explored.Item2 + dy[i];
                    
                    if (nx < 0 || nx >= _gridWidth || ny < 0 || ny >= _gridHeight) continue;
                    if (visited.Contains((nx, ny))) continue;
                    if (_exploredCells.Contains((nx, ny))) continue;
                    
                    // Check if this cell could be walkable
                    if (_cells.TryGetValue((nx, ny), out var cell))
                    {
                        if (!cell.IsWalkable || cell.IsEdge) continue;
                    }
                    
                    visited.Add((nx, ny));
                    
                    // Calculate priority based on distance from center and unexplored neighbors
                    var (worldX, worldY) = GridToWorld(nx, ny);
                    float centerDist = (float)Math.Sqrt(
                        Math.Pow(worldX - (MinX + Width / 2), 2) + 
                        Math.Pow(worldY - (MinY + Height / 2), 2));
                    
                    // Prefer cells away from center (toward unexplored areas)
                    float priority = centerDist;
                    
                    frontiers.Add((nx, ny, playerZ, priority));
                }
            }
            
            return frontiers.OrderByDescending(f => f.priority);
        }

        public void UpdateDynamicObstacles(IController hud, IWorldCoordinate playerPos)
        {
            _dynamicBlocked.Clear();
            
            foreach (var actor in hud.Game.Actors)
            {
                if (actor.FloorCoordinate == null) continue;
                
                float ax = actor.FloorCoordinate.X;
                float ay = actor.FloorCoordinate.Y;
                float az = actor.FloorCoordinate.Z;
                
                if (ax < MinX || ax > MaxX || ay < MinY || ay > MaxY) continue;
                if (Math.Abs(az - playerPos.Z) > Z_TOLERANCE) continue;
                
                if (IsBlockingActor(actor))
                {
                    float radius = Math.Max(2f, actor.RadiusBottom);
                    MarkDynamicObstacle(ax, ay, radius);
                }
            }
        }

        private bool IsBlockingActor(IActor actor)
        {
            if (actor.GizmoType == GizmoType.Door ||
                actor.GizmoType == GizmoType.Gate ||
                actor.GizmoType == GizmoType.BreakableDoor ||
                actor.GizmoType == GizmoType.DestroyableObject)
            {
                return true;
            }
            
            string code = (actor.SnoActor?.Code ?? "").ToLowerInvariant();
            return code.Contains("wall") ||
                   code.Contains("block") ||
                   code.Contains("fence") ||
                   code.Contains("barrier") ||
                   code.Contains("pillar");
        }

        private void MarkDynamicObstacle(float worldX, float worldY, float radius)
        {
            int cellRadius = (int)Math.Ceiling(radius / CELL_SIZE);
            var (centerGX, centerGY) = WorldToGrid(worldX, worldY);
            
            for (int dx = -cellRadius; dx <= cellRadius; dx++)
            {
                for (int dy = -cellRadius; dy <= cellRadius; dy++)
                {
                    int gx = centerGX + dx;
                    int gy = centerGY + dy;
                    
                    if (gx >= 0 && gx < _gridWidth && gy >= 0 && gy < _gridHeight)
                    {
                        float dist = (float)Math.Sqrt(dx * dx + dy * dy) * CELL_SIZE;
                        if (dist <= radius)
                        {
                            _dynamicBlocked.Add((gx, gy));
                        }
                    }
                }
            }
        }

        public (int gx, int gy) WorldToGrid(float worldX, float worldY)
        {
            int gx = (int)((worldX - MinX) / CELL_SIZE);
            int gy = (int)((worldY - MinY) / CELL_SIZE);
            return (gx, gy);
        }

        public (float x, float y) GridToWorld(int gx, int gy)
        {
            return (MinX + gx * CELL_SIZE + CELL_SIZE / 2f,
                    MinY + gy * CELL_SIZE + CELL_SIZE / 2f);
        }

        /// <summary>
        /// Extract master data for this tile type (for transfer learning)
        /// </summary>
        public MasterNavData ExtractMasterData()
        {
            var data = new MasterNavData 
            { 
                SceneSnoId = SceneSnoId,
                SceneCode = SceneCode,
                Width = Width,
                Height = Height
            };
            
            foreach (var kvp in _cells)
            {
                if (kvp.Value.IsConfirmed)
                {
                    // Store relative position (normalized to scene size)
                    float relX = (float)kvp.Key.Item1 / _gridWidth;
                    float relY = (float)kvp.Key.Item2 / _gridHeight;
                    
                    data.WalkableCells.Add(new MasterCell
                    {
                        RelX = relX,
                        RelY = relY,
                        IsWalkable = kvp.Value.IsWalkable,
                        RelZ = (kvp.Value.Z - BaseZ) / 100f, // Normalize Z
                        Confidence = Math.Min(1f, kvp.Value.VisitCount / 5f)
                    });
                }
            }
            
            return data;
        }

        /// <summary>
        /// Apply master data from previous learning
        /// </summary>
        public void ApplyMasterData(MasterNavData master)
        {
            foreach (var cell in master.WalkableCells)
            {
                // Convert relative position back to grid coords
                int gx = (int)(cell.RelX * _gridWidth);
                int gy = (int)(cell.RelY * _gridHeight);
                
                if (gx < 0 || gx >= _gridWidth || gy < 0 || gy >= _gridHeight) continue;
                
                if (!_cells.ContainsKey((gx, gy)))
                {
                    _cells[(gx, gy)] = new NavCellData
                    {
                        IsWalkable = cell.IsWalkable,
                        IsConfirmed = cell.Confidence > 0.5f,
                        Z = BaseZ + cell.RelZ * 100f,
                        FromMaster = true
                    };
                    
                    if (cell.IsWalkable && cell.Confidence > 0.5f)
                    {
                        _exploredCells.Add((gx, gy));
                    }
                }
            }
        }
    }

    #region Data Classes

    public class NavCellData
    {
        public bool IsWalkable { get; set; }
        public bool IsConfirmed { get; set; }
        public bool IsEdge { get; set; }
        public bool FromMaster { get; set; }
        public float Z { get; set; }
        public long LastSeen { get; set; }
        public int VisitCount { get; set; }
    }

    public class PlayerSnapshot
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public uint SceneId { get; set; }
        public int Tick { get; set; }
    }

    /// <summary>
    /// Master navigation data for a tile type (SceneSnoId)
    /// This allows learning to transfer across same tile types
    /// </summary>
    public class MasterNavData
    {
        public uint SceneSnoId { get; set; }
        public string SceneCode { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public List<MasterCell> WalkableCells { get; set; } = new List<MasterCell>();
        
        public void MergeWith(MasterNavData other)
        {
            if (other.SceneSnoId != SceneSnoId) return;
            
            var existing = WalkableCells.ToDictionary(c => (c.RelX, c.RelY));
            
            foreach (var cell in other.WalkableCells)
            {
                var key = (cell.RelX, cell.RelY);
                if (existing.TryGetValue(key, out var existingCell))
                {
                    // Merge - increase confidence
                    existingCell.Confidence = Math.Min(1f, existingCell.Confidence + cell.Confidence * 0.2f);
                    if (!cell.IsWalkable && existingCell.Confidence < 0.3f)
                    {
                        existingCell.IsWalkable = false;
                    }
                }
                else
                {
                    WalkableCells.Add(cell);
                }
            }
        }
    }

    public class MasterCell
    {
        public float RelX { get; set; }
        public float RelY { get; set; }
        public float RelZ { get; set; }
        public bool IsWalkable { get; set; }
        public float Confidence { get; set; }
    }

    public class ExplorationFrontier
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Distance { get; set; }
        public float Priority { get; set; }
        public float Direction { get; set; } // Angle in degrees
    }

    public class PortalInfo
    {
        public IWorldCoordinate Position { get; set; }
        public GizmoType Type { get; set; }
        public string Name { get; set; }
        public uint SceneId { get; set; }
    }

    #endregion
}
