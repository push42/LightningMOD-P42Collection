# Smart Salvage Plugin

An intelligent auto-salvage plugin for TurboHUD with build-specific blacklists, profile management, and Maxroll guide import functionality.

## Features

### Core Salvage Features
- **Auto-Salvage**: Automatically salvages items based on your blacklist configuration
- **Smart Protection**: Protects ancient/primal items, armory items, enchanted items, and socketed items
- **Build-Specific Blacklists**: Pre-configured blacklists for popular builds
- **Visual Feedback**: Yellow highlight for protected items, red for items to be salvaged

### Profile Management
- **Built-in Profiles**: Pre-configured protection lists for popular builds across all classes
- **Custom Profiles**: Create your own protection lists
- **Toggle On/Off**: Easily enable/disable profiles with a click
- **Import/Export**: Share profiles or backup your configuration
- **Persistence**: Profiles are automatically saved and loaded

### Maxroll Import
- **One-Click Import**: Paste a Maxroll guide URL to import the build's item list
- **Automatic Parsing**: Extracts item names from gear sections, cube powers, and alternatives
- **Smart Detection**: Identifies hero class and build name automatically

## Keybindings

| Key | Action |
|-----|--------|
| `U` | Start/Stop auto-salvage |
| `Shift+U` | Open/Close profile manager |

## Usage

### Basic Salvaging
1. Open the blacksmith salvage window
2. Press `U` to start auto-salvaging
3. Press `U` again to stop

### Managing Profiles
1. Open the blacksmith window
2. Press `Shift+U` to open the profile manager
3. Click on a profile to toggle it on/off
4. Green = Protected, Red = Can be salvaged

### Importing from Maxroll
1. Open the profile manager (`Shift+U`)
2. Copy a Maxroll D3 guide URL (e.g., `https://maxroll.gg/d3/guides/god-ha-demon-hunter-guide`)
3. Click on the URL input field (it will paste from clipboard)
4. Click "Import Build from URL"
5. The profile will be created and enabled automatically

### Saving/Exporting
- Click "?? Save" to save all profiles to disk
- Click "?? Export" to copy all profiles to clipboard
- Click "?? Reset" to restore default profiles

## Built-in Profiles

| Profile | Class | Status |
|---------|-------|--------|
| ?? Universal Items | All | Enabled |
| ?? Necro: LoD Death Nova | Necromancer | Enabled |
| ?? DH: GoD Hungering Arrow | Demon Hunter | Enabled |
| ?? WD: Mundunugu Spirit Barrage | Witch Doctor | Enabled |
| ?? Barb: Whirlwind Rend | Barbarian | Disabled |
| ?? Crus: Akkhan Bombardment | Crusader | Disabled |
| ?? Monk: Inna Mystic Ally | Monk | Disabled |
| ?? Wiz: Firebird Mirror Image | Wizard | Disabled |
| ?? Crafted Sets | All | Enabled |
| ?? Legendary Gems | All | Enabled |

## Configuration

Edit `SmartSalvageCustomizer.cs` to configure:

```csharp
public void Customize()
{
    Hud.RunOnPlugin<SmartSalvagePlugin>(plugin =>
    {
        // Key bindings
        plugin.SalvageKey = Hud.Input.CreateKeyEvent(true, Key.U, false, false, false);
        plugin.ManagerKey = Hud.Input.CreateKeyEvent(true, Key.U, false, false, true);

        // Salvage behavior
        plugin.AutoRepair = true;
        plugin.SalvageAncient = 1; // 0=smart, 1=never, 2=always
        plugin.SalvagePrimal = 1;  // 0=smart, 1=never, 2=always

        // Add custom protected items
        plugin.AddToBlacklist("Item Name 1", "Item Name 2");

        // Enable/disable profiles
        plugin.SetBuildEnabled("BarbWW", true);
    });
}
```

## Files

| File | Description |
|------|-------------|
| `SmartSalvagePlugin.cs` | Main plugin with UI and salvage logic |
| `SmartSalvageCustomizer.cs` | User configuration |
| `BlacklistProfile.cs` | Profile data model |
| `BlacklistManager.cs` | Profile management and persistence |
| `MaxrollCrawler.cs` | Maxroll guide HTML parser |
| `SmartSalvage_Profiles.txt` | Saved profiles (auto-generated) |

## Item Protection Rules

Items are **protected** (never salvaged) if:
- In the active blacklist (any enabled profile)
- Ancient or Primal (configurable)
- In an Armory set
- Has enchanted affixes
- Has gems socketed
- Is inventory locked
- Is vendor-bought
- Is a special item (Puzzle Ring, Ramaladni's Gift, etc.)

Items are **salvaged** if:
- Not in any protection category
- In your inventory (not equipped)
- Is loot or potion type

## Tips

1. **Enable profiles for your active builds** - Only enable profiles for builds you're currently using
2. **Import your own builds** - Use Maxroll import for any build not pre-configured
3. **Backup your profiles** - Use Export to save your configuration
4. **Check before salvaging** - Yellow = protected, Red = will be salvaged

## Troubleshooting

### Items not being protected
- Check if the profile is enabled (green indicator)
- Verify the item name matches exactly (English name)
- Try adding the item manually via customizer

### Import not working
- Ensure the URL is a valid Maxroll D3 guide
- Check your internet connection
- Some guides may have different HTML structures

### Profiles not saving
- Ensure the plugin folder is writable
- Check `SmartSalvage_Profiles.txt` was created

## Version History

- **v2.0** - Complete rewrite with profile management, Maxroll import, improved UI
- **v1.0** - Initial release with basic blacklist functionality
