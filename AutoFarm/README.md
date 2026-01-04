# God-Tier GR Auto-Farm Bot

Fully automatic Greater Rift farming with intelligent navigation, elite prioritization, and comprehensive safety systems.

---

## ?? IMPORTANT WARNING

**Using bots may violate Blizzard's Terms of Service and could result in account suspension or ban. Use at your own risk!**

This is for **educational purposes** and **personal use only**.

---

## ?? Features

### Core Automation
- ? **Full GR Loop** - Town ? Open GR ? Farm ? Boss ? Gems ? Repeat
- ? **Smart Navigation** - A* pathfinding with danger avoidance
- ? **Elite Hunting** - Prioritizes elite packs for efficiency
- ? **Pylon Management** - Saves power pylons for boss fight
- ? **Auto Gem Upgrades** - Upgrades gems after each GR
- ? **Loot Collection** - Picks up legendaries, ancients, primals

### Intelligence
- ? **Spatial Awareness** - Real-time danger zone tracking
- ? **Monster Prediction** - Predicts monster movement
- ? **Safe Pathing** - Avoids ground effects automatically
- ? **Density Detection** - Finds optimal kill spots

### Safety Systems
- ? **Death Tracking** - Stops after configurable deaths
- ? **Stuck Detection** - Detects and recovers from stuck
- ? **Health Monitoring** - Pauses when low HP
- ? **Timeout Protection** - Maximum run time limit

---

## ?? Hotkeys

| Key | Action |
|-----|--------|
| **Ctrl+Shift+F** | Toggle bot ON/OFF |
| **Ctrl+F** | Pause/Resume |
| **F11** | Toggle overlay |

---

## ?? State Machine

```
???????????????????????????????????????????????????????????????
?                      GR BOT STATES                          ?
???????????????????????????????????????????????????????????????
?                                                             ?
?  IDLE ??????????> IN_TOWN                                   ?
?                      ?                                      ?
?                      ?                                      ?
?               OPENING_RIFT                                  ?
?                      ?                                      ?
?                      ?                                      ?
?            WAITING_FOR_PORTAL                               ?
?                      ?                                      ?
?                      ?                                      ?
?              ENTERING_RIFT                                  ?
?                      ?                                      ?
?                      ?                                      ?
?  ????????????????IN_RIFT?????????????????????              ?
?  ?                   ?                       ?              ?
?  ?    ???????????????????????????????       ?              ?
?  ?    ?              ?              ?       ?              ?
?  ? HUNTING      KILLING_TRASH   MOVING_TO  ?              ?
?  ? _ELITES          ?          _OBJECTIVE  ?              ?
?  ?    ?              ?              ?       ?              ?
?  ?    ???????????????????????????????       ?              ?
?  ?                   ?                       ?              ?
?  ?                   ? (100% progress)       ?              ?
?  ?          WAITING_FOR_BOSS                ?              ?
?  ?                   ?                       ?              ?
?  ?                   ?                       ?              ?
?  ?            KILLING_BOSS                  ?              ?
?  ?                   ?                       ?              ?
?  ?                   ?                       ?              ?
?  ?          COLLECTING_LOOT                 ?              ?
?  ?                   ?                       ?              ?
?  ?                   ?                       ?              ?
?  ?            EXITING_RIFT                  ?              ?
?  ?                   ?                       ?              ?
?  ?                   ?                       ?              ?
?  ?          UPGRADING_GEMS                  ?              ?
?  ?                   ?                       ?              ?
?  ???????????????????????????> COMPLETED ?????              ?
?                                    ?                        ?
?                                    ?                        ?
?                               IN_TOWN (loop)                ?
?                                                             ?
???????????????????????????????????????????????????????????????
```

---

## ?? Configuration

Edit `GRAutoFarmCustomizer.cs`:

### General
```csharp
config.TargetGRLevel = 100;          // GR level to run
config.MaxDeaths = 3;                // Max deaths per run
config.MaxRunTimeMinutes = 15;       // Timeout
```

### Combat
```csharp
config.KiteDistance = 15f;           // Ranged kiting distance
config.EngageDistance = 40f;         // Attack range
config.SafeHealthPercent = 50f;      // Retreat HP threshold
```

