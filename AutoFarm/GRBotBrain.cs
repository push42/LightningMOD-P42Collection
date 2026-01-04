namespace Turbo.Plugins.Custom.AutoFarm
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Turbo.Plugins.Default;
    using Turbo.Plugins.Custom.Navigation;
    using Turbo.Plugins.Custom.Navigation.NavMesh;

    /// <summary>
    /// GR Bot Brain - Decision making and objective management
    /// </summary>
    public class GRBotBrain
    {
        public IController Hud { get; set; }
        public GRBotConfig Config { get; set; }
        public GRBotState CurrentState { get; private set; } = GRBotState.Idle;
        public GRRunStats CurrentRun { get; private set; }
        public GRSessionStats SessionStats { get; private set; } = new GRSessionStats();
        
        // Navigation
        public SpatialAwarenessEngine Awareness { get; set; }
        public NavMeshManager NavMesh { get; set; }
        
        // Current objective
        public NavObjective CurrentObjective { get; private set; }
        public Queue<NavObjective> ObjectiveQueue { get; private set; } = new Queue<NavObjective>();
        
        // Pathfinding
        public PathResult CurrentPath { get; private set; }
        public int PathWaypointIndex { get; private set; }
        
        // State tracking
        private IWatch _stateTimer;
        private IWatch _pathRecalcTimer;
        private IWatch _stuckTimer;
        private IWorldCoordinate _lastPosition;
        private float _distanceSinceLastCheck;
        private int _deathsThisRun;
        
        // UI element references for town navigation
        private IUiElement _obeliskUi;
        private IUiElement _riftAcceptUi;
        private IUiElement _urshiUpgradeUi;
        
        // Known locations
        private Dictionary<string, IWorldCoordinate> _knownLocations = new Dictionary<string, IWorldCoordinate>();
        
        // Pylon tracking
        private HashSet<uint> _usedPylons = new HashSet<uint>();
        private List<(IShrine shrine, bool saved)> _pylonsThisFloor = new List<(IShrine, bool)>();

        public void Initialize()
        {
            _stateTimer = Hud.Time.CreateWatch();
            _pathRecalcTimer = Hud.Time.CreateWatch();
            _stuckTimer = Hud.Time.CreateWatch();
            
            // Initialize NavMesh
            if (NavMesh == null)
            {
                NavMesh = new NavMeshManager { Hud = Hud };
                NavMesh.Initialize();
            }
            
            // Initialize Awareness
            if (Awareness == null)
            {
                Awareness = new SpatialAwarenessEngine { Hud = Hud };
            }

            // Register UI elements
            _obeliskUi = Hud.Render.RegisterUiElement("Root.NormalLayer.rift_dialog_mainPage", null, null);
            _riftAcceptUi = Hud.Render.RegisterUiElement("Root.NormalLayer.rift_dialog_mainPage.LayoutRoot.button_openRift", null, null);
            _urshiUpgradeUi = Hud.Render.RegisterUiElement("Root.NormalLayer.vendor_dialog_mainPage.rift_reward_dialog", null, null);
        }

        /// <summary>
        /// Main update loop - called every frame
        /// </summary>
        public void Update()
        {
            if (Hud?.Game?.Me == null) return;
            
            // Update subsystems
            NavMesh.Update();
            Awareness.Update();
            
            // Update stats
            UpdatePositionTracking();
            
            // State machine
            switch (CurrentState)
            {
                case GRBotState.Idle:
                    // Do nothing, waiting for Start()
                    break;
                    
                case GRBotState.InTown:
                    HandleTownState();
                    break;
                    
                case GRBotState.OpeningRift:
                    HandleOpeningRiftState();
                    break;
                    
                case GRBotState.WaitingForPortal:
                    HandleWaitingForPortalState();
                    break;
                    
                case GRBotState.EnteringRift:
                    HandleEnteringRiftState();
                    break;
                    
                case GRBotState.InRift:
                    HandleInRiftState();
                    break;
                    
                case GRBotState.KillingTrash:
                case GRBotState.HuntingElites:
                    HandleCombatState();
                    break;
                    
                case GRBotState.MovingToObjective:
                    HandleMovingState();
                    break;
                    
                case GRBotState.WaitingForBoss:
                    HandleWaitingForBossState();
                    break;
                    
                case GRBotState.KillingBoss:
                    HandleKillingBossState();
                    break;
                    
                case GRBotState.CollectingLoot:
                    HandleCollectingLootState();
                    break;
                    
                case GRBotState.ExitingRift:
                    HandleExitingRiftState();
                    break;
                    
                case GRBotState.UpgradingGems:
                    HandleUpgradingGemsState();
                    break;
                    
                case GRBotState.Completed:
                    HandleCompletedState();
                    break;
                    
                case GRBotState.Error:
                case GRBotState.Paused:
                    // Wait for user intervention
                    break;
            }

            // Safety checks
            PerformSafetyChecks();
        }

        #region State Machine Methods

        private void HandleTownState()
        {
            // Check if we need to do town activities
            if (Config.AutoRepair && NeedsRepair())
            {
                SetObjective(ObjectiveType.Blacksmith, "Repair");
                return;
            }
            
            if (Config.AutoStash && InventoryNeedsStashing())
            {
                SetObjective(ObjectiveType.Stash, "Stash items");
                return;
            }
            
            // Ready to start GR - go to obelisk
            SetObjective(ObjectiveType.Obelisk, "Open GR");
            TransitionTo(GRBotState.OpeningRift);
        }

        private void HandleOpeningRiftState()
        {
            // Check if at obelisk
            var obelisk = FindNearestObelisk();
            if (obelisk == null)
            {
                // Navigate to obelisk
                // This would use known town coordinates
                return;
            }

            float dist = Hud.Game.Me.FloorCoordinate.XYDistanceTo(obelisk.FloorCoordinate);
            if (dist > 10)
            {
                // Move closer
                MoveToward(obelisk.FloorCoordinate);
                return;
            }

            // Interact with obelisk
            if (_obeliskUi != null && _obeliskUi.Visible)
            {
                // Dialog is open, click accept
                if (_riftAcceptUi != null && _riftAcceptUi.Visible)
                {
                    ClickUiElement(_riftAcceptUi);
                    TransitionTo(GRBotState.WaitingForPortal);
                }
            }
            else
            {
                // Open dialog
                InteractWith(obelisk);
            }
        }

        private void HandleWaitingForPortalState()
        {
            // Look for GR portal
            var portal = FindGRPortal();
            if (portal != null)
            {
                SetObjective(ObjectiveType.NextFloor, portal.FloorCoordinate, "Enter GR");
                TransitionTo(GRBotState.EnteringRift);
            }
            else if (_stateTimer.ElapsedMilliseconds > 10000)
            {
                // Timeout - something went wrong
                TransitionTo(GRBotState.Error);
                CurrentRun.FailureReason = "Portal did not spawn";
            }
        }

        private void HandleEnteringRiftState()
        {
            var portal = FindGRPortal();
            if (portal == null)
            {
                TransitionTo(GRBotState.WaitingForPortal);
                return;
            }

            float dist = Hud.Game.Me.FloorCoordinate.XYDistanceTo(portal.FloorCoordinate);
            
            if (dist > 8)
            {
                MoveToward(portal.FloorCoordinate);
            }
            else
            {
                // Close enough - interact with portal
                InteractWith(portal);
                
                // Check if we're in the rift now
                if (IsInGreaterRift())
                {
                    TransitionTo(GRBotState.InRift);
                    _pylonsThisFloor.Clear();
                }
            }
        }

        private void HandleInRiftState()
        {
            // Main rift decision loop
            UpdateObjectives();
            
            // Check rift completion
            if (Hud.Game.RiftPercentage >= 100 && !IsBossSpawned())
            {
                TransitionTo(GRBotState.WaitingForBoss);
                return;
            }
            
            if (IsBossSpawned())
            {
                TransitionTo(GRBotState.KillingBoss);
                return;
            }

            // Get next objective
            var objective = GetNextObjective();
            if (objective != null)
            {
                CurrentObjective = objective;
                
                switch (objective.Type)
                {
                    case ObjectiveType.Elite:
                    case ObjectiveType.TrashPack:
                        TransitionTo(GRBotState.HuntingElites);
                        break;
                    case ObjectiveType.Pylon:
                    case ObjectiveType.Shrine:
                        TransitionTo(GRBotState.MovingToObjective);
                        break;
                    default:
                        TransitionTo(GRBotState.MovingToObjective);
                        break;
                }
            }
            else
            {
                // No objectives - explore
                ExploreNewArea();
            }
        }

        private void HandleCombatState()
        {
            var myPos = Hud.Game.Me.FloorCoordinate;
            
            // Check if we should retreat
            if (Hud.Game.Me.Defense.HealthPct < Config.SafeHealthPercent)
            {
                var escapeRoute = Awareness.GetBestEscapeRoute();
                if (escapeRoute != null && !escapeRoute.IsBlocked)
                {
                    MoveToward(escapeRoute.TargetX, escapeRoute.TargetY);
                    return;
                }
            }

            // Find targets
            var targets = GetCombatTargets();
            if (!targets.Any())
            {
                // No more targets
                TransitionTo(GRBotState.InRift);
                return;
            }

            // Get primary target
            var primary = targets.First();
            float dist = myPos.XYDistanceTo(primary.FloorCoordinate);

            // Position for combat
            if (dist > Config.EngageDistance)
            {
                MoveToward(primary.FloorCoordinate);
            }
            else if (dist < Config.KiteDistance && !IsMelee())
            {
                // Too close for ranged - kite
                var escapeRoute = Awareness.GetBestEscapeRoute();
                if (escapeRoute != null)
                    MoveToward(escapeRoute.TargetX, escapeRoute.TargetY);
            }
            else
            {
                // In range - attack
                AttackTarget(primary);
            }
        }

        private void HandleMovingState()
        {
            if (CurrentObjective == null)
            {
                TransitionTo(GRBotState.InRift);
                return;
            }

            var myPos = Hud.Game.Me.FloorCoordinate;
            float dist = CurrentObjective.DistanceTo(myPos.X, myPos.Y);

            if (dist < 5)
            {
                // Reached objective
                HandleObjectiveReached();
            }
            else
            {
                // Continue moving
                NavigateToObjective();
            }
        }

        private void HandleWaitingForBossState()
        {
            if (IsBossSpawned())
            {
                TransitionTo(GRBotState.KillingBoss);
                return;
            }

            // Move to center or safe area while waiting
            var safeZone = Awareness.GetSafestPosition();
            if (safeZone != null)
            {
                MoveToward(safeZone.X, safeZone.Y);
            }
        }

        private void HandleKillingBossState()
        {
            var boss = GetRiftGuardian();
            if (boss == null || !boss.IsAlive)
            {
                CurrentRun.BossKilled = true;
                TransitionTo(GRBotState.CollectingLoot);
                return;
            }

            var myPos = Hud.Game.Me.FloorCoordinate;
            float dist = myPos.XYDistanceTo(boss.FloorCoordinate);

            // Use saved pylons if available
            if (Config.SavePylonsForBoss)
            {
                UseSavedPylon();
            }

            // Boss combat logic
            if (Hud.Game.Me.Defense.HealthPct < Config.SafeHealthPercent)
            {
                // Kite
                var escapeRoute = Awareness.GetBestEscapeRoute();
                if (escapeRoute != null)
                    MoveToward(escapeRoute.TargetX, escapeRoute.TargetY);
            }
            else if (dist > Config.EngageDistance)
            {
                MoveToward(boss.FloorCoordinate);
            }
            else
            {
                AttackTarget(boss);
            }
        }

        private void HandleCollectingLootState()
        {
            // Collect nearby loot
            var loot = GetNearbyLoot();
            if (loot.Any())
            {
                var item = loot.First();
                float dist = Hud.Game.Me.FloorCoordinate.XYDistanceTo(item.FloorCoordinate);
                
                if (dist > 5)
                    MoveToward(item.FloorCoordinate);
                else
                    PickupItem(item);
            }
            else
            {
                // Look for exit portal
                var exit = FindExitPortal();
                if (exit != null)
                {
                    SetObjective(ObjectiveType.ExitPortal, exit.FloorCoordinate, "Exit GR");
                    TransitionTo(GRBotState.ExitingRift);
                }
            }
        }

        private void HandleExitingRiftState()
        {
            var exit = FindExitPortal();
            if (exit == null)
            {
                // No exit yet - wait
                return;
            }

            float dist = Hud.Game.Me.FloorCoordinate.XYDistanceTo(exit.FloorCoordinate);
            
            if (dist > 8)
            {
                MoveToward(exit.FloorCoordinate);
            }
            else
            {
                InteractWith(exit);
                
                // Check if we're at Urshi
                if (IsAtUrshi())
                {
                    TransitionTo(GRBotState.UpgradingGems);
                }
            }
        }

        private void HandleUpgradingGemsState()
        {
            if (!Config.UpgradeGems)
            {
                TransitionTo(GRBotState.Completed);
                return;
            }

            // Check if upgrade dialog is open
            if (_urshiUpgradeUi != null && _urshiUpgradeUi.Visible)
            {
                // Select gem and upgrade
                // This would require more UI interaction logic
                CurrentRun.GemsUpgraded++;
                
                if (CurrentRun.GemsUpgraded >= Config.MaxGemUpgradeAttempts)
                {
                    TransitionTo(GRBotState.Completed);
                }
            }
            else
            {
                // Find and interact with Urshi
                var urshi = FindUrshi();
                if (urshi != null)
                {
                    float dist = Hud.Game.Me.FloorCoordinate.XYDistanceTo(urshi.FloorCoordinate);
                    if (dist > 8)
                        MoveToward(urshi.FloorCoordinate);
                    else
                        InteractWith(urshi);
                }
            }
        }

        private void HandleCompletedState()
        {
            // Run complete
            CurrentRun.EndTime = DateTime.Now;
            CurrentRun.Completed = true;
            CurrentRun.FinalState = GRBotState.Completed;
            
            SessionStats.AddRun(CurrentRun);
            
            // Start new run or stop
            if (Config.Enabled)
            {
                StartNewRun();
            }
            else
            {
                TransitionTo(GRBotState.Idle);
            }
        }

        #endregion

        #region Helper Methods

        private void TransitionTo(GRBotState newState)
        {
            CurrentState = newState;
            _stateTimer.Restart();
        }

        private void UpdateObjectives()
        {
            ObjectiveQueue.Clear();

            var myPos = Hud.Game.Me.FloorCoordinate;

            // Check for pylons
            foreach (var shrine in Hud.Game.Shrines.Where(s => !s.IsOperated))
            {
                if (_usedPylons.Contains(shrine.AnnId)) continue;
                
                bool isPylon = shrine.SnoActor.Sno.ToString().Contains("Pylon");
                bool shouldSave = Config.SavePylonsForBoss && 
                    Config.PylonsToSave.Any(p => shrine.SnoActor.NameLocalized.Contains(p));
                
                if (isPylon && shouldSave && Hud.Game.RiftPercentage < 95)
                {
                    _pylonsThisFloor.Add((shrine, true));
                    continue; // Save for boss
                }
                
                ObjectiveQueue.Enqueue(new NavObjective
                {
                    Type = isPylon ? ObjectiveType.Pylon : ObjectiveType.Shrine,
                    X = shrine.FloorCoordinate.X,
                    Y = shrine.FloorCoordinate.Y,
                    Priority = isPylon ? 90 : 70,
                    Target = shrine
                });
            }

            // Check for elites
            foreach (var elite in Hud.Game.AliveMonsters.Where(m => 
                m.Rarity == ActorRarity.Champion || 
                m.Rarity == ActorRarity.Rare || 
                m.Rarity == ActorRarity.Boss))
            {
                float dist = myPos.XYDistanceTo(elite.FloorCoordinate);
                if (dist > Config.EliteSearchRadius) continue;

                ObjectiveQueue.Enqueue(new NavObjective
                {
                    Type = ObjectiveType.Elite,
                    X = elite.FloorCoordinate.X,
                    Y = elite.FloorCoordinate.Y,
                    Priority = 80 - (dist / Config.EliteSearchRadius * 20),
                    Target = elite
                });
            }

            // Check for trash packs
            if (!Config.SkipTrashPacks)
            {
                var density = Awareness.MonsterDensity.GetHighestDensity();
                if (density.density > Config.MinMobsToEngage)
                {
                    ObjectiveQueue.Enqueue(new NavObjective
                    {
                        Type = ObjectiveType.TrashPack,
                        X = density.x,
                        Y = density.y,
                        Priority = 50
                    });
                }
            }
        }

        private NavObjective GetNextObjective()
        {
            if (ObjectiveQueue.Count == 0) return null;
            
            // Sort by priority and return highest
            var sorted = ObjectiveQueue.OrderByDescending(o => o.Priority).ToList();
            ObjectiveQueue.Clear();
            foreach (var obj in sorted.Skip(1))
                ObjectiveQueue.Enqueue(obj);
            
            return sorted.First();
        }

        private void HandleObjectiveReached()
        {
            if (CurrentObjective == null) return;

            switch (CurrentObjective.Type)
            {
                case ObjectiveType.Pylon:
                case ObjectiveType.Shrine:
                    InteractWith(CurrentObjective.Target as IShrine);
                    _usedPylons.Add((CurrentObjective.Target as IShrine).AnnId);
                    CurrentRun.PylonsUsed++;
                    break;
                    
                case ObjectiveType.Elite:
                case ObjectiveType.TrashPack:
                    TransitionTo(GRBotState.KillingTrash);
                    return; // Don't clear objective yet
            }

            CurrentObjective.Completed = true;
            CurrentObjective = null;
            TransitionTo(GRBotState.InRift);
        }

        private void NavigateToObjective()
        {
            if (CurrentObjective == null) return;

            var myPos = Hud.Game.Me.FloorCoordinate;
            
            // Recalculate path periodically
            if (CurrentPath == null || !CurrentPath.IsValid || 
                _pathRecalcTimer.ElapsedMilliseconds > Config.PathRecalcIntervalMs)
            {
                CurrentPath = Awareness.Pathfinder.FindSafePath(
                    myPos.X, myPos.Y,
                    CurrentObjective.X, CurrentObjective.Y
                );
                PathWaypointIndex = 0;
                _pathRecalcTimer.Restart();
                
                if (CurrentPath.IsValid)
                    CurrentRun.PathsCalculated++;
            }

            if (CurrentPath == null || !CurrentPath.IsValid)
            {
                // Direct movement if no path
                MoveToward(CurrentObjective.X, CurrentObjective.Y);
                return;
            }

            // Follow path
            if (PathWaypointIndex < CurrentPath.Path.Count)
            {
                var wp = CurrentPath.Path[PathWaypointIndex];
                float dist = (float)Math.Sqrt(
                    Math.Pow(myPos.X - wp.WorldX, 2) + 
                    Math.Pow(myPos.Y - wp.WorldY, 2));
                
                if (dist < 3)
                {
                    PathWaypointIndex++;
                }
                else
                {
                    MoveToward(wp.WorldX, wp.WorldY);
                }
            }
        }

        private void ExploreNewArea()
        {
            // Move toward unexplored areas
            var myPos = Hud.Game.Me.FloorCoordinate;
            var scene = Hud.Game.Me.Scene;
            if (scene == null) return;

            // Find direction with most unexplored space
            float bestX = myPos.X + 30; // Default: move right
            float bestY = myPos.Y;
            
            // Check scene boundaries and move toward center if near edge
            float distToLeft = myPos.X - scene.PosX;
            float distToRight = scene.MaxX - myPos.X;
            float distToBottom = myPos.Y - scene.PosY;
            float distToTop = scene.MaxY - myPos.Y;

            if (distToRight > distToLeft)
                bestX = myPos.X + 30;
            else
                bestX = myPos.X - 30;
                
            if (distToTop > distToBottom)
                bestY = myPos.Y + 30;
            else
                bestY = myPos.Y - 30;

            SetObjective(ObjectiveType.None, bestX, bestY, "Explore");
            TransitionTo(GRBotState.MovingToObjective);
        }

        private void SetObjective(ObjectiveType type, string reason)
        {
            // For town objectives, use known locations
        }

        private void SetObjective(ObjectiveType type, IWorldCoordinate pos, string reason)
        {
            CurrentObjective = new NavObjective
            {
                Type = type,
                X = pos.X,
                Y = pos.Y,
                Z = pos.Z,
                Priority = 100
            };
        }

        private void SetObjective(ObjectiveType type, float x, float y, string reason)
        {
            CurrentObjective = new NavObjective
            {
                Type = type,
                X = x,
                Y = y,
                Priority = 100
            };
        }

        private void UpdatePositionTracking()
        {
            var myPos = Hud.Game.Me.FloorCoordinate;
            
            if (_lastPosition != null)
            {
                float moved = _lastPosition.XYDistanceTo(myPos);
                CurrentRun.DistanceTraveled += moved;
                _distanceSinceLastCheck += moved;
                
                // Stuck detection
                if (_distanceSinceLastCheck < 1f && _stuckTimer.ElapsedMilliseconds > 2000)
                {
                    CurrentRun.TimesStuck++;
                    _stuckTimer.Restart();
                    
                    if (Config.PauseIfStuck && _stuckTimer.ElapsedMilliseconds > Config.StuckTimeoutSeconds * 1000)
                    {
                        TransitionTo(GRBotState.Error);
                        CurrentRun.FailureReason = "Stuck for too long";
                    }
                }
                else if (_distanceSinceLastCheck > 5f)
                {
                    _distanceSinceLastCheck = 0;
                    _stuckTimer.Restart();
                }
            }
            
            _lastPosition = Hud.Window.CreateWorldCoordinate(myPos.X, myPos.Y, myPos.Z);
        }

        private void PerformSafetyChecks()
        {
            if (!Config.Enabled) return;

            // Check deaths
            if (Hud.Game.Me.IsDead)
            {
                _deathsThisRun++;
                CurrentRun.Deaths++;
                
                if (_deathsThisRun >= Config.MaxDeaths)
                {
                    TransitionTo(GRBotState.Error);
                    CurrentRun.FailureReason = $"Too many deaths ({_deathsThisRun})";
                }
            }

            // Check run time
            if (CurrentRun.Duration.TotalMinutes > Config.MaxRunTimeMinutes)
            {
                TransitionTo(GRBotState.Error);
                CurrentRun.FailureReason = "Run timed out";
            }

            // Low health pause
            if (Config.PauseOnLowHealth && 
                Hud.Game.Me.Defense.HealthPct < Config.PauseHealthThreshold)
            {
                // Don't transition to paused, just log
            }
        }

        #endregion

        #region Game Interaction Stubs

        private void MoveToward(IWorldCoordinate target)
        {
            MoveToward(target.X, target.Y);
        }

        private void MoveToward(float x, float y)
        {
            var target = Hud.Window.CreateWorldCoordinate(x, y, Hud.Game.Me.FloorCoordinate.Z);
            var screen = target.ToScreenCoordinate();
            
            if (screen.X > 10 && screen.X < Hud.Window.Size.Width - 10 &&
                screen.Y > 10 && screen.Y < Hud.Window.Size.Height - 10)
            {
                Hud.Interaction.MouseMove((int)screen.X, (int)screen.Y, 1, 1);
                Hud.Interaction.MouseDown(System.Windows.Forms.MouseButtons.Left);
                Hud.Wait(3);
                Hud.Interaction.MouseUp(System.Windows.Forms.MouseButtons.Left);
            }
        }

        private void InteractWith(IActor actor)
        {
            // Implementation would click on the actor
        }

        private void InteractWith(IShrine shrine)
        {
            // Implementation would click on the shrine
        }

        private void AttackTarget(IMonster target)
        {
            // Implementation would use skills on target
        }

        private void PickupItem(IItem item)
        {
            // Implementation would click item
        }

        private void ClickUiElement(IUiElement element)
        {
            if (element == null || !element.Visible) return;
            var rect = element.Rectangle;
            int x = (int)(rect.X + rect.Width / 2);
            int y = (int)(rect.Y + rect.Height / 2);
            Hud.Interaction.MouseMove(x, y, 1, 1);
            Hud.Interaction.MouseDown(System.Windows.Forms.MouseButtons.Left);
            Hud.Wait(10);
            Hud.Interaction.MouseUp(System.Windows.Forms.MouseButtons.Left);
        }

        private void UseSavedPylon()
        {
            // Use pylons saved for boss
            var saved = _pylonsThisFloor.FirstOrDefault(p => p.saved && !p.shrine.IsOperated);
            if (saved.shrine != null)
            {
                float dist = Hud.Game.Me.FloorCoordinate.XYDistanceTo(saved.shrine.FloorCoordinate);
                if (dist < 10)
                    InteractWith(saved.shrine);
            }
        }

        #endregion

        #region Query Methods

        private IEnumerable<IMonster> GetCombatTargets()
        {
            var myPos = Hud.Game.Me.FloorCoordinate;
            
            return Hud.Game.AliveMonsters
                .Where(m => myPos.XYDistanceTo(m.FloorCoordinate) < Config.EngageDistance * 2)
                .OrderByDescending(m => m.Rarity)
                .ThenBy(m => myPos.XYDistanceTo(m.FloorCoordinate));
        }

        private IEnumerable<IItem> GetNearbyLoot()
        {
            var myPos = Hud.Game.Me.FloorCoordinate;
            
            return Hud.Game.Items.Where(i => 
                i.Location == ItemLocation.Floor &&
                myPos.XYDistanceTo(i.FloorCoordinate) < 30 &&
                ShouldPickup(i));
        }

        private bool ShouldPickup(IItem item)
        {
            // Check loot filter
            if (item.IsLegendary)
            {
                if (item.AncientRank > 0 && Config.PickupPrimals) return true;
                if (item.AncientRank == 1 && Config.PickupAncients) return true;
                if (Config.PickupLegendaries) return true;
            }
            
            // Add more loot rules
            return false;
        }

        private IActor FindNearestObelisk()
        {
            return Hud.Game.Actors
                .Where(a => a.SnoActor.NameLocalized.Contains("Nephalem Obelisk") ||
                           a.SnoActor.NameLocalized.Contains("Greater Rift"))
                .OrderBy(a => Hud.Game.Me.FloorCoordinate.XYDistanceTo(a.FloorCoordinate))
                .FirstOrDefault();
        }

        private IPortal FindGRPortal()
        {
            return Hud.Game.Portals
                .Where(p => p.SnoActor.NameLocalized.Contains("Greater Rift"))
                .FirstOrDefault();
        }

        private IPortal FindExitPortal()
        {
            return Hud.Game.Portals
                .Where(p => p.SnoActor.NameLocalized.Contains("Exit"))
                .FirstOrDefault();
        }

        private IActor FindUrshi()
        {
            return Hud.Game.Actors
                .Where(a => a.SnoActor.NameLocalized.Contains("Urshi"))
                .FirstOrDefault();
        }

        private IMonster GetRiftGuardian()
        {
            return Hud.Game.AliveMonsters
                .FirstOrDefault(m => m.Rarity == ActorRarity.Boss);
        }

        private bool IsInGreaterRift() => Hud.Game.Me.InGreaterRift;
        private bool IsBossSpawned() => GetRiftGuardian() != null;
        private bool IsAtUrshi() => FindUrshi() != null;
        private bool NeedsRepair() => false; // TODO: Implement
        private bool InventoryNeedsStashing() => false; // TODO: Implement
        private bool IsMelee() => false; // TODO: Check class/build

        #endregion

        #region Public Control Methods

        public void Start()
        {
            if (CurrentState != GRBotState.Idle && CurrentState != GRBotState.Paused)
                return;
            
            StartNewRun();
        }

        public void Stop()
        {
            Config.Enabled = false;
            
            if (CurrentRun != null)
            {
                CurrentRun.EndTime = DateTime.Now;
                CurrentRun.FinalState = CurrentState;
                SessionStats.AddRun(CurrentRun);
            }
            
            TransitionTo(GRBotState.Idle);
        }

        public void Pause()
        {
            TransitionTo(GRBotState.Paused);
        }

        public void Resume()
        {
            if (CurrentState == GRBotState.Paused)
                TransitionTo(GRBotState.InRift); // Or previous state
        }

        private void StartNewRun()
        {
            CurrentRun = new GRRunStats
            {
                StartTime = DateTime.Now,
                GRLevel = Config.TargetGRLevel
            };
            
            _deathsThisRun = 0;
            _usedPylons.Clear();
            _pylonsThisFloor.Clear();
            ObjectiveQueue.Clear();
            CurrentObjective = null;
            CurrentPath = null;
            
            if (Hud.Game.IsInTown)
                TransitionTo(GRBotState.InTown);
            else
                TransitionTo(GRBotState.InRift);
        }

        #endregion
    }
}
