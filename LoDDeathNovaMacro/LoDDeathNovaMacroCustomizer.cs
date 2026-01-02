namespace Turbo.Plugins.Custom.LoDDeathNovaMacro
{
    using SharpDX.DirectInput;
    using Turbo.Plugins.Default;

    /// <summary>
    /// Customizer for LoD Death Nova (Blood Nova) Necromancer Macro
    /// 
    /// IMPORTANT: This build does NOT manually cast Death Nova!
    /// Iron Rose auto-casts Blood Nova while you channel Siphon Blood.
    /// 
    /// F1 = Toggle ON/OFF
    /// F2 = Switch between SPEED and PUSH mode
    /// F3 = Force Nuke (manual CoE sync override)
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

                // F3 = Force Nuke (bypass CoE timing in push mode)
                plugin.ForceNukeKeyEvent = Hud.Input.CreateKeyEvent(true, Key.F3, false, false, false);


                // ========================================
                // CoE TIMING SETTINGS (PUSH MODE)
                // ========================================

                // Physical CoE icon index (Necromancer: Cold=2, Physical=6, Poison=7)
                plugin.PhysicalCoEIconIndex = 6;

                // Seconds before Physical CoE to start preparing
                // Use this time to position Simulacrums and stack Funerary Pick
                plugin.PrePhysicalPrepSeconds = 1.0f;

                // Minimum Funerary Pick stacks before nuking (0-10)
                // Each stack = 20% damage, 10 stacks = 200% bonus
                plugin.MinFuneraryPickStacks = 5;

                // Minimum enemies nearby to trigger CoE nuke in push mode
                // More enemies = more Bloodtide Blade damage (400% per enemy up to 4000%)
                plugin.MinEnemiesForCoENuke = 3;


                // ========================================
                // BUFF REFRESH SETTINGS
                // ========================================

                // Bone Armor refresh threshold (seconds remaining)
                // Bone Armor gives up to 30% DR from 10 stacks
                // Also applies STUN for Krysbin's 300% bonus
                plugin.BoneArmorRefreshTime = 5.0f;


                // ========================================
                // COMBAT SETTINGS
                // ========================================

                // Range to detect enemies (yards)
                plugin.EnemyDetectionRange = 60f;

                // Range to detect elites for priority targeting
                plugin.EliteDetectionRange = 40f;

                // Health percent to trigger emergency Blood Rush
                plugin.EmergencyBloodRushHealthPct = 0.35f;

                // Bloodtide Blade range for stacking (yards)
                // Default is 25 yards matching the item effect
                // Each enemy = +400% Death Nova damage (up to 10 = 4000%)
                plugin.BloodtideRange = 25f;


                // ========================================
                // TIMING SETTINGS
                // ========================================

                // Delay between skill casts (ms)
                plugin.CastDelay = 50;


                // ========================================
                // UI / FEATURE SETTINGS
                // ========================================

                // Hide panel when macro is off
                plugin.IsHideTip = false;

                // Enable Oculus Ring circle detection and indicator
                // Oculus Ring gives +85% damage when standing in the circle
                plugin.EnableOculusDetection = true;
            });
        }
    }
}
