namespace Turbo.Plugins.Custom.Core
{
    using SharpDX.DirectInput;
    using Turbo.Plugins.Default;

    /// <summary>
    /// Customizer for the Core Plugin Framework
    /// 
    /// Allows changing hotkeys and default settings.
    /// Edit this file to customize the Core behavior.
    /// 
    /// ═══════════════════════════════════════════════════════════════════════════
    /// DEFAULT HOTKEYS (chosen to avoid TurboHUD defaults F8-F12)
    /// ═══════════════════════════════════════════════════════════════════════════
    /// 
    /// CORE PLUGIN:
    ///   ` (Grave/Tilde)     = Open Plugin Hub / Settings Panel
    ///   Ctrl + `            = Toggle Debug Overlay
    ///   Shift + `           = Quick Toggle All Plugins ON/OFF
    /// 
    /// UTILITY PLUGINS:
    ///   P                   = Toggle Auto Pickup (AutoMaster)
    ///   K                   = Sort Inventory/Stash (InventorySorter)
    ///   Shift + K           = Cycle Sort Mode
    ///   Ctrl + K            = Open Sorter Config Panel
    ///   Shift + G           = Toggle Kadala Auto-Buy
    ///   Ctrl + G            = Cycle Kadala Item Type
    ///   Shift + J           = Toggle Smart Evade
    /// 
    /// BUILD-SPECIFIC MACROS (share same keys, only active for that class/build):
    ///   F1                  = Toggle Macro ON/OFF
    ///   F2                  = Switch Mode (Pull/Damage, etc.)
    ///   F3                  = Force Action (where applicable)
    /// 
    /// TURBOHUD DEFAULTS (DO NOT USE):
    ///   F8                  = Hide HUD
    ///   F9                  = Stat Tracker
    ///   F10                 = Bounty Table
    ///   F11                 = Debug Overlay
    ///   F12                 = Party Inspector
    ///   Ctrl+Alt+C          = Save Debug Data
    ///   Alt+C               = Capture Screenshot
    ///   Alt+End             = Exit TurboHUD
    /// 
    /// ═══════════════════════════════════════════════════════════════════════════
    /// </summary>
    public class CoreCustomizer : BasePlugin, ICustomizer
    {
        public CoreCustomizer()
        {
            Enabled = true;
        }

        public void Customize()
        {
            Hud.RunOnPlugin<CorePlugin>(plugin =>
            {
                // ═══════════════════════════════════════════════════════════
                // HOTKEYS - Uncomment and modify to change defaults
                // ═══════════════════════════════════════════════════════════
                
                // Settings panel (default: ` Grave/Tilde key)
                // Opens the Custom Plugins Hub where you can enable/disable
                // plugins and access their settings
                // plugin.SettingsKey = Hud.Input.CreateKeyEvent(true, Key.Grave, false, false, false);
                
                // Debug overlay (default: Ctrl+`)
                // Shows FPS, memory usage, entity counts, etc.
                // plugin.DebugKey = Hud.Input.CreateKeyEvent(true, Key.Grave, true, false, false);
                
                // Quick toggle all plugins (default: Shift+`)
                // Quickly enable/disable all registered plugins
                // plugin.QuickToggleKey = Hud.Input.CreateKeyEvent(true, Key.Grave, false, false, true);

                // ═══════════════════════════════════════════════════════════
                // ALTERNATIVE HOTKEY EXAMPLES
                // ═══════════════════════════════════════════════════════════
                
                // Use Insert key for settings panel:
                // plugin.SettingsKey = Hud.Input.CreateKeyEvent(true, Key.Insert, false, false, false);
                
                // Use Home key for debug overlay:
                // plugin.DebugKey = Hud.Input.CreateKeyEvent(true, Key.Home, false, false, false);
                
                // Use End key for quick toggle:
                // plugin.QuickToggleKey = Hud.Input.CreateKeyEvent(true, Key.End, false, false, false);
            });
        }
    }
}
