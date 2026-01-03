namespace Turbo.Plugins.Custom.SmartSalvage
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Crawls build guides from Maxroll.gg AND Icy-Veins.com to extract item blacklists
    /// Uses a whitelist approach - only known D3 item names are accepted
    /// </summary>
    public class MaxrollCrawler
    {
        #region Constants

        private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
        private const int TimeoutMs = 30000;

        #endregion

        #region Properties

        public string LastError { get; private set; }
        public bool IsCrawling { get; private set; }
        public string StatusMessage { get; private set; }

        #endregion

        #region Known D3 Items Database

        // Comprehensive list of ALL known D3 legendary/set items
        private static readonly HashSet<string> KnownItems = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // === UNIVERSAL ITEMS ===
            "Puzzle Ring", "Ancient Puzzle Ring", "Bovine Bardiche", "Ramaladni's Gift",
            "Ring of Royal Grandeur", "Gloves of Worship", "Avarice Band", "Illusory Boots",
            "Nemesis Bracers", "Goldwrap", "Squirt's Necklace", "The Flavor of Time",
            "Convention of Elements", "Unity", "Stone of Jordan", "Focus", "Restraint",
            "The Compass Rose", "The Traveler's Pledge", "Krelm's Buff Belt", "Krelm's Buff Bracers",
            "Warzechian Armguards", "In-geom", "Echoing Fury", "Rechel's Ring of Larceny",
            "Leoric's Crown", "Aquila Cuirass", "Ice Climbers", "Stone Gauntlets",
            "Mantle of Channeling", "Obsidian Ring of the Zodiac", "Halo of Karini",
            "The Witching Hour", "String of Ears", "Pride's Fall", "Andariel's Visage",
            "Magefist", "Frostburn", "Tasker and Theo", "St. Archew's Gage",
            "Strongarm Bracers", "Ancient Parthan Defenders", "Lacuni Prowlers",
            "Pox Faulds", "Hexing Pants of Mr. Yan", "Swamp Land Waders",
            "Blackthorne's Jousting Mail", "Blackthorne's Notched Belt", "Blackthorne's Surcoat",
            "Blackthorne's Duncraig Cross", "Blackthorne's Spurs",
            "Oculus Ring", "Krysbin's Sentence", "Briggs' Wrath",
            "Lornelle's Sunstone", "Pandemonium Loop", "Wyrdward",
            "Leonine Bow of Hashir", "Cluckeye", "Windforce",
            "The Furnace", "Messerschmidt's Reaver", "Heart Slaughter",
            "Pig Sticker", "Hack", "Wizardspike",
            "The Executioner", "Blade of the Warlord", "Devastator",
            
            // === LEGENDARY GEMS ===
            "Legacy of Dreams", "Bane of the Stricken", "Bane of the Trapped", "Bane of the Powerful",
            "Taeguk", "Simplicity's Strength", "Gogok of Swiftness", "Zei's Stone of Vengeance",
            "Pain Enhancer", "Enforcer", "Molten Wildebeest's Gizzard", "Esoteric Alteration",
            "Moratorium", "Gem of Efficacious Toxin", "Mirinae, Teardrop of the Starweaver",
            "Boon of the Hoarder", "Wreath of Lightning", "Mutilation Guard", "Boyarsky's Chip",
            "Iceblink", "Gem of Ease", "Red Soul Shard", "Invigorating Gemstone",
            
            // === NECROMANCER ITEMS ===
            "Bloodtide Blade", "Funerary Pick", "Iron Rose", "Haunted Visions",
            "Krysbin's Sentence", "Dayntee's Binding", "Steuart's Greaves",
            "Briggs' Wrath", "Trag'Oul's Corroded Fang", "Scythe of the Cycle",
            "Jesseth Skullscythe", "Jesseth Skullshield", "Reilena's Shadowhook",
            "Nayr's Black Death", "Maltorius' Petrified Spike", "Leger's Disdain",
            "Lost Time", "Bone Ringer", "Spear of Jairo", "Circle of Nailuj's Evol",
            "Grasps of Essence", "Razeth's Volition", "Corpsewhisper Pauldrons",
            "Wisdom of Kalan", "Fate's Vow", "Bloodsong Mail",
            // Trag'Oul Set
            "Trag'Oul's Guise", "Trag'Oul's Heart", "Trag'Oul's Claws",
            "Trag'Oul's Hide", "Trag'Oul's Stalwart Greaves", "Trag'Oul's Wings",
            // Rathma Set
            "Rathma's Skull Helm", "Rathma's Ribcage Plate", "Rathma's Macabre Vambraces",
            "Rathma's Skeletal Legplates", "Rathma's Ossified Sabatons", "Rathma's Spikes",
            // Grace of Inarius Set
            "Inarius's Understanding", "Inarius's Conviction", "Inarius's Reticence",
            "Inarius's Will", "Inarius's Perseverance", "Inarius's Martyrdom",
            // Pestilence Set
            "Pestilence Mask", "Pestilence Defense", "Pestilence Gloves",
            "Pestilence Incantations", "Pestilence Battle Boots", "Pestilence Robe",
            // Masquerade Set
            "Masquerade of the Burning Carnival Mask", "Masquerade of the Burning Carnival Shroud",
            "Masquerade of the Burning Carnival Gauntlets", "Masquerade of the Burning Carnival Breeches",
            "Masquerade of the Burning Carnival Boots", "Masquerade of the Burning Carnival Epaulets",
            
            // === DEMON HUNTER ITEMS ===
            "Dystopian Goggles", "Mechanical Pauldrons", "Galvanized Vest",
            "Gas Powered Automail Forearm", "Cold Cathode Trousers", "Antique Vintage Boots",
            "The Ninth Cirri Satchel", "Hunter's Wrath", "Dawn", "Depth Diggers",
            "Wraps of Clarity", "Elusive Ring", "Fortress Ballista", "Valla's Bequest",
            "Odyssey's End", "Yang's Recurve", "Buriza-Do Kyanon", "Manticore",
            "Karlei's Point", "Lord Greenstone's Fan", "Lianna's Wings",
            "K'mar Tenclip", "Wojahnni Assaulter", "Hellrack", "Chanon Bolter",
            "Guardian's Case", "Guardian's Aversion", "Sin Seekers", "Holy Point Shot",
            "Bombardier's Rucksack", "Dead Man's Legacy", "Emimei's Duffel",
            "Zoey's Secret", "Omryn's Chain", "Hellcat Waistguard",
            "Calamity", "Danetta's Spite", "Danetta's Revenge",
            // Natalya Set
            "Natalya's Sight", "Natalya's Embrace", "Natalya's Touch",
            "Natalya's Leggings", "Natalya's Bloody Footprints", "Natalya's Slayer",
            "Natalya's Reflection",
            // Shadow Set
            "The Shadow's Mask", "The Shadow's Bane", "The Shadow's Grasp",
            "The Shadow's Coil", "The Shadow's Heels", "The Shadow's Mantle",
            // Marauder Set
            "Marauder's Visage", "Marauder's Carapace", "Marauder's Gloves",
            "Marauder's Encasement", "Marauder's Treads", "Marauder's Spines",
            // Unhallowed Essence Set
            "Unsanctified Shoulders", "Cage of the Hellborn", "Fiendish Grips",
            "Unholy Plates", "Hell Walkers", "Accursed Visage",
            // Gears of Dreadlands Set (GoD)
            "Gears of Dreadlands",
            
            // === WITCH DOCTOR ITEMS ===
            "Mundunugu's Headdress", "Mundunugu's Descendant", "Mundunugu's Robe",
            "Mundunugu's Rhythm", "Mundunugu's Dance",
            "The Barber", "Gazing Demise", "Voo's Juicer", "Sacred Harvester",
            "Ring of Emptiness", "Lakumba's Ornament", "Shukrani's Triumph",
            "Staff of Chiroptera", "The Spider Queen's Grasp", "Wormwood",
            "Rhen'ho Flayer", "The Dagger of Darts", "Starmetal Kukri",
            "Thing of the Deep", "Uhkapian Serpent", "Henri's Perquisition",
            "Mask of Jeram", "Quetzalcoatl", "Carnevil", "Tiklandian Visage",
            "Homunculus", "Belt of Transcendence", "Hwoj Wrap", "Haunting Girdle",
            "Coils of the First Spider", "Bracers of the First Men",
            "Short Man's Finger", "Tall Man's Finger",
            // Helltooth Set
            "Helltooth Mask", "Helltooth Mantle", "Helltooth Tunic",
            "Helltooth Gauntlets", "Helltooth Leg Guards", "Helltooth Greaves",
            // Arachyr Set
            "Spirit of Arachyr", "Arachyr's Visage", "Arachyr's Carapace",
            "Arachyr's Claws", "Arachyr's Legs", "Arachyr's Stride",
            // Jade Harvester Set
            "Jade Harvester's Wisdom", "Jade Harvester's Courage", "Jade Harvester's Joy",
            "Jade Harvester's Mercy", "Jade Harvester's Swiftness", "Jade Harvester's Peace",
            // Zunimassa Set
            "Zunimassa's Vision", "Zunimassa's Marrow", "Zunimassa's Finger Wraps",
            "Zunimassa's Cloth", "Zunimassa's Trail", "Zunimassa's String of Skulls",
            "Zunimassa's Pox",
            
            // === BARBARIAN ITEMS ===
            "Helm of the Wastes", "Pauldrons of the Wastes", "Cuirass of the Wastes",
            "Gauntlet of the Wastes", "Tasset of the Wastes", "Sabaton of the Wastes",
            "Ambo's Pride", "Bul-Kathos's Solemn Vow", "Bul-Kathos's Warrior Blood",
            "Lamentation", "Mortick's Brace", "Band of Might",
            "The Gavel of Judgment", "Remorseless", "Blade of the Tribes",
            "Fury of the Vanished Peak", "The Grandfather", "Fjord Cutter",
            "Oathkeeper", "Little Rogue", "The Slanderer", "Istvan's Paired Blades",
            "Bracers of the First Men", "Bracers of Destruction", "Skular's Salvation",
            "Pride of Cassius", "Chilanik's Chain", "The Undisputed Champion",
            "Arreat's Law", "Three Hundredth Spear", "Standoff",
            // Immortal King Set
            "Immortal King's Triumph", "Immortal King's Eternal Reign", "Immortal King's Irons",
            "Immortal King's Stature", "Immortal King's Stride", "Immortal King's Boulder Breaker",
            "Immortal King's Tribal Binding",
            // Raekor Set
            "Raekor's Will", "Raekor's Burden", "Raekor's Heart",
            "Raekor's Wraps", "Raekor's Breeches", "Raekor's Striders",
            // Might of the Earth Set
            "Eyes of the Earth", "Spires of the Earth", "Spirit of the Earth",
            "Weight of the Earth", "Foundation of the Earth", "Pull of the Earth",
            // Horde of the Ninety Savages
            "Savages Helm", "Savages Spaulders", "Savages Chest",
            "Savages Gloves", "Savages Pants", "Savages Boots",
            
            // === CRUSADER ITEMS ===
            "Helm of Akkhan", "Pauldrons of Akkhan", "Breastplate of Akkhan",
            "Gauntlets of Akkhan", "Cuisses of Akkhan", "Sabatons of Akkhan",
            "Talisman of Akkhan",
            "The Mortal Drama", "Belt of the Trove", "Norvald's Fervor", "Norvald's Favor",
            "Blade of Prophecy", "Fate of the Fell", "Golden Flense",
            "Johanna's Argument", "Gyrfalcon's Foote", "Cam's Rebuttal",
            "Flail of the Ascended", "Shield of Fury", "Denial",
            "Jekangbord", "Frydehr's Wrath", "Salvation", "Hallowed Bulwark",
            "Piro Marella", "Guard of Johanna", "The Final Witness",
            "Bracer of Fury", "Gabriel's Vambraces", "Akkhan's Manacles", "Akkhan's Leniency",
            "Faithful Memory", "Darklight", "Baleful Remnant",
            // Invoker Set
            "Crown of the Invoker", "Burden of the Invoker", "Pride of the Invoker",
            "Shackles of the Invoker", "Renewal of the Invoker", "Zeal of the Invoker",
            // Roland Set
            "Roland's Visage", "Roland's Mantle", "Roland's Bearing",
            "Roland's Grasp", "Roland's Determination", "Roland's Stride",
            // Seeker of the Light Set
            "Crown of the Light", "Foundation of the Light", "Heart of the Light",
            "Mountain of the Light", "Towers of the Light", "Will of the Light",
            // Aegis of Valor Set
            "Helm of Valor", "Spaulders of Valor", "Brigandine of Valor",
            "Gauntlets of Valor", "Cuisses of Valor", "Greaves of Valor",
            
            // === MONK ITEMS ===
            "Inna's Radiance", "Inna's Vast Expanse", "Inna's Reach",
            "Inna's Hold", "Inna's Temperance", "Inna's Sandals", "Inna's Favor",
            "The Crudest Boots", "Bindings of the Lesser Gods", "Lefebvre's Soliloquy",
            "Crystal Fist", "Vengeful Wind", "Flying Dragon", "The Fist of Az'Turrasq",
            "Won Khim Lau", "Rabid Strike", "Kyoshiro's Blade", "Shenlong's Fist of Legend",
            "Shenlong's Relentless Assault", "Lion's Claw", "Scarbringer",
            "Rivera Dancers", "Gungdo Gear", "Pinto's Pride", "Spirit Guards",
            "Cesar's Memento", "Binding of the Lost",
            "Kyoshiro's Soul", "Balance", "Incense Torch of the Grand Temple",
            // Sunwuko Set
            "Sunwuko's Crown", "Sunwuko's Balance", "Sunwuko's Paws",
            "Sunwuko's Leggings", "Sunwuko's Shines", "Sunwuko's Soul",
            // Raiment of a Thousand Storms Set
            "Mask of the Searing Sky", "Mantle of the Upside-Down Sinners",
            "Heart of the Crashing Wave", "Fists of Thunder", "Scales of the Dancing Serpent",
            "Eight-Demon Boots",
            // Uliana Set
            "Uliana's Spirit", "Uliana's Fury", "Uliana's Burden",
            "Uliana's Stratagem", "Uliana's Destiny", "Uliana's Strength",
            // Patterns of Justice Set
            "Patterns of Justice Mantle", "Patterns of Justice Sages",
            "Patterns of Justice Crown", "Patterns of Justice Cuffs",
            "Patterns of Justice Boots", "Patterns of Justice Leggings",
            
            // === WIZARD ITEMS ===
            "Firebird's Pinions", "Firebird's Tarsi", "Firebird's Breast",
            "Firebird's Down", "Firebird's Plume", "Firebird's Talons", "Firebird's Eye",
            "The Shame of Delsere", "Orb of Infinite Depth", "Deathwish", "Etched Sigil",
            "Fragment of Destiny", "Unstable Scepter", "Serpent's Sparker",
            "Wand of Woh", "Starfire", "Slorak's Madness", "Gesture of Orpheus",
            "Triumvirate", "Winter Flurry", "Myken's Ball of Hate", "Light of Grace",
            "Chantodo's Will", "Chantodo's Force", "Tal Rasha's Allegiance",
            "Nilfur's Boast", "Ranslor's Folly", "Ashnagarr's Blood Bracer",
            "Halo of Arlyse", "Halo of Karini",
            "Aether Walker", "Valthek's Rebuke", "The Twisted Sword",
            // Tal Rasha Set
            "Tal Rasha's Guise of Wisdom", "Tal Rasha's Relentless Pursuit",
            "Tal Rasha's Grasp", "Tal Rasha's Stride", "Tal Rasha's Brace",
            "Tal Rasha's Unwavering Glare",
            // Delsere Set
            "Dashing Pauldrons of Despair", "Harness of Truth", "Fierce Gauntlets",
            "Leg Guards of Mystery", "Striders of Destiny", "Shrouded Mask",
            // Vyr Set
            "Vyr's Proud Pauldrons", "Vyr's Astonishing Aura", "Vyr's Grasping Gauntlets",
            "Vyr's Fantastic Finery", "Vyr's Swaggering Stance", "Vyr's Sightless Skull",
            // Typhon Set
            "Typhon's Frons", "Typhon's Thorax", "Typhon's Claws",
            "Typhon's Abdomen", "Typhon's Tarsus", "Typhon's Tibia",
            
            // === CRAFTED SETS ===
            "Captain Crimson's Silk Girdle", "Captain Crimson's Thrust", "Captain Crimson's Waders",
            "Aughild's Power", "Aughild's Search", "Aughild's Dominion",
            "Sage's Journey", "Sage's Passage", "Sage's Apogee",
            "Cain's Habit", "Cain's Travelers", "Cain's Insight",
            "Born's Command", "Born's Privilege", "Born's Furious Wrath",
            "Guardian's Case", "Guardian's Aversion", "Guardian's Gaze",
            "Asheara's Custodian", "Asheara's Pace", "Asheara's Ward", "Asheara's Finders",
            "Demon's Hide", "Demon's Animus", "Demon's Aileron", "Demon's Plate", "Demon's Marrow"
        };

        #endregion

        #region Constructor

        public MaxrollCrawler()
        {
            LastError = null;
            IsCrawling = false;
            StatusMessage = "Ready";
        }

        #endregion

        #region Public Methods

        public MaxrollBuildData CrawlGuide(string url)
        {
            if (IsCrawling)
            {
                LastError = "Another crawl in progress";
                return null;
            }

            IsCrawling = true;
            LastError = null;
            StatusMessage = "Starting...";

            try
            {
                string fullUrl = NormalizeUrl(url);
                if (string.IsNullOrEmpty(fullUrl))
                {
                    LastError = "Invalid URL format";
                    return null;
                }

                StatusMessage = "Fetching page...";
                string html = FetchHtml(fullUrl);
                if (string.IsNullOrEmpty(html))
                {
                    return null;
                }

                StatusMessage = "Parsing content...";
                var buildData = ParseGuideHtml(html, fullUrl);

                if (buildData == null || buildData.Items.Count == 0)
                {
                    LastError = "No items found in guide";
                    return null;
                }

                StatusMessage = $"Found {buildData.Items.Count} items";
                return buildData;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return null;
            }
            finally
            {
                IsCrawling = false;
            }
        }

        public bool IsValidMaxrollUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            
            // Maxroll URLs
            if (url.Contains("maxroll.gg/d3/guides/")) return true;
            
            // Icy Veins URLs
            if (url.Contains("icy-veins.com/d3/")) return true;
            
            // Simple slug format
            if (Regex.IsMatch(url, @"^[a-z0-9\-]+$")) return true;
            
            return false;
        }

        #endregion

        #region Private Methods

        private string NormalizeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            url = url.Trim();

            // Already a full URL
            if (url.StartsWith("http"))
            {
                if (url.Contains("maxroll.gg") || url.Contains("icy-veins.com"))
                    return url;
            }

            // Maxroll partial path
            if (url.StartsWith("d3/guides/") || url.StartsWith("/d3/guides/"))
                return "https://maxroll.gg/" + url.TrimStart('/');

            // Just a slug - assume Maxroll
            if (!url.Contains("/") && !url.Contains("."))
                return "https://maxroll.gg/d3/guides/" + url;

            return null;
        }

        private string FetchHtml(string url)
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11;
                
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.UserAgent = UserAgent;
                request.Timeout = TimeoutMs;
                request.Accept = "text/html,application/xhtml+xml";
                request.Headers.Add("Accept-Language", "en-US,en;q=0.9");

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var stream = response.GetResponseStream())
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (WebException ex)
            {
                if (ex.Response is HttpWebResponse httpResponse)
                {
                    LastError = $"HTTP {(int)httpResponse.StatusCode}: {httpResponse.StatusDescription}";
                }
                else
                {
                    LastError = "Network error: " + ex.Message;
                }
                return null;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return null;
            }
        }

        private MaxrollBuildData ParseGuideHtml(string html, string sourceUrl)
        {
            var data = new MaxrollBuildData
            {
                SourceUrl = sourceUrl,
                Items = new List<string>()
            };

            // Extract build name from title
            var titleMatch = Regex.Match(html, @"<title>([^<]+)</title>", RegexOptions.IgnoreCase);
            if (titleMatch.Success)
            {
                var title = titleMatch.Groups[1].Value;
                // Clean up title
                title = Regex.Replace(title, @"\s*[-–|]\s*(Maxroll|Icy Veins).*$", "", RegexOptions.IgnoreCase);
                title = Regex.Replace(title, @"\s*(Guide|Build).*$", "", RegexOptions.IgnoreCase);
                title = WebUtility.HtmlDecode(title);
                data.BuildName = title.Trim();
            }

            // Detect hero class from URL
            data.HeroClass = DetectHeroClass(sourceUrl, html);

            // Extract items using whitelist approach
            var foundItems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Check for each known item in the HTML content
            foreach (var knownItem in KnownItems)
            {
                if (ContainsItemReference(html, knownItem))
                {
                    foundItems.Add(knownItem);
                }
            }

            data.Items.AddRange(foundItems.OrderBy(i => i));

            if (string.IsNullOrEmpty(data.Description))
            {
                data.Description = $"{data.HeroClass} build with {data.Items.Count} items";
            }

            return data;
        }

        private bool ContainsItemReference(string html, string itemName)
        {
            string escapedName = Regex.Escape(itemName);
            
            // Pattern 1: In data attributes or item sections
            if (Regex.IsMatch(html, @"data-[^""]*=""[^""]*" + escapedName + @"[^""]*""", RegexOptions.IgnoreCase))
                return true;
            
            // Pattern 2: As text content with word boundaries
            if (Regex.IsMatch(html, @"(?<=[>""\s])" + escapedName + @"(?=[<""\s,\.])", RegexOptions.IgnoreCase))
            {
                var context = GetContext(html, itemName);
                if (!IsNavigationContext(context))
                    return true;
            }

            // Pattern 3: In class names or item-name spans
            if (Regex.IsMatch(html, @"class=""[^""]*item[^""]*""[^>]*>" + escapedName, RegexOptions.IgnoreCase))
                return true;

            return false;
        }

        private string GetContext(string html, string itemName)
        {
            int idx = html.IndexOf(itemName, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return "";
            
            int start = Math.Max(0, idx - 150);
            int end = Math.Min(html.Length, idx + itemName.Length + 150);
            return html.Substring(start, end - start);
        }

        private bool IsNavigationContext(string context)
        {
            var navPatterns = new[] { 
                "href=\"/d3/", "href='/d3/", 
                "href=\"/guides", "href='/guides",
                "class=\"nav", "class='nav",
                "menu", "sidebar", "footer", "header",
                "breadcrumb", "pagination"
            };
            
            foreach (var pattern in navPatterns)
            {
                if (context.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        private HeroClass DetectHeroClass(string url, string html)
        {
            var urlLower = url.ToLower();
            var htmlLower = html?.ToLower() ?? "";

            // Check URL first (most reliable)
            if (urlLower.Contains("barbarian") || urlLower.Contains("barb")) return HeroClass.Barbarian;
            if (urlLower.Contains("crusader") || urlLower.Contains("crus")) return HeroClass.Crusader;
            if (urlLower.Contains("demon-hunter") || urlLower.Contains("demonhunter") || urlLower.Contains("-dh-")) return HeroClass.DemonHunter;
            if (urlLower.Contains("monk")) return HeroClass.Monk;
            if (urlLower.Contains("necromancer") || urlLower.Contains("necro")) return HeroClass.Necromancer;
            if (urlLower.Contains("witch-doctor") || urlLower.Contains("witchdoctor") || urlLower.Contains("-wd-")) return HeroClass.WitchDoctor;
            if (urlLower.Contains("wizard") || urlLower.Contains("wiz")) return HeroClass.Wizard;

            // Check HTML content
            if (htmlLower.Contains("barbarian")) return HeroClass.Barbarian;
            if (htmlLower.Contains("crusader")) return HeroClass.Crusader;
            if (htmlLower.Contains("demon hunter")) return HeroClass.DemonHunter;
            if (htmlLower.Contains("necromancer")) return HeroClass.Necromancer;
            if (htmlLower.Contains("witch doctor")) return HeroClass.WitchDoctor;
            if (htmlLower.Contains("wizard")) return HeroClass.Wizard;
            if (htmlLower.Contains("monk")) return HeroClass.Monk;

            return HeroClass.None;
        }

        #endregion
    }

    /// <summary>
    /// Data extracted from a build guide
    /// </summary>
    public class MaxrollBuildData
    {
        public string BuildName { get; set; }
        public string Description { get; set; }
        public string SourceUrl { get; set; }
        public HeroClass HeroClass { get; set; }
        public List<string> Items { get; set; }

        public MaxrollBuildData()
        {
            Items = new List<string>();
            HeroClass = HeroClass.None;
        }
    }
}
