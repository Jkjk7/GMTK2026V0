/// <summary>
/// 可放置模块类型。手牌与放置逻辑用此枚举区分预制体。
/// </summary>
public enum ModuleType
{
    /// <summary>收束器：90° 弯道，可旋转。</summary>
    Redirector = 0,

    /// <summary>查理激光塔：吸能后向最近敌人发射激光。</summary>
    Projectile = 1
}
