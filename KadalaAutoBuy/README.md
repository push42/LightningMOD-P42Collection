# Kadala Auto-Buy Plugin v4.0

**FULLY AUTOMATIC** Kadala gambling - just select item type and go!

## What's New in v4.0

- ? **No more manual calibration** - Reads actual UI slots automatically
- ? **Auto tab switching** - Switches to correct tab (Weapons/Armor/Jewelry/Other)
- ? **Works at any resolution** - No position percentages to configure
- ? **Simple item selection** - Just pick what you want to buy

## How It Works

1. **Select item type** - Use F10 ? Kadala Auto-Buy ? Click an item button
2. **Open Kadala** - Plugin switches tabs and starts buying automatically!
3. **Done** - Closes when inventory is full or you run out of shards

## Features

- ? **Auto tab switching** - No need to be on correct tab when opening Kadala
- ? **All item types supported** - Weapons, armor, jewelry, off-hands
- ? **Shard thresholds** - Start at X shards, stop at Y shards
- ? **Statistics tracking** - Session and total items bought
- ? **Inventory protection** - Stops when inventory is full

## Item Types & Costs

### Weapons (75 shards)
| Item | Tab |
|------|-----|
| 1-H Weapon | Weapons |
| 2-H Weapon | Weapons |

### Off-Hand (25 shards)
| Item | Tab |
|------|-----|
| Quiver | Weapons |
| Orb | Weapons |
| Mojo | Weapons |
| Phylactery | Weapons |

### Armor (25 shards)
| Item | Tab |
|------|-----|
| Helm | Armor |
| Gloves | Armor |
| Boots | Armor |
| Chest Armor | Armor |
| Belt | Armor |
| Shoulders | Armor |
| Pants | Armor |
| Bracers | Armor |

### Jewelry
| Item | Cost | Tab |
|------|------|-----|
| Ring | 50 | Jewelry |
| Amulet | 100 | Jewelry |

### Other (25 shards)
| Item | Tab |
|------|-----|
| Shield | Other |

## Hotkeys

| Key | Action |
|-----|--------|
| `Shift+K` | Toggle auto-buy ON/OFF |
| `Ctrl+K` | Cycle through item types |

## Settings

### Item Selection
Click item buttons in the settings panel (F10 ? Kadala Auto-Buy)

### Thresholds
- **Start at Shards** - Only auto-buy if you have at least this many
- **Stop at Shards** - Stop buying at this amount (0 = spend all)
- **Buy Speed** - Milliseconds between purchases

### Debug Mode
Enable "Debug Overlay" to see slot rectangles and tab states (for troubleshooting)

## Customizer Configuration

Edit `KadalaAutoBuyCustomizer.cs`:

```csharp
// Select item to buy
plugin.SelectedItem = KadalaAutoBuyPlugin.KadalaItemType.Ring;

// Other options:
// KadalaItemType.Amulet
// KadalaItemType.Gloves
// KadalaItemType.OneHandWeapon
// etc.

// Thresholds
plugin.MinBloodShardsToStart = 100;
plugin.StopAtBloodShards = 0;
plugin.BuyIntervalMs = 50;
```

## Tips

1. **Ring/Amulet gambling** - Higher cost but best for build-defining items
2. **Armor gambling** - 25 shards each, good for set pieces
3. **Use Ctrl+K** - Quick way to cycle items without opening settings
4. **Debug overlay** - Enable if plugin isn't clicking the right spot

## Troubleshooting

**Plugin not clicking correct slot?**
- Enable Debug Overlay to see where it's trying to click
- Make sure Kadala's shop is fully loaded before it starts

**Not switching tabs?**
- Wait a moment after opening Kadala
- The plugin needs to detect the current tab first

**Buying wrong item?**
- Check selected item in settings (F10)
- Use Ctrl+K to verify current selection

## Changelog

### v4.0.0 - Complete Rewrite
- Proper UI element detection (no more manual calibration!)
- Automatic tab switching
- New item type selection system
- Debug overlay for troubleshooting
- Cleaner settings panel with organized item categories

### v3.0.0
- Core plugin system integration
- Calibration mode

### v2.0.0
- Initial position-based system
