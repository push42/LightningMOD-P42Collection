namespace Turbo.Plugins.Custom.SmartEvade
{
    using SharpDX.DirectInput;
    using Turbo.Plugins.Default;

    /// <summary>
    /// Customizer for Smart Evade v3 - God-Tier Auto-Evade with Navigation Integration
    /// 
    /// NOTE: When using ROS Bot, set ExecuteMovement = false
    /// This makes SmartEvade "overlay only" - shows danger info but doesn't move
    /// ROS Bot handles all movement, we just provide visual awareness
    /// 
    /// PRIVATE VERSION - Use SmartEvadeLite for community sharing
    /// </summary>
    public class SmartEvadeCustomizer : BasePlugin, ICustomizer
    {
        public SmartEvadeCustomizer()
        {
            Enabled = true;
        }

        public void Customize()
        {
            Hud.RunOnPlugin<SmartEvadePlugin>(plugin =>
            {
                // ========================================
                // KEY BINDING
                // ========================================
                
                // Shift+J = Toggle evade on/off
                plugin.ToggleKey = Hud.Input.CreateKeyEvent(true, Key.J, false, false, true);

                // Start disabled (press Shift+J to enable)
                plugin.SetActive(false);


                // ========================================
                // ROS BOT COMPATIBILITY
                // ========================================
                
                // IMPORTANT: Disable our navigation since ROS Bot handles pathing
                plugin.UseNavigationSystem = false;  // Don't use our NavMesh (ROS has better one)
                plugin.UseNavMeshWalkability = false;
                plugin.UseAStarForEscape = false;
                plugin.UseMonsterPrediction = false;  // ROS handles this


                // ========================================
                // DISTANCE SETTINGS
                // ========================================
                
                plugin.SafeDistance = 15f;
                plugin.MinKiteDistance = 8f;
                plugin.MaxEnemyRange = 50f;
                plugin.DangerRadiusMultiplier = 1.3f;


                // ========================================
                // WALL AWARENESS SETTINGS
                // ========================================
                
                plugin.WallDetectionRange = 12f;
                plugin.WallAvoidanceWeight = 3.0f;
                plugin.MinWallClearance = 5f;


                // ========================================
                // MOVEMENT SETTINGS
                // ========================================
                
                plugin.EscapeDirections = 16;
                plugin.EscapeDistance = 10f;
                plugin.MovementSmoothing = 0.7f;
                plugin.ActionCooldownMs = 35;
                plugin.EnablePredictivePathing = false;  // ROS handles pathing


                // ========================================
                // COMBAT SETTINGS
                // ========================================
                
                plugin.PrioritizeGroundEffects = true;
                plugin.EvadeHealthThreshold = 100f;


                // ========================================
                // VISUAL SETTINGS - Minimal to avoid ROS Bot overlap
                // ========================================
                
                // Disable most visuals since ROS Bot shows pathing
                plugin.ShowDebugCircles = false;      // ROS shows danger zones
                plugin.ShowWallMarkers = false;       // ROS shows walls
                plugin.ShowEscapeRoutes = false;      // ROS shows paths
                plugin.ShowMovementIndicator = false;
                plugin.ShowSafeZones = false;
                plugin.ShowPredictedPaths = false;
                
                // Keep panel for status info
                plugin.ShowPanel = true;
                plugin.PanelX = 0.005f;
                plugin.PanelY = 0.28f;
            });
        }
    }
}
