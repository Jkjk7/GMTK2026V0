using UnityEngine;

/// <summary>One cached pixel atlas for the hourglass, its sand levels and six reusable grains.</summary>
public static class PixelHourglassSprites
{
    public const int Width = 48;
    public const int Height = 64;
    const int Levels = 32;
    static Sprite _frame, _reflections, _grain;
    static readonly Sprite[] Upper = new Sprite[Levels + 1];
    static readonly Sprite[] Lower = new Sprite[Levels + 1];
    static readonly Color32 Ink = C(10, 17, 29);
    static readonly Color32 Bronze = C(103, 66, 45);
    static readonly Color32 Brass = C(186, 126, 61);
    static readonly Color32 Gold = C(241, 187, 87);
    static readonly Color32 Light = C(255, 229, 151);

    public static Sprite Frame { get { Ensure(); return _frame; } }
    public static Sprite Reflections { get { Ensure(); return _reflections; } }
    public static Sprite Grain { get { Ensure(); return _grain; } }
    public static Sprite Sand(bool upper, float amount)
    {
        Ensure();
        int level = amount <= 0f ? 0 : Mathf.Clamp(Mathf.CeilToInt(amount * Levels), 1, Levels);
        return upper ? Upper[level] : Lower[level];
    }

    static void Ensure()
    {
        if (_frame != null) return;
        const int columns = Levels + 1;
        var texture = new Texture2D(Width * columns, Height * 3, TextureFormat.RGBA32, false);
        texture.name = "PixelHourglassAtlas";
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        var atlas = new Color32[texture.width * texture.height];
        for (int level = 0; level <= Levels; level++)
        {
            Copy(atlas, texture.width, level, 0, MakeSand(true, level / (float)Levels));
            Copy(atlas, texture.width, level, 1, MakeSand(false, level / (float)Levels));
        }
        Copy(atlas, texture.width, 0, 2, MakeFrame());
        Copy(atlas, texture.width, 1, 2, MakeReflections());
        var grain = new Color32[Width * Height];
        grain[32 * Width + 24] = C(255, 242, 174);
        grain[31 * Width + 24] = Gold;
        Copy(atlas, texture.width, 2, 2, grain);
        texture.SetPixels32(atlas);
        texture.Apply(false, false);
        _frame = SpriteAt(texture, 0, 2, "PixelHourglassFrame");
        _reflections = SpriteAt(texture, 1, 2, "PixelHourglassReflections");
        _grain = Sprite.Create(texture, new Rect(2 * Width + 24, 2 * Height + 31, 1, 2),
            new Vector2(.5f, .5f), 1, 0, SpriteMeshType.FullRect);
        _grain.name = "PixelHourglassGrain";
        for (int level = 0; level <= Levels; level++)
        {
            Upper[level] = SpriteAt(texture, level, 0, "PixelHourglassUpper_" + level);
            Lower[level] = SpriteAt(texture, level, 1, "PixelHourglassLower_" + level);
        }
    }

