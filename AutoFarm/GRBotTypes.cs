namespace Turbo.Plugins.Custom.AutoFarm
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// GR Bot State Machine
    /// Manages the overall state of the GR farming bot
    /// </summary>
    public enum GRBotState
    {
        Idle,               // Waiting for user to start
        InTown,             // In town, preparing
        OpeningRift,        // Opening GR portal
        WaitingForPortal,   // Portal is spawning
        EnteringRift,       // Clicking portal to enter
        InRift,             // Inside GR, farming
        KillingTrash,       // Killing regular mobs
        HuntingElites,      // Seeking elite packs
        MovingToObjective,  // Pathfinding to next objective
        CollectingProgress, // Picking up progress orbs
        WaitingForBoss,     // Rift guardian spawning
        KillingBoss,        // Fighting RG
        CollectingLoot,     // Picking up drops
        ExitingRift,        // Leaving through portal
        UpgradingGems,      // At Urshi upgrading gems
        Completed,          // GR complete
        Error,              // Something went wrong
        Paused              // User paused
    }

    /// <summary>
    /// GR Bot Configuration
    /// All settings for the auto-farm bot
    /// </summary>
    public class GRBotConfig
    {
        // === GENERAL ===
        public bool Enabled { get; set; } = false;
        public int TargetGRLevel { get; set; } = 100;
        public int MaxDeaths { get; set; } = 3;
        public int MaxRunTimeMinutes { get; set; } = 15;
        
        // === TOWN BEHAVIOR ===
        public bool AutoSalvage { get; set; } = true;
        public bool AutoStash { get; set; } = true;
        public bool AutoRepair { get; set; } = true;
        public bool AutoIdentify { get; set; } = true;
        
        // === RIFT BEHAVIOR ===
        public bool SkipTrashPacks { get; set; } = false;
        public int MinMobsToEngage { get; set; } = 5;
        public float EliteSearchRadius { get; set; } = 80f;
        public bool PrioritizeElites { get; set; } = true;
        public bool PrioritizePylons { get; set; } = true;
        
        // === COMBAT ===
        public float KiteDistance { get; set; } = 15f;
        public float EngageDistance { get; set; } = 40f;
        public float SafeHealthPercent { get; set; } = 50f;
        public bool UseDefensivesAutomatically { get; set; } = true;
        
        // === BOSS ===
        public bool SavePylonsForBoss { get; set; } = true;
        public string[] PylonsToSave { get; set; } = { "Power", "Conduit", "Channeling" };
        
        // === GEM UPGRADES ===
        public bool UpgradeGems { get; set; } = true;
        public int MaxGemUpgradeAttempts { get; set; } = 3;
        public string[] GemsToUpgrade { get; set; } = { "Bane of the Trapped", "Bane of the Stricken", "Zei's Stone of Vengeance" };
        public bool EmpowerRifts { get; set; } = true;
        
        // === MOVEMENT ===
        public float MovementSpeed { get; set; } = 1.0f;
        public bool UseMovementSkills { get; set; } = true;
        public int PathRecalcIntervalMs { get; set; } = 500;
        
        // === SAFETY ===
        public bool PauseOnLowHealth { get; set; } = true;
        public float PauseHealthThreshold { get; set; } = 25f;
        public bool PauseIfStuck { get; set; } = true;
        public int StuckTimeoutSeconds { get; set; } = 10;
        
        // === LOOT ===
        public bool PickupDeathsBreath { get; set; } = true;
        public bool PickupLegendaries { get; set; } = true;
        public bool PickupPrimals { get; set; } = true;
        public bool PickupAncients { get; set; } = true;
        public bool PickupGems { get; set; } = true;
        public bool PickupCraftingMats { get; set; } = true;
        
        // === DEBUG ===
        public bool ShowDebugOverlay { get; set; } = true;
        public bool VerboseLogging { get; set; } = false;
    }

    /// <summary>
    /// GR Run Statistics
    /// Tracks performance metrics for the current run
    /// </summary>
    public class GRRunStats
    {
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan Duration => EndTime.HasValue ? EndTime.Value - StartTime : DateTime.Now - StartTime;
        
        public int GRLevel { get; set; }
        public int ElitesKilled { get; set; }
        public int TrashKilled { get; set; }
        public int Deaths { get; set; }
        public int GemsUpgraded { get; set; }
        
        public double ProgressPercent { get; set; }
        public bool BossKilled { get; set; }
        public bool Completed { get; set; }
        
        public int LegendariesFound { get; set; }
        public int AncientsFound { get; set; }
        public int PrimalsFound { get; set; }
        public int DeathsBreathCollected { get; set; }
        
        public int PylonsUsed { get; set; }
        public int ShrinesTouched { get; set; }
        
        public float DistanceTraveled { get; set; }
        public int PathsCalculated { get; set; }
        public int TimesStuck { get; set; }
        
        public GRBotState FinalState { get; set; }
        public string FailureReason { get; set; }
    }

    /// <summary>
    /// Session Statistics
    /// Aggregate stats across multiple GR runs
    /// </summary>
    public class GRSessionStats
    {
        public DateTime SessionStart { get; set; } = DateTime.Now;
        public int TotalRuns { get; set; }
        public int SuccessfulRuns { get; set; }
        public int FailedRuns { get; set; }
        
        public TimeSpan TotalPlayTime { get; set; }
        public TimeSpan AverageRunTime => TotalRuns > 0 ? TimeSpan.FromTicks(TotalPlayTime.Ticks / TotalRuns) : TimeSpan.Zero;
        public TimeSpan FastestRun { get; set; } = TimeSpan.MaxValue;
        public TimeSpan SlowestRun { get; set; } = TimeSpan.Zero;
        
        public int TotalElitesKilled { get; set; }
        public int TotalDeaths { get; set; }
        public int TotalLegendaries { get; set; }
        public int TotalAncients { get; set; }
        public int TotalPrimals { get; set; }
        public int TotalDeathsBreath { get; set; }
        public int TotalGemsUpgraded { get; set; }
        
        public float RunsPerHour => TotalPlayTime.TotalHours > 0 ? (float)(TotalRuns / TotalPlayTime.TotalHours) : 0;
        public float SuccessRate => TotalRuns > 0 ? (float)SuccessfulRuns / TotalRuns * 100 : 0;
        
        public List<GRRunStats> RunHistory { get; set; } = new List<GRRunStats>();
        
        public void AddRun(GRRunStats run)
        {
            RunHistory.Add(run);
            TotalRuns++;
            
            if (run.Completed)
                SuccessfulRuns++;
            else
                FailedRuns++;
            
            TotalPlayTime += run.Duration;
            
            if (run.Duration < FastestRun && run.Completed)
                FastestRun = run.Duration;
            if (run.Duration > SlowestRun)
                SlowestRun = run.Duration;
            
            TotalElitesKilled += run.ElitesKilled;
            TotalDeaths += run.Deaths;
            TotalLegendaries += run.LegendariesFound;
            TotalAncients += run.AncientsFound;
            TotalPrimals += run.PrimalsFound;
            TotalDeathsBreath += run.DeathsBreathCollected;
            TotalGemsUpgraded += run.GemsUpgraded;
        }
    }

    /// <summary>
    /// Objective Types for navigation
    /// </summary>
    public enum ObjectiveType
    {
        None,
        Elite,
        TrashPack,
        Pylon,
        Shrine,
        ProgressOrb,
        RiftGuardian,
        ExitPortal,
        LootItem,
        NextFloor,
        Obelisk,      // GR obelisk in town
        Urshi,        // Gem upgrader
        Blacksmith,
        Stash
    }

    /// <summary>
    /// Navigation Objective
    /// </summary>
    public class NavObjective
    {
        public ObjectiveType Type { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Priority { get; set; }
        public object Target { get; set; } // IMonster, IShrine, IItem, etc.
        public bool Completed { get; set; }
        
        public float DistanceTo(float px, float py)
        {
            return (float)Math.Sqrt(Math.Pow(X - px, 2) + Math.Pow(Y - py, 2));
        }
    }
}
