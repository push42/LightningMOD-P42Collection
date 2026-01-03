namespace Turbo.Plugins.Custom.SmartSalvage
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Manages blacklist profiles: storage, import/export, and active blacklist compilation
    /// </summary>
    public class BlacklistManager
    {
        #region Constants

        private const string ProfilesFileName = "SmartSalvage_Profiles.txt";
        private const string SettingsFileName = "SmartSalvage_Settings.txt";

        #endregion

        #region Properties

        /// <summary>
        /// All registered profiles
        /// </summary>
        public Dictionary<string, BlacklistProfile> Profiles { get; private set; }

        /// <summary>
        /// Currently active (compiled) blacklist of all enabled profiles
        /// </summary>
        public HashSet<string> ActiveBlacklist { get; private set; }

        /// <summary>
        /// Custom items added directly (not part of any profile)
        /// </summary>
        public HashSet<string> CustomItems { get; private set; }

        /// <summary>
        /// Directory path for saving/loading profiles
        /// </summary>
        public string DataDirectory { get; set; }

        #endregion

        #region Events

        public event Action OnProfilesChanged;
        public event Action OnActiveBlacklistChanged;

        #endregion

        #region Constructor

        public BlacklistManager()
        {
            Profiles = new Dictionary<string, BlacklistProfile>(StringComparer.OrdinalIgnoreCase);
            ActiveBlacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            CustomItems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        #endregion

        #region Profile Management

        /// <summary>
        /// Adds a profile to the manager
        /// </summary>
        public void AddProfile(BlacklistProfile profile)
        {
            if (profile == null || string.IsNullOrEmpty(profile.Id)) return;

            Profiles[profile.Id] = profile;
            RebuildActiveBlacklist();
            OnProfilesChanged?.Invoke();
        }

        /// <summary>
        /// Removes a profile from the manager
        /// </summary>
        public bool RemoveProfile(string profileId)
        {
            if (string.IsNullOrEmpty(profileId)) return false;
            if (Profiles.TryGetValue(profileId, out var profile) && profile.IsBuiltIn) return false;

            var result = Profiles.Remove(profileId);
            if (result)
            {
                RebuildActiveBlacklist();
                OnProfilesChanged?.Invoke();
            }
            return result;
        }

        /// <summary>
        /// Gets a profile by ID
        /// </summary>
        public BlacklistProfile GetProfile(string profileId)
        {
            return Profiles.TryGetValue(profileId, out var profile) ? profile : null;
        }

        /// <summary>
        /// Toggles a profile's enabled state
        /// </summary>
        public void ToggleProfile(string profileId)
        {
            if (Profiles.TryGetValue(profileId, out var profile))
            {
                profile.IsEnabled = !profile.IsEnabled;
                profile.ModifiedDate = DateTime.Now;
                RebuildActiveBlacklist();
                OnProfilesChanged?.Invoke();
            }
        }

        /// <summary>
        /// Sets a profile's enabled state
        /// </summary>
        public void SetProfileEnabled(string profileId, bool enabled)
        {
            if (Profiles.TryGetValue(profileId, out var profile))
            {
                profile.IsEnabled = enabled;
                profile.ModifiedDate = DateTime.Now;
                RebuildActiveBlacklist();
            }
        }

        /// <summary>
        /// Creates a copy of an existing profile
        /// </summary>
        public BlacklistProfile DuplicateProfile(string profileId)
        {
            if (!Profiles.TryGetValue(profileId, out var source)) return null;

            var copy = source.Clone();
            Profiles[copy.Id] = copy;
            OnProfilesChanged?.Invoke();
            return copy;
        }

        /// <summary>
        /// Renames a profile
        /// </summary>
        public void RenameProfile(string profileId, string newName)
        {
            if (Profiles.TryGetValue(profileId, out var profile))
            {
                profile.DisplayName = newName;
                profile.ModifiedDate = DateTime.Now;
                OnProfilesChanged?.Invoke();
            }
        }

        #endregion

        #region Custom Items

        /// <summary>
        /// Adds custom items to the blacklist (not part of any profile)
        /// </summary>
        public void AddCustomItems(params string[] items)
        {
            foreach (var item in items)
            {
                if (!string.IsNullOrWhiteSpace(item))
                    CustomItems.Add(item);
            }
            RebuildActiveBlacklist();
        }

        /// <summary>
        /// Removes custom items from the blacklist
        /// </summary>
        public void RemoveCustomItems(params string[] items)
        {
            foreach (var item in items)
            {
                CustomItems.Remove(item);
            }
            RebuildActiveBlacklist();
        }

        #endregion

        #region Active Blacklist

        /// <summary>
        /// Rebuilds the active blacklist from all enabled profiles and custom items
        /// </summary>
        public void RebuildActiveBlacklist()
        {
            ActiveBlacklist.Clear();

            // Add custom items
            foreach (var item in CustomItems)
            {
                ActiveBlacklist.Add(item);
            }

            // Add items from enabled profiles
            foreach (var profile in Profiles.Values.Where(p => p.IsEnabled))
            {
                foreach (var item in profile.Items)
                {
                    ActiveBlacklist.Add(item);
                }
            }

            OnActiveBlacklistChanged?.Invoke();
        }

        /// <summary>
        /// Checks if an item name is in the active blacklist
        /// </summary>
        public bool IsBlacklisted(string itemName)
        {
            return ActiveBlacklist.Contains(itemName);
        }

        /// <summary>
        /// Checks if an item is blacklisted by any of its names
        /// </summary>
        public bool IsBlacklisted(string localizedName, string fullName, string englishName)
        {
            return ActiveBlacklist.Contains(localizedName) ||
                   ActiveBlacklist.Contains(fullName) ||
                   ActiveBlacklist.Contains(englishName);
        }

        #endregion

        #region Import/Export

        /// <summary>
        /// Exports a single profile to a string
        /// </summary>
        public string ExportProfile(string profileId)
        {
            return Profiles.TryGetValue(profileId, out var profile) ? profile.ToExportString() : null;
        }

        /// <summary>
        /// Exports all profiles to a string
        /// </summary>
        public string ExportAllProfiles()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Smart Salvage - All Profiles Export");
            sb.AppendLine("# Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine();

            foreach (var profile in Profiles.Values.Where(p => !p.IsBuiltIn))
            {
                sb.AppendLine("=== PROFILE START ===");
                sb.AppendLine(profile.ToExportString());
                sb.AppendLine("=== PROFILE END ===");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Imports a profile from string
        /// </summary>
        public BlacklistProfile ImportProfile(string data)
        {
            try
            {
                var profile = BlacklistProfile.FromExportString(data);
                if (profile != null && !string.IsNullOrEmpty(profile.DisplayName))
                {
                    profile.IsImported = true;
                    profile.IsBuiltIn = false;
                    AddProfile(profile);
                    return profile;
                }
            }
            catch (Exception)
            {
                // Import failed
            }
            return null;
        }

        /// <summary>
        /// Imports a profile from Maxroll crawl data
        /// </summary>
        public BlacklistProfile ImportFromMaxrollData(MaxrollBuildData data)
        {
            if (data == null || data.Items.Count == 0) return null;

            var profile = new BlacklistProfile
            {
                DisplayName = data.BuildName ?? "Imported Build",
                Description = data.Description ?? "",
                SourceUrl = data.SourceUrl ?? "",
                HeroClass = data.HeroClass,
                Icon = GetIconForHeroClass(data.HeroClass),
                IsEnabled = true,
                IsBuiltIn = false,
                IsImported = true,
                Items = new List<string>(data.Items)
            };

            AddProfile(profile);
            return profile;
        }

        private string GetIconForHeroClass(HeroClass heroClass)
        {
            switch (heroClass)
            {
                case HeroClass.Barbarian: return "⚔️";
                case HeroClass.Crusader: return "⚜️";
                case HeroClass.DemonHunter: return "🏹";
                case HeroClass.Monk: return "☯️";
                case HeroClass.Necromancer: return "💀";
                case HeroClass.WitchDoctor: return "🐸";
                case HeroClass.Wizard: return "🔥";
                case HeroClass.Universal: return "🔑";
                default: return "📋";
            }
        }

        #endregion

        #region Persistence

        /// <summary>
        /// Saves all profiles and settings to disk
        /// </summary>
        public void SaveToFile()
        {
            if (string.IsNullOrEmpty(DataDirectory)) return;

            try
            {
                Directory.CreateDirectory(DataDirectory);

                var sb = new StringBuilder();
                sb.AppendLine("# Smart Salvage Profiles");
                sb.AppendLine("# Do not edit manually");
                sb.AppendLine("# Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                sb.AppendLine();

                // Save custom items
                sb.AppendLine("[CustomItems]");
                foreach (var item in CustomItems)
                {
                    sb.AppendLine(item);
                }
                sb.AppendLine();

                // Save non-builtin profiles
                foreach (var profile in Profiles.Values.Where(p => !p.IsBuiltIn))
                {
                    sb.AppendLine("[Profile:" + profile.Id + "]");
                    sb.AppendLine("Name=" + profile.DisplayName);
                    sb.AppendLine("Description=" + (profile.Description ?? ""));
                    sb.AppendLine("SourceUrl=" + (profile.SourceUrl ?? ""));
                    sb.AppendLine("HeroClass=" + profile.HeroClass);
                    sb.AppendLine("Icon=" + profile.Icon);
                    sb.AppendLine("IsEnabled=" + profile.IsEnabled);
                    sb.AppendLine("IsImported=" + profile.IsImported);
                    sb.AppendLine("Items=" + string.Join("|", profile.Items));
                    sb.AppendLine();
                }

                // Save enabled states for built-in profiles
                sb.AppendLine("[BuiltInStates]");
                foreach (var profile in Profiles.Values.Where(p => p.IsBuiltIn))
                {
                    sb.AppendLine(profile.Id + "=" + profile.IsEnabled);
                }

                File.WriteAllText(Path.Combine(DataDirectory, ProfilesFileName), sb.ToString());
            }
            catch (Exception)
            {
                // Failed to save
            }
        }

        /// <summary>
        /// Loads profiles and settings from disk
        /// </summary>
        public void LoadFromFile()
        {
            if (string.IsNullOrEmpty(DataDirectory)) return;

            var filePath = Path.Combine(DataDirectory, ProfilesFileName);
            if (!File.Exists(filePath)) return;

            try
            {
                var lines = File.ReadAllLines(filePath);
                string currentSection = null;
                string currentProfileId = null;
                BlacklistProfile currentProfile = null;

                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                        continue;

                    // Section detection
                    if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                    {
                        var section = trimmed.Substring(1, trimmed.Length - 2);

                        if (section == "CustomItems")
                        {
                            currentSection = "CustomItems";
                            currentProfile = null;
                        }
                        else if (section == "BuiltInStates")
                        {
                            currentSection = "BuiltInStates";
                            currentProfile = null;
                        }
                        else if (section.StartsWith("Profile:"))
                        {
                            currentSection = "Profile";
                            currentProfileId = section.Substring(8);
                            currentProfile = new BlacklistProfile { Id = currentProfileId, IsBuiltIn = false };
                        }
                        continue;
                    }

                    // Process content based on section
                    if (currentSection == "CustomItems")
                    {
                        CustomItems.Add(trimmed);
                    }
                    else if (currentSection == "BuiltInStates")
                    {
                        var parts = trimmed.Split('=');
                        if (parts.Length == 2 && Profiles.TryGetValue(parts[0], out var profile))
                        {
                            bool.TryParse(parts[1], out bool enabled);
                            profile.IsEnabled = enabled;
                        }
                    }
                    else if (currentSection == "Profile" && currentProfile != null)
                    {
                        var eqIdx = trimmed.IndexOf('=');
                        if (eqIdx > 0)
                        {
                            var key = trimmed.Substring(0, eqIdx);
                            var value = trimmed.Substring(eqIdx + 1);

                            switch (key)
                            {
                                case "Name":
                                    currentProfile.DisplayName = value;
                                    break;
                                case "Description":
                                    currentProfile.Description = value;
                                    break;
                                case "SourceUrl":
                                    currentProfile.SourceUrl = value;
                                    break;
                                case "HeroClass":
                                    Enum.TryParse(value, out HeroClass hc);
                                    currentProfile.HeroClass = hc;
                                    break;
                                case "Icon":
                                    currentProfile.Icon = value;
                                    break;
                                case "IsEnabled":
                                    bool.TryParse(value, out bool enabled);
                                    currentProfile.IsEnabled = enabled;
                                    break;
                                case "IsImported":
                                    bool.TryParse(value, out bool imported);
                                    currentProfile.IsImported = imported;
                                    break;
                                case "Items":
                                    currentProfile.Items = new List<string>(value.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries));
                                    // Add profile when items are loaded
                                    if (!string.IsNullOrEmpty(currentProfile.DisplayName))
                                    {
                                        Profiles[currentProfile.Id] = currentProfile;
                                    }
                                    break;
                            }
                        }
                    }
                }

                RebuildActiveBlacklist();
            }
            catch (Exception)
            {
                // Failed to load
            }
        }

        #endregion

        #region Built-in Profiles

        /// <summary>
        /// Initializes the default built-in profiles
        /// </summary>
        public void InitializeBuiltInProfiles()
        {
            // === UNIVERSAL ITEMS ===
            var universal = new BlacklistProfile("🔑 Universal Items", true)
            {
                Id = "Universal",
                Description = "Items that should never be salvaged for any build",
                Icon = "🔑",
                HeroClass = HeroClass.Universal,
                IsBuiltIn = true
            };
            universal.AddItems(
                "Puzzle Ring", "Ancient Puzzle Ring", "Bovine Bardiche", "Ramaladni's Gift",
                "Ring of Royal Grandeur", "Gloves of Worship", "Avarice Band", "Illusory Boots",
                "Nemesis Bracers", "Goldwrap", "Squirt's Necklace", "The Flavor of Time",
                "Convention of Elements", "Unity", "Stone of Jordan", "Focus", "Restraint",
                "The Compass Rose", "The Traveler's Pledge", "Krelm's Buff Belt",
                "Warzechian Armguards", "In-geom", "Echoing Fury", "Rechel's Ring of Larceny"
            );
            Profiles["Universal"] = universal;

            // === NECROMANCER - LoD Death Nova ===
            var necroLod = new BlacklistProfile("💀 Necro: LoD Death Nova", true)
            {
                Id = "NecroLoD",
                Description = "Legacy of Dreams Death Nova build",
                SourceUrl = "https://maxroll.gg/d3/guides/lod-death-nova-necromancer-guide",
                Icon = "💀",
                HeroClass = HeroClass.Necromancer,
                IsBuiltIn = true
            };
            necroLod.AddItems(
                "Legacy of Dreams", "Bloodtide Blade", "Funerary Pick", "Iron Rose", "Haunted Visions",
                "Krysbin's Sentence", "Dayntee's Binding", "Stone Gauntlets", "Ice Climbers",
                "Steuart's Greaves", "Aquila Cuirass", "Mantle of Channeling",
                "Ancient Parthan Defenders", "Blackthorne's Jousting Mail", "Leoric's Crown",
                "Briggs' Wrath", "Trag'Oul's Corroded Fang", "Scythe of the Cycle",
                "Strongarm Bracers", "Andariel's Visage"
            );
            Profiles["NecroLoD"] = necroLod;

            // === DEMON HUNTER - GoD Hungering Arrow ===
            var dhGod = new BlacklistProfile("🏹 DH: GoD Hungering Arrow", true)
            {
                Id = "DHGoD",
                Description = "Gears of Dreadlands Hungering Arrow build",
                SourceUrl = "https://maxroll.gg/d3/guides/god-ha-demon-hunter-guide",
                Icon = "🏹",
                HeroClass = HeroClass.DemonHunter,
                IsBuiltIn = true
            };
            dhGod.AddItems(
                "Dystopian Goggles", "Mechanical Pauldrons", "Galvanized Vest",
                "Gas Powered Automail Forearm", "Cold Cathode Trousers", "Antique Vintage Boots",
                "The Ninth Cirri Satchel", "Hunter's Wrath", "Dawn", "Depth Diggers",
                "Wraps of Clarity", "Elusive Ring", "Fortress Ballista", "Valla's Bequest",
                "Odyssey's End", "Yang's Recurve", "Buriza-Do Kyanon",
                "Guardian's Case", "Guardian's Aversion"
            );
            Profiles["DHGoD"] = dhGod;

            // === WITCH DOCTOR - Mundunugu Spirit Barrage ===
            var wdMundu = new BlacklistProfile("🐸 WD: Mundunugu Spirit Barrage", true)
            {
                Id = "WDMundu",
                Description = "Mundunugu's Regalia Spirit Barrage build",
                SourceUrl = "https://maxroll.gg/d3/guides/mundunugu-spirit-barrage-witch-doctor-guide",
                Icon = "🐸",
                HeroClass = HeroClass.WitchDoctor,
                IsBuiltIn = true
            };
            wdMundu.AddItems(
                "Mundunugu's Headdress", "Mundunugu's Descendant", "Mundunugu's Robe",
                "Mundunugu's Rhythm", "Mundunugu's Dance", "The Barber", "Gazing Demise",
                "Voo's Juicer", "Sacred Harvester", "Ring of Emptiness", "Lakumba's Ornament",
                "Captain Crimson's Silk Girdle", "Captain Crimson's Thrust",
                "Shukrani's Triumph", "Frostburn"
            );
            Profiles["WDMundu"] = wdMundu;

            // === BARBARIAN - Whirlwind Rend ===
            var barbWW = new BlacklistProfile("⚔️ Barb: Whirlwind Rend", false)
            {
                Id = "BarbWW",
                Description = "Wrath of the Wastes Whirlwind Rend build",
                SourceUrl = "https://maxroll.gg/d3/guides/whirlwind-rend-barbarian-guide",
                Icon = "⚔️",
                HeroClass = HeroClass.Barbarian,
                IsBuiltIn = true
            };
            barbWW.AddItems(
                "Helm of the Wastes", "Pauldrons of the Wastes", "Cuirass of the Wastes",
                "Gauntlet of the Wastes", "Tasset of the Wastes", "Sabaton of the Wastes",
                "Ambo's Pride", "Bul-Kathos's Solemn Vow", "Bul-Kathos's Warrior Blood",
                "Lamentation", "Mortick's Brace", "Band of Might", "Obsidian Ring of the Zodiac"
            );
            Profiles["BarbWW"] = barbWW;

            // === CRUSADER - Akkhan Bombardment ===
            var crusAkkhan = new BlacklistProfile("⚜️ Crus: Akkhan Bombardment", false)
            {
                Id = "CrusAkkhan",
                Description = "Armor of Akkhan Bombardment build",
                SourceUrl = "https://maxroll.gg/d3/guides/akkhan-bombardment-crusader-guide",
                Icon = "⚜️",
                HeroClass = HeroClass.Crusader,
                IsBuiltIn = true
            };
            crusAkkhan.AddItems(
                "Helm of Akkhan", "Pauldrons of Akkhan", "Breastplate of Akkhan",
                "Gauntlets of Akkhan", "Cuisses of Akkhan", "Sabatons of Akkhan",
                "The Mortal Drama", "Belt of the Trove", "Stone Gauntlets",
                "Norvald's Fervor", "Norvald's Favor"
            );
            Profiles["CrusAkkhan"] = crusAkkhan;

            // === MONK - Inna Mystic Ally ===
            var monkInna = new BlacklistProfile("☯️ Monk: Inna Mystic Ally", false)
            {
                Id = "MonkInna",
                Description = "Inna's Mantra Mystic Ally build",
                SourceUrl = "https://maxroll.gg/d3/guides/inna-mystic-ally-monk-guide",
                Icon = "☯️",
                HeroClass = HeroClass.Monk,
                IsBuiltIn = true
            };
            monkInna.AddItems(
                "Inna's Radiance", "Inna's Vast Expanse", "Inna's Reach",
                "Inna's Hold", "Inna's Temperance", "Inna's Sandals",
                "The Crudest Boots", "Bindings of the Lesser Gods", "Lefebvre's Soliloquy",
                "Tasker and Theo", "Echoing Fury", "Crystal Fist"
            );
            Profiles["MonkInna"] = monkInna;

            // === WIZARD - Firebird Mirror Image ===
            var wizFirebird = new BlacklistProfile("🔥 Wiz: Firebird Mirror Image", false)
            {
                Id = "WizFirebird",
                Description = "Firebird's Finery Mirror Image build",
                SourceUrl = "https://maxroll.gg/d3/guides/firebird-mirror-image-wizard-guide",
                Icon = "🔥",
                HeroClass = HeroClass.Wizard,
                IsBuiltIn = true
            };
            wizFirebird.AddItems(
                "Firebird's Pinions", "Firebird's Tarsi", "Firebird's Breast",
                "Firebird's Down", "Firebird's Plume", "Firebird's Talons",
                "The Shame of Delsere", "Orb of Infinite Depth", "Halo of Karini",
                "Deathwish", "Etched Sigil"
            );
            Profiles["WizFirebird"] = wizFirebird;

            // === CRAFTED SETS ===
            var crafted = new BlacklistProfile("🔨 Crafted Sets", true)
            {
                Id = "Crafted",
                Description = "Important craftable set items",
                Icon = "🔨",
                HeroClass = HeroClass.Universal,
                IsBuiltIn = true
            };
            crafted.AddItems(
                "Captain Crimson's Silk Girdle", "Captain Crimson's Thrust", "Captain Crimson's Waders",
                "Aughild's Power", "Aughild's Search", "Aughild's Dominion",
                "Sage's Journey", "Sage's Passage", "Sage's Apogee",
                "Cain's Habit", "Cain's Travelers", "Cain's Insight",
                "Born's Command", "Born's Privilege", "Born's Furious Wrath",
                "Guardian's Case", "Guardian's Aversion"
            );
            Profiles["Crafted"] = crafted;

            // === LEGENDARY GEMS ===
            var gems = new BlacklistProfile("💎 Legendary Gems", true)
            {
                Id = "Gems",
                Description = "Legendary gems that should never be salvaged",
                Icon = "💎",
                HeroClass = HeroClass.Universal,
                IsBuiltIn = true
            };
            gems.AddItems(
                "Legacy of Dreams", "Bane of the Stricken", "Bane of the Trapped", "Bane of the Powerful",
                "Taeguk", "Simplicity's Strength", "Gogok of Swiftness",
                "Zei's Stone of Vengeance", "Pain Enhancer", "Enforcer",
                "Molten Wildebeest's Gizzard", "Esoteric Alteration", "Moratorium",
                "Gem of Efficacious Toxin", "Mirinae, Teardrop of the Starweaver",
                "Boon of the Hoarder", "Wreath of Lightning"
            );
            Profiles["Gems"] = gems;

            RebuildActiveBlacklist();
        }

        #endregion

        #region Statistics

        /// <summary>
        /// Gets the total count of items in the active blacklist
        /// </summary>
        public int GetActiveItemCount()
        {
            return ActiveBlacklist.Count;
        }

        /// <summary>
        /// Gets the count of enabled profiles
        /// </summary>
        public int GetEnabledProfileCount()
        {
            return Profiles.Values.Count(p => p.IsEnabled);
        }

        /// <summary>
        /// Gets profiles grouped by hero class
        /// </summary>
        public Dictionary<HeroClass, List<BlacklistProfile>> GetProfilesByClass()
        {
            return Profiles.Values
                .GroupBy(p => p.HeroClass)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        #endregion
    }
}
