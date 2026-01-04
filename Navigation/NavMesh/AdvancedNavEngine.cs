namespace Turbo.Plugins.Custom.Navigation.NavMesh
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Turbo.Plugins.Default;

    /// <summary>
    /// Advanced Navigation Engine - Inspired by Nav library and Enigma.D3
    /// 
    /// Features from Nav library:
    /// - AABB 3D cells with proper bounds
    /// - Dynamic navmesh building from cell soup
    /// - Anti-stuck system with automatic path alteration
    /// - Exploration engine for automatic traversal
    /// - Ray casting for line-of-sight
    /// - Threaded updates for non-blocking operation
    /// 
    /// Features from Enigma.D3:
    /// - Memory-based scene/cell data structures
    /// - Proper D3 coordinate handling
    /// - Scene grid calculations
    /// </summary>
    public class AdvancedNavEngine
    {
        public IController Hud { get; set; }
        
        // Core navmesh data
        private List<NavCell3D> _cells = new List<NavCell3D>();
        private Dictionary<int, NavCell3D> _cellById = new Dictionary<int, NavCell3D>();
        private Dictionary<(int, int, int), List<NavCell3D>> _spatialGrid = new Dictionary<(int, int, int), List<NavCell3D>>();
        private const float SPATIAL_GRID_SIZE = 20f;
        
        // Anti-stuck system
        private Queue<Vec3> _positionHistory = new Queue<Vec3>();
        private const int STUCK_HISTORY_SIZE = 30;
        private const float STUCK_DISTANCE_THRESHOLD = 3f;
        private int _stuckCounter;
        private HashSet<int> _avoidCells = new HashSet<int>();
        
        // Exploration engine
        private List<NavCell3D> _unexploredCells = new List<NavCell3D>();
        private NavCell3D _explorationTarget;
        private float _explorationRadius = 100f;
        
        // Path state
        public Vec3 CurrentPos { get; private set; }
        public Vec3 Destination { get; set; }
        public Vec3 GoToPosition { get; private set; }
        public List<Vec3> CurrentPath { get; private set; } = new List<Vec3>();
        public bool IsStuck { get; private set; }
        public float DistanceTraveled { get; private set; }
        
        // Threading
        private CancellationTokenSource _cts;
        private Task _updateTask;
        private object _lock = new object();
        private bool _pathDirty;
        
        // Stats
        public int CellCount => _cells.Count;
        public int UnexploredCount => _unexploredCells.Count;
        public string Status { get; private set; } = "Idle";

        public AdvancedNavEngine()
        {
        }

        #region Initialization

        public void Initialize()
        {
            _cells.Clear();
            _cellById.Clear();
            _spatialGrid.Clear();
            _positionHistory.Clear();
            _unexploredCells.Clear();
            _avoidCells.Clear();
            _stuckCounter = 0;
            CurrentPath.Clear();
            
            _cts = new CancellationTokenSource();
            Status = "Initialized";
        }

        public void Shutdown()
        {
            _cts?.Cancel();
            _updateTask?.Wait(1000);
        }

        #endregion

        #region Cell Management

        /// <summary>
        /// Add a 3D cell to the navmesh (AABB style like Nav library)
        /// </summary>
        public void AddCell(NavCell3D cell)
        {
            lock (_lock)
            {
                if (_cellById.ContainsKey(cell.Id)) return;
                
                _cells.Add(cell);
                _cellById[cell.Id] = cell;
                
                // Add to spatial grid for fast lookups
                var gridKey = GetSpatialGridKey(cell.Center);
                if (!_spatialGrid.TryGetValue(gridKey, out var bucket))
                {
                    bucket = new List<NavCell3D>();
                    _spatialGrid[gridKey] = bucket;
                }
                bucket.Add(cell);
                
                // Mark as unexplored initially
                if (!cell.Explored)
                {
                    _unexploredCells.Add(cell);
                }
                
                // Rebuild neighbors
                RebuildNeighbors(cell);
                
                _pathDirty = true;
            }
        }

        /// <summary>
        /// Add cells from scene data
        /// </summary>
        public void AddCellsFromScene(IScene scene, float cellSize = 2.5f)
        {
            if (scene == null) return;
            
            int cellsX = (int)Math.Ceiling(scene.W / cellSize);
            int cellsY = (int)Math.Ceiling(scene.H / cellSize);
            
            for (int cx = 0; cx < cellsX; cx++)
            {
                for (int cy = 0; cy < cellsY; cy++)
                {
                    float minX = scene.PosX + cx * cellSize;
                    float minY = scene.PosY + cy * cellSize;
                    float z = scene.Z;
                    
                    // Skip edge cells (usually walls)
                    bool isEdge = cx == 0 || cy == 0 || cx == cellsX - 1 || cy == cellsY - 1;
                    
                    var cell = new NavCell3D
                    {
                        Id = GenerateCellId(scene.SceneId, cx, cy),
                        Min = new Vec3(minX, minY, z - 5),
                        Max = new Vec3(minX + cellSize, minY + cellSize, z + 15),
                        SceneId = scene.SceneId,
                        Flags = isEdge ? NavCell3DFlags.Blocked : NavCell3DFlags.Walkable,
                        MovementCost = 1f
                    };
                    
                    AddCell(cell);
                }
            }
        }

        private int GenerateCellId(uint sceneId, int cx, int cy)
        {
            return (int)((sceneId << 16) | ((uint)cx << 8) | (uint)cy);
        }

        private void RebuildNeighbors(NavCell3D cell)
        {
            cell.Neighbors.Clear();
            
            // Find neighbors in nearby spatial grid buckets
            var gridKey = GetSpatialGridKey(cell.Center);
            
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        var neighborKey = (gridKey.Item1 + dx, gridKey.Item2 + dy, gridKey.Item3 + dz);
                        if (_spatialGrid.TryGetValue(neighborKey, out var bucket))
                        {
                            foreach (var other in bucket)
                            {
                                if (other.Id == cell.Id) continue;
                                if (cell.IsTouching(other))
                                {
                                    cell.Neighbors.Add(other);
                                }
                            }
                        }
                    }
                }
            }
        }

        private (int, int, int) GetSpatialGridKey(Vec3 pos)
        {
            return (
                (int)Math.Floor(pos.X / SPATIAL_GRID_SIZE),
                (int)Math.Floor(pos.Y / SPATIAL_GRID_SIZE),
                (int)Math.Floor(pos.Z / SPATIAL_GRID_SIZE)
            );
        }

        #endregion

        #region Position & Path Update

        /// <summary>
        /// Update current position - call every frame
        /// </summary>
        public void UpdateCurrentPos(float x, float y, float z)
        {
            var newPos = new Vec3(x, y, z);
            
            // Track distance traveled
            if (CurrentPos != null)
            {
                DistanceTraveled += CurrentPos.DistanceTo(newPos);
            }
            
            CurrentPos = newPos;
            
            // Update position history for stuck detection
            _positionHistory.Enqueue(newPos);
            while (_positionHistory.Count > STUCK_HISTORY_SIZE)
                _positionHistory.Dequeue();
            
            // Check for stuck condition
            CheckStuck();
            
            // Mark current cell as explored
            var currentCell = GetCellAt(x, y, z);
            if (currentCell != null && !currentCell.Explored)
            {
                currentCell.Explored = true;
                _unexploredCells.Remove(currentCell);
            }
            
            // Update GoToPosition
            UpdateGoToPosition();
        }

        private void CheckStuck()
        {
            if (_positionHistory.Count < STUCK_HISTORY_SIZE) return;
            
            var positions = _positionHistory.ToArray();
            var oldest = positions[0];
            var newest = positions[positions.Length - 1];
            
            float distance = oldest.DistanceTo(newest);
            
            if (distance < STUCK_DISTANCE_THRESHOLD)
            {
                _stuckCounter++;
                
                if (_stuckCounter > 10)
                {
                    IsStuck = true;
                    HandleStuck();
                }
            }
            else
            {
                _stuckCounter = 0;
                IsStuck = false;
            }
        }

        private void HandleStuck()
        {
            Status = "STUCK - Replanning...";
            
            // Add current path cells to avoid list temporarily
            if (CurrentPath.Count > 0)
            {
                foreach (var pos in CurrentPath.Take(3))
                {
                    var cell = GetCellAt(pos.X, pos.Y, pos.Z);
                    if (cell != null)
                    {
                        _avoidCells.Add(cell.Id);
                    }
                }
            }
            
            // Force path recalculation
            _pathDirty = true;
            _positionHistory.Clear();
            _stuckCounter = 0;
        }

        private void UpdateGoToPosition()
        {
            if (CurrentPath == null || CurrentPath.Count == 0)
            {
                GoToPosition = Destination;
                return;
            }
            
            // Find next waypoint on path
            float lookAhead = 5f;
            
            foreach (var waypoint in CurrentPath)
            {
                float dist = CurrentPos.DistanceTo(waypoint);
                if (dist > lookAhead)
                {
                    GoToPosition = waypoint;
                    return;
                }
            }
            
            // At end of path
            GoToPosition = Destination ?? CurrentPos;
        }

        #endregion

        #region Pathfinding (A* with modifications)

        /// <summary>
        /// Find path to destination
        /// </summary>
        public List<Vec3> FindPath(Vec3 start, Vec3 end)
        {
            var startCell = GetCellAt(start.X, start.Y, start.Z);
            var endCell = GetCellAt(end.X, end.Y, end.Z);
            
            if (startCell == null || endCell == null)
                return new List<Vec3> { end };
            
            if (startCell.Id == endCell.Id)
                return new List<Vec3> { end };
            
            // A* with anti-stuck modifications
            var openSet = new SortedSet<AStarNode>(new AStarComparer());
            var closedSet = new HashSet<int>();
            var cameFrom = new Dictionary<int, int>();
            var gScore = new Dictionary<int, float> { [startCell.Id] = 0 };
            var fScore = new Dictionary<int, float>();
            
            float h = startCell.Center.DistanceTo(endCell.Center);
            fScore[startCell.Id] = h;
            openSet.Add(new AStarNode(startCell.Id, h));
            
            int iterations = 0;
            const int maxIterations = 5000;
            
            while (openSet.Count > 0 && iterations++ < maxIterations)
            {
                var currentNode = openSet.Min;
                openSet.Remove(currentNode);
                int currentId = currentNode.CellId;
                
                if (currentId == endCell.Id)
                {
                    return ReconstructPath(cameFrom, currentId, end);
                }
                
                closedSet.Add(currentId);
                
                var currentCell = _cellById[currentId];
                
                foreach (var neighbor in currentCell.Neighbors)
                {
                    if (closedSet.Contains(neighbor.Id)) continue;
                    if (!neighbor.IsWalkable) continue;
                    
                    // Anti-stuck: Avoid cells we got stuck at
                    float avoidPenalty = _avoidCells.Contains(neighbor.Id) ? 100f : 0f;
                    
                    float tentativeG = gScore[currentId] + 
                        currentCell.Center.DistanceTo(neighbor.Center) * neighbor.MovementCost +
                        avoidPenalty;
                    
                    if (!gScore.ContainsKey(neighbor.Id) || tentativeG < gScore[neighbor.Id])
                    {
                        cameFrom[neighbor.Id] = currentId;
                        gScore[neighbor.Id] = tentativeG;
                        float f = tentativeG + neighbor.Center.DistanceTo(endCell.Center);
                        fScore[neighbor.Id] = f;
                        
                        var node = new AStarNode(neighbor.Id, f);
                        if (!openSet.Contains(node))
                            openSet.Add(node);
                    }
                }
            }
            
            // No path found - return direct line
            Status = "No path found";
            return new List<Vec3> { end };
        }

        private List<Vec3> ReconstructPath(Dictionary<int, int> cameFrom, int current, Vec3 end)
        {
            var path = new List<Vec3> { end };
            
            while (cameFrom.ContainsKey(current))
            {
                var cell = _cellById[current];
                path.Insert(0, cell.Center);
                current = cameFrom[current];
            }
            
            // Smooth path
            path = SmoothPath(path);
            
            return path;
        }

        /// <summary>
        /// Smooth path using ray casting
        /// </summary>
        private List<Vec3> SmoothPath(List<Vec3> path)
        {
            if (path.Count <= 2) return path;
            
            var smoothed = new List<Vec3> { path[0] };
            int current = 0;
            
            while (current < path.Count - 1)
            {
                // Find furthest visible point
                int furthest = current + 1;
                
                for (int i = path.Count - 1; i > current + 1; i--)
                {
                    if (HasLineOfSight(path[current], path[i]))
                    {
                        furthest = i;
                        break;
                    }
                }
                
                smoothed.Add(path[furthest]);
                current = furthest;
            }
            
            return smoothed;
        }

        #endregion

        #region Ray Casting

        /// <summary>
        /// Check if there's line of sight between two points
        /// </summary>
        public bool HasLineOfSight(Vec3 from, Vec3 to)
        {
            float dist = from.DistanceTo(to);
            int steps = Math.Max(3, (int)(dist / 2f));
            
            float dx = (to.X - from.X) / steps;
            float dy = (to.Y - from.Y) / steps;
            float dz = (to.Z - from.Z) / steps;
            
            for (int i = 1; i <= steps; i++)
            {
                float x = from.X + dx * i;
                float y = from.Y + dy * i;
                float z = from.Z + dz * i;
                
                var cell = GetCellAt(x, y, z);
                if (cell == null || !cell.IsWalkable)
                    return false;
            }
            
            return true;
        }

        /// <summary>
        /// Ray cast and return hit point
        /// </summary>
        public Vec3 RayCast(Vec3 from, Vec3 direction, float maxDistance)
        {
            float step = 1f;
            Vec3 current = from;
            float traveled = 0;
            
            while (traveled < maxDistance)
            {
                current = new Vec3(
                    from.X + direction.X * traveled,
                    from.Y + direction.Y * traveled,
                    from.Z + direction.Z * traveled
                );
                
                var cell = GetCellAt(current.X, current.Y, current.Z);
                if (cell == null || !cell.IsWalkable)
                {
                    return current;
                }
                
                traveled += step;
            }
            
            return current;
        }

        #endregion

        #region Exploration Engine

        /// <summary>
        /// Get best exploration target
        /// </summary>
        public Vec3 GetExplorationTarget()
        {
            if (_unexploredCells.Count == 0)
                return null;
            
            // Find nearest unexplored cell that's reachable
            var sorted = _unexploredCells
                .Where(c => c.IsWalkable)
                .OrderBy(c => CurrentPos.DistanceTo(c.Center))
                .Take(10)
                .ToList();
            
            foreach (var cell in sorted)
            {
                // Check if we can path to it
                var path = FindPath(CurrentPos, cell.Center);
                if (path.Count > 0)
                {
                    _explorationTarget = cell;
                    return cell.Center;
                }
            }
            
            return null;
        }

        /// <summary>
        /// Update exploration - auto-set destination to unexplored areas
        /// </summary>
        public void UpdateExploration()
        {
            if (Destination == null || CurrentPos.DistanceTo(Destination) < 5f)
            {
                Destination = GetExplorationTarget();
                if (Destination != null)
                {
                    CurrentPath = FindPath(CurrentPos, Destination);
                    Status = $"Exploring ({_unexploredCells.Count} left)";
                }
                else
                {
                    Status = "Exploration complete";
                }
            }
        }

        #endregion

        #region Cell Queries

        /// <summary>
        /// Get cell at world position
        /// </summary>
        public NavCell3D GetCellAt(float x, float y, float z)
        {
            var gridKey = GetSpatialGridKey(new Vec3(x, y, z));
            
            // Check this bucket and neighbors
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    var key = (gridKey.Item1 + dx, gridKey.Item2 + dy, gridKey.Item3);
                    if (_spatialGrid.TryGetValue(key, out var bucket))
                    {
                        foreach (var cell in bucket)
                        {
                            if (cell.Contains(x, y, z))
                                return cell;
                        }
                    }
                }
            }
            
            return null;
        }

        /// <summary>
        /// Check if position is walkable
        /// </summary>
        public bool IsWalkable(float x, float y, float z)
        {
            var cell = GetCellAt(x, y, z);
            return cell != null && cell.IsWalkable;
        }

        /// <summary>
        /// Get all cells in radius
        /// </summary>
        public IEnumerable<NavCell3D> GetCellsInRadius(Vec3 center, float radius)
        {
            var gridKey = GetSpatialGridKey(center);
            int gridRadius = (int)Math.Ceiling(radius / SPATIAL_GRID_SIZE);
            
            for (int dx = -gridRadius; dx <= gridRadius; dx++)
            {
                for (int dy = -gridRadius; dy <= gridRadius; dy++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        var key = (gridKey.Item1 + dx, gridKey.Item2 + dy, gridKey.Item3 + dz);
                        if (_spatialGrid.TryGetValue(key, out var bucket))
                        {
                            foreach (var cell in bucket)
                            {
                                if (center.DistanceTo(cell.Center) <= radius)
                                    yield return cell;
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region Danger/Avoid Areas

        /// <summary>
        /// Mark area as dangerous (increases movement cost)
        /// </summary>
        public void MarkDangerous(float x, float y, float z, float radius, float costMultiplier = 5f)
        {
            foreach (var cell in GetCellsInRadius(new Vec3(x, y, z), radius))
            {
                cell.Flags |= NavCell3DFlags.Dangerous;
                cell.MovementCost = Math.Max(cell.MovementCost, costMultiplier);
            }
            _pathDirty = true;
        }

        /// <summary>
        /// Clear danger flags (call at start of each frame)
        /// </summary>
        public void ClearDangerFlags()
        {
            foreach (var cell in _cells)
            {
                cell.Flags &= ~NavCell3DFlags.Dangerous;
                cell.MovementCost = 1f;
            }
        }

        /// <summary>
        /// Temporarily avoid a cell (for anti-stuck)
        /// </summary>
        public void AvoidCell(int cellId, int duration = 100)
        {
            _avoidCells.Add(cellId);
            // TODO: Add timer to clear after duration
        }

        /// <summary>
        /// Clear avoid list
        /// </summary>
        public void ClearAvoidCells()
        {
            _avoidCells.Clear();
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// 3D Navigation Cell (AABB style like Nav library)
    /// </summary>
    public class NavCell3D
    {
        public int Id { get; set; }
        public Vec3 Min { get; set; }
        public Vec3 Max { get; set; }
        public uint SceneId { get; set; }
        public NavCell3DFlags Flags { get; set; }
        public float MovementCost { get; set; } = 1f;
        public bool Explored { get; set; }
        public List<NavCell3D> Neighbors { get; set; } = new List<NavCell3D>();
        
        public Vec3 Center => new Vec3(
            (Min.X + Max.X) / 2f,
            (Min.Y + Max.Y) / 2f,
            (Min.Z + Max.Z) / 2f
        );
        
        public bool IsWalkable => (Flags & NavCell3DFlags.Walkable) != 0 && 
                                  (Flags & NavCell3DFlags.Blocked) == 0;
        
        public bool Contains(float x, float y, float z)
        {
            return x >= Min.X && x <= Max.X &&
                   y >= Min.Y && y <= Max.Y &&
                   z >= Min.Z && z <= Max.Z;
        }
        
        public bool IsTouching(NavCell3D other)
        {
            // Check if cells are adjacent (touching but not overlapping)
            bool xTouch = Math.Abs(Max.X - other.Min.X) < 0.1f || Math.Abs(Min.X - other.Max.X) < 0.1f ||
                         (Min.X <= other.Max.X && Max.X >= other.Min.X);
            bool yTouch = Math.Abs(Max.Y - other.Min.Y) < 0.1f || Math.Abs(Min.Y - other.Max.Y) < 0.1f ||
                         (Min.Y <= other.Max.Y && Max.Y >= other.Min.Y);
            bool zTouch = Math.Abs(Max.Z - other.Min.Z) < 20f || Math.Abs(Min.Z - other.Max.Z) < 20f ||
                         (Min.Z <= other.Max.Z + 20 && Max.Z >= other.Min.Z - 20);
            
            return xTouch && yTouch && zTouch;
        }
    }

    [Flags]
    public enum NavCell3DFlags : uint
    {
        None = 0,
        Walkable = 1 << 0,
        Blocked = 1 << 1,
        Water = 1 << 2,
        Dangerous = 1 << 3,
        Explored = 1 << 4,
        HasMonster = 1 << 5,
        HasElite = 1 << 6,
        HasGroundEffect = 1 << 7,
        Portal = 1 << 8,
        Waypoint = 1 << 9
    }

    /// <summary>
    /// 3D Vector
    /// </summary>
    public class Vec3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        
        public Vec3() { }
        
        public Vec3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
        
        public float DistanceTo(Vec3 other)
        {
            if (other == null) return float.MaxValue;
            return (float)Math.Sqrt(
                Math.Pow(X - other.X, 2) +
                Math.Pow(Y - other.Y, 2) +
                Math.Pow(Z - other.Z, 2)
            );
        }
        
        public float DistanceXY(Vec3 other)
        {
            if (other == null) return float.MaxValue;
            return (float)Math.Sqrt(
                Math.Pow(X - other.X, 2) +
                Math.Pow(Y - other.Y, 2)
            );
        }
        
        public Vec3 Normalized()
        {
            float len = (float)Math.Sqrt(X * X + Y * Y + Z * Z);
            if (len < 0.0001f) return new Vec3(0, 0, 0);
            return new Vec3(X / len, Y / len, Z / len);
        }
        
        public override string ToString() => $"({X:F1}, {Y:F1}, {Z:F1})";
    }

    /// <summary>
    /// A* node for priority queue
    /// </summary>
    public class AStarNode
    {
        public int CellId { get; }
        public float FScore { get; }
        
        public AStarNode(int cellId, float fScore)
        {
            CellId = cellId;
            FScore = fScore;
        }
        
        public override bool Equals(object obj)
        {
            return obj is AStarNode other && CellId == other.CellId;
        }
        
        public override int GetHashCode() => CellId;
    }

    public class AStarComparer : IComparer<AStarNode>
    {
        public int Compare(AStarNode a, AStarNode b)
        {
            int result = a.FScore.CompareTo(b.FScore);
            if (result == 0)
                result = a.CellId.CompareTo(b.CellId);
            return result;
        }
    }

    #endregion
}
