namespace Turbo.Plugins.Custom.LoDDeathNovaMacro
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
    /// LoD Death Nova Necromancer Macro - Core Integrated v2.0
    /// 
    /// Now integrates with the Core Plugin Framework for:
    /// - Centralized enable/disable via F8 hub
    /// - Settings panel integration
    /// - Shared UI components
    /// 
    /// Build: https://maxroll.gg/d3/guides/lod-death-nova-necromancer-guide
    /// 
    /// TWO BUILD VARIANTS SUPPORTED:
    /// 
    /// === STANDARD (Simulacrum + Haunted Visions) ===
    /// - Channel Siphon Blood → Iron Rose auto-casts Blood Nova
    /// - Simulacrums proc Blood Nova WITH Area Damage
    /// - Use SPEED mode for this variant
    /// 
    /// === PUSH (Nayr's + Squirt's - No Simulacrum) ===
    /// - Manual Death Nova in large packs → Triggers Area Damage
    /// - Channel Siphon Blood on boss → Funerary Pick stacks
    /// - Nayr's Black Death with Blight rune (Poison) stacks damage
    /// - Use PUSH mode for this variant
    /// 
    /// Hotkeys:
    /// - F1 = Toggle macro ON/OFF
    /// - F2 = Switch between SPEED mode and PUSH mode
    /// - F3 = Force Nuke (manual Death Nova spam)
    /// </summary>
    public class LoDDeathNovaMacroPlugin : CustomPluginBase, IInGameTopPainter, IKeyEventHandler, IAfterCollectHandler
    {
        #region Plugin Metadata (Required by Core)

        public override string PluginId => "lod-death-nova-macro";
        public override string PluginName => "LoD Death Nova";
        public override string PluginDescription => "Automated rotation for LoD Blood Nova Necromancer";
        public override string PluginVersion => "2.0.0";
        public override string PluginCategory => "macro";
        public override string PluginIcon => "💀";
        public override bool HasSettings => true;

        #endregion

        #region Requirements (Necromancer Only)

        public override HeroClass? RequiredHeroClass => HeroClass.Necromancer;
        public override string RequiredBuild => "LoD Death Nova";
        
        public override bool RequirementsMet
        {
            get
            {
                if (!Hud.Game.IsInGame) return false;
                if (Hud.Game.Me?.HeroClassDefinition?.HeroClass != HeroClass.Necromancer) return false;
                // Could add additional checks here for specific skills/items
                return true;
            }
        }

        #endregion

        #region Runtime State

        public override bool IsActive => Running;
        public override string StatusText => !Running ? "OFF" : (_isInCombat ? _phase.ToString() : "Ready");

        #endregion

        #region Settings

        public IKeyEvent ToggleKeyEvent { get; set; }
        public IKeyEvent ModeKeyEvent { get; set; }
        public IKeyEvent ForceNukeKeyEvent { get; set; }
        public bool IsHideTip { get; set; } = false;

        /// <summary>
        /// CoE Physical element icon index (6 = Physical for Necro)
        /// </summary>
        public int PhysicalCoEIconIndex { get; set; } = 6;

        /// <summary>
        /// Seconds before Physical CoE to prepare
        /// </summary>
        public float PrePhysicalPrepSeconds { get; set; } = 1.0f;

        /// <summary>
        /// Minimum Funerary Pick stacks before nuking (0-10)
        /// </summary>
        public int MinFuneraryPickStacks { get; set; } = 5;

        /// <summary>
        /// Bone Armor refresh threshold (seconds remaining)
        /// </summary>
        public float BoneArmorRefreshTime { get; set; } = 5.0f;

        /// <summary>
        /// Range to detect enemies
        /// </summary>
        public float EnemyDetectionRange { get; set; } = 60f;

        /// <summary>
        /// Range to detect elites
        /// </summary>
        public float EliteDetectionRange { get; set; } = 40f;

        /// <summary>
        /// Range for close combat (Death Nova range)
        /// </summary>
        public float CloseRange { get; set; } = 25f;

        /// <summary>
        /// Health percent for emergency Blood Rush
        /// </summary>
        public float EmergencyBloodRushHealthPct { get; set; } = 0.35f;

        /// <summary>
        /// Minimum enemies for Death Nova spam (large packs)
        /// </summary>
        public int MinEnemiesForNovaNuke { get; set; } = 5;

        /// <summary>
        /// Death Nova spam count during nuke phase
        /// </summary>
        public int DeathNovaSpamCount { get; set; } = 5;

        /// <summary>
        /// Delay between Death Nova casts (ms)
        /// </summary>
        public int DeathNovaDelay { get; set; } = 100;

        /// <summary>
        /// Delay after Bone Armor before nuking (ms) - for Krysbin's stun
        /// </summary>
        public int BoneArmorWaitTime { get; set; } = 150;

        /// <summary>
        /// Time to channel Siphon Blood after nukes (ms)
        /// </summary>
        public int SiphonChannelTime { get; set; } = 400;

        /// <summary>
        /// Force movement delay when no enemies (ms)
        /// </summary>
        public int MovementDelay { get; set; } = 100;

        /// <summary>
        /// Combat exit delay to prevent flickering (ms)
        /// </summary>
        public int CombatExitDelay { get; set; } = 600;

        /// <summary>
        /// Enable Oculus Ring circle detection
        /// </summary>
        public bool EnableOculusDetection { get; set; } = true;

        /// <summary>
        /// Bloodtide Blade nearby enemy range
        /// </summary>
        public float BloodtideRange { get; set; } = 25f;

        #endregion

        #region Private Fields

        public bool Running { get; private set; }
        public bool IsPushMode { get; private set; } = false;
        private bool _isNecromancer = false;
        private bool _forceNukeRequested = false;
        private bool _isInOculusCircle = false;
        private bool _isInCombat = false;

        // Skill references
        private IPlayerSkill _skillSiphonBlood;
        private IPlayerSkill _skillDeathNova;
        private IPlayerSkill _skillBoneArmor;
        private IPlayerSkill _skillSimulacrum;
        private IPlayerSkill _skillFrailty;
        private IPlayerSkill _skillBloodRush;

        // SNO Powers
        private uint _boneArmorSno;
        private uint _simulacrumSno;
        private uint _funeraryPickSno;
        private uint _coeSno;
        private uint _oculusRingSno;
        private uint _stoneGauntletsSno;
        private uint _nayrsSno;

        // Timers
        private IWatch _phaseTimer;
        private IWatch _movementTimer;
        private IWatch _combatExitTimer;
        private IWatch _novaTimer;

        // State
        private MacroPhase _phase = MacroPhase.Idle;
        private int _novasPlaced = 0;
        private int _nearbyEnemyCount = 0;
        private int _closeEnemyCount = 0;
        private int _bloodtideStacks = 0;
        private bool _hasSimulacrum = false;

        // Legacy fonts (for standalone rendering)
        private IFont _titleFont;
        private IFont _runningFont;
        private IFont _modeFont;
        private IFont _stoppedFont;
        private IFont _tipFont;
        private IFont _movingFont;
        private IFont _phaseFont;
        private IFont _stackFont;
        private IFont _bloodtideFont;
        private IFont _oculusFont;

        // UI
        private IUiElement _chatUI;
        private IBrush _panelBrush;
        private IBrush _borderBrush;
        private IBrush _accentOnBrush;
        private IBrush _accentOffBrush;

        #endregion

        #region Initialization

        public LoDDeathNovaMacroPlugin()
        {
            Enabled = true;
            Order = 50004;
        }

        public override void Load(IController hud)
        {
            base.Load(hud);

            ToggleKeyEvent = Hud.Input.CreateKeyEvent(true, Key.F1, false, false, false);
            ModeKeyEvent = Hud.Input.CreateKeyEvent(true, Key.F2, false, false, false);
            ForceNukeKeyEvent = Hud.Input.CreateKeyEvent(true, Key.F3, false, false, false);

            _phaseTimer = Hud.Time.CreateWatch();
            _movementTimer = Hud.Time.CreateWatch();
            _combatExitTimer = Hud.Time.CreateWatch();
            _novaTimer = Hud.Time.CreateWatch();

            // SNO powers
            _boneArmorSno = Hud.Sno.SnoPowers.Necromancer_BoneArmor.Sno;
            _simulacrumSno = Hud.Sno.SnoPowers.Necromancer_Simulacrum.Sno;
            _funeraryPickSno = 476587;
            _coeSno = 430674;
            _oculusRingSno = 402461;
            _stoneGauntletsSno = 318820;
            _nayrsSno = 476699;

            // Legacy fonts (used when Core not available)
            _titleFont = Hud.Render.CreateFont("tahoma", 8, 255, 150, 255, 150, true, false, 180, 0, 0, 0, true);
            _runningFont = Hud.Render.CreateFont("tahoma", 7.5f, 255, 0, 255, 0, true, false, 160, 0, 0, 0, true);
            _modeFont = Hud.Render.CreateFont("tahoma", 7.5f, 255, 255, 200, 0, true, false, 160, 0, 0, 0, true);
            _stoppedFont = Hud.Render.CreateFont("tahoma", 7.5f, 255, 255, 100, 100, true, false, 160, 0, 0, 0, true);
            _tipFont = Hud.Render.CreateFont("tahoma", 7, 200, 180, 180, 180, false, false, 140, 0, 0, 0, true);
            _movingFont = Hud.Render.CreateFont("tahoma", 7.5f, 255, 100, 180, 255, true, false, 160, 0, 0, 0, true);
            _phaseFont = Hud.Render.CreateFont("tahoma", 6.5f, 200, 150, 150, 150, false, false, 130, 0, 0, 0, true);
            _stackFont = Hud.Render.CreateFont("tahoma", 7, 255, 255, 220, 100, false, false, 140, 0, 0, 0, true);
            _bloodtideFont = Hud.Render.CreateFont("tahoma", 7, 255, 255, 100, 100, false, false, 140, 0, 0, 0, true);
            _oculusFont = Hud.Render.CreateFont("tahoma", 7, 255, 255, 255, 100, true, false, 140, 0, 0, 0, true);

            _panelBrush = Hud.Render.CreateBrush(235, 15, 30, 15, 0);
            _borderBrush = Hud.Render.CreateBrush(200, 50, 100, 50, 1f);
            _accentOnBrush = Hud.Render.CreateBrush(255, 80, 200, 80, 0);
            _accentOffBrush = Hud.Render.CreateBrush(255, 200, 80, 80, 0);

            _chatUI = Hud.Render.RegisterUiElement("Root.NormalLayer.chatentry_dialog_backgroundScreen.chatentry_content.chat_editline", null, null);
        }

        #endregion

        #region Settings Panel

        public override void DrawSettings(IController hud, RectangleF rect, Dictionary<string, RectangleF> clickAreas, int scrollOffset)
        {
            float x = rect.X, y = rect.Y, w = rect.Width;

            // Status indicator
            string statusText = Running ? "● RUNNING" : "○ STOPPED";
            var statusFont = Running ? (HasCore ? Core.FontSuccess : _runningFont) : (HasCore ? Core.FontError : _stoppedFont);
            var statusLayout = statusFont.GetTextLayout(statusText);
            statusFont.DrawText(statusLayout, x, y);
            y += statusLayout.Metrics.Height + 10;

            // Mode section
            y += DrawSettingsHeader(x, y, "Mode Settings");
            y += 8;

            // Mode toggle
            string modeText = IsPushMode ? "PUSH Mode" : "SPEED Mode";
            y += DrawToggleSetting(x, y, w, modeText, IsPushMode, clickAreas, "toggle_mode");

            // Oculus detection
            y += DrawToggleSetting(x, y, w, "Oculus Detection", EnableOculusDetection, clickAreas, "toggle_oculus");

            // Hide tip when off
            y += DrawToggleSetting(x, y, w, "Hide When Stopped", IsHideTip, clickAreas, "toggle_hide");

            y += 12;

            // Combat section
            y += DrawSettingsHeader(x, y, "Combat Settings");
            y += 8;

            // Enemy thresholds
            y += DrawSelectorSetting(x, y, w, "Min Pack Size", MinEnemiesForNovaNuke.ToString(), clickAreas, "sel_packsize");
            y += DrawSelectorSetting(x, y, w, "Nova Count", DeathNovaSpamCount.ToString(), clickAreas, "sel_novacount");
            y += DrawSelectorSetting(x, y, w, "Detection Range", EnemyDetectionRange.ToString("F0"), clickAreas, "sel_range");

            y += 12;

            // Timing section
            y += DrawSettingsHeader(x, y, "Timing (ms)");
            y += 8;

            y += DrawSelectorSetting(x, y, w, "Nova Delay", DeathNovaDelay.ToString(), clickAreas, "sel_novadelay");
            y += DrawSelectorSetting(x, y, w, "Channel Time", SiphonChannelTime.ToString(), clickAreas, "sel_channel");

            y += 16;

            // Info
            y += DrawSettingsHint(x, y, "[F1] Toggle • [F2] Mode • [F3] Force Nuke");
        }

        public override void HandleSettingsClick(string clickId)
        {
            switch (clickId)
            {
                case "toggle_mode":
                    IsPushMode = !IsPushMode;
                    ResetCombatCycle();
                    break;
                case "toggle_oculus":
                    EnableOculusDetection = !EnableOculusDetection;
                    break;
                case "toggle_hide":
                    IsHideTip = !IsHideTip;
                    break;
                case "sel_packsize_prev":
                    MinEnemiesForNovaNuke = Math.Max(1, MinEnemiesForNovaNuke - 1);
                    break;
                case "sel_packsize_next":
                    MinEnemiesForNovaNuke = Math.Min(15, MinEnemiesForNovaNuke + 1);
                    break;
                case "sel_novacount_prev":
                    DeathNovaSpamCount = Math.Max(1, DeathNovaSpamCount - 1);
                    break;
                case "sel_novacount_next":
                    DeathNovaSpamCount = Math.Min(20, DeathNovaSpamCount + 1);
                    break;
                case "sel_range_prev":
                    EnemyDetectionRange = Math.Max(20, EnemyDetectionRange - 5);
                    break;
                case "sel_range_next":
                    EnemyDetectionRange = Math.Min(100, EnemyDetectionRange + 5);
                    break;
                case "sel_novadelay_prev":
                    DeathNovaDelay = Math.Max(50, DeathNovaDelay - 25);
                    break;
                case "sel_novadelay_next":
                    DeathNovaDelay = Math.Min(500, DeathNovaDelay + 25);
                    break;
                case "sel_channel_prev":
                    SiphonChannelTime = Math.Max(100, SiphonChannelTime - 50);
                    break;
                case "sel_channel_next":
                    SiphonChannelTime = Math.Min(1000, SiphonChannelTime + 50);
                    break;
            }

            SavePluginSettings();
        }

        // Settings persistence
        protected override object GetSettingsObject() => new MacroSettings
        {
            IsPushMode = this.IsPushMode,
            IsHideTip = this.IsHideTip,
            EnableOculusDetection = this.EnableOculusDetection,
            MinEnemiesForNovaNuke = this.MinEnemiesForNovaNuke,
            DeathNovaSpamCount = this.DeathNovaSpamCount,
            DeathNovaDelay = this.DeathNovaDelay,
            SiphonChannelTime = this.SiphonChannelTime,
            EnemyDetectionRange = this.EnemyDetectionRange
        };

        protected override void ApplySettingsObject(object settings)
        {
            if (settings is MacroSettings s)
            {
                IsPushMode = s.IsPushMode;
                IsHideTip = s.IsHideTip;
                EnableOculusDetection = s.EnableOculusDetection;
                MinEnemiesForNovaNuke = s.MinEnemiesForNovaNuke;
                DeathNovaSpamCount = s.DeathNovaSpamCount;
                DeathNovaDelay = s.DeathNovaDelay;
                SiphonChannelTime = s.SiphonChannelTime;
                EnemyDetectionRange = s.EnemyDetectionRange;
            }
        }

        private class MacroSettings : PluginSettingsBase
        {
            public bool IsPushMode { get; set; }
            public bool IsHideTip { get; set; }
            public bool EnableOculusDetection { get; set; }
            public int MinEnemiesForNovaNuke { get; set; }
            public int DeathNovaSpamCount { get; set; }
            public int DeathNovaDelay { get; set; }
            public int SiphonChannelTime { get; set; }
            public float EnemyDetectionRange { get; set; }
        }

        #endregion

        #region Key Handler

        public void OnKeyEvent(IKeyEvent keyEvent)
        {
            if (!Hud.Game.IsInGame) return;
            if (!Enabled) return;  // Respect Core enable state
            if (!_isNecromancer) return;
            if (Hud.Inventory.InventoryMainUiElement.Visible) return;

            if (ToggleKeyEvent.Matches(keyEvent) && keyEvent.IsPressed)
            {
                if (Running) StopMacro();
                else StartMacro();
            }

            if (ModeKeyEvent.Matches(keyEvent) && keyEvent.IsPressed)
            {
                IsPushMode = !IsPushMode;
                ResetCombatCycle();
            }

            if (ForceNukeKeyEvent.Matches(keyEvent) && keyEvent.IsPressed)
            {
                if (Running) _forceNukeRequested = true;
            }
        }

        #endregion

        #region AfterCollect

        public void AfterCollect()
        {
            if (!Hud.Game.IsInGame) return;
            if (!Enabled) return;  // Respect Core enable state

            _isNecromancer = Hud.Game.Me.HeroClassDefinition.HeroClass == HeroClass.Necromancer;
            if (!_isNecromancer)
            {
                if (Running) StopMacro();
                return;
            }

            FindSkills();

            // Update enemy counts
            _nearbyEnemyCount = Hud.Game.AliveMonsters.Count(m => m.CentralXyDistanceToMe <= EnemyDetectionRange);
            _closeEnemyCount = Hud.Game.AliveMonsters.Count(m => m.CentralXyDistanceToMe <= CloseRange);
            _bloodtideStacks = Math.Min(10, Hud.Game.AliveMonsters.Count(m => m.CentralXyDistanceToMe <= BloodtideRange));

            // Check Oculus
            if (EnableOculusDetection)
                _isInOculusCircle = Hud.Game.Me.Powers.BuffIsActive(_oculusRingSno);

            // Check if we have Simulacrum equipped
            _hasSimulacrum = _skillSimulacrum != null;

            if (!Running) return;
            if (ShouldPauseMacro()) return;

            UpdateCombatState();
            ProcessMacro();
        }

        #endregion

        #region Combat State & Processing

        private void UpdateCombatState()
        {
            bool hasEnemies = _nearbyEnemyCount >= 1;

            if (hasEnemies)
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
            _novasPlaced = 0;
            _phaseTimer.Restart();
            _novaTimer.Restart();
        }

        private void FindSkills()
        {
            _skillSiphonBlood = null;
            _skillDeathNova = null;
            _skillBoneArmor = null;
            _skillSimulacrum = null;
            _skillFrailty = null;
            _skillBloodRush = null;

            foreach (var skill in Hud.Game.Me.Powers.UsedSkills)
            {
                var sno = skill.SnoPower.Sno;
                
                if (sno == Hud.Sno.SnoPowers.Necromancer_SiphonBlood.Sno)
                    _skillSiphonBlood = skill;
                else if (sno == Hud.Sno.SnoPowers.Necromancer_DeathNova.Sno)
                    _skillDeathNova = skill;
                else if (sno == Hud.Sno.SnoPowers.Necromancer_BoneArmor.Sno)
                    _skillBoneArmor = skill;
                else if (sno == Hud.Sno.SnoPowers.Necromancer_Simulacrum.Sno)
                    _skillSimulacrum = skill;
                else if (sno == Hud.Sno.SnoPowers.Necromancer_Frailty.Sno)
                    _skillFrailty = skill;
                else if (sno == Hud.Sno.SnoPowers.Necromancer_BloodRush.Sno)
                    _skillBloodRush = skill;
            }
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
            // Emergency Blood Rush
            if (Hud.Game.Me.Defense.HealthPct < EmergencyBloodRushHealthPct)
            {
                if (_skillBloodRush != null && !_skillBloodRush.IsOnCooldown)
                {
                    Hud.Interaction.DoAction(_skillBloodRush.Key);
                    return;
                }
            }

            // Maintain Simulacrum if equipped
            if (_hasSimulacrum)
            {
                MaintainSimulacrum();
            }

            // Combat or Movement
            if (_isInCombat)
            {
                if (IsPushMode)
                    ProcessPushMode();
                else
                    ProcessSpeedMode();
            }
            else
            {
                ProcessMovement();
            }
        }

        private void ProcessMovement()
        {
            if (_movementTimer.IsRunning && _movementTimer.ElapsedMilliseconds < MovementDelay)
                return;

            Hud.Interaction.DoAction(ActionKey.Move);
            _movementTimer.Restart();
        }

        private void ProcessSpeedMode()
        {
            RefreshBoneArmor();

            if (_skillSiphonBlood != null)
            {
                Hud.Interaction.DoAction(_skillSiphonBlood.Key);
            }
        }

        private void ProcessPushMode()
        {
            bool hasLargePack = _closeEnemyCount >= MinEnemiesForNovaNuke;
            bool hasElite = Hud.Game.AliveMonsters.Any(m => m.IsElite && m.CentralXyDistanceToMe <= CloseRange);
            bool isBossFight = Hud.Game.AliveMonsters.Any(m => m.SnoMonster.Priority == MonsterPriority.boss && m.CentralXyDistanceToMe <= CloseRange);

            if (_forceNukeRequested)
            {
                StartNukeSequence();
                _forceNukeRequested = false;
                return;
            }

            switch (_phase)
            {
                case MacroPhase.Idle:
                    if (isBossFight)
                    {
                        _phase = MacroPhase.Channeling;
                        _phaseTimer.Restart();
                    }
                    else if (hasLargePack || hasElite)
                    {
                        StartNukeSequence();
                    }
                    else
                    {
                        RefreshBoneArmor();
                        if (_skillSiphonBlood != null)
                        {
                            Hud.Interaction.DoAction(_skillSiphonBlood.Key);
                        }
                    }
                    break;

                case MacroPhase.BoneArmor:
                    ProcessBoneArmorPhase();
                    break;

                case MacroPhase.Nuking:
                    ProcessNukingPhase();
                    break;

                case MacroPhase.Channeling:
                    ProcessChannelingPhase();
                    break;
            }
        }

        private void StartNukeSequence()
        {
            _phase = MacroPhase.BoneArmor;
            _novasPlaced = 0;
            _phaseTimer.Restart();
        }

        private void ProcessBoneArmorPhase()
        {
            if (_skillBoneArmor != null && !_skillBoneArmor.IsOnCooldown)
            {
                Hud.Interaction.DoAction(_skillBoneArmor.Key);
            }

            if (_phaseTimer.ElapsedMilliseconds >= BoneArmorWaitTime)
            {
                _phase = MacroPhase.Nuking;
                _phaseTimer.Restart();
                _novaTimer.Restart();
            }
        }

        private void ProcessNukingPhase()
        {
            if (_skillDeathNova == null)
            {
                _phase = MacroPhase.Channeling;
                _phaseTimer.Restart();
                return;
            }

            if (_novaTimer.ElapsedMilliseconds >= DeathNovaDelay)
            {
                Hud.Interaction.DoAction(_skillDeathNova.Key);
                _novasPlaced++;
                _novaTimer.Restart();
            }

            if (_novasPlaced >= DeathNovaSpamCount)
            {
                _phase = MacroPhase.Channeling;
                _phaseTimer.Restart();
            }
        }

        private void ProcessChannelingPhase()
        {
            if (_skillSiphonBlood != null)
            {
                Hud.Interaction.DoAction(_skillSiphonBlood.Key);
            }

            if (_phaseTimer.ElapsedMilliseconds >= SiphonChannelTime)
            {
                _phase = MacroPhase.Idle;
            }
        }

        private void MaintainSimulacrum()
        {
            if (_skillSimulacrum == null || _skillSimulacrum.IsOnCooldown) return;

            var buff = Hud.Game.Me.Powers.GetBuff(_simulacrumSno);
            bool needsRefresh = buff == null || buff.IconCounts[0] <= 0;
            
            if (needsRefresh)
            {
                var stoneGauntletsBuff = Hud.Game.Me.Powers.GetBuff(_stoneGauntletsSno);
                if (stoneGauntletsBuff != null)
                {
                    if (stoneGauntletsBuff.IconCounts[0] >= 5)
                    {
                        Hud.Interaction.DoAction(_skillSimulacrum.Key);
                    }
                }
                else
                {
                    Hud.Interaction.DoAction(_skillSimulacrum.Key);
                }
            }
        }

        private void RefreshBoneArmor()
        {
            if (_skillBoneArmor == null || _skillBoneArmor.IsOnCooldown) return;
            if (_closeEnemyCount == 0) return;

            var buff = Hud.Game.Me.Powers.GetBuff(_boneArmorSno);
            double buffTime = buff != null && buff.IconCounts[0] > 0 ? buff.TimeLeftSeconds[0] : 0;
            
            if (buffTime < BoneArmorRefreshTime)
            {
                Hud.Interaction.DoAction(_skillBoneArmor.Key);
            }
        }

        private int GetFuneraryPickStacks()
        {
            var buff = Hud.Game.Me.Powers.GetBuff(_funeraryPickSno);
            return buff?.IconCounts[0] ?? 0;
        }

        private void StartMacro()
        {
            Running = true;
            ResetCombatCycle();
            _isInCombat = false;
            _forceNukeRequested = false;
            _movementTimer.Restart();
            _combatExitTimer.Restart();
            
            SetCoreStatus($"{PluginName} started", StatusType.Success);
        }

        private void StopMacro()
        {
            Running = false;
            ResetCombatCycle();
            _isInCombat = false;
            _forceNukeRequested = false;
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
            if (!Hud.Game.IsInGame) return;
            if (!Enabled) return;  // Respect Core enable state
            if (!_isNecromancer) return;

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

            // Use Core fonts if available, fall back to legacy
            var titleFont = HasCore ? Core.FontTitle : _titleFont;
            var runningFontToUse = HasCore ? Core.FontSuccess : _runningFont;
            var modeFontToUse = HasCore ? Core.FontWarning : _modeFont;
            var stoppedFontToUse = HasCore ? Core.FontError : _stoppedFont;
            var tipFontToUse = HasCore ? Core.FontMuted : _tipFont;
            var movingFontToUse = HasCore ? Core.FontAccent : _movingFont;

            if (Running)
            {
                // Title
                var layout1 = titleFont.GetTextLayout("💀 LoD Blood Nova");
                titleFont.DrawText(layout1, centerX - layout1.Metrics.Width / 2, baseY);
                baseY += layout1.Metrics.Height + 2;

                if (_isInCombat)
                {
                    var layout2 = runningFontToUse.GetTextLayout($"● COMBAT ({_closeEnemyCount}/{_nearbyEnemyCount})");
                    runningFontToUse.DrawText(layout2, centerX - layout2.Metrics.Width / 2, baseY);
                    baseY += layout2.Metrics.Height + 2;

                    string modeText = IsPushMode ? "PUSH (Manual Nova)" : "SPEED (Channel)";
                    var modeLayout = modeFontToUse.GetTextLayout(modeText);
                    modeFontToUse.DrawText(modeLayout, centerX - modeLayout.Metrics.Width / 2, baseY);
                    baseY += modeLayout.Metrics.Height + 2;

                    if (IsPushMode)
                    {
                        string phaseText = GetPhaseText();
                        var phaseLayout = _phaseFont.GetTextLayout(phaseText);
                        _phaseFont.DrawText(phaseLayout, centerX - phaseLayout.Metrics.Width / 2, baseY);
                        baseY += phaseLayout.Metrics.Height + 2;
                    }

                    int stacks = GetFuneraryPickStacks();
                    string stackText = $"Funerary: {stacks}/10";
                    var stackLayout = _stackFont.GetTextLayout(stackText);
                    _stackFont.DrawText(stackLayout, centerX - stackLayout.Metrics.Width / 2, baseY);
                    baseY += stackLayout.Metrics.Height + 2;

                    string bloodtideText = $"Bloodtide: {_bloodtideStacks}/10";
                    var bloodtideLayout = _bloodtideFont.GetTextLayout(bloodtideText);
                    _bloodtideFont.DrawText(bloodtideLayout, centerX - bloodtideLayout.Metrics.Width / 2, baseY);
                    baseY += bloodtideLayout.Metrics.Height + 2;

                    if (EnableOculusDetection && _isInOculusCircle)
                    {
                        var oculusLayout = _oculusFont.GetTextLayout("★ OCULUS +85%");
                        _oculusFont.DrawText(oculusLayout, centerX - oculusLayout.Metrics.Width / 2, baseY);
                    }
                }
                else
                {
                    var layout2 = movingFontToUse.GetTextLayout("● MOVING");
                    movingFontToUse.DrawText(layout2, centerX - layout2.Metrics.Width / 2, baseY);
                    baseY += layout2.Metrics.Height + 2;

                    string modeText = IsPushMode ? "[F2] PUSH" : "[F2] SPEED";
                    var modeLayout = tipFontToUse.GetTextLayout(modeText);
                    tipFontToUse.DrawText(modeLayout, centerX - modeLayout.Metrics.Width / 2, baseY);
                }
            }
            else
            {
                var layout1 = titleFont.GetTextLayout("💀 LoD Blood Nova");
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
                case MacroPhase.BoneArmor: return "Stunning...";
                case MacroPhase.Nuking: return $"NOVA {_novasPlaced}/{DeathNovaSpamCount}";
                case MacroPhase.Channeling: return "Channeling...";
                default: return "";
            }
        }

        #endregion
    }

    internal enum MacroPhase
    {
        Idle,
        BoneArmor,
        Nuking,
        Channeling
    }
}
