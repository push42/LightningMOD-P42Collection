namespace Turbo.Plugins.Custom.Navigation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// A* Pathfinder with advanced features:
    /// - Diagonal movement with proper costs
    /// - Danger avoidance weighting
    /// - Path smoothing
    /// - Jump Point Search optimization (optional)
    /// - Dynamic replanning
    /// </summary>
    public class AStarPathfinder
    {
        public NavGrid Grid { get; set; }
        
        // Pathfinding settings
        public float DiagonalCost { get; set; } = 1.414f;
        public float DangerPenalty { get; set; } = 10f;
        public float NearWallPenalty { get; set; } = 2f;
        public float ElitePenalty { get; set; } = 20f;
        public int MaxIterations { get; set; } = 5000;
        public bool AllowDiagonals { get; set; } = true;
        public bool SmoothPath { get; set; } = true;
        public bool AvoidDanger { get; set; } = true;

        private readonly int[] _dx = { -1, 1, 0, 0, -1, 1, -1, 1 };
        private readonly int[] _dy = { 0, 0, -1, 1, -1, -1, 1, 1 };

        public PathResult FindPath(float startX, float startY, float goalX, float goalY)
        {
            var result = new PathResult();
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var (sx, sy) = Grid.WorldToGrid(startX, startY);
            var (gx, gy) = Grid.WorldToGrid(goalX, goalY);

            if (!Grid.IsWalkable(gx, gy))
            {
                result.Status = PathStatus.GoalBlocked;
                return result;
            }

            var openSet = new SortedSet<NavCell>(new NavCellComparer());
            var closedSet = new HashSet<(int, int)>();
            var openDict = new Dictionary<(int, int), NavCell>();

            var startCell = Grid.GetOrCreateCell(sx, sy);
            startCell.GScore = 0;
            startCell.FScore = Heuristic(sx, sy, gx, gy);
            startCell.Parent = null;
            
            openSet.Add(startCell);
            openDict[(sx, sy)] = startCell;

            int iterations = 0;
            int directions = AllowDiagonals ? 8 : 4;

            while (openSet.Count > 0 && iterations < MaxIterations)
            {
                iterations++;
                
                var current = openSet.Min;
                openSet.Remove(current);
                openDict.Remove((current.X, current.Y));

                if (current.X == gx && current.Y == gy)
                {
                    result.Path = ReconstructPath(current);
                    result.Status = PathStatus.Found;
                    result.Iterations = iterations;
                    result.TimeMs = sw.ElapsedMilliseconds;
                    
                    if (SmoothPath)
                        result.Path = SmoothPathPoints(result.Path);
                    
                    return result;
                }

                closedSet.Add((current.X, current.Y));

                for (int i = 0; i < directions; i++)
                {
                    int nx = current.X + _dx[i];
                    int ny = current.Y + _dy[i];

                    if (closedSet.Contains((nx, ny))) continue;
                    if (!Grid.IsWalkable(nx, ny)) continue;

                    // Check diagonal blocking
                    if (i >= 4 && AllowDiagonals)
                    {
                        if (!Grid.IsWalkable(current.X + _dx[i], current.Y) ||
                            !Grid.IsWalkable(current.X, current.Y + _dy[i]))
                            continue;
                    }

                    float moveCost = i < 4 ? 1f : DiagonalCost;
                    float tentativeG = current.GScore + moveCost + GetCellCost(nx, ny);

                    NavCell neighbor;
                    bool inOpen = openDict.TryGetValue((nx, ny), out neighbor);

                    if (!inOpen)
                    {
                        neighbor = Grid.GetOrCreateCell(nx, ny);
                        neighbor.GScore = float.MaxValue;
                    }

                    if (tentativeG < neighbor.GScore)
                    {
                        neighbor.Parent = current;
                        neighbor.GScore = tentativeG;
                        neighbor.FScore = tentativeG + Heuristic(nx, ny, gx, gy);

                        if (inOpen)
                        {
                            openSet.Remove(neighbor);
                        }
                        openSet.Add(neighbor);
                        openDict[(nx, ny)] = neighbor;
                    }
                }
            }

            result.Status = iterations >= MaxIterations ? PathStatus.MaxIterations : PathStatus.NoPath;
            result.Iterations = iterations;
            result.TimeMs = sw.ElapsedMilliseconds;
            return result;
        }

        private float GetCellCost(int x, int y)
        {
            if (!AvoidDanger) return 0;
            
            float cost = 0;
            var cell = Grid.GetCell(x, y);
            
            if (cell != null)
            {
                if ((cell.Flags & NavCellFlags.Dangerous) != 0) cost += DangerPenalty;
                if ((cell.Flags & NavCellFlags.HasElite) != 0) cost += ElitePenalty;
                if ((cell.Flags & NavCellFlags.NearWall) != 0) cost += NearWallPenalty;
                if ((cell.Flags & NavCellFlags.HasGroundEffect) != 0) cost += DangerPenalty * 2;
            }
            
            return cost;
        }

        private float Heuristic(int x1, int y1, int x2, int y2)
        {
            // Octile distance for 8-directional movement
            int dx = Math.Abs(x1 - x2);
            int dy = Math.Abs(y1 - y2);
            return Math.Max(dx, dy) + (DiagonalCost - 1) * Math.Min(dx, dy);
        }

        private List<PathPoint> ReconstructPath(NavCell endCell)
        {
            var path = new List<PathPoint>();
            var current = endCell;

            while (current != null)
            {
                path.Add(new PathPoint
                {
                    GridX = current.X,
                    GridY = current.Y,
                    WorldX = current.WorldX,
                    WorldY = current.WorldY,
                    WorldZ = current.WorldZ,
                    Flags = current.Flags
                });
                current = current.Parent;
            }

            path.Reverse();
            return path;
        }

        private List<PathPoint> SmoothPathPoints(List<PathPoint> path)
        {
            if (path.Count <= 2) return path;

            var smoothed = new List<PathPoint> { path[0] };
            int current = 0;

            while (current < path.Count - 1)
            {
                int furthest = current + 1;
                
                // Find furthest visible point
                for (int test = path.Count - 1; test > current + 1; test--)
                {
                    if (HasLineOfSight(path[current], path[test]))
                    {
                        furthest = test;
                        break;
                    }
                }

                smoothed.Add(path[furthest]);
                current = furthest;
            }

            return smoothed;
        }

        private bool HasLineOfSight(PathPoint a, PathPoint b)
        {
            // Bresenham's line algorithm
            int x0 = a.GridX, y0 = a.GridY;
            int x1 = b.GridX, y1 = b.GridY;

            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                if (!Grid.IsWalkable(x0, y0)) return false;
                if (x0 == x1 && y0 == y1) break;

                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }

            return true;
        }

        /// <summary>
        /// Find a path that avoids all danger zones
        /// </summary>
        public PathResult FindSafePath(float startX, float startY, float goalX, float goalY)
        {
            float oldPenalty = DangerPenalty;
            DangerPenalty = 1000f; // Make danger zones nearly impassable
            var result = FindPath(startX, startY, goalX, goalY);
            DangerPenalty = oldPenalty;
            return result;
        }

        /// <summary>
        /// Find path to nearest safe position from current location
        /// </summary>
        public PathResult FindEscapePath(float startX, float startY, float dangerCenterX, float dangerCenterY, float minDistance = 20f)
        {
            var (sx, sy) = Grid.WorldToGrid(startX, startY);
            var (dx, dy) = Grid.WorldToGrid(dangerCenterX, dangerCenterY);

            // Calculate escape direction (away from danger)
            float dirX = startX - dangerCenterX;
            float dirY = startY - dangerCenterY;
            float len = (float)Math.Sqrt(dirX * dirX + dirY * dirY);
            
            if (len > 0)
            {
                dirX /= len;
                dirY /= len;
            }
            else
            {
                // Random direction if at danger center
                dirX = 1;
                dirY = 0;
            }

            // Find safe point in escape direction
            float goalX = startX + dirX * minDistance;
            float goalY = startY + dirY * minDistance;

            return FindSafePath(startX, startY, goalX, goalY);
        }
    }

    public class PathPoint
    {
        public int GridX { get; set; }
        public int GridY { get; set; }
        public float WorldX { get; set; }
        public float WorldY { get; set; }
        public float WorldZ { get; set; }
        public NavCellFlags Flags { get; set; }
    }

    public class PathResult
    {
        public List<PathPoint> Path { get; set; } = new List<PathPoint>();
        public PathStatus Status { get; set; } = PathStatus.None;
        public int Iterations { get; set; }
        public long TimeMs { get; set; }
        
        public bool IsValid => Status == PathStatus.Found && Path.Count > 0;
        public float TotalDistance => CalculateTotalDistance();
        
        private float CalculateTotalDistance()
        {
            if (Path.Count < 2) return 0;
            float total = 0;
            for (int i = 1; i < Path.Count; i++)
            {
                float dx = Path[i].WorldX - Path[i-1].WorldX;
                float dy = Path[i].WorldY - Path[i-1].WorldY;
                total += (float)Math.Sqrt(dx * dx + dy * dy);
            }
            return total;
        }
    }

    public enum PathStatus
    {
        None,
        Found,
        NoPath,
        GoalBlocked,
        MaxIterations
    }

    internal class NavCellComparer : IComparer<NavCell>
    {
        public int Compare(NavCell a, NavCell b)
        {
            int result = a.FScore.CompareTo(b.FScore);
            if (result == 0)
                result = a.GScore.CompareTo(b.GScore);
            if (result == 0)
                result = a.X.CompareTo(b.X);
            if (result == 0)
                result = a.Y.CompareTo(b.Y);
            return result;
        }
    }
}
