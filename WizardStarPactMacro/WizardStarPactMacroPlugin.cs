namespace Turbo.Plugins.Custom.WizardStarPactMacro
{
    using System;
    using System.Linq;
    using System.Windows.Forms;
    using SharpDX.DirectInput;
    using Turbo.Plugins.Default;

    /// <summary>
    /// Wizard Star Pact Macro - AGGRESSIVE MODE
    /// Weekly Challenge Rift Build - MAXIMUM DPS
    /// 
    /// Press F1 to toggle - unleashes full auto-combat
    /// </summary>
    public class WizardStarPactMacroPlugin : BasePlugin, IInGameTopPainter, IKeyEventHandler, IAfterCollectHandler
    {
        #region Settings

        public IKeyEvent ToggleKeyEvent { get; set; }
        public bool IsHideTip { get; set; } = false;

        /// <summary>
        /// Delay between actions in milliseconds (lower = faster)
        /// </summary>
        public int ActionDelay { get; set; } = 30;

        /// <summary>
        /// How often to refresh buffs in seconds
        /// </summary>
        public float BuffRefreshTime { get; set; } = 50f;

        /// <summary>
        /// Range to detect enemies for attacking
        /// </summary>
        public float AttackRange { get; set; } = 60f;

        /// <summary>
        /// Range for Spectral Blade (melee)
        /// </summary>
        public float MeleeRange { get; set; } = 20f;

        #endregion

        #region Private Fields

        public bool Running { get; private set; }
        private bool _isWizard = false;

        // Skill references
        private IPlayerSkill _skillSpectralBlade;  // Left click - primary
        private IPlayerSkill _skillHydra;          // Right click
        private IPlayerSkill _skillMeteor;         // Slot 1
        private IPlayerSkill _skillTeleport;       // Slot 2 - manual
        private IPlayerSkill _skillMagicWeapon;    // Slot 3
        private IPlayerSkill _skillStormArmor;     // Slot 4

        // Timers
        private IWatch _actionTimer;
        private IWatch _hydraTimer;
        private IWatch _bladeTimer;
        private IWatch _meteorTimer;

        // State
        private int _currentDynamoStacks = 0;
        private bool _hasArcaneDynamo = false;

        // Fonts
        private IFont _runningFont;
        private IFont _stoppedFont;
        private IFont _tipFont;
        private IFont _stackFont;

        // UI Elements for safety checks
        private IUiElement _chatUI;

        #endregion

        public WizardStarPactMacroPlugin()
        {
            Enabled = true;
        }

        public override void Load(IController hud)
        {
            base.Load(hud);

            ToggleKeyEvent = Hud.Input.CreateKeyEvent(true, Key.F1, false, false, false);

            _actionTimer = Hud.Time.CreateWatch();
            _hydraTimer = Hud.Time.CreateWatch();
            _bladeTimer = Hud.Time.CreateWatch();
            _meteorTimer = Hud.Time.CreateWatch();

            _runningFont = Hud.Render.CreateFont("tahoma", 9, 255, 0, 255, 0, true, false, 255, 0, 0, 0, true);
            _stoppedFont = Hud.Render.CreateFont("tahoma", 9, 255, 255, 200, 0, true, false, 255, 0, 0, 0, true);
            _tipFont = Hud.Render.CreateFont("tahoma", 7, 200, 180, 180, 180, false, false, 140, 0, 0, 0, true);
            _stackFont = Hud.Render.CreateFont("tahoma", 8, 255, 255, 100, 100, true, false, 200, 0, 0, 0, true);

            _chatUI = Hud.Render.RegisterUiElement("Root.NormalLayer.chatentry_dialog_backgroundScreen.chatentry_content.chat_editline", null, null);
        }

        public void OnKeyEvent(IKeyEvent keyEvent)
        {
            if (!Hud.Game.IsInGame) return;
            if (Hud.Game.Me.HeroClassDefinition.HeroClass != HeroClass.Wizard) return;
            if (Hud.Inventory.InventoryMainUiElement.Visible) return;

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
        }

        public void AfterCollect()
        {
            if (!Hud.Game.IsInGame) return;

            // Check if player is a Wizard
            _isWizard = Hud.Game.Me.HeroClassDefinition.HeroClass == HeroClass.Wizard;
            if (!_isWizard)
            {
                if (Running) StopMacro();
                return;
            }

            // Find skills every frame
            FindSkills();

            // Check for Arcane Dynamo passive
            _hasArcaneDynamo = Hud.Game.Me.Powers.UsedPassives.Any(p => p.Sno == Hud.Sno.SnoPowers.Wizard_Passive_ArcaneDynamo.Sno);

            if (!Running) return;

            // Safety checks
            if (ShouldPauseMacro())
            {
                return;
            }

            // AGGRESSIVE MACRO LOOP
            ProcessAggressiveMacro();
        }

        private void FindSkills()
        {
            _skillSpectralBlade = null;
            _skillHydra = null;
            _skillMeteor = null;
            _skillTeleport = null;
            _skillMagicWeapon = null;
            _skillStormArmor = null;

            foreach (var skill in Hud.Game.Me.Powers.UsedSkills)
            {
                var sno = skill.SnoPower.Sno;
                if (sno == Hud.Sno.SnoPowers.Wizard_SpectralBlade.Sno)
                    _skillSpectralBlade = skill;
                else if (sno == Hud.Sno.SnoPowers.Wizard_Hydra.Sno)
                    _skillHydra = skill;
                else if (sno == Hud.Sno.SnoPowers.Wizard_Meteor.Sno)
                    _skillMeteor = skill;
                else if (sno == Hud.Sno.SnoPowers.Wizard_Teleport.Sno)
                    _skillTeleport = skill;
                else if (sno == Hud.Sno.SnoPowers.Wizard_MagicWeapon.Sno)
                    _skillMagicWeapon = skill;
                else if (sno == Hud.Sno.SnoPowers.Wizard_StormArmor.Sno)
                    _skillStormArmor = skill;
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

        private void ProcessAggressiveMacro()
        {
            // Get Arcane Dynamo stacks
            var dynamoBuff = Hud.Game.Me.Powers.GetBuff(Hud.Sno.SnoPowers.Wizard_Passive_ArcaneDynamo.Sno);
            _currentDynamoStacks = (dynamoBuff != null && _hasArcaneDynamo) ? dynamoBuff.IconCounts[1] : 0;

            // Check for enemies
            bool hasEnemiesNearby = Hud.Game.AliveMonsters.Any(m => m.CentralXyDistanceToMe <= AttackRange);
            bool hasEnemiesMelee = Hud.Game.AliveMonsters.Any(m => m.CentralXyDistanceToMe <= MeleeRange);

            // PRIORITY 1: Keep buffs up (instant, no delay check)
            TryRefreshBuffsInstant();

            // Minimum action delay
            if (_actionTimer.ElapsedMilliseconds < ActionDelay) return;

            // PRIORITY 2: METEOR with 5 stacks - NUKE EVERYTHING
            if (_hasArcaneDynamo && _currentDynamoStacks >= 5 && hasEnemiesNearby)
            {
                if (_skillMeteor != null && !_skillMeteor.IsOnCooldown)
                {
                    FireSkill(_skillMeteor);
                    _meteorTimer.Restart();
                    _actionTimer.Restart();
                    return;
                }
            }

            // PRIORITY 3: Place Hydra with 5 stacks for big damage
            if (_hasArcaneDynamo && _currentDynamoStacks >= 5 && hasEnemiesNearby)
            {
                if (_skillHydra != null && !_skillHydra.IsOnCooldown && _hydraTimer.ElapsedMilliseconds > 5000)
                {
                    FireSkill(_skillHydra);
                    _hydraTimer.Restart();
                    _actionTimer.Restart();
                    return;
                }
            }

            // PRIORITY 4: Spam Spectral Blade to build stacks FAST
            if (hasEnemiesMelee)
            {
                if (_skillSpectralBlade != null && _bladeTimer.ElapsedMilliseconds > 80)
                {
                    FireSkill(_skillSpectralBlade);
                    _bladeTimer.Restart();
                    _actionTimer.Restart();
                    return;
                }
            }

            // PRIORITY 5: If no Arcane Dynamo passive, just spam meteor anyway
            if (!_hasArcaneDynamo && hasEnemiesNearby)
            {
                if (_skillMeteor != null && !_skillMeteor.IsOnCooldown && HasEnoughResource(_skillMeteor))
                {
                    FireSkill(_skillMeteor);
                    _actionTimer.Restart();
                    return;
                }
                
                // Spam Hydra
                if (_skillHydra != null && !_skillHydra.IsOnCooldown && _hydraTimer.ElapsedMilliseconds > 3000)
                {
                    FireSkill(_skillHydra);
                    _hydraTimer.Restart();
                    _actionTimer.Restart();
                    return;
                }

                // Spam Spectral Blade
                if (_skillSpectralBlade != null && hasEnemiesMelee)
                {
                    FireSkill(_skillSpectralBlade);
                    _actionTimer.Restart();
                    return;
                }
            }

            // PRIORITY 6: Move towards cursor aggressively
            ForceMove();
            _actionTimer.Restart();
        }

        private void TryRefreshBuffsInstant()
        {
            // Magic Weapon - keep it up always
            if (_skillMagicWeapon != null && !_skillMagicWeapon.IsOnCooldown)
            {
                double buffTime = _skillMagicWeapon.BuffIsActive ? _skillMagicWeapon.RemainingBuffTime() : 0;
                if (buffTime < BuffRefreshTime)
                {
                    Hud.Interaction.DoAction(_skillMagicWeapon.Key);
                }
            }

            // Storm Armor - keep it up always
            if (_skillStormArmor != null && !_skillStormArmor.IsOnCooldown)
            {
                double buffTime = _skillStormArmor.BuffIsActive ? _skillStormArmor.RemainingBuffTime() : 0;
                if (buffTime < BuffRefreshTime)
                {
                    Hud.Interaction.DoAction(_skillStormArmor.Key);
                }
            }
        }

        private bool HasEnoughResource(IPlayerSkill skill)
        {
            if (skill == null) return false;
            var cost = skill.ResourceCost;
            if (cost <= 0) return true;
            return Hud.Game.Me.Stats.ResourceCurArcane >= cost;
        }

        private void FireSkill(IPlayerSkill skill)
        {
            if (skill == null) return;

            // Stop movement briefly to cast
            StopMove();
            
            // FIRE!
            Hud.Interaction.DoAction(skill.Key);
        }

        private void ForceMove()
        {
            if (!Hud.Interaction.IsContinuousActionStarted(ActionKey.Move))
            {
                Hud.Interaction.StartContinuousAction(ActionKey.Move, false);
            }
        }

        private void StopMove()
        {
            if (Hud.Interaction.IsContinuousActionStarted(ActionKey.Move))
            {
                Hud.Interaction.StopContinuousAction(ActionKey.Move);
            }
        }

        private void StartMacro()
        {
            Running = true;
            _actionTimer.Restart();
            _hydraTimer.Restart();
            _bladeTimer.Restart();
            _meteorTimer.Restart();
        }

        private void StopMacro()
        {
            Running = false;
            StopMove();
            _actionTimer.Stop();
        }

        public void PaintTopInGame(ClipState clipState)
        {
            if (clipState != ClipState.AfterClip) return;
            if (!Hud.Game.IsInGame) return;
            if (!_isWizard) return;

            // Stop macro if inventory is open
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
                var layout1 = _runningFont.GetTextLayout("★ STAR PACT ★");
                _runningFont.DrawText(layout1, centerX - layout1.Metrics.Width / 2, baseY);
                baseY += layout1.Metrics.Height + 2;

                // Show Dynamo stacks
                if (_hasArcaneDynamo)
                {
                    string stackText = _currentDynamoStacks >= 5 ? "NUKE!" : $"Dynamo: {_currentDynamoStacks}/5";
                    var stackLayout = (_currentDynamoStacks >= 5 ? _stackFont : _tipFont).GetTextLayout(stackText);
                    (_currentDynamoStacks >= 5 ? _stackFont : _tipFont).DrawText(stackLayout, centerX - stackLayout.Metrics.Width / 2, baseY);
                }
            }
            else
            {
                // OFF state - minimal display
                var layout1 = _stoppedFont.GetTextLayout("Star Pact");
                _stoppedFont.DrawText(layout1, centerX - layout1.Metrics.Width / 2, baseY);
                baseY += layout1.Metrics.Height + 2;

                var layout2 = _tipFont.GetTextLayout("OFF [F1]");
                _tipFont.DrawText(layout2, centerX - layout2.Metrics.Width / 2, baseY);
            }
        }
    }
}
