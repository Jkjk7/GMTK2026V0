using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Loads generated countdown art from Resources with centered runtime pivots.
/// UseFormalArt=false 时模块/图标回退原型；环境背景可由 UseFormalEnvironmentArt 单独打开。
/// </summary>
public static class CountdownArtResources
{
    /// <summary>是否使用正式模块/图标贴图。false = 原型方块。</summary>
    public static bool UseFormalArt = false;

    /// <summary>是否使用战斗区/棋盘等环境背景正式贴图（不含模块）。</summary>
    public static bool UseFormalEnvironmentArt = true;

    public const string BoardCellPath = "Countdown/board_cell";
    public const string BattleBackdropPath = "Countdown/battle_lane_backdrop";
    public const string ModuleRootPath = "Countdown/Modules/";
    public const string HourglassFramePath = "Countdown/UI/hourglass_frame";
    public const string TimerPlaquePath = "Countdown/UI/timer_plaque";
    public const string RingOrnamentPath = "Countdown/Environment/countdown_ring_ornament";
    public const string PanelBackgroundPath = "Countdown/Environment/panel_background";
    public const string BoardFramePath = "Countdown/Environment/board_frame";
    public const string BoardStateOverlayPath = "Countdown/Environment/board_state_overlay";

    static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

    public static bool IsEnvironmentArtPath(string path) =>
        path == BoardCellPath
        || path == BattleBackdropPath
        || path == BoardFramePath
        || path == BoardStateOverlayPath
        || path == PanelBackgroundPath
        || path == RingOrnamentPath
        || path == TimerPlaquePath;

    public static bool IsAlwaysAllowedFormalPath(string path) =>
        path == HourglassFramePath
        || (UseFormalEnvironmentArt && IsEnvironmentArtPath(path));

    public static Sprite LoadSprite(string path, Sprite fallback)
    {
        if (!UseFormalArt && !IsAlwaysAllowedFormalPath(path))
        {
            return fallback;
        }

        if (Cache.TryGetValue(path, out Sprite cached) && cached != null)
        {
            return cached;
        }

        Texture2D texture = Resources.Load<Texture2D>(path);
        if (texture == null)
        {
            return fallback;
        }

        float pixelsPerUnit = Mathf.Max(1f, texture.height);
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit,
            0,
            SpriteMeshType.FullRect);
        sprite.name = texture.name + "_RuntimeCentered";
        Cache[path] = sprite;
        return sprite;
    }

    public static Vector3 FitScale(Sprite sprite, float width, float height)
    {
        Vector2 size = sprite != null ? sprite.bounds.size : Vector2.one;
        return new Vector3(
            width / Mathf.Max(0.001f, size.x),
            height / Mathf.Max(0.001f, size.y),
            1f);
    }

    public static Sprite LoadModuleSprite(ModuleType type)
    {
        if (!UseFormalArt)
        {
            return PrototypeSprites.Square;
        }

        string fileName;
        switch (type)
        {
            case ModuleType.Redirector: fileName = "module_redirector"; break;
            case ModuleType.Projectile: fileName = "module_projectile"; break;
            case ModuleType.Bomb: fileName = "module_bomb"; break;
            case ModuleType.IceLaser: fileName = "module_ice_laser"; break;
            case ModuleType.Miner: fileName = "module_miner"; break;
            case ModuleType.BlackHole: fileName = "module_black_hole"; break;
            case ModuleType.FlameAmp: fileName = "module_flame_amp"; break;
            case ModuleType.IceAmp: fileName = "module_ice_laser"; break; // 暂复用雪花图标
            case ModuleType.Spark: fileName = "module_spark"; break;
            case ModuleType.Splitter: fileName = "module_splitter"; break;
            case ModuleType.Portal: fileName = "module_portal"; break;
            case ModuleType.Relay: fileName = "module_relay"; break;
            case ModuleType.Accelerator: fileName = "module_accelerator"; break;
            case ModuleType.Fusion: fileName = "module_fusion"; break;
            case ModuleType.Fission: fileName = "module_fission"; break;
            case ModuleType.FireEnchant: fileName = "module_fire_enchant"; break;
            case ModuleType.Surprise: fileName = "module_surprise"; break;
            case ModuleType.Heatwave: fileName = "module_heatwave"; break;
            case ModuleType.FlameWall: return PrototypeSprites.Triangle;
            case ModuleType.FlameBlessing: return PrototypeSprites.Square;
            case ModuleType.Purify: return PrototypeSprites.Circle;
            case ModuleType.FrostMushroom: return PrototypeSprites.Circle;
            case ModuleType.LaserCannon: fileName = "module_laser_cannon"; break;
            case ModuleType.FrostFreeze: fileName = "module_frost_freeze"; break;
            case ModuleType.ArcaneMissile: fileName = "module_arcane_missile"; break;
            default: return PrototypeSprites.Square;
        }

        return LoadSprite(ModuleRootPath + fileName, PrototypeSprites.Square);
    }

    public static bool IsFormalModuleSprite(Sprite sprite)
    {
        return sprite != null && sprite != PrototypeSprites.Square;
    }
}
