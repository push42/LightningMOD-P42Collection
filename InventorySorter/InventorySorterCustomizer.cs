namespace Turbo.Plugins.Custom.InventorySorter
{
    using SharpDX.DirectInput;
    using Turbo.Plugins.Default;

    /// <summary>
    /// Customizer for Inventory Sorter Plugin
    /// 
    /// Hotkeys:
    /// - K = Start sorting (or cancel if running)
    /// - Shift+K = Cycle sort mode
    /// - Ctrl+K = Open configuration panel
    /// - ESC = Cancel sorting / Close config
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
                // KEY BINDINGS
                // ========================================
                
                // K = Sort (also cancels if running)
                plugin.SortKey = Hud.Input.CreateKeyEvent(true, Key.K, false, false, false);
                
                // Shift+K = Cycle through sort modes
                plugin.ModeKey = Hud.Input.CreateKeyEvent(true, Key.K, false, false, true);
                
                // Ctrl+K = Open configuration panel
                plugin.ConfigKey = Hud.Input.CreateKeyEvent(true, Key.K, true, false, false);
                
                // ESC = Cancel sorting / Close config
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


                // ========================================
                // SORTING RULES
                // ========================================
                
                // Sort highest quality items first
                plugin.Config.SortByQualityFirst = true;
                
                // Group set items together
                plugin.Config.GroupSets = true;
                
                // Group gems by color
                plugin.Config.GroupGemsByColor = true;
                
                // Always put Primals at the top
                plugin.Config.PrimalsFirst = true;


                // ========================================
                // TIMING SETTINGS (adjust if too fast/slow)
                // ========================================
                
                // Delay when moving items (ms)
                plugin.Config.MoveDelayMs = 50;
                
                // Delay between clicks (ms)
                plugin.Config.ClickDelayMs = 30;
                
                // Wait after each move (ms)
                plugin.Config.WaitAfterMoveMs = 20;


                // ========================================
                // UI SETTINGS
                // ========================================
                
                // Show green/gold highlights on items
                plugin.Config.ShowHighlights = true;
                
                // Show progress during sorting
                plugin.Config.ShowProgress = true;
                
                // Ask for confirmation before sorting
                plugin.Config.ConfirmBeforeSort = false;
            });
        }
    }
}