### Pylon Strategy
```csharp
config.SavePylonsForBoss = true;     // Save good pylons
config.PylonsToSave = new[] {
    "Power",
    "Conduit",
    "Channeling"
};
```

### Gem Upgrades
```csharp
config.UpgradeGems = true;
config.MaxGemUpgradeAttempts = 3;
config.EmpowerRifts = true;          // Extra upgrade
config.GemsToUpgrade = new[] {
    "Bane of the Trapped",
    "Bane of the Stricken",
    "Zei's Stone of Vengeance"
};
```

---

## ?? Statistics

The bot tracks comprehensive statistics:

### Per-Run Stats
- Duration
- Deaths
- Elites killed
- Progress %
- Legendaries found
- Gems upgraded
- Distance traveled
- Paths calculated

### Session Stats
- Total runs
- Success rate
- Runs per hour
- Average time
- Fastest/slowest runs
- Total legendaries
- Total gems upgraded

---

## ??? Navigation System

The bot uses the full Navigation system:

### Components Used
1. **NavMesh Manager** - Loads extracted NavMesh data
2. **Spatial Awareness** - Real-time danger detection
3. **A* Pathfinder** - Optimal path calculation
4. **Escape Route Analysis** - Safe retreat paths

### Path Features
- Danger avoidance (ground effects)
- Wall awareness
- Dynamic replanning
- Path smoothing

---

## ?? Files

| File | Description |
|------|-------------|
| `GRBotTypes.cs` | State machine, config, stats classes |
| `GRBotBrain.cs` | Decision making and state management |
| `GRAutoFarmPlugin.cs` | Main plugin with UI |
| `GRAutoFarmCustomizer.cs` | Configuration |
| `README.md` | This documentation |

---

## ?? Integration

### Using with SmartEvade

The bot automatically uses the Spatial Awareness engine which includes:
- Ground effect detection
- Safe zone finding
- Escape route calculation

### Using with Navigation

Full integration with the Navigation system:
- A* pathfinding
- NavMesh data
- Wall detection

---

## ?? Objective Priority

1. **Pylons** (90) - Grab power-ups first
2. **Elites** (80) - Hunt elite packs
3. **Shrines** (70) - Regular shrines
4. **Trash Packs** (50) - Dense mob groups

---

## ?? Class-Specific Settings

### Ranged Builds
```csharp
config.KiteDistance = 20f;
config.EngageDistance = 50f;
config.SafeHealthPercent = 60f;
```

### Melee Builds
```csharp
config.KiteDistance = 0f;            // Don't kite
config.EngageDistance = 20f;
config.SafeHealthPercent = 40f;
```

### Tanky Builds
```csharp
config.SkipTrashPacks = false;
config.SafeHealthPercent = 30f;
config.MaxDeaths = 5;
```

### Speed Builds
```csharp
config.SkipTrashPacks = true;        // Skip low density
config.MinMobsToEngage = 10;
config.PrioritizeElites = true;
```

---

## ?? Troubleshooting

### Bot gets stuck
- Check `StuckTimeoutSeconds` setting
- Enable `VerboseLogging` to see what's happening
- May need NavMesh data for the area

### Bot dies too much
- Lower `SafeHealthPercent`
- Check `KiteDistance` for your build
- Enable `UseDefensivesAutomatically`

### Not picking up loot
- Check `PickupLegendaries` etc. settings
- Verify items are on the floor

### Portal not clicking
- Check UI element detection
- May need to adjust interaction timing

---

## ?? TODO

- [ ] Better UI element detection
- [ ] Class-specific skill rotation integration
- [ ] Bounty farming mode
- [ ] Key farming mode
- [ ] Multi-GR level optimization
- [ ] Follower management

---

## ?? Changelog

### v1.0.0
- Initial release
- Full GR loop automation
- Smart navigation with A*
- Pylon management
- Gem upgrades
- Comprehensive statistics

---

*Press Ctrl+Shift+F to start the bot!*

**?? Use responsibly - botting violates Blizzard ToS!**
