namespace Turbo.Plugins.Custom.SmartSalvage
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a blacklist profile containing items that should not be salvaged
    /// </summary>
    public class BlacklistProfile
    {
        /// <summary>
        /// Unique identifier for the profile
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Display name shown in the UI
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Short description of the build/purpose
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Source URL (e.g., Maxroll guide link)
        /// </summary>
        public string SourceUrl { get; set; }

        /// <summary>
        /// Hero class this profile is for (if applicable)
        /// </summary>
        public HeroClass HeroClass { get; set; }

        /// <summary>
        /// Category icon/emoji for display
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// Whether this profile is currently enabled
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Whether this is a built-in profile (cannot be deleted)
        /// </summary>
        public bool IsBuiltIn { get; set; }

        /// <summary>
        /// Whether this profile was imported from Maxroll
        /// </summary>
        public bool IsImported { get; set; }

        /// <summary>
        /// Date when the profile was created or imported
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Date when the profile was last modified
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        /// <summary>
        /// List of item names in this blacklist
        /// </summary>
        public List<string> Items { get; set; }

        /// <summary>
        /// Creates a new empty blacklist profile
        /// </summary>
        public BlacklistProfile()
        {
            Id = Guid.NewGuid().ToString("N").Substring(0, 8);
            Items = new List<string>();
            CreatedDate = DateTime.Now;
            ModifiedDate = DateTime.Now;
            HeroClass = HeroClass.None;
            Icon = "📋";
            IsEnabled = true;
            IsBuiltIn = false;
            IsImported = false;
        }

        /// <summary>
        /// Creates a new blacklist profile with the specified name
        /// </summary>
        public BlacklistProfile(string displayName, bool enabled = true) : this()
        {
            DisplayName = displayName;
            IsEnabled = enabled;
        }

        /// <summary>
        /// Creates a deep copy of this profile
        /// </summary>
        public BlacklistProfile Clone()
        {
            var clone = new BlacklistProfile
            {
                Id = Guid.NewGuid().ToString("N").Substring(0, 8),
                DisplayName = DisplayName + " (Copy)",
                Description = Description,
                SourceUrl = SourceUrl,
                HeroClass = HeroClass,
                Icon = Icon,
                IsEnabled = IsEnabled,
                IsBuiltIn = false,
                IsImported = IsImported,
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now,
                Items = new List<string>(Items)
            };
            return clone;
        }

        /// <summary>
        /// Adds items to the blacklist
        /// </summary>
        public void AddItems(params string[] itemNames)
        {
            foreach (var name in itemNames)
            {
                if (!string.IsNullOrWhiteSpace(name) && !Items.Contains(name))
                {
                    Items.Add(name);
                }
            }
            ModifiedDate = DateTime.Now;
        }

        /// <summary>
        /// Removes items from the blacklist
        /// </summary>
        public void RemoveItems(params string[] itemNames)
        {
            foreach (var name in itemNames)
            {
                Items.Remove(name);
            }
            ModifiedDate = DateTime.Now;
        }

        /// <summary>
        /// Serializes the profile to a simple text format for export
        /// </summary>
        public string ToExportString()
        {
            var lines = new List<string>
            {
                "# Smart Salvage Blacklist Profile",
                "# Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                "",
                "[Profile]",
                "Name=" + DisplayName,
                "Description=" + (Description ?? ""),
                "SourceUrl=" + (SourceUrl ?? ""),
                "HeroClass=" + HeroClass.ToString(),
                "Icon=" + Icon,
                "",
                "[Items]"
            };

            foreach (var item in Items)
            {
                lines.Add(item);
            }

            return string.Join("\n", lines);
        }

        /// <summary>
        /// Parses a profile from export string format
        /// </summary>
        public static BlacklistProfile FromExportString(string data)
        {
            var profile = new BlacklistProfile();
            var lines = data.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            bool inItems = false;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                
                // Skip comments and empty lines
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                    continue;

                // Section headers
                if (trimmed == "[Profile]")
                {
                    inItems = false;
                    continue;
                }
                if (trimmed == "[Items]")
                {
                    inItems = true;
                    continue;
                }

                if (inItems)
                {
                    // Item name
                    if (!string.IsNullOrWhiteSpace(trimmed))
                    {
                        profile.Items.Add(trimmed);
                    }
                }
                else
                {
                    // Profile property
                    var eqIndex = trimmed.IndexOf('=');
                    if (eqIndex > 0)
                    {
                        var key = trimmed.Substring(0, eqIndex).Trim();
                        var value = trimmed.Substring(eqIndex + 1).Trim();

                        switch (key.ToLower())
                        {
                            case "name":
                                profile.DisplayName = value;
                                break;
                            case "description":
                                profile.Description = value;
                                break;
                            case "sourceurl":
                                profile.SourceUrl = value;
                                break;
                            case "heroclass":
                                Enum.TryParse(value, out HeroClass hc);
                                profile.HeroClass = hc;
                                break;
                            case "icon":
                                profile.Icon = value;
                                break;
                        }
                    }
                }
            }

            profile.IsImported = true;
            return profile;
        }
    }

    /// <summary>
    /// Hero class enumeration for profile categorization
    /// </summary>
    public enum HeroClass
    {
        None = 0,
        Barbarian = 1,
        Crusader = 2,
        DemonHunter = 3,
        Monk = 4,
        Necromancer = 5,
        WitchDoctor = 6,
        Wizard = 7,
        Universal = 99
    }
}
