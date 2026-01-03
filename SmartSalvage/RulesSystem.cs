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
            string opStr;
            switch (Operator)
            {
                case CompareOp.GreaterThan: opStr = ">"; break;
                case CompareOp.GreaterOrEqual: opStr = ">="; break;
                case CompareOp.LessThan: opStr = "<"; break;
                case CompareOp.LessOrEqual: opStr = "<="; break;
                case CompareOp.Equal: opStr = "="; break;
                default: opStr = "?"; break;
            }
            return string.Format("{0} {1} {2}", GetStatDisplayName(Stat), opStr, FormatValue(Stat, Value));
        }

        private string GetStatDisplayName(StatType stat)
        {
            switch (stat)
            {
                case StatType.CooldownReduction: return "CDR";
                case StatType.ResourceCostReduction: return "RCR";
                case StatType.CritChance: return "CHC";
                case StatType.CritDamage: return "CHD";
                case StatType.AttackSpeed: return "IAS";
                case StatType.AreaDamage: return "AD";
                case StatType.Strength: return "STR";
                case StatType.Dexterity: return "DEX";
                case StatType.Intelligence: return "INT";
                case StatType.Vitality: return "VIT";
                case StatType.AllResist: return "AR";
                case StatType.Armor: return "Armor";
                case StatType.LifePercent: return "Life%";
                case StatType.LifePerHit: return "LoH";
                case StatType.LifeRegen: return "Regen";
                case StatType.EliteDamage: return "Elite%";
                case StatType.SocketCount: return "Sockets";
                case StatType.Perfection: return "Perf%";
                default: return stat.ToString();
            }
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
                return string.Format("{0:F1}%", value);
            }
            
            // Integer stats
            if (stat == StatType.SocketCount)
            {
                return string.Format("{0}", (int)value);
            }

            // Large number stats
            return value >= 1000 ? string.Format("{0:N0}", value) : string.Format("{0:F0}", value);
        }
    }

    /// <summary>
    /// Global rules that override everything
    /// </summary>
    public class GlobalRules
    {
        // Always Keep Rules
        public bool AlwaysKeepPrimals { get; set; }
        public bool AlwaysKeepAncients { get; set; }
        public bool AlwaysKeepSetItems { get; set; }
        public bool AlwaysKeepHighPerfection { get; set; }
        public double HighPerfectionThreshold { get; set; }

        // Always Salvage Rules  
        public bool AlwaysSalvageNonAncient { get; set; }
        public bool AlwaysSalvageLowPerfection { get; set; }
        public double LowPerfectionThreshold { get; set; }
        public bool AlwaysSalvageDuplicates { get; set; }

        // Protection Rules
        public bool ProtectSocketedItems { get; set; }
        public bool ProtectEnchantedItems { get; set; }
        public bool ProtectArmoryItems { get; set; }
        public bool ProtectLockedSlots { get; set; }

        public GlobalRules()
        {
            AlwaysKeepPrimals = true;
            AlwaysKeepAncients = false;
            AlwaysKeepSetItems = false;
            AlwaysKeepHighPerfection = false;
            HighPerfectionThreshold = 95.0;
            AlwaysSalvageNonAncient = false;
            AlwaysSalvageLowPerfection = false;
            LowPerfectionThreshold = 50.0;
            AlwaysSalvageDuplicates = false;
            ProtectSocketedItems = true;
            ProtectEnchantedItems = true;
            ProtectArmoryItems = true;
            ProtectLockedSlots = true;
        }
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

            // Example: Convention of Elements with CHC + CHD
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
