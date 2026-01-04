# Silent Auto Pickup

Enhanced auto-pickup plugin that does **NOT interrupt mouse movement**. 

## What's Different?

| Feature | Old AutoMaster | Silent Version |
|---------|----------------|----------------|
| Mouse interruption | Yes, visible cursor movement | **No**, instant restore |
| Click duration | 3-5ms with wait loops | **1ms** ultra-fast |
| Pickup method | Loop until picked | **Queue-based**, one attempt |
| Movement blocking | Blocks if moving | **Never blocks** |

## How It Works

1. **Scans** for items in range (every 100ms)
2. **Queues** items for pickup
3. **Instant click** - moves mouse, clicks, restores position in ~1ms
4. **No interruption** - you won't notice the pickup happening

## Features

- **Silent operation** - no visible mouse movement
- **Queue system** - processes items one at a time
- **Ultra-fast clicks** - 1ms click duration
- **Smart scanning** - only checks every 100ms
- **Same pickup rules** - legendaries, gems, materials, etc.
- **Same interactions** - shrines, chests, doors, etc.

## Hotkey

| Key | Action |
|-----|--------|
| `H` | Toggle ON/OFF |

## Configuration

Edit `AutoPickupSilentCustomizer.cs`:

```csharp
// Silent mode timing
plugin.PickupInterval = 100;      // Check every 100ms
plugin.ClickDuration = 1;         // Ultra-fast click
plugin.OnlyWhenStationary = false; // Always pickup

// Ranges
plugin.PickupRange = 12.0;        // Yards
plugin.InteractRange = 10.0;      // Yards
```

## Pickup Settings

```csharp
// High priority (always on)
plugin.PickupLegendary = true;
plugin.PickupAncient = true;
plugin.PickupPrimal = true;
plugin.PickupSet = true;
plugin.PickupGems = true;
plugin.PickupCraftingMaterials = true;
plugin.PickupDeathsBreath = true;
plugin.PickupRiftKeys = true;
plugin.PickupRamaladni = true;

// Low priority (off for speed)
plugin.PickupGold = false;
plugin.PickupRare = false;
```

## Tips

1. **Works while moving** - pickup happens seamlessly
2. **No skill interruption** - won't break your attack rotation
3. **Lower range = faster** - smaller pickup range means less scanning
4. **Disable old plugin** - the customizer auto-disables AutoMaster

## Files

| File | Description |
|------|-------------|
| `AutoPickupSilentPlugin.cs` | Main plugin |
| `AutoPickupSilentCustomizer.cs` | Configuration |
| `README.md` | This file |

---

*Press H to toggle - you won't even notice it's working!*
