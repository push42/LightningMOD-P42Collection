namespace Turbo.Plugins.Custom.SmartSalvage
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Windows.Forms;
    using SharpDX.DirectInput;
    using Turbo.Plugins.Default;

    /// <summary>
    /// Smart Salvage Plugin v3.0 - Premium Edition
    /// 
    /// Features:
    /// - Beautiful, polished UI with smooth animations
    /// - Build profile management with Maxroll/Icy-Veins import
    /// - STAT-BASED RULES: Keep items only if they meet stat requirements
    /// - GLOBAL RULES: Always keep Primals, high perfection items, etc.
    /// - Visual item highlights with quality indicators
    /// 
    /// Hotkeys:
    /// - U = Start/Stop auto-salvage
    /// - Shift+U = Open profile manager
    /// - Ctrl+U = Quick toggle all profiles
    /// </summary>
    public class SmartSalvagePlugin : BasePlugin, IKeyEventHandler, IInGameTopPainter, IAfterCollectHandler
    {
        #region Public Settings

        public IKeyEvent SalvageKey { get; set; }
        public IKeyEvent ManagerKey { get; set; }
        public IKeyEvent QuickToggleKey { get; set; }

        public bool AutoRepair { get; set; }

        public BlacklistManager BlacklistMgr { get; private set; }
        public MaxrollCrawler Crawler { get; private set; }
        public RulesManager RulesMgr { get; private set; }

        #endregion

        #region Private Fields - UI Elements

        private IUiElement _vendorPage;
        private IUiElement _salvageDialog;
        private IUiElement _salvageButton1;
        private IUiElement _salvageButton2;
        private IUiElement _repairDialog;
        private IUiElement _salvageTab;
        private IUiElement _okButton;

        #endregion

        #region Private Fields - Visual Design System

        // Typography
        private IFont _fontTitle;
        private IFont _fontHeader;
        private IFont _fontBody;
        private IFont _fontSmall;
        private IFont _fontMicro;
        private IFont _fontMono;
        private IFont _fontIcon;

        // Semantic Colors
        private IFont _fontSuccess;
        private IFont _fontWarning;
        private IFont _fontError;
        private IFont _fontAccent;
        private IFont _fontMuted;

        // Surface Brushes
        private IBrush _surfaceBase;
        private IBrush _surfaceElevated;
        private IBrush _surfaceOverlay;
        private IBrush _surfaceCard;

        // Border Brushes
        private IBrush _borderDefault;
        private IBrush _borderAccent;
        private IBrush _borderSuccess;
        private IBrush _borderWarning;
        private IBrush _borderError;

        // Interactive Brushes
        private IBrush _btnDefault;
        private IBrush _btnHover;
        private IBrush _btnActive;
        private IBrush _btnDisabled;

        // Toggle Brushes
        private IBrush _toggleOn;
        private IBrush _toggleOff;
        private IBrush _toggleTrack;
        private IBrush _toggleKnob;

        // Status Brushes
        private IBrush _statusSuccess;
        private IBrush _statusWarning;
        private IBrush _statusError;
        private IBrush _statusInfo;

        // Highlight Brushes
        private IBrush _highlightProtected;
        private IBrush _highlightSalvage;
        private IBrush _highlightAncient;
        private IBrush _highlightPrimal;
        private IBrush _highlightStatRule;

        // Progress Brushes
        private IBrush _progressTrack;
        private IBrush _progressFill;

        // Scroll Brushes
        private IBrush _scrollTrack;
        private IBrush _scrollThumb;

        #endregion

        #region Private Fields - State

        private bool _isRunning;
        private bool _isSalvaging;
        private IWatch _timer;
        private IWatch _statusTimer;
        private IWatch _animTimer;
        private IWatch _clickTimer;
        private IWatch _pulseTimer;

        private int _lastCursorX;
        private int _lastCursorY;
        private HashSet<string> _salvageAttempted;
        private string _statusMessage;
        private StatusType _statusType;

        // UI State
        private bool _showManager;
        private int _scrollOffset;
        private int _maxVisibleItems = 8;
        private string _activeTab = "profiles";  // profiles, rules, global, import

        // Import state
        private string _importUrl;
        private bool _isImporting;
        private string _importStatus;
        private StatusType _importStatusType;

        // Click tracking
        private bool _wasMouseDown;
        private Dictionary<string, RectangleF> _clickAreas;

        // Animation
        private float _panelSlide = 0f;
        private float _progressValue = 0f;
        private float _pulsePhase = 0f;
        private int _salvageTotal = 0;
        private int _salvageDone = 0;

        // Tooltip
        private string _tooltipText;
        private RectangleF _tooltipAnchor;

        #endregion

        #region Initialization

        public SmartSalvagePlugin()
        {
            Enabled = true;
            Order = 9998;
        }

        public override void Load(IController hud)
        {
            base.Load(hud);

            // Hotkeys
            SalvageKey = Hud.Input.CreateKeyEvent(true, Key.U, false, false, false);
            ManagerKey = Hud.Input.CreateKeyEvent(true, Key.U, false, false, true);
            QuickToggleKey = Hud.Input.CreateKeyEvent(true, Key.U, true, false, false);

            AutoRepair = true;

            // Initialize managers
            BlacklistMgr = new BlacklistManager();
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            BlacklistMgr.DataDirectory = Path.Combine(basePath, "plugins", "Custom", "SmartSalvage");
            BlacklistMgr.InitializeBuiltInProfiles();
            BlacklistMgr.LoadFromFile();

            Crawler = new MaxrollCrawler();
            RulesMgr = new RulesManager();

            _salvageAttempted = new HashSet<string>();
            _clickAreas = new Dictionary<string, RectangleF>();

            InitializeUiElements();
            InitializeDesignSystem();

            // Timers
            _timer = Hud.Time.CreateAndStartWatch();
            _statusTimer = Hud.Time.CreateWatch();
            _animTimer = Hud.Time.CreateAndStartWatch();
            _clickTimer = Hud.Time.CreateAndStartWatch();
            _pulseTimer = Hud.Time.CreateAndStartWatch();

            _statusMessage = "";
            _importUrl = "";
            _importStatus = "";
        }

        private void InitializeUiElements()
        {
            _vendorPage = Hud.Render.RegisterUiElement("Root.NormalLayer.vendor_dialog_mainPage", Hud.Inventory.InventoryMainUiElement, null);
            _salvageDialog = Hud.Render.RegisterUiElement("Root.NormalLayer.vendor_dialog_mainPage.salvage_dialog", _vendorPage, null);
            _salvageButton1 = Hud.Render.RegisterUiElement("Root.NormalLayer.vendor_dialog_mainPage.salvage_dialog.salvage_all_wrapper.salvage_button", _salvageDialog, null);
            _salvageButton2 = Hud.Render.RegisterUiElement("Root.NormalLayer.vendor_dialog_mainPage.salvage_dialog.salvage_button", _salvageDialog, null);
            _repairDialog = Hud.Render.RegisterUiElement("Root.NormalLayer.vendor_dialog_mainPage.repair_dialog", _vendorPage, null);
            _salvageTab = Hud.Render.RegisterUiElement("Root.NormalLayer.vendor_dialog_mainPage.tab_2", _vendorPage, null);
            _okButton = Hud.Render.RegisterUiElement("Root.TopLayer.confirmation.subdlg.stack.wrap.button_ok", _salvageDialog, null);
        }

        private void InitializeDesignSystem()
        {
            // === TYPOGRAPHY ===
            _fontTitle = Hud.Render.CreateFont("segoe ui", 12, 255, 255, 220, 120, true, false, 220, 0, 0, 0, true);
            _fontHeader = Hud.Render.CreateFont("segoe ui semibold", 9, 255, 245, 245, 250, true, false, 200, 0, 0, 0, true);
            _fontBody = Hud.Render.CreateFont("segoe ui", 7.5f, 255, 220, 220, 230, false, false, 180, 0, 0, 0, true);
            _fontSmall = Hud.Render.CreateFont("segoe ui", 6.5f, 230, 170, 170, 185, false, false, 150, 0, 0, 0, true);
            _fontMicro = Hud.Render.CreateFont("segoe ui", 6f, 200, 140, 140, 155, false, false, 120, 0, 0, 0, true);
            _fontMono = Hud.Render.CreateFont("consolas", 7, 255, 190, 200, 210, false, false, 160, 0, 0, 0, true);
            _fontIcon = Hud.Render.CreateFont("segoe ui symbol", 9, 255, 255, 255, 255, false, false, 200, 0, 0, 0, true);

            // Semantic fonts
            _fontSuccess = Hud.Render.CreateFont("segoe ui", 7.5f, 255, 120, 230, 140, true, false, 180, 0, 0, 0, true);
            _fontWarning = Hud.Render.CreateFont("segoe ui", 7.5f, 255, 255, 200, 100, true, false, 180, 0, 0, 0, true);
            _fontError = Hud.Render.CreateFont("segoe ui", 7.5f, 255, 255, 120, 120, true, false, 180, 0, 0, 0, true);
            _fontAccent = Hud.Render.CreateFont("segoe ui", 7.5f, 255, 120, 180, 255, true, false, 180, 0, 0, 0, true);
            _fontMuted = Hud.Render.CreateFont("segoe ui", 7f, 180, 130, 130, 145, false, false, 140, 0, 0, 0, true);

            // === SURFACES ===
            _surfaceBase = Hud.Render.CreateBrush(250, 18, 18, 24, 0);
            _surfaceElevated = Hud.Render.CreateBrush(248, 24, 24, 32, 0);
            _surfaceOverlay = Hud.Render.CreateBrush(245, 30, 30, 40, 0);
            _surfaceCard = Hud.Render.CreateBrush(240, 35, 38, 48, 0);

            // === BORDERS ===
            _borderDefault = Hud.Render.CreateBrush(180, 55, 58, 75, 1f);
            _borderAccent = Hud.Render.CreateBrush(255, 100, 160, 255, 1.5f);
            _borderSuccess = Hud.Render.CreateBrush(255, 80, 200, 120, 1.5f);
            _borderWarning = Hud.Render.CreateBrush(255, 255, 180, 80, 1.5f);
            _borderError = Hud.Render.CreateBrush(255, 255, 100, 100, 1.5f);

            // === BUTTONS ===
            _btnDefault = Hud.Render.CreateBrush(220, 45, 50, 65, 0);
            _btnHover = Hud.Render.CreateBrush(240, 60, 70, 95, 0);
            _btnActive = Hud.Render.CreateBrush(255, 80, 100, 140, 0);
            _btnDisabled = Hud.Render.CreateBrush(150, 35, 38, 48, 0);

            // === TOGGLES ===
            _toggleOn = Hud.Render.CreateBrush(255, 70, 190, 110, 0);
            _toggleOff = Hud.Render.CreateBrush(255, 110, 60, 60, 0);
            _toggleTrack = Hud.Render.CreateBrush(200, 45, 48, 60, 0);
            _toggleKnob = Hud.Render.CreateBrush(255, 240, 240, 245, 0);

            // === STATUS ===
            _statusSuccess = Hud.Render.CreateBrush(255, 60, 190, 100, 0);
            _statusWarning = Hud.Render.CreateBrush(255, 230, 170, 60, 0);
            _statusError = Hud.Render.CreateBrush(255, 230, 90, 90, 0);
            _statusInfo = Hud.Render.CreateBrush(255, 90, 160, 230, 0);

            // === HIGHLIGHTS ===
            _highlightProtected = Hud.Render.CreateBrush(160, 80, 220, 120, 2.5f);
            _highlightSalvage = Hud.Render.CreateBrush(160, 230, 90, 90, 2.5f);
            _highlightAncient = Hud.Render.CreateBrush(160, 255, 190, 90, 2.5f);
            _highlightPrimal = Hud.Render.CreateBrush(180, 255, 90, 90, 3f);
            _highlightStatRule = Hud.Render.CreateBrush(160, 180, 120, 255, 2.5f);

            // === PROGRESS ===
            _progressTrack = Hud.Render.CreateBrush(200, 40, 42, 55, 0);
            _progressFill = Hud.Render.CreateBrush(255, 80, 200, 120, 0);

            // === SCROLL ===
            _scrollTrack = Hud.Render.CreateBrush(150, 40, 42, 55, 0);
            _scrollThumb = Hud.Render.CreateBrush(200, 80, 85, 105, 0);
        }

        #endregion

        #region Key Handler

        public void OnKeyEvent(IKeyEvent keyEvent)
        {
            if (!Hud.Game.IsInGame) return;

            if (QuickToggleKey.Matches(keyEvent) && keyEvent.IsPressed)
            {
                bool anyEnabled = BlacklistMgr.Profiles.Values.Any(p => p.IsEnabled);
                foreach (var profile in BlacklistMgr.Profiles.Values)
                    profile.IsEnabled = !anyEnabled;
                BlacklistMgr.RebuildActiveBlacklist();
                SetStatus(anyEnabled ? "All profiles OFF" : "All profiles ON", anyEnabled ? StatusType.Warning : StatusType.Success);
                return;
            }

            if (ManagerKey.Matches(keyEvent) && keyEvent.IsPressed)
            {
                if (IsSalvageWindowOpen() || IsRepairWindowOpen())
                {
                    _showManager = !_showManager;
                    _scrollOffset = 0;
                }
                return;
            }

            if (SalvageKey.Matches(keyEvent) && keyEvent.IsPressed)
            {
                if (_isRunning)
                {
                    _isRunning = false;
                    _isSalvaging = false;
                    SetStatus("Stopped", StatusType.Warning);
                }
                else if (IsSalvageWindowOpen())
                {
                    _isRunning = true;
                    _salvageAttempted.Clear();
                    _salvageDone = 0;
                    _salvageTotal = GetItemsToSalvage().Count;
                    SetStatus("Starting...", StatusType.Info);
                }
            }
        }

        #endregion

        #region Main Processing

        public void AfterCollect()
        {
            if (!Hud.Game.IsInGame || !_isRunning) return;
            if (!ValidateState()) { _isRunning = false; _isSalvaging = false; return; }
            if (_isSalvaging) return;
            ProcessSalvage();
        }

        private bool ValidateState()
        {
            return Hud.Window.IsForeground && 
                   (IsSalvageWindowOpen() || IsRepairWindowOpen()) && 
                   Hud.Inventory.InventoryMainUiElement.Visible;
        }

        private void ProcessSalvage()
        {
            _isSalvaging = true;
            _lastCursorX = Hud.Window.CursorX;
            _lastCursorY = Hud.Window.CursorY;

            try
            {
                if (IsRepairWindowOpen() && !IsSalvageWindowOpen())
                {
                    Hud.Interaction.ClickUiElement(MouseButtons.Left, _salvageTab);
                    Hud.Wait(100);
                    Hud.ReCollect();
                    _isSalvaging = false;
                    return;
                }

                if (!IsSalvageWindowOpen()) { _isSalvaging = false; return; }

                var items = GetItemsToSalvage();
                _salvageTotal = items.Count;

                if (items.Count == 0)
                {
                    SetAnvil(false);
                    _isRunning = false;
                    _isSalvaging = false;
                    SetStatus("✓ Complete!", StatusType.Success);
                    Hud.Interaction.MouseMove(_lastCursorX, _lastCursorY, 1, 1);
                    return;
                }

                SetAnvil(true);
                Hud.Wait(50);

                foreach (var item in items)
                {
                    if (!_isRunning || !IsSalvageWindowOpen()) break;
                    if (_salvageAttempted.Contains(item.ItemUniqueId)) continue;

                    _salvageAttempted.Add(item.ItemUniqueId);
                    _salvageDone++;
                    _progressValue = (float)_salvageDone / _salvageTotal;

                    SetStatus($"Salvaging {_salvageDone}/{_salvageTotal}...", StatusType.Info);

                    Hud.Interaction.MoveMouseOverInventoryItem(item);
                    Hud.Wait(10);
                    Hud.Interaction.MouseDown(MouseButtons.Left);
                    Hud.Wait(5);
                    Hud.Interaction.MouseUp(MouseButtons.Left);

                    if (item.IsLegendary)
                    {
                        Hud.Wait(50);
                        if (_okButton.Visible) { Hud.Interaction.PressEnter(); Hud.Wait(30); }
                    }
                    Hud.Wait(20);
                }

                SetAnvil(false);
            }
            finally
            {
                Hud.Interaction.MouseMove(_lastCursorX, _lastCursorY, 1, 1);
                _isSalvaging = false;
            }
        }

        private void SetAnvil(bool enabled)
        {
            IUiElement btn = _salvageButton1?.Visible == true ? _salvageButton1 : 
                             _salvageButton2?.Visible == true ? _salvageButton2 : null;
            if (btn == null) return;
            bool isEnabled = btn.AnimState == 19 || btn.AnimState == 20;
            if (enabled != isEnabled) { Hud.Interaction.ClickUiElement(MouseButtons.Left, btn); Hud.Wait(100); }
        }

        private List<IItem> GetItemsToSalvage()
        {
            var result = new List<IItem>();
            foreach (var item in Hud.Inventory.ItemsInInventory)
                if (CanSalvage(item)) result.Add(item);
            result.Sort((a, b) => a.InventoryX != b.InventoryX ? a.InventoryX.CompareTo(b.InventoryX) : a.InventoryY.CompareTo(b.InventoryY));
            return result;
        }

        private bool CanSalvage(IItem item)
        {
            if (item?.SnoItem == null) return false;
            if (item.SnoItem.Kind != ItemKind.loot && item.SnoItem.Kind != ItemKind.potion) return false;

            // === GLOBAL PROTECTION RULES ===
            var gr = RulesMgr.GlobalRules;
            
            if (gr.ProtectLockedSlots && item.IsInventoryLocked) return false;
            if (gr.ProtectSocketedItems && item.ItemsInSocket?.Length > 0) return false;
            if (gr.ProtectEnchantedItems && item.EnchantedAffixCounter > 0) return false;
            if (gr.ProtectArmoryItems && IsInArmorySet(item)) return false;
            if (item.VendorBought || item.Quantity > 1 || item.Location != ItemLocation.Inventory) return false;

            // === GLOBAL ALWAYS KEEP RULES ===
            if (gr.AlwaysKeepPrimals && item.AncientRank == 2) return false;
            if (gr.AlwaysKeepAncients && item.AncientRank == 1) return false;
            if (gr.AlwaysKeepSetItems && item.SetSno != 0) return false;
            if (gr.AlwaysKeepHighPerfection && item.Perfection * 100 >= gr.HighPerfectionThreshold) return false;

            // === STAT-BASED RULES ===
            string itemName = item.SnoItem.NameEnglish ?? item.SnoItem.NameLocalized ?? "";
            var statRule = RulesMgr.GetRule(itemName);
            if (statRule != null)
            {
                // Item has a stat rule - check if it meets requirements
                bool meetsRequirements = EvaluateStatRule(item, statRule);
                if (meetsRequirements) return false;  // Keep it!
                // Doesn't meet requirements - continue to salvage
            }

            // === BLACKLIST CHECK ===
            string localName = item.SnoItem.NameLocalized ?? "";
            string fullName = item.FullNameLocalized ?? "";
            if (BlacklistMgr.IsBlacklisted(localName, fullName, itemName)) return false;

            // === GLOBAL ALWAYS SALVAGE RULES ===
            // Note: These only apply if item wasn't protected above

            // === ITEM TYPE FILTERS ===
            var mainGroup = item.SnoItem.MainGroupCode ?? "";
            string[] skipGroups = { "riftkeystone", "horadriccache", "plans", "-", "pony", "gems_unique" };
            if (skipGroups.Any(g => mainGroup.Contains(g)) || mainGroup.Contains("cosmetic")) return false;

            string[] skipNames = { "Staff of Herding", "Hellforge Ember", "Puzzle Ring", "Bovine Bardiche", "Ramaladni's Gift" };
            if (skipNames.Contains(itemName)) return false;
            if (item.SnoItem.Code?.StartsWith("P72_Soulshard") == true) return false;

            return true;
        }

        private bool EvaluateStatRule(IItem item, StatRule rule)
        {
            if (rule.Conditions == null || rule.Conditions.Count == 0) return true;

            var results = new List<bool>();
            foreach (var cond in rule.Conditions)
            {
                double statValue = GetStatValue(item, cond.Stat);
                bool matches = false;
                
                switch (cond.Operator)
                {
                    case CompareOp.GreaterThan: matches = statValue > cond.Value; break;
                    case CompareOp.GreaterOrEqual: matches = statValue >= cond.Value; break;
                    case CompareOp.LessThan: matches = statValue < cond.Value; break;
                    case CompareOp.LessOrEqual: matches = statValue <= cond.Value; break;
                    case CompareOp.Equal: matches = Math.Abs(statValue - cond.Value) < 0.001; break;
                }
                results.Add(matches);
            }

            return rule.Logic == RuleLogic.And ? results.All(r => r) : results.Any(r => r);
        }

        private double GetStatValue(IItem item, StatType stat)
        {
            if (item.Perfections == null) return 0;

            foreach (var perf in item.Perfections)
            {
                if (perf?.Attribute == null) continue;
                string code = perf.Attribute.Code ?? "";

                bool match = false;
                switch (stat)
                {
                    case StatType.CooldownReduction: match = code.Contains("Cooldown_Reduction"); break;
                    case StatType.ResourceCostReduction: match = code.Contains("Resource_Cost_Reduction"); break;
                    case StatType.CritChance: match = code.Contains("Crit_Percent"); break;
                    case StatType.CritDamage: match = code.Contains("Crit_Damage"); break;
                    case StatType.AttackSpeed: match = code.Contains("Attacks_Per_Second"); break;
                    case StatType.AreaDamage: match = code.Contains("Area_Damage"); break;
                    case StatType.Strength: match = code == "Strength_Item"; break;
                    case StatType.Dexterity: match = code == "Dexterity_Item"; break;
                    case StatType.Intelligence: match = code == "Intelligence_Item"; break;
                    case StatType.Vitality: match = code == "Vitality_Item"; break;
                    case StatType.AllResist: match = code == "Resistance_All"; break;
                    case StatType.Armor: match = code.Contains("Armor"); break;
                    case StatType.LifePercent: match = code.Contains("Hitpoints_Max_Percent"); break;
                    case StatType.LifePerHit: match = code.Contains("Hitpoints_On_Hit"); break;
                    case StatType.SocketCount: match = code == "Sockets"; break;
                }

                if (match)
                {
                    // Convert to percentage if needed
                    if (stat == StatType.CooldownReduction || stat == StatType.ResourceCostReduction ||
                        stat == StatType.CritChance || stat == StatType.CritDamage ||
                        stat == StatType.AttackSpeed || stat == StatType.AreaDamage ||
                        stat == StatType.LifePercent)
                    {
                        return perf.Cur * 100;
                    }
                    return perf.Cur;
                }
            }

            // Special cases
            if (stat == StatType.Perfection) return item.Perfection * 100;
            if (stat == StatType.AncientRank) return item.AncientRank;
            if (stat == StatType.SocketCount) return item.SocketCount;

            return 0;
        }

        private bool IsInArmorySet(IItem item)
        {
            return Hud.Game.Me.ArmorySets?.Any(s => s?.ContainsItem(item) == true) ?? false;
        }

        private bool IsSalvageWindowOpen() => _salvageDialog?.Visible == true;
        private bool IsRepairWindowOpen() => _repairDialog?.Visible == true;

        #endregion

        #region UI Rendering

        public void PaintTopInGame(ClipState clipState)
        {
            if (clipState != ClipState.Inventory || !Hud.Game.IsInGame) return;
            if (!IsSalvageWindowOpen() && !IsRepairWindowOpen()) return;

            // Update animations
            UpdateAnimations();

            _clickAreas.Clear();
            _tooltipText = null;

            DrawMainPanel();
            if (_panelSlide > 0.02f) DrawManagerPanel();
            if (IsSalvageWindowOpen()) DrawItemHighlights();

            HandleMouseInput();
            DrawTooltip();
        }

        private void UpdateAnimations()
        {
            float targetSlide = _showManager ? 1f : 0f;
            _panelSlide += (targetSlide - _panelSlide) * 0.18f;
            _pulsePhase = (_pulseTimer.ElapsedMilliseconds % 2000) / 2000f;
        }

        private void DrawMainPanel()
        {
            var inv = Hud.Inventory.InventoryMainUiElement.Rectangle;
            float w = 230, h = 145;
            float x = Math.Max(10, inv.X - w - 15);
            float y = inv.Y;

            // Panel background with glow when running
            _surfaceBase.DrawRectangle(x, y, w, h);
            var border = _isRunning ? _borderSuccess : _borderDefault;
            border.DrawRectangle(x, y, w, h);

            // Accent bar
            float accentW = 3;
            var accent = _isRunning ? _statusSuccess : _statusInfo;
            accent.DrawRectangle(x, y, accentW, h);

            float pad = 14, cx = x + pad + accentW, cy = y + pad, cw = w - pad * 2 - accentW;

            // Title row
            string icon = _isRunning ? "⚡" : "🔧";
            var titleLayout = _fontTitle.GetTextLayout($"{icon} Smart Salvage");
            _fontTitle.DrawText(titleLayout, cx, cy);

            // Version badge
            var verLayout = _fontMicro.GetTextLayout("v3.0");
            _fontMicro.DrawText(verLayout, cx + cw - verLayout.Metrics.Width, cy + 2);
            cy += titleLayout.Metrics.Height + 10;

            // Progress/Status
            if (_isRunning)
            {
                // Progress bar with animation
                float barH = 5;
                _progressTrack.DrawRectangle(cx, cy, cw, barH);
                _progressFill.DrawRectangle(cx, cy, cw * _progressValue, barH);
                cy += barH + 6;

                var statusLayout = _fontSuccess.GetTextLayout(_statusMessage ?? "Running...");
                _fontSuccess.DrawText(statusLayout, cx, cy);
            }
            else if (_statusTimer.IsRunning && _statusTimer.ElapsedMilliseconds < 4000)
            {
                var font = GetStatusFont(_statusType);
                var statusLayout = font.GetTextLayout(_statusMessage ?? "");
                font.DrawText(statusLayout, cx, cy);
            }
            else
            {
                var infoLayout = _fontMuted.GetTextLayout("Press U to start salvage");
                _fontMuted.DrawText(infoLayout, cx, cy);
            }
            cy += 22;

            // Stats badges
            int toSalvage = IsSalvageWindowOpen() ? GetItemsToSalvage().Count : 0;
            int protect = BlacklistMgr.GetActiveItemCount();
            int rules = RulesMgr.StatRules.Count(r => r.IsEnabled);

            DrawStatBadge(cx, cy, "🛡", protect.ToString(), _statusSuccess);
            DrawStatBadge(cx + 55, cy, "🗑", toSalvage.ToString(), toSalvage > 0 ? _statusError : _statusInfo);
            DrawStatBadge(cx + 105, cy, "📐", rules.ToString(), _statusInfo);
            cy += 28;

            // Manager button
            float btnH = 26;
            var btnRect = new RectangleF(cx, cy, cw, btnH);
            _clickAreas["manager"] = btnRect;
            bool hovered = IsMouseOver(btnRect);
            DrawButton(btnRect, _showManager ? "▼ Close Panel" : "▶ Open Panel [Shift+U]", hovered);
        }

        private void DrawManagerPanel()
        {
            var inv = Hud.Inventory.InventoryMainUiElement.Rectangle;
            float w = 320, h = 520;
            float baseX = inv.X - w - 15;
            float x = baseX - (1f - _panelSlide) * 40;
            float y = inv.Y + 155;

            if (x < 10) x = 10;
            if (y + h > Hud.Window.Size.Height - 20) h = Hud.Window.Size.Height - y - 20;

            byte alpha = (byte)(_panelSlide * 250);
            var panelBrush = Hud.Render.CreateBrush(alpha, 18, 18, 24, 0);
            panelBrush.DrawRectangle(x, y, w, h);
            
            var borderBrush = Hud.Render.CreateBrush((byte)(_panelSlide * 180), 55, 58, 75, 1f);
            borderBrush.DrawRectangle(x, y, w, h);

            float pad = 14, cx = x + pad, cy = y + pad, cw = w - pad * 2;

            // Tab bar
            string[] tabs = { "profiles", "rules", "global", "import" };
            string[] labels = { "📋 Profiles", "📐 Rules", "⚙ Global", "🌐 Import" };
            float tabW = (cw - 12) / 4;
            float tabH = 28;

            for (int i = 0; i < 4; i++)
            {
                var rect = new RectangleF(cx + (tabW + 4) * i, cy, tabW, tabH);
                _clickAreas[$"tab_{tabs[i]}"] = rect;
                bool active = _activeTab == tabs[i];
                bool hover = IsMouseOver(rect);

                var brush = active ? _btnActive : (hover ? _btnHover : _btnDefault);
                brush.DrawRectangle(rect);

                var font = active ? _fontBody : _fontSmall;
                var layout = font.GetTextLayout(labels[i]);
                font.DrawText(layout, rect.X + (tabW - layout.Metrics.Width) / 2, rect.Y + (tabH - layout.Metrics.Height) / 2);
            }
            cy += tabH + 14;

            // Tab content
            float contentH = h - (cy - y) - pad;
            switch (_activeTab)
            {
                case "profiles": DrawProfilesTab(cx, cy, cw, contentH); break;
                case "rules": DrawRulesTab(cx, cy, cw, contentH); break;
                case "global": DrawGlobalTab(cx, cy, cw, contentH); break;
                case "import": DrawImportTab(cx, cy, cw, contentH); break;
            }
        }

        private void DrawProfilesTab(float x, float y, float w, float h)
        {
            // Header
            var headerLayout = _fontHeader.GetTextLayout($"Build Profiles ({BlacklistMgr.GetEnabledProfileCount()}/{BlacklistMgr.Profiles.Count})");
            _fontHeader.DrawText(headerLayout, x, y);
            y += headerLayout.Metrics.Height + 10;

            // Quick actions
            float btnW = (w - 10) / 2, btnH = 24;
            var enableRect = new RectangleF(x, y, btnW, btnH);
            var disableRect = new RectangleF(x + btnW + 10, y, btnW, btnH);
            _clickAreas["enable_all"] = enableRect;
            _clickAreas["disable_all"] = disableRect;
            DrawButton(enableRect, "✓ Enable All", IsMouseOver(enableRect));
            DrawButton(disableRect, "✗ Disable All", IsMouseOver(disableRect));
            y += btnH + 12;

            // Profile list
            var profiles = BlacklistMgr.Profiles.Values
                .OrderByDescending(p => p.IsEnabled)
                .ThenBy(p => p.DisplayName)
                .ToList();

            float rowH = 42;
            int visible = Math.Min(_maxVisibleItems, profiles.Count - _scrollOffset);

            for (int i = 0; i < visible && i + _scrollOffset < profiles.Count; i++)
            {
                var p = profiles[i + _scrollOffset];
                var rect = new RectangleF(x, y, w - 14, rowH);
                _clickAreas[$"profile_{p.Id}"] = rect;

                bool hover = IsMouseOver(rect);
                var rowBrush = hover ? _surfaceOverlay : _surfaceCard;
                rowBrush.DrawRectangle(rect);

                // Toggle
                float toggleW = 40, toggleH = 20;
                float tx = x + 8, ty = y + (rowH - toggleH) / 2;
                _toggleTrack.DrawRectangle(tx, ty, toggleW, toggleH);
                
                float knobSize = toggleH - 4;
                float knobX = p.IsEnabled ? tx + toggleW - knobSize - 2 : tx + 2;
                var knobBrush = p.IsEnabled ? _toggleOn : _toggleOff;
                knobBrush.DrawRectangle(knobX, ty + 2, knobSize, knobSize);

                // Info
                float textX = tx + toggleW + 12;
                string icon = p.Icon ?? "📋";
                string name = p.DisplayName;
                if (name.Length > 22) name = name.Substring(0, 19) + "...";

                var nameFont = p.IsEnabled ? _fontBody : _fontMuted;
                var nameLayout = nameFont.GetTextLayout($"{icon} {name}");
                nameFont.DrawText(nameLayout, textX, y + 6);

                var countLayout = _fontMicro.GetTextLayout($"{p.Items.Count} items");
                _fontMicro.DrawText(countLayout, textX, y + 6 + nameLayout.Metrics.Height + 2);

                y += rowH + 3;
            }

            // Scrollbar
            if (profiles.Count > _maxVisibleItems)
            {
                float sx = x + w - 10, sh = rowH * visible, sy = y - sh - 3;
                _scrollTrack.DrawRectangle(sx, sy, 6, sh);
                float thumbH = sh * ((float)visible / profiles.Count);
                float thumbY = sy + (sh - thumbH) * ((float)_scrollOffset / Math.Max(1, profiles.Count - visible));
                _scrollThumb.DrawRectangle(sx, thumbY, 6, thumbH);

                _clickAreas["scroll_up"] = new RectangleF(sx - 15, sy, 25, sh / 2);
                _clickAreas["scroll_down"] = new RectangleF(sx - 15, sy + sh / 2, 25, sh / 2);
            }
        }

        private void DrawRulesTab(float x, float y, float w, float h)
        {
            var headerLayout = _fontHeader.GetTextLayout("Stat-Based Rules");
            _fontHeader.DrawText(headerLayout, x, y);
            y += headerLayout.Metrics.Height + 6;

            var descLayout = _fontMuted.GetTextLayout("Keep items only if they meet stat requirements");
            _fontMuted.DrawText(descLayout, x, y);
            y += descLayout.Metrics.Height + 12;

            // Rules list
            float rowH = 48;
            foreach (var rule in RulesMgr.StatRules)
            {
                var rect = new RectangleF(x, y, w - 14, rowH);
                _clickAreas[$"rule_{rule.Id}"] = rect;

                bool hover = IsMouseOver(rect);
                var rowBrush = hover ? _surfaceOverlay : _surfaceCard;
                rowBrush.DrawRectangle(rect);

                // Toggle
                float toggleW = 40, toggleH = 20;
                float tx = x + 8, ty = y + (rowH - toggleH) / 2;
                _toggleTrack.DrawRectangle(tx, ty, toggleW, toggleH);
                
                float knobSize = toggleH - 4;
                float knobX = rule.IsEnabled ? tx + toggleW - knobSize - 2 : tx + 2;
                var knobBrush = rule.IsEnabled ? _toggleOn : _toggleOff;
                knobBrush.DrawRectangle(knobX, ty + 2, knobSize, knobSize);

                // Info
                float textX = tx + toggleW + 12;
                var nameFont = rule.IsEnabled ? _fontBody : _fontMuted;
                var nameLayout = nameFont.GetTextLayout($"📐 {rule.ItemName}");
                nameFont.DrawText(nameLayout, textX, y + 6);

                // Conditions summary
                string condText = string.Join(rule.Logic == RuleLogic.And ? " AND " : " OR ", 
                    rule.Conditions.Select(c => c.ToString()));
                if (condText.Length > 35) condText = condText.Substring(0, 32) + "...";
                var condLayout = _fontMicro.GetTextLayout(condText);
                _fontMicro.DrawText(condLayout, textX, y + 8 + nameLayout.Metrics.Height);

                y += rowH + 3;
            }

            // Info
            y += 10;
            var infoLayout = _fontMuted.GetTextLayout("Example: Keep 'Dawn' only if CDR ≥ 8%");
            _fontMuted.DrawText(infoLayout, x, y);
        }

        private void DrawGlobalTab(float x, float y, float w, float h)
        {
            var headerLayout = _fontHeader.GetTextLayout("Global Rules");
            _fontHeader.DrawText(headerLayout, x, y);
            y += headerLayout.Metrics.Height + 6;

            var descLayout = _fontMuted.GetTextLayout("These rules override all other settings");
            _fontMuted.DrawText(descLayout, x, y);
            y += descLayout.Metrics.Height + 14;

            var gr = RulesMgr.GlobalRules;

            // Always Keep section
            var keepHeader = _fontAccent.GetTextLayout("🛡 Always Keep:");
            _fontAccent.DrawText(keepHeader, x, y);
            y += keepHeader.Metrics.Height + 8;

            DrawGlobalToggle(x, y, w, "Primal Items", gr.AlwaysKeepPrimals, "global_primals");
            y += 32;
            DrawGlobalToggle(x, y, w, "Ancient Items", gr.AlwaysKeepAncients, "global_ancients");
            y += 32;
            DrawGlobalToggle(x, y, w, "Set Items", gr.AlwaysKeepSetItems, "global_sets");
            y += 32;
            DrawGlobalToggle(x, y, w, $"High Perfection (≥{gr.HighPerfectionThreshold:F0}%)", gr.AlwaysKeepHighPerfection, "global_highperf");
            y += 40;

            // Protection section
            var protHeader = _fontAccent.GetTextLayout("🔒 Protection:");
            _fontAccent.DrawText(protHeader, x, y);
            y += protHeader.Metrics.Height + 8;

            DrawGlobalToggle(x, y, w, "Socketed Items", gr.ProtectSocketedItems, "global_socketed");
            y += 32;
            DrawGlobalToggle(x, y, w, "Enchanted Items", gr.ProtectEnchantedItems, "global_enchanted");
            y += 32;
            DrawGlobalToggle(x, y, w, "Armory Items", gr.ProtectArmoryItems, "global_armory");
            y += 32;
            DrawGlobalToggle(x, y, w, "Locked Slots", gr.ProtectLockedSlots, "global_locked");
        }

        private void DrawGlobalToggle(float x, float y, float w, string label, bool value, string clickId)
        {
            var rect = new RectangleF(x, y - 4, w - 14, 28);
            _clickAreas[clickId] = rect;

            bool hover = IsMouseOver(rect);
            if (hover) _surfaceOverlay.DrawRectangle(rect);

            var labelLayout = _fontBody.GetTextLayout(label);
            _fontBody.DrawText(labelLayout, x + 8, y);

            // Toggle on right
            float toggleW = 40, toggleH = 18;
            float tx = x + w - 14 - toggleW - 8, ty = y - 1;
            _toggleTrack.DrawRectangle(tx, ty, toggleW, toggleH);
            
            float knobSize = toggleH - 4;
            float knobX = value ? tx + toggleW - knobSize - 2 : tx + 2;
            var knobBrush = value ? _toggleOn : _toggleOff;
            knobBrush.DrawRectangle(knobX, ty + 2, knobSize, knobSize);
        }

        private void DrawImportTab(float x, float y, float w, float h)
        {
            var headerLayout = _fontHeader.GetTextLayout("Import from Build Guides");
            _fontHeader.DrawText(headerLayout, x, y);
            y += headerLayout.Metrics.Height + 6;

            var sitesLayout = _fontMuted.GetTextLayout("Supported: Maxroll.gg • Icy-Veins.com");
            _fontMuted.DrawText(sitesLayout, x, y);
            y += sitesLayout.Metrics.Height + 14;

            // URL input
            var labelLayout = _fontBody.GetTextLayout("Build Guide URL:");
            _fontBody.DrawText(labelLayout, x, y);
            y += labelLayout.Metrics.Height + 6;

            float inputH = 30;
            var inputRect = new RectangleF(x, y, w - 14, inputH);
            _clickAreas["import_input"] = inputRect;

            bool inputHover = IsMouseOver(inputRect);
            var inputBrush = inputHover ? _surfaceOverlay : _surfaceCard;
            inputBrush.DrawRectangle(inputRect);
            _borderDefault.DrawRectangle(inputRect);

            string displayUrl = string.IsNullOrEmpty(_importUrl) ? "Click to paste URL from clipboard..." : _importUrl;
            if (displayUrl.Length > 42) displayUrl = displayUrl.Substring(0, 39) + "...";
            var urlFont = string.IsNullOrEmpty(_importUrl) ? _fontMuted : _fontMono;
            var urlLayout = urlFont.GetTextLayout(displayUrl);
            urlFont.DrawText(urlLayout, x + 10, y + (inputH - urlLayout.Metrics.Height) / 2);
            y += inputH + 12;

            // Import button
            float btnH = 34;
            var importRect = new RectangleF(x, y, w - 14, btnH);
            _clickAreas["import_btn"] = importRect;
            string importText = _isImporting ? "⏳ Importing..." : "🌐 Import Build";
            DrawButton(importRect, importText, IsMouseOver(importRect), _isImporting);
            y += btnH + 10;

            // Status
            if (!string.IsNullOrEmpty(_importStatus))
            {
                var statusFont = GetStatusFont(_importStatusType);
                var statusLayout = statusFont.GetTextLayout(_importStatus);
                statusFont.DrawText(statusLayout, x, y);
                y += statusLayout.Metrics.Height + 10;
            }

            // Save/Export
            y = h - 40;
            float actionBtnW = (w - 24) / 2;
            var saveRect = new RectangleF(x, y, actionBtnW, 28);
            var exportRect = new RectangleF(x + actionBtnW + 10, y, actionBtnW, 28);
            _clickAreas["save"] = saveRect;
            _clickAreas["export"] = exportRect;
            DrawButton(saveRect, "💾 Save", IsMouseOver(saveRect));
            DrawButton(exportRect, "📤 Export", IsMouseOver(exportRect));
        }

        private void DrawItemHighlights()
        {
            var toSalvage = new HashSet<string>(GetItemsToSalvage().Select(i => i.ItemUniqueId));

            foreach (var item in Hud.Inventory.ItemsInInventory)
            {
                var rect = Hud.Inventory.GetItemRect(item);
                if (rect == RectangleF.Empty) continue;

                if (toSalvage.Contains(item.ItemUniqueId))
                {
                    var brush = item.AncientRank == 2 ? _highlightPrimal :
                                item.AncientRank == 1 ? _highlightAncient : _highlightSalvage;
                    brush.DrawRectangle(rect);
                }
                else
                {
                    // Check if protected by stat rule
                    string name = item.SnoItem.NameEnglish ?? "";
                    var rule = RulesMgr.GetRule(name);
                    if (rule != null && EvaluateStatRule(item, rule))
                    {
                        _highlightStatRule.DrawRectangle(rect);
                    }
                    else if (IsItemBlacklisted(item))
                    {
                        _highlightProtected.DrawRectangle(rect);
                    }
                }
            }
        }

        private bool IsItemBlacklisted(IItem item)
        {
            string a = item.SnoItem.NameLocalized ?? "";
            string b = item.FullNameLocalized ?? "";
            string c = item.SnoItem.NameEnglish ?? "";
            return BlacklistMgr.IsBlacklisted(a, b, c);
        }

        #endregion

        #region UI Helpers

        private void DrawButton(RectangleF rect, string text, bool hovered, bool disabled = false)
        {
            var brush = disabled ? _btnDisabled : (hovered ? _btnHover : _btnDefault);
            brush.DrawRectangle(rect);
            var layout = _fontBody.GetTextLayout(text);
            _fontBody.DrawText(layout, rect.X + (rect.Width - layout.Metrics.Width) / 2,
                              rect.Y + (rect.Height - layout.Metrics.Height) / 2);
        }

        private void DrawStatBadge(float x, float y, string icon, string value, IBrush bg)
        {
            var iconLayout = _fontIcon.GetTextLayout(icon);
            var valLayout = _fontBody.GetTextLayout(value);
            float w = iconLayout.Metrics.Width + valLayout.Metrics.Width + 12;
            float h = Math.Max(iconLayout.Metrics.Height, valLayout.Metrics.Height) + 6;

            bg.DrawRectangle(x, y, w, h);
            _fontIcon.DrawText(iconLayout, x + 4, y + 3);
            _fontBody.DrawText(valLayout, x + iconLayout.Metrics.Width + 8, y + 3);
        }

        private void DrawTooltip()
        {
            if (string.IsNullOrEmpty(_tooltipText)) return;
            // TODO: Implement tooltip rendering
        }

        private IFont GetStatusFont(StatusType type)
        {
            switch (type)
            {
                case StatusType.Success: return _fontSuccess;
                case StatusType.Warning: return _fontWarning;
                case StatusType.Error: return _fontError;
                default: return _fontAccent;
            }
        }

        private void SetStatus(string msg, StatusType type)
        {
            _statusMessage = msg;
            _statusType = type;
            _statusTimer.Restart();
        }

        private bool IsMouseOver(RectangleF rect)
        {
            int mx = Hud.Window.CursorX, my = Hud.Window.CursorY;
            return mx >= rect.X && mx <= rect.X + rect.Width && my >= rect.Y && my <= rect.Y + rect.Height;
        }

        #endregion

        #region Mouse Input

        private void HandleMouseInput()
        {
            if (_isRunning) return;

            bool down = Hud.Input.IsKeyDown(Keys.LButton);
            bool clicked = !down && _wasMouseDown && _clickTimer.ElapsedMilliseconds > 150;
            _wasMouseDown = down;

            if (!clicked) return;
            _clickTimer.Restart();

            foreach (var kvp in _clickAreas)
                if (IsMouseOver(kvp.Value)) { HandleClick(kvp.Key); return; }
        }

        private void HandleClick(string id)
        {
            if (id == "manager") { _showManager = !_showManager; _scrollOffset = 0; return; }

            if (id.StartsWith("tab_")) { _activeTab = id.Substring(4); return; }

            if (id.StartsWith("profile_"))
            {
                BlacklistMgr.ToggleProfile(id.Substring(8));
                return;
            }

            if (id.StartsWith("rule_"))
            {
                RulesMgr.ToggleRule(id.Substring(5));
                return;
            }

            // Global toggles
            var gr = RulesMgr.GlobalRules;
            switch (id)
            {
                case "global_primals": gr.AlwaysKeepPrimals = !gr.AlwaysKeepPrimals; return;
                case "global_ancients": gr.AlwaysKeepAncients = !gr.AlwaysKeepAncients; return;
                case "global_sets": gr.AlwaysKeepSetItems = !gr.AlwaysKeepSetItems; return;
                case "global_highperf": gr.AlwaysKeepHighPerfection = !gr.AlwaysKeepHighPerfection; return;
                case "global_socketed": gr.ProtectSocketedItems = !gr.ProtectSocketedItems; return;
                case "global_enchanted": gr.ProtectEnchantedItems = !gr.ProtectEnchantedItems; return;
                case "global_armory": gr.ProtectArmoryItems = !gr.ProtectArmoryItems; return;
                case "global_locked": gr.ProtectLockedSlots = !gr.ProtectLockedSlots; return;
            }

            if (id == "enable_all")
            {
                foreach (var p in BlacklistMgr.Profiles.Values) p.IsEnabled = true;
                BlacklistMgr.RebuildActiveBlacklist();
                SetStatus("All profiles enabled", StatusType.Success);
                return;
            }

            if (id == "disable_all")
            {
                foreach (var p in BlacklistMgr.Profiles.Values) p.IsEnabled = false;
                BlacklistMgr.RebuildActiveBlacklist();
                SetStatus("All profiles disabled", StatusType.Warning);
                return;
            }

            if (id == "scroll_up") { _scrollOffset = Math.Max(0, _scrollOffset - 1); return; }
            if (id == "scroll_down")
            {
                int max = Math.Max(0, BlacklistMgr.Profiles.Count - _maxVisibleItems);
                _scrollOffset = Math.Min(max, _scrollOffset + 1);
                return;
            }

            if (id == "import_input")
            {
                try { if (Clipboard.ContainsText()) { _importUrl = Clipboard.GetText().Trim(); _importStatus = "URL pasted"; _importStatusType = StatusType.Info; } }
                catch { _importStatus = "Clipboard error"; _importStatusType = StatusType.Error; }
                return;
            }

            if (id == "import_btn" && !_isImporting) { StartImport(); return; }

            if (id == "save") { BlacklistMgr.SaveToFile(); SetStatus("✓ Saved!", StatusType.Success); return; }

            if (id == "export")
            {
                try { Clipboard.SetText(BlacklistMgr.ExportAllProfiles()); SetStatus("✓ Exported!", StatusType.Success); }
                catch { SetStatus("Export failed", StatusType.Error); }
            }
        }

        private void StartImport()
        {
            if (string.IsNullOrWhiteSpace(_importUrl))
            {
                _importStatus = "Paste a URL first";
                _importStatusType = StatusType.Warning;
                return;
            }

            if (!_importUrl.Contains("maxroll.gg") && !_importUrl.Contains("icy-veins.com"))
            {
                _importStatus = "Unsupported site";
                _importStatusType = StatusType.Error;
                return;
            }

            _isImporting = true;
            _importStatus = "Fetching...";
            _importStatusType = StatusType.Info;

            try
            {
                var data = Crawler.CrawlGuide(_importUrl);
                if (data?.Items.Count > 0)
                {
                    var profile = BlacklistMgr.ImportFromMaxrollData(data);
                    if (profile != null)
                    {
                        _importStatus = $"✓ {profile.DisplayName} ({profile.Items.Count} items)";
                        _importStatusType = StatusType.Success;
                        _importUrl = "";
                        BlacklistMgr.SaveToFile();
                    }
                    else { _importStatus = "Failed to create profile"; _importStatusType = StatusType.Error; }
                }
                else { _importStatus = Crawler.LastError ?? "No items found"; _importStatusType = StatusType.Error; }
            }
            catch (Exception ex) { _importStatus = ex.Message; _importStatusType = StatusType.Error; }
            finally { _isImporting = false; }
        }

        #endregion

        #region Public API

        public void AddToBlacklist(params string[] items) => BlacklistMgr.AddCustomItems(items);
        public void RemoveFromBlacklist(params string[] items) => BlacklistMgr.RemoveCustomItems(items);
        public void SetBuildEnabled(string id, bool enabled) => BlacklistMgr.SetProfileEnabled(id, enabled);

        #endregion
    }

    public enum StatusType { Info, Success, Warning, Error }
}
