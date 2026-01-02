namespace Turbo.Plugins.Custom.LoDDeathNovaMacro
{
    using System;
    using System.Linq;
    using System.Windows.Forms;
    using SharpDX.DirectInput;
    using Turbo.Plugins.Default;

    /// <summary>
    /// LoD Death Nova Necromancer Macro - Ultimate Version
    /// 
    /// Build: https://maxroll.gg/d3/guides/lod-death-nova-necromancer-guide
    /// 
    /// IMPORTANT BUILD MECHANICS:
    /// - Death Nova is NOT cast manually! It's on bar for the Blood Nova rune only
    /// - Iron Rose auto-casts Blood Nova while channeling Siphon Blood
    /// - Simulacrums also proc Blood Nova (and their casts DO proc Area Damage!)
    /// - Simulacrums are the MAIN damage source in large pulls
    /// - Bone Armor applies STUN for Krysbin's 300% bonus
    /// - Funerary Pick stacks (from Siphon Blood) give 200% damage
    /// - Frailty (Aura of Frailty) is used for Dayntee's DR and finishing enemies
    /// 
    /// Skills Setup:
    /// - Left Click: Siphon Blood (Power Shift) - MAIN DAMAGE via Iron Rose procs
    /// - Right Click: Death Nova (Blood Nova) - DO NOT USE, just for rune
    /// - 1: Bone Armor (Dislocation) - Stun for Krysbin's + DR
    /// - 2: Simulacrum (Blood and Bone) - Permanent with Haunted Visions
    /// - 3: Frailty (Aura of Frailty) - Auto-curse + Dayntee's DR
    /// - 4: Blood Rush (Potency) - Mobility
    /// 
    /// F1 = Toggle macro ON/OFF
    /// F2 = Switch between SPEED mode and PUSH mode
    /// F3 = Force Nuke (manual CoE sync)
    /// 
    /// SPEED MODE: Continuous Siphon Blood channeling, auto Bone Armor
    /// PUSH MODE: CoE Physical window nukes, Bone Armor for Krysbin's stun
    /// </summary>
    public class LoDDeathNovaMacroPlugin : BasePlugin, IInGameTopPainter, IKeyEventHandler, IAfterCollectHandler
    {
        #region Settings

        public IKeyEvent ToggleKeyEvent { get; set; }
        public IKeyEvent ModeKeyEvent { get; set; }
        public IKeyEvent ForceNukeKeyEvent { get; set; }
        public bool IsHideTip { get; set; } = false;

        /// <summary>
        /// CoE Physical element icon index (6 = Physical for Necro)
        /// Necromancer CoE: Cold=2, Physical=6, Poison=7
        /// </summary>
        public int PhysicalCoEIconIndex { get; set; } = 6;

        /// <summary>
        /// Seconds before Physical CoE to prepare (position Simulacrums)
        /// </summary>
        public float PrePhysicalPrepSeconds { get; set; } = 1.0f;

        /// <summary>
        /// Minimum Funerary Pick stacks before nuking (0-10, from Siphon Blood)
        /// Each stack = 20% damage, 10 stacks = 200% bonus
        /// </summary>
        public int MinFuneraryPickStacks { get; set; } = 5;

        /// <summary>
        /// Bone Armor refresh threshold (seconds remaining)
        /// Bone Armor gives up to 30% DR from stacks
        /// </summary>
        public float BoneArmorRefreshTime { get; set; } = 5.0f;

        /// <summary>
        /// Range to detect enemies
        /// </summary>
        public float EnemyDetectionRange { get; set; } = 60f;

        /// <summary>
        /// Range to detect elites for priority targeting
        /// </summary>
        public float EliteDetectionRange { get; set; } = 40f;

        /// <summary>
        /// Health percent to use Blood Rush defensively
        /// </summary>
        public float EmergencyBloodRushHealthPct { get; set; } = 0.35f;

        /// <summary>
        /// Minimum enemies nearby for CoE nuke in push mode
        /// More enemies = more Bloodtide Blade stacks = more damage
        /// </summary>
        public int MinEnemiesForCoENuke { get; set; } = 3;

        /// <summary>
        /// Delay between skill casts (ms)
        /// </summary>
        public int CastDelay { get; set; } = 50;

        /// <summary>
        /// Enable Oculus Ring circle detection for positioning hints
        /// </summary>
        public bool EnableOculusDetection { get; set; } = true;

        /// <summary>
        /// Bloodtide Blade nearby enemy range (for stack counting)
        /// Bloodtide adds 400% Death Nova damage per enemy, up to 10 = 4000%!
        /// </summary>
        public float BloodtideRange { get; set; } = 25f;

        #endregion

        #region Private Fields

        public bool Running { get; private set; }
        public bool IsPushMode { get; private set; } = false;
        private bool _isNecromancer = false;
        private bool _forceNukeRequested = false;
        private bool _isInOculusCircle = false;

        // Skill references
        private IPlayerSkill _skillSiphonBlood;   // Left click - MAIN DAMAGE
        private IPlayerSkill _skillDeathNova;     // Right click - DO NOT USE (for rune only)
        private IPlayerSkill _skillBoneArmor;     // Slot 1 - stun + DR
        private IPlayerSkill _skillSimulacrum;    // Slot 2 - permanent clones
        private IPlayerSkill _skillFrailty;       // Slot 3 - curse (default)
        private IPlayerSkill _skillBloodRush;     // Slot 4 - mobility
        private IPlayerSkill _skillDecrepify;     // Alternative curse
        private IPlayerSkill _skillLeech;         // Alternative curse

        // SNO Powers for buff checking
        private uint _boneArmorSno;
        private uint _simulacrumSno;
        private uint _funeraryPickSno;
        private uint _coeSno;
        private uint _oculusRingSno;
        private uint _stoneGauntletsSno;

        // Timers
        private IWatch _actionTimer;
        private IWatch _siphonTimer;
        private IWatch _cycleTimer;
        private IWatch _lastNukeTimer;

        // State
        private MacroPhase _phase = MacroPhase.Idle;
        private bool _wasInPhysicalCoE = false;
        private int _bloodtideStacks = 0;

        // Fonts
        private IFont _titleFont;
        private IFont _runningFont;
        private IFont _modeFont;
        private IFont _stoppedFont;
        private IFont _tipFont;
        private IFont _coEFont;
        private IFont _coEActiveFont;
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

            _actionTimer = Hud.Time.CreateWatch();
            _siphonTimer = Hud.Time.CreateWatch();
            _cycleTimer = Hud.Time.CreateWatch();
            _lastNukeTimer = Hud.Time.CreateWatch();

            // Get SNO powers
            _boneArmorSno = Hud.Sno.SnoPowers.Necromancer_BoneArmor.Sno;
            _simulacrumSno = Hud.Sno.SnoPowers.Necromancer_Simulacrum.Sno;
            _funeraryPickSno = 476587; // Funerary Pick item buff
            _coeSno = 430674; // Convention of Elements
            _oculusRingSno = 402461; // Oculus Ring ground effect buff
            _stoneGauntletsSno = 318820; // Stone Gauntlets buff

            // UI Fonts - green theme for Necro
            _titleFont = Hud.Render.CreateFont("tahoma", 8, 255, 150, 255, 150, true, false, 180, 0, 0, 0, true);
            _runningFont = Hud.Render.CreateFont("tahoma", 7.5f, 255, 0, 255, 0, true, false, 160, 0, 0, 0, true);
            _modeFont = Hud.Render.CreateFont("tahoma", 7.5f, 255, 255, 200, 0, true, false, 160, 0, 0, 0, true);
            _stoppedFont = Hud.Render.CreateFont("tahoma", 7.5f, 255, 255, 100, 100, true, false, 160, 0, 0, 0, true);
            _tipFont = Hud.Render.CreateFont("tahoma", 7, 200, 180, 180, 180, false, false, 140, 0, 0, 0, true);
            _coEFont = Hud.Render.CreateFont("tahoma", 7.5f, 255, 200, 150, 255, true, false, 160, 0, 0, 0, true);
            _coEActiveFont = Hud.Render.CreateFont("tahoma", 7.5f, 255, 255, 50, 255, true, false, 160, 0, 0, 0, true);
            _stackFont = Hud.Render.CreateFont("tahoma", 7, 255, 255, 220, 100, false, false, 140, 0, 0, 0, true);
            _bloodtideFont = Hud.Render.CreateFont("tahoma", 7, 255, 255, 100, 100, false, false, 140, 0, 0, 0, true);
            _oculusFont = Hud.Render.CreateFont("tahoma", 7, 255, 255, 255, 100, true, false, 140, 0, 0, 0, true);

            // Panel styling - dark green for Necro
            _panelBrush = Hud.Render.CreateBrush(235, 15, 30, 15, 0);
            _borderBrush = Hud.Render.CreateBrush(200, 50, 100, 50, 1f);
            _accentOnBrush = Hud.Render.CreateBrush(255, 80, 200, 80, 0);
            _accentOffBrush = Hud.Render.CreateBrush(255, 200, 80, 80, 0);

            _chatUI = Hud.Render.RegisterUiElement("Root.NormalLayer.chatentry_dialog_backgroundScreen.chatentry_content.chat_editline", null, null);
        }

        public void OnKeyEvent(IKeyEvent keyEvent)
        {
            if (!Hud.Game.IsInGame) return;
            if (!_isNecromancer) return;
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
                IsPushMode = !IsPushMode;
                _phase = MacroPhase.Idle;
            }

            // F3 - Force Nuke
            if (ForceNukeKeyEvent.Matches(keyEvent) && keyEvent.IsPressed)
            {
                if (Running) _forceNukeRequested = true;
            }
        }

        public void AfterCollect()
        {
            if (!Hud.Game.IsInGame) return;

            _isNecromancer = Hud.Game.Me.HeroClassDefinition.HeroClass == HeroClass.Necromancer;
            if (!_isNecromancer)
            {
                if (Running) StopMacro();
                return;
            }

            FindSkills();

            // Update Bloodtide stacks count (enemies within 25 yards)
            _bloodtideStacks = Hud.Game.AliveMonsters.Count(m => m.CentralXyDistanceToMe <= BloodtideRange);
            if (_bloodtideStacks > 10) _bloodtideStacks = 10;

            // Check Oculus Ring buff
            if (EnableOculusDetection)
            {
                _isInOculusCircle = Hud.Game.Me.Powers.BuffIsActive(_oculusRingSno);
            }

            if (!Running) return;

            if (ShouldPauseMacro()) return;

            ProcessMacro();
        }

        private void FindSkills()
        {
            _skillSiphonBlood = null;
            _skillDeathNova = null;
            _skillBoneArmor = null;
            _skillSimulacrum = null;
            _skillFrailty = null;
            _skillBloodRush = null;
            _skillDecrepify = null;
            _skillLeech = null;

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
                else if (sno == Hud.Sno.SnoPowers.Necromancer_Decrepify.Sno)
                    _skillDecrepify = skill;
                else if (sno == Hud.Sno.SnoPowers.Necromancer_Leech.Sno)
                    _skillLeech = skill;
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
            // Emergency Blood Rush if health is critical
            if (Hud.Game.Me.Defense.HealthPct < EmergencyBloodRushHealthPct)
            {
                if (_skillBloodRush != null && !_skillBloodRush.IsOnCooldown)
                {
                    Hud.Interaction.DoAction(_skillBloodRush.Key);
                    return;
                }
            }

            // 1. ALWAYS maintain Simulacrum (permanent with Haunted Visions)
            MaintainSimulacrum();

            // 2. Check for enemies
            bool hasEnemies = Hud.Game.AliveMonsters.Any(m => m.CentralXyDistanceToMe <= EnemyDetectionRange);
            bool hasElites = Hud.Game.AliveMonsters.Any(m => m.IsElite && m.CentralXyDistanceToMe <= EliteDetectionRange);
            int nearbyEnemyCount = Hud.Game.AliveMonsters.Count(m => m.CentralXyDistanceToMe <= 20f);

            if (!hasEnemies)
            {
                _phase = MacroPhase.Idle;
                _forceNukeRequested = false;
                return;
            }

            // 3. Execute based on mode
            if (IsPushMode)
            {
                ProcessPushMode(hasElites, nearbyEnemyCount);
            }
            else
            {
                ProcessSpeedMode(hasElites);
            }
        }

        private void MaintainSimulacrum()
        {
            if (_skillSimulacrum == null || _skillSimulacrum.IsOnCooldown) return;

            var buff = Hud.Game.Me.Powers.GetBuff(_simulacrumSno);
            bool needsRefresh = buff == null || buff.IconCounts[0] <= 0;
            
            if (needsRefresh)
            {
                // If using Stone Gauntlets, wait for 5 stacks before summoning
                // Simulacrums snapshot your armor at summon time!
                var stoneGauntletsBuff = Hud.Game.Me.Powers.GetBuff(_stoneGauntletsSno);
                if (stoneGauntletsBuff != null)
                {
                    // Has Stone Gauntlets - wait for enough stacks
                    if (stoneGauntletsBuff.IconCounts[0] >= 5)
                    {
                        Hud.Interaction.DoAction(_skillSimulacrum.Key);
                    }
                    // else: wait for more stacks
                }
                else
                {
                    // No Stone Gauntlets - summon immediately
                    Hud.Interaction.DoAction(_skillSimulacrum.Key);
                }
            }
        }

        private void ProcessSpeedMode(bool hasElites)
        {
            // SPEED MODE: Continuous channeling
            // Iron Rose will auto-cast Blood Nova while we channel Siphon Blood
            
            bool hasEnemiesNearby = Hud.Game.AliveMonsters.Any(m => m.CentralXyDistanceToMe <= 20f);
            
            // Maintain Bone Armor for DR (30% from 10 stacks)
            if (hasEnemiesNearby && _skillBoneArmor != null && !_skillBoneArmor.IsOnCooldown)
            {
                var buff = Hud.Game.Me.Powers.GetBuff(_boneArmorSno);
                double buffTime = buff != null && buff.IconCounts[0] > 0 ? buff.TimeLeftSeconds[0] : 0;
                
                // Refresh if low or if we have elites (for Krysbin's stun)
                if (buffTime < BoneArmorRefreshTime || hasElites)
                {
                    Hud.Interaction.DoAction(_skillBoneArmor.Key);
                    return;
                }
            }

            // Channel Siphon Blood - this is the main damage!
            // Iron Rose will auto-cast Blood Nova
            // Simulacrums will also proc Blood Nova (with Area Damage!)
            if (_skillSiphonBlood != null)
            {
                Hud.Interaction.DoAction(_skillSiphonBlood.Key);
            }
        }

        private void ProcessPushMode(bool hasElites, int nearbyEnemyCount)
        {
            // PUSH MODE: CoE-synchronized nukes for maximum damage
            // Key: Bone Armor STUN during Physical CoE for Krysbin's 300%
            
            var coEState = GetCoEState();

            // Force nuke request
            if (_forceNukeRequested)
            {
                ExecuteNukeSequence();
                _forceNukeRequested = false;
                _lastNukeTimer.Restart();
                return;
            }

            bool hasEnemiesNearby = Hud.Game.AliveMonsters.Any(m => m.CentralXyDistanceToMe <= 20f);

            switch (coEState)
            {
                case CoEState.PrePhysical:
                    // PREPARE: Stack Funerary Pick, position Simulacrums
                    // Channel Siphon Blood to build stacks
                    if (_skillSiphonBlood != null && GetFuneraryPickStacks() < 10)
                    {
                        Hud.Interaction.DoAction(_skillSiphonBlood.Key);
                    }
                    break;

                case CoEState.Physical:
                    // NUKE PHASE!
                    // Only nuke if we have enough enemies for Bloodtide Blade
                    if (nearbyEnemyCount >= MinEnemiesForCoENuke || hasElites)
                    {
                        ExecuteNukeSequence();
                    }
                    else
                    {
                        // Not enough enemies - maintain Funerary stacks
                        if (_skillSiphonBlood != null)
                        {
                            Hud.Interaction.DoAction(_skillSiphonBlood.Key);
                        }
                    }
                    break;

                case CoEState.PostPhysical:
                case CoEState.Other:
                default:
                    // DOWNTIME: Maintain Funerary Pick stacks, kite/position
                    // Light channeling to maintain stacks (they last 3 seconds)
                    if (_skillSiphonBlood != null && _cycleTimer.ElapsedMilliseconds > 800)
                    {
                        // Tap siphon to refresh Funerary stacks
                        Hud.Interaction.DoAction(_skillSiphonBlood.Key);
                        _cycleTimer.Restart();
                    }
                    
                    // Also maintain Bone Armor DR during downtime
                    if (hasEnemiesNearby && _skillBoneArmor != null && !_skillBoneArmor.IsOnCooldown)
                    {
                        var buff = Hud.Game.Me.Powers.GetBuff(_boneArmorSno);
                        double buffTime = buff != null && buff.IconCounts[0] > 0 ? buff.TimeLeftSeconds[0] : 0;
                        if (buffTime < BoneArmorRefreshTime)
                        {
                            Hud.Interaction.DoAction(_skillBoneArmor.Key);
                        }
                    }
                    break;
            }
        }

        private void ExecuteNukeSequence()
        {
            // OPTIMAL NUKE SEQUENCE:
            // 1. Bone Armor (STUN for Krysbin's 300% bonus)
            // 2. Channel Siphon Blood (Iron Rose procs Blood Nova)
            //    - Your Blood Nova procs (no Area Damage)
            //    - Simulacrum Blood Novas proc (WITH Area Damage!)
            
            bool hasEnemiesNearby = Hud.Game.AliveMonsters.Any(m => m.CentralXyDistanceToMe <= 15f);
            
            // Cast Bone Armor for STUN (Krysbin's 300% bonus)
            if (_skillBoneArmor != null && !_skillBoneArmor.IsOnCooldown && hasEnemiesNearby)
            {
                Hud.Interaction.DoAction(_skillBoneArmor.Key);
            }

            // Channel Siphon Blood - Iron Rose procs Blood Nova automatically!
            if (_skillSiphonBlood != null)
            {
                Hud.Interaction.DoAction(_skillSiphonBlood.Key);
            }
        }

        private int GetFuneraryPickStacks()
        {
            var buff = Hud.Game.Me.Powers.GetBuff(_funeraryPickSno);
            return buff?.IconCounts[0] ?? 0;
        }

        private CoEState GetCoEState()
        {
            var buff = Hud.Game.Me.Powers.GetBuff(_coeSno);
            if (buff == null || buff.IconCounts[0] <= 0)
                return CoEState.None;

            // Check if Physical is active (IconIndex 6 for Physical on Necro)
            double physicalTimeLeft = buff.TimeLeftSeconds[PhysicalCoEIconIndex];
            
            if (physicalTimeLeft > 0)
            {
                _wasInPhysicalCoE = true;
                return CoEState.Physical;
            }
            
            // Check if we're about to enter Physical
            // Necro rotation: Cold (4s) -> Physical (4s) -> Poison (4s)
            double coldTimeLeft = buff.TimeLeftSeconds[2]; // Cold = index 2
            double poisonTimeLeft = buff.TimeLeftSeconds[7]; // Poison = index 7
            
            if (coldTimeLeft > 0 && coldTimeLeft <= PrePhysicalPrepSeconds)
            {
                return CoEState.PrePhysical;
            }
            
            if (_wasInPhysicalCoE && poisonTimeLeft > 0)
            {
                _wasInPhysicalCoE = false;
                return CoEState.PostPhysical;
            }
            
            return CoEState.Other;
        }

        private void StartMacro()
        {
            Running = true;
            _phase = MacroPhase.Idle;
            _forceNukeRequested = false;
            _actionTimer.Restart();
            _siphonTimer.Restart();
            _cycleTimer.Restart();
            _lastNukeTimer.Restart();
        }

        private void StopMacro()
        {
            Running = false;
            _phase = MacroPhase.Idle;
            _forceNukeRequested = false;
            _actionTimer.Stop();
            _siphonTimer.Stop();
            _cycleTimer.Stop();
        }

        public void PaintTopInGame(ClipState clipState)
        {
            if (clipState != ClipState.AfterClip) return;
            if (!Hud.Game.IsInGame) return;
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
            // Get player screen position for centered display below character
            var playerScreenPos = Hud.Game.Me.FloorCoordinate.ToScreenCoordinate();
            float centerX = playerScreenPos.X;
            float baseY = playerScreenPos.Y + 10; // Offset below character

            if (Running)
            {
                // Build status text lines
                string line1 = "LoD Blood Nova";
                string line2 = "● ACTIVE";
                string line3 = IsPushMode ? "PUSH (CoE)" : "SPEED";
                
                // Draw centered below character
                var layout1 = _titleFont.GetTextLayout(line1);
                _titleFont.DrawText(layout1, centerX - layout1.Metrics.Width / 2, baseY);
                baseY += layout1.Metrics.Height + 2;

                var layout2 = _runningFont.GetTextLayout(line2);
                _runningFont.DrawText(layout2, centerX - layout2.Metrics.Width / 2, baseY);
                baseY += layout2.Metrics.Height + 2;

                var layout3 = _modeFont.GetTextLayout(line3);
                _modeFont.DrawText(layout3, centerX - layout3.Metrics.Width / 2, baseY);
                baseY += layout3.Metrics.Height + 2;

                // CoE Status (Push mode only)
                if (IsPushMode)
                {
                    var coEState = GetCoEState();
                    string coEText;
                    IFont coETextFont;
                    
                    if (coEState == CoEState.Physical)
                    {
                        coEText = "★ PHYSICAL ★";
                        coETextFont = _coEActiveFont;
                    }
                    else if (coEState == CoEState.PrePhysical)
                    {
                        coEText = "PREPARE...";
                        coETextFont = _coEActiveFont;
                    }
                    else
                    {
                        coEText = "Waiting...";
                        coETextFont = _coEFont;
                    }
                    
                    var coELayout = coETextFont.GetTextLayout(coEText);
                    coETextFont.DrawText(coELayout, centerX - coELayout.Metrics.Width / 2, baseY);
                    baseY += coELayout.Metrics.Height + 2;
                }

                // Funerary Pick stacks
                int stacks = GetFuneraryPickStacks();
                string stackText = $"Funerary: {stacks}/10";
                var stackLayout = _stackFont.GetTextLayout(stackText);
                _stackFont.DrawText(stackLayout, centerX - stackLayout.Metrics.Width / 2, baseY);
                baseY += stackLayout.Metrics.Height + 2;

                // Bloodtide Blade stacks
                string bloodtideText = $"Bloodtide: {_bloodtideStacks}/10";
                var bloodtideLayout = _bloodtideFont.GetTextLayout(bloodtideText);
                _bloodtideFont.DrawText(bloodtideLayout, centerX - bloodtideLayout.Metrics.Width / 2, baseY);
                baseY += bloodtideLayout.Metrics.Height + 2;

                // Oculus Ring indicator
                if (EnableOculusDetection && _isInOculusCircle)
                {
                    var oculusLayout = _oculusFont.GetTextLayout("★ OCULUS +85%");
                    _oculusFont.DrawText(oculusLayout, centerX - oculusLayout.Metrics.Width / 2, baseY);
                }
            }
            else
            {
                // OFF state - minimal display
                var layout1 = _titleFont.GetTextLayout("LoD Blood Nova");
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
        Preparing,
        Nuking,
        Recovering
    }

    internal enum CoEState
    {
        None,
        Physical,
        PrePhysical,
        PostPhysical,
        Other
    }
}
