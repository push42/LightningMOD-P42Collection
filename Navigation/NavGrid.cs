namespace Turbo.Plugins.Custom.Navigation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Navigation Cell - Represents a single walkable/non-walkable cell in the navigation grid
    /// </summary>
    public class NavCell
    {
        public int X { get; set; }
        public int Y { get; set; }
        public float WorldX { get; set; }
        public float WorldY { get; set; }
        public float WorldZ { get; set; }
        public NavCellFlags Flags { get; set; }
        public float Cost { get; set; } = 1f;
        public uint SceneId { get; set; }
        
        // Pathfinding helpers
        public float GScore { get; set; }
        public float FScore { get; set; }
        public NavCell Parent { get; set; }
        
        public bool IsWalkable => (Flags & NavCellFlags.Walkable) != 0;
        public bool IsBlocked => (Flags & NavCellFlags.Blocked) != 0;
        public bool IsDangerous => (Flags & NavCellFlags.Dangerous) != 0;
        
        public (int, int) GridPos => (X, Y);
    }

    [Flags]
    public enum NavCellFlags : uint
    {
        None = 0,
        Walkable = 1 << 0,
        Blocked = 1 << 1,
        Water = 1 << 2,
        Dangerous = 1 << 3,
        NearWall = 1 << 4,
        NearEdge = 1 << 5,
        HasMonster = 1 << 6,
        HasElite = 1 << 7,
        HasGroundEffect = 1 << 8,
        HighTraffic = 1 << 9,
        Portal = 1 << 10,
        Shrine = 1 << 11,
        Interactable = 1 << 12,
        SceneBoundary = 1 << 13,
        Explored = 1 << 14,
        Cached = 1 << 15
    }

    /// <summary>
    /// Navigation Grid - Spatial grid for fast lookups and pathfinding
    /// </summary>
    public class NavGrid
    {
        public const float DEFAULT_CELL_SIZE = 2.5f;
        
        public float CellSize { get; }
        public float OriginX { get; private set; }
        public float OriginY { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        
        private Dictionary<(int, int), NavCell> _cells = new Dictionary<(int, int), NavCell>();
        private HashSet<(int, int)> _blockedCells = new HashSet<(int, int)>();
        private HashSet<(int, int)> _dangerousCells = new HashSet<(int, int)>();
        
        public NavGrid(float cellSize = DEFAULT_CELL_SIZE)
        {
            CellSize = cellSize;
        }

        public void Initialize(float minX, float minY, float maxX, float maxY)
        {
            OriginX = minX;
            OriginY = minY;
            Width = (int)Math.Ceiling((maxX - minX) / CellSize);
            Height = (int)Math.Ceiling((maxY - minY) / CellSize);
        }

        public (int x, int y) WorldToGrid(float worldX, float worldY)
        {
            int x = (int)((worldX - OriginX) / CellSize);
            int y = (int)((worldY - OriginY) / CellSize);
            return (x, y);
        }

        public (float x, float y) GridToWorld(int gridX, int gridY)
        {
            float x = OriginX + gridX * CellSize + CellSize / 2f;
            float y = OriginY + gridY * CellSize + CellSize / 2f;
            return (x, y);
        }

        public NavCell GetCell(int x, int y)
        {
            return _cells.TryGetValue((x, y), out var cell) ? cell : null;
        }

        public NavCell GetCellAtWorld(float worldX, float worldY)
        {
            var (x, y) = WorldToGrid(worldX, worldY);
            return GetCell(x, y);
        }

        public NavCell GetOrCreateCell(int x, int y)
        {
            if (!_cells.TryGetValue((x, y), out var cell))
            {
                var (worldX, worldY) = GridToWorld(x, y);
                cell = new NavCell
                {
                    X = x,
                    Y = y,
                    WorldX = worldX,
                    WorldY = worldY,
                    Flags = NavCellFlags.Walkable
                };
                _cells[(x, y)] = cell;
            }
            return cell;
        }

        public void SetCellFlags(int x, int y, NavCellFlags flags)
        {
            var cell = GetOrCreateCell(x, y);
            cell.Flags |= flags;
            
            if ((flags & NavCellFlags.Blocked) != 0)
                _blockedCells.Add((x, y));
            if ((flags & NavCellFlags.Dangerous) != 0)
                _dangerousCells.Add((x, y));
        }

        public void ClearCellFlags(int x, int y, NavCellFlags flags)
        {
            var cell = GetCell(x, y);
            if (cell != null)
            {
                cell.Flags &= ~flags;
                
                if ((flags & NavCellFlags.Blocked) != 0)
                    _blockedCells.Remove((x, y));
                if ((flags & NavCellFlags.Dangerous) != 0)
                    _dangerousCells.Remove((x, y));
            }
        }

        public bool IsWalkable(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return false;
            if (_blockedCells.Contains((x, y))) return false;
            
            var cell = GetCell(x, y);
            return cell == null || cell.IsWalkable;
        }

        public bool IsDangerous(int x, int y)
        {
            return _dangerousCells.Contains((x, y));
        }

        public IEnumerable<NavCell> GetNeighbors(NavCell cell, bool includeDiagonals = true)
        {
            // Cardinal directions
            yield return GetCell(cell.X - 1, cell.Y);
            yield return GetCell(cell.X + 1, cell.Y);
            yield return GetCell(cell.X, cell.Y - 1);
            yield return GetCell(cell.X, cell.Y + 1);

            if (includeDiagonals)
            {
                yield return GetCell(cell.X - 1, cell.Y - 1);
                yield return GetCell(cell.X + 1, cell.Y - 1);
                yield return GetCell(cell.X - 1, cell.Y + 1);
                yield return GetCell(cell.X + 1, cell.Y + 1);
            }
        }

        public IEnumerable<NavCell> GetCellsInRadius(float worldX, float worldY, float radius)
        {
            int cellRadius = (int)Math.Ceiling(radius / CellSize);
            var (centerX, centerY) = WorldToGrid(worldX, worldY);
            
            for (int dx = -cellRadius; dx <= cellRadius; dx++)
            {
                for (int dy = -cellRadius; dy <= cellRadius; dy++)
                {
                    var cell = GetCell(centerX + dx, centerY + dy);
                    if (cell != null)
                    {
                        float dist = (float)Math.Sqrt(dx * dx + dy * dy) * CellSize;
                        if (dist <= radius)
                            yield return cell;
                    }
                }
            }
        }

        public void ClearDynamicFlags()
        {
            var dynamicFlags = NavCellFlags.HasMonster | NavCellFlags.HasElite | 
                              NavCellFlags.HasGroundEffect | NavCellFlags.Dangerous;
            
            foreach (var cell in _cells.Values)
            {
                cell.Flags &= ~dynamicFlags;
            }
            _dangerousCells.Clear();
        }

        public void Clear()
        {
            _cells.Clear();
            _blockedCells.Clear();
            _dangerousCells.Clear();
        }

        public int CellCount => _cells.Count;
        public int BlockedCount => _blockedCells.Count;
        public int DangerousCount => _dangerousCells.Count;
    }
}
