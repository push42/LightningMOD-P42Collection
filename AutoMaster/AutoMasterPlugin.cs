namespace Turbo.Plugins.Custom.AutoMaster
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;
    using SharpDX.DirectInput;
    using Turbo.Plugins.Default;

    /// <summary>
    /// Auto Pickup & Interact Plugin
    /// Automatically picks up items and interacts with objects
    /// </summary>
    public class AutoMasterPlugin : BasePlugin, IKeyEventHandler, IInGameTopPainter, IAfterCollectHandler, INewAreaHandler
    {
        #region Public Settings

        public bool IsActive { get; set; }
        public IKeyEvent ToggleKey { get; set; }

        // === PICKUP SETTINGS ===
        public bool PickupLegendary { get; set; }
        public bool PickupAncient { get; set; }
        public bool PickupPrimal { get; set; }
        public bool PickupSet { get; set; }
        public bool PickupGems { get; set; }
        public bool PickupCraftingMaterials { get; set; }
        public bool PickupDeathsBreath { get; set; }
        public bool PickupForgottenSoul { get; set; }
        public bool PickupRiftKeys { get; set; }
        public bool PickupBloodShards { get; set; }
        public bool PickupGold { get; set; }
        public bool PickupPotions { get; set; }
        public bool PickupRamaladni { get; set; }
        public bool PickupRare { get; set; }
        public bool PickupMagic { get; set; }
        public bool PickupWhite { get; set; }

        // === INTERACT SETTINGS ===
        public bool InteractShrines { get; set; }
        public bool InteractPylons { get; set; }
        public bool InteractPylonsInGR { get; set; }
        public int GRLevelForAutoPylon { get; set; }
        public bool InteractChests { get; set; }
        public bool InteractNormalChests { get; set; }
        public bool InteractResplendentChests { get; set; }
        public bool InteractDoors { get; set; }
        public bool InteractPoolOfReflection { get; set; }
        public bool InteractHealingWells { get; set; }
        public bool InteractDeadBodies { get; set; }
        public bool InteractWeaponRacks { get; set; }
        public bool InteractArmorRacks { get; set; }
        public bool InteractClickables { get; set; }

        // === RANGE SETTINGS ===
        public double PickupRange { get; set; }
        public double InteractRange { get; set; }

        // === UI SETTINGS ===
        public bool ShowStatusPanel { get; set; }
        public float PanelX { get; set; }
        public float PanelY { get; set; }

        #endregion

        #region Private Fields

        private IWatch _pickupTimer;
        private IWatch _interactTimer;
        private readonly HashSet<uint> _clickedItems = new HashSet<uint>();
        private readonly HashSet<uint> _clickedActors = new HashSet<uint>();
        private bool _isProcessing;
        private IUiElement _inventoryElement;

        // UI - Unified styling
        private IFont _titleFont;
        private IFont _statusFont;
        private IFont _infoFont;
        private IBrush _panelBrush;
        private IBrush _borderBrush;
        private IBrush _accentOnBrush;
        private IBrush _accentOffBrush;

        // Stats
        private int _itemsPickedUp;
        private int _objectsInteracted;

        // Blacklists
        private readonly HashSet<ActorSnoEnum> _doorBlacklist = new HashSet<ActorSnoEnum>();
        private readonly HashSet<ActorSnoEnum> _actorBlacklist = new HashSet<ActorSnoEnum>();

        #endregion

        #region Initialization

        public AutoMasterPlugin()
        {
            Enabled = true;
            Order = 9999;
        }

        public override void Load(IController hud)
        {
            base.Load(hud);

            IsActive = true;
            ToggleKey = Hud.Input.CreateKeyEvent(true, Key.P, false, false, false);  // P = Toggle Auto Pickup

            // Pickup defaults
            PickupLegendary = true;
            PickupAncient = true;
            PickupPrimal = true;
            PickupSet = true;
            PickupGems = true;
            PickupCraftingMaterials = true;
            PickupDeathsBreath = true;
            PickupForgottenSoul = true;
            PickupRiftKeys = true;
            PickupBloodShards = true;
            PickupGold = false;
            PickupPotions = true;
            PickupRamaladni = true;
            PickupRare = false;
            PickupMagic = false;
            PickupWhite = false;

            // Interact defaults
            InteractShrines = true;
            InteractPylons = true;
            InteractPylonsInGR = true;
            GRLevelForAutoPylon = 100;
            InteractChests = true;
            InteractNormalChests = true;
            InteractResplendentChests = true;
            InteractDoors = true;
            InteractPoolOfReflection = true;
            InteractHealingWells = true;
            InteractDeadBodies = true;
            InteractWeaponRacks = true;
            InteractArmorRacks = true;
            InteractClickables = true;

            // Ranges
            PickupRange = 15.0;
            InteractRange = 12.0;

            // UI
            ShowStatusPanel = true;
            PanelX = 0.005f;
            PanelY = 0.42f;  // Below SmartEvade (0.35)

            // Initialize
            _pickupTimer = Hud.Time.CreateAndStartWatch();
            _interactTimer = Hud.Time.CreateAndStartWatch();
            _inventoryElement = Hud.Inventory.InventoryMainUiElement;

            // Unified UI styling
            _titleFont = Hud.Render.CreateFont("tahoma", 8, 255, 220, 180, 100, true, false, 180, 0, 0, 0, true);
            _statusFont = Hud.Render.CreateFont("tahoma", 7.5f, 255, 255, 255, 255, true, false, 160, 0, 0, 0, true);
            _infoFont = Hud.Render.CreateFont("tahoma", 7, 200, 180, 180, 180, false, false, 140, 0, 0, 0, true);
            
            _panelBrush = Hud.Render.CreateBrush(235, 15, 15, 25, 0);
            _borderBrush = Hud.Render.CreateBrush(200, 60, 60, 80, 1f);
            _accentOnBrush = Hud.Render.CreateBrush(255, 80, 200, 80, 0);
            _accentOffBrush = Hud.Render.CreateBrush(255, 200, 80, 80, 0);

            InitializeBlacklists();
        }

        private void InitializeBlacklists()
        {
            _doorBlacklist.Add(ActorSnoEnum._cald_merchant_cart);
            _doorBlacklist.Add(ActorSnoEnum._a2dun_cald_exit_gate);
            _doorBlacklist.Add(ActorSnoEnum._trout_cultists_summoning_portal_b);
            _doorBlacklist.Add(ActorSnoEnum._caout_target_dummy);
            _doorBlacklist.Add(ActorSnoEnum._a3dun_keep_bridge);
            _doorBlacklist.Add(ActorSnoEnum._x1_global_chest_cursedchest);
            _doorBlacklist.Add(ActorSnoEnum._x1_global_chest_cursedchest_b);
            _doorBlacklist.Add(ActorSnoEnum._p1_tgoblin_gate);
            _doorBlacklist.Add(ActorSnoEnum._p1_tgoblin_vault_door);
            _doorBlacklist.Add(ActorSnoEnum._event_1000monster_portal);

            _actorBlacklist.Add(ActorSnoEnum._cos_pet_mimic_01);
            _actorBlacklist.Add(ActorSnoEnum._x1_fortress_crystal_prison_shield);
        }

        #endregion

        #region Key Handler

        public void OnKeyEvent(IKeyEvent keyEvent)
        {
            if (!Hud.Game.IsInGame) return;

            if (ToggleKey.Matches(keyEvent) && keyEvent.IsPressed)
            {
                IsActive = !IsActive;
                _clickedItems.Clear();
                _clickedActors.Clear();
            }
        }

        #endregion

        #region New Area Handler

        public void OnNewArea(bool newGame, ISnoArea area)
        {
            _clickedItems.Clear();
            _clickedActors.Clear();
            if (newGame)
            {
                _itemsPickedUp = 0;
                _objectsInteracted = 0;
            }
        }

        #endregion

        #region Main Processing

        public void AfterCollect()
        {
            if (!IsActive) return;
            if (!CanProcess()) return;
            if (_isProcessing) return;

            _isProcessing = true;

            try
            {
                if (_pickupTimer.ElapsedMilliseconds >= 5)
                {
                    ProcessPickups();
                    _pickupTimer.Restart();
                }

                if (_interactTimer.ElapsedMilliseconds >= 10)
                {
                    ProcessInteractions();
                    _interactTimer.Restart();
                }
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private bool CanProcess()
        {
            if (!Hud.Game.IsInGame) return false;
            if (Hud.Game.IsPaused) return false;
            if (Hud.Game.IsLoading) return false;
            if (Hud.Game.Me.IsDead) return false;
            if (!Hud.Window.IsForeground) return false;
            if (!Hud.Render.MinimapUiElement.Visible) return false;
            if (Hud.Render.IsAnyBlockingUiElementVisible) return false;
            if (_inventoryElement.Visible) return false;
            if (Hud.Game.Me.Powers.BuffIsActive(Hud.Sno.SnoPowers.Generic_ActorGhostedBuff.Sno)) return false;

            if (Hud.Interaction.IsHotKeySet(ActionKey.Move) && 
                Hud.Interaction.IsContinuousActionStarted(ActionKey.Move))
                return false;

            return true;
        }

        #endregion

        #region Pickup Processing

        private void ProcessPickups()
        {
            var itemsToPickup = GetItemsToPickup();
            if (itemsToPickup.Count == 0) return;

            var item = itemsToPickup[0];
            if (item == null || item.Location != ItemLocation.Floor) return;
            if (!HasInventorySpace(item)) return;

            PickupItem(item);
        }

        private List<IItem> GetItemsToPickup()
        {
            var result = new List<IItem>();

            foreach (var item in Hud.Game.Items)
            {
                if (item.Location != ItemLocation.Floor) continue;
                if (!item.IsOnScreen) continue;
                if (item.NormalizedXyDistanceToMe > PickupRange) continue;
                if (_clickedItems.Contains(item.AnnId)) continue;
                if (item.AccountBound && !item.BoundToMyAccount) continue;

                if (ShouldPickup(item))
                    result.Add(item);
            }

            result.Sort((a, b) => a.NormalizedXyDistanceToMe.CompareTo(b.NormalizedXyDistanceToMe));
            return result;
        }

        private bool ShouldPickup(IItem item)
        {
            var sno = item.SnoItem;
            if (sno == null) return false;

            if (PickupPrimal && item.AncientRank == 2) return true;
            if (PickupAncient && item.AncientRank == 1) return true;
            if (PickupLegendary && item.IsLegendary && item.AncientRank == 0 && 
                (sno.Kind == ItemKind.loot || sno.Kind == ItemKind.potion)) return true;
            if (PickupSet && item.SetSno != 0) return true;
            if (PickupGems && sno.Kind == ItemKind.gem) return true;
            if (PickupGems && sno.MainGroupCode == "gems_unique") return true;
            if (PickupDeathsBreath && sno.Sno == 2087837753) return true;
            if (PickupCraftingMaterials && sno.Kind == ItemKind.craft) return true;
            if (PickupRiftKeys && sno.HasGroupCode("riftkeystone")) return true;
            if (PickupRamaladni && sno.NameEnglish == "Ramaladni's Gift") return true;
            if (PickupPotions && sno.Kind == ItemKind.potion) return true;
            if (PickupCraftingMaterials && sno.Code != null && sno.Code.StartsWith("P72_Soulshard")) return true;
            if (PickupCraftingMaterials && sno.NameEnglish == "Hellforge Ember") return true;
            if (PickupCraftingMaterials && sno.HasGroupCode("uber")) return true;
            if (PickupGems && sno.NameEnglish == "Whisper of Atonement") return true;
            if (PickupRare && item.IsRare && sno.Kind == ItemKind.loot) return true;
            if (PickupMagic && item.IsMagic && sno.Kind == ItemKind.loot) return true;
            if (PickupWhite && item.IsNormal && sno.Kind == ItemKind.loot) return true;

            return false;
        }

        private bool HasInventorySpace(IItem item)
        {
            int freeSpace = Hud.Game.Me.InventorySpaceTotal - Hud.Game.InventorySpaceUsed;
            int needed = item.SnoItem.ItemWidth * item.SnoItem.ItemHeight;

            if (item.SnoItem.Kind == ItemKind.gem || item.SnoItem.Kind == ItemKind.craft)
            {
                foreach (var invItem in Hud.Inventory.ItemsInInventory)
                {
                    if (invItem.SnoItem.Sno == item.SnoItem.Sno && invItem.Quantity + item.Quantity <= 5000)
                        return true;
                }
            }

            return freeSpace >= needed;
        }

        private void PickupItem(IItem item)
        {
            _clickedItems.Add(item.AnnId);
            int tempX = Hud.Window.CursorX;
            int tempY = Hud.Window.CursorY;

            try
            {
                for (int i = 0; i < 30; i++)
                {
                    if (!Hud.Game.Items.Any(x => x.AnnId == item.AnnId && x.Location == ItemLocation.Floor))
                    {
                        _itemsPickedUp++;
                        break;
                    }
                    Hud.Interaction.MouseMove((int)item.ScreenCoordinate.X, (int)item.ScreenCoordinate.Y, 1, 1);
                    Hud.Interaction.MouseDown(MouseButtons.Left);
                    Hud.Interaction.MouseUp(MouseButtons.Left);
                    Hud.Wait(3);
                }
            }
            finally
            {
                Hud.Interaction.MouseMove(tempX, tempY, 1, 1);
            }
        }

        #endregion

        #region Interaction Processing

        private void ProcessInteractions()
        {
            var actor = GetActorToInteract();
            if (actor == null) return;
            InteractWithActor(actor);
        }

        private IActor GetActorToInteract()
        {
            IActor best = null;
            double bestDist = double.MaxValue;

            foreach (var actor in Hud.Game.Actors)
            {
                if (!actor.IsOnScreen) continue;
                if (actor.IsDisabled || actor.IsOperated) continue;
                if (actor.NormalizedXyDistanceToMe > InteractRange) continue;
                if (_clickedActors.Contains(actor.AnnId)) continue;
                if (_actorBlacklist.Contains(actor.SnoActor.Sno)) continue;
                if (!ShouldInteract(actor)) continue;

                if (actor.NormalizedXyDistanceToMe < bestDist)
                {
                    bestDist = actor.NormalizedXyDistanceToMe;
                    best = actor;
                }
            }

            if (InteractShrines || InteractPylons)
            {
                foreach (var shrine in Hud.Game.Shrines)
                {
                    if (!shrine.IsOnScreen) continue;
                    if (shrine.IsDisabled || shrine.IsOperated) continue;
                    if (shrine.NormalizedXyDistanceToMe > InteractRange) continue;
                    if (_clickedActors.Contains(shrine.AnnId)) continue;
                    if (!ShouldInteractShrine(shrine)) continue;

                    if (shrine.NormalizedXyDistanceToMe < bestDist)
                    {
                        bestDist = shrine.NormalizedXyDistanceToMe;
                        best = shrine;
                    }
                }
            }

            if (InteractChests)
            {
                if (InteractNormalChests)
                {
                    foreach (var chest in Hud.Game.NormalChests)
                    {
                        if (!chest.IsOnScreen || chest.IsDisabled || chest.IsOperated) continue;
                        if (chest.NormalizedXyDistanceToMe > InteractRange) continue;
                        if (_clickedActors.Contains(chest.AnnId)) continue;

                        if (chest.NormalizedXyDistanceToMe < bestDist)
                        {
                            bestDist = chest.NormalizedXyDistanceToMe;
                            best = chest;
                        }
                    }
                }

                if (InteractResplendentChests)
                {
                    foreach (var chest in Hud.Game.ResplendentChests)
                    {
                        if (!chest.IsOnScreen || chest.IsDisabled || chest.IsOperated) continue;
                        if (chest.NormalizedXyDistanceToMe > InteractRange) continue;
                        if (_clickedActors.Contains(chest.AnnId)) continue;

                        if (chest.NormalizedXyDistanceToMe < bestDist)
                        {
                            bestDist = chest.NormalizedXyDistanceToMe;
                            best = chest;
                        }
                    }
                }
            }

            if (InteractDoors)
            {
                foreach (var door in Hud.Game.Doors)
                {
                    if (!door.IsOnScreen || door.IsDisabled || door.IsOperated) continue;
                    if (door.NormalizedXyDistanceToMe > InteractRange) continue;
                    if (_clickedActors.Contains(door.AnnId)) continue;
                    if (_doorBlacklist.Contains(door.SnoActor.Sno)) continue;

                    if (door.NormalizedXyDistanceToMe < bestDist)
                    {
                        bestDist = door.NormalizedXyDistanceToMe;
                        best = door;
                    }
                }
            }

            return best;
        }

        private bool ShouldInteract(IActor actor)
        {
            var gizmo = actor.GizmoType;
            var kind = actor.SnoActor.Kind;

            if (InteractDeadBodies && kind == ActorKind.DeadBody) return true;
            if (InteractWeaponRacks && kind == ActorKind.WeaponRack) return true;
            if (InteractArmorRacks && kind == ActorKind.ArmorRack) return true;

            if (InteractClickables && gizmo == GizmoType.Chest && kind == ActorKind.None)
            {
                if (actor.SnoActor.NameEnglish == "Chandelier Chain") return false;
                if (actor.SnoActor.NameEnglish == "Diabolical Chest") return false;
                return true;
            }

            return false;
        }

        private bool ShouldInteractShrine(IShrine shrine)
        {
            if (InteractHealingWells && shrine.GizmoType == GizmoType.HealingWell)
                return Hud.Game.Me.Defense.HealthPct < 0.7;

            if (InteractPoolOfReflection && shrine.GizmoType == GizmoType.PoolOfReflection)
                return Hud.Game.NumberOfPlayersInGame == 1;

            if (shrine.SnoActor.Kind == ActorKind.Shrine && shrine.GizmoType != GizmoType.HealingWell && 
                shrine.GizmoType != GizmoType.PoolOfReflection)
            {
                bool isPylon = shrine.SnoActor.NameEnglish != null && 
                              (shrine.SnoActor.NameEnglish.Contains("Pylon") || 
                               shrine.SnoActor.NameEnglish.Contains("pylon"));

                if (isPylon)
                {
                    if (!InteractPylons) return false;

                    if (Hud.Game.Me.InGreaterRiftRank > 0)
                    {
                        if (!InteractPylonsInGR) return false;
                        if (Hud.Game.Me.InGreaterRiftRank > GRLevelForAutoPylon) return false;

                        bool someoneHasNemesis = Hud.Game.Players
                            .Where(p => p.CoordinateKnown && !p.IsMe)
                            .Any(p => p.Powers.BuffIsActive(Hud.Sno.SnoPowers.NemesisBracers.Sno));

                        if (someoneHasNemesis && !Hud.Game.Me.Powers.BuffIsActive(Hud.Sno.SnoPowers.NemesisBracers.Sno))
                            return false;
                    }
                    return true;
                }
                else
                {
                    return InteractShrines;
                }
            }

            return false;
        }

        private void InteractWithActor(IActor actor)
        {
            _clickedActors.Add(actor.AnnId);
            int tempX = Hud.Window.CursorX;
            int tempY = Hud.Window.CursorY;

            try
            {
                for (int i = 0; i < 50; i++)
                {
                    if (!Hud.Game.Actors.Any(x => x.AnnId == actor.AnnId && !x.IsDisabled && !x.IsOperated))
                    {
                        _objectsInteracted++;
                        break;
                    }
                    Hud.Interaction.MouseMove((int)actor.ScreenCoordinate.X, (int)actor.ScreenCoordinate.Y, 1, 1);
                    Hud.Interaction.MouseDown(MouseButtons.Left);
                    Hud.Interaction.MouseUp(MouseButtons.Left);
                    Hud.Wait(5);
                }
            }
            finally
            {
                Hud.Interaction.MouseMove(tempX, tempY, 1, 1);
            }
        }

        #endregion

        #region UI Rendering

        public void PaintTopInGame(ClipState clipState)
        {
            if (clipState != ClipState.BeforeClip) return;
            if (!Hud.Game.IsInGame) return;
            if (!ShowStatusPanel) return;

            DrawStatusPanel();
        }

        private void DrawStatusPanel()
        {
            float x = Hud.Window.Size.Width * PanelX;
            float y = Hud.Window.Size.Height * PanelY;
            float w = 120;
            float h = 48;
            float pad = 6;

            // Panel background
            _panelBrush.DrawRectangle(x, y, w, h);
            _borderBrush.DrawRectangle(x, y, w, h);

            // Accent bar (left side indicator)
            var accentBrush = IsActive ? _accentOnBrush : _accentOffBrush;
            accentBrush.DrawRectangle(x, y, 3, h);

            float tx = x + pad + 3;
            float ty = y + pad;

            // Title
            var title = _titleFont.GetTextLayout("Auto Pickup");
            _titleFont.DrawText(title, tx, ty);
            ty += title.Metrics.Height + 2;

            // Status
            string statusStr = IsActive ? "● ON" : "○ OFF";
            var statusLayout = _statusFont.GetTextLayout(statusStr);
            _statusFont.DrawText(statusLayout, tx, ty);
            ty += statusLayout.Metrics.Height + 1;

            // Hotkey
            var hint = _infoFont.GetTextLayout("[P] Toggle");
            _infoFont.DrawText(hint, tx, ty);
        }

        #endregion
    }
}
