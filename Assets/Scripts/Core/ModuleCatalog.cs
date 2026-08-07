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
        ModuleType.Heatwave,
        ModuleType.LaserCannon,
        ModuleType.FrostFreeze,
        ModuleType.ArcaneMissile,
        ModuleType.IceAmp,
        ModuleType.FlameWall,
        ModuleType.FlameBlessing,
        ModuleType.Purify,
        ModuleType.FrostMushroom,
        ModuleType.FrostBomb,
        ModuleType.FrostCannon
    };

    public static bool IsAttackModule(ModuleType type) =>
        type == ModuleType.Projectile
        || type == ModuleType.Bomb
        || type == ModuleType.IceLaser
        || type == ModuleType.BlackHole
        || type == ModuleType.Spark
        || type == ModuleType.Heatwave
        || type == ModuleType.LaserCannon
        || type == ModuleType.FrostFreeze
        || type == ModuleType.ArcaneMissile
        || type == ModuleType.FlameWall
        || type == ModuleType.FrostBomb
        || type == ModuleType.FrostCannon;

    public static bool IsUtilityModule(ModuleType type) =>
        type == ModuleType.Redirector
        || type == ModuleType.Miner
        || type == ModuleType.FlameAmp
        || type == ModuleType.IceAmp
        || type == ModuleType.FireEnchant
        || type == ModuleType.Surprise
        || IsItemModule(type)
        || IsPathEffectModule(type);

    public static bool IsItemModule(ModuleType type) =>
        type == ModuleType.FlameBlessing
        || type == ModuleType.Purify
        || type == ModuleType.FrostMushroom;

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
            case ModuleType.IceAmp:
            case ModuleType.Portal:
            case ModuleType.Relay:
            case ModuleType.Accelerator:
            case ModuleType.FireEnchant:
            case ModuleType.Surprise:
            case ModuleType.Heatwave:
            case ModuleType.FrostFreeze:
            case ModuleType.ArcaneMissile:
            case ModuleType.FlameWall:
            case ModuleType.FlameBlessing:
            case ModuleType.Purify:
            case ModuleType.FrostCannon:
                return ModuleRarity.Rare;
            case ModuleType.FrostMushroom:
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

    public static float GetShopWeight(ModuleType type, int stage)
    {
        float weight = GetShopWeight(GetRarity(type), stage);
        if (IsItemModule(type))
        {
            return weight * 0.42f;
        }

        return weight;
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
            case ModuleType.IceAmp: return L("Ice Amplifier", "寒冰增幅");
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
            case ModuleType.LaserCannon: return L("Charlie Laser Cannon", "查理激光炮");
            case ModuleType.FrostFreeze: return L("Frost Freeze", "冰霜冻结");
            case ModuleType.ArcaneMissile: return L("Arcane Missile", "奥数飞弹");
            case ModuleType.FlameWall: return L("Flame Wall", "烈焰墙");
            case ModuleType.FlameBlessing: return L("Flame Blessing", "火焰祝福");
            case ModuleType.Purify: return L("Purify", "净化");
            case ModuleType.FrostMushroom: return L("Frost Mushroom", "寒冰菇");
            case ModuleType.FrostBomb: return L("Frost Bomb", "冰霜炸弹");
            case ModuleType.FrostCannon: return L("Frost Cannon", "冰霜炮");
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
             || card.Type == ModuleType.IceAmp
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
            case ModuleType.Projectile: return L("Single Target", "单体激光");
            case ModuleType.Bomb: return "最左AOE";
            case ModuleType.IceLaser: return L("Chill Bolt", "寒冷弹");
            case ModuleType.Miner: return L("Economy", "产金");
            case ModuleType.Redirector: return L("Routing", "直角改向");
            case ModuleType.BlackHole: return L("Crowd Control", "吸引聚怪");
            case ModuleType.FlameAmp: return L("Burn Amp", "灼烧增幅");
            case ModuleType.Spark: return L("Burn Bolt", "灼烧弹");
            case ModuleType.Splitter: return L("Split", "一分二");
            case ModuleType.Portal: return L("Teleport", "成对传送");
            case ModuleType.Relay: return L("Sustain", "汲能续寿");
            case ModuleType.Accelerator: return L("Speed", "球速×1.5");
            case ModuleType.Fusion: return L("Fusion", "五合一");
            case ModuleType.Fission: return L("Fission", "一变五");
            case ModuleType.FireEnchant: return L("Enchant", "灼烧附魔格");
            case ModuleType.Surprise: return L("Enchant", "随机附魔格");
            case ModuleType.Heatwave: return L("Screen Burn", "全屏灼烧");
            case ModuleType.LaserCannon: return L("Cannon", "高伤激光");
            case ModuleType.FrostFreeze: return L("Screen Chill", "全屏寒冷");
            case ModuleType.ArcaneMissile: return L("Missile", "紫色飞弹");
            case ModuleType.IceAmp: return L("Chill Amp", "寒冷增幅");
            case ModuleType.FlameWall: return L("Channel Burn", "持续火墙");
            case ModuleType.FlameBlessing: return L("Item", "道具");
            case ModuleType.Purify: return L("Item", "道具");
            case ModuleType.FrostMushroom: return L("Battle Item", "战斗道具");
            case ModuleType.FrostBomb: return L("Frost Ring", "霜环AOE");
            case ModuleType.FrostCannon: return L("Frost Bolt", "冰霜弹");
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
                return L(
                    "Fires a snowflake at the leftmost enemy; applies [Chill].",
                    "向最左敌人发射雪花飞弹，使敌人获得[寒冷]");
            case ModuleType.Miner:
                return L("Consumes energy to produce gold.", "消耗能量产出金币");
            case ModuleType.BlackHole:
                return L(
                    "Throws a black hole that pulls enemies together.",
                    "投掷黑洞吸引敌人");
            case ModuleType.FlameAmp:
                return L("Passively increases burn tick damage; multiple amplifiers stack.", "场上被动提高灼烧每次跳动伤害；多座叠加");
            case ModuleType.Spark:
                return L(
                    "Fires a spark at the leftmost enemy; applies [Burn].",
                    "向最左敌人发射火花飞弹，使敌人获得[灼烧]");
            case ModuleType.Splitter:
                return L("Splits one ball into two equal-energy balls with half remaining life.", "T 形一分二：原球销毁，左右口各出一球（同能量、剩余寿命减半）");
            case ModuleType.Portal:
                return L("A pair teleports balls while preserving their direction; maximum two.", "场上最多 2 座；成对时球保持飞行方向传送到另一门");
            case ModuleType.Relay:
                return L(
                    "Siphons energy from passing balls; when full, the next ball refreshes its lifetime.",
                    "球穿过时汲取能量，能量满后下一个到来的能量球会刷新寿命");
            case ModuleType.Accelerator:
                return L("Accelerates each eligible ball to ×1.5 speed once.", "使未加速过的球速度 ×1.5（每球仅一次）");
            case ModuleType.Fusion:
                return L("Fuses 5 balls into one with combined energy and averaged life and speed.", "吸收 5 颗球后射出 1 颗：能量相加，寿命与速度取平均");
            case ModuleType.Fission:
                return L("After storing 5 energy, fires 5 fast default balls over 0.5 seconds (fission balls cannot be re-absorbed).",
                    "吸收能量 ≥5 后，0.5 秒内依次射出 5 颗高速默认球（裂变球不可再被核裂变吸收）");
            case ModuleType.FireEnchant:
                return L(
                    "Turns random cells into flame enchant.",
                    "使随机地块变为火附魔");
            case ModuleType.Surprise:
                return L(
                    "Turns random cells into random enchants.",
                    "使随机地块变为随机附魔");
            case ModuleType.Heatwave:
                return L("At full charge, all enemies gain [Burn].", "储能满后全屏敌人获得[灼烧]");
            case ModuleType.LaserCannon:
                return L("Slowly fires a thicker high-damage laser at the nearest enemy.", "缓慢对最近敌人发射更大的激光");
            case ModuleType.FrostFreeze:
                return L(
                    "At full charge, all enemies gain [Chill].",
                    "储能满后全屏敌人获得[寒冷]");
            case ModuleType.ArcaneMissile:
                return L("Fires purple seeking missiles at the rightmost enemy.", "紫色索敌飞弹，锁定最右敌人");
            case ModuleType.IceAmp:
                return L(
                    "Passively increases chill slow; multiple amplifiers stack.",
                    "场上被动提高寒冷减速幅度；多座叠加");
            case ModuleType.FlameWall:
                return L(
                    "While charged, maintains a flame wall in the middle of the battle lane. Enemies that cross it take hit damage once and gain [Burn]. Continuously drains energy; the wall shuts off when empty.",
                    "有能量时在战斗区中间维持火焰墙；穿过的敌人受到一次伤害并获得[灼烧]。持续耗能，能量耗尽则关闭");
            case ModuleType.FlameBlessing:
                return L(
                    "Consume this item on a cell to turn it into a flame enchant, then the item disappears.",
                    "将此道具放到一个格子上，使其变为火焰附魔，随后道具消失");
            case ModuleType.Purify:
                return L(
                    "Consume this item on a cell to remove its curse, lock, and enchant, then the item disappears.",
                    "将此道具放到一个格子上，清除其诅咒、锁定与附魔，随后道具消失");
            case ModuleType.FrostMushroom:
                return L(
                    "Combat-only item. Use on any cell to freeze all enemies for 1 second and apply 2 seconds of chill, then the item disappears.",
                    "仅限战斗中使用。放到任意格子上后，全体敌人冻结 1 秒并获得 2 秒寒冷，随后道具消失");
            case ModuleType.FrostBomb:
                return L(
                    "Throws a frost bomb at the leftmost enemy. On impact it leaves a frost ring that continuously applies 2s [Chill].",
                    "向最左敌人投掷冰霜炸弹；落地留下霜环，踩上的敌人持续获得 2 秒[寒冷]");
            case ModuleType.FrostCannon:
                return L(
                    "Fires a fast frost bolt at the nearest enemy, dealing damage and applying 3s [Chill].",
                    "向最近敌人发射快速冰霜弹，造成伤害并使其获得 3 秒[寒冷]");
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
            case ModuleType.IceAmp:
                return L("Make the chill bite deeper.", "让寒冷咬得更紧");
            case ModuleType.Spark:
                return L("Crackling sparks in every direction.", "像 Noita 里那样噼里啪啦往外飞");
            case ModuleType.Splitter:
                return L("One ball becomes two; lifetime pays the price.", "一球拆两半，寿命也对半砍");
            case ModuleType.Portal:
                return L("Enter here, leave there—keep your bearings.", "这边进，那边出，方向别晕");
            case ModuleType.Relay:
                return L("Siphon while they pass; refresh when full.", "路过就汲能，满了再续命");
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
            case ModuleType.LaserCannon:
                return L("When a thin beam is not enough.", "细激光不够用时请叫重炮");
            case ModuleType.FrostFreeze:
                return L("Put the whole lane on ice.", "整条航道一起结霜");
            case ModuleType.ArcaneMissile:
                return L("Arcane bolts always hunt the backline first.", "奥术飞弹专打队尾");
            case ModuleType.FlameWall:
                return L("A burning curtain for the yellow rush.", "专治还没开火就冲进来的黄潮");
            case ModuleType.FlameBlessing:
                return L("Pocket fire, just add floor tiles.", "把火装进口袋，缺哪里点哪里");
            case ModuleType.Purify:
                return L("Soap, but for cursed machinery.", "像肥皂一样，把诅咒和锁一起洗掉");
            case ModuleType.FrostMushroom:
                return L("Emergency lane refrigeration in mushroom form.", "蘑菇形态的紧急制冷装置");
            case ModuleType.FrostBomb:
                return L("A blue puddle for the yellow rush.", "专治黄潮冲脸的蓝色水坑");
            case ModuleType.FrostCannon:
                return L("When chill needs to travel in a straight line.", "寒冷也需要直线高速送达");
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
        // 激光/雪花等：容量固定 5
        return BaseEnergyCapacity;
    }

    public static float GetFireInterval(int level)
    {
        // 激光/雪花：固定 5 发/秒
        return BaseFireInterval;
    }

    public static int GetBombDamage(int level)
    {
        // 与激光伤害对齐
        return GetDamagePerShot(level);
    }

    public static float GetBombRadius(int level)
    {
        int lvl = Mathf.Clamp(level, 1, ModulePricing.MaxAttackLevel);
        float baseR = 1.5f * (1f + 0.30f * (lvl - 1));
        float mult = RunModifiers.Instance != null ? RunModifiers.Instance.AoeRadiusMult : 1f;
        return baseR * mult;
    }

    public static int GetBombEnergyCapacity(int level = 1) => 5;

    public static float GetBombFireInterval(int level = 1) => 1f / 1.5f;

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

    public static int GetBlackHoleEnergyCapacity(int level = 1) => 5;

    public static int GetBlackHoleEnergyPerShot(int level = 1) => 5;

    public static float GetBlackHoleFireInterval(int level = 1) => 6f;

    public static int GetIceDamage(int level)
    {
        switch (Mathf.Clamp(level, 1, ModulePricing.MaxAttackLevel))
        {
            case 1: return 5;
            default: return 10;
        }
    }

    public static int GetIceEnergyPerShot(int level = 1) => 1;

    public static int GetIceEnergyCapacity(int level = 1) => 5;

    public static float GetIceFireInterval(int level = 1) => BaseFireInterval;

    public static float GetIceSlowDuration(int level)
    {
        int lvl = Mathf.Clamp(level, 1, ModulePricing.MaxAttackLevel);
        return 2f + (lvl - 1);
    }

    /// <summary>[寒冷] 状态的基础减速比例（不是“寒冷==减速”同义词，而是该状态的效果）。</summary>
    public const float IceSlowPercent = 0.30f;

    /// <summary>寒冷减速硬顶。</summary>
    public const float MaxChillSlowPercent = 0.70f;

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

    /// <summary>寒冰增幅：每级为寒冷减速额外 +5%（可叠加，总减速不超过 70%）。</summary>
    public static float GetIceAmpSlowBonus(int level)
    {
        int lvl = Mathf.Clamp(level, 1, ModulePricing.MaxAttackLevel);
        return 0.05f * lvl;
    }

    public static int GetSparkDamage(int level)
    {
        switch (Mathf.Clamp(level, 1, ModulePricing.MaxAttackLevel))
        {
            case 1: return 5;
            case 2: return 10;
            case 3: return 15;
            case 4: return 20;
            default: return 30;
        }
    }

    public static float GetSparkBurnDuration(int level)
    {
        int lvl = Mathf.Clamp(level, 1, ModulePricing.MaxAttackLevel);
        return 2f + 0.5f * (lvl - 1);
    }

    public static int GetSparkEnergyCapacity(int level) => 5;

    public static float GetSparkFireInterval(int level) => BaseFireInterval;

    public static int GetSparkEnergyPerShot(int level = 1) => 1;

    public static float GetHeatwaveBurnDuration(int level)
    {
        switch (Mathf.Clamp(level, 1, ModulePricing.MaxAttackLevel))
        {
            case 1:
            case 2:
            case 3: return 2f;
            default: return 3f;
        }
    }

    public static float GetHeatwaveFireInterval(int level)
    {
        switch (Mathf.Clamp(level, 1, ModulePricing.MaxAttackLevel))
        {
            case 1: return 5f;
            case 2: return 4.5f;
            case 3:
            case 4: return 4f;
            default: return 3f;
        }
    }

    public static float GetFrostFreezeChillDuration(int level) => GetHeatwaveBurnDuration(level);

    public static float GetFrostFreezeFireInterval(int level) => GetHeatwaveFireInterval(level);

    /// <summary>烈焰墙：固定储能上限。</summary>
    public static int GetFlameWallEnergyCapacity(int level) => 30;

    /// <summary>穿过火墙的瞬时伤害：5/10/15/29/50。</summary>
    public static int GetFlameWallDamage(int level)
    {
        switch (Mathf.Clamp(level, 1, ModulePricing.MaxAttackLevel))
        {
            case 1: return 5;
            case 2: return 10;
            case 3: return 15;
            case 4: return 29;
            default: return 50;
        }
    }

    /// <summary>穿过火墙挂烧时长：2/2/3/4/5 秒。</summary>
    public static float GetFlameWallBurnDuration(int level)
    {
        switch (Mathf.Clamp(level, 1, ModulePricing.MaxAttackLevel))
        {
            case 1:
            case 2: return 2f;
            case 3: return 3f;
            case 4: return 4f;
            default: return 5f;
        }
    }

    /// <summary>火墙维持时每秒连续耗能：2/2/2/2/4。</summary>
    public static float GetFlameWallEnergyDrainPerSecond(int level)
    {
        int lvl = Mathf.Clamp(level, 1, ModulePricing.MaxAttackLevel);
        return lvl >= 5 ? 4f : 2f;
    }

    public static int GetArcaneMissileDamage(int level)
    {
        switch (Mathf.Clamp(level, 1, ModulePricing.MaxAttackLevel))
        {
            case 1: return 10;
            case 2: return 20;
            case 3: return 30;
            case 4: return 50;
            default: return 80;
        }
    }

    public static int GetArcaneMissileEnergyCapacity(int level = 1) => 5;

    public static int GetArcaneMissileEnergyPerShot(int level = 1) => 1;

    public static float GetArcaneMissileFireInterval(int level = 1) => 1f / 5f;

    /// <summary>满储能理论持续 DPS（伤÷射间隔）；爆发/控场类为 0，便于表里手改。</summary>
    public static float GetFullEnergyDps(ModuleType type, int level)
    {
        int lv = Mathf.Clamp(level, 1, ModulePricing.MaxAttackLevel);
        switch (type)
        {
            case ModuleType.ArcaneMissile:
                return GetArcaneMissileDamage(lv) / GetArcaneMissileFireInterval(lv);
            case ModuleType.Spark:
                return GetSparkDamage(lv) / GetSparkFireInterval(lv);
            case ModuleType.IceLaser:
                return GetIceDamage(lv) / GetIceFireInterval(lv);
            case ModuleType.Projectile:
                return GetDamagePerShot(lv) / GetFireInterval(lv);
            case ModuleType.LaserCannon:
            {
                int dmg = GetLaserCannonDamage(lv);
                float interval = 1f;
                int shots = 20 / 5;
                float cycle = shots * interval;
                return cycle > 0.0001f ? (shots * dmg) / cycle : 0f;
            }
            case ModuleType.Bomb:
            {
                int dmg = GetBombDamage(lv);
                float interval = GetBombFireInterval(lv);
                int shots = GetBombEnergyCapacity(lv) / 5;
                float cycle = shots * interval;
                return cycle > 0.0001f ? (shots * dmg) / cycle : 0f;
            }
            case ModuleType.FrostCannon:
            {
                int dmg = GetFrostCannonDamage(lv);
                float interval = GetFrostCannonFireInterval(lv);
                int shots = GetFrostCannonEnergyCapacity(lv) / GetFrostCannonEnergyPerShot(lv);
                float cycle = shots * interval;
                return cycle > 0.0001f ? (shots * dmg) / cycle : 0f;
            }
            default:
                return 0f;
        }
    }

    public static int GetMinerEnergyCost(int level) => 10;

    public static int GetMinerGoldAmount(int level)
    {
        switch (Mathf.Clamp(level, 1, 3))
        {
            case 1: return 5;
            case 2: return 10;
            default: return 20;
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
            case ModuleType.IceAmp: return new Color(0.55f, 0.9f, 1f, 1f);
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
            case ModuleType.LaserCannon: return new Color(1f, 0.15f, 0.12f, 1f);
            case ModuleType.FrostFreeze: return new Color(0.45f, 0.85f, 1f, 1f);
            case ModuleType.ArcaneMissile: return new Color(0.72f, 0.35f, 1f, 1f);
            case ModuleType.FlameWall: return new Color(1f, 0.45f, 0.08f, 1f);
            case ModuleType.FlameBlessing: return new Color(1f, 0.55f, 0.15f, 1f);
            case ModuleType.Purify: return new Color(0.8f, 0.95f, 0.9f, 1f);
            case ModuleType.FrostMushroom: return new Color(0.55f, 0.9f, 1f, 1f);
            case ModuleType.FrostBomb: return new Color(0.4f, 0.8f, 1f, 1f);
            case ModuleType.FrostCannon: return new Color(0.6f, 0.92f, 1f, 1f);
            default: return Color.gray;
        }
    }

    public static int GetLaserCannonDamage(int level)
    {
        switch (Mathf.Clamp(level, 1, ModulePricing.MaxAttackLevel))
        {
            case 1: return 30;
            case 2: return 60;
            case 3: return 100;
            case 4: return 150;
            default: return 200;
        }
    }

    public static int GetFrostBombEnergyCapacity(int level = 1) => 1;

    public static int GetFrostBombEnergyPerShot(int level = 1) => 1;

    public static float GetFrostBombFireInterval(int level = 1) => 5f;

    public static float GetFrostBombRadius(int level)
    {
        return GetBombRadius(level) * 0.5f;
    }

    /// <summary>霜环持续时间：2/3/4/5/5 秒。</summary>
    public static float GetFrostBombRingDuration(int level)
    {
        switch (Mathf.Clamp(level, 1, ModulePricing.MaxAttackLevel))
        {
            case 1: return 2f;
            case 2: return 3f;
            case 3: return 4f;
            default: return 5f;
        }
    }

    public static int GetFrostCannonDamage(int level)
    {
        switch (Mathf.Clamp(level, 1, ModulePricing.MaxAttackLevel))
        {
            case 1: return 20;
            case 2: return 40;
            case 3: return 80;
            case 4: return 100;
            default: return 150;
        }
    }

    public static int GetFrostCannonEnergyCapacity(int level = 1) => 5;

    public static int GetFrostCannonEnergyPerShot(int level = 1) => 5;

    public static float GetFrostCannonFireInterval(int level = 1) => 1f;
}
