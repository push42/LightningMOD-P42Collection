namespace Turbo.Plugins.Custom.Navigation.NavMesh
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Newtonsoft.Json;

    /// <summary>
    /// Extracts NavMesh data from D3 CASC files
    /// 
    /// File Structure (Scene SNO with NavMesh):
    /// - SNO Header
    /// - Scene Data
    /// - NavMesh Data:
    ///   - NavZone Count
    ///   - For each NavZone:
    ///     - Zone Header (bounds, flags)
    ///     - Cell Count
    ///     - For each Cell:
    ///       - Bounds (MinX, MinY, MinZ, MaxX, MaxY, MaxZ)
    ///       - Flags
    ///       - Neighbor count
    ///       - Neighbor indices
    /// </summary>
    public class NavMeshExtractor
    {
        public string D3InstallPath { get; set; }
        public string OutputPath { get; set; }
        public bool VerboseLogging { get; set; } = true;
        
        private const uint SNO_MAGIC = 0xDEADBEEF;
        private const int SNO_TYPE_SCENE = 33; // Scene SNO type ID
        
        // Known offsets (these may vary by D3 version)
        private const int NAVMESH_OFFSET_IN_SCENE = 0x180; // Approximate offset to NavMesh data
        
        public NavMeshExtractor(string d3Path = null, string outputPath = null)
        {
            D3InstallPath = d3Path ?? @"C:\Program Files (x86)\Diablo III";
            OutputPath = outputPath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "TurboHUD", "NavMeshData"
            );
        }

        /// <summary>
        /// Extract all NavMesh data from D3 installation
        /// This requires CASCLib to be available
        /// </summary>
        public void ExtractAll()
        {
            Log("Starting NavMesh extraction...");
            Log($"D3 Path: {D3InstallPath}");
            Log($"Output Path: {OutputPath}");
            
            Directory.CreateDirectory(OutputPath);
            
            // Note: This is a placeholder for actual CASC extraction
            // In practice, you would use CASCLib here
            Log("WARNING: Full CASC extraction requires CASCLib NuGet package");
            Log("For now, generating sample NavMesh data for testing...");
            
            GenerateSampleNavMeshData();
        }

        /// <summary>
        /// Parse a raw NavMesh file (already extracted from CASC)
        /// </summary>
        public D3SceneNavMesh ParseNavMeshFile(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            using (var reader = new BinaryReader(stream))
            {
                return ParseNavMesh(reader, (uint)Path.GetFileNameWithoutExtension(filePath).GetHashCode());
            }
        }

        /// <summary>
        /// Parse NavMesh from binary data
        /// </summary>
        public D3SceneNavMesh ParseNavMesh(BinaryReader reader, uint sceneSnoId)
        {
            var navMesh = new D3SceneNavMesh
            {
                SceneSnoId = sceneSnoId
            };

            try
            {
                // Try to read as raw NavMesh format
                // This format is: ZoneCount, then Zone data
                
                int zoneCount = reader.ReadInt32();
                if (zoneCount <= 0 || zoneCount > 1000)
                {
                    // Invalid zone count, try alternative format
                    reader.BaseStream.Seek(0, SeekOrigin.Begin);
                    return ParseNavMeshAlternative(reader, sceneSnoId);
                }

                Log($"Parsing NavMesh with {zoneCount} zones");

                for (int z = 0; z < zoneCount; z++)
                {
                    var zone = ParseNavZone(reader, z);
                    if (zone != null)
                    {
                        navMesh.Zones.Add(zone);
                        
                        // Update bounds
                        if (navMesh.MinX > zone.MinX || z == 0) navMesh.MinX = zone.MinX;
                        if (navMesh.MinY > zone.MinY || z == 0) navMesh.MinY = zone.MinY;
                        if (navMesh.MaxX < zone.MaxX || z == 0) navMesh.MaxX = zone.MaxX;
                        if (navMesh.MaxY < zone.MaxY || z == 0) navMesh.MaxY = zone.MaxY;
                    }
                }

                navMesh.BuildLookupGrid();
            }
            catch (Exception ex)
            {
                Log($"Error parsing NavMesh: {ex.Message}");
            }

            return navMesh;
        }

        private D3NavZone ParseNavZone(BinaryReader reader, int zoneId)
        {
            var zone = new D3NavZone { ZoneId = zoneId };

            try
            {
                // Zone header
                zone.SceneSnoId = reader.ReadUInt32();
                zone.LevelAreaSnoId = reader.ReadInt32();
                
                // Bounds
                zone.MinX = reader.ReadSingle();
                zone.MinY = reader.ReadSingle();
                zone.MinZ = reader.ReadSingle();
                zone.MaxX = reader.ReadSingle();
                zone.MaxY = reader.ReadSingle();
                zone.MaxZ = reader.ReadSingle();

                // Cell count
                int cellCount = reader.ReadInt32();
                if (cellCount <= 0 || cellCount > 100000)
                {
                    Log($"Invalid cell count {cellCount} in zone {zoneId}");
                    return null;
                }

                // Parse cells
                for (int c = 0; c < cellCount; c++)
                {
                    var cell = ParseNavCell(reader);
                    if (cell != null)
                        zone.Cells.Add(cell);
                }

                // Connection count
                int connectionCount = reader.ReadInt32();
                if (connectionCount > 0 && connectionCount < 1000000)
                {
                    for (int i = 0; i < connectionCount; i++)
                    {
                        zone.Connections.Add(new D3NavCellConnection
                        {
                            FromCellIndex = reader.ReadInt32(),
                            ToCellIndex = reader.ReadInt32(),
                            Cost = reader.ReadSingle()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error parsing NavZone {zoneId}: {ex.Message}");
                return null;
            }

            return zone;
        }

        private D3NavCell ParseNavCell(BinaryReader reader)
        {
            var cell = new D3NavCell();

            cell.MinX = reader.ReadSingle();
            cell.MinY = reader.ReadSingle();
            cell.MinZ = reader.ReadSingle();
            cell.MaxX = reader.ReadSingle();
            cell.MaxY = reader.ReadSingle();
            cell.MaxZ = reader.ReadSingle();
            cell.Flags = (D3NavCellFlags)reader.ReadUInt16();
            cell.NeighborCount = reader.ReadUInt16();

            if (cell.NeighborCount > 0 && cell.NeighborCount < 100)
            {
                cell.NeighborIndices = new int[cell.NeighborCount];
                for (int i = 0; i < cell.NeighborCount; i++)
                {
                    cell.NeighborIndices[i] = reader.ReadInt32();
                }
            }

            return cell;
        }

        private D3SceneNavMesh ParseNavMeshAlternative(BinaryReader reader, uint sceneSnoId)
        {
            // Alternative parsing for different file formats
            var navMesh = new D3SceneNavMesh { SceneSnoId = sceneSnoId };
            
            // Try to detect format by reading first few bytes
            uint magic = reader.ReadUInt32();
            
            if (magic == SNO_MAGIC)
            {
                // This is a full SNO file, need to find NavMesh section
                return ParseNavMeshFromSno(reader, sceneSnoId);
            }
            
            // Unknown format
            Log($"Unknown NavMesh format, magic: 0x{magic:X8}");
            return navMesh;
        }

        private D3SceneNavMesh ParseNavMeshFromSno(BinaryReader reader, uint sceneSnoId)
        {
            var navMesh = new D3SceneNavMesh { SceneSnoId = sceneSnoId };
            
            // Skip SNO header
            reader.BaseStream.Seek(NAVMESH_OFFSET_IN_SCENE, SeekOrigin.Begin);
            
            // Try to find NavMesh data marker
            // This is approximate and may need adjustment
            
            Log("Parsing NavMesh from SNO format (experimental)");
            
            // For now, return empty mesh
            return navMesh;
        }

        /// <summary>
        /// Generate sample NavMesh data for testing the system
        /// </summary>
        private void GenerateSampleNavMeshData()
        {
            Log("Generating sample NavMesh data...");

            // Create sample data for common GR scenes
            var grScenes = new Dictionary<string, (float w, float h)>
            {
                { "GR_Standard_64x64", (160f, 160f) },
                { "GR_Corridor_128x32", (320f, 80f) },
                { "GR_OpenArea_96x96", (240f, 240f) },
                { "GR_Boss_Arena_48x48", (120f, 120f) }
            };

            foreach (var scene in grScenes)
            {
                var navMesh = GenerateSampleScene(scene.Key, scene.Value.w, scene.Value.h);
                SaveNavMesh(navMesh, scene.Key);
            }

            Log($"Sample data saved to {OutputPath}");
        }

        private D3SceneNavMesh GenerateSampleScene(string name, float width, float height)
        {
            var navMesh = new D3SceneNavMesh
            {
                SceneSnoId = (uint)name.GetHashCode(),
                SceneName = name,
                Version = 1,
                MinX = 0,
                MinY = 0,
                MaxX = width,
                MaxY = height
            };

            var zone = new D3NavZone
            {
                ZoneId = 0,
                SceneSnoId = navMesh.SceneSnoId,
                MinX = 0,
                MinY = 0,
                MaxX = width,
                MaxY = height
            };

            // Generate grid of cells
            float cellSize = 2.5f;
            int cellsX = (int)(width / cellSize);
            int cellsY = (int)(height / cellSize);
            
            Random rng = new Random(name.GetHashCode());

            for (int x = 0; x < cellsX; x++)
            {
                for (int y = 0; y < cellsY; y++)
                {
                    float minX = x * cellSize;
                    float minY = y * cellSize;
                    
                    // Determine if walkable (90% walkable, with some obstacles)
                    bool isWalkable = rng.NextDouble() > 0.1;
                    
                    // Add obstacles near edges
                    if (x == 0 || y == 0 || x == cellsX - 1 || y == cellsY - 1)
                        isWalkable = false;
                    
                    // Add some random pillars/obstacles
                    if (isWalkable && rng.NextDouble() < 0.02)
                        isWalkable = false;

                    var cell = new D3NavCell
                    {
                        MinX = minX,
                        MinY = minY,
                        MinZ = 0,
                        MaxX = minX + cellSize,
                        MaxY = minY + cellSize,
                        MaxZ = 10,
                        Flags = isWalkable ? D3NavCellFlags.AllowWalk | D3NavCellFlags.AllowProjectile 
                                          : D3NavCellFlags.None
                    };

                    zone.Cells.Add(cell);
                }
            }

            navMesh.Zones.Add(zone);
            navMesh.BuildLookupGrid();

            return navMesh;
        }

        /// <summary>
        /// Save NavMesh to JSON for caching
        /// </summary>
        public void SaveNavMesh(D3SceneNavMesh navMesh, string filename)
        {
            var path = Path.Combine(OutputPath, $"{filename}.json");
            var json = JsonConvert.SerializeObject(navMesh, Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
            File.WriteAllText(path, json);
            Log($"Saved: {path}");
        }

        /// <summary>
        /// Load NavMesh from cached JSON
        /// </summary>
        public D3SceneNavMesh LoadNavMesh(string filename)
        {
            var path = Path.Combine(OutputPath, $"{filename}.json");
            if (!File.Exists(path)) return null;
            
            var json = File.ReadAllText(path);
            var navMesh = JsonConvert.DeserializeObject<D3SceneNavMesh>(json);
            navMesh?.BuildLookupGrid();
            return navMesh;
        }

        /// <summary>
        /// Load all cached NavMeshes
        /// </summary>
        public Dictionary<string, D3SceneNavMesh> LoadAllCached()
        {
            var result = new Dictionary<string, D3SceneNavMesh>();
            
            if (!Directory.Exists(OutputPath)) return result;
            
            foreach (var file in Directory.GetFiles(OutputPath, "*.json"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                var navMesh = LoadNavMesh(name);
                if (navMesh != null)
                    result[name] = navMesh;
            }
            
            return result;
        }

        private void Log(string message)
        {
            if (VerboseLogging)
                Console.WriteLine($"[NavMeshExtractor] {message}");
        }
    }
}
