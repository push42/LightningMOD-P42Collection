namespace Turbo.Plugins.Custom.SmartEvadeLite
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
    /// Smart Evade Lite v2.0 - Core Integrated
    /// 
    /// Community-friendly auto-dodge with human-like reaction times
    /// Now integrated with Core Plugin Hub (F10)
    /// 
    /// Press J to toggle on/off
    /// </summary>
    public class SmartEvadeLitePlugin : CustomPluginBase, IKeyEventHandler, IAfterCollectHandler
    {
        #region Plugin Metadata

        public override string PluginId => "smart-evade-lite";
        public override string PluginName => "Evade Lite";
        public override string PluginDescription => "Human-like auto-dodge for dangerous ground effects";
        public override string PluginVersion => "2.0.0";
        public override string PluginCategory => "combat";
        public override string PluginIcon => "🛡️";
        public override bool HasSettings => true;

        #endregion

        #region Runtime State (for Core sidebar)

        // Override to report our actual IsActive state to Core
        public override bool IsActive => _isActiveInternal;
        
        // Override to show detailed status in sidebar
        public override string StatusText => !_isActiveInternal ? "OFF" : (_isInDanger ? $"!{_currentDangerName}" : "Ready");

        #endregion

        #region Public Settings

        public IKeyEvent ToggleKey { get; set; }
        private bool _isActiveInternal;
        
        /// <summary>
        /// Set the active state (for customizer use)
        /// </summary>
        public void SetActive(bool active) => _isActiveInternal = active;
        
        public float MinEvadeDelay { get; set; } = 1.25f;
        public float MaxEvadeDelay { get; set; } = 2.0f;
        public float EvadeDistance { get; set; } = 12f;
        public float EvadeCooldown { get; set; } = 3.0f;
        public bool ShowDangerCircles { get; set; } = true;
        public bool ShowPanel { get; set; } = false;  // Disabled - Core sidebar shows status
        public float PanelX { get; set; } = 0.005f;
        public float PanelY { get; set; } = 0.49f;

        #endregion

        #region Private Fields

        private Dictionary<ActorSnoEnum, DangerZoneInfo> _dangerZones;
        
        // Fallback UI
        private IFont _titleFont;
        private IFont _statusFont;
        private IFont _infoFont;
        private IBrush _panelBrush;
        private IBrush _borderBrush;
        private IBrush _accentOnBrush;
        private IBrush _accentOffBrush;
        private IBrush _dangerBrush;
        private IBrush _warningBrush;

        // State
        private IWatch _dangerTimer;
        private IWatch _evadeCooldownTimer;
        private float _currentEvadeDelay;
        private bool _isInDanger;
        private bool _evadeTriggered;
        private string _currentDangerName;
        private Random _random;
        private int _evadeCount;

        #endregion

        #region Initialization

        public SmartEvadeLitePlugin()
        {
            Enabled = true;
            Order = 50001;
        }

        public override void Load(IController hud)
        {
            base.Load(hud);

            ToggleKey = Hud.Input.CreateKeyEvent(true, Key.J, false, false, false);
            _isActiveInternal = false;
            _random = new Random();

            InitializeDangerZones();
            InitializeFallbackUI();

            _dangerTimer = Hud.Time.CreateWatch();
            _evadeCooldownTimer = Hud.Time.CreateAndStartWatch();
            _currentEvadeDelay = GetRandomDelay();

            Log("Evade Lite loaded");
        }

        private void InitializeFallbackUI()
        {
            _titleFont = Hud.Render.CreateFont("tahoma", 8, 255, 220, 180, 100, true, false, 180, 0, 0, 0, true);
            _statusFont = Hud.Render.CreateFont("tahoma", 7.5f, 255, 255, 255, 255, true, false, 160, 0, 0, 0, true);
            _infoFont = Hud.Render.CreateFont("tahoma", 7, 200, 180, 180, 180, false, false, 140, 0, 0, 0, true);
            
            _panelBrush = Hud.Render.CreateBrush(235, 15, 15, 25, 0);
            _borderBrush = Hud.Render.CreateBrush(200, 60, 60, 80, 1f);
            _accentOnBrush = Hud.Render.CreateBrush(255, 80, 200, 80, 0);
            _accentOffBrush = Hud.Render.CreateBrush(255, 200, 80, 80, 0);
            _dangerBrush = Hud.Render.CreateBrush(120, 255, 50, 50, 2f);
            _warningBrush = Hud.Render.CreateBrush(150, 255, 200, 0, 2f);
        }

        private void InitializeDangerZones()
        {
            _dangerZones = new Dictionary<ActorSnoEnum, DangerZoneInfo>
            {
                { ActorSnoEnum._monsteraffix_frozen_iceclusters, new DangerZoneInfo("Frozen", 14f, true) },
                { ActorSnoEnum._monsteraffix_molten_deathstart_proxy, new DangerZoneInfo("Molten Explosion", 12f, true) },
                { ActorSnoEnum._monsteraffix_molten_deathexplosion_proxy, new DangerZoneInfo("Molten", 11f, false) },
                { ActorSnoEnum._monsteraffix_arcaneenchanted_petsweep, new DangerZoneInfo("Arcane", 6f, true) },
                { ActorSnoEnum._monsteraffix_arcaneenchanted_petsweep_reverse, new DangerZoneInfo("Arcane", 6f, true) },
                { ActorSnoEnum._monsteraffix_desecrator_damage_aoe, new DangerZoneInfo("Desecrator", 7f, false) },
                { ActorSnoEnum._x1_monsteraffix_thunderstorm_impact, new DangerZoneInfo("Thunderstorm", 14f, true) },
                { ActorSnoEnum._monsteraffix_plagued_endcloud, new DangerZoneInfo("Plagued", 10f, false) },
                { ActorSnoEnum._creepmobarm, new DangerZoneInfo("Poison", 10f, false) },
                { ActorSnoEnum._x1_monsteraffix_frozenpulse_monster, new DangerZoneInfo("Frozen Pulse", 12f, false) },
                { ActorSnoEnum._x1_monsteraffix_orbiter_projectile, new DangerZoneInfo("Orbiter", 3f, false) },
                { ActorSnoEnum._gluttony_gascloud_proxy, new DangerZoneInfo("Gas Cloud", 18f, false) },
            };
        }

        private float GetRandomDelay() => MinEvadeDelay + (float)_random.NextDouble() * (MaxEvadeDelay - MinEvadeDelay);

        #endregion

        #region Settings Panel

        public override void DrawSettings(IController hud, RectangleF rect, Dictionary<string, RectangleF> clickAreas, int scrollOffset)
        {
            float x = rect.X, y = rect.Y, w = rect.Width;

            // Status
            string statusText = _isActiveInternal ? "● ACTIVE" : "○ OFF";
            var statusFont = _isActiveInternal ? (HasCore ? Core.FontSuccess : _statusFont) : (HasCore ? Core.FontError : _statusFont);
            var statusLayout = statusFont.GetTextLayout(statusText);
            statusFont.DrawText(statusLayout, x, y);
            y += statusLayout.Metrics.Height + 10;

            // Timing
            y += DrawSettingsHeader(x, y, "Timing");
            y += 8;

            y += DrawSelectorSetting(x, y, w, "Min Delay", $"{MinEvadeDelay:F2}s", clickAreas, "sel_mindelay");
            y += DrawSelectorSetting(x, y, w, "Max Delay", $"{MaxEvadeDelay:F2}s", clickAreas, "sel_maxdelay");
            y += DrawSelectorSetting(x, y, w, "Cooldown", $"{EvadeCooldown:F1}s", clickAreas, "sel_cooldown");

            y += 12;

            // Movement
            y += DrawSettingsHeader(x, y, "Movement");
            y += 8;

            y += DrawSelectorSetting(x, y, w, "Escape Distance", $"{EvadeDistance:F0}", clickAreas, "sel_distance");

            y += 12;

            // Display
            y += DrawSettingsHeader(x, y, "Display");
            y += 8;

            y += DrawToggleSetting(x, y, w, "Show Danger Circles", ShowDangerCircles, clickAreas, "toggle_circles");
            y += DrawToggleSetting(x, y, w, "Show Status Panel", ShowPanel, clickAreas, "toggle_panel");

            y += 16;
            y += DrawSettingsHint(x, y, $"[J] Toggle • Evades: {_evadeCount}");
        }

        public override void HandleSettingsClick(string clickId)
        {
            switch (clickId)
            {
                case "sel_mindelay_prev": MinEvadeDelay = Math.Max(0.5f, MinEvadeDelay - 0.25f); break;
                case "sel_mindelay_next": MinEvadeDelay = Math.Min(MaxEvadeDelay, MinEvadeDelay + 0.25f); break;
                case "sel_maxdelay_prev": MaxEvadeDelay = Math.Max(MinEvadeDelay, MaxEvadeDelay - 0.25f); break;
                case "sel_maxdelay_next": MaxEvadeDelay = Math.Min(5f, MaxEvadeDelay + 0.25f); break;
                case "sel_cooldown_prev": EvadeCooldown = Math.Max(1f, EvadeCooldown - 0.5f); break;
                case "sel_cooldown_next": EvadeCooldown = Math.Min(10f, EvadeCooldown + 0.5f); break;
                case "sel_distance_prev": EvadeDistance = Math.Max(5f, EvadeDistance - 2f); break;
                case "sel_distance_next": EvadeDistance = Math.Min(25f, EvadeDistance + 2f); break;
                case "toggle_circles": ShowDangerCircles = !ShowDangerCircles; break;
                case "toggle_panel": ShowPanel = !ShowPanel; break;
            }
            SavePluginSettings();
        }

        protected override object GetSettingsObject() => new EvadeLiteSettings
        {
            IsActive = this._isActiveInternal,
            MinEvadeDelay = this.MinEvadeDelay,
            MaxEvadeDelay = this.MaxEvadeDelay,
            EvadeCooldown = this.EvadeCooldown,
            EvadeDistance = this.EvadeDistance,
            ShowDangerCircles = this.ShowDangerCircles,
            ShowPanel = this.ShowPanel
        };

        protected override void ApplySettingsObject(object settings)
        {
            if (settings is EvadeLiteSettings s)
            {
                _isActiveInternal = s.IsActive;
                MinEvadeDelay = s.MinEvadeDelay;
                MaxEvadeDelay = s.MaxEvadeDelay;
                EvadeCooldown = s.EvadeCooldown;
                EvadeDistance = s.EvadeDistance;
                ShowDangerCircles = s.ShowDangerCircles;
                ShowPanel = s.ShowPanel;
            }
        }

        private class EvadeLiteSettings : PluginSettingsBase
        {
            public bool IsActive { get; set; }
            public float MinEvadeDelay { get; set; }
            public float MaxEvadeDelay { get; set; }
            public float EvadeCooldown { get; set; }
            public float EvadeDistance { get; set; }
            public bool ShowDangerCircles { get; set; }
            public bool ShowPanel { get; set; }
        }

        #endregion

        #region Key Handler

        public void OnKeyEvent(IKeyEvent keyEvent)
        {
            if (!Hud.Game.IsInGame) return;
            if (!Enabled) return;

            if (ToggleKey.Matches(keyEvent) && keyEvent.IsPressed)
            {
                _isActiveInternal = !_isActiveInternal;
                _isInDanger = false;
                _evadeTriggered = false;
                _dangerTimer.Reset();
                SetCoreStatus($"Evade Lite {(_isActiveInternal ? "ON" : "OFF")}", 
                             _isActiveInternal ? StatusType.Success : StatusType.Warning);
            }
        }

        #endregion

        #region Main Logic

        public void AfterCollect()
        {
            if (!Hud.Game.IsInGame) return;
            if (!Enabled) return;
            if (!_isActiveInternal) return;
            if (Hud.Game.IsInTown) return;
            if (!Hud.Window.IsForeground) return;
            if (Hud.Game.Me.IsDead) return;

            var danger = GetCurrentDanger();

            if (danger != null)
            {
                if (!_isInDanger)
                {
                    _isInDanger = true;
                    _evadeTriggered = false;
                    _currentEvadeDelay = GetRandomDelay();
                    _dangerTimer.Restart();
                    _currentDangerName = danger.Name;
                }
                else if (!_evadeTriggered && _dangerTimer.ElapsedMilliseconds >= _currentEvadeDelay * 1000)
                {
                    if (_evadeCooldownTimer.ElapsedMilliseconds >= EvadeCooldown * 1000)
                    {
                        ExecuteEvade(danger);
                        _evadeTriggered = true;
                        _evadeCooldownTimer.Restart();
                        _evadeCount++;
                    }
                }
            }
            else
            {
                _isInDanger = false;
                _evadeTriggered = false;
                _currentDangerName = null;
                _dangerTimer.Reset();
            }
        }

        private DangerInfo GetCurrentDanger()
        {
            var myPos = Hud.Game.Me.FloorCoordinate;
            DangerInfo closestDanger = null;
            float closestDistance = float.MaxValue;

            foreach (var actor in Hud.Game.Actors)
            {
                if (_dangerZones.TryGetValue(actor.SnoActor.Sno, out var zone))
                {
                    float distance = myPos.XYDistanceTo(actor.FloorCoordinate);
                    if (distance < zone.Radius && distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestDanger = new DangerInfo
                        {
                            Name = zone.Name,
                            Position = actor.FloorCoordinate,
                            Radius = zone.Radius,
                            Distance = distance,
                            IsHighPriority = zone.IsHighPriority
                        };
                    }
                }
            }

            foreach (var avoid in Hud.Game.Me.AvoidablesInRange)
            {
                float distance = myPos.XYDistanceTo(avoid.FloorCoordinate);
                float radius = avoid.AvoidableDefinition.Radius;
                if (distance < radius && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestDanger = new DangerInfo
                    {
                        Name = "Danger",
                        Position = avoid.FloorCoordinate,
                        Radius = radius,
                        Distance = distance,
                        IsHighPriority = avoid.AvoidableDefinition.InstantDeath
                    };
                }
            }

            return closestDanger;
        }

        private void ExecuteEvade(DangerInfo danger)
        {
            var myPos = Hud.Game.Me.FloorCoordinate;
            float dx = myPos.X - danger.Position.X;
            float dy = myPos.Y - danger.Position.Y;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);

            if (dist < 0.1f)
            {
                float angle = (float)(_random.NextDouble() * Math.PI * 2);
                dx = (float)Math.Cos(angle);
                dy = (float)Math.Sin(angle);
            }
            else
            {
                dx /= dist;
                dy /= dist;
            }

            float moveDistance = danger.Radius - danger.Distance + EvadeDistance;
            float targetX = myPos.X + dx * moveDistance;
            float targetY = myPos.Y + dy * moveDistance;

            var targetCoord = Hud.Window.CreateWorldCoordinate(targetX, targetY, myPos.Z);
            var screenPos = targetCoord.ToScreenCoordinate();

            if (screenPos.X > 50 && screenPos.X < Hud.Window.Size.Width - 50 &&
                screenPos.Y > 50 && screenPos.Y < Hud.Window.Size.Height - 50)
            {
                int savedX = Hud.Window.CursorX;
                int savedY = Hud.Window.CursorY;

                Hud.Interaction.MouseMove((int)screenPos.X, (int)screenPos.Y, 1, 1);
                Hud.Interaction.MouseDown(MouseButtons.Left);
                Hud.Wait(10 + _random.Next(20));
                Hud.Interaction.MouseUp(MouseButtons.Left);
                Hud.Interaction.MouseMove(savedX, savedY, 1, 1);
            }
        }

        #endregion

        #region Rendering

        public override void PaintTopInGame(ClipState clipState)
        {
            base.PaintTopInGame(clipState);
            
            if (clipState != ClipState.BeforeClip) return;
            if (!Hud.Game.IsInGame) return;
            if (!Enabled) return;

            // Only show panel if enabled AND Core is not available (fallback mode)
            // When registered with Core, the sidebar shows our status
            if (ShowPanel && !HasCore)
                DrawStatusPanel();

            if (_isActiveInternal && !Hud.Game.IsInTown && ShowDangerCircles)
                DrawDangerIndicators();
        }

        private void DrawStatusPanel()
        {
            float x = Hud.Window.Size.Width * PanelX;
            float y = Hud.Window.Size.Height * PanelY;
            float w = 120, h = _isInDanger ? 58 : 48, pad = 6;

            _panelBrush.DrawRectangle(x, y, w, h);
            _borderBrush.DrawRectangle(x, y, w, h);

            var accentBrush = _isActiveInternal ? _accentOnBrush : _accentOffBrush;
            accentBrush.DrawRectangle(x, y, 3, h);

            float tx = x + pad + 3, ty = y + pad;

            var title = _titleFont.GetTextLayout("Evade Lite");
            _titleFont.DrawText(title, tx, ty);
            ty += title.Metrics.Height + 2;

            string statusStr = !_isActiveInternal ? "OFF" : _isInDanger ? "! " + (_currentDangerName ?? "DANGER") : "Ready";
            var statusLayout = _statusFont.GetTextLayout(statusStr);
            _statusFont.DrawText(statusLayout, tx, ty);
            ty += statusLayout.Metrics.Height + 1;

            var hint = _infoFont.GetTextLayout("[J] Toggle");
            _infoFont.DrawText(hint, tx, ty);

            if (_isInDanger && !_evadeTriggered)
            {
                ty += hint.Metrics.Height + 1;
                float timeLeft = Math.Max(0, _currentEvadeDelay - (_dangerTimer.ElapsedMilliseconds / 1000f));
                var countdown = _infoFont.GetTextLayout($"Evade in: {timeLeft:F1}s");
                _infoFont.DrawText(countdown, tx, ty);
            }
        }

        private void DrawDangerIndicators()
        {
            var myPos = Hud.Game.Me.FloorCoordinate;

            foreach (var actor in Hud.Game.Actors)
            {
                if (_dangerZones.TryGetValue(actor.SnoActor.Sno, out var zone))
                {
                    float distance = myPos.XYDistanceTo(actor.FloorCoordinate);
                    if (distance < zone.Radius + 20f)
                    {
                        var brush = distance < zone.Radius ? _dangerBrush : _warningBrush;
                        brush.DrawWorldEllipse(zone.Radius, -1, actor.FloorCoordinate);
                    }
                }
            }
        }

        #endregion
    }

    #region Helper Classes

    internal class DangerZoneInfo
    {
        public string Name { get; set; }
        public float Radius { get; set; }
        public bool IsHighPriority { get; set; }

        public DangerZoneInfo(string name, float radius, bool highPriority)
        {
            Name = name;
            Radius = radius;
            IsHighPriority = highPriority;
        }
    }

    internal class DangerInfo
    {
        public string Name { get; set; }
        public IWorldCoordinate Position { get; set; }
        public float Radius { get; set; }
        public float Distance { get; set; }
        public bool IsHighPriority { get; set; }
    }

    #endregion
}
