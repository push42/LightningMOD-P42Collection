# Smart Evade v2.0 - God-Tier Auto-Evade

An intelligent evasion and kiting system with **wall awareness** and **anti-corner-lock** technology.

**?? PRIVATE VERSION - Not for community sharing**

---

## ?? What's New in v2.0

### Wall Awareness System
- **Scene boundary detection** - Knows where walls are using game scene data
- **Multi-directional escape analysis** - Evaluates 16 possible escape routes
- **Wall proximity scoring** - Penalizes routes that lead toward walls
- **Predictive pathing** - Looks ahead to avoid dead ends

### Anti-Corner-Lock Technology
- **Corner detection** - Identifies when trapped in corners
- **Stuck detection** - Monitors position history to detect getting stuck
- **Auto-recovery maneuver** - Automatically escapes when stuck
- **Emergency escape routing** - Finds the most open direction when cornered

### Buttery Smooth Movement
- **Direction smoothing** - Interpolates between escape directions
- **Configurable responsiveness** - Balance between smooth and reactive
- **Reduced jitter** - No more erratic back-and-forth movement

---

## Features

### ?? Intelligent Kiting
- Maintains configurable safe distance from enemies
- Prioritizes elite/boss threats
- **Never backs into walls**
- Smooth, precise movements

### ? Ground Effect Avoidance
- Detects ALL elite affixes with proper priorities:
  - **Critical**: Molten Explosion, Arcane Sentry, Frozen
  - **High**: Desecrator, Thunderstorm
  - **Medium**: Frozen Pulse, Plagued, Ghom Gas
  - **Low**: Orbiter, Wormhole, Poison Enchanted

### ?? Wall Awareness
- Uses scene boundaries to detect walls
- Calculates distance to all nearby walls
- Penalizes escape routes leading to walls
- **Never gets cornered anymore!**

### ?? Escape Route Analysis
- Evaluates 16 directions simultaneously
- Scores each route based on:
  - Distance from dangers
  - Distance from enemies
  - Wall proximity
  - Path openness (predictive)
- Selects optimal escape path
- Shows all routes visually (debug mode)

---

## Hotkey

| Key | Action |
|-----|--------|
| **Shift+J** | Toggle evade system ON/OFF |

*(J without Shift is for the Lite community version)*

---

## Visual Indicators

When debug mode is enabled:

| Color | Meaning |
|-------|---------|
| ?? **Green circle** | Your safe zone radius |
| ?? **Red circles** | Dangerous ground effects |
| ?? **Yellow line/dot** | Current escape direction |
| ?? **Orange circles** | Elite threat zones |
| ?? **Yellow dots** | Wall proximity markers |
| ?? **Cyan dots** | Evaluated escape routes |
| ?? **Red dots** | Blocked escape routes |

---

## Configuration

Edit `SmartEvadeCustomizer.cs`:

```csharp
// === DISTANCE SETTINGS ===
plugin.SafeDistance = 15f;           // Desired distance from enemies
plugin.MinKiteDistance = 8f;         // Start kiting when closer
plugin.MaxEnemyRange = 50f;          // Ignore enemies beyond this
plugin.DangerRadiusMultiplier = 1.3f; // Make danger zones 30% larger

// === WALL AWARENESS (NEW!) ===
plugin.WallDetectionRange = 12f;     // How far to detect walls
plugin.WallAvoidanceWeight = 3.0f;   // How strongly to avoid walls
plugin.MinWallClearance = 5f;        // Minimum distance from walls

// === MOVEMENT SETTINGS ===
plugin.EscapeDirections = 16;        // Directions to evaluate
plugin.EscapeDistance = 10f;         // Distance per escape move
plugin.MovementSmoothing = 0.7f;     // 0-1, higher = smoother
plugin.ActionCooldownMs = 35;        // MS between moves
plugin.EnablePredictivePathing = true; // Look ahead for dead ends

// === COMBAT SETTINGS ===
plugin.PrioritizeGroundEffects = true;
plugin.EvadeHealthThreshold = 100f;  // 100 = always evade

// === VISUAL DEBUG ===
plugin.ShowDebugCircles = true;      // Safe zone, dangers
plugin.ShowWallMarkers = true;       // Wall proximity indicators
plugin.ShowEscapeRoutes = true;      // All evaluated routes
plugin.ShowMovementIndicator = true; // Current direction
```

---

## Recommended Presets

### Echoing Nightmare (Maximum Safety)
```csharp
plugin.SafeDistance = 20f;
plugin.DangerRadiusMultiplier = 1.5f;
plugin.WallDetectionRange = 15f;
plugin.WallAvoidanceWeight = 4.0f;
plugin.ActionCooldownMs = 25;        // Faster reactions
plugin.MovementSmoothing = 0.5f;     // More responsive
```

