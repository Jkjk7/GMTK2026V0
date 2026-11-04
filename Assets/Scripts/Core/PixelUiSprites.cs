using UnityEngine;

/// <summary>Quiet pixel surfaces: crisp edges and restrained contrast behind gameplay.</summary>
public static class PixelUiSprites
{
    static Sprite _panel, _cell, _backdrop, _frame, _lock, _empty;
    public static Sprite Panel => _panel != null ? _panel : (_panel = MakePanel(false));
    public static Sprite Frame => _frame != null ? _frame : (_frame = MakePanel(true));
    public static Sprite Cell => _cell != null ? _cell : (_cell = MakeCell());
    public static Sprite Backdrop => _backdrop != null ? _backdrop : (_backdrop = MakeBackdrop());
    public static Sprite Lock => _lock != null ? _lock : (_lock = MakeGlyph(false));
    public static Sprite EmptySlot => _empty != null ? _empty : (_empty = MakeGlyph(true));

    public static Sprite Resolve(string path)
    {
        switch (path)
        {
            case CountdownArtResources.BoardCellPath: return Cell;
            case CountdownArtResources.BattleBackdropPath: return Backdrop;
            case CountdownArtResources.PanelBackgroundPath:
            case CountdownArtResources.TimerPlaquePath: return Panel;
            case CountdownArtResources.BoardFramePath: return Frame;
            case CountdownArtResources.RingOrnamentPath: return PixelVfxSprites.Ring;
            default: return null;
        }
    }

    static Sprite MakePanel(bool frameOnly)
    {
        const int size = 32;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int edge = Mathf.Min(x, y, size - 1 - x, size - 1 - y);
                Color32 color = C(24, 32, 48);
                if (edge == 0) color = C(10, 15, 25);
                else if (edge == 1) color = y > 16 ? C(202, 155, 83) : C(107, 74, 48);
                else if (edge == 2) color = C(47, 58, 74);
                else if (y > 26) color = C(31, 42, 59);
                if (frameOnly && edge > 2) color.a = 0;
                if ((x < 2 || x > 29) && (y < 2 || y > 29)) color.a = 0;
                pixels[y * size + x] = color;
            }
        return Make(frameOnly ? "PixelFrame" : "PixelPanel", size, size, pixels, new Vector4(4, 4, 4, 4));
    }

    static Sprite MakeCell()
    {
        const int size = 32;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int edge = Mathf.Min(x, y, 31 - x, 31 - y);
                Color32 c = C(26, 37, 51);
                if (edge == 0) c = C(10, 18, 29);
                else if (edge == 1) c = y > 16 ? C(62, 80, 94) : C(19, 27, 39);
                else if ((x == 4 || x == 27) && (y == 4 || y == 27)) c = C(127, 104, 69);
                pixels[y * size + x] = c;
            }
        return Make("PixelBoardCell", size, size, pixels, Vector4.zero);
    }

    static Sprite MakeBackdrop()
    {
        const int width = 384, height = 48;
        var pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                Color32 c = y > 13 ? C(19, 27, 43) : C(28, 37, 51);
                if (y == 12 || y == 1) c = C(73, 67, 58);
                if (y == 11 || y == 2) c = C(12, 18, 29);
                if (y < 11 && x % 24 == 0) c = C(19, 27, 40);
                if (y > 20 && y < 38 && x % 48 > 34 && x % 48 < 38) c = C(26, 37, 53);
                if (y == 16 && x % 32 < 4) c = C(107, 85, 53);
                pixels[y * width + x] = c;
            }
        return Make("PixelBattleLane", width, height, pixels, Vector4.zero);
    }

    static Sprite MakeGlyph(bool empty)
    {
        const int size = 32;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                if (empty)
                {
                    if ((x == 16 && y >= 12 && y <= 20) || (y == 16 && x >= 12 && x <= 20))
                        pixels[y * size + x] = C(78, 96, 116);
                }
                else
                {
                    bool shackle = ((x == 10 || x == 21) && y >= 17 && y <= 22)
                        || (y == 23 && x >= 11 && x <= 20);
                    bool body = x >= 8 && x <= 23 && y >= 7 && y <= 17;
                    if (body || shackle) pixels[y * size + x] = C(145, 125, 91);
                    if (x >= 15 && x <= 16 && y >= 10 && y <= 14) pixels[y * size + x] = C(18, 26, 40);
                }
            }
        return Make(empty ? "PixelEmptySlot" : "PixelLock", size, size, pixels, Vector4.zero);
    }

    static Color32 C(byte r, byte g, byte b) => new Color32(r, g, b, 255);
    static Sprite Make(string name, int width, int height, Color32[] pixels, Vector4 border)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = name;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        var sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f),
            height, 0, SpriteMeshType.FullRect, border);
        sprite.name = name;
        return sprite;
    }
}
