namespace Turbo.Plugins.Custom.InventorySorter
{
    using System.Collections.Generic;

    /// <summary>
    /// Configuration for the Inventory Sorter
    /// </summary>
    public class SorterConfiguration
    {
        // === Protection settings ===
        public bool RespectInventoryLock { get; set; } = true;
        public bool ProtectArmoryItems { get; set; } = true;
        public bool ProtectEnchantedItems { get; set; } = false;
        public bool ProtectSocketedItems { get; set; } = false;

        // === Sorting rules ===
        public bool SortByQualityFirst { get; set; } = true;
        public bool GroupSets { get; set; } = true;
        public bool GroupGemsByColor { get; set; } = true;
        public bool PrimalsFirst { get; set; } = true;
        public bool AncientsSeparate { get; set; } = true;

        // === Timing settings ===
        public int MoveDelayMs { get; set; } = 50;
        public int ClickDelayMs { get; set; } = 30;
        public int WaitAfterMoveMs { get; set; } = 20;

        // === UI settings ===
        public bool ShowHighlights { get; set; } = true;
        public bool ShowProgress { get; set; } = true;
        public bool ConfirmBeforeSort { get; set; } = false;
        public bool ShowZoneOverlay { get; set; } = true;

        // === Advanced ===
        public bool UseSmartPlacement { get; set; } = true;
        public bool OptimizeForSpace { get; set; } = false;
        public bool AllowCrossTabSort { get; set; } = false;
    }
}
