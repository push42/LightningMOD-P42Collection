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
    /// Smart Salvage Plugin v2.0 - Beautiful & Enhanced
    /// 
    /// Features:
    /// - Modern, responsive UI with animations
    /// - Build profile management
    /// - Import from Maxroll AND Icy Veins
    /// - Visual item highlights in inventory
    /// - Quick actions and keyboard shortcuts
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
        public int SalvageAncient { get; set; }  // 0=smart, 1=never, 2=always
        public int SalvagePrimal { get; set; }   // 0=smart, 1=never, 2=always

        public BlacklistManager BlacklistMgr { get; private set; }
        public MaxrollCrawler Crawler { get; private set; }

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

        #region Private Fields - Fonts & Brushes

        // Modern color scheme
        private IFont _titleFont;
        private IFont _headerFont;
        private IFont _bodyFont;
        private IFont _smallFont;
        private IFont _accentFont;
        private IFont _successFont;
        private IFont _warningFont;
        private IFont _monoFont;

        // Panel backgrounds
        private IBrush _panelBrush;
        private IBrush _panelDarkBrush;
        private IBrush _headerBrush;
        private IBrush _borderBrush;
        private IBrush _accentBorderBrush;

        // Interactive elements
        private IBrush _buttonBrush;
        private IBrush _buttonHoverBrush;
        private IBrush _buttonActiveBrush;
        private IBrush _inputBrush;
        private IBrush _inputFocusBrush;

        // Status indicators
        private IBrush _successBrush;
        private IBrush _warningBrush;
        private IBrush _errorBrush;
        private IBrush _infoBrush;

        // Toggles
        private IBrush _toggleOnBrush;
        private IBrush _toggleOffBrush;
        private IBrush _toggleTrackBrush;

        // Item highlights
        private IBrush _protectedHighlight;
        private IBrush _salvageHighlight;
        private IBrush _ancientHighlight;
        private IBrush _primalHighlight;

        // Scrollbar
        private IBrush _scrollTrackBrush;
        private IBrush _scrollThumbBrush;

        #endregion

        #region Private Fields - State

        private bool _isRunning;
        private bool _isSalvaging;
        private IWatch _timer;
        private IWatch _statusTimer;
        private IWatch _animTimer;
        private IWatch _clickTimer;

        private int _lastCursorX;
        private int _lastCursorY;
        private HashSet<string> _salvageAttempted;
        private string _statusMessage;
        private StatusType _statusType;

        // UI State
        private bool _showManager;
        private int _scrollOffset;
        private int _maxVisibleProfiles = 10;
        private string _hoveredProfileId;
        private string _hoveredButton;
        private string _activeTab = "profiles";  // profiles, import, settings

        // Import state
        private string _importUrl;
        private bool _isImporting;
        private string _importStatus;
        private StatusType _importStatusType;

        // Click tracking
        private bool _wasMouseDown;
        private Dictionary<string, RectangleF> _clickAreas;

        // Animation
        private float _panelAlpha = 0f;
        private float _progressValue = 0f;
        private int _salvageTotal = 0;
        private int _salvageDone = 0;

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

            // Settings
            AutoRepair = true;
            SalvageAncient = 1;
            SalvagePrimal = 1;

            // Initialize managers
            BlacklistMgr = new BlacklistManager();
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            BlacklistMgr.DataDirectory = Path.Combine(basePath, "plugins", "Custom", "SmartSalvage");
            BlacklistMgr.InitializeBuiltInProfiles();
            BlacklistMgr.LoadFromFile();

            Crawler = new MaxrollCrawler();

            _salvageAttempted = new HashSet<string>();
            _clickAreas = new Dictionary<string, RectangleF>();

            // UI Elements
            InitializeUiElements();
            InitializeFonts();
            InitializeBrushes();

            // Timers
            _timer = Hud.Time.CreateAndStartWatch();
            _statusTimer = Hud.Time.CreateWatch();
            _animTimer = Hud.Time.CreateAndStartWatch();
            _clickTimer = Hud.Time.CreateAndStartWatch();

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

        private void InitializeFonts()
        {
            // Modern font hierarchy
            _titleFont = Hud.Render.CreateFont("segoe ui", 11, 255, 255, 215, 100, true, false, 220, 0, 0, 0, true);
            _headerFont = Hud.Render.CreateFont("segoe ui", 9, 255, 240, 240, 245, true, false, 200, 0, 0, 0, true);
            _bodyFont = Hud.Render.CreateFont("segoe ui", 7.5f, 255, 210, 210, 215, false, false, 180, 0, 0, 0, true);
            _smallFont = Hud.Render.CreateFont("segoe ui", 6.5f, 220, 160, 160, 170, false, false, 150, 0, 0, 0, true);
            _accentFont = Hud.Render.CreateFont("segoe ui", 7.5f, 255, 100, 200, 255, true, false, 180, 0, 0, 0, true);
            _successFont = Hud.Render.CreateFont("segoe ui", 7.5f, 255, 100, 220, 120, true, false, 180, 0, 0, 0, true);
            _warningFont = Hud.Render.CreateFont("segoe ui", 7.5f, 255, 255, 180, 80, true, false, 180, 0, 0, 0, true);
            _monoFont = Hud.Render.CreateFont("consolas", 7, 255, 180, 180, 190, false, false, 160, 0, 0, 0, true);
        }

        private void InitializeBrushes()
        {
            // Modern dark theme
            _panelBrush = Hud.Render.CreateBrush(245, 22, 22, 30, 0);
            _panelDarkBrush = Hud.Render.CreateBrush(250, 15, 15, 20, 0);
            _headerBrush = Hud.Render.CreateBrush(240, 30, 30, 40, 0);
            _borderBrush = Hud.Render.CreateBrush(200, 50, 50, 65, 1.5f);
            _accentBorderBrush = Hud.Render.CreateBrush(255, 100, 180, 255, 2f);

            // Buttons
            _buttonBrush = Hud.Render.CreateBrush(220, 45, 55, 75, 0);
            _buttonHoverBrush = Hud.Render.CreateBrush(240, 60, 80, 110, 0);
            _buttonActiveBrush = Hud.Render.CreateBrush(255, 80, 120, 180, 0);
            _inputBrush = Hud.Render.CreateBrush(230, 18, 18, 25, 0);
            _inputFocusBrush = Hud.Render.CreateBrush(250, 25, 30, 40, 0);

            // Status colors
            _successBrush = Hud.Render.CreateBrush(255, 50, 180, 80, 0);
            _warningBrush = Hud.Render.CreateBrush(255, 220, 160, 50, 0);
            _errorBrush = Hud.Render.CreateBrush(255, 220, 70, 70, 0);
            _infoBrush = Hud.Render.CreateBrush(255, 80, 160, 220, 0);

            // Toggles
            _toggleOnBrush = Hud.Render.CreateBrush(255, 60, 180, 90, 0);
            _toggleOffBrush = Hud.Render.CreateBrush(255, 100, 50, 50, 0);
            _toggleTrackBrush = Hud.Render.CreateBrush(200, 40, 40, 50, 0);

            // Item highlights
            _protectedHighlight = Hud.Render.CreateBrush(180, 100, 220, 100, 2.5f);
            _salvageHighlight = Hud.Render.CreateBrush(180, 220, 80, 80, 2.5f);
            _ancientHighlight = Hud.Render.CreateBrush(180, 255, 180, 80, 2.5f);
            _primalHighlight = Hud.Render.CreateBrush(200, 255, 80, 80, 3f);

            // Scrollbar
            _scrollTrackBrush = Hud.Render.CreateBrush(150, 35, 35, 45, 0);
            _scrollThumbBrush = Hud.Render.CreateBrush(200, 70, 80, 100, 0);
        }

        #endregion

        #region Key Handler

        public void OnKeyEvent(IKeyEvent keyEvent)
        {
            if (!Hud.Game.IsInGame) return;

            // Ctrl+U = Quick toggle all profiles
            if (QuickToggleKey.Matches(keyEvent) && keyEvent.IsPressed)
            {
                bool anyEnabled = BlacklistMgr.Profiles.Values.Any(p => p.IsEnabled);
                foreach (var profile in BlacklistMgr.Profiles.Values)
                {
                    profile.IsEnabled = !anyEnabled;
                }
                BlacklistMgr.RebuildActiveBlacklist();
                SetStatus(anyEnabled ? "All profiles disabled" : "All profiles enabled", StatusType.Info);
                return;
            }

            // Shift+U = Toggle profile manager
            if (ManagerKey.Matches(keyEvent) && keyEvent.IsPressed)
            {
                if (IsSalvageWindowOpen() || IsRepairWindowOpen())
                {
                    _showManager = !_showManager;
                    _scrollOffset = 0;
                    _panelAlpha = 0f;
                }
                return;
            }

            // U = Start/stop salvage
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
            if (!Hud.Game.IsInGame) return;
            if (!_isRunning) return;
            if (!ValidateState())
            {
                _isRunning = false;
                _isSalvaging = false;
                return;
            }
            if (_isSalvaging) return;

            ProcessSalvage();
        }

        private bool ValidateState()
        {
            if (!Hud.Window.IsForeground) return false;
            if (!IsSalvageWindowOpen() && !IsRepairWindowOpen()) return false;
            if (!Hud.Inventory.InventoryMainUiElement.Visible) return false;
            return true;
        }

        private void ProcessSalvage()
        {
            _isSalvaging = true;
            _lastCursorX = Hud.Window.CursorX;
            _lastCursorY = Hud.Window.CursorY;

            try
            {
                // Switch to salvage tab if on repair
                if (IsRepairWindowOpen() && !IsSalvageWindowOpen())
                {
                    Hud.Interaction.ClickUiElement(MouseButtons.Left, _salvageTab);
                    Hud.Wait(100);
                    Hud.ReCollect();
                    _isSalvaging = false;
                    return;
                }

                if (!IsSalvageWindowOpen())
                {
                    _isSalvaging = false;
                    return;
                }

                var items = GetItemsToSalvage();
                _salvageTotal = items.Count;

                if (items.Count == 0)
                {
                    SetAnvil(false);
                    _isRunning = false;
                    _isSalvaging = false;
                    SetStatus("Complete! All done.", StatusType.Success);
                    Hud.Interaction.MouseMove(_lastCursorX, _lastCursorY, 1, 1);
                    return;
                }

                SetAnvil(true);
                Hud.Wait(50);

                foreach (var item in items)
                {
                    if (!_isRunning) break;
                    if (!IsSalvageWindowOpen()) break;

                    if (_salvageAttempted.Contains(item.ItemUniqueId))
                        continue;

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
                        if (_okButton.Visible)
                        {
                            Hud.Interaction.PressEnter();
                            Hud.Wait(30);
                        }
                    }

                    Hud.Wait(20);
                }

                SetAnvil(false);

                IUiElement chatLine = Hud.Render.GetUiElement("Root.NormalLayer.chatentry_dialog_backgroundScreen.chatentry_content.chat_editline");
                if (chatLine != null && chatLine.Visible)
                {
                    Hud.Interaction.PressEnter();
                }
            }
            finally
            {
                Hud.Interaction.MouseMove(_lastCursorX, _lastCursorY, 1, 1);
                _isSalvaging = false;
            }
        }

        private void SetAnvil(bool enabled)
        {
            IUiElement anvilButton = _salvageButton1?.Visible == true ? _salvageButton1 :
                                     _salvageButton2?.Visible == true ? _salvageButton2 : null;
            if (anvilButton == null) return;

            bool isEnabled = anvilButton.AnimState == 19 || anvilButton.AnimState == 20;
            if (enabled == isEnabled) return;

            Hud.Interaction.ClickUiElement(MouseButtons.Left, anvilButton);
            Hud.Wait(100);
        }

        private List<IItem> GetItemsToSalvage()
        {
            var result = new List<IItem>();
            foreach (var item in Hud.Inventory.ItemsInInventory)
            {
                if (CanSalvage(item))
                    result.Add(item);
            }
            result.Sort((a, b) =>
            {
                int cmp = a.InventoryX.CompareTo(b.InventoryX);
                return cmp != 0 ? cmp : a.InventoryY.CompareTo(b.InventoryY);
            });
            return result;
        }

        private bool CanSalvage(IItem item)
        {
            if (item == null || item.SnoItem == null) return false;
            if (item.SnoItem.Kind != ItemKind.loot && item.SnoItem.Kind != ItemKind.potion) return false;
            if (item.VendorBought || item.IsInventoryLocked) return false;
            if (item.ItemsInSocket != null && item.ItemsInSocket.Length > 0) return false;
            if (item.EnchantedAffixCounter > 0) return false;
            if (item.Quantity > 1 || item.Location != ItemLocation.Inventory) return false;
            if (IsInArmorySet(item)) return false;

            string itemName = item.SnoItem.NameLocalized ?? "";
            string fullName = item.FullNameLocalized ?? "";
            string englishName = item.SnoItem.NameEnglish ?? "";
            if (BlacklistMgr.IsBlacklisted(itemName, fullName, englishName)) return false;

            if (item.AncientRank == 2 && SalvagePrimal != 2) return false;
            if (item.AncientRank == 1 && SalvageAncient != 2) return false;

            var mainGroup = item.SnoItem.MainGroupCode ?? "";
            string[] skipGroups = { "riftkeystone", "horadriccache", "plans", "-", "pony", "gems_unique" };
            if (skipGroups.Any(g => mainGroup.Contains(g))) return false;
            if (mainGroup.Contains("cosmetic")) return false;

            string[] skipNames = { "Staff of Herding", "Hellforge Ember", "Puzzle Ring", "Bovine Bardiche", "Ramaladni's Gift" };
            if (skipNames.Contains(item.SnoItem.NameEnglish)) return false;

            if (item.SnoItem.Code?.StartsWith("P72_Soulshard") == true) return false;

            return true;
        }

        private bool IsInArmorySet(IItem item)
        {
            if (Hud.Game.Me.ArmorySets == null) return false;
            return Hud.Game.Me.ArmorySets.Any(set => set?.ContainsItem(item) == true);
        }

        private bool IsSalvageWindowOpen() => _salvageDialog?.Visible == true;
        private bool IsRepairWindowOpen() => _repairDialog?.Visible == true;

        #endregion

        #region UI Rendering

        public void PaintTopInGame(ClipState clipState)
        {
            if (clipState != ClipState.Inventory) return;
            if (!Hud.Game.IsInGame) return;
            if (!IsSalvageWindowOpen() && !IsRepairWindowOpen()) return;

            // Animate panel alpha
            float targetAlpha = _showManager ? 1f : 0f;
            _panelAlpha += (targetAlpha - _panelAlpha) * 0.15f;

            _clickAreas.Clear();

            // Draw main control panel
            DrawMainPanel();

            // Draw profile manager with animation
            if (_panelAlpha > 0.05f)
            {
                DrawProfileManager();
            }

            // Draw item highlights in inventory
            if (IsSalvageWindowOpen())
            {
                DrawItemHighlights();
            }

            // Handle mouse interactions
            HandleMouseInput();
        }

        private void DrawMainPanel()
        {
            var invRect = Hud.Inventory.InventoryMainUiElement.Rectangle;
            float panelW = 220;
            float panelH = 130;
            float panelX = invRect.X - panelW - 15;
            float panelY = invRect.Y;

            if (panelX < 10) panelX = 10;

            // Panel with rounded corners effect (via multiple rects)
            DrawPanel(panelX, panelY, panelW, panelH, _isRunning);

            float pad = 12;
            float x = panelX + pad;
            float y = panelY + pad;
            float contentW = panelW - pad * 2;

            // Title with icon
            string titleIcon = _isRunning ? "⚡" : "🔧";
            var titleLayout = _titleFont.GetTextLayout($"{titleIcon} Smart Salvage");
            _titleFont.DrawText(titleLayout, x, y);
            y += titleLayout.Metrics.Height + 8;

            // Status line with progress
            if (_isRunning)
            {
                // Progress bar
                float barH = 4;
                _toggleTrackBrush.DrawRectangle(x, y, contentW, barH);
                _successBrush.DrawRectangle(x, y, contentW * _progressValue, barH);
                y += barH + 6;

                var statusLayout = _successFont.GetTextLayout(_statusMessage ?? "Running...");
                _successFont.DrawText(statusLayout, x, y);
            }
            else if (_statusTimer.IsRunning && _statusTimer.ElapsedMilliseconds < 4000)
            {
                var font = GetStatusFont(_statusType);
                var statusLayout = font.GetTextLayout(_statusMessage ?? "");
                font.DrawText(statusLayout, x, y);
            }
            else
            {
                var infoLayout = _smallFont.GetTextLayout("Press U to start salvage");
                _smallFont.DrawText(infoLayout, x, y);
            }
            y += 20;

            // Stats row
            int salvageCount = IsSalvageWindowOpen() ? GetItemsToSalvage().Count : 0;
            int protectedCount = BlacklistMgr.GetActiveItemCount();
            int enabledProfiles = BlacklistMgr.GetEnabledProfileCount();

            // Protected items badge
            DrawBadge(x, y, $"🛡 {protectedCount}", _successBrush, _bodyFont);
            
            // Salvage items badge
            float badgeX = x + 70;
            DrawBadge(badgeX, y, $"🗑 {salvageCount}", salvageCount > 0 ? _errorBrush : _infoBrush, _bodyFont);
            
            // Profiles badge
            badgeX += 60;
            DrawBadge(badgeX, y, $"📋 {enabledProfiles}", _infoBrush, _bodyFont);
            y += 26;

            // Manager button
            float btnW = contentW;
            float btnH = 24;
            var btnRect = new RectangleF(x, y, btnW, btnH);
            _clickAreas["manager"] = btnRect;

            bool hovered = IsMouseOver(btnRect);
            DrawButton(btnRect, _showManager ? "▼ Close Profiles" : "▶ Manage Profiles [Shift+U]", hovered, false);
        }

        private void DrawProfileManager()
        {
            var invRect = Hud.Inventory.InventoryMainUiElement.Rectangle;
            float panelW = 300;
            float panelH = 480;
            float panelX = invRect.X - panelW - 15;
            float panelY = 145 + invRect.Y;

            if (panelX < 10) panelX = 10;
            if (panelY + panelH > Hud.Window.Size.Height - 20)
                panelH = Hud.Window.Size.Height - panelY - 20;

            // Animated panel
            float animatedX = panelX - (1f - _panelAlpha) * 30;
            byte alphaByte = (byte)(_panelAlpha * 255);

            // Panel background
            var panelBrushAnim = Hud.Render.CreateBrush(alphaByte, 22, 22, 30, 0);
            panelBrushAnim.DrawRectangle(animatedX, panelY, panelW, panelH);
            
            var borderBrushAnim = Hud.Render.CreateBrush((byte)(_panelAlpha * 200), 50, 50, 65, 1.5f);
            borderBrushAnim.DrawRectangle(animatedX, panelY, panelW, panelH);

            float pad = 12;
            float x = animatedX + pad;
            float y = panelY + pad;
            float contentW = panelW - pad * 2;

            // Tab bar
            float tabW = (contentW - 8) / 3;
            float tabH = 26;
            string[] tabs = { "profiles", "import", "settings" };
            string[] tabLabels = { "📋 Profiles", "🌐 Import", "⚙ Settings" };

            for (int i = 0; i < 3; i++)
            {
                var tabRect = new RectangleF(x + (tabW + 4) * i, y, tabW, tabH);
                _clickAreas[$"tab_{tabs[i]}"] = tabRect;
                
                bool isActive = _activeTab == tabs[i];
                bool isHovered = IsMouseOver(tabRect);
                
                var brush = isActive ? _buttonActiveBrush : (isHovered ? _buttonHoverBrush : _buttonBrush);
                brush.DrawRectangle(tabRect);
                
                var tabLayout = _smallFont.GetTextLayout(tabLabels[i]);
                _smallFont.DrawText(tabLayout, tabRect.X + (tabW - tabLayout.Metrics.Width) / 2, 
                                   tabRect.Y + (tabH - tabLayout.Metrics.Height) / 2);
            }
            y += tabH + 12;

            // Tab content
            switch (_activeTab)
            {
                case "profiles":
                    DrawProfilesTab(x, y, contentW, panelH - (y - panelY) - pad);
                    break;
                case "import":
                    DrawImportTab(x, y, contentW, panelH - (y - panelY) - pad);
                    break;
                case "settings":
                    DrawSettingsTab(x, y, contentW, panelH - (y - panelY) - pad);
                    break;
            }
        }

        private void DrawProfilesTab(float x, float y, float contentW, float contentH)
        {
            // Header with count
            var headerLayout = _headerFont.GetTextLayout($"Build Profiles ({BlacklistMgr.GetEnabledProfileCount()}/{BlacklistMgr.Profiles.Count})");
            _headerFont.DrawText(headerLayout, x, y);
            y += headerLayout.Metrics.Height + 8;

            // Quick actions
            float btnW = (contentW - 8) / 2;
            float btnH = 22;

            var enableAllRect = new RectangleF(x, y, btnW, btnH);
            _clickAreas["enable_all"] = enableAllRect;
            DrawButton(enableAllRect, "Enable All", IsMouseOver(enableAllRect), false);

            var disableAllRect = new RectangleF(x + btnW + 8, y, btnW, btnH);
            _clickAreas["disable_all"] = disableAllRect;
            DrawButton(disableAllRect, "Disable All", IsMouseOver(disableAllRect), false);
            y += btnH + 12;

            // Profile list
            var profiles = BlacklistMgr.Profiles.Values.OrderByDescending(p => p.IsEnabled).ThenBy(p => p.DisplayName).ToList();
            float rowH = 36;
            int visibleCount = Math.Min(_maxVisibleProfiles, profiles.Count - _scrollOffset);
            float listH = contentH - (y - (y - rowH * 2));

            // Scrollable area
            for (int i = 0; i < visibleCount && i + _scrollOffset < profiles.Count; i++)
            {
                var profile = profiles[i + _scrollOffset];
                var rowRect = new RectangleF(x, y, contentW - 12, rowH);
                _clickAreas[$"profile_{profile.Id}"] = rowRect;

                bool isHovered = IsMouseOver(rowRect);
                
                // Row background
                var rowBrush = isHovered ? _buttonHoverBrush : _panelDarkBrush;
                rowBrush.DrawRectangle(rowRect);

                // Toggle switch
                float toggleW = 36;
                float toggleH = 18;
                float toggleX = x + 8;
                float toggleY = y + (rowH - toggleH) / 2;
                
                _toggleTrackBrush.DrawRectangle(toggleX, toggleY, toggleW, toggleH);
                var toggleBrush = profile.IsEnabled ? _toggleOnBrush : _toggleOffBrush;
                float knobX = profile.IsEnabled ? toggleX + toggleW - toggleH + 2 : toggleX + 2;
                toggleBrush.DrawRectangle(knobX, toggleY + 2, toggleH - 4, toggleH - 4);

                // Profile info
                float textX = toggleX + toggleW + 10;
                
                // Icon + Name
                string icon = profile.Icon ?? "📋";
                string displayName = profile.DisplayName;
                if (displayName.Length > 25) displayName = displayName.Substring(0, 22) + "...";
                
                var nameFont = profile.IsEnabled ? _bodyFont : _smallFont;
                var nameLayout = nameFont.GetTextLayout($"{icon} {displayName}");
                nameFont.DrawText(nameLayout, textX, y + 4);

                // Item count
                string countText = $"{profile.Items.Count} items";
                var countLayout = _smallFont.GetTextLayout(countText);
                _smallFont.DrawText(countLayout, textX, y + 4 + nameLayout.Metrics.Height);

                // Source indicator
                if (!string.IsNullOrEmpty(profile.SourceUrl))
                {
                    var srcLayout = _smallFont.GetTextLayout("🌐");
                    _smallFont.DrawText(srcLayout, rowRect.Right - 24, y + (rowH - srcLayout.Metrics.Height) / 2);
                }

                y += rowH + 2;
            }

            // Scrollbar if needed
            if (profiles.Count > _maxVisibleProfiles)
            {
                float scrollX = x + contentW - 8;
                float scrollH = rowH * visibleCount;
                float scrollY = y - scrollH - 2;
                
                _scrollTrackBrush.DrawRectangle(scrollX, scrollY, 6, scrollH);
                
                float thumbH = scrollH * ((float)visibleCount / profiles.Count);
                float thumbY = scrollY + (scrollH - thumbH) * ((float)_scrollOffset / (profiles.Count - visibleCount));
                _scrollThumbBrush.DrawRectangle(scrollX, thumbY, 6, thumbH);

                // Scroll areas
                _clickAreas["scroll_up"] = new RectangleF(scrollX - 20, scrollY, 30, scrollH / 2);
                _clickAreas["scroll_down"] = new RectangleF(scrollX - 20, scrollY + scrollH / 2, 30, scrollH / 2);
            }
        }

        private void DrawImportTab(float x, float y, float contentW, float contentH)
        {
            // Header
            var headerLayout = _headerFont.GetTextLayout("Import from Build Guides");
            _headerFont.DrawText(headerLayout, x, y);
            y += headerLayout.Metrics.Height + 6;

            // Supported sites
            var sitesLayout = _smallFont.GetTextLayout("Supported: Maxroll.gg, Icy-Veins.com");
            _smallFont.DrawText(sitesLayout, x, y);
            y += sitesLayout.Metrics.Height + 12;

            // URL Input
            var labelLayout = _bodyFont.GetTextLayout("Build Guide URL:");
            _bodyFont.DrawText(labelLayout, x, y);
            y += labelLayout.Metrics.Height + 4;

            float inputH = 28;
            var inputRect = new RectangleF(x, y, contentW, inputH);
            _clickAreas["import_input"] = inputRect;

            bool inputHovered = IsMouseOver(inputRect);
            var inputBrush = inputHovered ? _inputFocusBrush : _inputBrush;
            inputBrush.DrawRectangle(inputRect);
            _borderBrush.DrawRectangle(inputRect);

            // URL text
            string displayUrl = string.IsNullOrEmpty(_importUrl) ? "Click to paste URL from clipboard..." : _importUrl;
            if (displayUrl.Length > 40) displayUrl = displayUrl.Substring(0, 37) + "...";
            
            var urlFont = string.IsNullOrEmpty(_importUrl) ? _smallFont : _monoFont;
            var urlLayout = urlFont.GetTextLayout(displayUrl);
            urlFont.DrawText(urlLayout, x + 8, y + (inputH - urlLayout.Metrics.Height) / 2);
            y += inputH + 12;

            // Import button
            float btnH = 32;
            var importRect = new RectangleF(x, y, contentW, btnH);
            _clickAreas["import_btn"] = importRect;

            bool importHovered = IsMouseOver(importRect);
            string importText = _isImporting ? "Importing..." : "🌐 Import Build";
            DrawButton(importRect, importText, importHovered, _isImporting);
            y += btnH + 12;

            // Status message
            if (!string.IsNullOrEmpty(_importStatus))
            {
                var statusFont = GetStatusFont(_importStatusType);
                var statusLayout = statusFont.GetTextLayout(_importStatus);
                statusFont.DrawText(statusLayout, x, y);
                y += statusLayout.Metrics.Height + 12;
            }

            // Example URLs
            y += 8;
            var exampleHeader = _smallFont.GetTextLayout("Example URLs:");
            _smallFont.DrawText(exampleHeader, x, y);
            y += exampleHeader.Metrics.Height + 4;

            string[] examples = {
                "maxroll.gg/d3/guides/god-ha-demon-hunter-guide",
                "icy-veins.com/d3/god-hungering-arrow-demon-hunter"
            };

            foreach (var example in examples)
            {
                var exLayout = _monoFont.GetTextLayout("• " + example);
                _monoFont.DrawText(exLayout, x, y);
                y += exLayout.Metrics.Height + 2;
            }

            // Action buttons at bottom
            y = contentH - 30;
            float actionBtnW = (contentW - 8) / 2;
            float actionBtnH = 24;

            var saveRect = new RectangleF(x, y, actionBtnW, actionBtnH);
            _clickAreas["save"] = saveRect;
            DrawButton(saveRect, "💾 Save", IsMouseOver(saveRect), false);

            var exportRect = new RectangleF(x + actionBtnW + 8, y, actionBtnW, actionBtnH);
            _clickAreas["export"] = exportRect;
            DrawButton(exportRect, "📤 Export All", IsMouseOver(exportRect), false);
        }

        private void DrawSettingsTab(float x, float y, float contentW, float contentH)
        {
            // Header
            var headerLayout = _headerFont.GetTextLayout("Salvage Settings");
            _headerFont.DrawText(headerLayout, x, y);
            y += headerLayout.Metrics.Height + 16;

            // Ancient items setting
            DrawSettingRow(x, y, contentW, "Ancient Items:", 
                           SalvageAncient == 1 ? "Never salvage" : (SalvageAncient == 2 ? "Always salvage" : "Smart"),
                           "setting_ancient");
            y += 40;

            // Primal items setting
            DrawSettingRow(x, y, contentW, "Primal Items:", 
                           SalvagePrimal == 1 ? "Never salvage" : (SalvagePrimal == 2 ? "Always salvage" : "Smart"),
                           "setting_primal");
            y += 40;

            // Auto repair
            DrawSettingRow(x, y, contentW, "Auto Repair:", 
                           AutoRepair ? "Enabled" : "Disabled",
                           "setting_repair");
            y += 50;

            // Info section
            var infoHeader = _smallFont.GetTextLayout("Always Protected:");
            _smallFont.DrawText(infoHeader, x, y);
            y += infoHeader.Metrics.Height + 4;

            string[] protected_items = {
                "• Items in Armory sets",
                "• Enchanted items",
                "• Socketed items",
                "• Locked inventory slots",
                "• Puzzle Ring, Bovine Bardiche",
                "• Ramaladni's Gift"
            };

            foreach (var item in protected_items)
            {
                var itemLayout = _smallFont.GetTextLayout(item);
                _smallFont.DrawText(itemLayout, x, y);
                y += itemLayout.Metrics.Height + 1;
            }
        }

        private void DrawSettingRow(float x, float y, float w, string label, string value, string clickId)
        {
            var labelLayout = _bodyFont.GetTextLayout(label);
            _bodyFont.DrawText(labelLayout, x, y);

            float btnW = 120;
            float btnH = 24;
            var btnRect = new RectangleF(x + w - btnW, y - 2, btnW, btnH);
            _clickAreas[clickId] = btnRect;

            DrawButton(btnRect, value, IsMouseOver(btnRect), false);
        }

        private void DrawItemHighlights()
        {
            var itemsToSalvage = GetItemsToSalvage();
            var salvageIds = new HashSet<string>(itemsToSalvage.Select(i => i.ItemUniqueId));

            foreach (var item in Hud.Inventory.ItemsInInventory)
            {
                var rect = Hud.Inventory.GetItemRect(item);
                if (rect == RectangleF.Empty) continue;

                if (salvageIds.Contains(item.ItemUniqueId))
                {
                    // Item will be salvaged - red
                    if (item.AncientRank == 2)
                        _primalHighlight.DrawRectangle(rect);
                    else if (item.AncientRank == 1)
                        _ancientHighlight.DrawRectangle(rect);
                    else
                        _salvageHighlight.DrawRectangle(rect);
                }
                else if (IsItemBlacklisted(item))
                {
                    // Item is protected - green
                    _protectedHighlight.DrawRectangle(rect);
                }
            }
        }

        private bool IsItemBlacklisted(IItem item)
        {
            string itemName = item.SnoItem.NameLocalized ?? "";
            string fullName = item.FullNameLocalized ?? "";
            string englishName = item.SnoItem.NameEnglish ?? "";
            return BlacklistMgr.IsBlacklisted(itemName, fullName, englishName);
        }

        #endregion

        #region UI Helpers

        private void DrawPanel(float x, float y, float w, float h, bool active)
        {
            _panelBrush.DrawRectangle(x, y, w, h);
            
            var border = active ? _accentBorderBrush : _borderBrush;
            border.DrawRectangle(x, y, w, h);

            // Accent bar on left
            var accent = active ? _successBrush : _infoBrush;
            accent.DrawRectangle(x, y, 3, h);
        }

        private void DrawButton(RectangleF rect, string text, bool hovered, bool disabled)
        {
            var brush = disabled ? _toggleTrackBrush : (hovered ? _buttonHoverBrush : _buttonBrush);
            brush.DrawRectangle(rect);

            var layout = _bodyFont.GetTextLayout(text);
            _bodyFont.DrawText(layout, rect.X + (rect.Width - layout.Metrics.Width) / 2,
                              rect.Y + (rect.Height - layout.Metrics.Height) / 2);
        }

        private void DrawBadge(float x, float y, string text, IBrush bgBrush, IFont font)
        {
            var layout = font.GetTextLayout(text);
            float badgeW = layout.Metrics.Width + 12;
            float badgeH = layout.Metrics.Height + 6;

            bgBrush.DrawRectangle(x, y, badgeW, badgeH);
            font.DrawText(layout, x + 6, y + 3);
        }

        private IFont GetStatusFont(StatusType type)
        {
            switch (type)
            {
                case StatusType.Success: return _successFont;
                case StatusType.Warning: return _warningFont;
                case StatusType.Error: return _warningFont;
                default: return _accentFont;
            }
        }

        private void SetStatus(string message, StatusType type)
        {
            _statusMessage = message;
            _statusType = type;
            _statusTimer.Restart();
        }

        private bool IsMouseOver(RectangleF rect)
        {
            int mx = Hud.Window.CursorX;
            int my = Hud.Window.CursorY;
            return mx >= rect.X && mx <= rect.X + rect.Width &&
                   my >= rect.Y && my <= rect.Y + rect.Height;
        }

        #endregion

        #region Mouse Input

        private void HandleMouseInput()
        {
            if (_isRunning) return;

            bool isMouseDown = Hud.Input.IsKeyDown(Keys.LButton);
            bool clicked = !isMouseDown && _wasMouseDown && _clickTimer.ElapsedMilliseconds > 150;
            _wasMouseDown = isMouseDown;

            // Handle scroll wheel
            // TODO: Add scroll wheel support if available in TurboHUD

            if (!clicked) return;
            _clickTimer.Restart();

            // Check click areas
            foreach (var kvp in _clickAreas)
            {
                if (!IsMouseOver(kvp.Value)) continue;

                HandleClick(kvp.Key);
                return;
            }
        }

        private void HandleClick(string areaId)
        {
            // Manager toggle
            if (areaId == "manager")
            {
                _showManager = !_showManager;
                _scrollOffset = 0;
                return;
            }

            // Tabs
            if (areaId.StartsWith("tab_"))
            {
                _activeTab = areaId.Substring(4);
                return;
            }

            // Profile toggle
            if (areaId.StartsWith("profile_"))
            {
                string profileId = areaId.Substring(8);
                BlacklistMgr.ToggleProfile(profileId);
                return;
            }

            // Enable/Disable all
            if (areaId == "enable_all")
            {
                foreach (var profile in BlacklistMgr.Profiles.Values)
                    profile.IsEnabled = true;
                BlacklistMgr.RebuildActiveBlacklist();
                SetStatus("All profiles enabled", StatusType.Success);
                return;
            }

            if (areaId == "disable_all")
            {
                foreach (var profile in BlacklistMgr.Profiles.Values)
                    profile.IsEnabled = false;
                BlacklistMgr.RebuildActiveBlacklist();
                SetStatus("All profiles disabled", StatusType.Warning);
                return;
            }

            // Scroll
            if (areaId == "scroll_up")
            {
                _scrollOffset = Math.max(0, _scrollOffset - 1);
                return;
            }

            if (areaId == "scroll_down")
            {
                int maxOffset = Math.max(0, BlacklistMgr.Profiles.Count - _maxVisibleProfiles);
                _scrollOffset = Math.min(maxOffset, _scrollOffset + 1);
                return;
            }

            // Import URL paste
            if (areaId == "import_input")
            {
                try
                {
                    if (Clipboard.ContainsText())
                    {
                        _importUrl = Clipboard.GetText().Trim();
                        _importStatus = "URL pasted";
                        _importStatusType = StatusType.Info;
                    }
                }
                catch { _importStatus = "Clipboard error"; _importStatusType = StatusType.Error; }
                return;
            }

            // Import button
            if (areaId == "import_btn" && !_isImporting)
            {
                StartImport();
                return;
            }

            // Save
            if (areaId == "save")
            {
                BlacklistMgr.SaveToFile();
                SetStatus("Saved!", StatusType.Success);
                return;
            }

            // Export
            if (areaId == "export")
            {
                try
                {
                    var data = BlacklistMgr.ExportAllProfiles();
                    Clipboard.SetText(data);
                    SetStatus("Exported to clipboard!", StatusType.Success);
                }
                catch { SetStatus("Export failed", StatusType.Error); }
                return;
            }

            // Settings toggles
            if (areaId == "setting_ancient")
            {
                SalvageAncient = (SalvageAncient + 1) % 3;
                return;
            }

            if (areaId == "setting_primal")
            {
                SalvagePrimal = (SalvagePrimal + 1) % 3;
                return;
            }

            if (areaId == "setting_repair")
            {
                AutoRepair = !AutoRepair;
                return;
            }
        }

        private void StartImport()
        {
            if (string.IsNullOrWhiteSpace(_importUrl))
            {
                _importStatus = "Please paste a URL first";
                _importStatusType = StatusType.Warning;
                return;
            }

            // Check for both Maxroll and Icy Veins
            bool isMaxroll = _importUrl.Contains("maxroll.gg");
            bool isIcyVeins = _importUrl.Contains("icy-veins.com");

            if (!isMaxroll && !isIcyVeins)
            {
                _importStatus = "Unsupported URL (use Maxroll or Icy Veins)";
                _importStatusType = StatusType.Error;
                return;
            }

            _isImporting = true;
            _importStatus = "Fetching...";
            _importStatusType = StatusType.Info;

            try
            {
                MaxrollBuildData buildData;
                
                if (isMaxroll)
                {
                    buildData = Crawler.CrawlGuide(_importUrl);
                }
                else
                {
                    // Use Maxroll crawler for Icy Veins too (similar HTML structure)
                    buildData = Crawler.CrawlGuide(_importUrl);
                }

                if (buildData != null && buildData.Items.Count > 0)
                {
                    var profile = BlacklistMgr.ImportFromMaxrollData(buildData);
                    if (profile != null)
                    {
                        _importStatus = $"✓ Imported '{profile.DisplayName}' ({profile.Items.Count} items)";
                        _importStatusType = StatusType.Success;
                        _importUrl = "";
                        BlacklistMgr.SaveToFile();
                    }
                    else
                    {
                        _importStatus = "Failed to create profile";
                        _importStatusType = StatusType.Error;
                    }
                }
                else
                {
                    _importStatus = Crawler.LastError ?? "No items found";
                    _importStatusType = StatusType.Error;
                }
            }
            catch (Exception ex)
            {
                _importStatus = "Error: " + ex.Message;
                _importStatusType = StatusType.Error;
            }
            finally
            {
                _isImporting = false;
            }
        }

        #endregion

        #region Public API

        public void AddToBlacklist(params string[] itemNames)
        {
            BlacklistMgr.AddCustomItems(itemNames);
        }

        public void RemoveFromBlacklist(params string[] itemNames)
        {
            BlacklistMgr.RemoveCustomItems(itemNames);
        }

        public void SetBuildEnabled(string profileId, bool enabled)
        {
            BlacklistMgr.SetProfileEnabled(profileId, enabled);
        }

        #endregion
    }

    public enum StatusType
    {
        Info,
        Success,
        Warning,
        Error
    }
}
