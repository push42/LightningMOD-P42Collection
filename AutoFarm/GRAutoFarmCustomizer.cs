namespace Turbo.Plugins.Custom.AutoFarm
{
    using SharpDX.DirectInput;
    using Turbo.Plugins.Default;

    /// <summary>
    /// Customizer for GR Auto-Farm Bot
    /// 
    /// DISABLED - Waiting for ROS bot integration to build proper navigation system
    /// </summary>
    public class GRAutoFarmCustomizer : BasePlugin, ICustomizer
    {
        public GRAutoFarmCustomizer()
        {
            Enabled = false; // DISABLED - Waiting for ROS bot integration
        }

        public void Customize()
        {
            // Disabled - no customization needed
        }
    }
}
