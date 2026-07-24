using UnityEngine;

/// <summary>
/// 当前可售卖/可出现的模块目录：名称、描述、留言、稀有度与预览数值。
/// </summary>
public static class ModuleCatalog
{
    const int BaseDamage = 5;
    const int BaseEnergyCapacity = 5;
    const float BaseFireInterval = 0.2f;

    static readonly ModuleType[] AllTypes =
    {
        ModuleType.Redirector,
        ModuleType.Projectile,
        ModuleType.Bomb,
        ModuleType.IceLaser,
        ModuleType.Miner,
        ModuleType.BlackHole,
        ModuleType.FlameAmp,
        ModuleType.Spark
    };

    public static bool IsAttackModule(ModuleType type) =>
        type == ModuleType.Projectile
        || type == ModuleType.Bomb
        || type == ModuleType.IceLaser
        || type == ModuleType.BlackHole
        || type == ModuleType.Spark;

    public static bool IsUtilityModule(ModuleType type) =>
        type == ModuleType.Redirector
        || type == ModuleType.Miner
        || type == ModuleType.FlameAmp;

    public static bool IsPathModule(ModuleType type) => type == ModuleType.Redirector;

    public static bool IsRotatable(ModuleType type) => type == ModuleType.Redirector;

    public static ModuleType[] GetSellableTypes() => AllTypes;

    public static ModuleRarity GetRarity(ModuleType type)
    {
        switch (type)
        {
            case ModuleType.Redirector:
            case ModuleType.Miner:
            case ModuleType.FlameAmp:
                return ModuleRarity.Rare;
            case ModuleType.BlackHole:
                return ModuleRarity.Epic;
            default:
                return ModuleRarity.Common;
        }
    }

    public static string GetRarityName(ModuleRarity rarity)
    {
        switch (rarity)
        {
            case ModuleRarity.Rare: return "稀有";
            case ModuleRarity.Epic: return "史诗";
            case ModuleRarity.Legendary: return "传奇";
            default: return "普通";
        }
    }

    public static Color GetRarityColor(ModuleRarity rarity)
    {
        switch (rarity)
        {
            case ModuleRarity.Rare: return new Color(0.35f, 0.65f, 1f, 1f);
            case ModuleRarity.Epic: return new Color(0.75f, 0.4f, 1f, 1f);
            case ModuleRarity.Legendary: return new Color(1f, 0.72f, 0.2f, 1f);
            default: return new Color(0.7f, 0.72f, 0.75f, 1f);
        }
    }

    /// <summary>商店基础权重：越高越容易刷出。</summary>
    public static float GetShopWeight(ModuleRarity rarity, int stage)
    {
        float s = Mathf.Max(0, stage);
        switch (rarity)
        {
            case ModuleRarity.Common: return 10f;
            case ModuleRarity.Rare: return 3.5f + s * 0.35f;
            case ModuleRarity.Epic: return 0.8f + s * 0.25f;
            case ModuleRarity.Legendary: return 0.15f + s * 0.1f;
            default: return 1f;
        }
    }

    public static ModuleType RollRandomType()
    {
        if (RunModulePool.Instance != null && RunModulePool.Instance.Count > 0)
        {
            var list = RunModulePool.Instance.Unlocked;
            return list[Random.Range(0, list.Count)];
        }

        return ModuleType.Redirector;
    }

    public static ModuleType RollShopSlotType(int slotIndex)
    {
        return RollShopSlotType(slotIndex, WaveManager.FindDisplayWave());
    }

