namespace Turbo.Plugins.Custom.LoDDeathNovaMacro
{
    using SharpDX.DirectInput;
    using Turbo.Plugins.Default;

    /// <summary>
    /// Customizer for LoD Death Nova Macro - Optimized Version
    /// 
    /// F1 = Toggle ON/OFF
    /// F2 = Switch between SPEED and PUSH mode
    /// F3 = Force Nuke (spam Death Nova)
    /// 
    /// SPEED MODE: Auto-move + Continuous Siphon Blood (Iron Rose procs)
    ///             Best for: Simulacrum + Haunted Visions builds
    /// 
    /// PUSH MODE: Auto-move + Manual Death Nova in packs + Siphon on boss
    ///            Best for: Nayr's + Squirt's (no Simulacrum) builds
    /// </summary>
    public class LoDDeathNovaMacroCustomizer : BasePlugin, ICustomizer
    {
        public LoDDeathNovaMacroCustomizer()
        {
            Enabled = true;
        }

        public void Customize()
        {
            Hud.RunOnPlugin<LoDDeathNovaMacroPlugin>(plugin =>
            {
                // ========================================
                // KEY BINDINGS
                // ========================================
                
                // F1 = Toggle macro ON/OFF
                plugin.ToggleKeyEvent = Hud.Input.CreateKeyEvent(true, Key.F1, false, false, false);
                
                // F2 = Switch between SPEED and PUSH mode
                plugin.ModeKeyEvent = Hud.Input.CreateKeyEvent(true, Key.F2, false, false, false);
                
                // F3 = Force Nuke (manual Death Nova spam)
                plugin.ForceNukeKeyEvent = Hud.Input.CreateKeyEvent(true, Key.F3, false, false, false);


                // ========================================
                // COMBAT SETTINGS
                // ========================================
                
                // Enemy detection range (yards)
                plugin.EnemyDetectionRange = 60f;
                
                // Elite detection range (yards)
                plugin.EliteDetectionRange = 40f;
                
                // Close range for Death Nova / combat (yards)
                plugin.CloseRange = 25f;
                
                // Bloodtide Blade range for stack counting (yards)
                plugin.BloodtideRange = 25f;
                
                // Minimum enemies to trigger Death Nova spam (large packs)
                plugin.MinEnemiesForNovaNuke = 5;


                // ========================================
                // TIMING SETTINGS (CRITICAL)
                // ========================================
                
                // Death Nova spam count during nuke phase
                plugin.DeathNovaSpamCount = 5;
                
                // Delay between Death Nova casts (ms)
                plugin.DeathNovaDelay = 100;
                
                // Delay after Bone Armor before nuking (ms)
                // Ensures stun applies for Krysbin's 300%
                plugin.BoneArmorWaitTime = 150;
                
                // Time to channel Siphon Blood after novas (ms)
                plugin.SiphonChannelTime = 400;
                
                // Force movement delay when no enemies (ms)
                plugin.MovementDelay = 100;
                
                // Combat exit delay to prevent flickering (ms)
                plugin.CombatExitDelay = 600;


                // ========================================
                // BUFF SETTINGS
                // ========================================
                
                // Bone Armor refresh threshold (seconds remaining)
                plugin.BoneArmorRefreshTime = 5.0f;
                
                // Minimum Funerary Pick stacks before nuking
                plugin.MinFuneraryPickStacks = 5;


                // ========================================
                // DEFENSE SETTINGS
                // ========================================
                
                // Health percent for emergency Blood Rush
                plugin.EmergencyBloodRushHealthPct = 0.35f;


                // ========================================
                // COE SETTINGS (for reference, currently push mode uses enemy count)
                // ========================================
                
                // Physical CoE index for Necromancer (6 = Physical)
                plugin.PhysicalCoEIconIndex = 6;
                
                // Seconds before Physical to prepare
                plugin.PrePhysicalPrepSeconds = 1.0f;


                // ========================================
                // UI SETTINGS
                // ========================================
                
                // Enable Oculus Ring circle detection
                plugin.EnableOculusDetection = true;
                
                // Hide panel when macro is off
                plugin.IsHideTip = false;
            });
        }
    }
}
