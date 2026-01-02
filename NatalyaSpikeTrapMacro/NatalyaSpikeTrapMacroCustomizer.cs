namespace Turbo.Plugins.Custom.NatalyaSpikeTrapMacro
{
    using SharpDX.DirectInput;
    using Turbo.Plugins.Default;

    /// <summary>
    /// Customizer for Natalya Spike Trap Macro - Optimized Version
    /// 
    /// F1 = Toggle ON/OFF
    /// F2 = Switch between PULL and DAMAGE mode
    /// 
    /// PULL MODE: Caltrops first → Wait for grouping → Traps → Detonate
    /// DAMAGE MODE: Traps → Wait for arming → Detonate
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
                
                // PULL MODE: 2 traps (enemies are grouped, don't need as many)
                plugin.PullModeTraps = 2;

                // DAMAGE MODE: 5 traps for optimal chain reaction
                plugin.DamageModeTraps = 5;


                // ========================================
                // TIMING SETTINGS (CRITICAL FOR SMOOTH ROTATION)
                // ========================================
                
                // Delay between trap placements (ms)
                // Too fast = traps may not register, too slow = DPS loss
                plugin.TrapPlacementDelay = 80;

                // Time to wait after Caltrops for enemies to group (ms)
                // This is key for PULL mode - enemies need time to walk to Caltrops
                plugin.CaltropsWaitTime = 350;

                // Time to wait after placing traps before detonating (ms)
                // Traps need to "arm" and enemies need to be standing on them
                plugin.DetonationWaitTime = 200;

                // How long to channel Evasive Fire for reliable detonation (ms)
                plugin.DetonationDuration = 150;

                // Time between force-move commands when no enemies (ms)
                plugin.MovementDelay = 100;

                // Delay before exiting combat state when no enemies (ms)
                // Prevents cycling in/out of combat rapidly
                plugin.CombatExitDelay = 600;


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

                // Range considered "close" for trap placement
                // Used to check if enemies actually grouped up
                plugin.CloseRange = 25f;

                // Minimum enemies to start combat rotation
                plugin.MinEnemiesForCombat = 1;

                // Auto-move when no enemies nearby
                plugin.EnableAutoMovement = true;


                // ========================================
                // UI SETTINGS
                // ========================================
                
                // Hide panel when macro is off
                plugin.IsHideTip = false;
            });
        }
    }
}
