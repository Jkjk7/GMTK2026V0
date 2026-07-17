using System;
using UnityEngine;

/// <summary>
/// 当前可售卖/可出现的模块目录。
/// 未来可扩展为卡池、稀有度权重；现阶段返回全部已实现类型。
/// </summary>
public static class ModuleCatalog
{
    static readonly ModuleType[] SellableTypes =
    {
        ModuleType.Redirector,
        ModuleType.Projectile
    };

    /// <summary>当前可进入商店随机池的模块种类。</summary>
    public static ModuleType[] GetSellableTypes()
    {
        return SellableTypes;
    }

    /// <summary>从当前池中均匀随机一种模块。</summary>
    public static ModuleType RollRandomType()
    {
        ModuleType[] pool = SellableTypes;
        if (pool == null || pool.Length == 0)
        {
            return ModuleType.Redirector;
        }

        return pool[UnityEngine.Random.Range(0, pool.Length)];
    }

    /// <summary>UI 显示名。</summary>
    public static string GetDisplayName(ModuleType type)
    {
        switch (type)
        {
            case ModuleType.Redirector: return "收束器";
            case ModuleType.Projectile: return "射弹塔";
            default: return type.ToString();
        }
    }

    /// <summary>UI 图标色。</summary>
    public static Color GetDisplayColor(ModuleType type)
    {
        switch (type)
        {
            case ModuleType.Redirector: return new Color(0.4f, 0.75f, 0.95f, 1f);
            case ModuleType.Projectile: return new Color(0.9f, 0.35f, 0.25f, 1f);
            default: return Color.gray;
        }
    }
}
