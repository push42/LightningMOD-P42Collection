namespace Turbo.Plugins.Custom.Navigation
{
    using SharpDX.DirectInput;
    using Turbo.Plugins.Default;

    /// <summary>
    /// Customizer for Navigation Plugin
    /// 
    /// NOTE: Navigation overlay disabled by default when using ROS Bot
    /// ROS Bot handles pathing - our system is for danger detection only
    /// </summary>
    public class NavigationCustomizer : BasePlugin, ICustomizer
    {
        public NavigationCustomizer()
        {
            Enabled = true;
        }

        public void Customize()
        {
            Hud.RunOnPlugin<NavigationPlugin>(plugin =>
            {
                // DISABLED - ROS Bot handles navigation/pathing
                // Keep system available but hide overlay to avoid clutter
                plugin.ShowOverlay = false;  // Changed: Hide overlay (ROS Bot has better visuals)
                plugin.ShowPanel = false;    // Changed: Hide panel
                
                // These are all disabled since ShowOverlay = false
                plugin.ShowGrid = false;
                plugin.ShowWalkability = false;
                plugin.ShowDangerZones = false;
                plugin.ShowSafeZones = false;
                plugin.ShowEscapeRoutes = false;
                plugin.ShowPath = false;
                plugin.ShowThreatIndicators = false;
                plugin.ShowDensityHeatmap = false;
                plugin.ShowWallMarkers = false;
                plugin.ShowFrontiers = false;
                plugin.ShowPortals = false;

                // Panel position (hidden but configured)
                plugin.PanelX = 0.005f;
                plugin.PanelY = 0.72f;
            });
        }
    }
}
