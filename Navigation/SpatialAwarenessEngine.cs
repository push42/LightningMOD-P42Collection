namespace Turbo.Plugins.Custom.Navigation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Turbo.Plugins.Default;
    using Turbo.Plugins.Custom.Navigation.NavMesh;

    /// <summary>
    /// Spatial Awareness System - God-Tier environmental analysis
    /// 
    /// Features:
    /// - Real-time danger zone mapping
    /// - Monster threat assessment
    /// - Ground effect tracking
    /// - Wall/obstacle detection using actual NavMesh
    /// - Safe zone identification
    /// - Escape route analysis
    /// - Density heatmaps
    /// </summary>
    public class SpatialAwarenessEngine
    {
        public IController Hud { get; set; }
        public NavGrid Grid { get; private set; }
        public AStarPathfinder Pathfinder { get; private set; }
        public NavMeshManager NavMesh { get; private set; }
        
        // Analysis results
        public List<DangerZoneInfo> DangerZones { get; private set; } = new List<DangerZoneInfo>();
        public List<ThreatInfo> Threats { get; private set; } = new List<ThreatInfo>();
        public List<SafeZoneInfo> SafeZones { get; private set; } = new List<SafeZoneInfo>();
        public List<EscapeRouteInfo> EscapeRoutes { get; private set; } = new List<EscapeRouteInfo>();
        public DensityMap MonsterDensity { get; private set; }
        public DensityMap DangerDensity { get; private set; }
        
        // Settings
        public float AnalysisRadius { get; set; } = 60f;
        public float DangerMultiplier { get; set; } = 1.3f;
        public float WallDetectionRange { get; set; } = 12f;
        public int EscapeDirections { get; set; } = 16;
        public float SafeZoneMinRadius { get; set; } = 8f;
        public bool TrackMonsterMovement { get; set; } = true;
        
        // Ground effect definitions
        private Dictionary<ActorSnoEnum, GroundEffectDef> _groundEffects;
        
        // Monster position history for movement prediction
        private Dictionary<uint, Queue<MonsterSnapshot>> _monsterHistory = 
            new Dictionary<uint, Queue<MonsterSnapshot>>();
        private const int HISTORY_SIZE = 10;
        
        // Debug info
        public string DebugInfo { get; private set; } = "";

        public SpatialAwarenessEngine()
        {
            Grid = new NavGrid();
            Pathfinder = new AStarPathfinder { Grid = Grid };
            NavMesh = new NavMeshManager();
            MonsterDensity = new DensityMap();
            DangerDensity = new DensityMap();
            InitializeGroundEffects();
        }

        private void InitializeGroundEffects()
        {
            _groundEffects = new Dictionary<ActorSnoEnum, GroundEffectDef>
            {
                // Critical - Instant death potential
                { ActorSnoEnum._monsteraffix_frozen_iceclusters, new GroundEffectDef("Frozen", 16f, 100, DangerLevel.Critical) },
                { ActorSnoEnum._monsteraffix_molten_deathstart_proxy, new GroundEffectDef("Molten Explosion", 14f, 100, DangerLevel.Critical) },
                { ActorSnoEnum._monsteraffix_molten_deathexplosion_proxy, new GroundEffectDef("Molten", 14f, 90, DangerLevel.Critical) },
                { ActorSnoEnum._monsteraffix_arcaneenchanted_petsweep, new GroundEffectDef("Arcane", 42f, 95, DangerLevel.Critical) },
                { ActorSnoEnum._monsteraffix_arcaneenchanted_petsweep_reverse, new GroundEffectDef("Arcane", 42f, 95, DangerLevel.Critical) },
                
                // High - Significant damage
                { ActorSnoEnum._monsteraffix_molten_firering, new GroundEffectDef("Molten Trail", 14f, 85, DangerLevel.High) },
                { ActorSnoEnum._arcaneenchanteddummy_spawn, new GroundEffectDef("Arcane Spawn", 8f, 80, DangerLevel.High) },
                { ActorSnoEnum._monsteraffix_desecrator_damage_aoe, new GroundEffectDef("Desecrator", 9f, 75, DangerLevel.High) },
                { ActorSnoEnum._x1_monsteraffix_thunderstorm_impact, new GroundEffectDef("Thunderstorm", 18f, 70, DangerLevel.High) },
                
                // Medium - Moderate damage
                { ActorSnoEnum._x1_monsteraffix_frozenpulse_monster, new GroundEffectDef("Frozen Pulse", 16f, 65, DangerLevel.Medium) },
                { ActorSnoEnum._monsteraffix_plagued_endcloud, new GroundEffectDef("Plagued", 14f, 60, DangerLevel.Medium) },
                { ActorSnoEnum._creepmobarm, new GroundEffectDef("Creep", 14f, 60, DangerLevel.Medium) },
                { ActorSnoEnum._gluttony_gascloud_proxy, new GroundEffectDef("Ghom Gas", 22f, 55, DangerLevel.Medium) },
                
                // Low - Minor threat
                { ActorSnoEnum._x1_monsteraffix_teleportmines, new GroundEffectDef("Wormhole", 5f, 40, DangerLevel.Low) },
                { ActorSnoEnum._x1_monsteraffix_corpsebomber_projectile, new GroundEffectDef("Poison", 6f, 35, DangerLevel.Low) },
                { ActorSnoEnum._x1_monsteraffix_orbiter_projectile, new GroundEffectDef("Orbiter", 4f, 30, DangerLevel.Low) },
            };
        }

        /// <summary>
        /// Full spatial analysis update - call each frame
        /// </summary>
        public void Update()
        {
            if (Hud?.Game?.Me == null || !Hud.Game.IsInGame || Hud.Game.IsInTown)
            {
                Clear();
                DebugInfo = "Not active";
                return;
            }

            // Initialize NavMesh if needed
            if (NavMesh.Hud == null)
            {
                NavMesh.Hud = Hud;
                NavMesh.Initialize();
            }

            var myPos = Hud.Game.Me.FloorCoordinate;
            var scene = Hud.Game.Me.Scene;
            
            if (scene == null) 
            {
                DebugInfo = "No scene";
                return;
            }

            // Update NavMesh data
            NavMesh.Update();

            // Update grid bounds based on current scene
            Grid.Initialize(
                scene.PosX - 20, scene.PosY - 20,
                scene.MaxX + 20, scene.MaxY + 20
            );

            // Clear dynamic data
            Grid.ClearDynamicFlags();
            DangerZones.Clear();
            Threats.Clear();
            SafeZones.Clear();
            EscapeRoutes.Clear();

            // Analyze environment using actual NavMesh
            AnalyzeWalkability(myPos);
            AnalyzeGroundEffects(myPos);
            AnalyzeMonsters(myPos);
            AnalyzeAvoidables(myPos);
            
            // Build density maps
            UpdateDensityMaps(myPos);
            
            // Find safe zones and escape routes
            FindSafeZones(myPos);
            CalculateEscapeRoutes(myPos);
            
            DebugInfo = $"Nav:{NavMesh.LastUpdateInfo} D:{DangerZones.Count} T:{Threats.Count} S:{SafeZones.Count} E:{EscapeRoutes.Count(r=>!r.IsBlocked)}";
        }

        /// <summary>
        /// Analyze walkability using actual NavMesh data
        /// </summary>
        private void AnalyzeWalkability(IWorldCoordinate myPos)
        {
            float checkStep = 2.5f;
            
            for (float dx = -AnalysisRadius; dx <= AnalysisRadius; dx += checkStep)
            {
                for (float dy = -AnalysisRadius; dy <= AnalysisRadius; dy += checkStep)
                {
                    float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                    if (dist > AnalysisRadius) continue;
                    
                    float x = myPos.X + dx;
                    float y = myPos.Y + dy;
                    
                    var (gx, gy) = Grid.WorldToGrid(x, y);
                    
                    // Use NavMesh for walkability check
                    bool walkable = NavMesh.IsWalkable(x, y);
                    
                    if (!walkable)
                    {
                        Grid.SetCellFlags(gx, gy, NavCellFlags.Blocked);
                    }
                    else
                    {
                        // Check if near non-walkable area (wall detection)
                        bool nearWall = false;
                        float wallCheckDist = WallDetectionRange;
                        
                        for (float angle = 0; angle < 360; angle += 45)
                        {
                            float rad = angle * (float)Math.PI / 180f;
                            float checkX = x + (float)Math.Cos(rad) * wallCheckDist;
                            float checkY = y + (float)Math.Sin(rad) * wallCheckDist;
                            
                            if (!NavMesh.IsWalkable(checkX, checkY))
                            {
                                nearWall = true;
                                break;
                            }
                        }
                        
                        if (nearWall)
                        {
                            Grid.SetCellFlags(gx, gy, NavCellFlags.NearWall);
                        }
                    }
                }
            }
        }

        private void AnalyzeGroundEffects(IWorldCoordinate myPos)
        {
            foreach (var actor in Hud.Game.Actors)
            {
                if (!_groundEffects.TryGetValue(actor.SnoActor.Sno, out var def)) continue;
                
                float distance = myPos.XYDistanceTo(actor.FloorCoordinate);
                if (distance > AnalysisRadius) continue;

                float effectiveRadius = def.Radius * DangerMultiplier;
                
                // Add to danger zones list
                DangerZones.Add(new DangerZoneInfo
                {
                    Name = def.Name,
                    Position = actor.FloorCoordinate,
                    Radius = effectiveRadius,
                    Priority = def.Priority,
                    Level = def.Level,
                    Distance = distance,
                    Actor = actor
                });

                // Mark grid cells
                MarkDangerCells(actor.FloorCoordinate.X, actor.FloorCoordinate.Y, effectiveRadius, 
                    NavCellFlags.HasGroundEffect | NavCellFlags.Dangerous);
            }
        }

        private void AnalyzeMonsters(IWorldCoordinate myPos)
        {
            foreach (var monster in Hud.Game.AliveMonsters)
            {
                float distance = myPos.XYDistanceTo(monster.FloorCoordinate);
                if (distance > AnalysisRadius) continue;

                // Track movement history
                if (TrackMonsterMovement)
                    UpdateMonsterHistory(monster);

                // Assess threat
                var threat = AssessMonsterThreat(monster, myPos, distance);
                Threats.Add(threat);

                // Mark grid cells
                var flags = NavCellFlags.HasMonster;
                if (threat.IsElite) flags |= NavCellFlags.HasElite;
                if (threat.ThreatLevel >= ThreatLevel.High) flags |= NavCellFlags.Dangerous;
                
                MarkDangerCells(monster.FloorCoordinate.X, monster.FloorCoordinate.Y, 
                    threat.ThreatRadius, flags);
            }

            // Cleanup old history
            CleanupMonsterHistory();
        }

        private ThreatInfo AssessMonsterThreat(IMonster monster, IWorldCoordinate myPos, float distance)
        {
            var threat = new ThreatInfo
            {
                Monster = monster,
                Position = monster.FloorCoordinate,
                Distance = distance,
                IsElite = monster.Rarity != ActorRarity.Normal
            };

            // Base threat radius
            threat.ThreatRadius = monster.RadiusBottom + 3f;

            // Assess threat level based on rarity and distance
            switch (monster.Rarity)
            {
                case ActorRarity.Boss:
                    threat.ThreatLevel = ThreatLevel.Critical;
                    threat.ThreatRadius += 10f;
                    threat.Priority = 100;
                    break;
                case ActorRarity.Champion:
                case ActorRarity.Rare:
                    threat.ThreatLevel = distance < 15 ? ThreatLevel.High : ThreatLevel.Medium;
                    threat.ThreatRadius += 5f;
                    threat.Priority = 80;
                    break;
                case ActorRarity.RareMinion:
                    threat.ThreatLevel = distance < 10 ? ThreatLevel.Medium : ThreatLevel.Low;
                    threat.ThreatRadius += 3f;
                    threat.Priority = 60;
                    break;
                default:
                    threat.ThreatLevel = distance < 8 ? ThreatLevel.Low : ThreatLevel.Minimal;
                    threat.Priority = 20;
                    break;
            }

            // Predict movement
            if (TrackMonsterMovement)
            {
                var velocity = GetMonsterVelocity(monster.AnnId);
                if (velocity.HasValue)
                {
                    threat.PredictedX = monster.FloorCoordinate.X + velocity.Value.vx * 0.5f;
                    threat.PredictedY = monster.FloorCoordinate.Y + velocity.Value.vy * 0.5f;
                    threat.IsMovingTowardPlayer = IsMovingToward(monster, myPos, velocity.Value);
                    
                    if (threat.IsMovingTowardPlayer)
                        threat.ThreatLevel = (ThreatLevel)Math.Min((int)threat.ThreatLevel + 1, (int)ThreatLevel.Critical);
                }
            }

            return threat;
        }

        private void AnalyzeAvoidables(IWorldCoordinate myPos)
        {
            foreach (var avoid in Hud.Game.Me.AvoidablesInRange)
            {
                float distance = myPos.XYDistanceTo(avoid.FloorCoordinate);
                float effectiveRadius = avoid.AvoidableDefinition.Radius * DangerMultiplier;

                DangerZones.Add(new DangerZoneInfo
                {
                    Name = "Avoidable",
                    Position = avoid.FloorCoordinate,
                    Radius = effectiveRadius,
                    Priority = avoid.AvoidableDefinition.InstantDeath ? 100 : 70,
                    Level = avoid.AvoidableDefinition.InstantDeath ? DangerLevel.Critical : DangerLevel.High,
                    Distance = distance,
                    IsAvoidable = true
                });

                MarkDangerCells(avoid.FloorCoordinate.X, avoid.FloorCoordinate.Y, effectiveRadius,
                    NavCellFlags.Dangerous | NavCellFlags.HasGroundEffect);
            }
        }

        private void MarkDangerCells(float centerX, float centerY, float radius, NavCellFlags flags)
        {
            foreach (var cell in Grid.GetCellsInRadius(centerX, centerY, radius))
            {
                Grid.SetCellFlags(cell.X, cell.Y, flags);
            }
        }

        private void UpdateDensityMaps(IWorldCoordinate myPos)
        {
            MonsterDensity.Clear();
            DangerDensity.Clear();

            foreach (var threat in Threats)
            {
                MonsterDensity.AddPoint(threat.Position.X, threat.Position.Y, threat.Priority);
            }

            foreach (var danger in DangerZones)
            {
                DangerDensity.AddPoint(danger.Position.X, danger.Position.Y, danger.Priority);
            }
        }

        private void FindSafeZones(IWorldCoordinate myPos)
        {
            float sampleStep = SafeZoneMinRadius;
            
            for (float dx = -AnalysisRadius; dx <= AnalysisRadius; dx += sampleStep)
            {
                for (float dy = -AnalysisRadius; dy <= AnalysisRadius; dy += sampleStep)
                {
                    float x = myPos.X + dx;
                    float y = myPos.Y + dy;
                    float distFromPlayer = (float)Math.Sqrt(dx * dx + dy * dy);
                    
                    if (distFromPlayer > AnalysisRadius) continue;
                    
                    // Must be walkable according to NavMesh
                    if (!NavMesh.IsWalkable(x, y)) continue;
                    
                    float safetyScore = CalculateSafetyScore(x, y);
                    
                    if (safetyScore > 50)
                    {
                        SafeZones.Add(new SafeZoneInfo
                        {
                            X = x,
                            Y = y,
                            SafetyScore = safetyScore,
                            DistanceFromPlayer = distFromPlayer
                        });
                    }
                }
            }

            SafeZones.Sort((a, b) => b.SafetyScore.CompareTo(a.SafetyScore));
        }

        private float CalculateSafetyScore(float x, float y)
        {
            float score = 100;

            // Must be walkable
            if (!NavMesh.IsWalkable(x, y)) return -1;

            // Distance from dangers
            foreach (var danger in DangerZones)
            {
                float dist = (float)Math.Sqrt(
                    Math.Pow(x - danger.Position.X, 2) +
                    Math.Pow(y - danger.Position.Y, 2));
                
                if (dist < danger.Radius)
                    return -1; // Inside danger
                    
                if (dist < danger.Radius * 2)
                    score -= (danger.Priority * (1 - dist / (danger.Radius * 2)));
            }

            // Distance from monsters
            foreach (var threat in Threats.Where(t => t.IsElite || t.ThreatLevel >= ThreatLevel.Medium))
            {
                float dist = (float)Math.Sqrt(
                    Math.Pow(x - threat.Position.X, 2) +
                    Math.Pow(y - threat.Position.Y, 2));
                
                if (dist < threat.ThreatRadius * 2)
                    score -= (threat.Priority * 0.5f * (1 - dist / (threat.ThreatRadius * 2)));
            }

            return Math.Max(0, score);
        }

        private void CalculateEscapeRoutes(IWorldCoordinate myPos)
        {
            float angleStep = 360f / EscapeDirections;
            float escapeDistance = 15f;

            for (int i = 0; i < EscapeDirections; i++)
            {
                float angle = i * angleStep;
                float radians = angle * (float)Math.PI / 180f;
                
                float dirX = (float)Math.Cos(radians);
                float dirY = (float)Math.Sin(radians);
                
                float targetX = myPos.X + dirX * escapeDistance;
                float targetY = myPos.Y + dirY * escapeDistance;

                var route = new EscapeRouteInfo
                {
                    Angle = angle,
                    DirectionX = dirX,
                    DirectionY = dirY,
                    TargetX = targetX,
                    TargetY = targetY
                };

                // Check if target is walkable using NavMesh
                route.IsBlocked = !NavMesh.IsWalkable(targetX, targetY);

                if (!route.IsBlocked)
                {
                    // Check if path is clear (line of walkable cells)
                    route.HasLineOfSight = HasClearPath(myPos.X, myPos.Y, targetX, targetY);
                    
                    // Calculate safety score
                    route.SafetyScore = CalculateSafetyScore(targetX, targetY);
                    
                    // Mark as having path if direct line is clear
                    route.HasPath = route.HasLineOfSight;
                    route.PathLength = escapeDistance;
                    
                    // If no direct line, try to find alternate path
                    if (!route.HasLineOfSight)
                    {
                        var pathResult = Pathfinder.FindPath(myPos.X, myPos.Y, targetX, targetY);
                        route.HasPath = pathResult.IsValid;
                        route.PathLength = pathResult.TotalDistance;
                    }
                }
                else
                {
                    route.SafetyScore = -100;
                }

                EscapeRoutes.Add(route);
            }

            EscapeRoutes.Sort((a, b) => b.SafetyScore.CompareTo(a.SafetyScore));
        }

        /// <summary>
        /// Check if there's a clear walkable path between two points
        /// </summary>
        private bool HasClearPath(float x1, float y1, float x2, float y2)
        {
            float dist = (float)Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
            int steps = Math.Max(3, (int)(dist / 2f)); // Check every 2 units
            
            float dx = (x2 - x1) / steps;
            float dy = (y2 - y1) / steps;
            
            for (int i = 1; i <= steps; i++)
            {
                float checkX = x1 + dx * i;
                float checkY = y1 + dy * i;
                
                if (!NavMesh.IsWalkable(checkX, checkY))
                    return false;
            }
            
            return true;
        }

        #region Monster Tracking

        private void UpdateMonsterHistory(IMonster monster)
        {
            if (!_monsterHistory.TryGetValue(monster.AnnId, out var history))
            {
                history = new Queue<MonsterSnapshot>();
                _monsterHistory[monster.AnnId] = history;
            }

            history.Enqueue(new MonsterSnapshot
            {
                X = monster.FloorCoordinate.X,
                Y = monster.FloorCoordinate.Y,
                Tick = Hud.Game.CurrentGameTick
            });

            while (history.Count > HISTORY_SIZE)
                history.Dequeue();
        }

        private (float vx, float vy)? GetMonsterVelocity(uint annId)
        {
            if (!_monsterHistory.TryGetValue(annId, out var history) || history.Count < 2)
                return null;

            var snapshots = history.ToArray();
            var oldest = snapshots[0];
            var newest = snapshots[snapshots.Length - 1];

            int tickDiff = newest.Tick - oldest.Tick;
            if (tickDiff <= 0) return null;

            float vx = (newest.X - oldest.X) / tickDiff * 60;
            float vy = (newest.Y - oldest.Y) / tickDiff * 60;

            return (vx, vy);
        }

        private bool IsMovingToward(IMonster monster, IWorldCoordinate target, (float vx, float vy) velocity)
        {
            float toTargetX = target.X - monster.FloorCoordinate.X;
            float toTargetY = target.Y - monster.FloorCoordinate.Y;
            
            // Dot product
            float dot = velocity.vx * toTargetX + velocity.vy * toTargetY;
            return dot > 0;
        }

        private void CleanupMonsterHistory()
        {
            var aliveIds = new HashSet<uint>(Hud.Game.AliveMonsters.Select(m => m.AnnId));
            var toRemove = _monsterHistory.Keys.Where(id => !aliveIds.Contains(id)).ToList();
            
            foreach (var id in toRemove)
                _monsterHistory.Remove(id);
        }

        #endregion

        public void Clear()
        {
            Grid.Clear();
            DangerZones.Clear();
            Threats.Clear();
            SafeZones.Clear();
            EscapeRoutes.Clear();
            MonsterDensity.Clear();
            DangerDensity.Clear();
        }

        /// <summary>
        /// Get best escape route away from current dangers
        /// </summary>
        public EscapeRouteInfo GetBestEscapeRoute()
        {
            return EscapeRoutes.FirstOrDefault(r => !r.IsBlocked && r.HasPath && r.SafetyScore > 30);
        }

        /// <summary>
        /// Get safest reachable position
        /// </summary>
        public SafeZoneInfo GetSafestPosition()
        {
            return SafeZones.FirstOrDefault(s => s.SafetyScore > 50);
        }

        /// <summary>
        /// Check if position is currently safe
        /// </summary>
        public bool IsPositionSafe(float x, float y, float minSafety = 50)
        {
            return CalculateSafetyScore(x, y) >= minSafety;
        }
    }

    #region Data Classes

    public class DangerZoneInfo
    {
        public string Name { get; set; }
        public IWorldCoordinate Position { get; set; }
        public float Radius { get; set; }
        public int Priority { get; set; }
        public DangerLevel Level { get; set; }
        public float Distance { get; set; }
        public IActor Actor { get; set; }
        public bool IsAvoidable { get; set; }
    }

    public class ThreatInfo
    {
        public IMonster Monster { get; set; }
        public IWorldCoordinate Position { get; set; }
        public float Distance { get; set; }
        public float ThreatRadius { get; set; }
        public ThreatLevel ThreatLevel { get; set; }
        public int Priority { get; set; }
        public bool IsElite { get; set; }
        public float PredictedX { get; set; }
        public float PredictedY { get; set; }
        public bool IsMovingTowardPlayer { get; set; }
    }

    public class SafeZoneInfo
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float SafetyScore { get; set; }
        public float DistanceFromPlayer { get; set; }
    }

    public class EscapeRouteInfo
    {
        public float Angle { get; set; }
        public float DirectionX { get; set; }
        public float DirectionY { get; set; }
        public float TargetX { get; set; }
        public float TargetY { get; set; }
        public float SafetyScore { get; set; }
        public bool IsBlocked { get; set; }
        public bool HasLineOfSight { get; set; }
        public bool HasPath { get; set; }
        public float PathLength { get; set; }
    }

    public class GroundEffectDef
    {
        public string Name { get; }
        public float Radius { get; }
        public int Priority { get; }
        public DangerLevel Level { get; }

        public GroundEffectDef(string name, float radius, int priority, DangerLevel level)
        {
            Name = name;
            Radius = radius;
            Priority = priority;
            Level = level;
        }
    }

    public class MonsterSnapshot
    {
        public float X { get; set; }
        public float Y { get; set; }
        public int Tick { get; set; }
    }

    public enum DangerLevel
    {
        None,
        Low,
        Medium,
        High,
        Critical
    }

    public enum ThreatLevel
    {
        Minimal,
        Low,
        Medium,
        High,
        Critical
    }

    public class DensityMap
    {
        private Dictionary<(int, int), float> _density = new Dictionary<(int, int), float>();
        private const float CELL_SIZE = 5f;

        public void AddPoint(float x, float y, float weight)
        {
            var (gx, gy) = ((int)(x / CELL_SIZE), (int)(y / CELL_SIZE));
            
            if (!_density.ContainsKey((gx, gy)))
                _density[(gx, gy)] = 0;
            
            _density[(gx, gy)] += weight;
        }

        public float GetDensity(float x, float y)
        {
            var (gx, gy) = ((int)(x / CELL_SIZE), (int)(y / CELL_SIZE));
            return _density.TryGetValue((gx, gy), out var d) ? d : 0;
        }

        public (float x, float y, float density) GetHighestDensity()
        {
            if (_density.Count == 0) return (0, 0, 0);
            
            var max = _density.OrderByDescending(kv => kv.Value).First();
            return (max.Key.Item1 * CELL_SIZE + CELL_SIZE / 2,
                    max.Key.Item2 * CELL_SIZE + CELL_SIZE / 2,
                    max.Value);
        }

        public void Clear() => _density.Clear();
    }

    #endregion
}
