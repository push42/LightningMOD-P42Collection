namespace Turbo.Plugins.Custom.SmartSalvage
{
    using SharpDX.DirectInput;
    using Turbo.Plugins.Default;

    /// <summary>
    /// Customizer for Smart Salvage Plugin
    /// Configure keybindings, salvage behavior, and custom blacklists
    /// </summary>
    public class SmartSalvageCustomizer : BasePlugin, ICustomizer
    {
        public SmartSalvageCustomizer()
        {
            Enabled = true;
        }

        public void Customize()
        {
            Hud.RunOnPlugin<SmartSalvagePlugin>(plugin =>
            {
                // ========================================
                // KEY BINDINGS
                // ========================================
                
                // Key to start/stop auto-salvage
                plugin.SalvageKey = Hud.Input.CreateKeyEvent(true, Key.U, false, false, false);
                
                // Key to open/close profile manager (Shift+U)
                plugin.ManagerKey = Hud.Input.CreateKeyEvent(true, Key.U, false, false, true);


                // ========================================
                // SALVAGE BEHAVIOR
                // ========================================
                
                // Auto repair before salvaging
                plugin.AutoRepair = true;

                // Ancient items: 0=smart, 1=never salvage (default), 2=always salvage
                plugin.SalvageAncient = 1;

                // Primal items: 0=smart, 1=never salvage (default), 2=always salvage
                plugin.SalvagePrimal = 1;


                // ========================================
                // CUSTOM ITEM PROTECTION
                // ========================================
                
                // Add custom items that should NEVER be salvaged
                // These are added on top of any profile settings
                
                // Example: Add items by exact name (English)
                // plugin.AddToBlacklist("Wand of Woh");
                // plugin.AddToBlacklist("Deathwish");
                
                // Example: Add multiple items at once
                // plugin.AddToBlacklist("Item1", "Item2", "Item3");


                // ========================================
                // PROFILE MANAGEMENT
                // ========================================
                
                // Enable/disable specific built-in profiles programmatically
                // Profile IDs: Universal, NecroLoD, DHGoD, WDMundu, BarbWW, 
                //              CrusAkkhan, MonkInna, WizFirebird, Crafted, Gems
                
                // Example: Disable a specific build profile
                // plugin.SetBuildEnabled("BarbWW", false);
                
                // Example: Enable all class profiles
                // plugin.SetBuildEnabled("NecroLoD", true);
                // plugin.SetBuildEnabled("DHGoD", true);
                // plugin.SetBuildEnabled("WDMundu", true);


                // ========================================
                // ADVANCED: Direct BlacklistManager Access
                // ========================================
                
                // You can access the BlacklistManager directly for advanced operations
                // var mgr = plugin.BlacklistMgr;
                
                // Create a custom profile programmatically
                // var myProfile = new BlacklistProfile("My Custom Build", true);
                // myProfile.AddItems("Item1", "Item2", "Item3");
                // mgr.AddProfile(myProfile);
            });
        }
    }
}