    public static ModuleType RollShopSlotType(int slotIndex, int waveNumber)
    {
        if (RunModulePool.Instance != null)
        {
            return RunModulePool.Instance.RollShopSlotType(slotIndex, waveNumber);
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
            case ModuleType.IceLaser: return "雪花发射塔";
            case ModuleType.Miner: return "比特币采矿机";
            case ModuleType.BlackHole: return "黑洞发射器";
            case ModuleType.FlameAmp: return "火焰增幅";
            case ModuleType.Spark: return "火花发射塔";
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

        if ((card.Type == ModuleType.Miner || card.Type == ModuleType.FlameAmp) && card.Level > 1)
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
            case ModuleType.BlackHole: return "聚怪";
            case ModuleType.FlameAmp: return "灼烧";
            case ModuleType.Spark: return "火花";
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
                return "射出弧线淡蓝白雪花弹，造成恒定 5 点伤害并施加 30% 寒冷；升级延长减速时长";
            case ModuleType.Miner:
                return "消耗固定能量开采比特币；升级大幅提高每次产金";
            case ModuleType.BlackHole:
                return "向最近敌人投掷黑洞，落地后大范围吸引敌人向中心聚拢；射速 3 秒一发，越靠近中心吸力越强";
            case ModuleType.FlameAmp:
                return "场上被动提高灼烧每次跳动伤害；多座叠加";
            case ModuleType.Spark:
                return "射出弧线橙红火花弹，命中施加灼烧；适合与雪花触发融化";
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
                return "不是激光，是会拐弯的小雪花";
            case ModuleType.Miner:
                return "为了更香的电子金币";
            case ModuleType.BlackHole:
                return "时空工厂的禁售库存，专治散装怪物";
            case ModuleType.FlameAmp:
                return "把整条航道烧得更烫一点";
            case ModuleType.Spark:
                return "像 Noita 里那样噼里啪啦往外飞";
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
        float baseR = 1.5f * (1f + 0.30f * (lvl - 1));
        float mult = RunModifiers.Instance != null ? RunModifiers.Instance.AoeRadiusMult : 1f;
        return baseR * mult;
    }

    public static float GetBlackHoleRadius(int level)
    {
        int lvl = Mathf.Clamp(level, 1, ModulePricing.MaxAttackLevel);
        float baseR = 2.2f * (1f + 0.25f * (lvl - 1));
        float mult = RunModifiers.Instance != null ? RunModifiers.Instance.AoeRadiusMult : 1f;
        return baseR * mult;
    }

    public static float GetBlackHoleDuration(int level)
    {
        int lvl = Mathf.Clamp(level, 1, ModulePricing.MaxAttackLevel);
        return 2.2f + 0.4f * (lvl - 1);
    }

    public static float GetBlackHolePullStrength(int level)
    {
        int lvl = Mathf.Clamp(level, 1, ModulePricing.MaxAttackLevel);
        return 3.5f + 0.8f * (lvl - 1);
    }

    public static int GetIceDamage(int level) => 5;

    public static int GetIceEnergyPerShot(int level = 1) => 1;

    public static float GetIceSlowDuration(int level)
    {
        int lvl = Mathf.Clamp(level, 1, ModulePricing.MaxAttackLevel);
        return 2f + (lvl - 1);
    }

    public const float IceSlowPercent = 0.30f;

    public static int GetFlameAmpBonus(int level)
    {
        switch (Mathf.Clamp(level, 1, ModulePricing.MaxAttackLevel))
        {
            case 1: return 1;
            case 2: return 3;
            case 3: return 5;
            case 4: return 7;
            default: return 10;
        }
    }

    public static int GetSparkDamage(int level)
    {
        switch (Mathf.Clamp(level, 1, ModulePricing.MaxAttackLevel))
        {
            case 1:
            case 2: return 1;
            case 3:
            case 4: return 2;
            default: return 3;
        }
    }

    public static float GetSparkBurnDuration(int level)
    {
        int lvl = Mathf.Clamp(level, 1, ModulePricing.MaxAttackLevel);
        return 2f + 0.5f * (lvl - 1);
    }

    public static int GetSparkEnergyCapacity(int level)
    {
        int lvl = Mathf.Clamp(level, 1, ModulePricing.MaxAttackLevel);
        return 20 + 4 * (lvl - 1);
    }

    public static float GetSparkFireInterval(int level)
    {
        int lvl = Mathf.Clamp(level, 1, ModulePricing.MaxAttackLevel);
        return Mathf.Max(0.05f, 0.2f / (1f + 0.05f * (lvl - 1)));
    }

    public static int GetSparkEnergyPerShot(int level = 1) => 1;

    public static int GetMinerEnergyCost(int level) => 10;

    public static int GetMinerGoldAmount(int level)
    {
        switch (Mathf.Clamp(level, 1, 3))
        {
            case 1: return 1;
            case 2: return 3;
            default: return 8;
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
            case ModuleType.BlackHole: return new Color(0.35f, 0.15f, 0.55f, 1f);
            case ModuleType.FlameAmp: return new Color(1f, 0.4f, 0.15f, 1f);
            case ModuleType.Spark: return new Color(1f, 0.7f, 0.2f, 1f);
            default: return Color.gray;
        }
    }
}
