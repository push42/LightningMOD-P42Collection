namespace Turbo.Plugins.Custom.InventorySorter
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Windows.Forms;
    using SharpDX.DirectInput;
    using Turbo.Plugins.Custom.Core;
    using Turbo.Plugins.Default;

    /// <summary>
    /// Advanced Inventory/Stash Sorter Plugin - Core Integrated v2.0
    /// 
    /// Now integrated with the Core Plugin Framework!
    /// 
    /// Features:
    /// - Preset-based stash organization (Speed Farmer, Collector, etc.)
    /// - In-game configuration UI with click blocking
    /// - Zone-based sorting within tabs
    /// - Row-locked gem sorting (each color gets own row)
    /// - Smart item placement
    /// 
    /// Hotkeys:
    /// - K = Sort current tab/inventory
    /// - Shift+K = Cycle sort mode
    /// - Ctrl+K = Open configuration panel
    /// - ESC = Cancel sorting
    /// </summary>
    public class InventorySorterPlugin : CustomPluginBase, IKeyEventHandler, IInGameTopPainter, IAfterCollectHandler
    {
        #region Plugin Metadata

        public override string PluginId => "inventory-sorter";
        public override string PluginName => "Inventory Sorter";
        public override string PluginDescription => "Smart inventory and stash organization";
        public override string PluginVersion => "2.0.0";
        public override string PluginCategory => "inventory";
        public override string PluginIcon => "📦";
        public override bool HasSettings => true;

        #endregion

        #region Public Properties

        public SorterConfiguration Config { get; private set; }
        public PresetManager PresetMgr { get; private set; }
        public IKeyEvent SortKey { get; set; }
        public IKeyEvent ModeKey { get; set; }
        public IKeyEvent ConfigKey { get; set; }
        public IKeyEvent CancelKey { get; set; }
        
        public SortMode CurrentMode { get { return _currentMode; } }
        // UI settings - DISABLED by default, Core sidebar shows status
        public float PanelX { get; set; } = 0.005f;
        public float PanelY { get; set; } = 0.56f;
        private bool _showPanel = false; // Disabled - use Core sidebar

        #endregion

        #region Private Fields

        private bool _isRunning;
        private bool _shouldCancel;
        private SortMode _currentMode = SortMode.ByCategory;
        
        // Advanced UI
        private SorterConfigUI _configUI;
        
        // Fallback fonts
        private IFont _titleFont;
        private IFont _statusFont;
        private IFont _infoFont;
        private IFont _smallFont;
        private IFont _progressFont;
        
        // Brushes
        private IBrush _panelBrush;
        private IBrush _borderBrush;
        private IBrush _accentBrush;
        private IBrush _accentOffBrush;
        private IBrush _highlightBrush;
        private IBrush _protectedBrush;
        private IBrush _progressBgBrush;
        private IBrush _progressFillBrush;
        
        // Stash
        private IUiElement _stashElement;
        
        // Status
        private string _statusText = "";
        private int _sortedCount;
        private int _totalToSort;
        private IWatch _statusTimer;

        #endregion

        #region Initialization

        public InventorySorterPlugin()
        {
            Enabled = true;
            Order = 10000;
        }

        public override void Load(IController hud)
        {
            base.Load(hud);

            // Configuration
            Config = new SorterConfiguration();
            
            // Preset Manager
            PresetMgr = new PresetManager();
            PresetMgr.DataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "Custom", "InventorySorter");
            PresetMgr.Initialize();

            // Key bindings
            SortKey = Hud.Input.CreateKeyEvent(true, Key.K, false, false, false);
            ModeKey = Hud.Input.CreateKeyEvent(true, Key.K, false, false, true);
            ConfigKey = Hud.Input.CreateKeyEvent(true, Key.K, true, false, false);
            CancelKey = Hud.Input.CreateKeyEvent(true, Key.Escape, false, false, false);

            // Fallback fonts
            _titleFont = Hud.Render.CreateFont("tahoma", 8, 255, 220, 180, 100, true, false, 180, 0, 0, 0, true);
            _statusFont = Hud.Render.CreateFont("tahoma", 7.5f, 255, 255, 255, 255, true, false, 160, 0, 0, 0, true);
            _infoFont = Hud.Render.CreateFont("tahoma", 7, 200, 180, 180, 180, false, false, 140, 0, 0, 0, true);
            _smallFont = Hud.Render.CreateFont("tahoma", 6.5f, 200, 150, 150, 150, false, false, 120, 0, 0, 0, true);
            _progressFont = Hud.Render.CreateFont("tahoma", 7, 255, 100, 255, 100, true, false, 150, 0, 0, 0, true);
            
            // Brushes
            _panelBrush = Hud.Render.CreateBrush(235, 15, 15, 25, 0);
            _borderBrush = Hud.Render.CreateBrush(200, 60, 60, 80, 1f);
            _accentBrush = Hud.Render.CreateBrush(255, 220, 180, 100, 0);
            _accentOffBrush = Hud.Render.CreateBrush(255, 100, 100, 100, 0);
            _highlightBrush = Hud.Render.CreateBrush(150, 100, 255, 100, 2f);
            _protectedBrush = Hud.Render.CreateBrush(150, 255, 200, 0, 2f);
            _progressBgBrush = Hud.Render.CreateBrush(200, 30, 30, 45, 0);
            _progressFillBrush = Hud.Render.CreateBrush(255, 80, 180, 100, 0);

            _stashElement = Hud.Inventory.StashMainUiElement;
            _statusTimer = Hud.Time.CreateWatch();

            // Config UI
            _configUI = new SorterConfigUI(Hud, this);
            _configUI.SetReferences(PresetMgr, Config);

            Log("Inventory Sorter loaded");
        }

        #endregion

        #region Settings Panel

        public override void DrawSettings(IController hud, RectangleF rect, Dictionary<string, RectangleF> clickAreas, int scrollOffset)
        {
            float x = rect.X, y = rect.Y, w = rect.Width;

            // Status
            string statusText = _isRunning ? "● SORTING" : "○ READY";
            var statusFont = _isRunning ? (HasCore ? Core.FontSuccess : _progressFont) : (HasCore ? Core.FontBody : _infoFont);
            var statusLayout = statusFont.GetTextLayout(statusText);
            statusFont.DrawText(statusLayout, x, y);
            y += statusLayout.Metrics.Height + 10;

            // Mode section
            y += DrawSettingsHeader(x, y, "Sort Mode");
            y += 8;

            y += DrawSelectorSetting(x, y, w, "Mode", GetModeName(_currentMode), clickAreas, "sel_mode");

            y += 12;

            // Protection section
            y += DrawSettingsHeader(x, y, "Protection");
            y += 8;

            y += DrawToggleSetting(x, y, w, "Respect Inventory Lock", Config.RespectInventoryLock, clickAreas, "toggle_invlock");
            y += DrawToggleSetting(x, y, w, "Protect Armory Items", Config.ProtectArmoryItems, clickAreas, "toggle_armory");
            y += DrawToggleSetting(x, y, w, "Protect Enchanted", Config.ProtectEnchantedItems, clickAreas, "toggle_enchanted");
            y += DrawToggleSetting(x, y, w, "Protect Socketed", Config.ProtectSocketedItems, clickAreas, "toggle_socketed");

            y += 12;

            // Sorting Options
            y += DrawSettingsHeader(x, y, "Sort Options");
            y += 8;

            y += DrawToggleSetting(x, y, w, "Primals First", Config.PrimalsFirst, clickAreas, "toggle_primals");
            y += DrawToggleSetting(x, y, w, "Group Sets", Config.GroupSets, clickAreas, "toggle_sets");
            y += DrawToggleSetting(x, y, w, "Group Gems by Color", Config.GroupGemsByColor, clickAreas, "toggle_gems");
            y += DrawToggleSetting(x, y, w, "Show Highlights", Config.ShowHighlights, clickAreas, "toggle_highlights");

            y += 16;
            y += DrawSettingsHint(x, y, "[K] Sort • [⇧K] Mode • [^K] Config");
        }

        public override void HandleSettingsClick(string clickId)
        {
            switch (clickId)
            {
                case "sel_mode_prev":
                case "sel_mode_next":
                    CycleMode();
                    break;
                case "toggle_invlock":
                    Config.RespectInventoryLock = !Config.RespectInventoryLock;
                    break;
                case "toggle_armory":
                    Config.ProtectArmoryItems = !Config.ProtectArmoryItems;
                    break;
                case "toggle_enchanted":
                    Config.ProtectEnchantedItems = !Config.ProtectEnchantedItems;
                    break;
                case "toggle_socketed":
                    Config.ProtectSocketedItems = !Config.ProtectSocketedItems;
                    break;
                case "toggle_primals":
                    Config.PrimalsFirst = !Config.PrimalsFirst;
                    break;
                case "toggle_sets":
                    Config.GroupSets = !Config.GroupSets;
                    break;
                case "toggle_gems":
                    Config.GroupGemsByColor = !Config.GroupGemsByColor;
                    break;
                case "toggle_highlights":
                    Config.ShowHighlights = !Config.ShowHighlights;
                    break;
            }
            SavePluginSettings();
        }

        protected override object GetSettingsObject() => new SorterSettings
        {
            CurrentMode = (int)_currentMode,
            RespectInventoryLock = Config.RespectInventoryLock,
            ProtectArmoryItems = Config.ProtectArmoryItems,
            ProtectEnchantedItems = Config.ProtectEnchantedItems,
            ProtectSocketedItems = Config.ProtectSocketedItems,
            PrimalsFirst = Config.PrimalsFirst,
            GroupSets = Config.GroupSets,
            GroupGemsByColor = Config.GroupGemsByColor,
            ShowHighlights = Config.ShowHighlights
        };

        protected override void ApplySettingsObject(object settings)
        {
            if (settings is SorterSettings s)
            {
                _currentMode = (SortMode)s.CurrentMode;
                Config.RespectInventoryLock = s.RespectInventoryLock;
                Config.ProtectArmoryItems = s.ProtectArmoryItems;
                Config.ProtectEnchantedItems = s.ProtectEnchantedItems;
                Config.ProtectSocketedItems = s.ProtectSocketedItems;
                Config.PrimalsFirst = s.PrimalsFirst;
                Config.GroupSets = s.GroupSets;
                Config.GroupGemsByColor = s.GroupGemsByColor;
                Config.ShowHighlights = s.ShowHighlights;
            }
        }

        private class SorterSettings : PluginSettingsBase
        {
            public int CurrentMode { get; set; }
            public bool RespectInventoryLock { get; set; }
            public bool ProtectArmoryItems { get; set; }
            public bool ProtectEnchantedItems { get; set; }
            public bool ProtectSocketedItems { get; set; }
            public bool PrimalsFirst { get; set; }
            public bool GroupSets { get; set; }
            public bool GroupGemsByColor { get; set; }
            public bool ShowHighlights { get; set; }
        }

        #endregion

        #region Public Methods

        public void SetSortMode(SortMode mode)
        {
            _currentMode = mode;
            _statusText = "Mode: " + GetModeName(mode);
            _statusTimer.Restart();
            SetCoreStatus($"Sort mode: {GetModeName(mode)}", StatusType.Info);
        }

        #endregion

        #region Key Handler

        public void OnKeyEvent(IKeyEvent keyEvent)
        {
            if (!Hud.Game.IsInGame) return;
            if (!Enabled) return;
            if (!IsInventoryOpen()) return;

            if (ConfigKey.Matches(keyEvent) && keyEvent.IsPressed)
            {
                _configUI.IsVisible = !_configUI.IsVisible;
                return;
            }

            if (_configUI.IsVisible && _configUI.IsMouseOverUI) return;

            if (ModeKey.Matches(keyEvent) && keyEvent.IsPressed)
            {
                CycleMode();
                return;
            }

            if (SortKey.Matches(keyEvent) && keyEvent.IsPressed)
            {
                if (_isRunning)
                {
                    _shouldCancel = true;
                    _statusText = "Cancelling...";
                }
                else
                {
                    StartSort();
                }
                return;
            }

            if (CancelKey.Matches(keyEvent) && keyEvent.IsPressed)
            {
                if (_isRunning)
                {
                    _shouldCancel = true;
                    _statusText = "Cancelling...";
                }
                else if (_configUI.IsVisible)
                {
                    _configUI.IsVisible = false;
                }
            }
        }

        #endregion

        #region Main Loop

        public void AfterCollect()
        {
            if (!Hud.Game.IsInGame) return;
            if (!Enabled) return;

            if (_configUI.IsVisible && IsInventoryOpen())
                _configUI.HandleMouseInput();

            if (!_isRunning) return;

            if (_shouldCancel || !IsInventoryOpen() || !Hud.Window.IsForeground)
                StopSort();
        }

        #endregion

        #region Sort Logic

        private void StartSort()
        {
            if (_isRunning) return;
            
            _isRunning = true;
            _shouldCancel = false;
            _sortedCount = 0;
            _statusText = "Analyzing...";
            _statusTimer.Restart();

            try
            {
                bool isStash = IsStashOpen();
                var items = CollectItems(isStash);
                
                if (items.Count == 0)
                {
                    _statusText = "No items to sort";
                    _isRunning = false;
                    return;
                }

                var sorted = SortItemList(items);
                List<MoveOp> moves;

                if (_currentMode == SortMode.RowLocked)
                    moves = PlanRowLockedMoves(sorted, isStash);
                else
                    moves = PlanMoves(sorted, isStash);
                
                if (moves.Count == 0)
                {
                    _statusText = "Already sorted!";
                    SetCoreStatus("Already sorted!", StatusType.Success);
                    _isRunning = false;
                    return;
                }

                _totalToSort = moves.Count;
                int cursorX = Hud.Window.CursorX;
                int cursorY = Hud.Window.CursorY;

                foreach (var move in moves)
                {
                    if (_shouldCancel || !IsInventoryOpen()) break;

                    ExecuteMove(move, isStash);
                    _sortedCount++;
                    _statusText = string.Format("Sorting {0}/{1}", _sortedCount, _totalToSort);
                    
                    Hud.ReCollect();
                    Hud.Wait(Config.WaitAfterMoveMs);
                }

                Hud.Interaction.MouseMove(cursorX, cursorY, 1, 1);
                _statusText = _shouldCancel ? "Cancelled" : string.Format("Done! ({0} items)", _sortedCount);
                SetCoreStatus(_statusText, _shouldCancel ? StatusType.Warning : StatusType.Success);
            }
            catch (Exception)
            {
                _statusText = "Error!";
                SetCoreStatus("Sort error", StatusType.Error);
            }
            finally
            {
                _isRunning = false;
                _shouldCancel = false;
            }
        }

        private void StopSort()
        {
            _isRunning = false;
            _shouldCancel = false;
            _statusText = "Stopped";
        }

        private void CycleMode()
        {
            var modes = new[] { SortMode.ByCategory, SortMode.ByQuality, SortMode.ByType, SortMode.BySize, SortMode.Alphabetical, SortMode.RowLocked };
            int idx = Array.IndexOf(modes, _currentMode);
            _currentMode = modes[(idx + 1) % modes.Length];
            _statusText = "Mode: " + GetModeName(_currentMode);
            _statusTimer.Restart();
            SetCoreStatus($"Mode: {GetModeName(_currentMode)}", StatusType.Info);
        }

        private List<SortItem> CollectItems(bool isStash)
        {
            var result = new List<SortItem>();
            IEnumerable<IItem> items;
            int gridStartY = 0;

            if (isStash)
            {
                int page = Hud.Inventory.SelectedStashPageIndex;
                int tab = Hud.Inventory.SelectedStashTabIndex;
                gridStartY = (page * Hud.Inventory.MaxStashTabCountPerPage + tab) * 10;
                items = Hud.Inventory.ItemsInStash.Where(i => 
                    i.InventoryY >= gridStartY && i.InventoryY < gridStartY + 10);
            }
            else
            {
                items = Hud.Inventory.ItemsInInventory;
            }

            foreach (var item in items)
            {
                if (item == null || item.SnoItem == null) continue;
                if (IsProtected(item)) continue;

                var si = new SortItem
                {
                    Item = item,
                    UniqueId = item.ItemUniqueId,
                    X = item.InventoryX,
                    Y = isStash ? item.InventoryY - gridStartY : item.InventoryY,
                    Width = item.SnoItem.ItemWidth,
                    Height = item.SnoItem.ItemHeight,
                    Category = GetCategory(item),
                    SubCategory = GetSubCategory(item),
                    Quality = GetQuality(item),
                    Name = item.SnoItem.NameLocalized ?? "",
                    SetSno = item.SetSno,
                    GemType = GetGemType(item),
                    GemRank = GetGemRank(item),
                    RowGroup = GetRowGroup(item)
                };
                result.Add(si);
            }

            return result;
        }

        private int GetRowGroup(IItem item)
        {
            var sno = item.SnoItem;
            if (sno == null) return 999;

            if (sno.MainGroupCode == "gems_unique") return 0;
            if (sno.Kind == ItemKind.gem) return GetGemType(item);
            if (sno.Kind == ItemKind.craft) return 10;
            if (item.AncientRank == 2) return 20;
            if (item.AncientRank == 1) return 30;
            if (item.SetSno != 0) return 40;
            if (item.IsLegendary) return 50;
            return 100;
        }

        private List<SortItem> SortItemList(List<SortItem> items)
        {
            IOrderedEnumerable<SortItem> query;

            switch (_currentMode)
            {
                case SortMode.ByCategory:
                    if (Config.PrimalsFirst)
                        query = items.OrderByDescending(i => i.Item.AncientRank == 2 ? 1 : 0).ThenBy(i => (int)i.Category);
                    else
                        query = items.OrderBy(i => (int)i.Category);

                    if (Config.GroupSets)
                        query = query.ThenBy(i => i.SetSno == 0 ? 1 : 0).ThenBy(i => i.SetSno);
                    if (Config.GroupGemsByColor)
                        query = query.ThenBy(i => i.GemType);

                    return query.ThenByDescending(i => i.GemRank).ThenByDescending(i => i.Quality).ThenBy(i => i.Name).ToList();

                case SortMode.ByQuality:
                    return items.OrderByDescending(i => i.Quality).ThenByDescending(i => i.GemRank).ThenBy(i => i.Name).ToList();

                case SortMode.ByType:
                    return items.OrderBy(i => GetSlotOrder(i.Item)).ThenBy(i => i.SetSno).ThenByDescending(i => i.Quality).ThenBy(i => i.Name).ToList();

                case SortMode.BySize:
                    return items.OrderByDescending(i => i.Width * i.Height).ThenByDescending(i => i.Quality).ThenBy(i => i.Name).ToList();

                case SortMode.Alphabetical:
                    return items.OrderBy(i => i.Name).ThenByDescending(i => i.Quality).ToList();

                case SortMode.RowLocked:
                    return items.OrderBy(i => i.RowGroup).ThenByDescending(i => i.GemRank).ThenByDescending(i => i.Quality).ThenBy(i => i.Name).ToList();

                default:
                    return items;
            }
        }

        private List<MoveOp> PlanRowLockedMoves(List<SortItem> sortedItems, bool isStash)
        {
            var moves = new List<MoveOp>();
            int gridW = isStash ? 7 : 10;
            int gridH = isStash ? 10 : 6;
            bool[,] grid = new bool[gridW, gridH];

            if (!isStash && Config.RespectInventoryLock)
            {
                var lockArea = Hud.Inventory.InventoryLockArea;
                for (int x = lockArea.X; x < lockArea.X + lockArea.Width && x < gridW; x++)
                    for (int y = lockArea.Y; y < lockArea.Y + lockArea.Height && y < gridH; y++)
                        if (x >= 0 && y >= 0) grid[x, y] = true;
            }

            var groupedItems = sortedItems.GroupBy(i => i.RowGroup).OrderBy(g => g.Key).ToList();
            int currentRow = 0, currentX = 0;

            foreach (var group in groupedItems)
            {
                var itemsInGroup = group.OrderByDescending(i => i.GemRank).ThenByDescending(i => i.Quality).ToList();
                
                if (currentX > 0) { currentRow++; currentX = 0; }

                foreach (var item in itemsInGroup)
                {
                    if (currentRow >= gridH) break;
                    bool placed = false;
                    
                    while (currentX <= gridW - item.Width)
                    {
                        if (CanPlace(grid, currentX, currentRow, item.Width, item.Height))
                        {
                            for (int dx = 0; dx < item.Width; dx++)
                                for (int dy = 0; dy < item.Height; dy++)
                                    if (currentRow + dy < gridH) grid[currentX + dx, currentRow + dy] = true;

                            if (item.X != currentX || item.Y != currentRow)
                                moves.Add(new MoveOp { Item = item, TargetX = currentX, TargetY = currentRow });

                            currentX += item.Width;
                            placed = true;
                            break;
                        }
                        currentX++;
                    }

                    if (!placed)
                    {
                        currentRow++;
                        currentX = 0;
                        if (currentRow >= gridH) break;

                        if (CanPlace(grid, currentX, currentRow, item.Width, item.Height))
                        {
                            for (int dx = 0; dx < item.Width; dx++)
                                for (int dy = 0; dy < item.Height; dy++)
                                    if (currentRow + dy < gridH) grid[currentX + dx, currentRow + dy] = true;

                            if (item.X != currentX || item.Y != currentRow)
                                moves.Add(new MoveOp { Item = item, TargetX = currentX, TargetY = currentRow });

                            currentX += item.Width;
                        }
                    }
                }
            }

            return ReorderMoves(moves);
        }

        private List<MoveOp> PlanMoves(List<SortItem> sortedItems, bool isStash)
        {
            var moves = new List<MoveOp>();
            int gridW = isStash ? 7 : 10;
            int gridH = isStash ? 10 : 6;
            bool[,] grid = new bool[gridW, gridH];

            if (!isStash && Config.RespectInventoryLock)
            {
                var lockArea = Hud.Inventory.InventoryLockArea;
                for (int x = lockArea.X; x < lockArea.X + lockArea.Width && x < gridW; x++)
                    for (int y = lockArea.Y; y < lockArea.Y + lockArea.Height && y < gridH; y++)
                        if (x >= 0 && y >= 0) grid[x, y] = true;
            }

            foreach (var item in sortedItems)
            {
                int targetX = -1, targetY = -1;
                
                for (int y = 0; y <= gridH - item.Height && targetX < 0; y++)
                    for (int x = 0; x <= gridW - item.Width && targetX < 0; x++)
                        if (CanPlace(grid, x, y, item.Width, item.Height))
                            { targetX = x; targetY = y; }

                if (targetX < 0) continue;

                for (int dx = 0; dx < item.Width; dx++)
                    for (int dy = 0; dy < item.Height; dy++)
                        grid[targetX + dx, targetY + dy] = true;

                if (item.X != targetX || item.Y != targetY)
                    moves.Add(new MoveOp { Item = item, TargetX = targetX, TargetY = targetY });
            }

            return ReorderMoves(moves);
        }

        private List<MoveOp> ReorderMoves(List<MoveOp> moves)
        {
            var result = new List<MoveOp>();
            var remaining = new List<MoveOp>(moves);
            int maxIter = moves.Count * 3;

            for (int iter = 0; iter < maxIter && remaining.Count > 0; iter++)
            {
                for (int i = remaining.Count - 1; i >= 0; i--)
                {
                    var move = remaining[i];
                    bool blocked = remaining.Any(other => other != move && 
                        Overlaps(move.TargetX, move.TargetY, move.Item.Width, move.Item.Height,
                                other.Item.X, other.Item.Y, other.Item.Width, other.Item.Height));

                    if (!blocked) { result.Add(move); remaining.RemoveAt(i); }
                }
            }

            result.AddRange(remaining);
            return result;
        }

        private void ExecuteMove(MoveOp move, bool isStash)
        {
            var item = move.Item.Item;
            if (item == null) return;

            Hud.Interaction.ClickInventoryItem(MouseButtons.Left, item);
            Hud.Wait(Config.ClickDelayMs);

            RectangleF targetRect = isStash 
                ? Hud.Inventory.GetRectInStash(move.TargetX, move.TargetY, 1, 1)
                : Hud.Inventory.GetRectInInventory(move.TargetX, move.TargetY, 1, 1);

            float cx = targetRect.X + targetRect.Width / 2;
            float cy = targetRect.Y + targetRect.Height / 2;
            
            Hud.Interaction.MouseMove((int)cx, (int)cy, 1, 1);
            Hud.Wait(Config.MoveDelayMs);
            Hud.Interaction.MouseDown(MouseButtons.Left);
            Hud.Wait(10);
            Hud.Interaction.MouseUp(MouseButtons.Left);
            Hud.Wait(Config.ClickDelayMs);
        }

        private bool CanPlace(bool[,] grid, int x, int y, int w, int h)
        {
            int gridW = grid.GetLength(0), gridH = grid.GetLength(1);
            if (x + w > gridW || y + h > gridH) return false;
            for (int dx = 0; dx < w; dx++)
                for (int dy = 0; dy < h; dy++)
                    if (grid[x + dx, y + dy]) return false;
            return true;
        }

        private bool Overlaps(int x1, int y1, int w1, int h1, int x2, int y2, int w2, int h2)
        {
            return !(x1 + w1 <= x2 || x2 + w2 <= x1 || y1 + h1 <= y2 || y2 + h2 <= y1);
        }

        #endregion

        #region UI Painting

        public override void PaintTopInGame(ClipState clipState)
        {
            // Call base for Core registration
            base.PaintTopInGame(clipState);
            
            if (clipState != ClipState.BeforeClip) return;
            if (!Hud.Game.IsInGame || !Enabled) return;

            DrawPanel();

            if (_configUI.IsVisible && IsInventoryOpen())
            {
                var invRect = Hud.Inventory.InventoryMainUiElement.Rectangle;
                _configUI.Render(invRect);
            }

            if (Config.ShowHighlights && IsInventoryOpen())
                DrawItemHighlights();
        }

        private void DrawPanel()
        {
            // Panel disabled by default - Core sidebar shows status
            if (!_showPanel && !_isRunning) return;
            
            // Only show when sorting is active
            if (!_isRunning) return;
            
            float x = Hud.Window.Size.Width * PanelX;
            float y = Hud.Window.Size.Height * PanelY;
            float w = 150, h = 72, pad = 6;

            _panelBrush.DrawRectangle(x, y, w, h);
            _borderBrush.DrawRectangle(x, y, w, h);

            var accentBrush = _accentBrush;
            accentBrush.DrawRectangle(x, y, 3, h);

            float tx = x + pad + 3, ty = y + pad, contentW = w - pad * 2 - 3;

            var titleFont = HasCore ? Core.FontTitle : _titleFont;
            var title = titleFont.GetTextLayout("📦 Sorting...");
            titleFont.DrawText(title, tx, ty);
            ty += title.Metrics.Height + 2;

            // Progress bar
            float progressW = contentW, progressH = 6;
            _progressBgBrush.DrawRectangle(tx, ty, progressW, progressH);
            float pct = _totalToSort > 0 ? (float)_sortedCount / _totalToSort : 0;
            _progressFillBrush.DrawRectangle(tx, ty, progressW * pct, progressH);
            ty += progressH + 3;

            var sLayout = _progressFont.GetTextLayout(_statusText);
            _progressFont.DrawText(sLayout, tx, ty);
        }

        private void DrawItemHighlights()
        {
            bool isStash = IsStashOpen();
            var items = CollectItems(isStash);
            var sortableIds = new HashSet<string>(items.Select(i => i.UniqueId));

            foreach (var item in isStash ? Hud.Inventory.ItemsInStash : Hud.Inventory.ItemsInInventory)
            {
                if (item == null) continue;
                var rect = Hud.Inventory.GetItemRect(item);
                if (rect == RectangleF.Empty) continue;

                if (isStash)
                {
                    int page = Hud.Inventory.SelectedStashPageIndex;
                    int tab = Hud.Inventory.SelectedStashTabIndex;
                    int gridStartY = (page * Hud.Inventory.MaxStashTabCountPerPage + tab) * 10;
                    if (item.InventoryY < gridStartY || item.InventoryY >= gridStartY + 10) continue;
                }

                if (sortableIds.Contains(item.ItemUniqueId))
                    _highlightBrush.DrawRectangle(rect);
                else if (IsProtected(item))
                    _protectedBrush.DrawRectangle(rect);
            }
        }

        private string GetModeName(SortMode mode)
        {
            switch (mode)
            {
                case SortMode.ByCategory: return "Category";
                case SortMode.ByQuality: return "Quality";
                case SortMode.ByType: return "Type";
                case SortMode.BySize: return "Size";
                case SortMode.Alphabetical: return "A-Z";
                case SortMode.RowLocked: return "Row-Lock";
                default: return "?";
            }
        }

        #endregion

        #region Helper Methods

        private bool IsInventoryOpen() => Hud.Inventory.InventoryMainUiElement?.Visible == true;
        private bool IsStashOpen() => _stashElement?.Visible == true && IsInventoryOpen();

        private bool IsProtected(IItem item)
        {
            if (Config.RespectInventoryLock && item.IsInventoryLocked) return true;
            if (Config.ProtectArmoryItems && Hud.Game.Me.ArmorySets != null)
                foreach (var set in Hud.Game.Me.ArmorySets)
                    if (set?.ContainsItem(item) == true) return true;
            if (Config.ProtectEnchantedItems && item.EnchantedAffixCounter > 0) return true;
            if (Config.ProtectSocketedItems && item.ItemsInSocket?.Length > 0) return true;
            return false;
        }

        private ItemCategory GetCategory(IItem item)
        {
            var sno = item.SnoItem;
            if (sno == null) return ItemCategory.Unknown;

            if (sno.MainGroupCode == "gems_unique") return ItemCategory.LegendaryGem;
            if (sno.Kind == ItemKind.gem) return ItemCategory.FlawlessRoyalGem;
            if (sno.Kind == ItemKind.craft) return ItemCategory.CraftingMaterial;

            bool isWeapon = (sno.MainGroupCode ?? "").Contains("weapon") || (sno.MainGroupCode ?? "").Contains("sword");
            bool isJewelry = (sno.MainGroupCode ?? "").Contains("ring") || (sno.MainGroupCode ?? "").Contains("amulet");

            if (item.AncientRank == 2) return isWeapon ? ItemCategory.PrimalAncientWeapon : isJewelry ? ItemCategory.PrimalAncientJewelry : ItemCategory.PrimalAncientArmor;
            if (item.AncientRank == 1) return isWeapon ? ItemCategory.AncientWeapon : isJewelry ? ItemCategory.AncientJewelry : ItemCategory.AncientArmor;
            if (item.SetSno != 0) return isWeapon ? ItemCategory.SetWeapon : isJewelry ? ItemCategory.SetJewelry : ItemCategory.SetArmor;
            if (item.IsLegendary) return isWeapon ? ItemCategory.LegendaryWeapon : isJewelry ? ItemCategory.LegendaryJewelry : ItemCategory.LegendaryArmor;
            if (item.IsRare) return ItemCategory.RareArmor;
            if (item.IsMagic) return ItemCategory.MagicArmor;

            return ItemCategory.Unknown;
        }

        private int GetSubCategory(IItem item)
        {
            var sno = item.SnoItem;
            if (sno == null) return 0;
            if (sno.Kind == ItemKind.gem) return GetGemType(item);
            if (sno.MainGroupCode == "gems_unique") return (int)sno.Sno;
            if (item.SetSno != 0) return (int)item.SetSno;
            return 0;
        }

        private int GetGemType(IItem item)
        {
            if (item.SnoItem.Kind != ItemKind.gem) return 0;
            string code = item.SnoItem.Code ?? "";
            string name = (item.SnoItem.NameEnglish ?? "").ToLower();
            if (code.Contains("Amethyst") || name.Contains("amethyst")) return 1;
            if (code.Contains("Diamond") || name.Contains("diamond")) return 2;
            if (code.Contains("Emerald") || name.Contains("emerald")) return 3;
            if (code.Contains("Ruby") || name.Contains("ruby")) return 4;
            if (code.Contains("Topaz") || name.Contains("topaz")) return 5;
            return 9;
        }

        private int GetGemRank(IItem item)
        {
            if (item.SnoItem.MainGroupCode == "gems_unique") return item.JewelRank;
            if (item.SnoItem.Kind == ItemKind.gem)
            {
                string code = item.SnoItem.Code ?? "";
                string name = (item.SnoItem.NameEnglish ?? "").ToLower();
                if (code.Contains("FlawlessRoyal") || name.Contains("flawless royal")) return 10;
                if (code.Contains("Royal") || name.Contains("royal")) return 9;
                if (code.Contains("FlawlessImperial") || name.Contains("flawless imperial")) return 8;
                if (code.Contains("Imperial") || name.Contains("imperial")) return 7;
                if (code.Contains("FlawlessMarquise") || name.Contains("flawless marquise")) return 6;
                if (code.Contains("Marquise") || name.Contains("marquise")) return 5;
                return 1;
            }
            return 0;
        }

        private int GetQuality(IItem item)
        {
            return item.AncientRank * 1000 + (int)item.Quality * 100 + (item.SetSno != 0 ? 50 : 0) + item.JewelRank;
        }

        private int GetSlotOrder(IItem item)
        {
            if (item?.SnoItem == null) return 99;
            switch (item.SnoItem.UsedLocation1)
            {
                case ItemLocation.Head: return 1;
                case ItemLocation.Shoulders: return 2;
                case ItemLocation.Torso: return 3;
                case ItemLocation.Hands: return 4;
                case ItemLocation.Waist: return 5;
                case ItemLocation.Legs: return 6;
                case ItemLocation.Feet: return 7;
                case ItemLocation.Bracers: return 8;
                case ItemLocation.Neck: return 9;
                case ItemLocation.LeftRing:
                case ItemLocation.RightRing: return 10;
                case ItemLocation.RightHand: return 11;
                case ItemLocation.LeftHand: return 12;
                default: return 99;
            }
        }

        #endregion
    }

    #region Helper Classes

    internal class SortItem
    {
        public IItem Item;
        public string UniqueId;
        public int X, Y, Width, Height;
        public ItemCategory Category;
        public int SubCategory;
        public int Quality;
        public string Name;
        public uint SetSno;
        public int GemType;
        public int GemRank;
        public int RowGroup;
    }

    internal class MoveOp
    {
        public SortItem Item;
        public int TargetX, TargetY;
    }

    #endregion
}
