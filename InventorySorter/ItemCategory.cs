namespace Turbo.Plugins.Custom.InventorySorter
{
    /// <summary>
    /// Item category enumeration for sorting priority and organization.
    /// Lower values = higher priority in sorting.
    /// </summary>
    public enum ItemCategory
    {
        // === ULTRA HIGH PRIORITY - PRIMAL/ANCIENT EQUIPMENT ===
        PrimalAncientWeapon = 0,
        PrimalAncientArmor = 1,
        PrimalAncientJewelry = 2,
        AncientWeapon = 3,
        AncientArmor = 4,
        AncientJewelry = 5,

        // === HIGH PRIORITY - LEGENDARY EQUIPMENT ===
        LegendaryWeapon = 10,
        LegendaryArmor = 11,
        LegendaryJewelry = 12,
        EtherealWeapon = 13,

        // === SET ITEMS ===
        SetWeapon = 20,
        SetArmor = 21,
        SetJewelry = 22,

        // === GEMS & JEWELS ===
        LegendaryGem = 30,
        FlawlessRoyalGem = 31,
        RoyalGem = 32,
        MarquisGem = 33,
        LowerGem = 34,

        // === CRAFTING & CONSUMABLES ===
        CraftingMaterial = 40,
        KeystoneFragment = 41,
        UberKey = 42,
        HoradricCache = 43,
        RamaladnisGift = 44,
        Consumable = 45,

        // === POTIONS ===
        LegendaryPotion = 50,
        NormalPotion = 51,

        // === FOLLOWERS ===
        FollowerToken = 60,

        // === SOUL SHARDS (Season 25) ===
        SoulShardPrime = 70,
        SoulShardLesser = 71,

        // === LOWER TIER EQUIPMENT ===
        RareWeapon = 80,
        RareArmor = 81,
        RareJewelry = 82,
        MagicWeapon = 90,
        MagicArmor = 91,
        MagicJewelry = 92,
        NormalEquipment = 100,

        // === MISCELLANEOUS ===
        Cosmetic = 110,
        Plans = 111,
        Dye = 112,
        
        // === UNKNOWN/OTHER ===
        Unknown = 999
    }
}
