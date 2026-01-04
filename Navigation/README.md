# God-Tier Navigation System

Advanced navigation and spatial awareness system for TurboHUD with A* pathfinding, danger avoidance, and real-time environmental analysis.

---

## ?? Features

### Spatial Awareness Engine
- **Real-time danger zone mapping** - Tracks all ground effects with priorities
- **Monster threat assessment** - Evaluates threats based on type, distance, and movement
- **Movement prediction** - Predicts where monsters are heading
- **Safe zone identification** - Finds optimal safe positions
- **Escape route analysis** - Evaluates 16+ escape directions
- **Wall/obstacle detection** - Knows where you can and can't go
- **Density heatmaps** - Visualize monster/danger concentration

### A* Pathfinding
- **Optimal path calculation** - Always finds the best route
- **Danger avoidance** - Automatically routes around threats
- **Path smoothing** - Removes unnecessary waypoints
- **Dynamic replanning** - Updates path when situation changes
- **Diagonal movement** - Natural 8-directional movement
- **Line of sight optimization** - Skips waypoints when possible

### Auto-Navigation
- **Click-to-navigate** - Set destination with right-click
- **Safe pathing** - Always takes the safest route
- **Automatic rerouting** - Adapts to new dangers
- **Target tracking** - Navigates to moving targets

---

## ?? Hotkeys

| Key | Action |
|-----|--------|
| **Ctrl+N** | Toggle navigation overlay |
| **Ctrl+Shift+N** | Toggle auto-navigation mode |

---

## ?? Visual Indicators

### Danger Zones
| Color | Danger Level | Examples |
|-------|--------------|----------|
| ?? **Red** | Critical | Frozen, Molten Explosion, Arcane |
| ?? **Orange** | High | Desecrator, Thunderstorm |
| ?? **Yellow** | Medium | Frozen Pulse, Plagued |
| ? **Gray** | Low | Orbiter, Wormhole |

### Other Indicators
| Color | Meaning |
|-------|---------|
| ?? **Green dots** | Safe zones (size = safety score) |
| ?? **Cyan dots** | Escape route endpoints |
| ?? **Blue line** | Best escape route |
| ?? **Purple line** | Navigation path |
| ?? **Yellow dots** | Wall proximity markers |
| ? **White/Pink circles** | Monster threat zones |
| ?? **Purple circles** | Elite threat zones |

---

## ??? Architecture

```
Navigation/
??? NavGrid.cs                  - Spatial grid for fast lookups
??? AStarPathfinder.cs          - A* pathfinding implementation
??? SpatialAwarenessEngine.cs   - Environmental analysis system
??? NavigationPlugin.cs         - Main plugin with visualization
??? NavigationCustomizer.cs     - Configuration
??? README.md                   - This file
```

### Class Overview

#### NavGrid
```csharp
// Spatial grid for navigation
var grid = new NavGrid(cellSize: 2.5f);
grid.Initialize(minX, minY, maxX, maxY);

// Convert coordinates
var (gx, gy) = grid.WorldToGrid(worldX, worldY);
var (wx, wy) = grid.GridToWorld(gridX, gridY);

// Check walkability
bool walkable = grid.IsWalkable(x, y);
bool dangerous = grid.IsDangerous(x, y);

// Set cell flags
grid.SetCellFlags(x, y, NavCellFlags.Blocked | NavCellFlags.Dangerous);
```

#### AStarPathfinder
```csharp
// Find a path
var pathfinder = new AStarPathfinder { Grid = grid };
var result = pathfinder.FindPath(startX, startY, goalX, goalY);

// Find safe path (avoids all dangers)
var safeResult = pathfinder.FindSafePath(startX, startY, goalX, goalY);

// Find escape path (away from danger center)
var escapeResult = pathfinder.FindEscapePath(startX, startY, dangerX, dangerY, minDistance: 20f);

// Check result
if (result.IsValid) {
    foreach (var point in result.Path) {
        // point.WorldX, point.WorldY
    }
}
```

#### SpatialAwarenessEngine
```csharp
var awareness = new SpatialAwarenessEngine { Hud = hud };

// Update each frame
awareness.Update();

// Get analysis results
var dangers = awareness.DangerZones;      // All ground effects
var threats = awareness.Threats;           // All monster threats
var safeZones = awareness.SafeZones;       // Safe positions
var escapeRoutes = awareness.EscapeRoutes; // Escape directions

// Queries
var bestEscape = awareness.GetBestEscapeRoute();
var safest = awareness.GetSafestPosition();
bool safe = awareness.IsPositionSafe(x, y, minSafety: 50);
```

---

## ?? Configuration

Edit `NavigationCustomizer.cs`:

