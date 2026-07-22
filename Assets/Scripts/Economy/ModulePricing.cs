using UnityEngine;

/// <summary>
/// 模块定价、刷新价、拆除价、商店阶段。
/// </summary>
public static class ModulePricing
{
    public const int MaxAttackLevel = 5;
    public const int ProjectileBasePrice = 20;
    public const int RedirectorBasePrice = 15;
    public const int BombBasePrice = 25;
    public const int IceLaserBasePrice = 22;
    public const int MinerBasePrice = 18;
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
        // 每 5 波一大跳：stage0=1、stage1≈1.8、stage2≈3.2
        float stageMult = Mathf.Pow(1.80f, stage);
        int basePrice = GetBasePrice(type);

        if (ModuleCatalog.IsAttackModule(type))
        {
            int lvl = Mathf.Clamp(level, 1, MaxAttackLevel);
            float price = basePrice
                          * Mathf.Pow(2.25f, lvl - 1)
                          * stageMult;
            return RoundToFive(Mathf.RoundToInt(price));
        }

        if (type == ModuleType.Miner)
        {
            int lvl = Mathf.Clamp(level, 1, 3);
            float price = basePrice * (1f + 0.35f * (lvl - 1)) * stageMult;
            return RoundToFive(Mathf.RoundToInt(price));
        }

        // 功能模块：同阶段跳价，不再用 2× 基础价硬顶死（否则后期跳价无效）
        float util = basePrice * stageMult;
        return RoundToFive(Mathf.RoundToInt(util));
    }

    static int GetBasePrice(ModuleType type)
    {
        switch (type)
        {
            case ModuleType.Bomb: return BombBasePrice;
            case ModuleType.IceLaser: return IceLaserBasePrice;
            case ModuleType.Miner: return MinerBasePrice;
            case ModuleType.Redirector: return RedirectorBasePrice;
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
    /// 按商店阶段滚动攻击模块等级。
    /// </summary>
    public static int RollAttackLevel(int waveNumber)
    {
        int stage = GetStage(waveNumber);
        float r = Random.value;
        // 简化概率表
        switch (stage)
        {
            case 0:
                return 1;
            case 1:
                return r < 0.85f ? 1 : 2;
            case 2:
                if (r < 0.65f) return 1;
                if (r < 0.95f) return 2;
                return 3;
            case 3:
                if (r < 0.50f) return 1;
                if (r < 0.88f) return 2;
                return 3;
            case 4:
                if (r < 0.35f) return 1;
                if (r < 0.75f) return 2;
                if (r < 0.97f) return 3;
                return 4;
            case 5:
                if (r < 0.25f) return 1;
                if (r < 0.63f) return 2;
                if (r < 0.93f) return 3;
                return 4;
            case 6:
                if (r < 0.18f) return 1;
                if (r < 0.48f) return 2;
                if (r < 0.86f) return 3;
                return 4;
            case 7:
                if (r < 0.12f) return 1;
                if (r < 0.35f) return 2;
                if (r < 0.73f) return 3;
                if (r < 0.97f) return 4;
                return 5;
            case 8:
                if (r < 0.08f) return 1;
                if (r < 0.25f) return 2;
                if (r < 0.57f) return 3;
                if (r < 0.93f) return 4;
                return 5;
            default:
                if (r < 0.05f) return 1;
                if (r < 0.17f) return 2;
                if (r < 0.43f) return 3;
                if (r < 0.87f) return 4;
                return 5;
        }
    }
}
