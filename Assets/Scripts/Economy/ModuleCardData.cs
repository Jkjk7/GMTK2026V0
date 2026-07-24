using UnityEngine;

/// <summary>
/// 手牌/商店/棋盘模块实例数据：类型、等级、投入金币。
/// </summary>
[System.Serializable]
public struct ModuleCardData
{
    public ModuleType Type;
    public int Level;
    public int InvestedGold;

    public static ModuleCardData Create(ModuleType type, int level, int investedGold)
    {
        return new ModuleCardData
        {
            Type = type,
            Level = Mathf.Max(1, level),
            InvestedGold = Mathf.Max(0, investedGold)
        };
    }

    public static ModuleCardData FromShopPurchase(ModuleType type, int level, int pricePaid)
    {
        int lvl;
        if (ModuleCatalog.IsAttackModule(type) || type == ModuleType.FlameAmp)
        {
            lvl = Mathf.Clamp(level, 1, ModulePricing.MaxAttackLevel);
        }
        else if (type == ModuleType.Miner)
        {
            lvl = Mathf.Clamp(level, 1, 3);
        }
        else
        {
            lvl = 1;
        }

        return Create(type, lvl, pricePaid);
    }

    public bool CanFuseWith(ModuleCardData other)
    {
        if (Type == ModuleType.Miner)
        {
            return Type == other.Type && Level == other.Level && Level < 3;
        }

        if (Type == ModuleType.FlameAmp)
        {
            return Type == other.Type
                   && Level == other.Level
                   && Level < ModulePricing.MaxAttackLevel;
        }

        return ModuleCatalog.IsAttackModule(Type)
               && Type == other.Type
               && Level == other.Level
               && Level < ModulePricing.MaxAttackLevel;
    }

    public ModuleCardData FusedWith(ModuleCardData other)
    {
        return Create(Type, Level + 1, InvestedGold + other.InvestedGold);
    }

    /// <summary>按 investedGold×30% 返还（向下取整，可为 0）。</summary>
    public int ScrapRefund
    {
        get
        {
            int raw = Mathf.FloorToInt(InvestedGold * ModulePricing.ScrapRefundRate);
            if (InvestedGold <= 0)
            {
                return 0;
            }

            return Mathf.Max(1, raw);
        }
    }
}