### High GRs (Ranged Builds)
```csharp
plugin.SafeDistance = 25f;
plugin.EvadeHealthThreshold = 80f;   // Only when damaged
plugin.WallDetectionRange = 12f;
plugin.EscapeDistance = 12f;
plugin.MovementSmoothing = 0.7f;
```

### Melee Builds (Stay Close)
```csharp
plugin.SafeDistance = 8f;
plugin.EvadeHealthThreshold = 60f;   // Only when low HP
plugin.WallDetectionRange = 8f;
plugin.EscapeDistance = 6f;
plugin.MovementSmoothing = 0.8f;     // Very smooth
```

### Dense Maps (Tight Corridors)
```csharp
plugin.WallDetectionRange = 15f;     // Detect walls early
plugin.WallAvoidanceWeight = 5.0f;   // Strong wall avoidance
plugin.MinWallClearance = 6f;        // Stay further from walls
plugin.EnablePredictivePathing = true;
plugin.EscapeDirections = 24;        // More direction options
```

---

## Status Panel

```
???????????????????
? Smart Evade v2  ?
? ? Evade: Arcane ?  <- Current action
? [Shift+J] Toggle?
? D:3 M:12 Walls:2?  <- Dangers, Mobs, Nearby walls
???????????????????
```

### Status Messages
| Message | Meaning |
|---------|---------|
| **Ready** | Active, no threats detected |
| **Evade: [type]** | Dodging specific ground effect |
| **Kiting** | Moving away from enemies |
| **Safe** | No threats in range |
| **UNSTUCK!** | Executing stuck recovery |
| **? CORNER!** | Detected corner trap situation |

---

## How It Works

### 1. Threat Collection
Every frame, the plugin collects:
- All ground effects with their danger radii
- All enemies with their threat levels
- Current wall/boundary positions

### 2. Escape Route Analysis
Evaluates 16 directions (configurable) and scores each based on:
```
Score = 100 (base)
      - WallPenalty (distance to walls)
      + DangerBonus (moving away from effects)
      + KiteBonus (moving away from enemies)
      - DeadEndPenalty (predictive pathing)
      + CornerEscapeBonus (when cornered)
```

### 3. Route Selection
- Filters out blocked routes (would hit wall)
- Sorts by score
- Selects highest scoring valid route

### 4. Movement Execution
- Applies direction smoothing
- Checks for stuck condition
- Executes move or unstuck maneuver

---

## Anti-Corner-Lock System

The v2.0 anti-corner system uses multiple strategies:

### Corner Detection
- Monitors distance to scene boundaries
- Detects when 2+ walls are within close range
- Triggers special escape routing

### Stuck Detection
- Tracks last 10 positions
- If total movement < 2 yards while evading = stuck
- After 5 consecutive stuck frames, triggers recovery

### Recovery Maneuver
- Ignores dangers temporarily
- Finds most open direction
- Executes longer escape distance
- Resets stuck counter

---

## Supported Ground Effects

| Effect | Radius | Priority |
|--------|--------|----------|
| Molten Explosion | 14y | 100 (Critical) |
| Arcane Sentry | 42y | 95 (Critical) |
| Frozen (Ice Ball) | 16y | 100 (Critical) |
| Molten Trail | 14y | 85-90 |
| Arcane Spawn | 8y | 80 |
| Desecrator | 9y | 75 |
| Thunderstorm | 18y | 70 |
| Frozen Pulse | 16y | 65 |
| Plagued | 14y | 60 |
| Ghom Gas | 22y | 55 |
| Wormhole | 5y | 40 |
| Poison Enchanted | 6y | 35 |
| Orbiter | 4y | 30 |

---

## Files

| File | Description |
|------|-------------|
| `SmartEvadePlugin.cs` | Main plugin (~650 lines) |
| `SmartEvadeCustomizer.cs` | Configuration |
| `README.md` | This documentation |

---

## Changelog

### v2.0 (2024-12-31)
- **NEW**: Wall awareness using scene boundaries
- **NEW**: Anti-corner-lock technology
- **NEW**: Multi-directional escape analysis (16 directions)
- **NEW**: Predictive pathing (dead-end avoidance)
- **NEW**: Movement smoothing/interpolation
- **NEW**: Stuck detection and auto-recovery
- **NEW**: Visual escape route indicators
- **NEW**: Wall proximity markers
- **IMPROVED**: Danger zone priorities
- **IMPROVED**: Status panel with wall info

### v1.0 (2024-12-30)
- Initial release
- Basic ground effect detection
- Simple escape vector calculation
- Enemy kiting

---

## Tips

1. **Enable debug visuals** first to understand how it works
2. **Adjust WallAvoidanceWeight** if you're still hitting walls
3. **Lower MovementSmoothing** for faster reactions in dangerous content
4. **Increase EscapeDirections** to 24 for tight corridors
5. **Use presets** as starting points for your build type

---

*Press Shift+J to toggle, watch the magic happen!*

**?? PRIVATE VERSION - Not for community sharing**