    static Color32[] MakeFrame()
    {
        var p = new Color32[Width * Height];
        // Narrow cyan neck and broad glass bowls, with hard stepped edges.
        for (int y = 10; y <= 54; y++)
        {
            int half = HalfWidth(y);
            int left = 24 - half, right = 23 + half;
            for (int x = left; x <= right; x++)
            {
                int edge = Mathf.Min(x - left, right - x);
                Color32 c = C(21, 48, 62);
                if (edge == 0 || y == 10 || y == 54) c = Ink;
                else if (edge == 1) c = x < 24 ? C(99, 205, 204) : C(46, 115, 137);
                else if (edge == 2) c = C(36, 83, 98);
                p[y * Width + x] = c;
            }
        }
        // Turned brass columns and their dark recesses.
        for (int side = 0; side < 2; side++)
        {
            int x = side == 0 ? 5 : 39;
            Fill(p, x - 1, 9, 6, 46, Ink);
            Fill(p, x, 10, 4, 44, Bronze);
            Fill(p, x, 11, 1, 42, Gold);
            Fill(p, x + 1, 11, 1, 42, Brass);
            Fill(p, x - 1, 12, 6, 3, Ink);
            Fill(p, x, 13, 4, 1, Gold);
            Fill(p, x - 1, 49, 6, 3, Ink);
            Fill(p, x, 50, 4, 1, Gold);
        }
        for (int y = 3; y <= 57; y += 54)
        {
            Fill(p, 5, y - 1, 38, 7, Ink);
            Fill(p, 3, y + 1, 42, 3, Ink);
            Fill(p, 6, y, 36, 5, Bronze);
            Fill(p, 4, y + 1, 40, 2, Brass);
            Fill(p, 6, y + 4, 36, 1, Light);
            Fill(p, 8, y + 3, 32, 1, Gold);
            Fill(p, 9, y, 30, 1, C(65, 42, 36));
        }
        Fill(p, 9, 1, 6, 2, Ink);
        Fill(p, 33, 1, 6, 2, Ink);
        Fill(p, 11, 2, 3, 1, Brass);
        Fill(p, 34, 2, 3, 1, Brass);
        // Small inset turquoise crown, echoing the battle UI palette.
        Fill(p, 21, 56, 6, 7, Ink);
        Fill(p, 22, 57, 4, 5, Bronze);
        Fill(p, 23, 58, 2, 3, C(71, 177, 183));
        p[60 * Width + 23] = C(167, 245, 224);
        return p;
    }

    static Color32[] MakeReflections()
    {
        var p = new Color32[Width * Height];
        Fill(p, 14, 47, 1, 5, C(145, 231, 221));
        Fill(p, 15, 44, 1, 3, C(79, 171, 185));
        Fill(p, 32, 14, 1, 5, C(93, 194, 197));
        Fill(p, 31, 19, 1, 3, C(54, 128, 153));
        return p;
    }

    static Color32[] MakeSand(bool upper, float amount)
    {
        var p = new Color32[Width * Height];
        if (amount <= 0f) return p;
        int level = upper ? 34 + Mathf.CeilToInt(19f * Mathf.Sqrt(amount))
            : 11 + Mathf.CeilToInt(18f * amount);
        for (int y = upper ? 35 : 12; y <= (upper ? 52 : 29); y++)
        {
            int half = HalfWidth(y) - 2;
            for (int x = 24 - half; x <= 23 + half; x++)
            {
                int distance = Mathf.Abs(x - 24);
                int surface = upper ? level - (distance < 3 ? 1 : 0)
                    : Mathf.Min(29, level + Mathf.Max(0, 3 - distance / 2));
                if (y > surface) continue;
                Color32 color = y == surface ? Light : Gold;
                if (y < surface - 1 && (x + y * 3) % 11 == 0) color = Brass;
                if (y < (upper ? 38 : 14)) color = Brass;
                p[y * Width + x] = color;
            }
        }
        return p;
    }

    static int HalfWidth(int y) => Mathf.Clamp(Mathf.Abs(y - 32) * 3 / 4, 2, 14);
    static void Fill(Color32[] p, int x, int y, int width, int height, Color32 color)
    {
        for (int row = y; row < y + height; row++)
            for (int col = x; col < x + width; col++) p[row * Width + col] = color;
    }
    static void Copy(Color32[] atlas, int stride, int col, int row, Color32[] pixels)
    {
        for (int y = 0; y < Height; y++)
            System.Array.Copy(pixels, y * Width, atlas, (row * Height + y) * stride + col * Width, Width);
    }
    static Sprite SpriteAt(Texture2D texture, int col, int row, string name)
    {
        Sprite sprite = Sprite.Create(texture, new Rect(col * Width, row * Height, Width, Height),
            new Vector2(.5f, .5f), Height, 0, SpriteMeshType.FullRect);
        sprite.name = name;
        return sprite;
    }
    static Color32 C(byte r, byte g, byte b) => new Color32(r, g, b, 255);
}
