using UnityEngine;

/// <summary>
/// 当前可售卖/可出现的模块目录：名称、描述、留言、稀有度与预览数值。
/// </summary>
public static class ModuleCatalog
{
    const int BaseDamage = 5;
    const int BaseEnergyCapacity = 5;
    const float BaseFireInterval = 0.2f;

    static string L(string english, string chinese) => GameLocalization.Text(english, chinese);

    static readonly ModuleType[] AllTypes =
    {
        ModuleType.Redirector,
        ModuleType.Projectile,
        ModuleType.Bomb,
        ModuleType.IceLaser,
        ModuleType.Miner,
        ModuleType.BlackHole,
        ModuleType.FlameAmp,
        ModuleType.Spark,
        ModuleType.Splitter,
        ModuleType.Portal,
        ModuleType.Relay,
        ModuleType.Accelerator,
        ModuleType.Fusion,
        ModuleType.Fission,
        ModuleType.FireEnchant,
        ModuleType.Surprise,
        ModuleType.Heatwave
    };

    public static bool IsAttackModule(ModuleType type) =>
        type == ModuleType.Projectile
        || type == ModuleType.Bomb
        || type == ModuleType.IceLaser
        || type == ModuleType.BlackHole
        || type == ModuleType.Spark
        || type == ModuleType.Heatwave;

    public static bool IsUtilityModule(ModuleType type) =>
        type == ModuleType.Redirector
        || type == ModuleType.Miner
        || type == ModuleType.FlameAmp
        || type == ModuleType.FireEnchant
        || type == ModuleType.Surprise
        || IsPathEffectModule(type);

    public static bool IsPathEffectModule(ModuleType type) =>
        type == ModuleType.Splitter
        || type == ModuleType.Portal
        || type == ModuleType.Relay
        || type == ModuleType.Accelerator
        || type == ModuleType.Fusion
        || type == ModuleType.Fission;

    public static bool CanBendWithRedirector(ModuleType type) =>
        type == ModuleType.Portal
        || type == ModuleType.Relay
        || type == ModuleType.Accelerator
        || type == ModuleType.Fusion
        || type == ModuleType.Fission;

    public static bool IsPathModule(ModuleType type) =>
        type == ModuleType.Redirector || IsPathEffectModule(type);

    public static bool IsRotatable(ModuleType type) => IsPathModule(type);

    public static ModuleType[] GetSellableTypes() => AllTypes;

    public static ModuleRarity GetRarity(ModuleType type)
    {
        switch (type)
        {
            case ModuleType.Redirector:
            case ModuleType.Miner:
            case ModuleType.FlameAmp:
            case ModuleType.Portal:
            case ModuleType.Relay:
            case ModuleType.Accelerator:
            case ModuleType.FireEnchant:
            case ModuleType.Surprise:
            case ModuleType.Heatwave:
                return ModuleRarity.Rare;
            case ModuleType.BlackHole:
            case ModuleType.Splitter:
            case ModuleType.Fusion:
            case ModuleType.Fission:
                return ModuleRarity.Epic;
            default:
                return ModuleRarity.Common;
        }
    }

    public static string GetRarityName(ModuleRarity rarity)
    {
        switch (rarity)
        {
            case ModuleRarity.Rare: return L("Rare", "稀有");
            case ModuleRarity.Epic: return L("Epic", "史诗");
            case ModuleRarity.Legendary: return L("Legendary", "传奇");
            default: return L("Common", "普通");
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
            case ModuleType.Redirector: return L("Redirector", "收束器");
            case ModuleType.Projectile: return L("Charlie Laser Tower", "查理激光塔");
            case ModuleType.Bomb: return L("David Bomb Tower", "大卫炸弹塔");
            case ModuleType.IceLaser: return L("Snowflake Launcher", "雪花发射塔");
            case ModuleType.Miner: return L("Bitcoin Miner", "比特币采矿机");
            case ModuleType.BlackHole: return L("Black Hole Launcher", "黑洞发射器");
            case ModuleType.FlameAmp: return L("Flame Amplifier", "火焰增幅");
            case ModuleType.Spark: return L("Spark Launcher", "火花发射塔");
            case ModuleType.Splitter: return L("Splitter", "分裂器");
            case ModuleType.Portal: return L("Portal", "传送门");
            case ModuleType.Relay: return L("Relay", "中续器");
            case ModuleType.Accelerator: return L("Accelerator", "加速器");
            case ModuleType.Fusion: return L("Fusion", "核聚变");
            case ModuleType.Fission: return L("Fission", "核裂变");
            case ModuleType.FireEnchant: return L("Fire Enchanter", "火附魔");
            case ModuleType.Surprise: return L("Surprise", "惊喜");
            case ModuleType.Heatwave: return L("Heatwave", "热浪");
            default: return type.ToString();
        }
    }

