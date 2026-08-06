/// <summary>
/// 棋盘格子附魔效果（一格一种）。
/// </summary>
public enum CellEnchant
{
    None = 0,
    Flame = 1,
    DamageUp = 2,
    Frost = 3,
    Shrink = 4,
    // 5 曾为 Cooldown，已删除；运行时见 CellEnchantRules.Normalize
    Weak = 6
}
