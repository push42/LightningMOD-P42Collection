namespace Turbo.Plugins.Custom.CustomPluginConfigPanel
{
    using Turbo.Plugins.Default;

    /// <summary>
    /// Customizer for Custom Plugin Config Panel
    /// </summary>
    public class CustomPluginConfigPanelCustomizer : BasePlugin, ICustomizer
    {
        public CustomPluginConfigPanelCustomizer()
        {
            Enabled = true;
        }

        public void Customize()
        {
            Hud.RunOnPlugin<CustomPluginConfigPanelPlugin>(plugin =>
            {
                // Panel position (percentage of screen)
                plugin.PanelX = 0.25f;  // 25% from left
                plugin.PanelY = 0.15f;  // 15% from top
                plugin.PanelWidth = 420f;
                plugin.PanelHeight = 450f;
            });
        }
    }
}
