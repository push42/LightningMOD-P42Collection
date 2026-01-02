# LoD Death Nova (Blood Nova) Necromancer Macro

Ultimate auto-combat macro for the Legacy of Dreams Blood Nova build.

**Build Guide:** https://maxroll.gg/d3/guides/lod-death-nova-necromancer-guide

---

## ?? CRITICAL BUILD MECHANIC

**Death Nova is NOT cast manually!**

The build works like this:
1. You **channel Siphon Blood** (hold left click)
2. **Iron Rose** automatically casts **Blood Nova** while you channel
3. Your **Simulacrums** also proc Blood Nova when you channel
4. **Simulacrum Blood Novas are the main damage** because they proc Area Damage!
5. Your own Blood Novas do NOT proc Area Damage (they're just extra)

---

## Overview

This macro automates the LoD Blood Nova Necromancer rotation:
- **Continuous Siphon Blood channeling** - Triggers Iron Rose Blood Nova procs
- **Simulacrum management** - Keeps clones alive with Stone Gauntlet snapshot
- **Bone Armor timing** - Applies STUN for Krysbin's 300% bonus
- **Funerary Pick stacking** - Maintains 200% damage buff
- **CoE synchronization** - Nukes during Physical window (Push mode)

---

## Skill Setup

| Slot | Skill | Rune | Behavior |
|------|-------|------|----------|
| Left Click | **Siphon Blood** | Power Shift | Auto - MAIN DAMAGE (procs Iron Rose) |
| Right Click | Death Nova | Blood Nova | **DO NOT USE** - Only for rune |
| 1 | Bone Armor | Dislocation | Auto - STUN for Krysbin's + DR |
| 2 | Simulacrum | Blood and Bone | Auto - Permanent with Haunted Visions |
| 3 | Frailty | Aura of Frailty | Auto - Passive curse for Dayntee's |
| 4 | Blood Rush | Potency | Manual / Emergency |

**Note:** Death Nova is on the bar ONLY for the Blood Nova rune! Iron Rose uses whatever rune is selected.

---

## Hotkeys

| Key | Action |
|-----|--------|
| `F1` | Toggle macro ON/OFF |
| `F2` | Switch between SPEED and PUSH mode |
| `F3` | Force Nuke (bypass CoE timing) |

---

## Modes

### SPEED Mode (Default)
- **Continuous Siphon Blood channeling**
- **Auto Bone Armor** refresh for DR
- **No CoE waiting** - constant damage output
- **Purpose:** T16, low GRs, fast farming

### PUSH Mode
- **CoE Physical synchronized** - Nukes only during Physical window
- **Bone Armor STUN timing** - For Krysbin's 300% bonus
- **Funerary Pick management** - Stack before nuke
- **Bloodtide awareness** - Waits for enough enemies
- **Purpose:** High GR pushing, maximum burst damage

---

## Build Mechanics

### Damage Flow
```
You channel Siphon Blood
    ?
Iron Rose auto-casts Blood Nova
    ?
Simulacrums ALSO cast Blood Nova (with Area Damage!)
    ?
Massive AoE damage in large pulls
```

### Damage Multipliers (All Stack!)
| Source | Bonus | Notes |
|--------|-------|-------|
| **Bloodtide Blade** | +400% per enemy (max 4000%) | Within 25 yards |
| **Funerary Pick** | +200% | 10 Siphon Blood stacks |
| **Krysbin's Sentence** | +300% | Stunned enemies (Bone Armor) |
| **Convention of Elements** | +200% | Physical rotation (4 sec) |
| **Iron Rose** | Auto Blood Nova | Procs while channeling |
| **Simulacrum** | 2x Blood Nova | AND they proc Area Damage! |
| **Oculus Ring** | +85% | Standing in circle |

### Why Simulacrums Are Key
- Your Blood Novas from Iron Rose do **NOT** proc Area Damage
- Simulacrum Blood Novas **DO** proc Area Damage
- In large pulls, Simulacrum damage >> Your damage
- Position them well before nuking!

### Stone Gauntlet Snapshot
- Simulacrums inherit your Armor when summoned
- Macro waits for 5 Stone Gauntlet stacks before summoning
- This makes Simulacrums much tankier!

---

## Optimal Rotation (Push Mode)

```
[Cold CoE] 
- Channel Siphon Blood to stack Funerary Pick
- Position Simulacrums in the pull
    ?
[Physical CoE - NUKE!]
- Cast Bone Armor (STUN for Krysbin's 300%)
- Channel Siphon Blood (Iron Rose procs Blood Nova)
- Stay in position, let Simulacrums do work
    ?
[Poison CoE]
- Maintain Funerary stacks (tap Siphon every 2-3 sec)
- Kite enemies, build next pull
    ?
Repeat
```

### Necromancer CoE Rotation (12 seconds)
- **Cold** (4 sec) ? Preparation
- **Physical** (4 sec) ? **NUKE PHASE**
- **Poison** (4 sec) ? Recovery

---

## Status Panel Display

| Display | Meaning |
|---------|---------|
| **Mode** | SPEED or PUSH (CoE Sync) |
| **CoE Status** | Current phase (Push mode) |
| **Funerary** | 0-10 stacks (% damage bonus) |
| **Bloodtide** | 0-10 enemies (% damage bonus) |
| **? IN OCULUS** | Standing in +85% damage circle |

---

## Configuration

Edit `LoDDeathNovaMacroCustomizer.cs`:

```csharp
// === CoE Timing (Push Mode) ===
plugin.PhysicalCoEIconIndex = 6;       // Physical element
plugin.PrePhysicalPrepSeconds = 1.0f;  // Prep time before Physical
plugin.MinFuneraryPickStacks = 5;      // Min stacks to nuke
plugin.MinEnemiesForCoENuke = 3;       // Min Bloodtide stacks

// === Combat ===
plugin.EnemyDetectionRange = 60f;
plugin.EliteDetectionRange = 40f;
plugin.BloodtideRange = 25f;
plugin.EmergencyBloodRushHealthPct = 0.35f;

// === Buff ===
plugin.BoneArmorRefreshTime = 5.0f;

// === Features ===
plugin.EnableOculusDetection = true;
plugin.IsHideTip = false;
```

---

## Gameplay Tips

### Speed Mode
1. Run into packs
2. Macro channels Siphon Blood continuously
3. Iron Rose procs Blood Nova automatically
4. Use Blood Rush manually for mobility

### Push Mode
1. **Build large pulls** - Bloodtide Blade scales with density
2. **Position Simulacrums** - They're your main damage
3. **Watch CoE panel** - Nuke only during Physical
4. **Stand in Oculus circles** - +85% damage
5. **Skip small packs** - Not worth Physical window
6. **Save pylons for boss** - Use Nemesis Bracers

### Simulacrum Care
- Wait for Stone Gauntlet stacks (macro does this)
- Avoid Grotesque explosions (can one-shot clones)
- Avoid Molten explosions
- Long cooldown if they die!

### Boss Strategy
- Stack Bane of the Stricken during non-Physical
- Use saved pylons with Nemesis Bracers for adds
- More enemies = more Bloodtide damage
- Use F3 to force nuke anytime

---

## Files

| File | Description |
|------|-------------|
| `LoDDeathNovaMacroPlugin.cs` | Main plugin |
| `LoDDeathNovaMacroCustomizer.cs` | Configuration |
| `README.md` | This file |

---

## Troubleshooting

### No damage
- Are you channeling Siphon Blood? (not casting Death Nova!)
- Check Iron Rose phylactery is equipped
- Check Funerary Pick stacks on panel

### Simulacrums dying
- Wait for Stone Gauntlet stacks before summoning
- Avoid Grotesque/Molten explosions
- Check Haunted Visions amulet

### Low damage in pulls
- Check Bloodtide stacks (need 5+ enemies)
- Position in center of pull
- Use Push mode for CoE sync

### Dying too much
- Check Bone Armor is up (30% DR)
- Check Frailty curse for Dayntee's
- Lower GR level

---

## Changelog

### v2.0 - Complete Rewrite
- Fixed build mechanics (Siphon Blood, not Death Nova!)
- Iron Rose auto-cast Blood Nova
- Proper Simulacrum management
- Stone Gauntlet snapshot
- Correct curse (Frailty Aura)
- Better CoE timing
- Improved status panel

### v1.0
- Initial (incorrect) release

---

*Press F1 to start, F2 to switch modes, F3 to force nuke!*

**Build Guide:** https://maxroll.gg/d3/guides/lod-death-nova-necromancer-guide
