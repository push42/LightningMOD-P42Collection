namespace Turbo.Plugins.Custom.PylonAlert
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Media;
    using System.Threading;
    using SharpDX.DirectInput;
    using Turbo.Plugins.Custom.Core;
    using Turbo.Plugins.Default;

    /// <summary>
    /// Pylon Alert Plugin - Core Integrated v1.0
    /// 
    /// Plays sound/speech notifications when your character receives a pylon buff.
    /// Works even when teammates grab pylons across the map!
    /// 
    /// Features:
    /// - Individual enable/disable for each pylon type
    /// - Text-to-Speech announcements
    /// - Sound effect alerts
    /// - Visual on-screen notifications
    /// - Cooldown to prevent spam
    /// - GR-only option
    /// 
    /// Hotkeys:
    /// - Shift+P = Toggle alerts on/off
    /// </summary>
    public class PylonAlertPlugin : CustomPluginBase, IInGameTopPainter, IKeyEventHandler, IAfterCollectHandler
    {
        #region Plugin Metadata

        public override string PluginId => "pylon-alert";
        public override string PluginName => "Pylon Alert";
        public override string PluginDescription => "Sound alerts when you receive pylon buffs";
        public override string PluginVersion => "1.0.0";
        public override string PluginCategory => "utility";
        public override string PluginIcon => "⚡";
        public override bool HasSettings => true;

        #endregion

        #region Runtime State

        public override bool IsActive => _isActiveInternal;
        public override string StatusText => !_isActiveInternal ? "OFF" : (_lastPylonName ?? "Ready");

        #endregion

        #region Public Settings

        public IKeyEvent ToggleKey { get; set; }
        
        // General settings
        public bool EnableSpeech { get; set; } = true;
        public bool EnableSound { get; set; } = true;
        public bool EnableVisual { get; set; } = true;
        public bool OnlyInGR { get; set; } = false;
        public float AlertCooldownSeconds { get; set; } = 2.0f;
        public float VisualDurationSeconds { get; set; } = 3.0f;
        
        // Individual pylon toggles
        public bool AlertPower { get; set; } = true;
        public bool AlertConduit { get; set; } = true;
        public bool AlertChanneling { get; set; } = true;
        public bool AlertShielding { get; set; } = true;
        public bool AlertSpeed { get; set; } = true;
        
        // Speech customization
        public string SpeechPower { get; set; } = "Power Pylon!";
        public string SpeechConduit { get; set; } = "Conduit!";
        public string SpeechChanneling { get; set; } = "Channeling Pylon!";
        public string SpeechShielding { get; set; } = "Shield Pylon!";
        public string SpeechSpeed { get; set; } = "Speed Pylon!";
        
        // Sound file (can be customized)
        public string SoundFileName { get; set; } = "notification_1.wav";

        #endregion

        #region Private Fields

        private bool _isActiveInternal = true;
        
        // Pylon buff SNOs
        private uint _powerPylonSno;
        private uint _conduitPylonSno;
        private uint _channelingPylonSno;
        private uint _shieldingPylonSno;
        private uint _speedPylonSno;
        
        // Tracking active buffs to detect new ones
        private HashSet<uint> _activePylonBuffs = new HashSet<uint>();
        private Dictionary<uint, PylonInfo> _pylonInfoMap;
        
        // Cooldown tracking
        private IWatch _lastAlertTime;
        private IWatch _visualTimer;
        private string _lastPylonName;
        private string _lastPylonIcon;
        private bool _showVisual;
        
        // Sound player (System.Media.SoundPlayer)
        private SoundPlayer _alertSound;
        
        // Visual elements
        private IFont _alertFont;
        private IFont _pylonNameFont;
        private IBrush _alertBgBrush;
        private IBrush _alertBorderBrush;

        #endregion

        #region Initialization

        public PylonAlertPlugin()
        {
            Enabled = true;
            Order = 50010;
        }

        public override void Load(IController hud)
        {
            base.Load(hud);

            ToggleKey = Hud.Input.CreateKeyEvent(true, Key.P, false, false, true); // Shift+P

            // Initialize timers
            _lastAlertTime = Hud.Time.CreateWatch();
            _visualTimer = Hud.Time.CreateWatch();

            // Pylon buff SNOs (these are the buff powers applied to player)
            _powerPylonSno = Hud.Sno.SnoPowers.Generic_PagesBuffDamage.Sno;        // Power Pylon
            _conduitPylonSno = Hud.Sno.SnoPowers.Generic_PagesBuffElectrified.Sno; // Conduit
            _channelingPylonSno = Hud.Sno.SnoPowers.Generic_PagesBuffInfiniteCasting.Sno; // Channeling
            _shieldingPylonSno = Hud.Sno.SnoPowers.Generic_PagesBuffInvulnerable.Sno;    // Shielding
            _speedPylonSno = Hud.Sno.SnoPowers.Generic_PagesBuffRunSpeed.Sno;      // Speed

            // Build pylon info map
            _pylonInfoMap = new Dictionary<uint, PylonInfo>
            {
                { _powerPylonSno, new PylonInfo("Power", "⚔️", () => AlertPower, () => SpeechPower) },
                { _conduitPylonSno, new PylonInfo("Conduit", "⚡", () => AlertConduit, () => SpeechConduit) },
                { _channelingPylonSno, new PylonInfo("Channeling", "🔮", () => AlertChanneling, () => SpeechChanneling) },
                { _shieldingPylonSno, new PylonInfo("Shielding", "🛡️", () => AlertShielding, () => SpeechShielding) },
                { _speedPylonSno, new PylonInfo("Speed", "💨", () => AlertSpeed, () => SpeechSpeed) },
            };

            // Load alert sound using TurboHUD's sound loader
            try
            {
                _alertSound = Hud.Sound.LoadSoundPlayer(SoundFileName);
            }
            catch
            {
                // Sound file might not exist, that's okay
                _alertSound = null;
            }

            // Visual elements
            _alertFont = Hud.Render.CreateFont("segoe ui", 14, 255, 255, 220, 100, true, false, 255, 0, 0, 0, true);
            _pylonNameFont = Hud.Render.CreateFont("segoe ui", 11, 255, 255, 255, 255, true, false, 200, 0, 0, 0, true);
            _alertBgBrush = Hud.Render.CreateBrush(230, 20, 20, 30, 0);
            _alertBorderBrush = Hud.Render.CreateBrush(255, 255, 200, 50, 2f);

            Log("Pylon Alert v1.0 loaded");
        }

        public void SetActive(bool active) => _isActiveInternal = active;

        #endregion

        #region Settings Panel

        public override void DrawSettings(IController hud, RectangleF rect, Dictionary<string, RectangleF> clickAreas, int scrollOffset)
        {
            float x = rect.X, y = rect.Y, w = rect.Width;

            // Status
            string statusText = _isActiveInternal ? "● ACTIVE" : "○ OFF";
            var statusFont = _isActiveInternal ? (HasCore ? Core.FontSuccess : _pylonNameFont) : (HasCore ? Core.FontError : _pylonNameFont);
            var statusLayout = statusFont.GetTextLayout(statusText);
            statusFont.DrawText(statusLayout, x, y);
            y += statusLayout.Metrics.Height + 10;

            // Alert Types section
            y += DrawSettingsHeader(x, y, "Alert Types");
            y += 8;

            y += DrawToggleSetting(x, y, w, "🔊 Speech Alerts", EnableSpeech, clickAreas, "toggle_speech");
            y += DrawToggleSetting(x, y, w, "🔔 Sound Alerts", EnableSound, clickAreas, "toggle_sound");
            y += DrawToggleSetting(x, y, w, "📺 Visual Alerts", EnableVisual, clickAreas, "toggle_visual");
            y += DrawToggleSetting(x, y, w, "GR Only", OnlyInGR, clickAreas, "toggle_gronly");

            y += 12;

            // Pylon Types section
            y += DrawSettingsHeader(x, y, "Pylon Types");
            y += 8;

            y += DrawToggleSetting(x, y, w, "⚔️ Power Pylon", AlertPower, clickAreas, "toggle_power");
            y += DrawToggleSetting(x, y, w, "⚡ Conduit Pylon", AlertConduit, clickAreas, "toggle_conduit");
            y += DrawToggleSetting(x, y, w, "🔮 Channeling Pylon", AlertChanneling, clickAreas, "toggle_channeling");
            y += DrawToggleSetting(x, y, w, "🛡️ Shielding Pylon", AlertShielding, clickAreas, "toggle_shielding");
            y += DrawToggleSetting(x, y, w, "💨 Speed Pylon", AlertSpeed, clickAreas, "toggle_speed");

            y += 12;

            // Timing section
            y += DrawSettingsHeader(x, y, "Timing");
            y += 8;

            y += DrawSelectorSetting(x, y, w, "Cooldown (s)", AlertCooldownSeconds.ToString("F1"), clickAreas, "sel_cooldown");
            y += DrawSelectorSetting(x, y, w, "Visual Duration", VisualDurationSeconds.ToString("F1"), clickAreas, "sel_duration");

            y += 16;
            y += DrawSettingsHint(x, y, "[Shift+P] Toggle alerts");
        }

        public override void HandleSettingsClick(string clickId)
        {
            switch (clickId)
            {
                // Alert types
                case "toggle_speech": EnableSpeech = !EnableSpeech; break;
                case "toggle_sound": EnableSound = !EnableSound; break;
                case "toggle_visual": EnableVisual = !EnableVisual; break;
                case "toggle_gronly": OnlyInGR = !OnlyInGR; break;
                
                // Pylon types
                case "toggle_power": AlertPower = !AlertPower; break;
                case "toggle_conduit": AlertConduit = !AlertConduit; break;
                case "toggle_channeling": AlertChanneling = !AlertChanneling; break;
                case "toggle_shielding": AlertShielding = !AlertShielding; break;
                case "toggle_speed": AlertSpeed = !AlertSpeed; break;
                
                // Timing
                case "sel_cooldown_prev": AlertCooldownSeconds = Math.Max(0.5f, AlertCooldownSeconds - 0.5f); break;
                case "sel_cooldown_next": AlertCooldownSeconds = Math.Min(10f, AlertCooldownSeconds + 0.5f); break;
                case "sel_duration_prev": VisualDurationSeconds = Math.Max(1f, VisualDurationSeconds - 0.5f); break;
                case "sel_duration_next": VisualDurationSeconds = Math.Min(10f, VisualDurationSeconds + 0.5f); break;
            }

            SavePluginSettings();
        }

        protected override object GetSettingsObject() => new PylonAlertSettings
        {
            IsActive = this._isActiveInternal,
            EnableSpeech = this.EnableSpeech,
            EnableSound = this.EnableSound,
            EnableVisual = this.EnableVisual,
            OnlyInGR = this.OnlyInGR,
            AlertCooldownSeconds = this.AlertCooldownSeconds,
            VisualDurationSeconds = this.VisualDurationSeconds,
            AlertPower = this.AlertPower,
            AlertConduit = this.AlertConduit,
            AlertChanneling = this.AlertChanneling,
            AlertShielding = this.AlertShielding,
            AlertSpeed = this.AlertSpeed,
            SpeechPower = this.SpeechPower,
            SpeechConduit = this.SpeechConduit,
            SpeechChanneling = this.SpeechChanneling,
            SpeechShielding = this.SpeechShielding,
            SpeechSpeed = this.SpeechSpeed
        };

        protected override void ApplySettingsObject(object settings)
        {
            if (settings is PylonAlertSettings s)
            {
                _isActiveInternal = s.IsActive;
                EnableSpeech = s.EnableSpeech;
                EnableSound = s.EnableSound;
                EnableVisual = s.EnableVisual;
                OnlyInGR = s.OnlyInGR;
                AlertCooldownSeconds = s.AlertCooldownSeconds;
                VisualDurationSeconds = s.VisualDurationSeconds;
                AlertPower = s.AlertPower;
                AlertConduit = s.AlertConduit;
                AlertChanneling = s.AlertChanneling;
                AlertShielding = s.AlertShielding;
                AlertSpeed = s.AlertSpeed;
                SpeechPower = s.SpeechPower ?? "Power Pylon!";
                SpeechConduit = s.SpeechConduit ?? "Conduit!";
                SpeechChanneling = s.SpeechChanneling ?? "Channeling Pylon!";
                SpeechShielding = s.SpeechShielding ?? "Shield Pylon!";
                SpeechSpeed = s.SpeechSpeed ?? "Speed Pylon!";
            }
        }

        private class PylonAlertSettings : PluginSettingsBase
        {
            public bool IsActive { get; set; }
            public bool EnableSpeech { get; set; }
            public bool EnableSound { get; set; }
            public bool EnableVisual { get; set; }
            public bool OnlyInGR { get; set; }
            public float AlertCooldownSeconds { get; set; }
            public float VisualDurationSeconds { get; set; }
            public bool AlertPower { get; set; }
            public bool AlertConduit { get; set; }
            public bool AlertChanneling { get; set; }
            public bool AlertShielding { get; set; }
            public bool AlertSpeed { get; set; }
            public string SpeechPower { get; set; }
            public string SpeechConduit { get; set; }
            public string SpeechChanneling { get; set; }
            public string SpeechShielding { get; set; }
            public string SpeechSpeed { get; set; }
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
                SetCoreStatus($"Pylon Alert {(_isActiveInternal ? "ON" : "OFF")}", 
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
            
            // Check GR-only setting
            if (OnlyInGR && !Hud.Game.Me.InGreaterRift) return;

            // Get current pylon buffs on player
            var currentBuffs = new HashSet<uint>();
            
            foreach (var pylonSno in _pylonInfoMap.Keys)
            {
                if (Hud.Game.Me.Powers.BuffIsActive(pylonSno))
                {
                    currentBuffs.Add(pylonSno);
                }
            }

            // Check for NEW buffs (buffs that weren't active before)
            foreach (var buffSno in currentBuffs)
            {
                if (!_activePylonBuffs.Contains(buffSno))
                {
                    // New pylon buff detected!
                    OnPylonBuffGained(buffSno);
                }
            }

            // Update tracked buffs
            _activePylonBuffs = currentBuffs;
        }

        private void OnPylonBuffGained(uint pylonSno)
        {
            if (!_pylonInfoMap.TryGetValue(pylonSno, out var info)) return;
            
            // Check if this pylon type is enabled
            if (!info.IsEnabled()) return;

            // Check cooldown
            if (_lastAlertTime.IsRunning && _lastAlertTime.ElapsedMilliseconds < AlertCooldownSeconds * 1000)
                return;

            _lastAlertTime.Restart();

            // Store for visual display
            _lastPylonName = info.Name;
            _lastPylonIcon = info.Icon;

            // Play speech (async)
            if (EnableSpeech)
            {
                try
                {
                    Hud.Sound.Speak(info.GetSpeech());
                }
                catch { }
            }

            // Play sound (async on thread pool to avoid blocking)
            if (EnableSound && _alertSound != null)
            {
                ThreadPool.QueueUserWorkItem(state =>
                {
                    try
                    {
                        _alertSound.PlaySync();
                    }
                    catch { }
                });
            }

            // Show visual
            if (EnableVisual)
            {
                _showVisual = true;
                _visualTimer.Restart();
            }

            // Update Core status
            SetCoreStatus($"{info.Icon} {info.Name}!", StatusType.Success);
            
            Log($"Pylon buff gained: {info.Name}");
        }

        #endregion

        #region Rendering

        public override void PaintTopInGame(ClipState clipState)
        {
            // Call base for Core registration
            base.PaintTopInGame(clipState);

            if (clipState != ClipState.AfterClip) return;
            if (!Hud.Game.IsInGame) return;
            if (!Enabled) return;
            if (!_isActiveInternal) return;

            // Check if visual should be hidden
            if (_showVisual && _visualTimer.ElapsedMilliseconds > VisualDurationSeconds * 1000)
            {
                _showVisual = false;
            }

            // Draw visual alert
            if (_showVisual && !string.IsNullOrEmpty(_lastPylonName))
            {
                DrawVisualAlert();
            }
        }

        private void DrawVisualAlert()
        {
            // Calculate fade
            float elapsed = _visualTimer.ElapsedMilliseconds / 1000f;
            float fadeStart = VisualDurationSeconds * 0.7f;
            float alpha = 1f;
            if (elapsed > fadeStart)
            {
                alpha = 1f - ((elapsed - fadeStart) / (VisualDurationSeconds - fadeStart));
            }
            alpha = Math.Max(0, Math.Min(1, alpha));

            // Position - top center of screen
            float screenW = Hud.Window.Size.Width;
            float screenH = Hud.Window.Size.Height;
            
            string text = $"{_lastPylonIcon} {_lastPylonName} PYLON!";
            var textLayout = _alertFont.GetTextLayout(text);
            
            float boxW = textLayout.Metrics.Width + 40;
            float boxH = textLayout.Metrics.Height + 20;
            float boxX = (screenW - boxW) / 2;
            float boxY = screenH * 0.15f;

            // Animate - slide down
            float slideOffset = (1f - alpha) * 20f;
            boxY += slideOffset;

            // Draw with alpha
            byte alphaB = (byte)(alpha * 230);
            var bgBrush = Hud.Render.CreateBrush(alphaB, 20, 20, 30, 0);
            var borderBrush = Hud.Render.CreateBrush((byte)(alpha * 255), 255, 200, 50, 2f);
            
            bgBrush.DrawRectangle(boxX, boxY, boxW, boxH);
            borderBrush.DrawRectangle(boxX, boxY, boxW, boxH);

            // Draw text
            var font = Hud.Render.CreateFont("segoe ui", 14, (byte)(alpha * 255), 255, 220, 100, true, false, (byte)(alpha * 255), 0, 0, 0, true);
            var layout = font.GetTextLayout(text);
            font.DrawText(layout, boxX + (boxW - layout.Metrics.Width) / 2, boxY + (boxH - layout.Metrics.Height) / 2);
        }

        #endregion

        #region Helper Classes

        private class PylonInfo
        {
            public string Name { get; }
            public string Icon { get; }
            private readonly Func<bool> _isEnabled;
            private readonly Func<string> _getSpeech;

            public PylonInfo(string name, string icon, Func<bool> isEnabled, Func<string> getSpeech)
            {
                Name = name;
                Icon = icon;
                _isEnabled = isEnabled;
                _getSpeech = getSpeech;
            }

            public bool IsEnabled() => _isEnabled();
            public string GetSpeech() => _getSpeech();
        }

        #endregion
    }
}
