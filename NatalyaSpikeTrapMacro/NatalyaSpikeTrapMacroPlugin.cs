namespace Turbo.Plugins.Custom.NatalyaSpikeTrapMacro
{
    using System;
    using System.Linq;
    using System.Windows.Forms;
    using SharpDX.DirectInput;
    using Turbo.Plugins.Default;

    /// <summary>
    /// Natalya Spike Trap Demon Hunter Macro - Optimized Version
    /// 
    /// Build: https://maxroll.gg/d3/guides/natalya-spike-trap-demon-hunter-guide
    /// 
    /// IMPORTANT: This macro only activates when Spike Trap is equipped!
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
    /// PULL MODE: Caltrops FIRST → Wait → Traps on pulled enemies → Detonate
    /// DAMAGE MODE: Traps → Wait for settle → Detonate
    /// </summary>
    public class NatalyaSpikeTrapMacroPlugin : BasePlugin, IInGameTopPainter, IKeyEventHandler, IAfterCollectHandler
    {
        #region Settings

        public IKeyEvent ToggleKeyEvent { get; set; }
        public IKeyEvent ModeKeyEvent { get; set; }
        public bool IsHideTip { get; set; } = false;

        /// <summary>
        /// Number of Spike Traps in PULL mode (2 is good, enemies are grouped)
        /// </summary>
        public int PullModeTraps { get; set; } = 2;

        /// <summary>
        /// Number of Spike Traps in DAMAGE mode (5 for optimal chain reaction)
        /// </summary>
        public int DamageModeTraps { get; set; } = 5;

        /// <summary>
        /// Delay between Spike Trap placements (ms) - traps need time to register
        /// </summary>
        public int TrapPlacementDelay { get; set; } = 80;

        /// <summary>
        /// Wait time after Caltrops for enemies to group up (ms)
        /// </summary>
        public int CaltropsWaitTime { get; set; } = 350;

        /// <summary>
        /// Wait time after placing all traps before detonating (ms)
        /// Traps need to "arm" and enemies need to be on them
        /// </summary>
        public int DetonationWaitTime { get; set; } = 200;

        /// <summary>
        /// Time to channel Evasive Fire for reliable detonation (ms)
        /// </summary>
        public int DetonationDuration { get; set; } = 150;

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
        /// Range considered "close" for trap placement
        /// </summary>
        public float CloseRange { get; set; } = 25f;

        /// <summary>
        /// Minimum number of enemies to trigger combat
        /// </summary>
        public int MinEnemiesForCombat { get; set; } = 1;

        /// <summary>
        /// Enable automatic force movement when no enemies nearby
        /// </summary>
        public bool EnableAutoMovement { get; set; } = true;

        /// <summary>
        /// Time to wait before exiting combat state (ms)
        /// </summary>
        public int CombatExitDelay { get; set; } = 600;

        #endregion

        #region Private Fields

        public bool Running { get; private set; }
        public bool IsDamageMode { get; private set; } = true;
        private bool _isDemonHunter = false;
        private bool _hasNatalyaSet = false;
        private bool _hasSpikeTrapEquipped = false;
        private bool _isInCombat = false;

        // Skill references
        private IPlayerSkill _skillEvasiveFire;
        private IPlayerSkill _skillSpikeTrap;
        private IPlayerSkill _skillCaltrops;
        private IPlayerSkill _skillVengeance;
        private IPlayerSkill _skillSmokeScreen;
        private IPlayerSkill _skillShadowPower;

        // Timers
        private IWatch _phaseTimer;
        private IWatch _trapTimer;
        private IWatch _movementTimer;
        private IWatch _combatExitTimer;
        private IWatch _detonationTimer;

        // State
        private int _trapsPlaced = 0;
        private MacroPhase _phase = MacroPhase.Idle;
        private int _nearbyEnemyCount = 0;
        private int _closeEnemyCount = 0;

        // Fonts
        private IFont _titleFont;
        private IFont _runningFont;
        private IFont _modeFont;
        private IFont _stoppedFont;
        private IFont _tipFont;
        private IFont _movingFont;
        private IFont _phaseFont;

        // UI
        private IUiElement _chatUI;
        private IBrush _panelBrush;
        private IBrush _borderBrush;
        private IBrush _accentOnBrush;
        private IBrush _accentOffBrush;

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

            _phaseTimer = Hud.Time.CreateWatch();
            _trapTimer = Hud.Time.CreateWatch();
            _movementTimer = Hud.Time.CreateWatch();
            _combatExitTimer = Hud.Time.CreateWatch();
            _detonationTimer = Hud.Time.CreateWatch();

            // UI Fonts
            _titleFont = Hud.Render.CreateFont("tahoma", 8, 255, 220, 180, 100, true, false, 180, 0, 0, 0, true);
            _runningFont = Hud.Render.CreateFont("tahoma", 7.5f, 255, 0, 255, 0, true, false, 160, 0, 0, 0, true);
            _modeFont = Hud.Render.CreateFont("tahoma", 7.5f, 255, 255, 200, 0, true, false, 160, 0, 0, 0, true);
            _stoppedFont = Hud.Render.CreateFont("tahoma", 7.5f, 255, 255, 100, 100, true, false, 160, 0, 0, 0, true);
            _tipFont = Hud.Render.CreateFont("tahoma", 7, 200, 180, 180, 180, false, false, 140, 0, 0, 0, true);
            _movingFont = Hud.Render.CreateFont("tahoma", 7.5f, 255, 100, 180, 255, true, false, 160, 0, 0, 0, true);
            _phaseFont = Hud.Render.CreateFont("tahoma", 6.5f, 200, 150, 150, 150, false, false, 130, 0, 0, 0, true);

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
                if (Running) StopMacro();
                else StartMacro();
            }

            // F2 - Switch mode
            if (ModeKeyEvent.Matches(keyEvent) && keyEvent.IsPressed)
            {
                if (Running)
                {
                    IsDamageMode = !IsDamageMode;
                    ResetCombatCycle();
                }
            }
        }

        public void AfterCollect()
        {
            if (!Hud.Game.IsInGame) return;

            _isDemonHunter = Hud.Game.Me.HeroClassDefinition.HeroClass == HeroClass.DemonHunter;
            if (!_isDemonHunter)
            {
                if (Running) StopMacro();
                _hasSpikeTrapEquipped = false;
                return;
            }

            FindSkills();
            CheckNatalyaSet();

            _hasSpikeTrapEquipped = _skillSpikeTrap != null;
            if (!_hasSpikeTrapEquipped)
            {
                if (Running) StopMacro();
                return;
            }

            if (!Running) return;
            if (ShouldPauseMacro()) return;

            // Count enemies at different ranges
            _nearbyEnemyCount = Hud.Game.AliveMonsters.Count(m => m.CentralXyDistanceToMe <= EnemyDetectionRange);
            _closeEnemyCount = Hud.Game.AliveMonsters.Count(m => m.CentralXyDistanceToMe <= CloseRange);

            UpdateCombatState();
            ProcessMacro();
        }

        private void UpdateCombatState()
        {
            bool hasEnoughEnemies = _nearbyEnemyCount >= MinEnemiesForCombat;

            if (hasEnoughEnemies)
            {
                if (!_isInCombat)
                {
                    _isInCombat = true;
                    ResetCombatCycle();
                }
                _combatExitTimer.Restart();
            }
            else if (_isInCombat)
            {
                if (_combatExitTimer.ElapsedMilliseconds >= CombatExitDelay)
                {
                    _isInCombat = false;
                    ResetCombatCycle();
                }
            }
        }

        private void ResetCombatCycle()
        {
            _phase = MacroPhase.Idle;
            _trapsPlaced = 0;
            _phaseTimer.Restart();
            _trapTimer.Restart();
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
            _hasNatalyaSet = Hud.Game.Me.GetSetItemCount(847985) >= 4;
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
            // 1. Always maintain buffs
            RefreshBuffs();

            // 2. Combat or Movement
            if (_isInCombat)
            {
                ProcessCombat();
            }
            else
            {
                ProcessMovement();
            }
        }

        private void ProcessMovement()
        {
            if (!EnableAutoMovement) return;

            if (_movementTimer.IsRunning && _movementTimer.ElapsedMilliseconds < MovementDelay)
                return;

            Hud.Interaction.DoAction(ActionKey.Move);
            _movementTimer.Restart();
        }

        private void ProcessCombat()
        {
            // === OPTIMIZED COMBAT ROTATION ===
            // 
            // PULL MODE:
            // 1. Caltrops (pulls enemies)
            // 2. WAIT for enemies to group (CaltropsWaitTime)
            // 3. Place traps on grouped enemies
            // 4. WAIT for traps to arm (DetonationWaitTime)
            // 5. Detonate with Evasive Fire
            //
            // DAMAGE MODE:
            // 1. Place traps
            // 2. WAIT for traps to arm (DetonationWaitTime)
            // 3. Detonate with Evasive Fire

            switch (_phase)
            {
                case MacroPhase.Idle:
                    StartCombatCycle();
                    break;

                case MacroPhase.Caltrops:
                    ProcessCaltropsPhase();
                    break;

                case MacroPhase.WaitingForPull:
                    ProcessWaitingForPull();
                    break;

                case MacroPhase.PlacingTraps:
                    ProcessPlacingTraps();
                    break;

                case MacroPhase.WaitingToDetonate:
                    ProcessWaitingToDetonate();
                    break;

                case MacroPhase.Detonating:
                    ProcessDetonating();
                    break;
            }
        }

        private void StartCombatCycle()
        {
            if (IsDamageMode)
            {
                // DAMAGE mode: Go straight to traps
                _phase = MacroPhase.PlacingTraps;
            }
            else
            {
                // PULL mode: Start with Caltrops
                _phase = MacroPhase.Caltrops;
            }
            _trapsPlaced = 0;
            _phaseTimer.Restart();
            _trapTimer.Restart();
        }

        private void ProcessCaltropsPhase()
        {
            // Cast Caltrops to pull enemies
            if (_skillCaltrops != null && !_skillCaltrops.IsOnCooldown)
            {
                Hud.Interaction.DoAction(_skillCaltrops.Key);
            }

            // Move to waiting phase
            _phase = MacroPhase.WaitingForPull;
            _phaseTimer.Restart();
        }

        private void ProcessWaitingForPull()
        {
            // Wait for Caltrops to pull enemies together
            if (_phaseTimer.ElapsedMilliseconds >= CaltropsWaitTime)
            {
                // Check if enemies actually grouped up (at least some close now)
                if (_closeEnemyCount >= 1 || _phaseTimer.ElapsedMilliseconds >= CaltropsWaitTime + 200)
                {
                    _phase = MacroPhase.PlacingTraps;
                    _phaseTimer.Restart();
                    _trapTimer.Restart();
                }
            }

            // Re-cast Caltrops if it's available (keeps pulling)
            if (_skillCaltrops != null && !_skillCaltrops.IsOnCooldown && 
                _phaseTimer.ElapsedMilliseconds > 150)
            {
                Hud.Interaction.DoAction(_skillCaltrops.Key);
            }
        }

        private void ProcessPlacingTraps()
        {
            if (_skillSpikeTrap == null) return;

            int targetTraps = IsDamageMode ? DamageModeTraps : PullModeTraps;

            // Wait between trap placements
            if (_trapTimer.ElapsedMilliseconds < TrapPlacementDelay && _trapsPlaced > 0)
                return;

            if (_trapsPlaced < targetTraps)
            {
                // Place trap at cursor position (should be on enemies)
                Hud.Interaction.DoAction(_skillSpikeTrap.Key);
                _trapsPlaced++;
                _trapTimer.Restart();
            }
            else
            {
                // All traps placed - wait for them to arm
                _phase = MacroPhase.WaitingToDetonate;
                _phaseTimer.Restart();
            }
        }

        private void ProcessWaitingToDetonate()
        {
            // Wait for traps to arm and enemies to be on them
            if (_phaseTimer.ElapsedMilliseconds >= DetonationWaitTime)
            {
                _phase = MacroPhase.Detonating;
                _detonationTimer.Restart();
            }
        }

        private void ProcessDetonating()
        {
            if (_skillEvasiveFire == null) return;

            // Keep firing Evasive Fire to ensure detonation
            Hud.Interaction.DoAction(_skillEvasiveFire.Key);

            // In PULL mode, also keep casting Caltrops for more pulls
            if (!IsDamageMode && _skillCaltrops != null && !_skillCaltrops.IsOnCooldown)
            {
                Hud.Interaction.DoAction(_skillCaltrops.Key);
            }

            // After detonation duration, start new cycle
            if (_detonationTimer.ElapsedMilliseconds >= DetonationDuration)
            {
                _phase = MacroPhase.Idle;
                _trapsPlaced = 0;
            }
        }

        private void RefreshBuffs()
        {
            // Vengeance
            if (_skillVengeance != null && !_skillVengeance.IsOnCooldown)
            {
                double buffTime = _skillVengeance.BuffIsActive ? _skillVengeance.RemainingBuffTime() : 0;
                if (buffTime < VengeanceRefreshTime)
                {
                    Hud.Interaction.DoAction(_skillVengeance.Key);
                }
            }

            // Shadow Power
            if (_skillShadowPower != null && !_skillShadowPower.IsOnCooldown)
            {
                double buffTime = _skillShadowPower.BuffIsActive ? _skillShadowPower.RemainingBuffTime() : 0;
                if (buffTime < ShadowPowerRefreshTime)
                {
                    Hud.Interaction.DoAction(_skillShadowPower.Key);
                }
            }

            // Smoke Screen (emergency)
            if (_skillSmokeScreen != null && !_skillSmokeScreen.IsOnCooldown)
            {
                if (Hud.Game.Me.Defense.HealthPct < 0.5)
                {
                    Hud.Interaction.DoAction(_skillSmokeScreen.Key);
                }
            }
        }

        private void StartMacro()
        {
            if (!_hasSpikeTrapEquipped) return;
            
            Running = true;
            ResetCombatCycle();
            _isInCombat = false;
            _movementTimer.Restart();
            _combatExitTimer.Restart();
        }

        private void StopMacro()
        {
            Running = false;
            ResetCombatCycle();
            _isInCombat = false;
            _movementTimer.Stop();
        }

        public void PaintTopInGame(ClipState clipState)
        {
            if (clipState != ClipState.AfterClip) return;
            if (!Hud.Game.IsInGame) return;
            if (!_isDemonHunter) return;
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
            var playerScreenPos = Hud.Game.Me.FloorCoordinate.ToScreenCoordinate();
            float centerX = playerScreenPos.X;
            float baseY = playerScreenPos.Y + 10;

            if (Running)
            {
                // Title
                var layout1 = _titleFont.GetTextLayout("N6 Spike Trap");
                _titleFont.DrawText(layout1, centerX - layout1.Metrics.Width / 2, baseY);
                baseY += layout1.Metrics.Height + 2;

                if (_isInCombat)
                {
                    // Combat status
                    var layout2 = _runningFont.GetTextLayout($"● COMBAT ({_closeEnemyCount}/{_nearbyEnemyCount})");
                    _runningFont.DrawText(layout2, centerX - layout2.Metrics.Width / 2, baseY);
                    baseY += layout2.Metrics.Height + 2;

                    // Mode
                    string modeText = IsDamageMode ? "DAMAGE" : "PULL";
                    int trapCount = IsDamageMode ? DamageModeTraps : PullModeTraps;
                    var modeLayout = _modeFont.GetTextLayout($"{modeText} ({trapCount} traps)");
                    _modeFont.DrawText(modeLayout, centerX - modeLayout.Metrics.Width / 2, baseY);
                    baseY += modeLayout.Metrics.Height + 2;

                    // Phase indicator
                    string phaseText = GetPhaseText();
                    var phaseLayout = _phaseFont.GetTextLayout(phaseText);
                    _phaseFont.DrawText(phaseLayout, centerX - phaseLayout.Metrics.Width / 2, baseY);
                }
                else
                {
                    // Moving status
                    var layout2 = _movingFont.GetTextLayout("● MOVING");
                    _movingFont.DrawText(layout2, centerX - layout2.Metrics.Width / 2, baseY);
                    baseY += layout2.Metrics.Height + 2;

                    string modeText = IsDamageMode ? "[F2] DAMAGE" : "[F2] PULL";
                    var modeLayout = _tipFont.GetTextLayout(modeText);
                    _tipFont.DrawText(modeLayout, centerX - modeLayout.Metrics.Width / 2, baseY);
                }
            }
            else
            {
                // OFF state
                var layout1 = _titleFont.GetTextLayout("N6 Spike Trap");
                _titleFont.DrawText(layout1, centerX - layout1.Metrics.Width / 2, baseY);
                baseY += layout1.Metrics.Height + 2;

                var layout2 = _stoppedFont.GetTextLayout("OFF [F1]");
                _stoppedFont.DrawText(layout2, centerX - layout2.Metrics.Width / 2, baseY);
            }
        }

        private string GetPhaseText()
        {
            switch (_phase)
            {
                case MacroPhase.Caltrops: return "Pulling...";
                case MacroPhase.WaitingForPull: return "Grouping...";
                case MacroPhase.PlacingTraps: return $"Traps {_trapsPlaced}/{(IsDamageMode ? DamageModeTraps : PullModeTraps)}";
                case MacroPhase.WaitingToDetonate: return "Arming...";
                case MacroPhase.Detonating: return "BOOM!";
                default: return "";
            }
        }
    }

    internal enum MacroPhase
    {
        Idle,
        Caltrops,           // Cast Caltrops to pull
        WaitingForPull,     // Wait for enemies to group
        PlacingTraps,       // Place Spike Traps
        WaitingToDetonate,  // Wait for traps to arm
        Detonating          // Fire Evasive Fire
    }
}
