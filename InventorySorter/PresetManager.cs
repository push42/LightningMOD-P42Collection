namespace Turbo.Plugins.Custom.InventorySorter
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Stash organization presets based on popular community layouts
    /// </summary>
    public enum StashPreset
    {
        /// <summary>
        /// Custom user-defined rules
        /// </summary>
        Custom,

        /// <summary>
        /// Speed Farmer - Fastest loop after GRs, minimal decisions
        /// Tab 1: Active builds, Tab 2: Keep candidates, Tab 3: Jewelry, Tab 4: Keys/Consumables, Tab 5: Gems
        /// </summary>
        SpeedFarmer,

        /// <summary>
        /// One tab per build - Everything for a build together
        /// Great for multi-class players
        /// </summary>
        PerBuild,

        /// <summary>
        /// Collector - One best copy of everything, organized by type
        /// Universal → Class Sets → Weapons → Jewelry → Trophies
        /// </summary>
        Collector,

        /// <summary>
        /// Minimalist - Keep only what you use, ruthless salvage rules
        /// </summary>
        Minimalist,

        /// <summary>
        /// Gem Focused - Color blocks with use-case rows
        /// </summary>
        GemOrganizer
    }

    /// <summary>
    /// Zone types within a stash tab
    /// </summary>
    public enum StashZoneType
    {
        /// <summary>
        /// Items you grab often (keys, common swaps)
        /// </summary>
        GrabOften,

        /// <summary>
        /// High value items (jewelry, primals, near-perfect ancients)
        /// </summary>
        HighValue,

        /// <summary>
        /// Main category items (set pieces, weapons, gems)
        /// </summary>
        MainCategory,

        /// <summary>
        /// Run starters (Puzzle Rings, Screams)
        /// </summary>
        RunStarters,

        /// <summary>
        /// Items to decide on later or roll
        /// </summary>
        ToDecide,

        /// <summary>
        /// Overflow / temporary
        /// </summary>
        Overflow
    }

    /// <summary>
    /// A zone definition within the stash grid
    /// </summary>
    public class StashZone
    {
        public string Name { get; set; }
        public StashZoneType ZoneType { get; set; }
        public int StartX { get; set; }
        public int StartY { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public List<ItemCategory> AllowedCategories { get; set; } = new List<ItemCategory>();
        public int Priority { get; set; } = 0; // Lower = higher priority

        public bool ContainsPoint(int x, int y)
        {
            return x >= StartX && x < StartX + Width && y >= StartY && y < StartY + Height;
        }

        public bool CanFit(int itemWidth, int itemHeight)
        {
            return itemWidth <= Width && itemHeight <= Height;
        }
    }

    /// <summary>
    /// Tab configuration for a stash preset
    /// </summary>
    public class StashTabConfig
    {
        public int TabIndex { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<StashZone> Zones { get; set; } = new List<StashZone>();
        public List<ItemCategory> PrimaryCategories { get; set; } = new List<ItemCategory>();
        public bool IsEnabled { get; set; } = true;
    }

    /// <summary>
    /// Complete preset configuration
    /// </summary>
    public class PresetConfiguration
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public StashPreset PresetType { get; set; }
        public List<StashTabConfig> TabConfigs { get; set; } = new List<StashTabConfig>();
        public bool IsBuiltIn { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Sorting preferences
        public SortMode DefaultSortMode { get; set; } = SortMode.ByCategory;
        public bool SortByQualityFirst { get; set; } = true;
        public bool GroupBySet { get; set; } = true;
        public bool KeepGemsGroupedByColor { get; set; } = true;
    }

    /// <summary>
    /// Manages presets and configurations
    /// </summary>
    public class PresetManager
    {
        public Dictionary<string, PresetConfiguration> Presets { get; private set; }
        public string ActivePresetId { get; set; }
        public string DataDirectory { get; set; }

        private const string PresetsFileName = "InventorySorter_Presets.txt";

        public PresetManager()
        {
            Presets = new Dictionary<string, PresetConfiguration>();
        }

        public void Initialize()
        {
            CreateBuiltInPresets();
            LoadFromFile();

            if (string.IsNullOrEmpty(ActivePresetId) || !Presets.ContainsKey(ActivePresetId))
            {
                ActivePresetId = "speedfarmer";
            }
        }

        public PresetConfiguration GetActivePreset()
        {
            if (Presets.TryGetValue(ActivePresetId, out var preset))
                return preset;
            return Presets.Values.FirstOrDefault();
        }

        public void SetActivePreset(string presetId)
        {
            if (Presets.ContainsKey(presetId))
            {
                ActivePresetId = presetId;
                SaveToFile();
            }
        }

        private void CreateBuiltInPresets()
        {
            // === SPEED FARMER PRESET ===
            var speedFarmer = new PresetConfiguration
            {
                Id = "speedfarmer",
                Name = "Speed Farmer",
                Description = "Fastest loop after GRs. Minimal decision fatigue.",
                PresetType = StashPreset.SpeedFarmer,
                IsBuiltIn = true,
                DefaultSortMode = SortMode.ByCategory,
                SortByQualityFirst = true
            };

            // Tab 1: Active Builds
            speedFarmer.TabConfigs.Add(new StashTabConfig
            {
                TabIndex = 0,
                Name = "Active Builds",
                Description = "1-2 full builds you actually use",
                PrimaryCategories = new List<ItemCategory>
                {
                    ItemCategory.PrimalAncientWeapon, ItemCategory.PrimalAncientArmor, ItemCategory.PrimalAncientJewelry,
                    ItemCategory.AncientWeapon, ItemCategory.AncientArmor, ItemCategory.AncientJewelry,
                    ItemCategory.SetWeapon, ItemCategory.SetArmor, ItemCategory.SetJewelry
                },
                Zones = new List<StashZone>
                {
                    new StashZone { Name = "Weapons", ZoneType = StashZoneType.MainCategory, StartX = 0, StartY = 0, Width = 2, Height = 10, Priority = 1 },
                    new StashZone { Name = "Armor", ZoneType = StashZoneType.MainCategory, StartX = 2, StartY = 0, Width = 3, Height = 10, Priority = 2 },
                    new StashZone { Name = "Jewelry", ZoneType = StashZoneType.HighValue, StartX = 5, StartY = 0, Width = 2, Height = 5, Priority = 3 },
                    new StashZone { Name = "Swaps", ZoneType = StashZoneType.GrabOften, StartX = 5, StartY = 5, Width = 2, Height = 5, Priority = 4 }
                }
            });

            // Tab 2: Keep Candidates
            speedFarmer.TabConfigs.Add(new StashTabConfig
            {
                TabIndex = 1,
                Name = "Keep Candidates",
                Description = "Items to evaluate - salvage if sitting >2 sessions",
                PrimaryCategories = new List<ItemCategory>
                {
                    ItemCategory.LegendaryWeapon, ItemCategory.LegendaryArmor, ItemCategory.LegendaryJewelry
                },
                Zones = new List<StashZone>
                {
                    new StashZone { Name = "Evaluate", ZoneType = StashZoneType.ToDecide, StartX = 0, StartY = 0, Width = 7, Height = 10, Priority = 1 }
                }
            });

            // Tab 3: Jewelry
            speedFarmer.TabConfigs.Add(new StashTabConfig
            {
                TabIndex = 2,
                Name = "Jewelry",
                Description = "Rings and amulets - hardest to replace!",
                PrimaryCategories = new List<ItemCategory>
                {
                    ItemCategory.PrimalAncientJewelry, ItemCategory.AncientJewelry, 
                    ItemCategory.SetJewelry, ItemCategory.LegendaryJewelry
                },
                Zones = new List<StashZone>
                {
                    new StashZone { Name = "Damage Rings", ZoneType = StashZoneType.HighValue, StartX = 0, StartY = 0, Width = 3, Height = 5, Priority = 1 },
                    new StashZone { Name = "Utility Rings", ZoneType = StashZoneType.MainCategory, StartX = 3, StartY = 0, Width = 2, Height = 5, Priority = 2 },
                    new StashZone { Name = "Support Rings", ZoneType = StashZoneType.MainCategory, StartX = 5, StartY = 0, Width = 2, Height = 5, Priority = 3 },
                    new StashZone { Name = "Amulets", ZoneType = StashZoneType.HighValue, StartX = 0, StartY = 5, Width = 7, Height = 5, Priority = 4 }
                }
            });

            // Tab 4: Keys & Consumables
            speedFarmer.TabConfigs.Add(new StashTabConfig
            {
                TabIndex = 3,
                Name = "Keys & Consumables",
                Description = "GR keys, Puzzle Rings, Ramaladni's, etc.",
                PrimaryCategories = new List<ItemCategory>
                {
                    ItemCategory.KeystoneFragment, ItemCategory.UberKey, 
                    ItemCategory.RamaladnisGift, ItemCategory.Consumable
                },
                Zones = new List<StashZone>
                {
                    new StashZone { Name = "GR Keys", ZoneType = StashZoneType.GrabOften, StartX = 0, StartY = 0, Width = 2, Height = 3, Priority = 1 },
                    new StashZone { Name = "Run Starters", ZoneType = StashZoneType.RunStarters, StartX = 0, StartY = 7, Width = 2, Height = 3, Priority = 2 },
                    new StashZone { Name = "Consumables", ZoneType = StashZoneType.MainCategory, StartX = 2, StartY = 0, Width = 5, Height = 10, Priority = 3 }
                }
            });

            // Tab 5: Gems
            speedFarmer.TabConfigs.Add(new StashTabConfig
            {
                TabIndex = 4,
                Name = "Gems",
                Description = "Top-tier gems by color",
                PrimaryCategories = new List<ItemCategory>
                {
                    ItemCategory.LegendaryGem, ItemCategory.FlawlessRoyalGem
                },
                Zones = new List<StashZone>
                {
                    new StashZone { Name = "Legendary Gems", ZoneType = StashZoneType.HighValue, StartX = 0, StartY = 0, Width = 7, Height = 3, Priority = 1 },
                    new StashZone { Name = "Ruby", ZoneType = StashZoneType.MainCategory, StartX = 0, StartY = 3, Width = 1, Height = 7, Priority = 2 },
                    new StashZone { Name = "Emerald", ZoneType = StashZoneType.MainCategory, StartX = 1, StartY = 3, Width = 1, Height = 7, Priority = 3 },
                    new StashZone { Name = "Topaz", ZoneType = StashZoneType.MainCategory, StartX = 2, StartY = 3, Width = 1, Height = 7, Priority = 4 },
                    new StashZone { Name = "Amethyst", ZoneType = StashZoneType.MainCategory, StartX = 3, StartY = 3, Width = 1, Height = 7, Priority = 5 },
                    new StashZone { Name = "Diamond", ZoneType = StashZoneType.MainCategory, StartX = 4, StartY = 3, Width = 1, Height = 7, Priority = 6 },
                    new StashZone { Name = "Overflow", ZoneType = StashZoneType.Overflow, StartX = 5, StartY = 3, Width = 2, Height = 7, Priority = 7 }
                }
            });

            Presets["speedfarmer"] = speedFarmer;

            // === COLLECTOR PRESET ===
            var collector = new PresetConfiguration
            {
                Id = "collector",
                Name = "Collector",
                Description = "Keep one best copy of everything. Organized by type.",
                PresetType = StashPreset.Collector,
                IsBuiltIn = true,
                DefaultSortMode = SortMode.ByType,
                GroupBySet = true
            };

            collector.TabConfigs.Add(new StashTabConfig
            {
                TabIndex = 0,
                Name = "Universal Items",
                Description = "Items used across multiple builds",
                PrimaryCategories = new List<ItemCategory>
                {
                    ItemCategory.LegendaryJewelry, ItemCategory.AncientJewelry
                }
            });

            collector.TabConfigs.Add(new StashTabConfig
            {
                TabIndex = 1,
                Name = "Class Sets",
                Description = "Set items organized by set"
            });

            collector.TabConfigs.Add(new StashTabConfig
            {
                TabIndex = 2,
                Name = "Weapons",
                Description = "All weapon types"
            });

            collector.TabConfigs.Add(new StashTabConfig
            {
                TabIndex = 3,
                Name = "Armor",
                Description = "Non-set armor pieces"
            });

            collector.TabConfigs.Add(new StashTabConfig
            {
                TabIndex = 4,
                Name = "Trophies",
                Description = "Primals, perfect rolls, sentimental items"
            });

            Presets["collector"] = collector;

            // === MINIMALIST PRESET ===
            var minimalist = new PresetConfiguration
            {
                Id = "minimalist",
                Name = "Minimalist",
                Description = "Keep only what you actively use. Ruthless salvaging.",
                PresetType = StashPreset.Minimalist,
                IsBuiltIn = true,
                DefaultSortMode = SortMode.ByQuality
            };

            minimalist.TabConfigs.Add(new StashTabConfig
            {
                TabIndex = 0,
                Name = "Current Build",
                Description = "Your one active build"
            });

            minimalist.TabConfigs.Add(new StashTabConfig
            {
                TabIndex = 1,
                Name = "Essentials",
                Description = "Keys, consumables, essential gems only"
            });

            Presets["minimalist"] = minimalist;

            // === CUSTOM PRESET (empty template) ===
            var custom = new PresetConfiguration
            {
                Id = "custom",
                Name = "Custom",
                Description = "Your own organization rules",
                PresetType = StashPreset.Custom,
                IsBuiltIn = true,
                DefaultSortMode = SortMode.ByCategory
            };

            Presets["custom"] = custom;
        }

        public void SaveToFile()
        {
            if (string.IsNullOrEmpty(DataDirectory)) return;

            try
            {
                var filePath = Path.Combine(DataDirectory, PresetsFileName);
                var lines = new List<string>
                {
                    "# InventorySorter Presets Configuration",
                    "# Auto-generated - DO NOT EDIT MANUALLY",
                    "",
                    $"ActivePreset={ActivePresetId}",
                    ""
                };

                // Save custom presets
                foreach (var preset in Presets.Values.Where(p => !p.IsBuiltIn))
                {
                    lines.Add($"[Preset:{preset.Id}]");
                    lines.Add($"Name={preset.Name}");
                    lines.Add($"Description={preset.Description}");
                    lines.Add($"SortMode={preset.DefaultSortMode}");
                    lines.Add("");
                }

                File.WriteAllLines(filePath, lines);
            }
            catch { }
        }

        public void LoadFromFile()
        {
            if (string.IsNullOrEmpty(DataDirectory)) return;

            try
            {
                var filePath = Path.Combine(DataDirectory, PresetsFileName);
                if (!File.Exists(filePath)) return;

                var lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    if (line.StartsWith("ActivePreset="))
                    {
                        ActivePresetId = line.Substring("ActivePreset=".Length).Trim();
                    }
                }
            }
            catch { }
        }

        public List<PresetConfiguration> GetAllPresets()
        {
            return Presets.Values.OrderBy(p => p.IsBuiltIn ? 0 : 1).ThenBy(p => p.Name).ToList();
        }
    }
}
