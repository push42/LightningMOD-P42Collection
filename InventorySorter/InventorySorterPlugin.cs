namespace Turbo.Plugins.Custom.InventorySorter
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
    /// Advanced Inventory/Stash Sorter Plugin with In-Game Configuration
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
    public class InventorySorterPlugin : BasePlugin, IKeyEventHandler, IInGameTopPainter, IAfterCollectHandler
    {
        #region Public Properties

        public SorterConfiguration Config { get; private set; }
        public PresetManager PresetMgr { get; private set; }
        public IKeyEvent SortKey { get; set; }
        public IKeyEvent ModeKey { get; set; }
        public IKeyEvent ConfigKey { get; set; }
        public IKeyEvent CancelKey { get; set; }
        
        public SortMode CurrentMode { get { return _currentMode; } }

        // Panel positioning (percentage of screen)
        public float PanelX { get; set; } = 0.005f;
        public float PanelY { get; set; } = 0.56f;

        #endregion

        #region Private Fields

        private bool _isRunning;
        private bool _shouldCancel;
        private SortMode _currentMode = SortMode.ByCategory;
        
        // Advanced UI
        private SorterConfigUI _configUI;
        
        // UI - Unified styling
        private IFont _titleFont;
        private IFont _statusFont;
        private IFont _infoFont;
        private IFont _smallFont;
        private IFont _progressFont;
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
            ModeKey = Hud.Input.CreateKeyEvent(true, Key.K, false, false, true);      // Shift+K
            ConfigKey = Hud.Input.CreateKeyEvent(true, Key.K, true, false, false);    // Ctrl+K
            CancelKey = Hud.Input.CreateKeyEvent(true, Key.Escape, false, false, false);

            // Initialize fonts
            _titleFont = Hud.Render.CreateFont("tahoma", 8, 255, 220, 180, 100, true, false, 180, 0, 0, 0, true);
            _statusFont = Hud.Render.CreateFont("tahoma", 7.5f, 255, 255, 255, 255, true, false, 160, 0, 0, 0, true);
            _infoFont = Hud.Render.CreateFont("tahoma", 7, 200, 180, 180, 180, false, false, 140, 0, 0, 0, true);
            _smallFont = Hud.Render.CreateFont("tahoma", 6.5f, 200, 150, 150, 150, false, false, 120, 0, 0, 0, true);
            _progressFont = Hud.Render.CreateFont("tahoma", 7, 255, 100, 255, 100, true, false, 150, 0, 0, 0, true);
            
            // Initialize brushes
            _panelBrush = Hud.Render.CreateBrush(235, 15, 15, 25, 0);
            _borderBrush = Hud.Render.CreateBrush(200, 60, 60, 80, 1f);
            _accentBrush = Hud.Render.CreateBrush(255, 220, 180, 100, 0);
            _accentOffBrush = Hud.Render.CreateBrush(255, 100, 100, 100, 0);
            _highlightBrush = Hud.Render.CreateBrush(150, 100, 255, 100, 2f);
            _protectedBrush = Hud.Render.CreateBrush(150, 255, 200, 0, 2f);
            _progressBgBrush = Hud.Render.CreateBrush(200, 30, 30, 45, 0);
            _progressFillBrush = Hud.Render.CreateBrush(255, 80, 180, 100, 0);

            // Stash element
            _stashElement = Hud.Inventory.StashMainUiElement;
            _statusTimer = Hud.Time.CreateWatch();

            // Config UI
            _configUI = new SorterConfigUI(Hud, this);
            _configUI.SetReferences(PresetMgr, Config);
        }

        #endregion

        #region Public Methods

        public void SetSortMode(SortMode mode)
        {
            _currentMode = mode;
            _statusText = "Mode: " + GetModeName(mode);
            _statusTimer.Restart();
        }

        #endregion

        #region Key Handler

        public void OnKeyEvent(IKeyEvent keyEvent)
        {
            if (!Hud.Game.IsInGame) return;
            if (!IsInventoryOpen()) return;

            // Ctrl+K = Toggle config panel
            if (ConfigKey.Matches(keyEvent) && keyEvent.IsPressed)
            {
                _configUI.IsVisible = !_configUI.IsVisible;
                return;
            }

            // If config is visible and handles input, don't process other keys
            if (_configUI.IsVisible && _configUI.IsMouseOverUI) return;

            // Shift+K = Cycle mode
            if (ModeKey.Matches(keyEvent) && keyEvent.IsPressed)
            {
                CycleMode();
                return;
            }

            // K = Sort or Cancel
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

            // ESC = Cancel
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

            // Handle config UI mouse input
            if (_configUI.IsVisible && IsInventoryOpen())
            {
                _configUI.HandleMouseInput();
            }

            if (!_isRunning) return;

            if (_shouldCancel || !IsInventoryOpen() || !Hud.Window.IsForeground)
            {
                StopSort();
            }
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

                // Use row-locked placement for RowLocked mode
                if (_currentMode == SortMode.RowLocked)
                {
                    moves = PlanRowLockedMoves(sorted, isStash);
                }
                else
                {
                    moves = PlanMoves(sorted, isStash);
                }
                
                if (moves.Count == 0)
                {
                    _statusText = "Already sorted!";
                    _isRunning = false;
                    return;
                }

                _totalToSort = moves.Count;
                int cursorX = Hud.Window.CursorX;
                int cursorY = Hud.Window.CursorY;

                foreach (var move in moves)
                {
                    if (_shouldCancel || !IsInventoryOpen())
                        break;

                    ExecuteMove(move, isStash);
                    _sortedCount++;
                    _statusText = string.Format("Sorting {0}/{1}", _sortedCount, _totalToSort);
                    
                    Hud.ReCollect();
                    Hud.Wait(Config.WaitAfterMoveMs);
                }

                Hud.Interaction.MouseMove(cursorX, cursorY, 1, 1);
                _statusText = _shouldCancel ? "Cancelled" : string.Format("Done! ({0} items)", _sortedCount);
            }
            catch (Exception)
            {
                _statusText = "Error!";
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

                var si = new SortItem();
                si.Item = item;
                si.UniqueId = item.ItemUniqueId;
                si.X = item.InventoryX;
                si.Y = isStash ? item.InventoryY - gridStartY : item.InventoryY;
                si.Width = item.SnoItem.ItemWidth;
                si.Height = item.SnoItem.ItemHeight;
                si.Category = GetCategory(item);
                si.SubCategory = GetSubCategory(item);
                si.Quality = GetQuality(item);
                si.Name = item.SnoItem.NameLocalized ?? "";
                si.SetSno = item.SetSno;
                si.GemType = GetGemType(item);
                si.GemRank = GetGemRank(item);
                si.RowGroup = GetRowGroup(item); // For row-locked sorting
                
                result.Add(si);
            }

            return result;
        }

        /// <summary>
        /// Get the row group for an item (used in RowLocked mode)
        /// Items in the same row group will be placed together in rows
        /// </summary>
        private int GetRowGroup(IItem item)
        {
            var sno = item.SnoItem;
            if (sno == null) return 999;

            // Legendary gems - group 0 (top priority)
            if (sno.MainGroupCode == "gems_unique") return 0;

            // Regular gems - group by color (1-6)
            if (sno.Kind == ItemKind.gem)
            {
                return GetGemType(item); // 1=Amethyst, 2=Diamond, 3=Emerald, 4=Ruby, 5=Topaz
            }

            // Crafting materials - group 10
            if (sno.Kind == ItemKind.craft) return 10;

            // Equipment by ancient rank
            if (item.AncientRank == 2) return 20; // Primals
            if (item.AncientRank == 1) return 30; // Ancients

            // Set items
            if (item.SetSno != 0) return 40;

            // Legendaries
            if (item.IsLegendary) return 50;

            // Other
            return 100;
        }

        private List<SortItem> SortItemList(List<SortItem> items)
        {
            IOrderedEnumerable<SortItem> query;

            switch (_currentMode)
            {
                case SortMode.ByCategory:
                    if (Config.PrimalsFirst)
                    {
                        query = items.OrderByDescending(i => i.Item.AncientRank == 2 ? 1 : 0)
                                     .ThenBy(i => (int)i.Category);
                    }
                    else
                    {
                        query = items.OrderBy(i => (int)i.Category);
                    }

                    if (Config.GroupSets)
                        query = query.ThenBy(i => i.SetSno == 0 ? 1 : 0).ThenBy(i => i.SetSno);

                    if (Config.GroupGemsByColor)
                        query = query.ThenBy(i => i.GemType);

                    return query.ThenByDescending(i => i.GemRank)
                                .ThenByDescending(i => i.Quality)
                                .ThenBy(i => i.Name)
                                .ToList();

                case SortMode.ByQuality:
                    return items.OrderByDescending(i => i.Quality)
                                .ThenByDescending(i => i.GemRank)
                                .ThenBy(i => i.Name)
                                .ToList();

                case SortMode.ByType:
                    return items.OrderBy(i => GetSlotOrder(i.Item))
                                .ThenBy(i => i.SetSno)
                                .ThenByDescending(i => i.Quality)
                                .ThenBy(i => i.Name)
                                .ToList();

                case SortMode.BySize:
                    return items.OrderByDescending(i => i.Width * i.Height)
                                .ThenByDescending(i => i.Quality)
                                .ThenBy(i => i.Name)
                                .ToList();

                case SortMode.Alphabetical:
                    return items.OrderBy(i => i.Name)
                                .ThenByDescending(i => i.Quality)
                                .ToList();

                case SortMode.RowLocked:
                    // Sort by row group first, then by rank/quality within group
                    return items.OrderBy(i => i.RowGroup)
                                .ThenByDescending(i => i.GemRank)
                                .ThenByDescending(i => i.Quality)
                                .ThenBy(i => i.Name)
                                .ToList();

                default:
                    return items;
            }
        }

        /// <summary>
        /// Plan moves for row-locked sorting where each category gets its own row(s)
        /// </summary>
        private List<MoveOp> PlanRowLockedMoves(List<SortItem> sortedItems, bool isStash)
        {
            var moves = new List<MoveOp>();
            int gridW = isStash ? 7 : 10;
            int gridH = isStash ? 10 : 6;
            
            bool[,] grid = new bool[gridW, gridH];

            // Respect inventory lock
            if (!isStash && Config.RespectInventoryLock)
            {
                var lockArea = Hud.Inventory.InventoryLockArea;
                for (int x = lockArea.X; x < lockArea.X + lockArea.Width && x < gridW; x++)
                    for (int y = lockArea.Y; y < lockArea.Y + lockArea.Height && y < gridH; y++)
                        if (x >= 0 && y >= 0) grid[x, y] = true;
            }

            // Group items by their row group
            var groupedItems = sortedItems.GroupBy(i => i.RowGroup).OrderBy(g => g.Key).ToList();

            int currentRow = 0;
            int currentX = 0;

            foreach (var group in groupedItems)
            {
                var itemsInGroup = group.OrderByDescending(i => i.GemRank).ThenByDescending(i => i.Quality).ToList();
                
                // Start each new group on a new row (unless it's the first group and row 0)
                if (currentX > 0)
                {
                    currentRow++;
                    currentX = 0;
                }

                foreach (var item in itemsInGroup)
                {
                    if (currentRow >= gridH) break; // Out of space

                    // Find position in current row
                    bool placed = false;
                    
                    // Try to place in current row
                    while (currentX <= gridW - item.Width)
                    {
                        if (CanPlace(grid, currentX, currentRow, item.Width, item.Height))
                        {
                            // Place item
                            for (int dx = 0; dx < item.Width; dx++)
                                for (int dy = 0; dy < item.Height; dy++)
                                    if (currentRow + dy < gridH)
                                        grid[currentX + dx, currentRow + dy] = true;

                            if (item.X != currentX || item.Y != currentRow)
                                moves.Add(new MoveOp { Item = item, TargetX = currentX, TargetY = currentRow });

                            currentX += item.Width;
                            placed = true;
                            break;
                        }
                        currentX++;
                    }

                    // If couldn't place in current row, move to next row
                    if (!placed)
                    {
                        currentRow++;
                        currentX = 0;

                        if (currentRow >= gridH) break;

                        // Try again in new row
                        if (CanPlace(grid, currentX, currentRow, item.Width, item.Height))
                        {
                            for (int dx = 0; dx < item.Width; dx++)
                                for (int dy = 0; dy < item.Height; dy++)
                                    if (currentRow + dy < gridH)
                                        grid[currentX + dx, currentRow + dy] = true;

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
            int gridW = grid.GetLength(0);
            int gridH = grid.GetLength(1);
            
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

        public void PaintTopInGame(ClipState clipState)
        {
            if (clipState != ClipState.BeforeClip) return;
            if (!Hud.Game.IsInGame) return;

            // Draw main control panel
            DrawPanel();

            // Draw config UI if visible
            if (_configUI.IsVisible && IsInventoryOpen())
            {
                var invRect = Hud.Inventory.InventoryMainUiElement.Rectangle;
                _configUI.Render(invRect);
            }

            // Draw item highlights
            if (Config.ShowHighlights && IsInventoryOpen())
            {
                DrawItemHighlights();
            }
        }

        private void DrawPanel()
        {
            float x = Hud.Window.Size.Width * PanelX;
            float y = Hud.Window.Size.Height * PanelY;
            float w = 150;
            float h = _isRunning ? 72 : 60;
            float pad = 6;

            // Panel background
            _panelBrush.DrawRectangle(x, y, w, h);
            _borderBrush.DrawRectangle(x, y, w, h);

            // Accent bar
            var accentBrush = _isRunning ? _accentBrush : _accentOffBrush;
            accentBrush.DrawRectangle(x, y, 3, h);

            float tx = x + pad + 3;
            float ty = y + pad;
            float contentW = w - pad * 2 - 3;

            // Title
            var title = _titleFont.GetTextLayout("Inventory Sort");
            _titleFont.DrawText(title, tx, ty);
            ty += title.Metrics.Height + 2;

            // Mode
            string modeStr = "Mode: " + GetModeName(_currentMode);
            var modeLayout = _statusFont.GetTextLayout(modeStr);
            _statusFont.DrawText(modeLayout, tx, ty);
            ty += modeLayout.Metrics.Height + 2;

            // Status or hints
            if (_isRunning)
            {
                // Progress bar
                float progressW = contentW;
                float progressH = 6;
                _progressBgBrush.DrawRectangle(tx, ty, progressW, progressH);
                
                float pct = _totalToSort > 0 ? (float)_sortedCount / _totalToSort : 0;
                _progressFillBrush.DrawRectangle(tx, ty, progressW * pct, progressH);
                ty += progressH + 3;

                var sLayout = _progressFont.GetTextLayout(_statusText);
                _progressFont.DrawText(sLayout, tx, ty);
            }
            else if (_statusTimer.IsRunning && _statusTimer.ElapsedMilliseconds < 2500 && !string.IsNullOrEmpty(_statusText))
            {
                var sLayout = _infoFont.GetTextLayout(_statusText);
                _infoFont.DrawText(sLayout, tx, ty);
            }
            else
            {
                var hint = _smallFont.GetTextLayout("[K] Sort [⇧K] Mode [^K] Config");
                _smallFont.DrawText(hint, tx, ty);
            }
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

                // Only highlight items in current tab for stash
                if (isStash)
                {
                    int page = Hud.Inventory.SelectedStashPageIndex;
                    int tab = Hud.Inventory.SelectedStashTabIndex;
                    int gridStartY = (page * Hud.Inventory.MaxStashTabCountPerPage + tab) * 10;
                    if (item.InventoryY < gridStartY || item.InventoryY >= gridStartY + 10)
                        continue;
                }

                if (sortableIds.Contains(item.ItemUniqueId))
                {
                    // Will be sorted - green highlight
                    _highlightBrush.DrawRectangle(rect);
                }
                else if (IsProtected(item))
                {
                    // Protected - gold highlight
                    _protectedBrush.DrawRectangle(rect);
                }
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
        public int RowGroup; // For row-locked sorting
    }

    internal class MoveOp
    {
        public SortItem Item;
        public int TargetX, TargetY;
    }

    #endregion
}
