# Custom Plugin Core Framework v2.0

A centralized framework for managing all custom TurboHUD plugins with a unified UI, shared design system, and settings management.

---

## Features

### ??? Central Plugin Hub (F8)
- View all registered plugins in one place
- Enable/disable plugins with a single click
- Category-based organization
- Per-plugin settings panels
- Quick toggle all plugins (Shift+F8)

### ?? Shared Design System
- Consistent typography (fonts for titles, headers, body, etc.)
- Semantic colors (success, warning, error, accent)
- Pre-built UI components (buttons, toggles, inputs, cards)
- Progress bars, badges, status indicators
- Item highlight brushes

### ?? Settings Persistence
- Plugin enabled states saved automatically
- Per-plugin settings support
- JSON-based storage

### ?? Developer Tools
- Debug overlay (F9) with FPS, memory, entity counts
- Consistent logging API
- Reusable UI components library

---

## Hotkeys

| Key | Action |
|-----|--------|
| F8 | Toggle Plugin Hub |
| Shift+F8 | Toggle all plugins ON/OFF |
| F9 | Toggle Debug Overlay |

---

## Creating a Custom Plugin

### 1. Inherit from CustomPluginBase

```csharp
using Turbo.Plugins.Custom.Core;

public class MyAwesomePlugin : CustomPluginBase, IInGameTopPainter
{
    // Required: Plugin metadata
    public override string PluginId => "my-awesome-plugin";
    public override string PluginName => "My Awesome Plugin";
    public override string PluginDescription => "Does awesome things";
    public override string PluginVersion => "1.0.0";
    public override string PluginCategory => "utility";  // combat, automation, inventory, visual, utility, debug
    public override string PluginIcon => "?";

    public MyAwesomePlugin()
    {
        Enabled = true;
    }

    public override void Load(IController hud)
    {
        base.Load(hud);  // Important: calls registration
        // Your initialization code
    }

    public void PaintTopInGame(ClipState clipState)
    {
        if (!Enabled) return;  // Check if plugin is enabled
        // Your rendering code
    }
}
```

### 2. Add Settings Panel (Optional)

```csharp
public class MyPluginWithSettings : CustomPluginBase
{
    // ... metadata ...

    // Enable settings panel
    public override bool HasSettings => true;

    // Settings values
    public bool FeatureEnabled { get; set; } = true;
    public int SomeValue { get; set; } = 50;

    // Draw settings UI
    public override void DrawSettings(IController hud, RectangleF rect, 
        Dictionary<string, RectangleF> clickAreas, int scrollOffset)
    {
        float x = rect.X, y = rect.Y, w = rect.Width;

        // Section header
        y += DrawSettingsHeader(x, y, "General Settings");
        y += 8;

        // Toggle setting
        y += DrawToggleSetting(x, y, w, "Enable Feature", FeatureEnabled, 
            clickAreas, "toggle_feature");
        
        // More settings...
    }

    // Handle clicks
    public override void HandleSettingsClick(string clickId)
    {
        if (clickId == "toggle_feature")
        {
            FeatureEnabled = !FeatureEnabled;
            SavePluginSettings();
        }
    }

    // Settings persistence
    protected override object GetSettingsObject() => new MySettings
    {
        FeatureEnabled = this.FeatureEnabled,
        SomeValue = this.SomeValue
    };

    protected override void ApplySettingsObject(object settings)
    {
        if (settings is MySettings s)
        {
            FeatureEnabled = s.FeatureEnabled;
            SomeValue = s.SomeValue;
        }
    }

    private class MySettings : PluginSettingsBase
    {
        public bool FeatureEnabled { get; set; }
        public int SomeValue { get; set; }
    }
}
```

### 3. Use Shared UI Components

```csharp
// Access Core's design system
if (HasCore)
{
    // Use shared fonts
    Core.FontTitle.DrawText(...);
    Core.FontBody.DrawText(...);
    Core.FontSuccess.DrawText(...);

    // Use shared brushes
    Core.SurfaceCard.DrawRectangle(...);
    Core.StatusSuccess.DrawRectangle(...);
    Core.HighlightPositive.DrawRectangle(...);
}

// Or use UIComponents static helpers
UIComponents.DrawButton(rect, "Click Me", isHovered);
UIComponents.DrawToggle(x, y, 44, 22, isEnabled);
UIComponents.DrawProgressBar(x, y, w, h, progress);
UIComponents.DrawBadge(x, y, "??", "5", StatusType.Success);
UIComponents.DrawItemHighlight(itemRect, HighlightType.Positive);
```

---

## Available Categories

| Category | Icon | Description |
|----------|------|-------------|
| combat | ?? | Combat automation and macros |
| automation | ?? | Auto-salvage, sorting, etc. |
| inventory | ?? | Inventory and stash management |
| visual | ??? | Visual overlays and highlights |
| utility | ?? | General utilities |
| debug | ?? | Development and debugging tools |

---

## UI Components Available

### Buttons
- `DrawButton()` - Standard button
- `DrawIconButton()` - Icon-only button
- `DrawToggleButton()` - Two-state toggle button
- `DrawTabButton()` - Tab-style button

### Inputs
- `DrawToggle()` - Toggle switch
- `DrawCheckbox()` - Checkbox
- `DrawInputField()` - Text input
- `DrawSelector()` - Value selector with arrows
- `DrawSlider()` - Slider control

### Layout
- `DrawPanel()` - Panel background
- `DrawCard()` - Card container
- `DrawSection()` - Section with header
- `DrawSeparator()` - Horizontal line

### Status
- `DrawProgressBar()` - Progress indicator
- `DrawBadge()` - Icon + value badge
- `DrawTag()` - Small tag/pill
- `DrawStatusDot()` - Status indicator dot

### Text
- `DrawTitle()` - Large title
- `DrawHeader()` - Section header
- `DrawSubheader()` - Subsection header
- `DrawText()` - Body text
- `DrawHint()` - Muted hint text

### Utility
- `DrawVerticalScrollbar()` - Vertical scrollbar
- `DrawItemHighlight()` - Item border highlight
- `DrawTooltip()` - Hover tooltip
- `IsMouseOver()` - Hit testing
- `TruncateText()` - Text truncation

---

## File Structure

```
plugins/Custom/Core/
??? CorePlugin.cs          # Main hub plugin
??? UIComponents.cs        # Shared UI component library
??? CustomPluginBase.cs    # Base class for custom plugins
??? CoreCustomizer.cs      # Optional customizer
??? README.md              # This file
??? settings.json          # Saved plugin states
```

---

## Best Practices

1. **Always check `Enabled`** - Respect the enable/disable state
2. **Use `HasCore`** - Check before accessing Core resources
3. **Save settings** - Call `SavePluginSettings()` after changes
4. **Use shared UI** - Maintain visual consistency
5. **Log appropriately** - Use `Log()`, `LogInfo()`, `LogWarn()`, `LogError()`
6. **Handle null gracefully** - Core may not be loaded yet

---

## Version History

### v2.0
- Per-plugin settings panels
- Settings persistence
- Enhanced UI components
- Improved category system
- Debug overlay
- Quick toggle all (Shift+F8)

### v1.0
- Initial release
- Basic plugin registry
- Shared design system

---

**Your plugins, unified! ?**
