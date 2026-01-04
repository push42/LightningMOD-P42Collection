namespace Turbo.Plugins.Custom.AutoMaster
{
    using SharpDX.DirectInput;
    using Turbo.Plugins.Default;

    /// <summary>
    /// Customizer for AutoMaster Plugin
    /// Modify settings here to personalize auto-pickup and auto-interact behavior
    /// </summary>
    public class AutoMasterCustomizer : BasePlugin, ICustomizer
    {
        public AutoMasterCustomizer()
        {
            Enabled = true;
        }

        public void Customize()
        {
            Hud.RunOnPlugin<AutoMasterPlugin>(plugin =>
            {
                // ========================================
                // MASTER TOGGLE
                // ========================================
                
                // Start with plugin active (true) or inactive (false)
                plugin.IsActive = true;
                
                // Key to toggle on/off (H key)
                plugin.ToggleKey = Hud.Input.CreateKeyEvent(true, Key.H, false, false, false);


                // ========================================
                // PICKUP SETTINGS
                // ========================================
                
                // High-value items - ALWAYS PICKUP
                plugin.PickupPrimal = true;
                plugin.PickupAncient = true;
                plugin.PickupLegendary = true;
                plugin.PickupSet = true;
                
                // Gems and materials
                plugin.PickupGems = true;
                plugin.PickupCraftingMaterials = true;
                plugin.PickupDeathsBreath = true;
                plugin.PickupForgottenSoul = true;
                plugin.PickupRiftKeys = true;
                plugin.PickupRamaladni = true;
                plugin.PickupPotions = true;
                
                // Lower tier items (set to false for speed farming)
                plugin.PickupRare = false;
                plugin.PickupMagic = false;
                plugin.PickupWhite = false;
                
                // Gold (usually handled by in-game pickup)
                plugin.PickupGold = false;


                // ========================================
                // INTERACT SETTINGS
                // ========================================
                
                // Shrines and Pylons
                plugin.InteractShrines = true;
                plugin.InteractPylons = true;
                plugin.InteractPylonsInGR = true;
                plugin.GRLevelForAutoPylon = 100; // Auto-click pylons up to this GR level
                
                // Chests
                plugin.InteractChests = true;
                plugin.InteractNormalChests = true;
                plugin.InteractResplendentChests = true;
                
                // Doors
                plugin.InteractDoors = true;
                
                // Experience and healing
                plugin.InteractPoolOfReflection = true;
                plugin.InteractHealingWells = true;
                
                // Loot objects (trigger Harrington Waistguard)
                plugin.InteractDeadBodies = true;
                plugin.InteractWeaponRacks = true;
                plugin.InteractArmorRacks = true;
                plugin.InteractClickables = true;


                // ========================================
                // RANGE SETTINGS
                // ========================================
                
                // How far to reach for items (in yards)
                plugin.PickupRange = 15.0;
                
                // How far to reach for interactions (in yards)
                plugin.InteractRange = 12.0;


                // ========================================
                // UI SETTINGS
                // ========================================
                
                // Show status panel on screen
                plugin.ShowStatusPanel = true;
                
                // Panel position (percentage of screen)
                plugin.PanelX = 0.005f;  // 0.5% from left
                plugin.PanelY = 0.42f;   // 42% from top (below SmartEvade at 0.35)
            });
        }
    }
}
