# AutoMaster Plugin for TurboHUD

A premium, always-active auto-pickup and auto-interact plugin that makes gameplay smoother and more efficient.

## Features

### Always Active
Unlike other plugins that require holding keys, AutoMaster runs continuously in the background. Just toggle it on and play normally!

### Auto-Pickup
Automatically picks up items as you move:
- **Primal Ancient** items
- **Ancient** items
- **Legendary** items
- **Set** items
- **Gems** (all types including Legendary Gems)
- **Crafting Materials** (Death's Breath, Forgotten Souls, etc.)
- **Rift Keys**
- **Ramaladni's Gift**
- **Potions**

### Auto-Interact
Automatically interacts with objects as you pass by:
- **Shrines** (all types)
- **Pylons** (with GR level limit option)
- **Chests** (Normal and Resplendent)
- **Doors**
- **Dead Bodies**
- **Weapon/Armor Racks**
- **Pool of Reflection** (single player only)
- **Healing Wells** (when health below 70%)

### Smart Features
- Respects Nemesis Bracers in group play (won't click pylon if teammate has Nemesis)
- Checks inventory space before pickup
- Handles stackable items properly
- Avoids dangerous objects (cursed chests, etc.)
- Doesn't interfere with force move

## Installation

1. Copy the `AutoMaster` folder to:
   ```
   TurboHUD\plugins\Custom\AutoMaster\
   ```

2. Plugin loads automatically on next TurboHUD start.

## Usage

### Toggle Key: **H**

Press **H** to toggle the plugin ON/OFF.

### Status Panel
A small panel on the left side of screen shows:
- Current status (ON/OFF)
- Toggle key reminder

## Configuration

Edit `AutoMasterCustomizer.cs` to customize:

```csharp
public void Customize()
{
    Hud.RunOnPlugin<AutoMasterPlugin>(plugin =>
    {
        // Change toggle key (example: use U instead of H)
        plugin.ToggleKey = Hud.Input.CreateKeyEvent(true, Key.U, false, false, false);
        
        // Disable rare item pickup for speed farming
        plugin.PickupRare = false;
        
        // Don't auto-click pylons in high GRs
        plugin.GRLevelForAutoPylon = 70;
        
        // Increase pickup range
        plugin.PickupRange = 20.0;
        
        // Move status panel
        plugin.PanelX = 0.01f;   // X position (% of screen)
        plugin.PanelY = 0.40f;   // Y position (% of screen)
    });
}
```

## Settings Reference

### Pickup Settings
| Setting | Default | Description |
|---------|---------|-------------|
| PickupPrimal | true | Primal ancient items |
| PickupAncient | true | Ancient items |
| PickupLegendary | true | Legendary items |
| PickupSet | true | Set items |
| PickupGems | true | All gems |
| PickupCraftingMaterials | true | All crafting materials |
| PickupDeathsBreath | true | Death's Breath |
| PickupRiftKeys | true | Greater Rift Keystones |
| PickupRamaladni | true | Ramaladni's Gift |
| PickupPotions | true | Legendary potions |
| PickupRare | false | Rare (yellow) items |
| PickupMagic | false | Magic (blue) items |
| PickupWhite | false | Normal (white) items |

### Interact Settings
| Setting | Default | Description |
|---------|---------|-------------|
| InteractShrines | true | All shrines |
| InteractPylons | true | All pylons |
| InteractPylonsInGR | true | Pylons in Greater Rifts |
| GRLevelForAutoPylon | 100 | Max GR level for auto-pylon |
| InteractChests | true | All chests |
| InteractDoors | true | Doors |
| InteractPoolOfReflection | true | Experience pools |
| InteractHealingWells | true | Healing wells |
| InteractDeadBodies | true | Dead bodies |
| InteractWeaponRacks | true | Weapon racks |
| InteractArmorRacks | true | Armor racks |

### Range Settings
| Setting | Default | Description |
|---------|---------|-------------|
| PickupRange | 15.0 | Item pickup range (yards) |
| InteractRange | 12.0 | Object interaction range (yards) |

## Files

| File | Description |
|------|-------------|
| `AutoMasterPlugin.cs` | Main plugin code |
| `AutoMasterCustomizer.cs` | User configuration |
| `README.md` | This documentation |

## Compatibility

- Works alongside other TurboHUD plugins
- Does not conflict with in-game auto-pickup
- Designed for TurboHUD Lightning MOD

## Tips

1. **Speed Farming**: Disable rare/magic/white pickup for maximum speed
2. **High GRs**: Set `GRLevelForAutoPylon` lower to avoid premature pylon clicks
3. **Solo Play**: Enable Pool of Reflection auto-click
4. **Group Play**: The plugin respects Nemesis Bracers - it won't click pylons if teammates have it equipped

## Changelog

### v1.0.0
- Initial release
- Auto-pickup for all valuable items
- Auto-interact for all common objects
- Smart pylon handling for group play
- Beautiful status panel UI
