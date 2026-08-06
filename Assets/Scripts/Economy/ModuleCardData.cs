using UnityEngine;

/// <summary>
/// 手牌/商店/棋盘模块实例数据：类型、等级、投入金币、路径形态与附魔实例种子。
/// </summary>
[System.Serializable]
public struct ModuleCardData
{
    public ModuleType Type;
    public int Level;
    public int InvestedGold;
    /// <summary>路径功能模块是否为 L 形拐弯（收束器合成）。</summary>
    public bool Bent;
    /// <summary>附魔模块实例种子；随卡牌移动和合成保留。</summary>
    public int InstanceSeed;

    public static ModuleCardData Create(
        ModuleType type,
        int level,
        int investedGold,
        bool bent = false,
        int instanceSeed = 0)
    {
        return new ModuleCardData
        {
            Type = type,
            Level = Mathf.Max(1, level),
            InvestedGold = Mathf.Max(0, investedGold),
            Bent = bent && ModuleCatalog.CanBendWithRedirector(type),
            InstanceSeed = NormalizeInstanceSeed(type, instanceSeed)
        };
    }

    static int NormalizeInstanceSeed(ModuleType type, int instanceSeed)
    {
        if (type != ModuleType.FireEnchant && type != ModuleType.Surprise)
        {
            return 0;
        }

        return instanceSeed != 0
            ? instanceSeed
            : Random.Range(1, int.MaxValue);
    }

    public static ModuleCardData FromShopPurchase(ModuleType type, int level, int pricePaid)
    {
        int lvl;
        if (ModuleCatalog.IsAttackModule(type)
            || type == ModuleType.FlameAmp
            || type == ModuleType.IceAmp
            || type == ModuleType.Heatwave
            || type == ModuleType.FrostFreeze)
        {
            lvl = Mathf.Clamp(level, 1, ModulePricing.MaxAttackLevel);
        }
        else if (type == ModuleType.FireEnchant || type == ModuleType.Surprise)
        {
            lvl = Mathf.Clamp(level, 1, 4);
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
        if (CanBendFuseWith(other))
        {
            return true;
        }

        if (Type == ModuleType.Miner)
        {
            return Type == other.Type && Level == other.Level && Level < 3 && Bent == other.Bent;
        }

        if (Type == ModuleType.FlameAmp
            || Type == ModuleType.IceAmp
            || Type == ModuleType.FireEnchant
            || Type == ModuleType.Surprise
            || Type == ModuleType.Heatwave
            || Type == ModuleType.FrostFreeze)
        {
            return Type == other.Type
                   && Level == other.Level
                   && Bent == other.Bent
                   && Level < ModulePricing.MaxAttackLevel;
        }

        return ModuleCatalog.IsAttackModule(Type)
               && Type == other.Type
               && Level == other.Level
               && Bent == other.Bent
               && Level < ModulePricing.MaxAttackLevel;
    }

    /// <summary>收束器 × 可拐弯功能模块 → 功能模块 Bent。</summary>
    public bool CanBendFuseWith(ModuleCardData other)
    {
        bool selfRed = Type == ModuleType.Redirector;
        bool otherRed = other.Type == ModuleType.Redirector;
        if (selfRed == otherRed)
        {
            return false;
        }

        ModuleCardData func = selfRed ? other : this;
        if (!ModuleCatalog.CanBendWithRedirector(func.Type))
        {
            return false;
        }

        return !func.Bent;
    }

    public ModuleCardData FusedWith(ModuleCardData other)
    {
        if (CanBendFuseWith(other))
        {
            ModuleCardData func = Type == ModuleType.Redirector ? other : this;
            return Create(
                func.Type,
                func.Level,
                InvestedGold + other.InvestedGold,
                bent: true,
                func.InstanceSeed);
        }

        return Create(
            Type,
            Level + 1,
            InvestedGold + other.InvestedGold,
            Bent,
            InstanceSeed);
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
