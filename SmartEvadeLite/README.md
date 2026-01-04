# Smart Evade Lite

A lightweight auto-dodge plugin for TurboHUD that helps you avoid dangerous ground effects with human-like reaction times.

## How It Works

Unlike aggressive auto-evade systems, Smart Evade Lite is designed to feel natural:

1. **Detection**: Only triggers when you're actually **standing inside** a dangerous area
2. **Delay**: Waits a **randomized 1.25-2 seconds** before reacting (simulates human reaction time)
3. **Single Action**: Performs **one escape movement** then waits for cooldown
4. **Cooldown**: Won't evade again for 3 seconds (prevents spammy behavior)

This makes it feel like a helpful assistant rather than a bot playing for you.

## Features

- **Human-like delays**: Random reaction time between 1.25-2 seconds
- **Cooldown system**: Prevents constant evading
- **Ground effect detection**: Recognizes major dangerous affixes
- **Visual indicators**: Optional danger circles on the ground
- **Minimal UI**: Small status panel showing current state

## Keybindings

| Key | Action |
|-----|--------|
| `J` | Toggle on/off |

## Danger Types Detected

| Affix | Priority | Notes |
|-------|----------|-------|
| Frozen | High | Ice ball explosions |
| Molten Explosion | High | Death explosion |
| Arcane | High | Rotating beams |
| Thunderstorm | High | Lightning strikes |
| Desecrator | Normal | Fire pools |
| Plagued | Normal | Poison pools |
| Frozen Pulse | Normal | Pulsing ice |
| Orbiter | Normal | Rotating orbs |
| Gas Cloud | Normal | Ghom-style poison |

## Configuration

Edit `SmartEvadeLiteCustomizer.cs`:

```csharp
public void Customize()
{
    Hud.RunOnPlugin<SmartEvadeLitePlugin>(plugin =>
    {
        // Toggle key
        plugin.ToggleKey = Hud.Input.CreateKeyEvent(true, Key.J, false, false, false);

        // Reaction time range (seconds)
        plugin.MinEvadeDelay = 1.25f;  // Minimum delay
        plugin.MaxEvadeDelay = 2.0f;   // Maximum delay

        // Cooldown between evades (seconds)
        plugin.EvadeCooldown = 3.0f;

        // Distance to move when evading
        plugin.EvadeDistance = 12f;

        // Show danger circles
        plugin.ShowDangerCircles = true;
    });
}
```

## Panel Display

The status panel shows:
- **OFF**: Plugin is disabled
- **Ready**: Plugin active, no danger detected
- **! [DangerType]**: Currently standing in danger, countdown to evade

When in danger, you'll see a countdown timer showing when the evade will trigger.

## Design Philosophy

This plugin is intentionally **not** a perfect auto-dodge system. It's designed to:

1. **Help casual players** avoid obvious mistakes (standing in fire)
2. **Feel natural** with human-like reaction delays
3. **Not replace skill** - you still need to position well
4. **Be fair** - single reaction, not constant kiting

If you want perfect, instant auto-dodge... this isn't for you. This is for players who occasionally miss a ground effect while focusing on damage or other mechanics.

## Tips

1. **Adjust delays** to match your playstyle - faster if you die too often, slower if it feels too automated
2. **Increase cooldown** if it triggers too often
3. **Watch the countdown** - you can manually move before the auto-evade if you prefer
4. **Disable in town** - it automatically disables in town areas

## Files

| File | Description |
|------|-------------|
| `SmartEvadeLitePlugin.cs` | Main plugin |
| `SmartEvadeLiteCustomizer.cs` | Configuration |
| `README.md` | This file |

## Comparison: Evade Lite vs Full Auto-Evade

| Feature | Evade Lite | Full Auto-Evade |
|---------|------------|-----------------|
| Reaction | 1.25-2s delay | Instant |
| Behavior | Single dodge | Constant kiting |
| Triggers | Only when inside danger | Preemptive avoidance |
| Cooldown | 3 seconds | None |
| Skill required | Medium | Low |
| Feels like | Helpful assistant | Bot playing for you |

---

*For personal use. Share responsibly.*
