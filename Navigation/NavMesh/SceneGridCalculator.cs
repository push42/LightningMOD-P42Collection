namespace Turbo.Plugins.Custom.Navigation.NavMesh
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Turbo.Plugins.Default;

    /// <summary>
    /// Scene Grid Calculator - Based on Enigma.D3 concepts
    /// 
    /// D3 uses a scene grid system where:
    /// - World is divided into scenes (tiles)
    /// - Each scene has a NavMesh with cells
    /// - Cells use local coordinates relative to scene
    /// - Scene grid uses 2.5 unit cells (standard D3 nav cell size)
    /// 
    /// This calculator converts between:
    /// - World coordinates
    /// - Scene coordinates
    /// - Grid coordinates
    /// - Cell indices
    /// </summary>
    public class SceneGridCalculator
    {
        // D3 standard cell size
        public const float CELL_SIZE = 2.5f;
        
        // Scene standard sizes (from D3 data)
        public static readonly float[] SCENE_SIZES = { 40f, 60f, 80f, 120f, 160f, 240f, 320f };
        
        /// <summary>
        /// Calculate grid dimensions for a scene
        /// </summary>
        public static (int width, int height) GetSceneGridSize(float sceneWidth, float sceneHeight)
        {
            return (
                (int)Math.Ceiling(sceneWidth / CELL_SIZE),
                (int)Math.Ceiling(sceneHeight / CELL_SIZE)
            );
        }

        /// <summary>
        /// Convert world coordinates to scene-local coordinates
        /// </summary>
        public static (float localX, float localY) WorldToLocal(float worldX, float worldY, IScene scene)
        {
            return (worldX - scene.PosX, worldY - scene.PosY);
        }

        /// <summary>
        /// Convert scene-local coordinates to world coordinates
        /// </summary>
        public static (float worldX, float worldY) LocalToWorld(float localX, float localY, IScene scene)
        {
            return (scene.PosX + localX, scene.PosY + localY);
        }

        /// <summary>
        /// Convert world coordinates to grid cell indices
        /// </summary>
        public static (int cellX, int cellY) WorldToGrid(float worldX, float worldY, IScene scene)
        {
            var (localX, localY) = WorldToLocal(worldX, worldY, scene);
            return (
                (int)Math.Floor(localX / CELL_SIZE),
                (int)Math.Floor(localY / CELL_SIZE)
            );
        }

        /// <summary>
        /// Convert grid cell indices to world coordinates (center of cell)
        /// </summary>
        public static (float worldX, float worldY) GridToWorld(int cellX, int cellY, IScene scene)
        {
            float localX = cellX * CELL_SIZE + CELL_SIZE / 2f;
            float localY = cellY * CELL_SIZE + CELL_SIZE / 2f;
            return LocalToWorld(localX, localY, scene);
        }

        /// <summary>
        /// Get cell index (1D) from grid coordinates
        /// </summary>
        public static int GridToCellIndex(int cellX, int cellY, int gridWidth)
        {
            return cellY * gridWidth + cellX;
        }

        /// <summary>
        /// Get grid coordinates from cell index
        /// </summary>
        public static (int cellX, int cellY) CellIndexToGrid(int cellIndex, int gridWidth)
        {
            return (cellIndex % gridWidth, cellIndex / gridWidth);
        }

        /// <summary>
        /// Check if a position is within scene bounds
        /// </summary>
        public static bool IsInScene(float worldX, float worldY, IScene scene)
        {
            return worldX >= scene.PosX && worldX <= scene.MaxX &&
                   worldY >= scene.PosY && worldY <= scene.MaxY;
        }

        /// <summary>
        /// Get distance to nearest scene edge
        /// </summary>
        public static float GetDistanceToEdge(float worldX, float worldY, IScene scene)
        {
            float distLeft = worldX - scene.PosX;
            float distRight = scene.MaxX - worldX;
            float distBottom = worldY - scene.PosY;
            float distTop = scene.MaxY - worldY;
            
            return Math.Min(Math.Min(distLeft, distRight), Math.Min(distBottom, distTop));
        }

        /// <summary>
        /// Get nearest edge direction (for wall detection)
        /// </summary>
        public static (float dirX, float dirY) GetNearestEdgeDirection(float worldX, float worldY, IScene scene)
        {
            float distLeft = worldX - scene.PosX;
            float distRight = scene.MaxX - worldX;
            float distBottom = worldY - scene.PosY;
            float distTop = scene.MaxY - worldY;
            
            float minDist = Math.Min(Math.Min(distLeft, distRight), Math.Min(distBottom, distTop));
            
            if (minDist == distLeft) return (-1, 0);
            if (minDist == distRight) return (1, 0);
            if (minDist == distBottom) return (0, -1);
            return (0, 1);
        }

        /// <summary>
        /// Get all cell positions in a scene as world coordinates
        /// </summary>
        public static IEnumerable<(float x, float y, int cellX, int cellY)> GetAllCellPositions(IScene scene)
        {
            var (gridW, gridH) = GetSceneGridSize(scene.W, scene.H);
            
            for (int cy = 0; cy < gridH; cy++)
            {
                for (int cx = 0; cx < gridW; cx++)
                {
                    var (wx, wy) = GridToWorld(cx, cy, scene);
                    yield return (wx, wy, cx, cy);
                }
            }
        }

        /// <summary>
        /// Get neighbor cell indices for a given cell
        /// </summary>
        public static IEnumerable<(int cellX, int cellY)> GetNeighborCells(int cellX, int cellY, int gridWidth, int gridHeight, bool includeDiagonals = true)
        {
            // Cardinal
            if (cellX > 0) yield return (cellX - 1, cellY);
            if (cellX < gridWidth - 1) yield return (cellX + 1, cellY);
            if (cellY > 0) yield return (cellX, cellY - 1);
            if (cellY < gridHeight - 1) yield return (cellX, cellY + 1);
            
            if (includeDiagonals)
            {
                if (cellX > 0 && cellY > 0) yield return (cellX - 1, cellY - 1);
                if (cellX < gridWidth - 1 && cellY > 0) yield return (cellX + 1, cellY - 1);
                if (cellX > 0 && cellY < gridHeight - 1) yield return (cellX - 1, cellY + 1);
                if (cellX < gridWidth - 1 && cellY < gridHeight - 1) yield return (cellX + 1, cellY + 1);
            }
        }
    }

    /// <summary>
    /// Scene NavMesh Grid - Holds nav data for a single scene
    /// Based on Enigma.D3 scene structure
    /// </summary>
    public class SceneNavGrid
    {
        public uint SceneId { get; set; }
        public uint SceneSnoId { get; set; }
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public float Z { get; set; }
        
        public int GridWidth { get; private set; }
        public int GridHeight { get; private set; }
        
        // Cell data stored as bit flags for efficiency
        private uint[] _cellFlags;
        private float[] _cellZ;
        private byte[] _cellCost;
        
        // Cell flag bits
        private const uint FLAG_WALKABLE = 1 << 0;
        private const uint FLAG_EXPLORED = 1 << 1;
        private const uint FLAG_BLOCKED = 1 << 2;
        private const uint FLAG_DANGEROUS = 1 << 3;
        private const uint FLAG_NEAR_WALL = 1 << 4;
        
        public void Initialize()
        {
            var (w, h) = SceneGridCalculator.GetSceneGridSize(Width, Height);
            GridWidth = w;
            GridHeight = h;
            
            int totalCells = GridWidth * GridHeight;
            _cellFlags = new uint[totalCells];
            _cellZ = new float[totalCells];
            _cellCost = new byte[totalCells];
            
            // Initialize all cells
            for (int i = 0; i < totalCells; i++)
            {
                _cellZ[i] = Z;
                _cellCost[i] = 10; // Default cost
            }
            
            // Mark edge cells as blocked
            MarkEdgeCells();
        }
        
        private void MarkEdgeCells()
        {
            // Top and bottom edges
            for (int x = 0; x < GridWidth; x++)
            {
                SetCellFlags(x, 0, FLAG_BLOCKED | FLAG_NEAR_WALL);
                SetCellFlags(x, GridHeight - 1, FLAG_BLOCKED | FLAG_NEAR_WALL);
            }
            
            // Left and right edges
            for (int y = 0; y < GridHeight; y++)
            {
                SetCellFlags(0, y, FLAG_BLOCKED | FLAG_NEAR_WALL);
                SetCellFlags(GridWidth - 1, y, FLAG_BLOCKED | FLAG_NEAR_WALL);
            }
            
            // Mark cells near edges (1 cell buffer)
            for (int x = 1; x < GridWidth - 1; x++)
            {
                SetCellFlags(x, 1, FLAG_NEAR_WALL);
                SetCellFlags(x, GridHeight - 2, FLAG_NEAR_WALL);
            }
            for (int y = 1; y < GridHeight - 1; y++)
            {
                SetCellFlags(1, y, FLAG_NEAR_WALL);
                SetCellFlags(GridWidth - 2, y, FLAG_NEAR_WALL);
            }
        }
        
        public int GetCellIndex(int cellX, int cellY)
        {
            if (cellX < 0 || cellX >= GridWidth || cellY < 0 || cellY >= GridHeight)
                return -1;
            return cellY * GridWidth + cellX;
        }
        
        public void SetCellFlags(int cellX, int cellY, uint flags)
        {
            int idx = GetCellIndex(cellX, cellY);
            if (idx >= 0)
                _cellFlags[idx] |= flags;
        }
        
        public void ClearCellFlags(int cellX, int cellY, uint flags)
        {
            int idx = GetCellIndex(cellX, cellY);
            if (idx >= 0)
                _cellFlags[idx] &= ~flags;
        }
        
        public bool HasCellFlag(int cellX, int cellY, uint flag)
        {
            int idx = GetCellIndex(cellX, cellY);
            if (idx < 0) return false;
            return (_cellFlags[idx] & flag) != 0;
        }
        
        public void SetCellZ(int cellX, int cellY, float z)
        {
            int idx = GetCellIndex(cellX, cellY);
            if (idx >= 0)
                _cellZ[idx] = z;
        }
        
        public float GetCellZ(int cellX, int cellY)
        {
            int idx = GetCellIndex(cellX, cellY);
            if (idx < 0) return Z;
            return _cellZ[idx];
        }
        
        public void SetCellCost(int cellX, int cellY, byte cost)
        {
            int idx = GetCellIndex(cellX, cellY);
            if (idx >= 0)
                _cellCost[idx] = cost;
        }
        
        public byte GetCellCost(int cellX, int cellY)
        {
            int idx = GetCellIndex(cellX, cellY);
            if (idx < 0) return 255; // Max cost for invalid cells
            return _cellCost[idx];
        }
        
        // Convenience methods
        public bool IsWalkable(int cellX, int cellY)
        {
            int idx = GetCellIndex(cellX, cellY);
            if (idx < 0) return false;
            
            uint flags = _cellFlags[idx];
            return (flags & FLAG_WALKABLE) != 0 && (flags & FLAG_BLOCKED) == 0;
        }
        
        public bool IsExplored(int cellX, int cellY)
        {
            return HasCellFlag(cellX, cellY, FLAG_EXPLORED);
        }
        
        public bool IsDangerous(int cellX, int cellY)
        {
            return HasCellFlag(cellX, cellY, FLAG_DANGEROUS);
        }
        
        public bool IsNearWall(int cellX, int cellY)
        {
            return HasCellFlag(cellX, cellY, FLAG_NEAR_WALL);
        }
        
        public void MarkWalkable(int cellX, int cellY, float z)
        {
            int idx = GetCellIndex(cellX, cellY);
            if (idx < 0) return;
            
            _cellFlags[idx] |= FLAG_WALKABLE | FLAG_EXPLORED;
            _cellFlags[idx] &= ~FLAG_BLOCKED;
            _cellZ[idx] = z;
        }
        
        public void MarkBlocked(int cellX, int cellY)
        {
            SetCellFlags(cellX, cellY, FLAG_BLOCKED);
            ClearCellFlags(cellX, cellY, FLAG_WALKABLE);
        }
        
        public void MarkDangerous(int cellX, int cellY, byte dangerCost = 50)
        {
            SetCellFlags(cellX, cellY, FLAG_DANGEROUS);
            int idx = GetCellIndex(cellX, cellY);
            if (idx >= 0)
                _cellCost[idx] = Math.Max(_cellCost[idx], dangerCost);
        }
        
        public void ClearDangerFlags()
        {
            for (int i = 0; i < _cellFlags.Length; i++)
            {
                _cellFlags[i] &= ~FLAG_DANGEROUS;
                _cellCost[i] = 10;
            }
        }
        
        // World coordinate convenience methods
        public bool IsWalkableWorld(float worldX, float worldY, float playerZ)
        {
            var (cx, cy) = SceneGridCalculator.WorldToGrid(worldX, worldY, 
                new MockScene { PosX = PosX, PosY = PosY, MaxX = PosX + Width, MaxY = PosY + Height });
            
            if (!IsWalkable(cx, cy)) return false;
            
            // Check Z tolerance
            float cellZ = GetCellZ(cx, cy);
            return Math.Abs(cellZ - playerZ) < 15f;
        }
        
        public void MarkWalkableWorld(float worldX, float worldY, float z)
        {
            var (cx, cy) = SceneGridCalculator.WorldToGrid(worldX, worldY,
                new MockScene { PosX = PosX, PosY = PosY, MaxX = PosX + Width, MaxY = PosY + Height });
            MarkWalkable(cx, cy, z);
        }
        
        // Statistics
        public int WalkableCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _cellFlags.Length; i++)
                {
                    if ((_cellFlags[i] & FLAG_WALKABLE) != 0 && (_cellFlags[i] & FLAG_BLOCKED) == 0)
                        count++;
                }
                return count;
            }
        }
        
        public int ExploredCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _cellFlags.Length; i++)
                {
                    if ((_cellFlags[i] & FLAG_EXPLORED) != 0)
                        count++;
                }
                return count;
            }
        }
        
        public float ExplorationPercent
        {
            get
            {
                if (_cellFlags.Length == 0) return 0;
                return (ExploredCount * 100f) / _cellFlags.Length;
            }
        }
    }
    
    // Helper class for coordinate conversion
    internal class MockScene : IScene
    {
        public ISnoScene SnoScene => null;
        public ISnoArea SnoArea => null;
        public uint WorldSno => 0;
        public uint NavMeshId => 0;
        public uint SceneId => 0;
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float MaxX { get; set; }
        public float MaxY { get; set; }
        public float W => MaxX - PosX;
        public float H => MaxY - PosY;
        public float Z => 0;
        public string CalculatedPosId => "";
    }
}
