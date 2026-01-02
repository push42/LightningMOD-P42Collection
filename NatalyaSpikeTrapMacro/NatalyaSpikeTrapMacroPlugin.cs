namespace Turbo.Plugins.Custom.NatalyaSpikeTrapMacro
{
    using System;
    using System.Linq;
    using System.Windows.Forms;
    using SharpDX.DirectInput;
    using Turbo.Plugins.Default;

    /// <summary>
    /// Natalya Spike Trap Demon Hunter Macro
    /// 
    /// Build: https://maxroll.gg/d3/guides/natalya-spike-trap-demon-hunter-guide
    /// 
    /// IMPORTANT: This macro only activates when Spike Trap is equipped!
    /// It will NOT show on GoD Strafe or other DH builds.
    /// 
    /// Skills:
    /// - Left Click: Evasive Fire (Hardened) - Detonates traps
    /// - Right Click: Spike Trap (Custom Trigger) - Main damage
    /// - 1: Caltrops (Bait the Trap) - Pull enemies
    /// - 2: Vengeance (Dark Heart) - Buff
    /// - 3: Smoke Screen (Healing Vapors) - Defense
    /// - 4: Shadow Power (Gloom) - Defense
    /// 
    /// F1 = Toggle macro ON/OFF
    /// F2 = Switch between PULL mode and DAMAGE mode
    /// 
    /// PULL MODE: 2x Spike Trap + Caltrops + Evasive Fire (pulls enemies together)
    /// DAMAGE MODE: 5x Spike Trap + Evasive Fire (maximum chain reaction damage)
    /// 
    /// When no enemies nearby: Uses Force Move to keep moving toward cursor
    /// </summary>
    public class NatalyaSpikeTrapMacroPlugin : BasePlugin, IInGameTopPainter, IKeyEventHandler, IAfterCollectHandler
    {
        #region Settings

        public IKeyEvent ToggleKeyEvent { get; set; }
        public IKeyEvent ModeKeyEvent { get; set; }
        public bool IsHideTip { get; set; } = false;

        /// <summary>
        /// Number of Spike Traps to place in PULL mode (1-2 recommended)
        /// </summary>
        public int PullModeTraps { get; set; } = 2;

        /// <summary>
        /// Number of Spike Traps to place in DAMAGE mode (5 for optimal chain reaction)
        /// </summary>
        public int DamageModeTraps { get; set; } = 5;

        /// <summary>
        /// Delay between Spike Trap placements (ms)
        /// </summary>
        public int TrapPlacementDelay { get; set; } = 30;

        /// <summary>
        /// Delay after placing all traps before detonating (ms)
        /// </summary>
        public int DetonationDelay { get; set; } = 50;

        /// <summary>
        /// Delay between force movement actions (ms)
        /// </summary>
        public int MovementDelay { get; set; } = 100;

        /// <summary>
        /// Buff refresh threshold for Vengeance (seconds remaining)
        /// </summary>
        public float VengeanceRefreshTime { get; set; } = 3.0f;

        /// <summary>
        /// Buff refresh threshold for Shadow Power (seconds remaining)
        /// </summary>
        public float ShadowPowerRefreshTime { get; set; } = 2.0f;

        /// <summary>
        /// Range to detect enemies and engage combat
        /// </summary>
        public float EnemyDetectionRange { get; set; } = 50f;

        /// <summary>
        /// Minimum number of enemies to trigger combat (0 = engage single targets)
        /// </summary>
        public int MinEnemiesForCombat { get; set; } = 1;

        /// <summary>
        /// Enable automatic force movement when no enemies nearby
        /// </summary>
        public bool EnableAutoMovement { get; set; } = true;

        #endregion

        #region Private Fields

        public bool Running { get; private set; }
        public bool IsDamageMode { get; private set; } = true; // Default to damage mode
        private bool _isDemonHunter = false;
        private bool _hasNatalyaSet = false;
        private bool _hasSpikeTrapEquipped = false;  // Track if Spike Trap is equipped
        private bool _isInCombat = false;  // Track combat state

        // Skill references
        private IPlayerSkill _skillEvasiveFire;   // Left click - detonator
        private IPlayerSkill _skillSpikeTrap;     // Right click - main damage
        private IPlayerSkill _skillCaltrops;      // Slot 1 - pull
        private IPlayerSkill _skillVengeance;     // Slot 2 - buff
        private IPlayerSkill _skillSmokeScreen;   // Slot 3 - defense
        private IPlayerSkill _skillShadowPower;   // Slot 4 - defense

        // Timers
        private IWatch _actionTimer;
        private IWatch _trapTimer;
        private IWatch _cycleTimer;
        private IWatch _movementTimer;
        private IWatch _combatExitTimer;  // Delay before exiting combat mode

        // State
        private int _trapsPlaced = 0;
        private MacroPhase _phase = MacroPhase.Idle;
        private int _nearbyEnemyCount = 0;

        // Fonts
        private IFont _titleFont;
        private IFont _runningFont;
        private IFont _modeFont;
        private IFont _stoppedFont;
        private IFont _tipFont;
        private IFont _movingFont;

        // UI
        private IUiElement _chatUI;
        private IBrush _panelBrush;
        private IBrush _borderBrush;
        private IBrush _accentOnBrush;
        private IBrush _accentOffBrush;

        // Combat exit delay (ms) - prevents flickering in/out of combat
        private const int CombatExitDelay = 500;

        #endregion

        public NatalyaSpikeTrapMacroPlugin()
        {
            Enabled = true;
            Order = 50003;
        }

        public override void Load(IController hud)
        {
            base.Load(hud);

            ToggleKeyEvent = Hud.Input.CreateKeyEvent(true, Key.F1, false, false, false);
            ModeKeyEvent = Hud.Input.CreateKeyEvent(true, Key.F2, false, false, false);

            _actionTimer = Hud.Time.CreateWatch();
            _trapTimer = Hud.Time.CreateWatch();
            _cycleTimer = Hud.Time.CreateWatch();
            _movementTimer = Hud.Time.CreateWatch();
            _combatExitTimer = Hud.Time.CreateWatch();

            // UI Fonts
            _titleFont = Hud.Render.CreateFont("tahoma", 8, 255, 220, 180, 100, true, false, 180, 0, 0, 0, true);
            _runningFont = Hud.Render.CreateFont("tahoma", 7.5f, 255, 0, 255, 0, true, false, 160, 0, 0, 0, true);
            _modeFont = Hud.Render.CreateFont("tahoma", 7.5f, 255, 255, 200, 0, true, false, 160, 0, 0, 0, true);
            _stoppedFont = Hud.Render.CreateFont("tahoma", 7.5f, 255, 255, 100, 100, true, false, 160, 0, 0, 0, true);
            _tipFont = Hud.Render.CreateFont("tahoma", 7, 200, 180, 180, 180, false, false, 140, 0, 0, 0, true);
            _movingFont = Hud.Render.CreateFont("tahoma", 7.5f, 255, 100, 180, 255, true, false, 160, 0, 0, 0, true);

            // Panel styling (matches other plugins)
            _panelBrush = Hud.Render.CreateBrush(235, 15, 15, 25, 0);
            _borderBrush = Hud.Render.CreateBrush(200, 60, 60, 80, 1f);
            _accentOnBrush = Hud.Render.CreateBrush(255, 80, 200, 80, 0);
            _accentOffBrush = Hud.Render.CreateBrush(255, 200, 80, 80, 0);

            _chatUI = Hud.Render.RegisterUiElement("Root.NormalLayer.chatentry_dialog_backgroundScreen.chatentry_content.chat_editline", null, null);
        }

        public void OnKeyEvent(IKeyEvent keyEvent)
        {
            if (!Hud.Game.IsInGame) return;
            if (!_isDemonHunter) return;
            if (!_hasSpikeTrapEquipped) return;
            if (Hud.Inventory.InventoryMainUiElement.Visible) return;

            // F1 - Toggle ON/OFF
            if (ToggleKeyEvent.Matches(keyEvent) && keyEvent.IsPressed)
            {
                if (Running)
                {
                    StopMacro();
                }
                else
                {
                    StartMacro();
                }
            }

            // F2 - Switch mode (only when running)
            if (ModeKeyEvent.Matches(keyEvent) && keyEvent.IsPressed)
            {
                if (Running)
                {
                    IsDamageMode = !IsDamageMode;
                    _phase = MacroPhase.PlacingTraps;
                    _trapsPlaced = 0;
                }
            }
        }

        public void AfterCollect()
        {
            if (!Hud.Game.IsInGame) return;

            // Check if player is Demon Hunter
            _isDemonHunter = Hud.Game.Me.HeroClassDefinition.HeroClass == HeroClass.DemonHunter;
            if (!_isDemonHunter)
            {
                if (Running) StopMacro();
                _hasSpikeTrapEquipped = false;
                return;
            }

            // Find skills and check for Natalya set
            FindSkills();
            CheckNatalyaSet();

            // Check if Spike Trap is equipped - if not, stop macro and don't process
            _hasSpikeTrapEquipped = _skillSpikeTrap != null;
            if (!_hasSpikeTrapEquipped)
            {
                if (Running) StopMacro();
                return;
            }

            if (!Running) return;

            // Safety checks
            if (ShouldPauseMacro())
            {
                return;
            }

            // Count nearby enemies
            _nearbyEnemyCount = Hud.Game.AliveMonsters.Count(m => m.CentralXyDistanceToMe <= EnemyDetectionRange);

            // Update combat state with hysteresis to prevent flickering
            UpdateCombatState();

            // Run the macro
            ProcessMacro();
        }

        private void UpdateCombatState()
        {
            bool hasEnoughEnemies = _nearbyEnemyCount >= MinEnemiesForCombat;

            if (hasEnoughEnemies)
            {
                // Enter combat immediately when enemies detected
                _isInCombat = true;
                _combatExitTimer.Restart();
            }
            else if (_isInCombat)
            {
                // Delay exiting combat to prevent flickering
                if (_combatExitTimer.ElapsedMilliseconds >= CombatExitDelay)
                {
                    _isInCombat = false;
                    // Reset combat state
                    _phase = MacroPhase.Idle;
                    _trapsPlaced = 0;
                }
            }
        }

        private void FindSkills()
        {
            _skillEvasiveFire = null;
            _skillSpikeTrap = null;
            _skillCaltrops = null;
            _skillVengeance = null;
            _skillSmokeScreen = null;
            _skillShadowPower = null;

            foreach (var skill in Hud.Game.Me.Powers.UsedSkills)
            {
                var sno = skill.SnoPower.Sno;
                
                if (sno == Hud.Sno.SnoPowers.DemonHunter_EvasiveFire.Sno)
                    _skillEvasiveFire = skill;
                else if (sno == Hud.Sno.SnoPowers.DemonHunter_SpikeTrap.Sno)
                    _skillSpikeTrap = skill;
                else if (sno == Hud.Sno.SnoPowers.DemonHunter_Caltrops.Sno)
                    _skillCaltrops = skill;
                else if (sno == Hud.Sno.SnoPowers.DemonHunter_Vengeance.Sno)
                    _skillVengeance = skill;
                else if (sno == Hud.Sno.SnoPowers.DemonHunter_SmokeScreen.Sno)
                    _skillSmokeScreen = skill;
                else if (sno == Hud.Sno.SnoPowers.DemonHunter_ShadowPower.Sno)
                    _skillShadowPower = skill;
            }
        }

        private void CheckNatalyaSet()
        {
            // Check for Natalya's Vengeance set (6 piece bonus)
            _hasNatalyaSet = Hud.Game.Me.GetSetItemCount(847985) >= 4; // Natalya set ID
        }

        private bool ShouldPauseMacro()
        {
            return Hud.Game.IsLoading
                || Hud.Game.IsPaused
                || Hud.Game.IsInTown
                || !Hud.Window.IsForeground
                || !Hud.Render.MinimapUiElement.Visible
                || Hud.Render.WorldMapUiElement.Visible
                || (_chatUI != null && _chatUI.Visible)
                || Hud.Game.Me.IsDead
                || Hud.Game.Me.AnimationState == AcdAnimationState.CastingPortal
                || Hud.Game.Me.AnimationState == AcdAnimationState.Transform
                || Hud.Render.IsAnyBlockingUiElementVisible
                || !Hud.Window.CursorInsideGameWindow()
                || Hud.Game.Me.Powers.BuffIsActive(Hud.Sno.SnoPowers.Generic_IdentifyAllWithCast.Sno)
                || Hud.Game.Me.Powers.BuffIsActive(Hud.Sno.SnoPowers.Generic_ActorGhostedBuff.Sno);
        }

        private void ProcessMacro()
        {
            // 1. ALWAYS keep buffs up (highest priority, even when moving)
            RefreshBuffs();

            // 2. Determine action based on combat state
            if (_isInCombat)
            {
                // Execute combat rotation
                ProcessCombat();
            }
            else
            {
                // No enemies - use force movement
                ProcessMovement();
            }
        }

        private void ProcessMovement()
        {
            if (!EnableAutoMovement) return;

            // Throttle movement commands
            if (_movementTimer.IsRunning && _movementTimer.ElapsedMilliseconds < MovementDelay)
                return;

            // Use force movement (ActionKey.Move) - moves toward cursor position
            Hud.Interaction.DoAction(ActionKey.Move);
            _movementTimer.Restart();
        }

        private void ProcessCombat()
        {
            // Execute combat rotation based on phase
            switch (_phase)
            {
                case MacroPhase.Idle:
                case MacroPhase.PlacingTraps:
                    ProcessPlacingTraps();
                    break;

                case MacroPhase.PlacingCaltrops:
                    ProcessPlacingCaltrops();
                    break;

                case MacroPhase.Detonating:
                    ProcessDetonating();
                    break;
            }
        }

        private void RefreshBuffs()
        {
            // Vengeance - keep up at all times
            if (_skillVengeance != null && !_skillVengeance.IsOnCooldown)
            {
                double buffTime = _skillVengeance.BuffIsActive ? _skillVengeance.RemainingBuffTime() : 0;
                if (buffTime < VengeanceRefreshTime)
                {
                    Hud.Interaction.DoAction(_skillVengeance.Key);
                }
            }

            // Shadow Power - keep up when fighting or moving
            if (_skillShadowPower != null && !_skillShadowPower.IsOnCooldown)
            {
                double buffTime = _skillShadowPower.BuffIsActive ? _skillShadowPower.RemainingBuffTime() : 0;
                if (buffTime < ShadowPowerRefreshTime)
                {
                    Hud.Interaction.DoAction(_skillShadowPower.Key);
                }
            }

            // Smoke Screen - use when taking damage (emergency defense)
            if (_skillSmokeScreen != null && !_skillSmokeScreen.IsOnCooldown)
            {
                if (Hud.Game.Me.Defense.HealthPct < 0.7)
                {
                    Hud.Interaction.DoAction(_skillSmokeScreen.Key);
                }
            }
        }

        private void ProcessPlacingTraps()
        {
            if (_skillSpikeTrap == null) return;

            int targetTraps = IsDamageMode ? DamageModeTraps : PullModeTraps;

            if (_trapTimer.ElapsedMilliseconds < TrapPlacementDelay && _trapTimer.IsRunning)
                return;

            if (_trapsPlaced < targetTraps)
            {
                // Place Spike Trap
                Hud.Interaction.DoAction(_skillSpikeTrap.Key);
                _trapsPlaced++;
                _trapTimer.Restart();
            }
            else
            {
                // All traps placed, move to next phase
                if (IsDamageMode)
                {
                    // In damage mode, go straight to detonation
                    _phase = MacroPhase.Detonating;
                }
                else
                {
                    // In pull mode, place Caltrops first
                    _phase = MacroPhase.PlacingCaltrops;
                }
                _actionTimer.Restart();
            }
        }

        private void ProcessPlacingCaltrops()
        {
            if (_skillCaltrops == null)
            {
                _phase = MacroPhase.Detonating;
                _actionTimer.Restart();
                return;
            }

            if (_actionTimer.ElapsedMilliseconds < DetonationDelay) return;

            // Place Caltrops for pull effect
            Hud.Interaction.DoAction(_skillCaltrops.Key);
            
            _phase = MacroPhase.Detonating;
            _actionTimer.Restart();
        }

        private void ProcessDetonating()
        {
            if (_skillEvasiveFire == null) return;

            if (_actionTimer.ElapsedMilliseconds < DetonationDelay) return;

            // Detonate with Evasive Fire
            Hud.Interaction.DoAction(_skillEvasiveFire.Key);

            // In pull mode, also place Caltrops simultaneously for pull effect
            if (!IsDamageMode && _skillCaltrops != null)
            {
                Hud.Interaction.DoAction(_skillCaltrops.Key);
            }

            // Reset for next cycle
            _phase = MacroPhase.PlacingTraps;
            _trapsPlaced = 0;
            _cycleTimer.Restart();
        }

        private void StartMacro()
        {
            // Double-check Spike Trap is equipped before starting
            if (!_hasSpikeTrapEquipped) return;
            
            Running = true;
            _phase = MacroPhase.Idle;  // Start in idle, will transition based on enemies
            _trapsPlaced = 0;
            _isInCombat = false;
            _actionTimer.Restart();
            _trapTimer.Restart();
            _cycleTimer.Restart();
            _movementTimer.Restart();
            _combatExitTimer.Restart();
        }

        private void StopMacro()
        {
            Running = false;
            _phase = MacroPhase.Idle;
            _trapsPlaced = 0;
            _isInCombat = false;
            _actionTimer.Stop();
            _trapTimer.Stop();
            _movementTimer.Stop();
        }

        public void PaintTopInGame(ClipState clipState)
        {
            if (clipState != ClipState.AfterClip) return;
            if (!Hud.Game.IsInGame) return;
            if (!_isDemonHunter) return;
            
            // Only show UI if Spike Trap is equipped
            if (!_hasSpikeTrapEquipped) return;

            if (Hud.Inventory.InventoryMainUiElement.Visible && Running)
            {
                StopMacro();
            }

            if (IsHideTip && !Running) return;

            DrawStatusPanel();
        }

        private void DrawStatusPanel()
        {
            // Get player screen position for centered display below character
            var playerScreenPos = Hud.Game.Me.FloorCoordinate.ToScreenCoordinate();
            float centerX = playerScreenPos.X;
            float baseY = playerScreenPos.Y + 10; // Offset below character

            if (Running)
            {
                // Title
                var layout1 = _titleFont.GetTextLayout("N6 Spike Trap");
                _titleFont.DrawText(layout1, centerX - layout1.Metrics.Width / 2, baseY);
                baseY += layout1.Metrics.Height + 2;

                // Combat state indicator
                if (_isInCombat)
                {
                    var layout2 = _runningFont.GetTextLayout($"● COMBAT ({_nearbyEnemyCount})");
                    _runningFont.DrawText(layout2, centerX - layout2.Metrics.Width / 2, baseY);
                    baseY += layout2.Metrics.Height + 2;

                    // Mode indicator
                    string modeText = IsDamageMode ? "DAMAGE (5 traps)" : "PULL (2 traps)";
                    var modeLayout = _modeFont.GetTextLayout(modeText);
                    _modeFont.DrawText(modeLayout, centerX - modeLayout.Metrics.Width / 2, baseY);
                }
                else
                {
                    var layout2 = _movingFont.GetTextLayout("● MOVING");
                    _movingFont.DrawText(layout2, centerX - layout2.Metrics.Width / 2, baseY);
                    baseY += layout2.Metrics.Height + 2;

                    // Show mode for reference
                    string modeText = IsDamageMode ? "[F2] DAMAGE" : "[F2] PULL";
                    var modeLayout = _tipFont.GetTextLayout(modeText);
                    _tipFont.DrawText(modeLayout, centerX - modeLayout.Metrics.Width / 2, baseY);
                }
            }
            else
            {
                // OFF state - minimal display
                var layout1 = _titleFont.GetTextLayout("N6 Spike Trap");
                _titleFont.DrawText(layout1, centerX - layout1.Metrics.Width / 2, baseY);
                baseY += layout1.Metrics.Height + 2;

                var layout2 = _stoppedFont.GetTextLayout("OFF [F1]");
                _stoppedFont.DrawText(layout2, centerX - layout2.Metrics.Width / 2, baseY);
            }
        }
    }

    internal enum MacroPhase
    {
        Idle,
        PlacingTraps,
        PlacingCaltrops,
        Detonating
    }
}
