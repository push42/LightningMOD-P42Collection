# Pylon Alert Plugin

Sound and speech alerts when you receive pylon buffs - works even when teammates grab pylons across the map!

## Features

- **Speech Alerts**: Text-to-speech announces which pylon was grabbed
- **Sound Alerts**: Plays a notification sound
- **Visual Alerts**: On-screen notification with fade animation
- **Per-Pylon Control**: Enable/disable alerts for each pylon type
- **Customizable Speech**: Change what is spoken for each pylon
- **GR-Only Mode**: Option to only alert during Greater Rifts
- **Cooldown System**: Prevents alert spam

## Supported Pylons

| Pylon | Icon | Default Speech |
|-------|------|----------------|
| Power | ?? | "Power Pylon!" |
| Conduit | ? | "Conduit!" |
| Channeling | ?? | "Channeling Pylon!" |
| Shielding | ??? | "Shield Pylon!" |
| Speed | ?? | "Speed Pylon!" |

## Hotkeys

| Key | Action |
|-----|--------|
| `Shift+P` | Toggle alerts ON/OFF |

## How It Works

The plugin monitors your character's active buffs. When a pylon buff appears that wasn't there before, it triggers the alert. This means:

- Works when **you** grab a pylon
- Works when a **teammate** grabs a pylon (you still get the buff!)
- Works even if you're on the **opposite side of the map**

## Settings (via F10 Plugin Hub)

### Alert Types
- **Speech Alerts**: Enable/disable voice announcements
- **Sound Alerts**: Enable/disable sound effects
- **Visual Alerts**: Enable/disable on-screen notifications
- **GR Only**: Only alert during Greater Rifts

### Pylon Types
Toggle which pylons trigger alerts:
- Power Pylon
- Conduit Pylon
- Channeling Pylon
- Shielding Pylon
- Speed Pylon

### Timing
- **Cooldown**: Minimum time between alerts (default: 2 seconds)
- **Visual Duration**: How long the visual notification shows (default: 3 seconds)

## Customizer Configuration

Edit `PylonAlertCustomizer.cs` to customize:

```csharp
// Change speech text
plugin.SpeechPower = "Power Pylon activated!";
plugin.SpeechConduit = "Conduit is up!";

// Only alert in Greater Rifts
plugin.OnlyInGR = true;

// Disable visual, keep speech
plugin.EnableVisual = false;
plugin.EnableSpeech = true;
```

## Integration with Core

This plugin is fully integrated with the Core Plugin Framework:
- Appears in the F10 Plugin Hub
- Settings panel with toggles and selectors
- Status shown in sidebar
- Enable/disable via Core

## Changelog

### v1.0.0
- Initial release
- Speech, sound, and visual alerts
- Per-pylon configuration
- Core integration
