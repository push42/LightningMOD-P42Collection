# Inventory Sorter Plugin for TurboHUD

A simple and reliable inventory/stash sorting plugin using **keyboard only** - no mouse clicking required!

---

## ?? Installation

1. Copy the `InventorySorter` folder to:
   ```
   TurboHUD\plugins\Custom\
   ```

2. Your folder structure should look like:
   ```
   TurboHUD\
   ??? plugins\
       ??? Custom\
           ??? InventorySorter\
               ??? InventorySorterPlugin.cs
               ??? InventorySorterCustomizer.cs
               ??? SorterConfiguration.cs
               ??? ItemCategory.cs
               ??? SortEnums.cs
               ??? README.md
   ```

3. Restart TurboHUD

---

## Features

- **Sort by Category** - Primal > Ancient > Legendary > Set > Gems > Materials
- **Sort by Quality** - Highest quality first
- **Sort by Type** - Group by equipment slot
- **Sort by Size** - Largest items first  
- **Sort Alphabetically** - A-Z by name

## Hotkeys

| Key | Action |
|-----|--------|
| **K** | Start sorting (or cancel if running) |
| **Shift+K** | Cycle through sort modes |
| **ESC** | Cancel sorting |

## Usage

1. Open your **inventory** or **stash**
2. Press **Shift+K** to select sort mode (Category, Quality, Type, Size, A-Z)
3. Press **K** to start sorting
4. Press **K** or **ESC** to cancel anytime

## UI Panel

A small info panel appears when inventory/stash is open:

```
????????????????????
? Inventory Sort   ?
? Mode: Category   ?
? [K] Sort [?K] Mode?
????????????????????
```

**Note:** The panel is display-only - use keyboard shortcuts to control!

## Configuration

Edit `InventorySorterCustomizer.cs`:

```csharp
// Change sort key (default: K)
plugin.SortKey = Hud.Input.CreateKeyEvent(true, Key.K, false, false, false);

// Change mode key (default: Shift+K)
plugin.ModeKey = Hud.Input.CreateKeyEvent(true, Key.K, false, false, true);

// Protection settings
plugin.Config.RespectInventoryLock = true;   // Don't move locked items
plugin.Config.ProtectArmoryItems = true;     // Don't move armory items
plugin.Config.ProtectEnchantedItems = false; // Move enchanted items
plugin.Config.ProtectSocketedItems = false;  // Move socketed items
```

## Why Keyboard Only?

TurboHUD overlays can't block mouse clicks from reaching the game. If we added clickable buttons, clicking them would also click the game world behind them - causing your character to move and closing the stash window!

Using keyboard shortcuts avoids this problem entirely.

## Files

| File | Description |
|------|-------------|
| `InventorySorterPlugin.cs` | Main plugin |
| `InventorySorterCustomizer.cs` | Configuration |
| `SorterConfiguration.cs` | Settings class |
| `ItemCategory.cs` | Category definitions |
| `SortEnums.cs` | Sort mode enums |
