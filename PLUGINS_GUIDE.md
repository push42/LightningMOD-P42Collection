# TurboHUD Custom Plugins Collection

A collection of custom TurboHUD plugins for Diablo 3, optimized for speed farming and automation.

---

## ?? Plugin Overview

| Plugin | Folder | Purpose | Hotkey |
|--------|--------|---------|--------|
| **Auto Pickup Silent** | `AutoPickupSilent/` | Aggressive item pickup without mouse interruption | `H` |
| **Wizard Star Pact Macro** | `WizardStarPactMacro/` | Auto-combat for Star Pact Wizard (Challenge Rift) | `F1` |
| **Smart Evade** | `SmartEvade/` | Full auto-dodge with enemy kiting (Private) | `Shift+J` |
| **Smart Evade Lite** | `SmartEvadeLite/` | Human-like delayed dodge (Community) | `J` |
| **Inventory Sorter** | `InventorySorter/` | Sort inventory/stash by category | `K` |
| **Smart Salvage** | `SmartSalvage/` | Auto-salvage with build blacklists | `U` |
| **Auto Master** | `AutoMaster/` | Original auto-pickup (disabled, replaced by Silent) | `H` |

---

## ?? Plugin Details

### 1. Auto Pickup Silent (RECOMMENDED)
**Location:** `plugins/Custom/AutoPickupSilent/`

Ultra-fast, non-intrusive auto-pickup optimized for speed builds like GoD Demon Hunter.

#### Files
| File | Description |
|------|-------------|
| `AutoPickupSilentPlugin.cs` | Main plugin logic |
| `AutoPickupSilentCustomizer.cs` | Configuration settings |
| `README.md` | Documentation |

#### Features
- ? Picks up **5 items per frame** (aggressive mode)
- ? **18 yard pickup range** for speed farming
- ? **No mouse interruption** - instant cursor restore
- ? Smart retry system for missed items
- ? Auto-interacts with shrines, pylons, chests, doors

#### Configuration
```csharp
plugin.PickupRange = 18.0;        // Extended range for speed
plugin.PickupsPerCycle = 5;       // Items per frame
plugin.RetryPickups = true;       // Retry failed pickups
```

---

### 2. Wizard Star Pact Macro
**Location:** `plugins/Custom/WizardStarPactMacro/`

Auto-combat macro for the Weekly Challenge Rift Wizard build (Meteor Star Pact with Arcane Dynamo).

#### Files
| File | Description |
|------|-------------|
| `WizardStarPactMacroPlugin.cs` | Main plugin logic |
| `WizardStarPactMacroCustomizer.cs` | Configuration settings |
| `README.md` | Documentation |

#### Build Skills
| Slot | Skill | Rune | Behavior |
|------|-------|------|----------|
| Left Click | Spectral Blade | Barrier Blades | Auto - builds Arcane Dynamo |
| Right Click | Hydra | Blazing Hydra | Auto - placed with 5 stacks |
| 1 | Meteor | Star Pact | Auto - cast with 5 stacks |
| 2 | Teleport | Wormhole | **MANUAL** |
| 3 | Magic Weapon | Deflection | Auto - buff |
| 4 | Storm Armor | Reactive Armor | Auto - buff |

#### Features
- ? Builds Arcane Dynamo stacks with Spectral Blade
- ? Casts Meteor at 5 stacks for maximum damage
- ? Places Hydra with 5 stacks for damage boost
- ? Keeps buffs (Magic Weapon, Storm Armor) active
- ? Auto-moves towards cursor
- ? Teleport remains **manual** for player control

---

### 3. Smart Evade (Private)
**Location:** `plugins/Custom/SmartEvade/`

?? **PRIVATE** - Full auto-dodge with instant reaction and enemy kiting.

#### Files
| File | Description |
|------|-------------|
| `SmartEvadePlugin.cs` | Main plugin logic |
| `SmartEvadeCustomizer.cs` | Configuration settings |
| `README.md` | Documentation |

#### Features
- Instant reaction to ground effects
- Continuous kiting from enemies
- Preemptive avoidance

---

### 4. Smart Evade Lite (Community)
**Location:** `plugins/Custom/SmartEvadeLite/`

? **COMMUNITY** - Human-like auto-dodge with delayed reactions.

#### Files
| File | Description |
|------|-------------|
| `SmartEvadeLitePlugin.cs` | Main plugin logic |
| `SmartEvadeLiteCustomizer.cs` | Configuration settings |
| `README.md` | Documentation |

#### Features
- 1.25-2 second random delay before evading
- Only triggers when **inside** danger
- 3 second cooldown between dodges
- Feels like a helpful assistant, not a bot

---

### 5. Inventory Sorter
**Location:** `plugins/Custom/InventorySorter/`

Sorts inventory and stash by various criteria.

#### Files
| File | Description |
|------|-------------|
| `InventorySorterPlugin.cs` | Main plugin logic |
| `InventorySorterCustomizer.cs` | Configuration settings |
| `SorterConfiguration.cs` | Settings class |
| `SortEnums.cs` | Sort mode definitions |
| `ItemCategory.cs` | Category definitions |
| `README.md` | Documentation |

