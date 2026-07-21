using UnityEngine;

/// <summary>
/// 当前可售卖/可出现的模块目录：名称、描述、留言与预览数值。
/// </summary>
public static class ModuleCatalog
{
    const int BaseDamage = 5;
    const int BaseEnergyCapacity = 10;
    const float BaseFireInterval = 0.1f;

    static readonly ModuleType[] AllTypes =
    {
        ModuleType.Redirector,
        ModuleType.Projectile,
        ModuleType.Bomb,
        ModuleType.IceLaser,
        ModuleType.Miner
    };

    public static bool IsAttackModule(ModuleType type) =>
        type == ModuleType.Projectile || type == ModuleType.Bomb || type == ModuleType.IceLaser;

    public static bool IsUtilityModule(ModuleType type) =>
        type == ModuleType.Redirector || type == ModuleType.Miner;

    public static bool IsPathModule(ModuleType type) => type == ModuleType.Redirector;

    public static ModuleType[] GetSellableTypes() => AllTypes;

    public static ModuleType RollRandomType()
    {
        if (RunModulePool.Instance != null && RunModulePool.Instance.Count > 0)
        {
            var list = RunModulePool.Instance.Unlocked;
            return list[Random.Range(0, list.Count)];
        }

        return ModuleType.Redirector;
    }

    /// <summary>商店货架：优先走本局解锁池。</summary>
    public static ModuleType RollShopSlotType(int slotIndex)
    {
        if (RunModulePool.Instance != null)
        {
            return RunModulePool.Instance.RollShopSlotType(slotIndex);
        }

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
            case ModuleType.Bomb: return "大卫炸弹塔";
            case ModuleType.IceLaser: return "查理寒冰塔";
            case ModuleType.Miner: return "比特币采矿机";
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

        if (card.Type == ModuleType.Miner && card.Level > 1)
        {
            return $"{baseName} Lv{card.Level}";
        }

        return baseName;
    }

    public static string GetTag(ModuleType type)
    {
        switch (type)
        {
            case ModuleType.Projectile: return "单体";
            case ModuleType.Bomb: return "AOE";
            case ModuleType.IceLaser: return "控制";
            case ModuleType.Miner: return "经济";
            case ModuleType.Redirector: return "路径";
            default: return string.Empty;
        }
    }

    public static string GetDescription(ModuleType type)
    {
        switch (type)
        {
            case ModuleType.Projectile:
                return "对最近的敌人发射激光造成少量伤害";
            case ModuleType.Redirector:
                return "将光球沿直角改向，连通两个相邻方向口";
            case ModuleType.Bomb:
                return "向最左敌人投掷炸弹，落地后造成范围爆炸伤害";
            case ModuleType.IceLaser:
                return "发射寒冰激光造成少量伤害并减速目标";
            case ModuleType.Miner:
                return "消耗能量开采比特币，为你提供金币";
            default:
                return string.Empty;
        }
    }

    public static string GetFlavor(ModuleType type)
    {
        switch (type)
        {
            case ModuleType.Projectile:
                return "查理工厂倾心制造的防御措施";
            case ModuleType.Redirector:
                return "把能量弯到该去的地方";
            case ModuleType.Bomb:
                return "大卫不喜欢精细活，只喜欢一声巨响";
            case ModuleType.IceLaser:
                return "查理工厂的制冷事业线";
            case ModuleType.Miner:
                return "为了更香的电子金币";
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

    public static int GetBombDamage(int level)
    {
        int lvl = Mathf.Clamp(level, 1, ModulePricing.MaxAttackLevel);
        return Mathf.RoundToInt(15f * Mathf.Pow(1.5f, lvl - 1));
    }

    public static float GetBombRadius(int level)
    {
        int lvl = Mathf.Clamp(level, 1, ModulePricing.MaxAttackLevel);
        return 1.5f * (1f + 0.30f * (lvl - 1));
    }

    public static int GetMinerEnergyCost(int level)
    {
        switch (Mathf.Clamp(level, 1, 3))
        {
            case 1: return 10;
            case 2: return 8;
            default: return 5;
        }
    }

    public static Color GetDisplayColor(ModuleType type)
    {
        switch (type)
        {
            case ModuleType.Redirector: return new Color(0.4f, 0.75f, 0.95f, 1f);
            case ModuleType.Projectile: return new Color(0.9f, 0.35f, 0.25f, 1f);
            case ModuleType.Bomb: return new Color(0.95f, 0.55f, 0.15f, 1f);
            case ModuleType.IceLaser: return new Color(0.45f, 0.85f, 1f, 1f);
            case ModuleType.Miner: return new Color(0.95f, 0.85f, 0.25f, 1f);
            default: return Color.gray;
        }
    }
}
