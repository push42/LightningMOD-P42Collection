namespace Turbo.Plugins.Community.NatalyaSpikeTrapMacro
{
    using SharpDX.DirectInput;
    using Turbo.Plugins.Default;

    /// <summary>
    /// Customizer for Natalya Spike Trap Macro
    /// 
    /// Edit this file to customize the macro settings!
    /// 
    /// F1 = Toggle ON/OFF
    /// F2 = Switch between PULL and DAMAGE mode
    /// </summary>
    public class NatalyaSpikeTrapMacroCustomizer : BasePlugin, ICustomizer
    {
        public NatalyaSpikeTrapMacroCustomizer()
        {
            Enabled = true;
        }

        public void Customize()
        {
            Hud.RunOnPlugin<NatalyaSpikeTrapMacroPlugin>(plugin =>
            {
                // ========================================
                // KEY BINDINGS
                // ========================================
                
                // F1 = Toggle macro ON/OFF
                plugin.ToggleKeyEvent = Hud.Input.CreateKeyEvent(true, Key.F1, false, false, false);
                
                // F2 = Switch between PULL and DAMAGE mode
                plugin.ModeKeyEvent = Hud.Input.CreateKeyEvent(true, Key.F2, false, false, false);


                // ========================================
                // TRAP SETTINGS
                // ========================================
                
                // PULL MODE: Place 2 traps, then Caltrops + Evasive Fire
                // Used to gather enemies together before nuking
                plugin.PullModeTraps = 2;

                // DAMAGE MODE: Place 5 traps, then Evasive Fire
                // Optimal for maximum chain reaction damage (10 traps with Custom Engineering)
                plugin.DamageModeTraps = 5;


                // ========================================
                // TIMING SETTINGS
                // ========================================
                
                // Delay between trap placements (ms) - lower = faster
                plugin.TrapPlacementDelay = 30;

                // Delay before detonating (ms)
                plugin.DetonationDelay = 50;

                // Delay between force movement commands (ms)
                plugin.MovementDelay = 100;


                // ========================================
                // BUFF SETTINGS
                // ========================================
                
                // Refresh Vengeance when this many seconds remain
                plugin.VengeanceRefreshTime = 3.0f;

                // Refresh Shadow Power when this many seconds remain
                plugin.ShadowPowerRefreshTime = 2.0f;


                // ========================================
                // COMBAT SETTINGS
                // ========================================
                
                // Range to detect enemies (yards)
                plugin.EnemyDetectionRange = 50f;

                // Minimum enemies to engage combat (1 = attack single targets)
                plugin.MinEnemiesForCombat = 1;

                // Enable auto-movement when no enemies nearby
                plugin.EnableAutoMovement = true;


                // ========================================
                // UI SETTINGS
                // ========================================
                
                // Hide panel when macro is off (set to true to hide)
                plugin.IsHideTip = false;
            });
        }
    }
}
