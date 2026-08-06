using UnityEngine;

/// <summary>
/// 模块定价、商店等级、刷新价、拆除价。
/// 同等级标价固定（不随波次涨价）；商店等级每 5 波升一档，决定货架可出等级。
/// </summary>
public static class ModulePricing
{
    public const int MaxAttackLevel = 5;
    public const int ProjectileBasePrice = 10;
    public const int RedirectorBasePrice = 15;
    public const int BombBasePrice = 25;
    public const int IceLaserBasePrice = 15;
    public const int MinerBasePrice = 18;
    public const int BlackHoleBasePrice = 45;
    public const int FlameAmpBasePrice = 20;
    public const int IceAmpBasePrice = 20;
    public const int SparkBasePrice = 15;
    public const int SplitterBasePrice = 55;
    public const int PortalBasePrice = 22;
    public const int RelayBasePrice = 22;
    public const int AcceleratorBasePrice = 20;
    public const int FusionBasePrice = 40;
    public const int FissionBasePrice = 40;
    public const int FireEnchantBasePrice = 20;
    public const int SurpriseBasePrice = 20;
    public const int HeatwaveBasePrice = 25;
    public const int LaserCannonBasePrice = 30;
    public const int FrostFreezeBasePrice = 25;
    public const int ArcaneMissileBasePrice = 22;
    public const int FlameWallBasePrice = 28;
    public const int FlameBlessingBasePrice = 24;
    public const int PurifyBasePrice = 28;
    public const int FrostMushroomBasePrice = 28;
    public const float ScrapRefundRate = 0.30f;

    public const int BoardExpandTo5Cost = 100;
    public const int BoardExpandTo7Cost = 300;

    /// <summary>兼容旧「阶段」概念：stage = 商店等级 − 1。</summary>
    public static int GetStage(int waveNumber) => GetShopLevel(waveNumber) - 1;

    /// <summary>
    /// 商店等级：波 1–5→1，6–10→2，11–15→3，16–20→4…
    /// </summary>
    public static int GetShopLevel(int waveNumber)
    {
        int w = Mathf.Max(1, waveNumber);
        return (w - 1) / 5 + 1;
    }

    public static int RoundToFive(int value)
    {
        if (value <= 0)
        {
            return 0;
        }

        return Mathf.Max(5, Mathf.RoundToInt(value / 5f) * 5);
    }

    /// <summary>
    /// 商店标价：只跟类型与等级有关，不随波次变化。
    /// waveNumber 参数保留兼容，忽略。
    /// </summary>
    public static int GetShopPrice(ModuleType type, int level, int waveNumber = 1)
    {
        int basePrice = GetBasePrice(type);
        ModuleRarity rarity = ModuleCatalog.GetRarity(type);
        float rarityMult = GetRarityPriceMult(rarity);
        float lvExp = GetRarityLevelExponent(rarity);

        if (ModuleCatalog.IsAttackModule(type))
        {
            int lvl = Mathf.Clamp(level, 1, MaxAttackLevel);
            float price = basePrice * Mathf.Pow(lvExp, lvl - 1) * rarityMult;
            return RoundToFive(Mathf.RoundToInt(price));
        }

        if (type == ModuleType.Miner)
        {
            int lvl = Mathf.Clamp(level, 1, 3);
            float price = basePrice * (1f + 0.35f * (lvl - 1)) * rarityMult;
            return RoundToFive(Mathf.RoundToInt(price));
        }

        if (type == ModuleType.FlameAmp
            || type == ModuleType.IceAmp
            || type == ModuleType.FireEnchant
            || type == ModuleType.Surprise)
        {
            int lvl = Mathf.Clamp(level, 1, MaxAttackLevel);
            float price = basePrice * (1f + 0.4f * (lvl - 1)) * rarityMult;
            return RoundToFive(Mathf.RoundToInt(price));
        }

        return RoundToFive(Mathf.RoundToInt(basePrice * rarityMult));
    }

    public static float GetRarityPriceMult(ModuleRarity rarity)
    {
        switch (rarity)
        {
            case ModuleRarity.Rare: return 1.5f;
            case ModuleRarity.Epic: return 2.5f;
            case ModuleRarity.Legendary: return 4f;
            default: return 1f;
        }
    }

    public static float GetRarityLevelExponent(ModuleRarity rarity)
    {
        switch (rarity)
        {
            case ModuleRarity.Rare: return 2.35f;
            case ModuleRarity.Epic: return 2.6f;
            case ModuleRarity.Legendary: return 2.85f;
            default: return 2.25f;
        }
    }

