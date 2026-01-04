namespace Turbo.Plugins.Custom.WizardStarPactMacro
{
    using SharpDX.DirectInput;
    using Turbo.Plugins.Default;

    /// <summary>
    /// Customizer for Wizard Star Pact Macro
    /// AGGRESSIVE MODE - Maximum DPS
    /// </summary>
    public class WizardStarPactMacroCustomizer : BasePlugin, ICustomizer
    {
        public WizardStarPactMacroCustomizer()
        {
            Enabled = true;
        }

        public void Customize()
        {
            Hud.RunOnPlugin<WizardStarPactMacroPlugin>(plugin =>
            {
                // ========================================
                // KEY BINDING
                // ========================================
                
                // F1 = Toggle macro on/off
                plugin.ToggleKeyEvent = Hud.Input.CreateKeyEvent(true, Key.F1, false, false, false);


                // ========================================
                // AGGRESSIVE SETTINGS
                // ========================================
                
                // Delay between actions (ms) - LOWER = FASTER
                // 30ms = ~33 actions per second (VERY AGGRESSIVE)
                plugin.ActionDelay = 30;

                // Attack range for Meteor/Hydra (yards)
                plugin.AttackRange = 60f;

                // Melee range for Spectral Blade (yards)
                plugin.MeleeRange = 20f;

                // Buff refresh threshold (seconds remaining)
                plugin.BuffRefreshTime = 50f;


                // ========================================
                // UI SETTINGS
                // ========================================
                
                // Hide the status tip when macro is off
                plugin.IsHideTip = false;
            });
        }
    }
}
