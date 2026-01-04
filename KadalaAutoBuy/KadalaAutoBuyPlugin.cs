namespace Turbo.Plugins.Custom.KadalaAutoBuy
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
    /// Kadala Auto-Buy Plugin v4.3 - Fixed mouse lock and tab switching
    /// </summary>
    public class KadalaAutoBuyPlugin : CustomPluginBase, IInGameTopPainter, IKeyEventHandler, IAfterCollectHandler
    {
        #region Plugin Metadata

        public override string PluginId => "kadala-auto-buy";
        public override string PluginName => "Kadala Auto-Buy";
        public override string PluginDescription => "Auto-buy items from Kadala";
        public override string PluginVersion => "4.3.0";
        public override string PluginCategory => "automation";
        public override string PluginIcon => "🎰";
        public override bool HasSettings => true;

        #endregion

        #region Kadala Item Types

        public enum KadalaItemType
        {
            // Weapons Tab (index 0)
            OneHandWeapon,
            TwoHandWeapon,
            Quiver,
            Orb,
            Mojo,
            Phylactery,
            
            // Armor Tab (index 1)
            Helm,
            Gloves,
            Boots,
            ChestArmor,
            Belt,
            Shoulders,
            Pants,
            Bracers,
            
            // Jewelry Tab (index 2)
            Ring,
            Amulet,
            
            // Other Tab (index 3)
            Shield
        }

        private static readonly Dictionary<KadalaItemType, (int TabIndex, int SlotRow, int SlotCol, int Cost, string Name)> ItemSlotMap = new Dictionary<KadalaItemType, (int, int, int, int, string)>
        {
            // Weapons Tab (0)
            { KadalaItemType.OneHandWeapon, (0, 0, 0, 75, "1-H Weapon") },
            { KadalaItemType.TwoHandWeapon, (0, 0, 1, 75, "2-H Weapon") },
            { KadalaItemType.Quiver,        (0, 1, 0, 25, "Quiver") },
            { KadalaItemType.Orb,           (0, 1, 1, 25, "Orb") },
            { KadalaItemType.Mojo,          (0, 2, 0, 25, "Mojo") },
            { KadalaItemType.Phylactery,    (0, 2, 1, 25, "Phylactery") },
            
            // Armor Tab (1)
            { KadalaItemType.Helm,       (1, 0, 0, 25, "Helm") },
            { KadalaItemType.Gloves,     (1, 0, 1, 25, "Gloves") },
            { KadalaItemType.Boots,      (1, 1, 0, 25, "Boots") },
            { KadalaItemType.ChestArmor, (1, 1, 1, 25, "Chest") },
            { KadalaItemType.Belt,       (1, 2, 0, 25, "Belt") },
            { KadalaItemType.Shoulders,  (1, 2, 1, 25, "Shoulders") },
            { KadalaItemType.Pants,      (1, 3, 0, 25, "Pants") },
            { KadalaItemType.Bracers,    (1, 3, 1, 25, "Bracers") },
            
            // Jewelry Tab (2)
            { KadalaItemType.Ring,   (2, 0, 0, 50, "Ring") },
            { KadalaItemType.Amulet, (2, 0, 1, 100, "Amulet") },
            
            // Other Tab (3)
            { KadalaItemType.Shield, (3, 0, 0, 25, "Shield") }
        };

        #endregion

        #region Runtime State

        public override bool IsActive => _running;
        public override string StatusText => !_enabled ? "OFF" : (_running ? "Buying..." : "Ready");

        private enum BuyState
        {
            Idle,
            WaitingForShop,
            SwitchingTab,
            WaitingForTabSwitch,
            MovingToSlot,
            WaitingForTooltip,
            Buying
        }
        
        private BuyState _state = BuyState.Idle;

        #endregion

        #region Public Settings

        public IKeyEvent ToggleKey { get; set; }
        public IKeyEvent CycleItemKey { get; set; }

        public bool AutoBuyEnabled { get => _enabled; set => _enabled = value; }
        public int MinBloodShardsToStart { get; set; } = 100;
        public int StopAtBloodShards { get; set; } = 0;
        public int BuyIntervalMs { get; set; } = 50;
        public int InitialDelayMs { get; set; } = 500;
        
        public KadalaItemType SelectedItem { get; set; } = KadalaItemType.Ring;
        
        public int TotalItemsBought { get; set; } = 0;
        public int SessionItemsBought { get; set; } = 0;
        
        public bool ShowDebugOverlay { get; set; } = false;

        #endregion

        #region Private Fields

        private bool _enabled = true;
        private bool _running = false;
        private bool _wasKadalaOpen = false;
        
        private IUiElement _shopMainPage;
        private IUiElement _shopPanel;
        private IUiElement _buyTooltip;
        
        private IWatch _buyTimer;
        private IWatch _openTimer;
        private IWatch _stateTimer;
        private IWatch _safetyTimer;  // Safety timer to prevent runaway
        
        private int _currentTargetTab = -1;
        private int _tabSwitchAttempts = 0;
        private const int MAX_TAB_SWITCH_ATTEMPTS = 5;
        
        private IFont _headerFont;
        private IFont _infoFont;
        private IFont _statusFont;
        private IFont _errorFont;
        private IBrush _highlightBrush;
        private IBrush _debugBrush;
        private IBrush _debugBrush2;
        
        // Settings UI brushes
        private IBrush _itemBtnDefault;
        private IBrush _itemBtnHover;
        private IBrush _itemBtnSelected;
        private IBrush _itemBtnSelectedBorder;

        #endregion

        #region Initialization

        public KadalaAutoBuyPlugin()
        {
            Enabled = true;
            Order = 50020;
        }

        public override void Load(IController hud)
        {
            base.Load(hud);

            ToggleKey = Hud.Input.CreateKeyEvent(true, Key.G, false, false, true);    // Shift+G = Toggle Kadala Auto-Buy
            CycleItemKey = Hud.Input.CreateKeyEvent(true, Key.G, true, false, false); // Ctrl+G = Cycle Item Type

            _buyTimer = Hud.Time.CreateAndStartWatch();
            _openTimer = Hud.Time.CreateWatch();
            _stateTimer = Hud.Time.CreateAndStartWatch();
            _safetyTimer = Hud.Time.CreateAndStartWatch();

            _shopMainPage = Hud.Render.RegisterUiElement("Root.NormalLayer.shop_dialog_mainPage", null, null);
            _shopPanel = Hud.Render.RegisterUiElement("Root.NormalLayer.shop_dialog_mainPage.panel", _shopMainPage, null);
            _buyTooltip = Hud.Render.RegisterUiElement("Root.TopLayer.item 2.stack.frame_instruction", null, null);

            _headerFont = Hud.Render.CreateFont("tahoma", 10, 255, 255, 220, 100, true, false, 255, 0, 0, 0, true);
            _infoFont = Hud.Render.CreateFont("tahoma", 8, 255, 200, 200, 200, true, false, 200, 0, 0, 0, true);
            _statusFont = Hud.Render.CreateFont("tahoma", 8, 255, 100, 255, 100, true, false, 200, 0, 0, 0, true);
            _errorFont = Hud.Render.CreateFont("tahoma", 8, 255, 255, 100, 100, true, false, 200, 0, 0, 0, true);
            
            _highlightBrush = Hud.Render.CreateBrush(180, 100, 255, 100, 2);
            _debugBrush = Hud.Render.CreateBrush(150, 255, 0, 0, 2);
            _debugBrush2 = Hud.Render.CreateBrush(150, 0, 255, 0, 2);
            
            // Settings UI brushes - better contrast
            _itemBtnDefault = Hud.Render.CreateBrush(200, 45, 50, 65, 0);
            _itemBtnHover = Hud.Render.CreateBrush(220, 60, 70, 90, 0);
            _itemBtnSelected = Hud.Render.CreateBrush(255, 50, 120, 80, 0);  // Green tint for selected
            _itemBtnSelectedBorder = Hud.Render.CreateBrush(255, 100, 255, 120, 2);

            Log("Kadala Auto-Buy v4.3 loaded");
        }

        public void SetEnabled(bool enabled) => _enabled = enabled;

        #endregion

        #region Position Calculations

        private RectangleF GetTabRect(int tabIndex)
        {
            if (_shopPanel?.Visible != true) return RectangleF.Empty;
            
            var panelRect = _shopPanel.Rectangle;
            float tabAreaHeight = panelRect.Height * 0.085f;
            float tabWidth = panelRect.Width / 4f;
            float tabX = panelRect.X + (tabIndex * tabWidth);
            float tabY = panelRect.Y;
            
            return new RectangleF(tabX, tabY, tabWidth, tabAreaHeight);
        }

        private RectangleF GetSlotRect(int row, int col)
        {
            if (_shopPanel?.Visible != true) return RectangleF.Empty;
            
            var panelRect = _shopPanel.Rectangle;
            
            float tabsHeight = panelRect.Height * 0.085f;
            float titleHeight = panelRect.Height * 0.055f;
            float footerHeight = panelRect.Height * 0.08f;
            
            float itemsTop = panelRect.Y + tabsHeight + titleHeight;
            float itemsHeight = panelRect.Height - tabsHeight - titleHeight - footerHeight;
            float colWidth = panelRect.Width / 2f;
            float rowHeight = itemsHeight / 4f;
            
            float slotX = panelRect.X + (col * colWidth);
            float slotY = itemsTop + (row * rowHeight);
            
            float padX = colWidth * 0.03f;
            float padY = rowHeight * 0.05f;
            
            return new RectangleF(slotX + padX, slotY + padY, colWidth - (2 * padX), rowHeight - (2 * padY));
        }

        private void ClickTab(int tabIndex)
        {
            var tabRect = GetTabRect(tabIndex);
            if (tabRect.IsEmpty) return;
            
            int clickX = (int)(tabRect.X + tabRect.Width / 2);
            int clickY = (int)(tabRect.Y + tabRect.Height / 2);
            
            Log($"Clicking tab {tabIndex} at ({clickX}, {clickY})");
            
            Hud.Interaction.MouseMove(clickX, clickY, 1, 1);
            Hud.Wait(50);
            Hud.Interaction.MouseDown(MouseButtons.Left);
            Hud.Wait(10);
            Hud.Interaction.MouseUp(MouseButtons.Left);
        }

        private void MoveToSlot(int row, int col)
        {
            var slotRect = GetSlotRect(row, col);
            if (slotRect.IsEmpty) return;
            
            int clickX = (int)(slotRect.X + slotRect.Width / 2);
            int clickY = (int)(slotRect.Y + slotRect.Height / 2);
            
            Hud.Interaction.MouseMove(clickX, clickY, 1, 1);
        }

        #endregion

        #region Main Logic

        public void AfterCollect()
        {
            if (!Hud.Game.IsInGame) 
            {
                // SAFETY: Not in game, stop everything
                if (_running) FullStop("Not in game");
                return;
            }
            
            if (!Enabled) 
            {
                if (_running) FullStop("Plugin disabled");
                return;
            }
            
            if (!_enabled) 
            {
                if (_running) FullStop("Auto-buy disabled");
                return;
            }

            bool kadalaOpen = _shopMainPage?.Visible == true && _shopPanel?.Visible == true;
            
            // SAFETY: If Kadala closed, immediately stop
            if (!kadalaOpen)
            {
                if (_running || _wasKadalaOpen)
                {
                    FullStop("Kadala closed");
                }
                _wasKadalaOpen = false;
                return;
            }
            
            // Kadala just opened
            if (kadalaOpen && !_wasKadalaOpen)
            {
                OnKadalaOpened();
            }
            
            _wasKadalaOpen = kadalaOpen;

            // SAFETY: Maximum run time of 30 seconds
            if (_running && _safetyTimer.ElapsedMilliseconds > 30000)
            {
                FullStop("Safety timeout");
                return;
            }

            if (_running && kadalaOpen)
            {
                ProcessStateMachine();
            }
        }

        private void OnKadalaOpened()
        {
            Log("Kadala opened");
            _openTimer.Restart();
            _safetyTimer.Restart();
            SessionItemsBought = 0;
            _currentTargetTab = -1;  // Force tab check
            _tabSwitchAttempts = 0;
            _state = BuyState.WaitingForShop;
            _stateTimer.Restart();
            
            if (CanStartBuying())
            {
                _running = true;
                var info = ItemSlotMap[SelectedItem];
                SetCoreStatus($"Auto-buying {info.Name}...", StatusType.Success);
                Log($"Starting auto-buy: {info.Name} (Tab {info.TabIndex}, Row {info.SlotRow}, Col {info.SlotCol})");
            }
            else
            {
                Log("Cannot start buying - not enough shards or space");
            }
        }

        /// <summary>
        /// Complete stop - releases all mouse buttons and resets state
        /// </summary>
        private void FullStop(string reason)
        {
            Log($"FullStop: {reason}");
            _running = false;
            _state = BuyState.Idle;
            _currentTargetTab = -1;
            _tabSwitchAttempts = 0;
            
            // IMPORTANT: Release all mouse buttons
            try
            {
                Hud.Interaction.MouseUp(MouseButtons.Left);
                Hud.Interaction.MouseUp(MouseButtons.Right);
            }
            catch { }
            
            if (SessionItemsBought > 0)
            {
                SetCoreStatus($"Bought {SessionItemsBought} items", StatusType.Success);
            }
            else if (!string.IsNullOrEmpty(reason))
            {
                SetCoreStatus(reason, StatusType.Warning);
            }
        }

        private void StopBuying(string reason)
        {
            FullStop(reason);
        }

        private bool CanStartBuying()
        {
            if (Hud.Game.Me == null) return false;
            
            int shards = (int)Hud.Game.Me.Materials.BloodShard;
            if (shards < MinBloodShardsToStart) return false;

            int freeSpace = Hud.Game.Me.InventorySpaceTotal - Hud.Game.InventorySpaceUsed;
            return freeSpace >= 2;
        }

        private void ProcessStateMachine()
        {
            // Safety check
            if (!_running || _shopPanel?.Visible != true)
            {
                FullStop("Shop not visible");
                return;
            }
            
            var itemInfo = ItemSlotMap[SelectedItem];
            int targetTab = itemInfo.TabIndex;
            int targetRow = itemInfo.SlotRow;
            int targetCol = itemInfo.SlotCol;
            int itemCost = itemInfo.Cost;

            int shards = (int)Hud.Game.Me.Materials.BloodShard;
            int freeSpace = Hud.Game.Me.InventorySpaceTotal - Hud.Game.InventorySpaceUsed;

            // Check stop conditions
            if (freeSpace < 2) { FullStop("Inventory full"); return; }
            if (shards < itemCost) { FullStop("Out of shards"); return; }
            if (shards <= StopAtBloodShards) { FullStop("Reached limit"); return; }

            switch (_state)
            {
                case BuyState.WaitingForShop:
                    // Wait for initial delay after shop opens
                    if (_openTimer.ElapsedMilliseconds >= InitialDelayMs)
                    {
                        // Always check if we need to switch tabs
                        _state = BuyState.SwitchingTab;
                        _stateTimer.Restart();
                        Log($"Initial delay done, switching to tab {targetTab}");
                    }
                    break;

                case BuyState.SwitchingTab:
                    // Check if we've tried too many times
                    if (_tabSwitchAttempts >= MAX_TAB_SWITCH_ATTEMPTS)
                    {
                        FullStop("Tab switch failed");
                        return;
                    }
                    
                    // Click the target tab
                    ClickTab(targetTab);
                    _currentTargetTab = targetTab;
                    _tabSwitchAttempts++;
                    _state = BuyState.WaitingForTabSwitch;
                    _stateTimer.Restart();
                    Log($"Clicked tab {targetTab} (attempt {_tabSwitchAttempts})");
                    break;

                case BuyState.WaitingForTabSwitch:
                    // Wait longer for tab to actually switch (300ms)
                    if (_stateTimer.ElapsedMilliseconds >= 300)
                    {
                        _state = BuyState.MovingToSlot;
                        _stateTimer.Restart();
                        Log($"Tab switch wait done, moving to slot ({targetRow}, {targetCol})");
                    }
                    break;

                case BuyState.MovingToSlot:
                    // Move mouse to the item slot
                    MoveToSlot(targetRow, targetCol);
                    _state = BuyState.WaitingForTooltip;
                    _stateTimer.Restart();
                    break;

                case BuyState.WaitingForTooltip:
                    // Wait for tooltip to appear (means we're hovering correctly)
                    if (_buyTooltip?.Visible == true)
                    {
                        _state = BuyState.Buying;
                        _buyTimer.Restart();
                        Log("Tooltip visible, starting to buy");
                    }
                    else if (_stateTimer.ElapsedMilliseconds > 1000)
                    {
                        // Tooltip didn't appear after 1 second - try moving again
                        // But first, maybe we need to switch tabs again
                        _state = BuyState.SwitchingTab;
                        _stateTimer.Restart();
                        Log("Tooltip timeout, retrying tab switch");
                    }
                    break;

                case BuyState.Buying:
                    // Safety: If tooltip disappeared, go back to moving
                    if (_buyTooltip?.Visible != true)
                    {
                        _state = BuyState.MovingToSlot;
                        _stateTimer.Restart();
                        Log("Lost tooltip, re-acquiring");
                        break;
                    }
                    
                    // Buy at interval
                    if (_buyTimer.TimerTest(BuyIntervalMs))
                    {
                        // Right-click to buy
                        Hud.Interaction.MouseDown(MouseButtons.Right);
                        Hud.Wait(5);
                        Hud.Interaction.MouseUp(MouseButtons.Right);
                        
                        SessionItemsBought++;
                        TotalItemsBought++;
                        
                        // Reset tab switch counter on successful buy
                        _tabSwitchAttempts = 0;
                    }
                    break;
            }
        }

        #endregion

        #region Key Handler

        public void OnKeyEvent(IKeyEvent keyEvent)
        {
            if (!Hud.Game.IsInGame || !Enabled) return;

            if (ToggleKey.Matches(keyEvent) && keyEvent.IsPressed)
            {
                _enabled = !_enabled;
                if (!_enabled)
                {
                    FullStop("Disabled by hotkey");
                }
                SetCoreStatus($"Kadala {(_enabled ? "ON" : "OFF")}", _enabled ? StatusType.Success : StatusType.Warning);
                SavePluginSettings();
            }

            if (CycleItemKey.Matches(keyEvent) && keyEvent.IsPressed)
            {
                var itemTypes = Enum.GetValues(typeof(KadalaItemType)).Cast<KadalaItemType>().ToArray();
                int nextIndex = (Array.IndexOf(itemTypes, SelectedItem) + 1) % itemTypes.Length;
                SelectedItem = itemTypes[nextIndex];
                _currentTargetTab = -1;  // Force tab recheck
                
                var info = ItemSlotMap[SelectedItem];
                SetCoreStatus($"{info.Name} ({info.Cost})", StatusType.Info);
                SavePluginSettings();
            }
        }

        #endregion

        #region Rendering

        public override void PaintTopInGame(ClipState clipState)
        {
            base.PaintTopInGame(clipState);

            if (clipState != ClipState.AfterClip) return;
            if (!Hud.Game.IsInGame || !Enabled) return;
            if (_shopMainPage?.Visible != true) return;

            DrawKadalaOverlay();
            if (ShowDebugOverlay) DrawDebugOverlay();
        }

        private void DrawKadalaOverlay()
        {
            if (_shopPanel?.Visible != true) return;
            
            var panelRect = _shopPanel.Rectangle;
            float y = panelRect.Y + panelRect.Height * 0.10f;
            float x = panelRect.X + panelRect.Width * 0.04f;

            var itemInfo = ItemSlotMap[SelectedItem];
            var headerLayout = _headerFont.GetTextLayout($"🎰 {itemInfo.Name}");
            _headerFont.DrawText(headerLayout, x, y);
            y += headerLayout.Metrics.Height * 1.2f;

            string info;
            IFont font;
            
            if (!_enabled) { info = "[Shift+K] to enable"; font = _infoFont; }
            else if (_running) { info = $"{_state} ({SessionItemsBought})"; font = _statusFont; }
            else if (!CanStartBuying()) { info = "Need shards/space"; font = _errorFont; }
            else { info = "Ready"; font = _statusFont; }

            font.DrawText(font.GetTextLayout(info), x, y);

            // Highlight target slot when running
            if (_running && _state >= BuyState.MovingToSlot)
            {
                var slotRect = GetSlotRect(itemInfo.SlotRow, itemInfo.SlotCol);
                if (!slotRect.IsEmpty) _highlightBrush.DrawRectangle(slotRect);
            }
        }

        private void DrawDebugOverlay()
        {
            if (_shopPanel?.Visible != true) return;
            
            var panelRect = _shopPanel.Rectangle;
            Hud.Render.CreateBrush(100, 255, 255, 255, 1).DrawRectangle(panelRect);
            
            // Draw tab rectangles
            for (int i = 0; i < 4; i++)
            {
                var tabRect = GetTabRect(i);
                _debugBrush.DrawRectangle(tabRect);
                _infoFont.DrawText(_infoFont.GetTextLayout($"T{i}"), tabRect.X + 5, tabRect.Y + 5);
            }
            
            // Draw slot rectangles
            var itemInfo = ItemSlotMap[SelectedItem];
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 2; col++)
                {
                    var slotRect = GetSlotRect(row, col);
                    bool isTarget = row == itemInfo.SlotRow && col == itemInfo.SlotCol;
                    (isTarget ? _debugBrush2 : Hud.Render.CreateBrush(80, 255, 255, 0, 1)).DrawRectangle(slotRect);
                    _infoFont.DrawText(_infoFont.GetTextLayout($"{row},{col}"), slotRect.X + 5, slotRect.Y + 5);
                }
            }
            
            // Debug info at bottom
            float debugY = panelRect.Y + panelRect.Height + 5;
            var debugInfo = $"State: {_state} | Tab: {_currentTargetTab}/{itemInfo.TabIndex} | Tooltip: {_buyTooltip?.Visible} | Attempts: {_tabSwitchAttempts}";
            _infoFont.DrawText(_infoFont.GetTextLayout(debugInfo), panelRect.X, debugY);
        }

        #endregion

        #region Settings Panel

        public override void DrawSettings(IController hud, RectangleF rect, Dictionary<string, RectangleF> clickAreas, int scrollOffset)
        {
            float x = rect.X, y = rect.Y, w = rect.Width;
            var itemInfo = ItemSlotMap[SelectedItem];

            // Current selection
            var selFont = HasCore ? Core.FontSubheader : _headerFont;
            var selLayout = selFont.GetTextLayout($"▶ {itemInfo.Name}");
            selFont.DrawText(selLayout, x, y);
            
            var costFont = HasCore ? Core.FontMuted : _infoFont;
            var costLayout = costFont.GetTextLayout($"  {itemInfo.Cost} shards");
            costFont.DrawText(costLayout, x + selLayout.Metrics.Width, y + 2);
            y += selLayout.Metrics.Height + 12;

            // Item grid
            float btnW = (w - 12) / 4;
            float btnH = 20;
            float btnGap = 3;

            // Row 1
            DrawCompactItemBtn(x + 0 * (btnW + btnGap), y, btnW, btnH, KadalaItemType.OneHandWeapon, "1H", 75, clickAreas);
            DrawCompactItemBtn(x + 1 * (btnW + btnGap), y, btnW, btnH, KadalaItemType.TwoHandWeapon, "2H", 75, clickAreas);
            DrawCompactItemBtn(x + 2 * (btnW + btnGap), y, btnW, btnH, KadalaItemType.Ring, "Ring", 50, clickAreas);
            DrawCompactItemBtn(x + 3 * (btnW + btnGap), y, btnW, btnH, KadalaItemType.Amulet, "Ammy", 100, clickAreas);
            y += btnH + btnGap;

            // Row 2
            DrawCompactItemBtn(x + 0 * (btnW + btnGap), y, btnW, btnH, KadalaItemType.Helm, "Helm", 25, clickAreas);
            DrawCompactItemBtn(x + 1 * (btnW + btnGap), y, btnW, btnH, KadalaItemType.Gloves, "Gloves", 25, clickAreas);
            DrawCompactItemBtn(x + 2 * (btnW + btnGap), y, btnW, btnH, KadalaItemType.Shoulders, "Shldrs", 25, clickAreas);
            DrawCompactItemBtn(x + 3 * (btnW + btnGap), y, btnW, btnH, KadalaItemType.ChestArmor, "Chest", 25, clickAreas);
            y += btnH + btnGap;

            // Row 3
            DrawCompactItemBtn(x + 0 * (btnW + btnGap), y, btnW, btnH, KadalaItemType.Belt, "Belt", 25, clickAreas);
            DrawCompactItemBtn(x + 1 * (btnW + btnGap), y, btnW, btnH, KadalaItemType.Pants, "Pants", 25, clickAreas);
            DrawCompactItemBtn(x + 2 * (btnW + btnGap), y, btnW, btnH, KadalaItemType.Boots, "Boots", 25, clickAreas);
            DrawCompactItemBtn(x + 3 * (btnW + btnGap), y, btnW, btnH, KadalaItemType.Bracers, "Bracers", 25, clickAreas);
            y += btnH + btnGap;

            // Row 4
            DrawCompactItemBtn(x + 0 * (btnW + btnGap), y, btnW, btnH, KadalaItemType.Quiver, "Quiver", 25, clickAreas);
            DrawCompactItemBtn(x + 1 * (btnW + btnGap), y, btnW, btnH, KadalaItemType.Orb, "Orb", 25, clickAreas);
            DrawCompactItemBtn(x + 2 * (btnW + btnGap), y, btnW, btnH, KadalaItemType.Mojo, "Mojo", 25, clickAreas);
            DrawCompactItemBtn(x + 3 * (btnW + btnGap), y, btnW, btnH, KadalaItemType.Phylactery, "Phylct", 25, clickAreas);
            y += btnH + btnGap;

            // Row 5
            DrawCompactItemBtn(x, y, btnW, btnH, KadalaItemType.Shield, "Shield", 25, clickAreas);
            y += btnH + 14;

            // Settings
            DrawSettingsSeparator(x, y, w);
            y += 8;

            y += DrawCustomToggle(x, y, w, "Auto-Buy", _enabled, clickAreas, "opt_autobuy");
            y += DrawCustomSelector(x, y, w, "Min Shards", MinBloodShardsToStart.ToString(), clickAreas, "opt_min");
            y += DrawCustomSelector(x, y, w, "Stop At", StopAtBloodShards.ToString(), clickAreas, "opt_stop");
            y += DrawCustomSelector(x, y, w, "Speed (ms)", BuyIntervalMs.ToString(), clickAreas, "opt_interval");
            
            y += 6;
            
            var statsFont = HasCore ? Core.FontMuted : _infoFont;
            statsFont.DrawText(statsFont.GetTextLayout($"Session: {SessionItemsBought}  Total: {TotalItemsBought}"), x + 4, y);
            y += 18;

            y += DrawCustomToggle(x, y, w, "Debug Overlay", ShowDebugOverlay, clickAreas, "opt_debug");
        }

        private float DrawCustomToggle(float x, float y, float width, string label, bool value, 
            Dictionary<string, RectangleF> clickAreas, string clickId)
        {
            float rowH = 26f;
            var rect = new RectangleF(x, y, width - 14, rowH);
            clickAreas[clickId] = rect;

            bool hovered = IsMouseOver(rect);
            if (hovered && HasCore)
                Core.SurfaceOverlay.DrawRectangle(rect);

            var labelFont = HasCore ? Core.FontBody : _infoFont;
            var labelLayout = labelFont.GetTextLayout(label);
            labelFont.DrawText(labelLayout, x + 8, y + (rowH - labelLayout.Metrics.Height) / 2);

            float toggleW = 40, toggleH = 18;
            float tx = x + width - 14 - toggleW - 8, ty = y + (rowH - toggleH) / 2;
            
            if (HasCore)
            {
                Core.ToggleTrack.DrawRectangle(tx, ty, toggleW, toggleH);
                float knobSize = toggleH - 4;
                float knobX = value ? tx + toggleW - knobSize - 2 : tx + 2;
                var knobBrush = value ? Core.ToggleOn : Core.ToggleOff;
                knobBrush.DrawRectangle(knobX, ty + 2, knobSize, knobSize);
            }
            else
            {
                _itemBtnDefault.DrawRectangle(tx, ty, toggleW, toggleH);
                var toggleLayout = _infoFont.GetTextLayout(value ? "ON" : "OFF");
                _infoFont.DrawText(toggleLayout, tx + (toggleW - toggleLayout.Metrics.Width) / 2, ty + 2);
            }

            return rowH + 4;
        }

        private float DrawCustomSelector(float x, float y, float width, string label, string value,
            Dictionary<string, RectangleF> clickAreas, string baseClickId)
        {
            float rowH = 26f;

            var labelFont = HasCore ? Core.FontBody : _infoFont;
            var labelLayout = labelFont.GetTextLayout(label);
            labelFont.DrawText(labelLayout, x + 8, y + (rowH - labelLayout.Metrics.Height) / 2);

            float selW = 80, selH = 22;
            float sx = x + width - 14 - selW - 8, sy = y + (rowH - selH) / 2;

            var prevRect = new RectangleF(sx, sy, 20, selH);
            clickAreas[$"{baseClickId}_prev"] = prevRect;
            var prevBrush = HasCore ? (IsMouseOver(prevRect) ? Core.BtnHover : Core.BtnDefault) : _itemBtnDefault;
            prevBrush.DrawRectangle(prevRect);
            var prevFont = HasCore ? Core.FontSmall : _infoFont;
            var prevLayout = prevFont.GetTextLayout("◀");
            prevFont.DrawText(prevLayout, sx + (20 - prevLayout.Metrics.Width) / 2, sy + (selH - prevLayout.Metrics.Height) / 2);

            var valFont = HasCore ? Core.FontMono : _infoFont;
            var valLayout = valFont.GetTextLayout(value);
            valFont.DrawText(valLayout, sx + 20 + (selW - 40 - valLayout.Metrics.Width) / 2, sy + (selH - valLayout.Metrics.Height) / 2);

            var nextRect = new RectangleF(sx + selW - 20, sy, 20, selH);
            clickAreas[$"{baseClickId}_next"] = nextRect;
            var nextBrush = HasCore ? (IsMouseOver(nextRect) ? Core.BtnHover : Core.BtnDefault) : _itemBtnDefault;
            nextBrush.DrawRectangle(nextRect);
            var nextLayout = prevFont.GetTextLayout("▶");
            prevFont.DrawText(nextLayout, sx + selW - 20 + (20 - nextLayout.Metrics.Width) / 2, sy + (selH - nextLayout.Metrics.Height) / 2);

            return rowH + 4;
        }

        private void DrawCompactItemBtn(float x, float y, float w, float h, KadalaItemType itemType, string label, int cost, Dictionary<string, RectangleF> clickAreas)
        {
            var rect = new RectangleF(x, y, w, h);
            clickAreas[$"item_{itemType}"] = rect;
            
            bool selected = SelectedItem == itemType;
            bool hovered = IsMouseOver(rect);
            
            IBrush bgBrush = selected ? _itemBtnSelected : (hovered ? _itemBtnHover : _itemBtnDefault);
            bgBrush.DrawRectangle(rect);
            
            if (selected)
                _itemBtnSelectedBorder.DrawRectangle(rect);
            
            var font = HasCore ? Core.FontSmall : _infoFont;
            var layout = font.GetTextLayout(label);
            font.DrawText(layout, x + (w - layout.Metrics.Width) / 2, y + (h - layout.Metrics.Height) / 2);
        }

        public override void HandleSettingsClick(string clickId)
        {
            if (clickId.StartsWith("item_"))
            {
                if (Enum.TryParse<KadalaItemType>(clickId.Substring(5), out var itemType))
                {
                    SelectedItem = itemType;
                    _currentTargetTab = -1;
                    var info = ItemSlotMap[itemType];
                    SetCoreStatus($"{info.Name} ({info.Cost})", StatusType.Success);
                }
                SavePluginSettings();
                return;
            }

            switch (clickId)
            {
                case "opt_autobuy":
                    _enabled = !_enabled;
                    if (!_enabled) FullStop("Disabled");
                    SetCoreStatus($"Auto-Buy {(_enabled ? "ON" : "OFF")}", _enabled ? StatusType.Success : StatusType.Warning);
                    break;
                case "opt_debug":
                    ShowDebugOverlay = !ShowDebugOverlay;
                    break;
                case "opt_min_prev": MinBloodShardsToStart = Math.Max(0, MinBloodShardsToStart - 50); break;
                case "opt_min_next": MinBloodShardsToStart = Math.Min(2000, MinBloodShardsToStart + 50); break;
                case "opt_stop_prev": StopAtBloodShards = Math.Max(0, StopAtBloodShards - 25); break;
                case "opt_stop_next": StopAtBloodShards = Math.Min(500, StopAtBloodShards + 25); break;
                case "opt_interval_prev": BuyIntervalMs = Math.Max(10, BuyIntervalMs - 10); break;
                case "opt_interval_next": BuyIntervalMs = Math.Min(200, BuyIntervalMs + 10); break;
                default: return;
            }
            SavePluginSettings();
        }

        protected override object GetSettingsObject() => new KadalaSettings
        {
            IsEnabled = _enabled,
            MinBloodShardsToStart = MinBloodShardsToStart,
            StopAtBloodShards = StopAtBloodShards,
            BuyIntervalMs = BuyIntervalMs,
            SelectedItem = (int)SelectedItem,
            TotalItemsBought = TotalItemsBought,
            ShowDebugOverlay = ShowDebugOverlay
        };

        protected override void ApplySettingsObject(object settings)
        {
            if (settings is KadalaSettings s)
            {
                _enabled = s.IsEnabled;
                MinBloodShardsToStart = s.MinBloodShardsToStart;
                StopAtBloodShards = s.StopAtBloodShards;
                BuyIntervalMs = s.BuyIntervalMs;
                SelectedItem = (KadalaItemType)s.SelectedItem;
                TotalItemsBought = s.TotalItemsBought;
                ShowDebugOverlay = s.ShowDebugOverlay;
            }
        }

        private class KadalaSettings : PluginSettingsBase
        {
            public bool IsEnabled { get; set; }
            public int MinBloodShardsToStart { get; set; }
            public int StopAtBloodShards { get; set; }
            public int BuyIntervalMs { get; set; }
            public int SelectedItem { get; set; }
            public int TotalItemsBought { get; set; }
            public bool ShowDebugOverlay { get; set; }
        }

        #endregion
    }
}
