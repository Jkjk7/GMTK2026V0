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

    /// <summary>查理寒冰激光塔：弱输出 + 减速。</summary>
    IceLaser = 3,

    /// <summary>比特币采矿机：能量换金币。</summary>
    Miner = 4
}