    public static string GetDisplayName(ModuleCardData card)
    {
        string baseName = GetDisplayName(card.Type);
        if (card.Bent)
        {
            baseName += L(" (Bent)", "（拐弯）");
        }

        if (IsAttackModule(card.Type) && card.Level > 1)
        {
            return $"{baseName} Lv{card.Level}";
        }

        if ((card.Type == ModuleType.Miner
             || card.Type == ModuleType.FlameAmp
             || card.Type == ModuleType.FireEnchant
             || card.Type == ModuleType.Surprise) && card.Level > 1)
        {
            return $"{baseName} Lv{card.Level}";
        }

        return baseName;
    }

    public static string GetTag(ModuleType type)
    {
        switch (type)
        {
            case ModuleType.Projectile: return L("Single Target", "单体");
            case ModuleType.Bomb: return "AOE";
            case ModuleType.IceLaser: return L("Control", "控制");
            case ModuleType.Miner: return L("Economy", "经济");
            case ModuleType.Redirector: return L("Routing", "路径");
            case ModuleType.BlackHole: return L("Crowd Control", "聚怪");
            case ModuleType.FlameAmp: return L("Burn", "灼烧");
            case ModuleType.Spark: return L("Spark", "火花");
            case ModuleType.Splitter: return L("Split", "分裂");
            case ModuleType.Portal: return L("Teleport", "传送");
            case ModuleType.Relay: return L("Sustain", "续航");
            case ModuleType.Accelerator: return L("Speed", "加速");
            case ModuleType.Fusion: return L("Fusion", "聚变");
            case ModuleType.Fission: return L("Fission", "裂变");
            case ModuleType.FireEnchant: return L("Enchant", "附魔");
            case ModuleType.Surprise: return L("Enchant", "附魔");
            case ModuleType.Heatwave: return L("Burn", "灼烧");
            default: return string.Empty;
        }
    }

    public static string GetDescription(ModuleType type)
    {
        switch (type)
        {
            case ModuleType.Projectile:
                return L("Fires a laser at the nearest enemy.", "对最近的敌人发射激光造成少量伤害");
            case ModuleType.Redirector:
                return L("Turns energy balls at a right angle.", "将光球沿直角改向，连通两个相邻方向口");
            case ModuleType.Bomb:
                return L("Throws an explosive bomb at the leading enemy.", "向最左敌人投掷炸弹，落地后造成范围爆炸伤害");
            case ModuleType.IceLaser:
                return L("Fires a snowflake for 5 damage and 30% chill; upgrades extend the slow.", "射出弧线淡蓝白雪花弹，造成恒定 5 点伤害并施加 30% 寒冷；升级延长减速时长");
            case ModuleType.Miner:
                return L("Consumes energy to mine gold; upgrades improve each payout.", "消耗固定能量开采比特币；升级大幅提高每次产金");
            case ModuleType.BlackHole:
                return L("Launches a black hole every 3 seconds that pulls enemies toward its center.", "向最近敌人投掷黑洞，落地后大范围吸引敌人向中心聚拢；射速 3 秒一发，越靠近中心吸力越强");
            case ModuleType.FlameAmp:
                return L("Passively increases burn tick damage; multiple amplifiers stack.", "场上被动提高灼烧每次跳动伤害；多座叠加");
            case ModuleType.Spark:
                return L("Applies burn; combine with snowflakes to trigger Melt.", "射出弧线橙红火花弹，命中施加灼烧；适合与雪花触发融化");
            case ModuleType.Splitter:
                return L("Splits one ball into two equal-energy balls with half remaining life.", "T 形一分二：原球销毁，左右口各出一球（同能量、剩余寿命减半）");
            case ModuleType.Portal:
                return L("A pair teleports balls while preserving their direction; maximum two.", "场上最多 2 座；成对时球保持飞行方向传送到另一门");
            case ModuleType.Relay:
                return L("Stores 20 energy, then refreshes the next ball's lifetime.", "吸收能量至 20；储能后下一球刷新寿命并清空储能");
            case ModuleType.Accelerator:
                return L("Accelerates each eligible ball to ×1.5 speed once.", "使未加速过的球速度 ×1.5（每球仅一次）");
            case ModuleType.Fusion:
                return L("Fuses 5 balls into one with combined energy and averaged life and speed.", "吸收 5 颗球后射出 1 颗：能量相加，寿命与速度取平均");
            case ModuleType.Fission:
                return L("After storing 5 energy, fires 5 fast default balls over 0.5 seconds.", "吸收能量 ≥5 后，0.5 秒内依次射出 5 颗高速默认球");
            case ModuleType.FireEnchant:
                return L("Enchant fixed seeded cells with burn; upgrades add one cell.", "按固定种子给若干格写入灼烧附魔；升级多附魔 1 格");
            case ModuleType.Surprise:
                return L("Apply random enchants to fixed seeded cells; upgrades add one cell.", "按固定种子给若干格写入随机附魔；升级多附魔 1 格");
            case ModuleType.Heatwave:
                return L("At full charge, burns every enemy; 5-second cooldown.", "储能满后释放热浪：全屏施加灼烧，冷却 5 秒");
            default:
                return string.Empty;
        }
    }

