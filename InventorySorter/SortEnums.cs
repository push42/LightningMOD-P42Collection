namespace Turbo.Plugins.Custom.InventorySorter
{
    /// <summary>
    /// Sorting mode enumeration for different sorting strategies.
    /// </summary>
    public enum SortMode
    {
        /// <summary>
        /// Sort by item category first, then by quality, then alphabetically.
        /// Best for organized stash management.
        /// </summary>
        ByCategory,

        /// <summary>
        /// Sort by item quality (Primal > Ancient > Legendary > Set > Rare > Magic > Normal).
        /// Best for quickly finding best items.
        /// </summary>
        ByQuality,

        /// <summary>
        /// Sort by item type (Weapons, Armor, Jewelry, etc.).
        /// Best for gear comparison.
        /// </summary>
        ByType,

        /// <summary>
        /// Sort by item size to optimize space usage (larger items first).
        /// Best for maximizing storage efficiency.
        /// </summary>
        BySize,

        /// <summary>
        /// Sort by item name alphabetically.
        /// Best for finding specific items quickly.
        /// </summary>
        Alphabetical,

        /// <summary>
        /// Custom sorting using user-defined rules.
        /// </summary>
        Custom
    }

    /// <summary>
    /// Sort direction enumeration.
    /// </summary>
    public enum SortDirection
    {
        Ascending,
        Descending
    }

    /// <summary>
    /// Target container for sorting operations.
    /// </summary>
    public enum SortTarget
    {
        /// <summary>
        /// Sort only the player inventory.
        /// </summary>
        Inventory,

        /// <summary>
        /// Sort only the current stash tab.
        /// </summary>
        CurrentStashTab,

        /// <summary>
        /// Sort all stash tabs.
        /// </summary>
        AllStashTabs,

        /// <summary>
        /// Sort both inventory and current stash tab.
        /// </summary>
        InventoryAndCurrentStash,

        /// <summary>
        /// Sort everything (inventory + all stash tabs).
        /// </summary>
        All
    }

    /// <summary>
    /// State of the sorting operation.
    /// </summary>
    public enum SortState
    {
        /// <summary>
        /// Not currently sorting.
        /// </summary>
        Idle,

        /// <summary>
        /// Analyzing items and planning sort.
        /// </summary>
        Analyzing,

        /// <summary>
        /// Picking up items to temporary storage.
        /// </summary>
        PickingUp,

        /// <summary>
        /// Placing items in sorted order.
        /// </summary>
        Placing,

        /// <summary>
        /// Moving items within container.
        /// </summary>
        Moving,

        /// <summary>
        /// Switching stash tabs.
        /// </summary>
        SwitchingTab,

        /// <summary>
        /// Waiting for game to update.
        /// </summary>
        WaitingForUpdate,

        /// <summary>
        /// Sort complete.
        /// </summary>
        Complete,

        /// <summary>
        /// Sort was cancelled.
        /// </summary>
        Cancelled,

        /// <summary>
        /// Sort encountered an error.
        /// </summary>
        Error,

        /// <summary>
        /// Paused - waiting for user input.
        /// </summary>
        Paused
    }
}
