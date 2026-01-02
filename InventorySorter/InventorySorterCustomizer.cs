namespace Turbo.Plugins.Custom.InventorySorter
{
    using SharpDX.DirectInput;
    using Turbo.Plugins.Default;

    /// <summary>
    /// Customizer for Inventory Sorter Plugin
    /// </summary>
    public class InventorySorterCustomizer : BasePlugin, ICustomizer
    {
        public InventorySorterCustomizer()
        {
            Enabled = true;
        }

        public void Customize()
        {
            Hud.RunOnPlugin<InventorySorterPlugin>(plugin =>
            {
                // ========================================
                // KEY BINDINGS (Keyboard only - no mouse!)
                // ========================================
                
                // K = Sort (also cancels if running)
                plugin.SortKey = Hud.Input.CreateKeyEvent(true, Key.K, false, false, false);
                
                // Shift+K = Cycle through sort modes
                plugin.ModeKey = Hud.Input.CreateKeyEvent(true, Key.K, false, false, true);
                
                // ESC = Cancel sorting
                plugin.CancelKey = Hud.Input.CreateKeyEvent(true, Key.Escape, false, false, false);


                // ========================================
                // PROTECTION SETTINGS
                // ========================================
                
                // Respect inventory lock area
                plugin.Config.RespectInventoryLock = true;
                
                // Don't move items in armory sets
                plugin.Config.ProtectArmoryItems = true;
                
                // Don't move enchanted items
                plugin.Config.ProtectEnchantedItems = false;
                
                // Don't move socketed items
                plugin.Config.ProtectSocketedItems = false;
            });
        }
    }
}
