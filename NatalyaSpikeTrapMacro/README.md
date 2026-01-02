# Natalya Spike Trap Macro for TurboHUD

A powerful automation macro for the **Natalya's Vengeance (N6) Spike Trap Demon Hunter** build.

**Build Guide:** https://maxroll.gg/d3/guides/natalya-spike-trap-demon-hunter-guide

---

## ?? Features

- **Smart Combat Detection** - Only attacks when enemies are nearby
- **Auto Movement** - Uses Force Move to travel when no enemies present
- **Two Combat Modes** - PULL mode for grouping, DAMAGE mode for nuking
- **Automatic Buff Management** - Keeps Vengeance, Shadow Power, and Smoke Screen active
- **Build-Specific Activation** - Only shows when Spike Trap is equipped (won't interfere with GoD builds)
- **Safety Pauses** - Automatically pauses during loading, menus, death, etc.

---

## ?? Installation

1. Copy the `NatalyaSpikeTrapMacro` folder to:
   ```
   TurboHUD\plugins\Custom\
   ```

2. Your folder structure should look like:
   ```
   TurboHUD\
   ??? plugins\
       ??? Custom\
           ??? NatalyaSpikeTrapMacro\
               ??? NatalyaSpikeTrapMacroPlugin.cs
               ??? NatalyaSpikeTrapMacroCustomizer.cs
               ??? README.md
   ```

3. Restart TurboHUD

---

## ?? Controls

| Key | Action |
|-----|--------|
| **F1** | Toggle macro ON/OFF |
| **F2** | Switch between PULL and DAMAGE mode |

---

## ?? Required Skill Setup

The macro expects this exact skill configuration:

| Slot | Skill | Rune | Purpose |
|------|-------|------|---------|
| **Left Click** | Evasive Fire | Hardened | Detonates traps |
| **Right Click** | Spike Trap | Custom Trigger | Main damage |
| **Skill 1** | Caltrops | Bait the Trap | Pull enemies (optional) |
| **Skill 2** | Vengeance | Dark Heart | Damage buff |
| **Skill 3** | Smoke Screen | Healing Vapors | Emergency defense |
| **Skill 4** | Shadow Power | Gloom | Sustain |

> **Note:** Skills 3-4 can be swapped. The macro detects skills by type, not by slot.

---

## ?? Combat Modes

### DAMAGE Mode (Default)
- Places **5 Spike Traps** rapidly
- Detonates with Evasive Fire
- Best for: Boss fights, Rift Guardians, dense packs

### PULL Mode
- Places **2 Spike Traps**
- Casts Caltrops to pull enemies
- Detonates with Evasive Fire
- Best for: Grouping scattered enemies

---

## ?? On-Screen Display

The macro shows a status panel below your character:

**When Active (Combat):**
```
N6 Spike Trap
? COMBAT (5)
DAMAGE (5 traps)
```

**When Active (Moving):**
```
N6 Spike Trap
? MOVING
[F2] DAMAGE
```

**When Inactive:**
```
N6 Spike Trap
OFF [F1]
```

---

## ?? Configuration

Edit `NatalyaSpikeTrapMacroCustomizer.cs` to customize:

### Key Bindings
```csharp
// Change toggle key (default: F1)
plugin.ToggleKeyEvent = Hud.Input.CreateKeyEvent(true, Key.F1, false, false, false);

// Change mode switch key (default: F2)
plugin.ModeKeyEvent = Hud.Input.CreateKeyEvent(true, Key.F2, false, false, false);
```

### Trap Settings
```csharp
// Traps in PULL mode (1-2 recommended)
plugin.PullModeTraps = 2;

// Traps in DAMAGE mode (5 for max chain reaction)
plugin.DamageModeTraps = 5;
```

### Timing Settings
```csharp
// Delay between trap placements (ms)
plugin.TrapPlacementDelay = 30;

// Delay before detonating (ms)
plugin.DetonationDelay = 50;
```

### Combat Settings
```csharp
// Enemy detection range (yards)
plugin.EnemyDetectionRange = 50f;

// Minimum enemies to start combat
plugin.MinEnemiesForCombat = 1;

// Auto-move when no enemies
plugin.EnableAutoMovement = true;
```

---

## ??? Safety Features

The macro automatically pauses when:
- Game is loading or paused
- Player is in town
- Game window is not focused
- Inventory/map/menus are open
- Chat is open
- Player is dead
- Casting town portal
- Cursor is outside game window

---

## ? FAQ

**Q: The macro doesn't show up?**
A: Make sure Spike Trap is equipped. The macro only activates for Spike Trap builds.

**Q: Can I use this with GoD Strafe?**
A: No need! The macro automatically disables when Spike Trap isn't equipped.

**Q: The timing feels off?**
A: Adjust `TrapPlacementDelay` and `DetonationDelay` in the customizer.

**Q: I want to change the keys?**
A: Edit the `Key.F1` and `Key.F2` values in the customizer file.

---

## ?? Changelog

### v1.0.0
- Initial release
- Smart combat detection
- Auto movement when no enemies
- PULL and DAMAGE modes
- Automatic buff management
- Build-specific activation

---

## ?? License

Free to use and modify. Credit appreciated but not required.

Enjoy pushing those Greater Rifts! ??
