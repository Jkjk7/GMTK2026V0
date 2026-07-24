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
    Spark = 7
}
