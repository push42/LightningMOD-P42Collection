# Natalya Spike Trap Demon Hunter Macro

Auto-combat macro for the Natalya's Vengeance Spike Trap build.

**Build Guide:** https://maxroll.gg/d3/guides/natalya-spike-trap-demon-hunter-guide

---

## Skill Setup

| Slot | Skill | Rune | Behavior |
|------|-------|------|----------|
| Left Click | Evasive Fire | Hardened | Auto - detonates traps |
| Right Click | Spike Trap | Custom Trigger | Auto - main damage |
| 1 | Caltrops | Bait the Trap | Auto - pulls enemies |
| 2 | Vengeance | Dark Heart | Auto - buff |
| 3 | Smoke Screen | Healing Vapors | Auto - defense |
| 4 | Shadow Power | Gloom | Auto - defense |

---

## Hotkeys

| Key | Action |
|-----|--------|
| `F1` | Toggle macro ON/OFF |
| `F2` | Switch between PULL and DAMAGE mode |

---

## Modes

### PULL Mode (F2 to switch)
- Places **2 Spike Traps**
- Then places **Caltrops**
- Then fires **Evasive Fire** to detonate
- **Purpose:** Pull enemies together into a pixelstack

### DAMAGE Mode (default)
- Places **5 Spike Traps** (optimal chain reaction)
- Then fires **Evasive Fire** to detonate
- **Purpose:** Maximum damage output

---

## How It Works

### Rotation Priority:
1. **Vengeance** - Keep buff up at all times
2. **Shadow Power** - Keep buff up when fighting
3. **Smoke Screen** - Use when health drops below 70%
4. **Combat Rotation** - Based on mode:

### Pull Mode Rotation:
```
2x Spike Trap ? Caltrops ? Evasive Fire
```
This pulls enemies into the Caltrops for stacking.

### Damage Mode Rotation:
```
5x Spike Trap ? Evasive Fire
```
This creates optimal chain reactions for maximum damage.

---

## Optimal Gameplay

1. **Start in PULL mode** (press F2 if needed)
2. Run into a pack of enemies
3. Let the macro pull them together
4. **Switch to DAMAGE mode** (press F2)
5. Let the macro nuke them with 5-trap chain reactions
6. Repeat

---

## Build Mechanics

From the Maxroll guide:

- **Natalya (2) Bonus:** No cost/cooldown for Spike Trap, +100% damage with Caltrops
- **Natalya (4) Bonus:** Caltrops pull enemies when hit by Spike Trap
- **Natalya (6) Bonus:** +10,000% damage, +25% per consecutive explosion
- **Custom Engineering:** Allows 10 traps (5 placements with Trag'Oul Scatter)
- **Trag'Oul Coils:** Adds Scatter rune (2 traps per placement)

### Chain Reaction Math:
- 5 trap placements × 2 (Scatter) = 10 traps
- Each consecutive explosion = +25% damage
- Max bonus: 9 × 25% = +225% on final tick
- Average damage increase: +112.5%

---

## Configuration

Edit `NatalyaSpikeTrapMacroCustomizer.cs`:

```csharp
// Trap counts
plugin.PullModeTraps = 2;      // Traps for pulling
plugin.DamageModeTraps = 5;    // Traps for damage (optimal)

// Timing (ms)
plugin.TrapPlacementDelay = 30;  // Between traps
plugin.DetonationDelay = 50;     // Before detonating

// Buff refresh (seconds remaining)
plugin.VengeanceRefreshTime = 3.0f;
plugin.ShadowPowerRefreshTime = 2.0f;

// Enemy detection range (yards)
plugin.EnemyDetectionRange = 50f;
```

---

## Tips

1. **Use PULL mode first** to gather enemies
2. **Switch to DAMAGE mode** once enemies are stacked
3. **Stand in Oculus Ring** circles for +85% damage
4. **Smoke Screen** is automatic when health is low
5. The macro keeps **Vengeance** and **Shadow Power** up automatically

---

## Files

| File | Description |
|------|-------------|
| `NatalyaSpikeTrapMacroPlugin.cs` | Main plugin |
| `NatalyaSpikeTrapMacroCustomizer.cs` | Configuration |
| `README.md` | This file |

---

## Panel Position

The status panel appears at Y=0.70 (70% from top) on the left side.

---

*Press F1 to start, F2 to switch modes!*
