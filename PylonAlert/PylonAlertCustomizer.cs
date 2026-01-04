namespace Turbo.Plugins.Custom.PylonAlert
{
    using SharpDX.DirectInput;
    using Turbo.Plugins.Default;

    /// <summary>
    /// Customizer for Pylon Alert Plugin
    /// Configure alerts and speech text
    /// </summary>
    public class PylonAlertCustomizer : BasePlugin, ICustomizer
    {
        public PylonAlertCustomizer()
        {
            Enabled = true;
        }

        public void Customize()
        {
            Hud.RunOnPlugin<PylonAlertPlugin>(plugin =>
            {
                // ========================================
                // KEY BINDING
                // ========================================
                
                // Shift+P = Toggle alerts on/off
                plugin.ToggleKey = Hud.Input.CreateKeyEvent(true, Key.P, false, false, true);

                // Start enabled
                plugin.SetActive(true);


                // ========================================
                // ALERT TYPES
                // ========================================
                
                plugin.EnableSpeech = true;   // Text-to-speech announcements
                plugin.EnableSound = true;    // Sound effect
                plugin.EnableVisual = true;   // On-screen notification
                plugin.OnlyInGR = false;      // Alert in any area (set true for GR only)


                // ========================================
                // PYLON TYPES TO ALERT
                // ========================================
                
                plugin.AlertPower = true;       // Power Pylon (+damage)
                plugin.AlertConduit = true;     // Conduit Pylon (lightning)
                plugin.AlertChanneling = true;  // Channeling Pylon (no resource cost)
                plugin.AlertShielding = true;   // Shielding Pylon (invulnerable)
                plugin.AlertSpeed = true;       // Speed Pylon (+movement)


                // ========================================
                // SPEECH CUSTOMIZATION
                // ========================================
                
                // Customize what is spoken for each pylon
                plugin.SpeechPower = "Power Pylon!";
                plugin.SpeechConduit = "Conduit!";
                plugin.SpeechChanneling = "Channeling Pylon!";
                plugin.SpeechShielding = "Shield Pylon!";
                plugin.SpeechSpeed = "Speed Pylon!";


                // ========================================
                // TIMING
                // ========================================
                
                plugin.AlertCooldownSeconds = 2.0f;   // Min time between alerts
                plugin.VisualDurationSeconds = 3.0f;  // How long visual shows
            });
        }
    }
}
