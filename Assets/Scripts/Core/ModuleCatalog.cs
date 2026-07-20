using UnityEngine;

/// <summary>
/// 当前可售卖/可出现的模块目录：名称、描述、留言与预览数值。
/// </summary>
public static class ModuleCatalog
{
    const int BaseDamage = 5;
    const int BaseEnergyCapacity = 10;
    const float BaseFireInterval = 0.1f;

    static readonly ModuleType[] SellableTypes =
    {
        ModuleType.Redirector,
        ModuleType.Projectile
    };

    public static bool IsAttackModule(ModuleType type) => type == ModuleType.Projectile;

    public static bool IsUtilityModule(ModuleType type) => type == ModuleType.Redirector;

    public static ModuleType[] GetSellableTypes() => SellableTypes;

    public static ModuleType RollRandomType()
    {
        ModuleType[] pool = SellableTypes;
        if (pool == null || pool.Length == 0)
        {
            return ModuleType.Redirector;
        }

        return pool[UnityEngine.Random.Range(0, pool.Length)];
    }

    /// <summary>商店货架：4 攻击 + 1 功能 + 1 随机。</summary>
    public static ModuleType RollShopSlotType(int slotIndex)
    {
        if (slotIndex < 4)
        {
            return ModuleType.Projectile;
        }

        if (slotIndex == 4)
        {
            return ModuleType.Redirector;
        }

        return RollRandomType();
    }

    public static string GetDisplayName(ModuleType type)
    {
        switch (type)
        {
            case ModuleType.Redirector: return "收束器";
            case ModuleType.Projectile: return "查理激光塔";
            default: return type.ToString();
        }
    }

    public static string GetDisplayName(ModuleCardData card)
    {
        string baseName = GetDisplayName(card.Type);
        if (IsAttackModule(card.Type) && card.Level > 1)
        {
            return $"{baseName} Lv{card.Level}";
        }

        return baseName;
    }

    /// <summary>玩法描述（详情中优先展示）。</summary>
    public static string GetDescription(ModuleType type)
    {
        switch (type)
        {
            case ModuleType.Projectile:
                return "对最近的敌人发射激光造成少量伤害";
            case ModuleType.Redirector:
                return "将光球沿直角改向，连通两个相邻方向口";
            default:
                return string.Empty;
        }
    }

    /// <summary>风味留言（次要，字号更小）。</summary>
    public static string GetFlavor(ModuleType type)
    {
        switch (type)
        {
            case ModuleType.Projectile:
                return "查理工厂倾心制造的防御措施";
            case ModuleType.Redirector:
                return "把能量弯到该去的地方";
            default:
                return string.Empty;
        }
    }

    public static int GetDamagePerShot(int level)
    {
        int lvl = Mathf.Clamp(level, 1, ModulePricing.MaxAttackLevel);
        return Mathf.RoundToInt(BaseDamage * Mathf.Pow(1.8f, lvl - 1));
    }

    public static int GetEnergyCapacity(int level)
    {
        int lvl = Mathf.Clamp(level, 1, ModulePricing.MaxAttackLevel);
        return BaseEnergyCapacity + (lvl - 1) * 2;
    }

    public static float GetFireInterval(int level)
    {
        int lvl = Mathf.Clamp(level, 1, ModulePricing.MaxAttackLevel);
        return Mathf.Max(0.05f, BaseFireInterval / (1f + 0.10f * (lvl - 1)));
    }

    public static Color GetDisplayColor(ModuleType type)
    {
        switch (type)
        {
            case ModuleType.Redirector: return new Color(0.4f, 0.75f, 0.95f, 1f);
            case ModuleType.Projectile: return new Color(0.9f, 0.35f, 0.25f, 1f);
            default: return Color.gray;
        }
    }
}
