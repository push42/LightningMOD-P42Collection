# Item Reveal Plugin for TurboHUD

**See item stats BEFORE identification!** No more wasting time with the Book of Cain!

TurboHUD can read item affixes even on unidentified items - the game just hides them visually. This plugin reveals those hidden stats instantly.

---

## ?? Installation

1. Copy the `ItemReveal` folder to:
   ```
   TurboHUD\plugins\Custom\
   ```

2. Your folder structure should look like:
   ```
   TurboHUD\
   ??? plugins\
       ??? Custom\
           ??? ItemReveal\
               ??? ItemRevealPlugin.cs
               ??? ItemRevealCustomizer.cs
               ??? README.md
   ```

3. Restart TurboHUD

---

## ? Features

### Instant Item Stats
- **Hover over unidentified items** to see all their stats
- Works in inventory, stash, and on the ground
- No need to identify first!

### Quality Indicators
- **? PRIMAL ANCIENT** - Red text for primal items
- **? ANCIENT** - Orange text for ancient items
- **Set Item** - Green text for set pieces

### Perfection Tracking
- Shows **overall perfection %** for each item
- Shows **individual stat perfection** for each affix
- **Color-coded**: White (normal), Green (85%+), Gold (95%+)

### Ground Loot Preview
- Unidentified legendaries on the ground show:
  - Ancient/Primal status
  - Overall perfection percentage
  - Instant assessment without picking up!

---

## ?? Hotkeys

| Key | Action |
|-----|--------|
| **F4** | Toggle Item Reveal ON/OFF |

---

## ?? Usage

1. **Enable the plugin** (ON by default)
2. **Hover over any unidentified legendary item**
3. **See the full stats instantly!**
4. **Decide** whether to keep or salvage before identifying

### What You'll See
- Item name
- Ancient/Primal status
- Set affiliation
- Overall perfection percentage
- Individual stat values with perfection %
- Legendary affixes/powers

---

## ?? Configuration

Edit `ItemRevealCustomizer.cs`:

```csharp
// === Display Settings ===
plugin.ShowInventoryStats = true;     // Show stats in inventory/stash
plugin.ShowGroundStats = true;        // Show stats on ground items
plugin.ShowPerfection = true;         // Show perfection percentages
plugin.ShowAncientStatus = true;      // Show Ancient/Primal indicator
plugin.LegendaryOnly = true;          // Only reveal legendary items
plugin.MaxStatsToShow = 8;            // Max stats in tooltip

// === Perfection Thresholds ===
plugin.GoodPerfectionThreshold = 85f;  // Green highlight at 85%+
plugin.GreatPerfectionThreshold = 95f; // Gold highlight at 95%+
```

---

## ?? Color Coding

| Color | Meaning |
|-------|---------|
| **White** | Normal perfection (below 85%) |
| **Green** | Good perfection (85-94%) |
| **Gold** | Great perfection (95%+) |
| **Orange** | Ancient item |
| **Red** | Primal Ancient item |

---

## ?? Tips

### Time Saving
- Skip the Book of Cain entirely
- Instantly know if a drop is worth keeping
- Focus on grinding, not identifying

### Quality Assessment
- Look for **high overall perfection** (90%+)
- Check **individual stat rolls** for key affixes
- Primals are always max rolled (100%)

### Ground Loot
- See Ancient/Primal status before picking up
- Know the perfection before cluttering inventory
- Skip bad legendaries immediately

---

## ? FAQ

**Q: Does this work for all items?**
A: By default, only legendary items. Set `LegendaryOnly = false` for all items.

**Q: Why doesn't an item show stats?**
A: Some items may not have readable stats until identified. This is rare.

**Q: Can I get banned for this?**
A: TurboHUD plugins read game memory; use at your own risk as with all TurboHUD features.

---

## ?? Files

| File | Description |
|------|-------------|
| `ItemRevealPlugin.cs` | Main plugin |
| `ItemRevealCustomizer.cs` | Configuration |
| `README.md` | This file |

---

## ?? Changelog

### v1.0.0 - Initial Release
- Reveal stats on unidentified items
- Inventory, stash, and ground support
- Perfection percentage display
- Ancient/Primal indicators
- Color-coded quality assessment

---

**Never waste time identifying again! ??**
