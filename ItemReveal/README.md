# Item Reveal Plugin for TurboHUD

**Instantly see if unidentified items are Ancient or Primal!**

---

## ?? Important Clarification

**Item stats are rolled SERVER-SIDE when you identify them.**

This plugin CANNOT show actual stat rolls before identification - that's impossible because they don't exist yet!

### What IS Accurate on Unidentified Items:
- ? **Ancient status** - 100% accurate!
- ? **Primal status** - 100% accurate!
- ? **Set piece identification** - 100% accurate!
- ? **Item type/name** - 100% accurate!

### What This Means for You:
- Skip identifying trash legendaries
- Prioritize identifying Ancients/Primals
- Know if a ground drop is worth picking up
- Save time by focusing on quality items

---

## ?? Installation

1. Copy the `ItemReveal` folder to:
   ```
   TurboHUD\plugins\Custom\
   ```

2. Restart TurboHUD

---

## ? Features

### Ancient/Primal Detection
- **!! PRIMAL !!** - Red highlight + text for primal items
- **\* ANCIENT \*** - Orange highlight + text for ancient items
- Works on ground loot AND inventory items

### Identified Item Stats
For items that ARE identified, the plugin shows:
- Full stat breakdown with perfection %
- Color-coded quality (Green 85%+, Gold 95%+)
- Legendary powers/affixes
- Overall perfection score

### Visual Highlights
| Item Type | Highlight Color |
|-----------|-----------------|
| Primal (Unidentified) | Bright Red border |
| Ancient (Unidentified) | Orange border |
| Regular Legendary | Yellow border |

---

## ?? Hotkeys

| Key | Action |
|-----|--------|
| **F4** | Toggle Item Reveal ON/OFF |

---

## ?? Usage

### Ground Loot
- Ancient/Primal items show labels above them
- Know instantly if a drop is worth picking up
- Skip regular legendaries, grab the Ancients!

### Inventory
- Hover over any legendary item
- Unidentified items show Ancient/Primal status
- Identified items show full stats + perfection

### Workflow
1. See "!! PRIMAL !!" on ground ? Pick it up immediately!
2. See "\* ANCIENT \*" on ground ? Worth identifying
3. No label ? Regular legendary, lower priority

---

## ?? Configuration

Edit `ItemRevealCustomizer.cs`:

```csharp
// === Display Settings ===
plugin.ShowInventoryStats = true;     // Show in inventory/stash
plugin.ShowGroundStats = true;        // Show on ground items
plugin.ShowPerfection = true;         // Show perfection % (identified only)
plugin.ShowAncientStatus = true;      // Show Ancient/Primal indicator
plugin.LegendaryOnly = true;          // Only legendary items
plugin.MaxStatsToShow = 10;           // Max stats in tooltip

// === Perfection Thresholds (identified items only) ===
plugin.GoodPerfectionThreshold = 85f;  // Green at 85%+
plugin.GreatPerfectionThreshold = 95f; // Gold at 95%+
```

---

## ?? Time-Saving Tips

1. **Ground Loot Triage**
   - Primals: Pick up immediately
   - Ancients: Pick up if relevant
   - No label: Leave or pick up last

2. **Inventory Management**
   - Identify Primals first
   - Then Ancients
   - Regular legendaries last (or salvage blind)

3. **Efficient Farming**
   - Don't waste time identifying every legendary
   - Focus on Ancient/Primal items
   - Salvage regular legendaries without looking

---

## ?? Files

| File | Description |
|------|-------------|
| `ItemRevealPlugin.cs` | Main plugin |
| `ItemRevealCustomizer.cs` | Configuration |
| `README.md` | This file |

---

## ?? Changelog

### v1.1.0 - Accuracy Update
- Fixed: Clarified that stats cannot be revealed before identification
- Fixed: Ancient/Primal status is what's actually reliable
- Added: Better visual highlights for Ancients/Primals
- Added: Full stat display for identified items

### v1.0.0 - Initial Release
- Ancient/Primal detection on unidentified items
- Stat display for identified items

---

**Know your Ancients and Primals instantly! ??**
