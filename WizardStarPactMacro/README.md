# Wizard Star Pact Macro

Auto-combat macro for the Weekly Challenge Rift Wizard build (Star Pact Meteor).

## Build Skills

| Slot | Skill | Rune | Behavior |
|------|-------|------|----------|
| Left Click | Spectral Blade | Barrier Blades | Auto - builds Arcane Dynamo |
| Right Click | Hydra | Blazing Hydra | Auto - placed with 5 stacks |
| 1 | Meteor | Star Pact | Auto - cast with 5 Arcane Dynamo stacks |
| 2 | Teleport | Wormhole | **MANUAL** - you control |
| 3 | Magic Weapon | Deflection | Auto - buff refresh |
| 4 | Storm Armor | Reactive Armor | Auto - buff refresh |

## Passives

- Unwavering Will
- **Arcane Dynamo** (key passive - 5 stacks = +60% damage)
- Galvanizing Ward
- Audacity

## How to Use

1. Press **F1** to enable the macro
2. The macro will:
   - Keep Magic Weapon and Storm Armor buffs active
   - Use Spectral Blade to build Arcane Dynamo stacks
   - Cast Meteor when at 5 stacks
   - Place Hydra when at 5 stacks
   - Auto-move towards cursor
3. You control **Teleport** manually (key 2)
4. Press **F1** again to stop

## Hotkey

| Key | Action |
|-----|--------|
| `F1` | Toggle macro ON/OFF |

## Status Panel

Shows in bottom-left area:
- **Star Pact RUNNING - Dynamo: X/5** when active
- **Star Pact OFF** when disabled

## Configuration

Edit `WizardStarPactMacroCustomizer.cs`:

```csharp
// Toggle key
plugin.ToggleKeyEvent = Key.F1;

// Arcane Dynamo stacks before casting Meteor (default 5)
plugin.ArcaneDynamoStacks = 5;

// Delay between actions (ms)
plugin.ActionDelay = 50;

// Buff refresh threshold (seconds)
plugin.BuffRefreshTime = 30f;

// Hide status when off
plugin.IsHideTip = false;
```

## Combat Logic

1. **Buffs first**: Refreshes Magic Weapon and Storm Armor if < 30s remaining
2. **Build stacks**: Casts Spectral Blade when enemies within 15 yards
3. **Spend stacks**: At 5 Arcane Dynamo stacks:
   - Cast Meteor (big damage)
   - Place Hydra (also benefits from stacks)
4. **Move**: Holds move towards cursor when not casting

## Tips

- **Teleport is manual** - use it to reposition safely
- The macro prioritizes building 5 stacks before spending
- Hydra is placed every ~8 seconds when enemies are nearby
- Macro stops if you open inventory, map, or chat

## Files

| File | Description |
|------|-------------|
| `WizardStarPactMacroPlugin.cs` | Main plugin |
| `WizardStarPactMacroCustomizer.cs` | Configuration |
| `README.md` | This file |

---

*Press F1 to toggle! Teleport manually with key 2.*