    public static string GetFlavor(ModuleType type)
    {
        switch (type)
        {
            case ModuleType.Projectile:
                return L("Charlie's favorite factory-made defense.", "查理工厂倾心制造的防御措施");
            case ModuleType.Redirector:
                return L("Bend energy toward where it belongs.", "把能量弯到该去的地方");
            case ModuleType.Bomb:
                return L("David prefers one loud bang to delicate work.", "大卫不喜欢精细活，只喜欢一声巨响");
            case ModuleType.IceLaser:
                return L("Not a laser—just a very determined snowflake.", "不是激光，是会拐弯的小雪花");
            case ModuleType.Miner:
                return L("For shinier electronic gold.", "为了更香的电子金币");
            case ModuleType.BlackHole:
                return L("Forbidden factory stock for tightly packing monsters.", "时空工厂的禁售库存，专治散装怪物");
            case ModuleType.FlameAmp:
                return L("Make the whole lane run hotter.", "把整条航道烧得更烫一点");
            case ModuleType.Spark:
                return L("Crackling sparks in every direction.", "像 Noita 里那样噼里啪啦往外飞");
            case ModuleType.Splitter:
                return L("One ball becomes two; lifetime pays the price.", "一球拆两半，寿命也对半砍");
            case ModuleType.Portal:
                return L("Enter here, leave there—keep your bearings.", "这边进，那边出，方向别晕");
            case ModuleType.Relay:
                return L("Fill up first, then renew a passing ball.", "先吃饱，再给路过的球续命");
            case ModuleType.Accelerator:
                return L("One push on the throttle is enough.", "油门踩一次就够了");
            case ModuleType.Fusion:
                return L("Five become one, with energy combined.", "五球归一心，能量叠满仓");
            case ModuleType.Fission:
                return L("Save enough, then burst into five.", "攒够就炸成五连发");
            case ModuleType.FireEnchant:
                return L("Brand the board with fire.", "把格子烫出烙印");
            case ModuleType.Surprise:
                return L("Open the box and discover the enchant.", "盒子一开，谁知道是啥附魔");
            case ModuleType.Heatwave:
                return L("Make the whole battlefield sweat.", "整片战场一起出汗");
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

    public static float GetHeatwaveBurnDuration(int level)
    {
        switch (Mathf.Clamp(level, 1, ModulePricing.MaxAttackLevel))
        {
            case 1: return 2f;
            case 2: return 3f;
            case 3: return 4f;
            default: return 5f;
        }
    }

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
            case ModuleType.Splitter: return new Color(0.85f, 0.45f, 0.95f, 1f);
            case ModuleType.Portal: return new Color(0.45f, 0.35f, 0.95f, 1f);
            case ModuleType.Relay: return new Color(0.35f, 0.9f, 0.75f, 1f);
            case ModuleType.Accelerator: return new Color(1f, 0.85f, 0.25f, 1f);
            case ModuleType.Fusion: return new Color(0.95f, 0.4f, 0.85f, 1f);
            case ModuleType.Fission: return new Color(1f, 0.55f, 0.2f, 1f);
            case ModuleType.FireEnchant: return new Color(1f, 0.35f, 0.1f, 1f);
            case ModuleType.Surprise: return new Color(0.95f, 0.55f, 0.85f, 1f);
            case ModuleType.Heatwave: return new Color(1f, 0.25f, 0.15f, 1f);
            default: return Color.gray;
        }
    }
}
