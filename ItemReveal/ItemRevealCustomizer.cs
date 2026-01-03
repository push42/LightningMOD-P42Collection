namespace Turbo.Plugins.Custom.ItemReveal
{
    using SharpDX.DirectInput;
    using Turbo.Plugins.Default;

    /// <summary>
    /// Customizer for Item Reveal Plugin
    /// 
    /// F4 = Toggle ON/OFF
    /// 
    /// This plugin shows item stats BEFORE identification!
    /// Hover over unidentified items to see their stats instantly.
    /// No more wasting time with the Book of Cain!
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


                // ========================================
                // DISPLAY SETTINGS
                // ========================================
                
                // Show stats on unidentified items in inventory/stash
                plugin.ShowInventoryStats = true;
                
                // Show stats on unidentified items on the ground
                plugin.ShowGroundStats = true;
                
                // Show perfection percentage for each stat
                plugin.ShowPerfection = true;
                
                // Show Ancient/Primal status on unidentified items
                plugin.ShowAncientStatus = true;
                
                // Only show for legendary items (set to false for all items)
                plugin.LegendaryOnly = true;
                
                // Maximum stats to show in tooltip
                plugin.MaxStatsToShow = 8;


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
