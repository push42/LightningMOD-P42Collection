# Custom Plugin Config Panel

Since TurboHUD's native config UI is protected and cannot be extended, this plugin provides an **in-game configuration overlay** for all custom plugins.

## How to Open

Press **Ctrl+Shift+C** to toggle the config panel on/off.

## Navigation

| Key | Action |
|-----|--------|
| **? / ?** | Navigate between plugins |
| **? / ?** | Switch category tabs |
| **Enter** | Toggle selected plugin on/off |
| **Escape** | Close the panel |
| **Ctrl+Shift+C** | Toggle panel visibility |

## Categories

### Macros
- **LoD Death Nova** - Necromancer Blood Nova macro
- **N6 Spike Trap** - Demon Hunter Natalya macro
- **Star Pact** - Wizard Meteor macro

### Evade
- **Smart Evade v2** - Auto-evade with wall awareness
- **Evade Lite** - Human-like delayed evade

### Utilities
- **Auto Pickup** - Auto-pickup items and globes
- **Smart Salvage** - Auto-salvage with blacklists
- **Inventory Sorter** - Sort inventory and stash

## Why This Exists

TurboHUD.exe is protected by ConfuserEx obfuscation, making it impossible to:
1. Decompile and modify the native config UI
2. Add custom categories to the existing plugin menu
3. Register new plugins with the built-in config system

This in-game panel is the alternative solution that provides similar functionality.

## Technical Notes

- The panel auto-discovers custom plugins by class name
- Settings are dynamically read from plugin properties
- Changes take effect immediately
- The panel only shows while in-game (not in main menu)

## Customization

Edit `CustomPluginConfigPanelCustomizer.cs` to adjust:
- Panel position (PanelX, PanelY)
- Panel size (PanelWidth, PanelHeight)
