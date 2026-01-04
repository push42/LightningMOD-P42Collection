namespace Turbo.Plugins.Custom.SmartEvadeLite
{
    using SharpDX.DirectInput;
    using Turbo.Plugins.Default;

    /// <summary>
    /// Customizer for Smart Evade Lite Plugin
    /// Configure keybindings and evade behavior
    /// </summary>
    public class SmartEvadeLiteCustomizer : BasePlugin, ICustomizer
    {
        public SmartEvadeLiteCustomizer()
        {
            Enabled = true;
        }

        public void Customize()
        {
            Hud.RunOnPlugin<SmartEvadeLitePlugin>(plugin =>
            {
                // ========================================
                // KEY BINDINGS
                // ========================================
                
                // Key to toggle evade on/off
                plugin.ToggleKey = Hud.Input.CreateKeyEvent(true, Key.J, false, false, false);

                // Start enabled or disabled
                plugin.SetActive(false);


                // ========================================
                // EVADE TIMING
                // ========================================
                
                // Minimum delay before evading (seconds)
                // Makes it look more human - doesn't instantly react
                plugin.MinEvadeDelay = 1.25f;

                // Maximum delay before evading (seconds)
                // Random delay between min and max is chosen each time
                plugin.MaxEvadeDelay = 2.0f;

                // Cooldown between evade attempts (seconds)
                // Prevents spam-dodging, makes it feel natural
                plugin.EvadeCooldown = 3.0f;


                // ========================================
                // EVADE BEHAVIOR
                // ========================================
                
                // How far to move when evading (yards)
                plugin.EvadeDistance = 12f;


                // ========================================
                // VISUAL SETTINGS
                // ========================================
                
                // Show danger circles on the ground
                plugin.ShowDangerCircles = true;

                // Panel position (percentage of screen)
                plugin.PanelX = 0.005f;
                plugin.PanelY = 0.49f;
            });
        }
    }
}
