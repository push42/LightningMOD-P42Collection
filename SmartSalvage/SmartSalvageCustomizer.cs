namespace Turbo.Plugins.Custom.SmartSalvage
{
    using SharpDX.DirectInput;
    using Turbo.Plugins.Default;

    /// <summary>
    /// Customizer for Smart Salvage Plugin v3.0
    /// Configure keybindings, global rules, and custom blacklists
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
                
                // Key to quick toggle all profiles (Ctrl+U)
                plugin.QuickToggleKey = Hud.Input.CreateKeyEvent(true, Key.U, true, false, false);


                // ========================================
                // GLOBAL RULES - Always Keep
                // ========================================
                
                var rules = plugin.RulesMgr.GlobalRules;
                
                // Always keep Primal items (default: true)
                rules.AlwaysKeepPrimals = true;
                
                // Always keep Ancient items (default: false)
                rules.AlwaysKeepAncients = false;
                
                // Always keep Set items (default: false)
                rules.AlwaysKeepSetItems = false;
                
                // Always keep items with high perfection (default: false)
                rules.AlwaysKeepHighPerfection = false;
                rules.HighPerfectionThreshold = 95.0;  // 95% or higher


                // ========================================
                // GLOBAL RULES - Protection
                // ========================================
                
                // Protect items with gems socketed (default: true)
                rules.ProtectSocketedItems = true;
                
                // Protect enchanted items (default: true)
                rules.ProtectEnchantedItems = true;
                
                // Protect items in Armory sets (default: true)
                rules.ProtectArmoryItems = true;
                
                // Protect items in locked inventory slots (default: true)
                rules.ProtectLockedSlots = true;


                // ========================================
                // STAT-BASED RULES
                // ========================================
                
                // Add rules to keep items only if they meet stat requirements
                // Example: Keep Dawn only if CDR >= 8%
                /*
                var dawnRule = new StatRule("Dawn");
                dawnRule.Conditions.Add(new StatCondition(StatType.CooldownReduction, CompareOp.GreaterOrEqual, 8.0));
                dawnRule.IsEnabled = true;
                plugin.RulesMgr.AddRule(dawnRule);
                */
                
                // Example: Keep Convention of Elements only if it has both CHC and CHD
                /*
                var coeRule = new StatRule("Convention of Elements");
                coeRule.Conditions.Add(new StatCondition(StatType.CritChance, CompareOp.GreaterOrEqual, 5.0));
                coeRule.Conditions.Add(new StatCondition(StatType.CritDamage, CompareOp.GreaterOrEqual, 45.0));
                coeRule.Logic = RuleLogic.And;  // Both conditions must be met
                coeRule.IsEnabled = true;
                plugin.RulesMgr.AddRule(coeRule);
                */


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
