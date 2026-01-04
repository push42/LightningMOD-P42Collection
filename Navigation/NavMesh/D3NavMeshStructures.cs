namespace Turbo.Plugins.Custom.Navigation.NavMesh
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// NavMesh Cell from D3 game data
    /// Each cell represents a small area with walkability flags
    /// </summary>
    public class D3NavCell
    {
        public float MinX { get; set; }
        public float MinY { get; set; }
        public float MinZ { get; set; }
        public float MaxX { get; set; }
        public float MaxY { get; set; }
        public float MaxZ { get; set; }
        public D3NavCellFlags Flags { get; set; }
        public ushort NeighborCount { get; set; }
        public int[] NeighborIndices { get; set; }
        
        public float CenterX => (MinX + MaxX) / 2f;
        public float CenterY => (MinY + MaxY) / 2f;
        public float CenterZ => (MinZ + MaxZ) / 2f;
        public float Width => MaxX - MinX;
        public float Height => MaxY - MinY;
        
        public bool IsWalkable => (Flags & D3NavCellFlags.AllowWalk) != 0;
        public bool AllowsProjectile => (Flags & D3NavCellFlags.AllowProjectile) != 0;
        public bool AllowsFlier => (Flags & D3NavCellFlags.AllowFlier) != 0;
        
        public bool Contains(float x, float y)
        {
            return x >= MinX && x <= MaxX && y >= MinY && y <= MaxY;
        }
    }

    [Flags]
    public enum D3NavCellFlags : ushort
    {
        None = 0,
        AllowWalk = 0x1,
        AllowFlier = 0x2,
        AllowSpider = 0x4,
        LevelAreaBit0 = 0x8,
        LevelAreaBit1 = 0x10,
        NoNavMeshIntersected = 0x20,
        NoSpawn = 0x40,
        Special0 = 0x80,
        Special1 = 0x100,
        SymbolNotFound = 0x200,
        AllowProjectile = 0x400,
        AllowGhost = 0x800,
        RoundedCorner0 = 0x1000,
        RoundedCorner1 = 0x2000,
        RoundedCorner2 = 0x4000,
        RoundedCorner3 = 0x8000
    }

    /// <summary>
    /// NavMesh Zone - A collection of cells for a specific area
    /// </summary>
    public class D3NavZone
    {
        public int ZoneId { get; set; }
        public uint SceneSnoId { get; set; }
        public int LevelAreaSnoId { get; set; }
        
        public float MinX { get; set; }
        public float MinY { get; set; }
        public float MaxX { get; set; }
        public float MaxY { get; set; }
        public float MinZ { get; set; }
        public float MaxZ { get; set; }
        
        public List<D3NavCell> Cells { get; set; } = new List<D3NavCell>();
        public List<D3NavCellConnection> Connections { get; set; } = new List<D3NavCellConnection>();
        
        public float Width => MaxX - MinX;
        public float Height => MaxY - MinY;
        
        public D3NavCell GetCellAt(float x, float y)
        {
            return Cells.FirstOrDefault(c => c.Contains(x, y));
        }
        
        public IEnumerable<D3NavCell> GetWalkableCells()
        {
            return Cells.Where(c => c.IsWalkable);
        }
    }

    /// <summary>
    /// Connection between nav cells
    /// </summary>
    public class D3NavCellConnection
    {
        public int FromCellIndex { get; set; }
        public int ToCellIndex { get; set; }
        public float Cost { get; set; }
    }

    /// <summary>
    /// Complete NavMesh for a scene
    /// </summary>
    public class D3SceneNavMesh
    {
        public uint SceneSnoId { get; set; }
        public string SceneName { get; set; }
        public int Version { get; set; }
        
        public float MinX { get; set; }
        public float MinY { get; set; }
        public float MaxX { get; set; }
        public float MaxY { get; set; }
        
        public List<D3NavZone> Zones { get; set; } = new List<D3NavZone>();
        
        // Lookup optimization
        private Dictionary<(int, int), D3NavCell> _cellGrid;
        private const float GRID_CELL_SIZE = 2.5f;
        
        public void BuildLookupGrid()
        {
            _cellGrid = new Dictionary<(int, int), D3NavCell>();
            
            foreach (var zone in Zones)
            {
                foreach (var cell in zone.Cells)
                {
                    // Add cell to all grid positions it covers
                    int minGX = (int)(cell.MinX / GRID_CELL_SIZE);
                    int maxGX = (int)(cell.MaxX / GRID_CELL_SIZE);
                    int minGY = (int)(cell.MinY / GRID_CELL_SIZE);
                    int maxGY = (int)(cell.MaxY / GRID_CELL_SIZE);
                    
                    for (int gx = minGX; gx <= maxGX; gx++)
                    {
                        for (int gy = minGY; gy <= maxGY; gy++)
                        {
                            var key = (gx, gy);
                            if (!_cellGrid.ContainsKey(key))
                                _cellGrid[key] = cell;
                        }
                    }
                }
            }
        }
        
        public D3NavCell GetCellAt(float x, float y)
        {
            if (_cellGrid == null) BuildLookupGrid();
            
            int gx = (int)(x / GRID_CELL_SIZE);
            int gy = (int)(y / GRID_CELL_SIZE);
            
            return _cellGrid.TryGetValue((gx, gy), out var cell) ? cell : null;
        }
        
        public bool IsWalkable(float x, float y)
        {
            var cell = GetCellAt(x, y);
            return cell?.IsWalkable ?? false;
        }
    }

    /// <summary>
    /// World NavMesh - Contains all scene navmeshes for a world
    /// </summary>
    public class D3WorldNavMesh
    {
        public uint WorldSnoId { get; set; }
        public string WorldName { get; set; }
        
        public Dictionary<uint, D3SceneNavMesh> SceneNavMeshes { get; set; } = 
            new Dictionary<uint, D3SceneNavMesh>();
        
        public void AddSceneNavMesh(D3SceneNavMesh navMesh)
        {
            SceneNavMeshes[navMesh.SceneSnoId] = navMesh;
        }
        
        public D3SceneNavMesh GetSceneNavMesh(uint sceneSnoId)
        {
            return SceneNavMeshes.TryGetValue(sceneSnoId, out var mesh) ? mesh : null;
        }
        
        public bool IsWalkable(float x, float y, uint sceneSnoId)
        {
            var sceneMesh = GetSceneNavMesh(sceneSnoId);
            return sceneMesh?.IsWalkable(x, y) ?? false;
        }
    }
}
