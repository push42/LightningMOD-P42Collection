# Inventory Sorter Plugin for TurboHUD

An advanced inventory/stash sorting plugin with **in-game configuration UI**, **preset layouts**, and **smart item organization**.

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
               ??? SorterConfigUI.cs
               ??? PresetManager.cs
               ??? ItemCategory.cs
               ??? SortEnums.cs
               ??? README.md
   ```

3. Restart TurboHUD

---

## ? Features

### Sorting Modes
- **Category** - Primal > Ancient > Legendary > Set > Gems > Materials
- **Quality** - Highest quality items first
- **Type** - Group by equipment slot (Helm ? Boots ? Weapons)
- **Size** - Largest items first (space optimization)
- **A-Z** - Alphabetical by name

### Preset Layouts
- **Speed Farmer** - Fastest post-GR loop, minimal decisions
- **Collector** - One best copy of everything, organized by type
- **Minimalist** - Keep only what you actively use
- **Custom** - Your own rules

### In-Game Configuration
- Full settings panel accessible with **Ctrl+K**
- Mouse clicks are blocked from reaching the game
- Change settings without editing files!

### Smart Features
- Item highlighting (green = will sort, gold = protected)
- Progress bar during sorting
- Zone-based organization within tabs
- Respects inventory lock area
- Protects armory/enchanted/socketed items

---

## ?? Hotkeys

| Key | Action |
|-----|--------|
| **K** | Start sorting (or cancel if running) |
| **Shift+K** | Cycle through sort modes |
| **Ctrl+K** | Open configuration panel |
| **ESC** | Cancel sorting / Close config |

---

## ?? Usage

### Quick Sort
1. Open your **inventory** or **stash**
2. Press **K** to start sorting
3. Items are moved automatically
4. Press **K** or **ESC** to cancel anytime

### Change Sort Mode
1. Press **Shift+K** to cycle modes
2. Current mode shows in the panel
3. Each mode sorts items differently

### Configure Settings
1. Press **Ctrl+K** to open the config panel
2. Panel appears to the LEFT of inventory (doesn't overlap stash!)
3. Click settings to change them
4. Close with **ESC** or the X button

---

## ?? Configuration Panel Tabs

### Presets Tab
Choose from built-in organization layouts:
- **Speed Farmer** - Tab 1: Active builds, Tab 2: Keep candidates, Tab 3: Jewelry, Tab 4: Keys, Tab 5: Gems
- **Collector** - Universal ? Class Sets ? Weapons ? Jewelry ? Trophies
- **Minimalist** - Current build + Essentials only

### Sort Rules Tab
- Primary sort mode selection
- Toggle: Sort by quality first
- Toggle: Group set items together
- Toggle: Group gems by color
- Toggle: Primals always first
- Protection settings (lock, armory, enchanted, socketed)

### Zones Tab
Visual preview of stash zones:
- **High Value** (gold) - Jewelry, primals, perfect items
- **Grab Often** (blue) - Keys, common swaps
- **Run Starters** (pink) - Puzzle Rings, Screams
- **To Decide** (orange) - Items to evaluate

### Settings Tab
- Sort speed sliders (move delay, click delay)
- UI options (highlights, progress, confirmation)
- Hotkey reference

---

## ?? Item Highlights

When inventory/stash is open:
- **Green border** = Item will be sorted
- **Gold border** = Item is protected (won't be moved)

---

## ?? File Configuration

Edit `InventorySorterCustomizer.cs` for permanent changes:

```csharp
// === Key Bindings ===
plugin.SortKey = Hud.Input.CreateKeyEvent(true, Key.K, false, false, false);
plugin.ModeKey = Hud.Input.CreateKeyEvent(true, Key.K, false, false, true);
plugin.ConfigKey = Hud.Input.CreateKeyEvent(true, Key.K, true, false, false);

// === Protection ===
plugin.Config.RespectInventoryLock = true;
plugin.Config.ProtectArmoryItems = true;
plugin.Config.ProtectEnchantedItems = false;
plugin.Config.ProtectSocketedItems = false;

// === Sorting Rules ===
plugin.Config.SortByQualityFirst = true;
plugin.Config.GroupSets = true;
plugin.Config.GroupGemsByColor = true;
plugin.Config.PrimalsFirst = true;

// === Timing (adjust if too fast/slow) ===
plugin.Config.MoveDelayMs = 50;
plugin.Config.ClickDelayMs = 30;

// === UI ===
plugin.Config.ShowHighlights = true;
plugin.Config.ShowProgress = true;
```

---

## ?? Files

| File | Description |
|------|-------------|
| `InventorySorterPlugin.cs` | Main plugin with sorting logic |
| `InventorySorterCustomizer.cs` | User configuration |
| `SorterConfiguration.cs` | Settings class |
| `SorterConfigUI.cs` | In-game config panel |
| `PresetManager.cs` | Preset layouts and zones |
| `ItemCategory.cs` | Category definitions |
| `SortEnums.cs` | Sort mode enums |

---

## ?? Tips

### Speed Optimization
- Lower `MoveDelayMs` and `ClickDelayMs` for faster sorting
- But too fast may cause items to not move correctly
- Default values (50ms/30ms) work well for most systems

### Stash Organization Philosophy
1. **Separate current from potential** - Active gear ? maybe later
2. **Every tab has one job** - Mixed purpose = junk drawer
3. **Fixed positions beat sorting** - Muscle memory is key
4. **Make first 10 seconds efficient** - Dump ? salvage ? close
5. **Keep less than you think** - Ruthless "keep rules" help

### Best Practices
- Use **Speed Farmer** preset for active seasons
- Sort gems by color for quick socketing
- Keep jewelry separate (hardest to replace)
- Put run starters in consistent corner

---

## ? FAQ

**Q: Why doesn't clicking the config panel move my character?**
A: The plugin blocks mouse clicks when hovering over the config UI!

**Q: Config panel overlaps the stash?**
A: It's positioned to the LEFT of inventory specifically to avoid this.

**Q: Items not moving correctly?**
A: Increase timing values in settings or customizer file.

**Q: How do I reset to defaults?**
A: Delete `InventorySorter_Presets.txt` in the plugin folder.

---

## ?? Changelog

### v2.0.0 - Major Update
- Added in-game configuration UI
- Added preset layouts (Speed Farmer, Collector, etc.)
- Added zone-based organization
- Added item highlighting
- Added progress bar
- Added Ctrl+K hotkey for config
- UI positioned to avoid stash overlap
- Mouse clicks blocked on config panel

### v1.0.0
- Initial release with basic sorting

---

**Happy organizing! ??**