#### Sort Modes
- Category (gems, armor, weapons)
- Quality (Primal > Ancient > Legendary)
- Type (equipment slot)
- Size (largest first)
- Alphabetical

---

### 6. Smart Salvage
**Location:** `plugins/Custom/SmartSalvage/`

Auto-salvage with build-specific item protection.

#### Files
| File | Description |
|------|-------------|
| `SmartSalvagePlugin.cs` | Main plugin with UI |
| `SmartSalvageCustomizer.cs` | Configuration settings |
| `BlacklistManager.cs` | Profile management |
| `BlacklistProfile.cs` | Profile data class |
| `MaxrollCrawler.cs` | Maxroll.gg build parser |
| `README.md` | Documentation |

#### Features
- Pre-configured blacklists for popular builds
- Import builds from Maxroll.gg URLs
- Protects ancients, primals, socketed items

---

### 7. Auto Master (Deprecated)
**Location:** `plugins/Custom/AutoMaster/`

?? **DISABLED** - Replaced by Auto Pickup Silent.

#### Files
| File | Description |
|------|-------------|
| `AutoMasterPlugin.cs` | Main plugin logic |
| `AutoMasterCustomizer.cs` | Configuration settings |
| `README.md` | Documentation |

---

## ?? Hotkey Reference

| Key | Action | Plugin |
|-----|--------|--------|
| `H` | Toggle Auto Pickup | AutoPickupSilent |
| `F1` | Toggle Star Pact Macro | WizardStarPactMacro |
| `J` | Toggle Evade Lite | SmartEvadeLite |
| `Shift+J` | Toggle Full Evade | SmartEvade |
| `K` | Sort Inventory | InventorySorter |
| `Shift+K` | Change Sort Mode | InventorySorter |
| `U` | Start Auto-Salvage | SmartSalvage |

---

## ?? UI Panel Positions (Left Side)

| Y Position | Plugin |
|------------|--------|
| 0.28 | Smart Evade |
| 0.35 | (reserved) |
| 0.42 | Auto Master (disabled) |
| 0.49 | Evade Lite |
| 0.56 | Inventory Sort |
| 0.63 | **Auto Pickup Silent** |

---

## ?? Installation

1. Copy the `plugins/Custom/` folder to your TurboHUD installation
2. Restart TurboHUD
3. Plugins auto-load and appear in-game

### File Structure
```
TurboHUD/
??? plugins/
    ??? Custom/
        ??? AutoPickupSilent/
        ?   ??? AutoPickupSilentPlugin.cs
        ?   ??? AutoPickupSilentCustomizer.cs
        ?   ??? README.md
        ??? WizardStarPactMacro/
        ?   ??? WizardStarPactMacroPlugin.cs
        ?   ??? WizardStarPactMacroCustomizer.cs
        ?   ??? README.md
        ??? SmartEvade/
        ?   ??? SmartEvadePlugin.cs
        ?   ??? SmartEvadeCustomizer.cs
        ?   ??? README.md
        ??? SmartEvadeLite/
        ?   ??? SmartEvadeLitePlugin.cs
        ?   ??? SmartEvadeLiteCustomizer.cs
        ?   ??? README.md
        ??? InventorySorter/
        ?   ??? InventorySorterPlugin.cs
        ?   ??? InventorySorterCustomizer.cs
        ?   ??? SorterConfiguration.cs
        ?   ??? SortEnums.cs
        ?   ??? ItemCategory.cs
        ?   ??? README.md
        ??? SmartSalvage/
        ?   ??? SmartSalvagePlugin.cs
        ?   ??? SmartSalvageCustomizer.cs
        ?   ??? BlacklistManager.cs
        ?   ??? BlacklistProfile.cs
        ?   ??? MaxrollCrawler.cs
        ?   ??? README.md
        ??? AutoMaster/
            ??? AutoMasterPlugin.cs
            ??? AutoMasterCustomizer.cs
            ??? README.md
```

---

## ?? Sharing Guide

### For Speed Farming (GoD DH, WW Barb, etc.)
**Required plugins:**
- `AutoPickupSilent/` - Fast item pickup
- `SmartEvadeLite/` - Safe dodging

### For Challenge Rifts (Wizard)
**Required plugins:**
- `WizardStarPactMacro/` - Auto-combat

### For General Use
**Recommended plugins:**
- `AutoPickupSilent/` - Item pickup
- `InventorySorter/` - Inventory management
- `SmartSalvage/` - Auto-salvage
- `SmartEvadeLite/` - Dodging

---

## ?? Notes

- **Auto Master is DISABLED** - AutoPickupSilent replaces it
- **SmartEvade is PRIVATE** - Don't share (too powerful)
- **SmartEvadeLite is COMMUNITY** - Safe to share
- All plugins have individual README.md files for detailed documentation

---

## ?? Changelog

### 2024-12-31
- **AutoPickupSilent**: Made aggressive (5 items/frame, 18yd range)
- **WizardStarPactMacro**: Created for Challenge Rift

### 2024-12-30
- Initial release of all plugins

---

*Made with ?? for Diablo 3 speed farming*
