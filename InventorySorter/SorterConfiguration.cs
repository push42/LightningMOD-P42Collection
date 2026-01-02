namespace Turbo.Plugins.Custom.InventorySorter
{
    using System.Collections.Generic;

    /// <summary>
    /// Configuration for the Inventory Sorter
    /// </summary>
    public class SorterConfiguration
    {
        // Protection settings
        public bool RespectInventoryLock { get; set; } = true;
        public bool ProtectArmoryItems { get; set; } = true;
        public bool ProtectEnchantedItems { get; set; } = false;
        public bool ProtectSocketedItems { get; set; } = false;
    }
}
