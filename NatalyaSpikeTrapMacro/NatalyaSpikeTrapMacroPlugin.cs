namespace Turbo.Plugins.Custom.NatalyaSpikeTrapMacro
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;
    using SharpDX.DirectInput;
    using Turbo.Plugins.Custom.Core;
    using Turbo.Plugins.Default;

    /// <summary>
    /// Natalya Spike Trap Demon Hunter Macro - Core Integrated v2.0
    /// 
    /// Now integrated with the Core Plugin Framework!
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
    /// Hotkeys:
    /// - F1 = Toggle macro ON/OFF
    /// - F2 = Switch between PULL mode and DAMAGE mode
    /// </summary>
    public class NatalyaSpikeTrapMacroPlugin : CustomPluginBase, IInGameTopPainter, IKeyEventHandler, IAfterCollectHandler
    {
        #region Plugin Metadata

        public override string PluginId => "natalya-spike-trap-macro";
        public override string PluginName => "N6 Spike Trap";
        public override string PluginDescription => "Automated rotation for Natalya Spike Trap DH";
        public override string PluginVersion => "2.0.0";
        public override string PluginCategory => "macro";
        public override string PluginIcon => "🎯";
        public override bool HasSettings => true;

        #endregion

        #region Requirements (Demon Hunter with Spike Trap)

        public override HeroClass? RequiredHeroClass => HeroClass.DemonHunter;
        public override string RequiredBuild => "Natalya's Set";
        
        public override bool RequirementsMet
        {
            get
            {
                if (!Hud.Game.IsInGame) return false;
                if (Hud.Game.Me?.HeroClassDefinition?.HeroClass != HeroClass.DemonHunter) return false;
                // Check if Spike Trap is equipped
                return _hasSpikeTrapEquipped;
            }
        }

        #endregion

        #region Settings

        public IKeyEvent ToggleKeyEvent { get; set; }
        public IKeyEvent ModeKeyEvent { get; set; }
        public bool IsHideTip { get; set; } = false;

        public int PullModeTraps { get; set; } = 2;
        public int DamageModeTraps { get; set; } = 5;
        public int TrapPlacementDelay { get; set; } = 80;
        public int CaltropsWaitTime { get; set; } = 350;
        public int DetonationWaitTime { get; set; } = 200;
        public int DetonationDuration { get; set; } = 150;
        public int MovementDelay { get; set; } = 100;
        public float VengeanceRefreshTime { get; set; } = 3.0f;
        public float ShadowPowerRefreshTime { get; set; } = 2.0f;
        public float EnemyDetectionRange { get; set; } = 50f;
        public float CloseRange { get; set; } = 25f;
        public int MinEnemiesForCombat { get; set; } = 1;
        public bool EnableAutoMovement { get; set; } = true;
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
        private N6MacroPhase _phase = N6MacroPhase.Idle;
        private int _nearbyEnemyCount = 0;
        private int _closeEnemyCount = 0;

        // Fallback fonts (used when Core not available)
        private IFont _titleFont;
        private IFont _runningFont;
        private IFont _modeFont;
        private IFont _stoppedFont;
        private IFont _tipFont;
        private IFont _movingFont;
        private IFont _phaseFont;

        // UI
        private IUiElement _chatUI;

        #endregion

        #region Initialization

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

            // Fallback fonts
            _titleFont = Hud.Render.CreateFont("tahoma", 8, 255, 220, 180, 100, true, false, 180, 0, 0, 0, true);
            _runningFont = Hud.Render.CreateFont("tahoma", 7.5f, 255, 0, 255, 0, true, false, 160, 0, 0, 0, true);
            _modeFont = Hud.Render.CreateFont("tahoma", 7.5f, 255, 255, 200, 0, true, false, 160, 0, 0, 0, true);
            _stoppedFont = Hud.Render.CreateFont("tahoma", 7.5f, 255, 255, 100, 100, true, false, 160, 0, 0, 0, true);
            _tipFont = Hud.Render.CreateFont("tahoma", 7, 200, 180, 180, 180, false, false, 140, 0, 0, 0, true);
            _movingFont = Hud.Render.CreateFont("tahoma", 7.5f, 255, 100, 180, 255, true, false, 160, 0, 0, 0, true);
            _phaseFont = Hud.Render.CreateFont("tahoma", 6.5f, 200, 150, 150, 150, false, false, 130, 0, 0, 0, true);

            _chatUI = Hud.Render.RegisterUiElement("Root.NormalLayer.chatentry_dialog_backgroundScreen.chatentry_content.chat_editline", null, null);

            Log("N6 Spike Trap Macro loaded");
        }

        #endregion

        #region Settings Panel

        public override void DrawSettings(IController hud, RectangleF rect, Dictionary<string, RectangleF> clickAreas, int scrollOffset)
        {
            float x = rect.X, y = rect.Y, w = rect.Width;

            // Status
            string statusText = Running ? "● RUNNING" : "○ STOPPED";
            var statusFont = Running ? (HasCore ? Core.FontSuccess : _runningFont) : (HasCore ? Core.FontError : _stoppedFont);
            var statusLayout = statusFont.GetTextLayout(statusText);
            statusFont.DrawText(statusLayout, x, y);
            y += statusLayout.Metrics.Height + 10;

            // Mode section
            y += DrawSettingsHeader(x, y, "Mode Settings");
            y += 8;

            string modeText = IsDamageMode ? "DAMAGE Mode" : "PULL Mode";
            y += DrawToggleSetting(x, y, w, modeText, IsDamageMode, clickAreas, "toggle_mode");
            y += DrawToggleSetting(x, y, w, "Auto Movement", EnableAutoMovement, clickAreas, "toggle_automove");
            y += DrawToggleSetting(x, y, w, "Hide When Stopped", IsHideTip, clickAreas, "toggle_hide");

            y += 12;

            // Trap Settings
            y += DrawSettingsHeader(x, y, "Trap Settings");
            y += 8;

            y += DrawSelectorSetting(x, y, w, "Pull Traps", PullModeTraps.ToString(), clickAreas, "sel_pulltraps");
            y += DrawSelectorSetting(x, y, w, "Damage Traps", DamageModeTraps.ToString(), clickAreas, "sel_dmgtraps");

            y += 12;

            // Timing section
            y += DrawSettingsHeader(x, y, "Timing (ms)");
            y += 8;

            y += DrawSelectorSetting(x, y, w, "Trap Delay", TrapPlacementDelay.ToString(), clickAreas, "sel_trapdelay");
            y += DrawSelectorSetting(x, y, w, "Caltrops Wait", CaltropsWaitTime.ToString(), clickAreas, "sel_caltropswait");
            y += DrawSelectorSetting(x, y, w, "Detonate Wait", DetonationWaitTime.ToString(), clickAreas, "sel_detwait");

            y += 16;
            y += DrawSettingsHint(x, y, "[F1] Toggle • [F2] Mode");
        }

        public override void HandleSettingsClick(string clickId)
        {
            switch (clickId)
            {
                case "toggle_mode":
                    IsDamageMode = !IsDamageMode;
                    ResetCombatCycle();
                    break;
                case "toggle_automove":
                    EnableAutoMovement = !EnableAutoMovement;
                    break;
                case "toggle_hide":
                    IsHideTip = !IsHideTip;
                    break;
                case "sel_pulltraps_prev":
                    PullModeTraps = Math.Max(1, PullModeTraps - 1);
                    break;
                case "sel_pulltraps_next":
                    PullModeTraps = Math.Min(5, PullModeTraps + 1);
                    break;
                case "sel_dmgtraps_prev":
                    DamageModeTraps = Math.Max(1, DamageModeTraps - 1);
                    break;
                case "sel_dmgtraps_next":
                    DamageModeTraps = Math.Min(10, DamageModeTraps + 1);
                    break;
                case "sel_trapdelay_prev":
                    TrapPlacementDelay = Math.Max(20, TrapPlacementDelay - 20);
                    break;
                case "sel_trapdelay_next":
                    TrapPlacementDelay = Math.Min(200, TrapPlacementDelay + 20);
                    break;
                case "sel_caltropswait_prev":
                    CaltropsWaitTime = Math.Max(100, CaltropsWaitTime - 50);
                    break;
                case "sel_caltropswait_next":
                    CaltropsWaitTime = Math.Min(800, CaltropsWaitTime + 50);
                    break;
                case "sel_detwait_prev":
                    DetonationWaitTime = Math.Max(50, DetonationWaitTime - 50);
                    break;
                case "sel_detwait_next":
                    DetonationWaitTime = Math.Min(500, DetonationWaitTime + 50);
                    break;
            }
            SavePluginSettings();
        }

        protected override object GetSettingsObject() => new N6Settings
        {
            IsDamageMode = this.IsDamageMode,
            IsHideTip = this.IsHideTip,
            EnableAutoMovement = this.EnableAutoMovement,
            PullModeTraps = this.PullModeTraps,
            DamageModeTraps = this.DamageModeTraps,
            TrapPlacementDelay = this.TrapPlacementDelay,
            CaltropsWaitTime = this.CaltropsWaitTime,
            DetonationWaitTime = this.DetonationWaitTime,
            DetonationDuration = this.DetonationDuration
        };

        protected override void ApplySettingsObject(object settings)
        {
            if (settings is N6Settings s)
            {
                IsDamageMode = s.IsDamageMode;
                IsHideTip = s.IsHideTip;
                EnableAutoMovement = s.EnableAutoMovement;
                PullModeTraps = s.PullModeTraps;
                DamageModeTraps = s.DamageModeTraps;
                TrapPlacementDelay = s.TrapPlacementDelay;
                CaltropsWaitTime = s.CaltropsWaitTime;
                DetonationWaitTime = s.DetonationWaitTime;
                DetonationDuration = s.DetonationDuration;
            }
        }

        private class N6Settings : PluginSettingsBase
        {
            public bool IsDamageMode { get; set; }
            public bool IsHideTip { get; set; }
            public bool EnableAutoMovement { get; set; }
            public int PullModeTraps { get; set; }
            public int DamageModeTraps { get; set; }
            public int TrapPlacementDelay { get; set; }
            public int CaltropsWaitTime { get; set; }
            public int DetonationWaitTime { get; set; }
            public int DetonationDuration { get; set; }
        }

        #endregion

        #region Key Handler

        public void OnKeyEvent(IKeyEvent keyEvent)
        {
            if (!Hud.Game.IsInGame) return;
            if (!Enabled) return;
            if (!_isDemonHunter) return;
            if (!_hasSpikeTrapEquipped) return;
            if (Hud.Inventory.InventoryMainUiElement.Visible) return;

            if (ToggleKeyEvent.Matches(keyEvent) && keyEvent.IsPressed)
            {
                if (Running) StopMacro();
                else StartMacro();
            }

            if (ModeKeyEvent.Matches(keyEvent) && keyEvent.IsPressed)
            {
                if (Running)
                {
                    IsDamageMode = !IsDamageMode;
                    ResetCombatCycle();
                }
            }
        }

        #endregion

        #region Main Logic

        public void AfterCollect()
        {
            if (!Hud.Game.IsInGame) return;
            if (!Enabled) return;

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
            _phase = N6MacroPhase.Idle;
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
                
                if (sno == Hud.Sno.SnoPowers.DemonHunter_EvasiveFire.Sno) _skillEvasiveFire = skill;
                else if (sno == Hud.Sno.SnoPowers.DemonHunter_SpikeTrap.Sno) _skillSpikeTrap = skill;
                else if (sno == Hud.Sno.SnoPowers.DemonHunter_Caltrops.Sno) _skillCaltrops = skill;
                else if (sno == Hud.Sno.SnoPowers.DemonHunter_Vengeance.Sno) _skillVengeance = skill;
                else if (sno == Hud.Sno.SnoPowers.DemonHunter_SmokeScreen.Sno) _skillSmokeScreen = skill;
                else if (sno == Hud.Sno.SnoPowers.DemonHunter_ShadowPower.Sno) _skillShadowPower = skill;
            }
        }

        private void CheckNatalyaSet()
        {
            _hasNatalyaSet = Hud.Game.Me.GetSetItemCount(847985) >= 4;
        }

        private bool ShouldPauseMacro()
        {
            return Hud.Game.IsLoading || Hud.Game.IsPaused || Hud.Game.IsInTown
                || !Hud.Window.IsForeground || !Hud.Render.MinimapUiElement.Visible
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
            RefreshBuffs();

            if (_isInCombat)
                ProcessCombat();
            else
                ProcessMovement();
        }

        private void ProcessMovement()
        {
            if (!EnableAutoMovement) return;
            if (_movementTimer.IsRunning && _movementTimer.ElapsedMilliseconds < MovementDelay) return;

            Hud.Interaction.DoAction(ActionKey.Move);
            _movementTimer.Restart();
        }

        private void ProcessCombat()
        {
            switch (_phase)
            {
                case N6MacroPhase.Idle: StartCombatCycle(); break;
                case N6MacroPhase.Caltrops: ProcessCaltropsPhase(); break;
                case N6MacroPhase.WaitingForPull: ProcessWaitingForPull(); break;
                case N6MacroPhase.PlacingTraps: ProcessPlacingTraps(); break;
                case N6MacroPhase.WaitingToDetonate: ProcessWaitingToDetonate(); break;
                case N6MacroPhase.Detonating: ProcessDetonating(); break;
            }
        }

        private void StartCombatCycle()
        {
            _phase = IsDamageMode ? N6MacroPhase.PlacingTraps : N6MacroPhase.Caltrops;
            _trapsPlaced = 0;
            _phaseTimer.Restart();
            _trapTimer.Restart();
        }

        private void ProcessCaltropsPhase()
        {
            if (_skillCaltrops != null && !_skillCaltrops.IsOnCooldown)
                Hud.Interaction.DoAction(_skillCaltrops.Key);

            _phase = N6MacroPhase.WaitingForPull;
            _phaseTimer.Restart();
        }

        private void ProcessWaitingForPull()
        {
            if (_phaseTimer.ElapsedMilliseconds >= CaltropsWaitTime)
            {
                if (_closeEnemyCount >= 1 || _phaseTimer.ElapsedMilliseconds >= CaltropsWaitTime + 200)
                {
                    _phase = N6MacroPhase.PlacingTraps;
                    _phaseTimer.Restart();
                    _trapTimer.Restart();
                }
            }

            if (_skillCaltrops != null && !_skillCaltrops.IsOnCooldown && _phaseTimer.ElapsedMilliseconds > 150)
                Hud.Interaction.DoAction(_skillCaltrops.Key);
        }

        private void ProcessPlacingTraps()
        {
            if (_skillSpikeTrap == null) return;

            int targetTraps = IsDamageMode ? DamageModeTraps : PullModeTraps;

            if (_trapTimer.ElapsedMilliseconds < TrapPlacementDelay && _trapsPlaced > 0) return;

            if (_trapsPlaced < targetTraps)
            {
                Hud.Interaction.DoAction(_skillSpikeTrap.Key);
                _trapsPlaced++;
                _trapTimer.Restart();
            }
            else
            {
                _phase = N6MacroPhase.WaitingToDetonate;
                _phaseTimer.Restart();
            }
        }

        private void ProcessWaitingToDetonate()
        {
            if (_phaseTimer.ElapsedMilliseconds >= DetonationWaitTime)
            {
                _phase = N6MacroPhase.Detonating;
                _detonationTimer.Restart();
            }
        }

        private void ProcessDetonating()
        {
            if (_skillEvasiveFire == null) return;

            Hud.Interaction.DoAction(_skillEvasiveFire.Key);

            if (!IsDamageMode && _skillCaltrops != null && !_skillCaltrops.IsOnCooldown)
                Hud.Interaction.DoAction(_skillCaltrops.Key);

            if (_detonationTimer.ElapsedMilliseconds >= DetonationDuration)
            {
                _phase = N6MacroPhase.Idle;
                _trapsPlaced = 0;
            }
        }

        private void RefreshBuffs()
        {
            if (_skillVengeance != null && !_skillVengeance.IsOnCooldown)
            {
                double buffTime = _skillVengeance.BuffIsActive ? _skillVengeance.RemainingBuffTime() : 0;
                if (buffTime < VengeanceRefreshTime)
                    Hud.Interaction.DoAction(_skillVengeance.Key);
            }

            if (_skillShadowPower != null && !_skillShadowPower.IsOnCooldown)
            {
                double buffTime = _skillShadowPower.BuffIsActive ? _skillShadowPower.RemainingBuffTime() : 0;
                if (buffTime < ShadowPowerRefreshTime)
                    Hud.Interaction.DoAction(_skillShadowPower.Key);
            }

            if (_skillSmokeScreen != null && !_skillSmokeScreen.IsOnCooldown && Hud.Game.Me.Defense.HealthPct < 0.5)
                Hud.Interaction.DoAction(_skillSmokeScreen.Key);
        }

        private void StartMacro()
        {
            if (!_hasSpikeTrapEquipped) return;
            
            Running = true;
            ResetCombatCycle();
            _isInCombat = false;
            _movementTimer.Restart();
            _combatExitTimer.Restart();
            
            SetCoreStatus($"{PluginName} started", StatusType.Success);
        }

        private void StopMacro()
        {
            Running = false;
            ResetCombatCycle();
            _isInCombat = false;
            _movementTimer.Stop();
            
            SetCoreStatus($"{PluginName} stopped", StatusType.Warning);
        }

        #endregion

        #region Rendering

        public override void PaintTopInGame(ClipState clipState)
        {
            // IMPORTANT: Call base to ensure Core registration
            base.PaintTopInGame(clipState);
            
            if (clipState != ClipState.AfterClip) return;
            if (!Hud.Game.IsInGame || !Enabled) return;
            if (!_isDemonHunter || !_hasSpikeTrapEquipped) return;

            if (Hud.Inventory.InventoryMainUiElement.Visible && Running)
                StopMacro();

            if (IsHideTip && !Running) return;

            DrawStatusPanel();
        }

        private void DrawStatusPanel()
        {
            var playerScreenPos = Hud.Game.Me.FloorCoordinate.ToScreenCoordinate();
            float centerX = playerScreenPos.X;
            float baseY = playerScreenPos.Y + 10;

            var titleFont = HasCore ? Core.FontTitle : _titleFont;
            var runningFontToUse = HasCore ? Core.FontSuccess : _runningFont;
            var modeFontToUse = HasCore ? Core.FontWarning : _modeFont;
            var stoppedFontToUse = HasCore ? Core.FontError : _stoppedFont;
            var tipFontToUse = HasCore ? Core.FontMuted : _tipFont;
            var movingFontToUse = HasCore ? Core.FontAccent : _movingFont;

            if (Running)
            {
                var layout1 = titleFont.GetTextLayout("🎯 N6 Spike Trap");
                titleFont.DrawText(layout1, centerX - layout1.Metrics.Width / 2, baseY);
                baseY += layout1.Metrics.Height + 2;

                if (_isInCombat)
                {
                    var layout2 = runningFontToUse.GetTextLayout($"● COMBAT ({_closeEnemyCount}/{_nearbyEnemyCount})");
                    runningFontToUse.DrawText(layout2, centerX - layout2.Metrics.Width / 2, baseY);
                    baseY += layout2.Metrics.Height + 2;

                    string modeText = IsDamageMode ? "DAMAGE" : "PULL";
                    int trapCount = IsDamageMode ? DamageModeTraps : PullModeTraps;
                    var modeLayout = modeFontToUse.GetTextLayout($"{modeText} ({trapCount} traps)");
                    modeFontToUse.DrawText(modeLayout, centerX - modeLayout.Metrics.Width / 2, baseY);
                    baseY += modeLayout.Metrics.Height + 2;

                    string phaseText = GetPhaseText();
                    var phaseLayout = _phaseFont.GetTextLayout(phaseText);
                    _phaseFont.DrawText(phaseLayout, centerX - phaseLayout.Metrics.Width / 2, baseY);
                }
                else
                {
                    var layout2 = movingFontToUse.GetTextLayout("● MOVING");
                    movingFontToUse.DrawText(layout2, centerX - layout2.Metrics.Width / 2, baseY);
                    baseY += layout2.Metrics.Height + 2;

                    string modeText = IsDamageMode ? "[F2] DAMAGE" : "[F2] PULL";
                    var modeLayout = tipFontToUse.GetTextLayout(modeText);
                    tipFontToUse.DrawText(modeLayout, centerX - modeLayout.Metrics.Width / 2, baseY);
                }
            }
            else
            {
                var layout1 = titleFont.GetTextLayout("🎯 N6 Spike Trap");
                titleFont.DrawText(layout1, centerX - layout1.Metrics.Width / 2, baseY);
                baseY += layout1.Metrics.Height + 2;

                var layout2 = stoppedFontToUse.GetTextLayout("OFF [F1]");
                stoppedFontToUse.DrawText(layout2, centerX - layout2.Metrics.Width / 2, baseY);
            }
        }

        private string GetPhaseText()
        {
            switch (_phase)
            {
                case N6MacroPhase.Caltrops: return "Pulling...";
                case N6MacroPhase.WaitingForPull: return "Grouping...";
                case N6MacroPhase.PlacingTraps: return $"Traps {_trapsPlaced}/{(IsDamageMode ? DamageModeTraps : PullModeTraps)}";
                case N6MacroPhase.WaitingToDetonate: return "Arming...";
                case N6MacroPhase.Detonating: return "BOOM!";
                default: return "";
            }
        }

        #endregion
    }

    internal enum N6MacroPhase
    {
        Idle,
        Caltrops,
        WaitingForPull,
        PlacingTraps,
        WaitingToDetonate,
        Detonating
    }
}
