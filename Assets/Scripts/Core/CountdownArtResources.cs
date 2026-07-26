using System.Collections.Generic;
using UnityEngine;

/// <summary>Loads generated countdown art from Resources with centered runtime pivots.</summary>
public static class CountdownArtResources
{
    public const string BoardCellPath = "Countdown/board_cell";
    public const string BattleBackdropPath = "Countdown/battle_lane_backdrop";

    static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

    public static Sprite LoadSprite(string path, Sprite fallback)
    {
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
}
