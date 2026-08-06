using UnityEngine;

/// <summary>地块附魔数值：战斗与悬停面板共用。</summary>
public static class CellEnchantRules
{
    public const float DamageUpMult = 1.5f;
    public const float ShrinkDamageMult = 0.5f;
    public const float WeakDamageMult = 0.8f;
    /// <summary>缩小附魔：射速翻倍 → 开火间隔 ×0.5。</summary>
    public const float ShrinkFireIntervalMult = 0.5f;
    /// <summary>虚弱附魔：开火间隔 ×1.2。</summary>
    public const float WeakIntervalMult = 1.2f;

    /// <summary>旧版冷却附魔枚举值；运行时视为无附魔。</summary>
    const int LegacyCooldownValue = 5;

    public static CellEnchant Normalize(CellEnchant enchant)
    {
        if ((int)enchant == LegacyCooldownValue)
        {
            return CellEnchant.None;
        }

        return enchant;
    }

    public static float GetDamageMultiplier(CellEnchant enchant)
    {
        switch (Normalize(enchant))
        {
            case CellEnchant.DamageUp: return DamageUpMult;
            case CellEnchant.Shrink: return ShrinkDamageMult;
            case CellEnchant.Weak: return WeakDamageMult;
            default: return 1f;
        }
    }

    /// <summary>开火间隔倍率：缩小 ×0.5，虚弱 ×1.2。</summary>
    public static float GetFireIntervalMultiplier(CellEnchant enchant)
    {
        switch (Normalize(enchant))
        {
            case CellEnchant.Shrink: return ShrinkFireIntervalMult;
            case CellEnchant.Weak: return WeakIntervalMult;
            default: return 1f;
        }
    }

    public static int ScaleDamage(int rawDamage, CellEnchant enchant)
    {
        if (rawDamage <= 0)
        {
            return 0;
        }

        float mult = GetDamageMultiplier(enchant);
        return Mathf.Max(1, Mathf.RoundToInt(rawDamage * mult));
    }

    public static float ScaleFireInterval(float interval, CellEnchant enchant) =>
        Mathf.Max(0.01f, interval * GetFireIntervalMultiplier(enchant));
}
