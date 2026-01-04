# Item Reveal Plugin v2.2 for TurboHUD

**See Ancient/Primal status and possible stat ranges on unidentified items!**

---

## ?? The Truth About Item Stats

### What We CAN See (100% Accurate):
| Data | Status | Notes |
|------|--------|-------|
| **Ancient/Primal** | ? Accurate | Always visible, even unidentified |
| **Set Item** | ? Accurate | SetSno is populated |
| **Item Type** | ? Accurate | From SnoItem data |
| **Stat Ranges** | ? Accurate | Min/Max values show what CAN roll |
| **Item Seed** | ? Available | Unique identifier |

### What Is HIDDEN (Server-Side):
| Data | Status | Reason |
|------|--------|--------|
| **Actual Stat Values** | ? Hidden | `Cur` is 0.00 until identified |
| **Perfection %** | ? Hidden | Calculated from actual values |
| **Legendary Power Values** | ? Hidden | Server determines on ID |

### Why?
Diablo 3 uses **server-authoritative stat rolling**. The actual values are NOT stored client-side until you identify the item. TurboHUD can only read what the game client knows.

---

## ? What This Plugin Shows

### For Unidentified Items:
```
Dead Man's Legacy
* ANCIENT *
[Set Item]
????????????????????????
? UNIDENTIFIED
????????????????????????
? Confirmed Data:
  Quality: ANCIENT
  Set Item: Yes
  Seed: 392493527

?? Possible Stats (ranges):
  IAS%: 15.0% - 20.0%
  LegPower: 150% - 200%

? Actual values hidden until ID
(Server-side stat rolling)
```

### For Identified Items:
- Full stat breakdown with actual values
- Perfection % for each stat
- Color-coded quality (Green 85%+, Gold 95%+)
- Legendary powers

---

## ?? Hotkeys

| Key | Action |
|-----|--------|
| **F4** | Toggle plugin ON/OFF |
| **F5** | Toggle Raw Data mode |

---

## ?? How To Use This

### Priority System:
1. **!! PRIMAL !!** ? Identify immediately! (Red highlight)
2. **\* ANCIENT \*** ? High priority (Orange highlight)
3. **[SET]** ? Check if you need it
4. No label ? Regular legendary

### Stat Range Reading:
- `IAS%: 15.0% - 20.0%` means the stat will roll somewhere in that range
- Narrow ranges = consistent rolls
- Wide ranges = more variance

### Ground Loot:
- Ancient/Primal labels appear above items on ground
- No need to hover - instant visibility

---

## ?? Configuration

Edit `ItemRevealCustomizer.cs`:

```csharp
plugin.ShowInventoryStats = true;      // Show in inventory
plugin.ShowGroundStats = true;         // Show on ground items
plugin.ShowPerfection = true;          // Show % on identified
plugin.ShowAncientStatus = true;       // Show Ancient/Primal
plugin.LegendaryOnly = true;           // Only legendaries
plugin.MaxStatsToShow = 12;            // Max stats displayed
plugin.GoodPerfectionThreshold = 85f;  // Green threshold
plugin.GreatPerfectionThreshold = 95f; // Gold threshold
```

---

## ?? Changelog

### v2.2.0 - Truth Edition
- FIXED: Now correctly shows stat RANGES for unidentified items
- FIXED: Clear indication of what data is confirmed vs hidden
- NEW: Shows "Possible Stats" with Min-Max ranges
- NEW: Cleaner layout for unidentified items
- IMPROVED: Better explanation of server-side stat rolling

### v2.1.0 - Deep Memory Explorer
- Added comprehensive debug mode
- Shows all available IItem properties

### v2.0.0 - Experimental
- Initial unidentified item exploration

### v1.0.0 - Initial
- Ancient/Primal detection

---

## ?? Technical Details

TurboHUD's `IItemPerfection` structure:
- `Min` - Minimum possible value (available)
- `Max` - Maximum possible value (available)  
- `Cur` - Current/actual value (0.00 for unidentified!)

The game client knows WHAT stats can roll (Min/Max), but the server determines the actual values when you identify.

---

**Know your Ancients instantly! ??**
