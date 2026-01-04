namespace Turbo.Plugins.Custom.ItemReveal
{
    using SharpDX.DirectInput;
    using Turbo.Plugins.Default;

    /// <summary>
    /// Customizer for Item Reveal Plugin v2.3 - Core Edition
    /// 
    /// F4 = Toggle ON/OFF
    /// F5 = Toggle Debug Mode
    /// 
    /// This plugin shows item stats and Ancient/Primal status!
    /// Integrated with the Core Plugin Framework.
    /// </summary>
    public class ItemRevealCustomizer : BasePlugin, ICustomizer
    {
        public ItemRevealCustomizer()
        {
            Enabled = true;
        }

        public void Customize()
        {
            Hud.RunOnPlugin<ItemRevealPlugin>(plugin =>
            {
                // ========================================
                // KEY BINDINGS
                // ========================================
                
                // F4 = Toggle reveal ON/OFF
                plugin.ToggleKeyEvent = Hud.Input.CreateKeyEvent(true, Key.F4, false, false, false);
                
                // F5 = Toggle debug mode (shows raw data values)
                plugin.DebugKeyEvent = Hud.Input.CreateKeyEvent(true, Key.F5, false, false, false);


                // ========================================
                // DISPLAY SETTINGS
                // ========================================
                
                // Show stats on items in inventory/stash
                plugin.ShowInventoryStats = true;
                
                // Show stats on items on the ground
                plugin.ShowGroundStats = true;
                
                // Show perfection percentage for each stat
                plugin.ShowPerfection = true;
                
                // Only show for legendary items (set to false for all items)
                plugin.LegendaryOnly = true;
                
                // Maximum stats to show in tooltip
                plugin.MaxStatsToShow = 12;


                // ========================================
                // PERFECTION THRESHOLDS
                // ========================================
                
                // Perfection % to highlight as "good" (green)
                plugin.GoodPerfectionThreshold = 85f;
                
                // Perfection % to highlight as "great" (gold)
                plugin.GreatPerfectionThreshold = 95f;
            });
        }
    }
}
