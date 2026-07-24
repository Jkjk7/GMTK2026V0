/// <summary>
/// 可放置模块类型。手牌与放置逻辑用此枚举区分预制体。
/// </summary>
public enum ModuleType
{
    /// <summary>收束器：90° 弯道，可旋转。</summary>
    Redirector = 0,

    /// <summary>查理激光塔：吸能后向最近敌人发射激光。</summary>
    Projectile = 1,

    /// <summary>大卫炸弹塔：AOE 清潮。</summary>
    Bomb = 2,

    /// <summary>雪花发射塔：弧线雪花弹 + 寒冷减速。</summary>
    IceLaser = 3,

    /// <summary>比特币采矿机：能量换金币。</summary>
    Miner = 4,

    /// <summary>黑洞发射器：史诗控场，吸引敌人聚堆。</summary>
    BlackHole = 5,

    /// <summary>火焰增幅：场上提高灼烧伤害。</summary>
    FlameAmp = 6,

    /// <summary>火花发射塔：弧线火花弹挂烧。</summary>
    Spark = 7,

    /// <summary>分裂器：T 形一分二。</summary>
    Splitter = 8,

    /// <summary>传送门：成对传送。</summary>
    Portal = 9,

    /// <summary>中续器：吸能后刷新下一球寿命。</summary>
    Relay = 10,

    /// <summary>加速：球速 ×1.5（一次）。</summary>
    Accelerator = 11,

    /// <summary>核聚变：5 球合成 1 球。</summary>
    Fusion = 12,

    /// <summary>核裂变：≥5 能裂变为 5 颗默认球。</summary>
    Fission = 13,

    /// <summary>火附魔：种子灼烧附魔格。</summary>
    FireEnchant = 14,

    /// <summary>惊喜：种子随机附魔格。</summary>
    Surprise = 15,

    /// <summary>热浪：全屏灼烧攻击塔。</summary>
    Heatwave = 16
}