    static int GetBasePrice(ModuleType type)
    {
        switch (type)
        {
            case ModuleType.Bomb: return BombBasePrice;
            case ModuleType.IceLaser: return IceLaserBasePrice;
            case ModuleType.Miner: return MinerBasePrice;
            case ModuleType.Redirector: return RedirectorBasePrice;
            case ModuleType.BlackHole: return BlackHoleBasePrice;
            case ModuleType.FlameAmp: return FlameAmpBasePrice;
            case ModuleType.IceAmp: return IceAmpBasePrice;
            case ModuleType.Spark: return SparkBasePrice;
            case ModuleType.Splitter: return SplitterBasePrice;
            case ModuleType.Portal: return PortalBasePrice;
            case ModuleType.Relay: return RelayBasePrice;
            case ModuleType.Accelerator: return AcceleratorBasePrice;
            case ModuleType.Fusion: return FusionBasePrice;
            case ModuleType.Fission: return FissionBasePrice;
            case ModuleType.FireEnchant: return FireEnchantBasePrice;
            case ModuleType.Surprise: return SurpriseBasePrice;
            case ModuleType.Heatwave: return HeatwaveBasePrice;
            case ModuleType.LaserCannon: return LaserCannonBasePrice;
            case ModuleType.FrostFreeze: return FrostFreezeBasePrice;
            case ModuleType.ArcaneMissile: return ArcaneMissileBasePrice;
            case ModuleType.FlameWall: return FlameWallBasePrice;
            case ModuleType.FlameBlessing: return FlameBlessingBasePrice;
            case ModuleType.Purify: return PurifyBasePrice;
            case ModuleType.FrostMushroom: return FrostMushroomBasePrice;
            default: return ProjectileBasePrice;
        }
    }

    /// <summary>
    /// 刷新费：商店 Lv1–2→5，Lv3–4→10，Lv5–6→15…
    /// </summary>
    public static int GetRefreshCost(int waveNumber)
    {
        int shopLv = GetShopLevel(waveNumber);
        return 5 * ((shopLv + 1) / 2);
    }

    public static int GetRefreshCost(int waveNumber, int refreshIndexInWave) => GetRefreshCost(waveNumber);

    public static int GetDismantleCost(ModuleCardData card, int waveNumber, bool inCombat)
    {
        if (!inCombat)
        {
            return 0;
        }

        int refPrice = GetShopPrice(card.Type, card.Level, waveNumber);
        float rate = ModuleCatalog.IsAttackModule(card.Type) ? 0.12f : 0.06f;
        return Mathf.Max(1, Mathf.RoundToInt(refPrice * rate));
    }

    /// <summary>历史：按波次准备秒数。现已改为无时限准备，仅保留供查阅/工具。</summary>
    public static float GetPrepSeconds(int waveNumber)
    {
        int w = Mathf.Max(1, waveNumber);
        float baseTime;
        if (w <= 5)
        {
            baseTime = 20f;
        }
        else if (w <= 10)
        {
            baseTime = 25f;
        }
        else if (w <= 20)
        {
            baseTime = 35f;
        }
        else if (w <= 30)
        {
            baseTime = 45f;
        }
        else if (w <= 40)
        {
            baseTime = 60f;
        }
        else
        {
            baseTime = 75f;
        }

        if (w % 5 == 0)
        {
            baseTime += 15f;
        }

        return baseTime;
    }

    /// <summary>
    /// 本商店等级可刷出的模块等级区间。
    /// Lv1:仅1；Lv2:1–2；Lv3:仅2；Lv4:2–3；… 高档偏高等级概率由 RollOfferLevel 处理。
    /// </summary>
    public static void GetOfferLevelRange(int shopLevel, out int minLevel, out int maxLevel)
    {
        int s = Mathf.Max(1, shopLevel);
        if ((s & 1) == 1)
        {
            minLevel = maxLevel = (s + 1) / 2;
        }
        else
        {
            minLevel = s / 2;
            maxLevel = minLevel + 1;
        }

        maxLevel = Mathf.Min(maxLevel, MaxAttackLevel);
        minLevel = Mathf.Clamp(minLevel, 1, maxLevel);
    }

    /// <summary>按当前波次商店等级滚动可升级模块的货架等级。</summary>
    public static int RollOfferLevel(int waveNumber)
    {
        GetOfferLevelRange(GetShopLevel(waveNumber), out int minLv, out int maxLv);
        if (minLv >= maxLv)
        {
            return minLv;
        }

        // 双档时偏高等级（约 40% 低 / 60% 高）
        return Random.value < 0.40f ? minLv : maxLv;
    }

    /// <summary>兼容旧名。</summary>
    public static int RollAttackLevel(int waveNumber) => RollOfferLevel(waveNumber);

    public static bool IsLevelableInShop(ModuleType type) =>
        ModuleCatalog.IsAttackModule(type)
        || type == ModuleType.FlameAmp
        || type == ModuleType.IceAmp
        || type == ModuleType.FireEnchant
        || type == ModuleType.Surprise
        || type == ModuleType.Miner;

    public static int ClampOfferLevelForType(ModuleType type, int rolled)
    {
        if (type == ModuleType.Miner)
        {
            return Mathf.Clamp(rolled, 1, 3);
        }

        if (type == ModuleType.FireEnchant || type == ModuleType.Surprise)
        {
            return Mathf.Clamp(rolled, 1, 4);
        }

        if (IsLevelableInShop(type))
        {
            return Mathf.Clamp(rolled, 1, MaxAttackLevel);
        }

        return 1;
    }
}