```csharp
// === DISPLAY OPTIONS ===
plugin.ShowOverlay = true;           // Show/hide overlay
plugin.ShowGrid = false;             // Show navigation grid
plugin.ShowDangerZones = true;       // Show ground effects
plugin.ShowSafeZones = true;         // Show safe positions
plugin.ShowEscapeRoutes = true;      // Show escape directions
plugin.ShowPath = true;              // Show navigation path
plugin.ShowThreatIndicators = true;  // Show monster threats
plugin.ShowWallMarkers = true;       // Show wall proximity

// === AWARENESS SETTINGS ===
awareness.AnalysisRadius = 80f;      // Analysis range (yards)
awareness.DangerMultiplier = 1.3f;   // Danger zone size multiplier
awareness.WallDetectionRange = 15f;  // Wall proximity range
awareness.EscapeDirections = 16;     // Number of escape directions
awareness.TrackMonsterMovement = true; // Enable movement prediction

// === PATHFINDING ===
pathfinder.DangerPenalty = 10f;      // Cost to path through danger
pathfinder.ElitePenalty = 20f;       // Cost near elites
pathfinder.MaxIterations = 5000;     // Max A* iterations
pathfinder.SmoothPath = true;        // Enable path smoothing
pathfinder.AvoidDanger = true;       // Enable danger avoidance
```

---

## ?? Integration with Other Plugins

### Using Navigation from SmartEvade

```csharp
public class SmartEvadePlugin : BasePlugin
{
    private NavigationPlugin _navigation;
    
    public override void Load(IController hud)
    {
        base.Load(hud);
        
        // Get navigation plugin
        _navigation = Hud.AllPlugins
            .OfType<NavigationPlugin>()
            .FirstOrDefault();
    }
    
    public void AfterCollect()
    {
        var awareness = _navigation?.GetAwareness();
        if (awareness == null) return;
        
        // Use awareness data
        var bestEscape = awareness.GetBestEscapeRoute();
        if (bestEscape != null && !bestEscape.IsBlocked)
        {
            // Move in escape direction
            MoveToward(bestEscape.TargetX, bestEscape.TargetY);
        }
        
        // Or use pathfinding
        var path = awareness.Pathfinder.FindEscapePath(
            myPos.X, myPos.Y,
            dangerCenter.X, dangerCenter.Y,
            minDistance: 20f
        );
        
        if (path.IsValid)
        {
            // Follow the path
        }
    }
}
```

### Auto-Navigate to Shrine

```csharp
public void NavigateToNearestShrine()
{
    var shrine = Hud.Game.Shrines
        .OrderBy(s => s.FloorCoordinate.XYDistanceTo(Hud.Game.Me.FloorCoordinate))
        .FirstOrDefault();
    
    if (shrine != null)
    {
        _navigation.NavigateTo(shrine.FloorCoordinate);
    }
}
```

### Navigate to Safety

```csharp
public void RunAway()
{
    _navigation.NavigateToSafety();
}
```

---

## ?? Performance

The system is optimized for real-time use:

| Component | Typical Time |
|-----------|--------------|
| Grid update | < 1ms |
| Danger analysis | < 2ms |
| Monster analysis | < 1ms |
| Safe zone detection | < 2ms |
| Escape route calculation | < 1ms |
| A* pathfinding (50 cells) | < 5ms |
| **Total per frame** | **< 10ms** |

### Optimization Tips

1. **Reduce AnalysisRadius** - Smaller radius = faster
2. **Disable ShowGrid** - Grid rendering is expensive
3. **Reduce EscapeDirections** - 8 instead of 16
4. **Increase cell size** - Fewer cells = faster pathfinding

---

## ??? Future Enhancements

- [ ] **NavMesh extraction** - Load actual game NavMesh data
- [ ] **Cross-scene navigation** - Navigate through portals
- [ ] **Objective-based navigation** - Auto-route to objectives
- [ ] **Group awareness** - Consider party member positions
- [ ] **Learning system** - Remember explored areas
- [ ] **Combat integration** - Navigate while fighting

---

## ?? Files

| File | Lines | Description |
|------|-------|-------------|
| NavGrid.cs | ~250 | Spatial grid system |
| AStarPathfinder.cs | ~300 | A* implementation |
| SpatialAwarenessEngine.cs | ~600 | Environmental analysis |
| NavigationPlugin.cs | ~450 | Main plugin |
| NavigationCustomizer.cs | ~60 | Configuration |
| **Total** | **~1660** | Complete system |

---

## ?? Usage Examples

### Example 1: Always Know Escape Routes
```csharp
// In any plugin's AfterCollect
var awareness = GetNavigation().GetAwareness();
var escape = awareness.GetBestEscapeRoute();

if (escape != null && escape.SafetyScore > 70)
{
    // Good escape route available
    DrawEscapeIndicator(escape);
}
else
{
    // Danger! No good escape
    ShowWarning("Trapped!");
}
```

### Example 2: Smart Positioning
```csharp
// Find position that's safe AND close to monsters (for AoE)
var awareness = GetNavigation().GetAwareness();

var optimalPos = awareness.SafeZones
    .Where(s => s.SafetyScore > 60)
    .OrderBy(s => awareness.MonsterDensity.GetDensity(s.X, s.Y))
    .LastOrDefault(); // Highest density among safe zones

if (optimalPos != null)
{
    NavigateTo(optimalPos.X, optimalPos.Y);
}
```

### Example 3: Danger Warning System
```csharp
// Check if current position is becoming dangerous
var awareness = GetNavigation().GetAwareness();
var myPos = Hud.Game.Me.FloorCoordinate;

if (!awareness.IsPositionSafe(myPos.X, myPos.Y, minSafety: 40))
{
    // We're in danger!
    PlayWarningSound();
    HighlightBestEscape();
}
```

---

*Press Ctrl+N to see the magic!* ?
