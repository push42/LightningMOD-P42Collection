namespace Turbo.Plugins.Custom.KadalaAutoBuy
{
    using SharpDX.DirectInput;
    using Turbo.Plugins.Default;

    /// <summary>
    /// Customizer for Kadala Auto-Buy Plugin v4.0
    /// 
    /// No more manual position calibration needed!
    /// Just select the item type you want to buy.
    /// 
    /// Available item types:
    /// - Weapons: OneHandWeapon, TwoHandWeapon
    /// - Off-hand: Quiver, Orb, Mojo, Phylactery
    /// - Armor: Helm, Gloves, Boots, ChestArmor, Belt, Shoulders, Pants, Bracers
    /// - Jewelry: Ring, Amulet
    /// - Other: Shield
    /// </summary>
    public class KadalaAutoBuyCustomizer : BasePlugin, ICustomizer
    {
        public KadalaAutoBuyCustomizer()
        {
            Enabled = true;
        }

        public void Customize()
        {
            Hud.RunOnPlugin<KadalaAutoBuyPlugin>(plugin =>
            {
                // ========================================
                // KEY BINDINGS
                // ========================================
                
                // Shift+K = Toggle auto-buy on/off
                plugin.ToggleKey = Hud.Input.CreateKeyEvent(true, Key.K, false, false, true);
                
                // Ctrl+K = Cycle through item types
                plugin.CycleItemKey = Hud.Input.CreateKeyEvent(true, Key.K, true, false, false);


                // ========================================
                // ITEM SELECTION
                // ========================================
                
                // Select which item type to auto-buy from Kadala
                // The plugin will automatically switch to the correct tab!
                
                // Popular choices:
                plugin.SelectedItem = KadalaAutoBuyPlugin.KadalaItemType.Ring;  // 50 shards
                // plugin.SelectedItem = KadalaAutoBuyPlugin.KadalaItemType.Amulet;  // 100 shards
                // plugin.SelectedItem = KadalaAutoBuyPlugin.KadalaItemType.Gloves;  // 25 shards
                // plugin.SelectedItem = KadalaAutoBuyPlugin.KadalaItemType.OneHandWeapon;  // 75 shards


                // ========================================
                // AUTO-BUY SETTINGS
                // ========================================
                
                plugin.AutoBuyEnabled = true;
                plugin.MinBloodShardsToStart = 100;  // Don't start buying until you have this many
                plugin.StopAtBloodShards = 0;        // Stop at this amount (0 = spend all)
                plugin.BuyIntervalMs = 50;           // Milliseconds between purchases
                plugin.InitialDelayMs = 300;         // Delay after opening Kadala before buying
                
                // Show debug overlay (slot rectangles and tab states)
                plugin.ShowDebugOverlay = false;
            });
        }
    }
}
