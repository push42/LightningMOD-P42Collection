namespace Turbo.Plugins.Custom.AutoPickupSilent
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
    /// Auto Pickup Silent v2.0 - Core Integrated
    /// 
    /// Ultra-fast silent pickup without interrupting mouse movement
    /// Now integrated with Core Plugin Hub (F10)
    /// 
    /// Press H to toggle on/off
    /// </summary>
    public class AutoPickupSilentPlugin : CustomPluginBase, IKeyEventHandler, IAfterCollectHandler, INewAreaHandler
    {
        #region Plugin Metadata

        public override string PluginId => "auto-pickup-silent";
        public override string PluginName => "Auto Pickup";
        public override string PluginDescription => "Ultra-fast silent item pickup for speed builds";
        public override string PluginVersion => "2.0.0";
        public override string PluginCategory => "automation";
        public override string PluginIcon => "🎁";
        public override bool HasSettings => true;

        #endregion

        #region Runtime State (for Core sidebar)

        // Override to report our actual IsActive state to Core
        public override bool IsActive => _isActiveInternal;
        
        // Override to show item count in sidebar
        public override string StatusText => _isActiveInternal ? $"{_itemsPickedUp}" : "OFF";

        #endregion

        #region Public Settings

        private bool _isActiveInternal;
        public IKeyEvent ToggleKey { get; set; }
        
        /// <summary>
        /// Set the active state (for customizer use)
        /// </summary>
        public void SetActive(bool active) => _isActiveInternal = active;

        // Pickup settings
        public bool PickupLegendary { get; set; } = true;
        public bool PickupAncient { get; set; } = true;
        public bool PickupPrimal { get; set; } = true;
        public bool PickupSet { get; set; } = true;
        public bool PickupGems { get; set; } = true;
        public bool PickupCraftingMaterials { get; set; } = true;
        public bool PickupDeathsBreath { get; set; } = true;
        public bool PickupForgottenSoul { get; set; } = true;
        public bool PickupRiftKeys { get; set; } = true;
        public bool PickupBloodShards { get; set; } = true;
        public bool PickupGold { get; set; } = false;
        public bool PickupPotions { get; set; } = true;
        public bool PickupRamaladni { get; set; } = true;
        public bool PickupRare { get; set; } = false;
        public bool PickupMagic { get; set; } = false;
        public bool PickupWhite { get; set; } = false;

        // Interact settings
        public bool InteractShrines { get; set; } = true;
        public bool InteractPylons { get; set; } = true;
        public bool InteractPylonsInGR { get; set; } = true;
        public int GRLevelForAutoPylon { get; set; } = 100;
        public bool InteractChests { get; set; } = true;
        public bool InteractNormalChests { get; set; } = true;
        public bool InteractResplendentChests { get; set; } = true;
        public bool InteractDoors { get; set; } = true;
        public bool InteractPoolOfReflection { get; set; } = true;
        public bool InteractHealingWells { get; set; } = true;
        public bool InteractDeadBodies { get; set; } = false;
        public bool InteractWeaponRacks { get; set; } = false;
        public bool InteractArmorRacks { get; set; } = false;
        public bool InteractClickables { get; set; } = false;

        // Range settings
        public double PickupRange { get; set; } = 18.0;
        public double InteractRange { get; set; } = 15.0;
        public int PickupsPerCycle { get; set; } = 5;
        public bool RetryPickups { get; set; } = true;

        // UI settings - DISABLED by default, Core sidebar shows status
        public bool ShowStatusPanel { get; set; } = false;
        public bool ShowPanel { get; set; } = false;
        public float PanelX { get; set; } = 0.005f;
        public float PanelY { get; set; } = 0.63f;

        #endregion

        #region Private Fields

        private IWatch _pickupTimer;
        private IWatch _interactTimer;
        private readonly HashSet<uint> _recentlyClicked = new HashSet<uint>();
        private readonly Dictionary<uint, int> _clickAttempts = new Dictionary<uint, int>();
        private IUiElement _inventoryElement;
        private IUiElement _chatUI;

        // Fallback UI
        private IFont _titleFont;
        private IFont _statusFont;
        private IFont _infoFont;
        private IBrush _panelBrush;
        private IBrush _borderBrush;
        private IBrush _accentOnBrush;
        private IBrush _accentOffBrush;

        private int _itemsPickedUp;
        private readonly HashSet<ActorSnoEnum> _doorBlacklist = new HashSet<ActorSnoEnum>();
        private IWatch _cleanupTimer;

        #endregion

        #region Initialization

        public AutoPickupSilentPlugin()
        {
            Enabled = true;
            Order = 50002;
        }

        public override void Load(IController hud)
        {
            base.Load(hud);

            _isActiveInternal = true;
            ToggleKey = Hud.Input.CreateKeyEvent(true, Key.H, false, false, false);

            _pickupTimer = Hud.Time.CreateAndStartWatch();
            _interactTimer = Hud.Time.CreateAndStartWatch();
            _cleanupTimer = Hud.Time.CreateAndStartWatch();
            _inventoryElement = Hud.Inventory.InventoryMainUiElement;
            _chatUI = Hud.Render.RegisterUiElement("Root.NormalLayer.chatentry_dialog_backgroundScreen.chatentry_content.chat_editline", null, null);

            InitializeFallbackUI();
            InitializeBlacklists();

            Log("Auto Pickup loaded");
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
        }

        private void InitializeBlacklists()
        {
            _doorBlacklist.Add(ActorSnoEnum._cald_merchant_cart);
            _doorBlacklist.Add(ActorSnoEnum._a2dun_cald_exit_gate);
            _doorBlacklist.Add(ActorSnoEnum._x1_global_chest_cursedchest);
            _doorBlacklist.Add(ActorSnoEnum._x1_global_chest_cursedchest_b);
            _doorBlacklist.Add(ActorSnoEnum._p1_tgoblin_gate);
        }

        #endregion

        #region Settings Panel

        public override void DrawSettings(IController hud, RectangleF rect, Dictionary<string, RectangleF> clickAreas, int scrollOffset)
        {
            float x = rect.X, y = rect.Y, w = rect.Width;

            // Status
            string statusText = IsActive ? "● ACTIVE" : "○ OFF";
            var statusFont = IsActive ? (HasCore ? Core.FontSuccess : _statusFont) : (HasCore ? Core.FontError : _statusFont);
            var statusLayout = statusFont.GetTextLayout(statusText);
            statusFont.DrawText(statusLayout, x, y);
            y += statusLayout.Metrics.Height + 10;

            // Pickup Items
            y += DrawSettingsHeader(x, y, "Item Pickup");
            y += 8;

            y += DrawToggleSetting(x, y, w, "Legendary Items", PickupLegendary, clickAreas, "toggle_legendary");
            y += DrawToggleSetting(x, y, w, "Ancient/Primal", PickupAncient, clickAreas, "toggle_ancient");
            y += DrawToggleSetting(x, y, w, "Set Items", PickupSet, clickAreas, "toggle_set");
            y += DrawToggleSetting(x, y, w, "Gems", PickupGems, clickAreas, "toggle_gems");
            y += DrawToggleSetting(x, y, w, "Materials", PickupCraftingMaterials, clickAreas, "toggle_mats");
            y += DrawToggleSetting(x, y, w, "Rare Items", PickupRare, clickAreas, "toggle_rare");

            y += 12;

            // Interactions
            y += DrawSettingsHeader(x, y, "Interactions");
            y += 8;

            y += DrawToggleSetting(x, y, w, "Shrines", InteractShrines, clickAreas, "toggle_shrines");
            y += DrawToggleSetting(x, y, w, "Pylons", InteractPylons, clickAreas, "toggle_pylons");
            y += DrawToggleSetting(x, y, w, "Chests", InteractChests, clickAreas, "toggle_chests");
            y += DrawToggleSetting(x, y, w, "Doors", InteractDoors, clickAreas, "toggle_doors");

            y += 12;

            // Range
            y += DrawSettingsHeader(x, y, "Range & Speed");
            y += 8;

            y += DrawSelectorSetting(x, y, w, "Pickup Range", $"{PickupRange:F0}", clickAreas, "sel_range");
            y += DrawSelectorSetting(x, y, w, "Items/Cycle", PickupsPerCycle.ToString(), clickAreas, "sel_percycle");

            y += 16;
            y += DrawSettingsHint(x, y, $"[H] Toggle • Picked: {_itemsPickedUp}");
        }

        public override void HandleSettingsClick(string clickId)
        {
            switch (clickId)
            {
                case "toggle_legendary": PickupLegendary = !PickupLegendary; break;
                case "toggle_ancient": PickupAncient = !PickupAncient; PickupPrimal = PickupAncient; break;
                case "toggle_set": PickupSet = !PickupSet; break;
                case "toggle_gems": PickupGems = !PickupGems; break;
                case "toggle_mats": PickupCraftingMaterials = !PickupCraftingMaterials; PickupDeathsBreath = PickupCraftingMaterials; break;
                case "toggle_rare": PickupRare = !PickupRare; break;
                case "toggle_shrines": InteractShrines = !InteractShrines; break;
                case "toggle_pylons": InteractPylons = !InteractPylons; break;
                case "toggle_chests": InteractChests = !InteractChests; InteractNormalChests = InteractChests; InteractResplendentChests = InteractChests; break;
                case "toggle_doors": InteractDoors = !InteractDoors; break;
                case "sel_range_prev": PickupRange = Math.Max(8, PickupRange - 2); break;
                case "sel_range_next": PickupRange = Math.Min(30, PickupRange + 2); break;
                case "sel_percycle_prev": PickupsPerCycle = Math.Max(1, PickupsPerCycle - 1); break;
                case "sel_percycle_next": PickupsPerCycle = Math.Min(10, PickupsPerCycle + 1); break;
            }
            SavePluginSettings();
        }

        protected override object GetSettingsObject() => new PickupSettings
        {
            IsActive = this._isActiveInternal,
            PickupLegendary = this.PickupLegendary,
            PickupAncient = this.PickupAncient,
            PickupSet = this.PickupSet,
            PickupGems = this.PickupGems,
            PickupCraftingMaterials = this.PickupCraftingMaterials,
            PickupRare = this.PickupRare,
            InteractShrines = this.InteractShrines,
            InteractPylons = this.InteractPylons,
            InteractChests = this.InteractChests,
            InteractDoors = this.InteractDoors,
            PickupRange = this.PickupRange,
            PickupsPerCycle = this.PickupsPerCycle
        };

        protected override void ApplySettingsObject(object settings)
        {
            if (settings is PickupSettings s)
            {
                _isActiveInternal = s.IsActive;
                PickupLegendary = s.PickupLegendary;
                PickupAncient = s.PickupAncient;
                PickupPrimal = s.PickupAncient;
                PickupSet = s.PickupSet;
                PickupGems = s.PickupGems;
                PickupCraftingMaterials = s.PickupCraftingMaterials;
                PickupDeathsBreath = s.PickupCraftingMaterials;
                PickupRare = s.PickupRare;
                InteractShrines = s.InteractShrines;
                InteractPylons = s.InteractPylons;
                InteractChests = s.InteractChests;
                InteractNormalChests = s.InteractChests;
                InteractResplendentChests = s.InteractChests;
                InteractDoors = s.InteractDoors;
                PickupRange = s.PickupRange;
                PickupsPerCycle = s.PickupsPerCycle;
            }
        }

        private class PickupSettings : PluginSettingsBase
        {
            public bool IsActive { get; set; }
            public bool PickupLegendary { get; set; }
            public bool PickupAncient { get; set; }
            public bool PickupSet { get; set; }
            public bool PickupGems { get; set; }
            public bool PickupCraftingMaterials { get; set; }
            public bool PickupRare { get; set; }
            public bool InteractShrines { get; set; }
            public bool InteractPylons { get; set; }
            public bool InteractChests { get; set; }
            public bool InteractDoors { get; set; }
            public double PickupRange { get; set; }
            public int PickupsPerCycle { get; set; }
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
                _recentlyClicked.Clear();
                _clickAttempts.Clear();
                SetCoreStatus($"Auto Pickup {(IsActive ? "ON" : "OFF")}", 
                             IsActive ? StatusType.Success : StatusType.Warning);
            }
        }

        #endregion

        #region New Area Handler

        public void OnNewArea(bool newGame, ISnoArea area)
        {
            _recentlyClicked.Clear();
            _clickAttempts.Clear();
            if (newGame) _itemsPickedUp = 0;
        }

        #endregion

        #region Main Processing

        public void AfterCollect()
        {
            if (!Enabled) return;
            if (!_isActiveInternal) return;
            if (!CanProcess()) return;

            if (_cleanupTimer.ElapsedMilliseconds > 2000)
            {
                CleanupTracking();
                _cleanupTimer.Restart();
            }

            ProcessPickups();

            if (_interactTimer.ElapsedMilliseconds >= 50)
            {
                ProcessInteractions();
                _interactTimer.Restart();
            }
        }

        private bool CanProcess()
        {
            if (!Hud.Game.IsInGame) return false;
            if (Hud.Game.IsPaused || Hud.Game.IsLoading) return false;
            if (Hud.Game.Me.IsDead) return false;
            if (!Hud.Window.IsForeground) return false;
            if (!Hud.Render.MinimapUiElement.Visible) return false;
            if (Hud.Render.IsAnyBlockingUiElementVisible) return false;
            if (_inventoryElement?.Visible == true) return false;
            if (_chatUI?.Visible == true) return false;
            if (Hud.Game.Me.Powers.BuffIsActive(Hud.Sno.SnoPowers.Generic_ActorGhostedBuff.Sno)) return false;
            if (Hud.Game.Me.AnimationState == AcdAnimationState.CastingPortal) return false;
            return true;
        }

        private void CleanupTracking()
        {
            var toRemove = _recentlyClicked.Where(id => 
                !Hud.Game.Items.Any(i => i.AnnId == id && i.Location == ItemLocation.Floor)).ToList();
            foreach (var id in toRemove)
            {
                _recentlyClicked.Remove(id);
                _clickAttempts.Remove(id);
            }
        }

        private void ProcessPickups()
        {
            var items = GetItemsToPickup();
            if (items.Count == 0) return;

            int origX = Hud.Window.CursorX;
            int origY = Hud.Window.CursorY;
            int pickedUp = 0;

            foreach (var item in items)
            {
                if (pickedUp >= PickupsPerCycle) break;
                
                if (_clickAttempts.TryGetValue(item.AnnId, out int attempts))
                {
                    if (attempts >= 3 && !RetryPickups) continue;
                    if (attempts >= 10) continue;
                }

                int x = (int)item.ScreenCoordinate.X;
                int y = (int)item.ScreenCoordinate.Y;
                if (x < 10 || x > Hud.Window.Size.Width - 10 || y < 10 || y > Hud.Window.Size.Height - 10) continue;

                Hud.Interaction.MouseMove(x, y, 0, 0);
                Hud.Interaction.MouseDown(MouseButtons.Left);
                Hud.Interaction.MouseUp(MouseButtons.Left);

                _recentlyClicked.Add(item.AnnId);
                _clickAttempts[item.AnnId] = (_clickAttempts.TryGetValue(item.AnnId, out int c) ? c : 0) + 1;
                _itemsPickedUp++;
                pickedUp++;
            }

            if (pickedUp > 0)
                Hud.Interaction.MouseMove(origX, origY, 0, 0);
        }

        private List<IItem> GetItemsToPickup()
        {
            return Hud.Game.Items
                .Where(i => i.Location == ItemLocation.Floor && i.IsOnScreen && 
                           i.NormalizedXyDistanceToMe <= PickupRange && ShouldPickup(i) &&
                           (!_recentlyClicked.Contains(i.AnnId) || !_clickAttempts.TryGetValue(i.AnnId, out int a) || a < 3))
                .OrderBy(i => i.NormalizedXyDistanceToMe)
                .ToList();
        }

        private bool ShouldPickup(IItem item)
        {
            var sno = item.SnoItem;
            if (sno == null) return false;

            if (PickupPrimal && item.AncientRank == 2) return true;
            if (PickupAncient && item.AncientRank == 1) return true;
            if (PickupRamaladni && sno.NameEnglish == "Ramaladni's Gift") return true;
            if (PickupLegendary && item.IsLegendary && item.AncientRank == 0 && sno.Kind == ItemKind.loot) return true;
            if (PickupSet && item.SetSno != 0) return true;
            if (PickupGems && (sno.Kind == ItemKind.gem || sno.MainGroupCode == "gems_unique")) return true;
            if (PickupDeathsBreath && sno.Sno == 2087837753) return true;
            if (PickupCraftingMaterials && sno.Kind == ItemKind.craft) return true;
            if (PickupRiftKeys && sno.HasGroupCode("riftkeystone")) return true;
            if (PickupPotions && sno.Kind == ItemKind.potion && item.IsLegendary) return true;
            if (PickupRare && item.IsRare && sno.Kind == ItemKind.loot) return true;
            if (PickupMagic && item.IsMagic && sno.Kind == ItemKind.loot) return true;
            if (PickupWhite && item.IsNormal && sno.Kind == ItemKind.loot) return true;

            return false;
        }

        private void ProcessInteractions()
        {
            var actor = GetActorToInteract();
            if (actor == null) return;

            int origX = Hud.Window.CursorX, origY = Hud.Window.CursorY;
            int x = (int)actor.ScreenCoordinate.X, y = (int)actor.ScreenCoordinate.Y;

            if (x > 10 && x < Hud.Window.Size.Width - 10 && y > 10 && y < Hud.Window.Size.Height - 10)
            {
                Hud.Interaction.MouseMove(x, y, 0, 0);
                Hud.Interaction.MouseDown(MouseButtons.Left);
                Hud.Interaction.MouseUp(MouseButtons.Left);
                Hud.Interaction.MouseMove(origX, origY, 0, 0);
            }
        }

        private IActor GetActorToInteract()
        {
            IActor best = null;
            double bestDist = double.MaxValue;

            if (InteractShrines || InteractPylons)
            {
                foreach (var shrine in Hud.Game.Shrines.Where(s => s.IsOnScreen && !s.IsDisabled && !s.IsOperated && s.NormalizedXyDistanceToMe <= InteractRange))
                {
                    if (!ShouldInteractShrine(shrine)) continue;
                    if (shrine.NormalizedXyDistanceToMe < bestDist) { bestDist = shrine.NormalizedXyDistanceToMe; best = shrine; }
                }
            }

            if (InteractChests && best == null)
            {
                if (InteractResplendentChests)
                {
                    foreach (var chest in Hud.Game.ResplendentChests.Where(c => c.IsOnScreen && !c.IsDisabled && !c.IsOperated && c.NormalizedXyDistanceToMe <= InteractRange))
                    {
                        if (chest.NormalizedXyDistanceToMe < bestDist) { bestDist = chest.NormalizedXyDistanceToMe; best = chest; }
                    }
                }
                if (InteractNormalChests)
                {
                    foreach (var chest in Hud.Game.NormalChests.Where(c => c.IsOnScreen && !c.IsDisabled && !c.IsOperated && c.NormalizedXyDistanceToMe <= InteractRange))
                    {
                        if (chest.NormalizedXyDistanceToMe < bestDist) { bestDist = chest.NormalizedXyDistanceToMe; best = chest; }
                    }
                }
            }

            if (InteractDoors && best == null)
            {
                foreach (var door in Hud.Game.Doors.Where(d => d.IsOnScreen && !d.IsDisabled && !d.IsOperated && 
                    d.NormalizedXyDistanceToMe <= InteractRange && !_doorBlacklist.Contains(d.SnoActor.Sno)))
                {
                    if (door.NormalizedXyDistanceToMe < bestDist) { bestDist = door.NormalizedXyDistanceToMe; best = door; }
                }
            }

            return best;
        }

        private bool ShouldInteractShrine(IShrine shrine)
        {
            if (shrine.GizmoType == GizmoType.PoolOfReflection) return InteractPoolOfReflection;
            if (shrine.GizmoType == GizmoType.HealingWell) return InteractHealingWells && Hud.Game.Me.Defense.HealthPct < 0.5;
            
            bool isPylon = shrine.SnoActor.NameEnglish?.Contains("Pylon") == true || 
                          shrine.SnoActor.NameEnglish?.Contains("pylon") == true;
            
            if (isPylon)
            {
                if (!InteractPylons) return false;
                if (Hud.Game.Me.InGreaterRiftRank > 0)
                {
                    if (!InteractPylonsInGR) return false;
                    if (Hud.Game.Me.InGreaterRiftRank > GRLevelForAutoPylon) return false;
                }
                return true;
            }
            
            return InteractShrines;
        }

        #endregion

        #region Rendering

        public override void PaintTopInGame(ClipState clipState)
        {
            base.PaintTopInGame(clipState);
            
            if (clipState != ClipState.BeforeClip) return;
            if (!Hud.Game.IsInGame || !Enabled) return;
            
            // Don't show panel when Core is active - the sidebar shows our status
            if (!ShowStatusPanel && !ShowPanel) return;
            if (HasCore) return;  // Core sidebar shows status, skip our panel

            DrawStatusPanel();
        }

        private void DrawStatusPanel()
        {
            // Panel disabled by default - Core sidebar shows status
            if (!ShowPanel && !ShowStatusPanel) return;
            
            float x = Hud.Window.Size.Width * PanelX;
            float y = Hud.Window.Size.Height * PanelY;
            float w = 120, h = 48, pad = 6;

            _panelBrush.DrawRectangle(x, y, w, h);
            _borderBrush.DrawRectangle(x, y, w, h);

            var accentBrush = IsActive ? _accentOnBrush : _accentOffBrush;
            accentBrush.DrawRectangle(x, y, 3, h);

            float tx = x + pad + 3, ty = y + pad;

            var title = _titleFont.GetTextLayout("Auto Pickup");
            _titleFont.DrawText(title, tx, ty);
            ty += title.Metrics.Height + 2;

            var status = _statusFont.GetTextLayout(IsActive ? "● ON" : "OFF");
            _statusFont.DrawText(status, tx, ty);
            ty += status.Metrics.Height + 1;

            var hint = _infoFont.GetTextLayout("[H] Toggle");
            _infoFont.DrawText(hint, tx, ty);
        }

        #endregion
    }
}
