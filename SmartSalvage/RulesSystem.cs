namespace Turbo.Plugins.Custom.SmartSalvage
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Stat-based item rule - keeps items only if they meet stat requirements
    /// Example: Keep "Dawn" only if CDR >= 8%
    /// </summary>
    public class StatRule
    {
        public string Id { get; set; }
        public string ItemName { get; set; }
        public List<StatCondition> Conditions { get; set; }
        public RuleLogic Logic { get; set; }  // AND or OR
        public bool IsEnabled { get; set; }

        public StatRule()
        {
            Id = Guid.NewGuid().ToString("N").Substring(0, 8);
            Conditions = new List<StatCondition>();
            Logic = RuleLogic.And;
            IsEnabled = true;
        }

        public StatRule(string itemName) : this()
        {
            ItemName = itemName;
        }
    }

    /// <summary>
    /// A single stat condition within a rule
    /// </summary>
    public class StatCondition
    {
        public StatType Stat { get; set; }
        public CompareOp Operator { get; set; }
        public double Value { get; set; }

        public StatCondition() { }

        public StatCondition(StatType stat, CompareOp op, double value)
        {
            Stat = stat;
            Operator = op;
            Value = value;
        }

        public override string ToString()
        {
            string opStr = Operator switch
            {
                CompareOp.GreaterThan => ">",
                CompareOp.GreaterOrEqual => "≥",
                CompareOp.LessThan => "<",
                CompareOp.LessOrEqual => "≤",
                CompareOp.Equal => "=",
                _ => "?"
            };
            return $"{GetStatDisplayName(Stat)} {opStr} {FormatValue(Stat, Value)}";
        }

        private string GetStatDisplayName(StatType stat)
        {
            return stat switch
            {
                StatType.CooldownReduction => "CDR",
                StatType.ResourceCostReduction => "RCR",
                StatType.CritChance => "CHC",
                StatType.CritDamage => "CHD",
                StatType.AttackSpeed => "IAS",
                StatType.AreaDamage => "AD",
                StatType.Strength => "STR",
                StatType.Dexterity => "DEX",
                StatType.Intelligence => "INT",
                StatType.Vitality => "VIT",
                StatType.AllResist => "AR",
                StatType.Armor => "Armor",
                StatType.LifePercent => "Life%",
                StatType.LifePerHit => "LoH",
                StatType.LifeRegen => "Regen",
                StatType.EliteDamage => "Elite%",
                StatType.SocketCount => "Sockets",
                StatType.Perfection => "Perf%",
                _ => stat.ToString()
            };
        }

        private string FormatValue(StatType stat, double value)
        {
            // Percentage stats
            if (stat == StatType.CooldownReduction || stat == StatType.ResourceCostReduction ||
                stat == StatType.CritChance || stat == StatType.CritDamage ||
                stat == StatType.AttackSpeed || stat == StatType.AreaDamage ||
                stat == StatType.LifePercent || stat == StatType.EliteDamage ||
                stat == StatType.Perfection)
            {
                return $"{value:F1}%";
            }
            
            // Integer stats
            if (stat == StatType.SocketCount)
            {
                return $"{(int)value}";
            }

            // Large number stats
            return value >= 1000 ? $"{value:N0}" : $"{value:F0}";
        }
    }

    /// <summary>
    /// Global rules that override everything
    /// </summary>
    public class GlobalRules
    {
        // Always Keep Rules
        public bool AlwaysKeepPrimals { get; set; } = true;
        public bool AlwaysKeepAncients { get; set; } = false;
        public bool AlwaysKeepSetItems { get; set; } = false;
        public bool AlwaysKeepHighPerfection { get; set; } = false;
        public double HighPerfectionThreshold { get; set; } = 95.0;

        // Always Salvage Rules  
        public bool AlwaysSalvageNonAncient { get; set; } = false;
        public bool AlwaysSalvageLowPerfection { get; set; } = false;
        public double LowPerfectionThreshold { get; set; } = 50.0;
        public bool AlwaysSalvageDuplicates { get; set; } = false;

        // Protection Rules
        public bool ProtectSocketedItems { get; set; } = true;
        public bool ProtectEnchantedItems { get; set; } = true;
        public bool ProtectArmoryItems { get; set; } = true;
        public bool ProtectLockedSlots { get; set; } = true;
    }

    /// <summary>
    /// Types of stats that can be checked
    /// </summary>
    public enum StatType
    {
        // Offensive
        CritChance,
        CritDamage,
        AttackSpeed,
        AreaDamage,
        EliteDamage,
        
        // Defensive
        Armor,
        AllResist,
        LifePercent,
        LifePerHit,
        LifeRegen,
        Vitality,
        
        // Primary
        Strength,
        Dexterity,
        Intelligence,
        
        // Utility
        CooldownReduction,
        ResourceCostReduction,
        SocketCount,
        
        // Quality
        Perfection,
        AncientRank
    }

    /// <summary>
    /// Comparison operators
    /// </summary>
    public enum CompareOp
    {
        GreaterThan,
        GreaterOrEqual,
        LessThan,
        LessOrEqual,
        Equal
    }

    /// <summary>
    /// Logic for combining multiple conditions
    /// </summary>
    public enum RuleLogic
    {
        And,  // All conditions must match
        Or    // Any condition can match
    }

    /// <summary>
    /// Manages stat-based rules
    /// </summary>
    public class RulesManager
    {
        public List<StatRule> StatRules { get; set; }
        public GlobalRules GlobalRules { get; set; }

        public RulesManager()
        {
            StatRules = new List<StatRule>();
            GlobalRules = new GlobalRules();
            InitializeDefaultRules();
        }

        private void InitializeDefaultRules()
        {
            // Example: Dawn with CDR >= 8%
            var dawnRule = new StatRule("Dawn")
            {
                Conditions = new List<StatCondition>
                {
                    new StatCondition(StatType.CooldownReduction, CompareOp.GreaterOrEqual, 8.0)
                },
                IsEnabled = false  // Disabled by default
            };
            StatRules.Add(dawnRule);

            // Example: Primal Jewelry with CHC + CHD
            var jewelryRule = new StatRule("Convention of Elements")
            {
                Conditions = new List<StatCondition>
                {
                    new StatCondition(StatType.CritChance, CompareOp.GreaterOrEqual, 5.0),
                    new StatCondition(StatType.CritDamage, CompareOp.GreaterOrEqual, 45.0)
                },
                Logic = RuleLogic.And,
                IsEnabled = false
            };
            StatRules.Add(jewelryRule);
        }

        public StatRule GetRule(string itemName)
        {
            return StatRules.FirstOrDefault(r => 
                r.IsEnabled && 
                r.ItemName.Equals(itemName, StringComparison.OrdinalIgnoreCase));
        }

        public void AddRule(StatRule rule)
        {
            // Remove existing rule for same item
            StatRules.RemoveAll(r => r.ItemName.Equals(rule.ItemName, StringComparison.OrdinalIgnoreCase));
            StatRules.Add(rule);
        }

        public void RemoveRule(string itemName)
        {
            StatRules.RemoveAll(r => r.ItemName.Equals(itemName, StringComparison.OrdinalIgnoreCase));
        }

        public void ToggleRule(string ruleId)
        {
            var rule = StatRules.FirstOrDefault(r => r.Id == ruleId);
            if (rule != null)
            {
                rule.IsEnabled = !rule.IsEnabled;
            }
        }
    }
}
