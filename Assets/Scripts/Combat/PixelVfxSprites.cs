using UnityEngine;

/// <summary>Small, shared, hard-edged pixel textures. No gameplay RNG or per-spawn texture work.</summary>
public static class PixelVfxSprites
{
    static Sprite _burst;
    static Sprite _ring;
    static Sprite _chip;

    public static Sprite Burst => _burst != null ? _burst : (_burst = Build("PixelHitBurst", 0));
    public static Sprite Ring => _ring != null ? _ring : (_ring = Build("PixelMeltRing", 1));
    public static Sprite Chip => _chip != null ? _chip : (_chip = Build("PixelShard", 2));

    static Sprite Build(string name, int shape)
    {
        const int size = 24;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = name;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int dx = Mathf.Abs(x * 2 - 23);
                int dy = Mathf.Abs(y * 2 - 23);
                bool solid;
                if (shape == 0)
                    solid = (dx <= 3 && dy <= 21) || (dy <= 3 && dx <= 21)
                        || (Mathf.Abs(dx - dy) <= 2 && dx + dy <= 26);
                else if (shape == 1)
                {
                    int edge = Mathf.Max(dx, dy);
                    solid = edge >= 15 && edge <= 21 && dx + dy <= 32;
                }
                else
                    solid = dx + dy / 2 <= 15 && dx + dy >= 7 && x >= 4 && y >= 4;
                pixels[y * size + x] = solid ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            }
        texture.SetPixels32(pixels);
        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        sprite.name = name;
        return sprite;
    }
}
