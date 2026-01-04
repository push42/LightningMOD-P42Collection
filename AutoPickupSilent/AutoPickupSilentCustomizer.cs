namespace Turbo.Plugins.Custom.AutoPickupSilent
{
    using SharpDX.DirectInput;
    using Turbo.Plugins.Default;

    /// <summary>
    /// Customizer for Silent Auto Pickup - AGGRESSIVE MODE
    /// Optimized for speed builds (GoD DH, WW Barb, etc.)
    /// </summary>
    public class AutoPickupSilentCustomizer : BasePlugin, ICustomizer
    {
        public AutoPickupSilentCustomizer()
        {
            Enabled = true;
        }

        public void Customize()
        {
            // Disable the old AutoMaster plugin
            Hud.RunOnPlugin<AutoMaster.AutoMasterPlugin>(plugin =>
            {
                plugin.Enabled = false;
            });

            Hud.RunOnPlugin<AutoPickupSilentPlugin>(plugin =>
            {
                // KEY BINDING
                plugin.ToggleKey = Hud.Input.CreateKeyEvent(true, Key.H, false, false, false);
                plugin.SetActive(true);

                // === PICKUP SETTINGS ===
                plugin.PickupLegendary = true;
                plugin.PickupAncient = true;
                plugin.PickupPrimal = true;
                plugin.PickupSet = true;
                plugin.PickupGems = true;
                plugin.PickupCraftingMaterials = true;
                plugin.PickupDeathsBreath = true;
                plugin.PickupForgottenSoul = true;
                plugin.PickupRiftKeys = true;
                plugin.PickupBloodShards = true;
                plugin.PickupPotions = true;
                plugin.PickupRamaladni = true;
                plugin.PickupGold = false;   // Skip for speed
                plugin.PickupRare = false;   // Skip for speed
                plugin.PickupMagic = false;
                plugin.PickupWhite = false;

                // === INTERACT SETTINGS ===
                plugin.InteractShrines = true;
                plugin.InteractPylons = true;
                plugin.InteractPylonsInGR = true;
                plugin.GRLevelForAutoPylon = 100;
                plugin.InteractChests = true;
                plugin.InteractNormalChests = true;
                plugin.InteractResplendentChests = true;
                plugin.InteractDoors = true;
                plugin.InteractPoolOfReflection = true;
                plugin.InteractHealingWells = true;
                plugin.InteractDeadBodies = false;   // Skip for speed
                plugin.InteractWeaponRacks = false;  // Skip for speed
                plugin.InteractArmorRacks = false;   // Skip for speed
                plugin.InteractClickables = false;   // Skip for speed

                // === RANGE SETTINGS (LARGER for speed) ===
                plugin.PickupRange = 18.0;    // Extended range
                plugin.InteractRange = 15.0;  // Extended range

                // === AGGRESSIVE SETTINGS ===
                plugin.PickupsPerCycle = 5;   // Pick up to 5 items per frame!
                plugin.RetryPickups = true;   // Retry failed pickups

                // === UI SETTINGS ===
                plugin.ShowStatusPanel = true;
                plugin.PanelX = 0.005f;
                plugin.PanelY = 0.63f;
            });
        }
    }
}
