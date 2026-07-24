using UnityEngine;

/// <summary>
/// 模块定价、刷新价、拆除价、商店阶段。
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
    public const int SparkBasePrice = 15;
    public const float ScrapRefundRate = 0.30f;

    public const int BoardExpandTo5Cost = 100;
    public const int BoardExpandTo7Cost = 300;

    public static int GetStage(int waveNumber)
    {
        int w = Mathf.Max(1, waveNumber);
        return (w - 1) / 5;
    }

    public static int RoundToFive(int value)
    {
        if (value <= 0)
        {
            return 0;
        }

        return Mathf.Max(5, Mathf.RoundToInt(value / 5f) * 5);
    }

    public static int GetShopPrice(ModuleType type, int level, int waveNumber)
    {
        int stage = GetStage(waveNumber);
        float stageMult = Mathf.Pow(1.80f, stage);
        int basePrice = GetBasePrice(type);
        ModuleRarity rarity = ModuleCatalog.GetRarity(type);
        float rarityMult = GetRarityPriceMult(rarity);
        float lvExp = GetRarityLevelExponent(rarity);

        if (ModuleCatalog.IsAttackModule(type))
        {
            int lvl = Mathf.Clamp(level, 1, MaxAttackLevel);
            float price = basePrice
                          * Mathf.Pow(lvExp, lvl - 1)
                          * stageMult
                          * rarityMult;
            return RoundToFive(Mathf.RoundToInt(price));
        }

        if (type == ModuleType.Miner)
        {
            int lvl = Mathf.Clamp(level, 1, 3);
            float price = basePrice * (1f + 0.35f * (lvl - 1)) * stageMult * rarityMult;
            return RoundToFive(Mathf.RoundToInt(price));
        }

        if (type == ModuleType.FlameAmp)
        {
            int lvl = Mathf.Clamp(level, 1, MaxAttackLevel);
            float price = basePrice * (1f + 0.4f * (lvl - 1)) * stageMult * rarityMult;
            return RoundToFive(Mathf.RoundToInt(price));
        }

        float util = basePrice * stageMult * rarityMult;
        return RoundToFive(Mathf.RoundToInt(util));
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
            case ModuleType.Spark: return SparkBasePrice;
            default: return ProjectileBasePrice;
        }
    }

    /// <summary>刷新费随商店阶段跳变，同阶段恒定。</summary>
    public static int GetRefreshCost(int waveNumber)
    {
        int stage = GetStage(waveNumber);
        // 波1–5:5；6–10:12；11–15:22
        return 5 + stage * (7 + stage);
    }

    // 兼容旧调用：忽略 refreshIndex
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

    /// <summary>按波号返回准备秒数（含大波额外 15s）。</summary>
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
    /// 商店可出现的最高等级。暂时锁 1：高等级标价买不起。
    /// 恢复高等级货架时改回 MaxAttackLevel（或 3）。
    /// </summary>
    public const int ShopMaxOfferLevel = 1;

    /// <summary>
    /// 按商店阶段滚动攻击模块等级。
    /// </summary>
    public static int RollAttackLevel(int waveNumber)
    {
        int stage = GetStage(waveNumber);
        float r = Random.value;
        int rolled;
        // 简化概率表
        switch (stage)
        {
            case 0:
                rolled = 1;
                break;
            case 1:
                rolled = r < 0.85f ? 1 : 2;
                break;
            case 2:
                if (r < 0.65f) rolled = 1;
                else if (r < 0.95f) rolled = 2;
                else rolled = 3;
                break;
            case 3:
                if (r < 0.50f) rolled = 1;
                else if (r < 0.88f) rolled = 2;
                else rolled = 3;
                break;
            case 4:
                if (r < 0.35f) rolled = 1;
                else if (r < 0.75f) rolled = 2;
                else if (r < 0.97f) rolled = 3;
                else rolled = 4;
                break;
            case 5:
                if (r < 0.25f) rolled = 1;
                else if (r < 0.63f) rolled = 2;
                else if (r < 0.93f) rolled = 3;
                else rolled = 4;
                break;
            case 6:
                if (r < 0.18f) rolled = 1;
                else if (r < 0.48f) rolled = 2;
                else if (r < 0.86f) rolled = 3;
                else rolled = 4;
                break;
            case 7:
                if (r < 0.12f) rolled = 1;
                else if (r < 0.35f) rolled = 2;
                else if (r < 0.73f) rolled = 3;
                else if (r < 0.97f) rolled = 4;
                else rolled = 5;
                break;
            case 8:
                if (r < 0.08f) rolled = 1;
                else if (r < 0.25f) rolled = 2;
                else if (r < 0.57f) rolled = 3;
                else if (r < 0.93f) rolled = 4;
                else rolled = 5;
                break;
            default:
                if (r < 0.05f) rolled = 1;
                else if (r < 0.17f) rolled = 2;
                else if (r < 0.43f) rolled = 3;
                else if (r < 0.87f) rolled = 4;
                else rolled = 5;
                break;
        }

        return Mathf.Clamp(rolled, 1, ShopMaxOfferLevel);
    }
}
