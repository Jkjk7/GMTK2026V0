using UnityEngine;

/// <summary>
/// A small, shared pixel atlas. Built once on first use; all sprites are exactly
/// one world unit, have centered pivots, and retain hard, unfiltered pixel edges.
/// Symbols are drawn in integer pixels so the same art reads in cards and on tiles.
/// </summary>
public static class PixelArtSprites
{
    public const int ModulePixelSize = 32;
    const int ModuleCount = 27;
    const int Columns = 9;
    static Sprite[,] _modules;

    static readonly Color32 Ink = C(22, 27, 43);
    static readonly Color32 Recess = C(37, 46, 62);
    static readonly Color32 Steel = C(90, 112, 130);
    static readonly Color32 Silver = C(171, 200, 204);
    static readonly Color32 Light = C(255, 239, 193);
    static readonly Color32 Brass = C(167, 110, 58);
    static readonly Color32 Gold = C(236, 182, 90);
    static readonly Color32 Fire = C(249, 112, 55);
    static readonly Color32 Ember = C(181, 57, 53);
    static readonly Color32 Ice = C(89, 201, 224);
    static readonly Color32 PaleIce = C(188, 247, 239);
    static readonly Color32 Arcane = C(166, 123, 224);
    static readonly Color32 PaleArcane = C(227, 190, 249);
    static readonly Color32 Mint = C(113, 215, 173);

    public static bool HasModule(ModuleType type) => (uint)(int)type < ModuleCount;

    public static Sprite Module(ModuleType type, int clockwiseTurns = 0)
    {
        if (!HasModule(type)) return PrototypeSprites.Square;
        // Native sprites can be destroyed between play sessions when domain
        // reload is disabled, while this managed cache remains populated.
        if (_modules == null || _modules[0, 0] == null) BuildModules();
        return _modules[(int)type, ((clockwiseTurns % 4) + 4) % 4];
    }

    static Color32 C(byte r, byte g, byte b) => new Color32(r, g, b, 255);

    static void BuildModules()
    {
        const int size = ModulePixelSize;
        // Every orientation is baked into this single shared texture, never generated per frame.
        var atlas = new Texture2D(Columns * size, 12 * size, TextureFormat.RGBA32, false);
        atlas.name = "Clockwork_Module_PixelAtlas";
        atlas.filterMode = FilterMode.Point;
        atlas.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color32[atlas.width * atlas.height];
        _modules = new Sprite[ModuleCount, 4];
        for (int i = 0; i < ModuleCount; i++)
        {
            var tile = new Tile();
            DrawBase(tile, Accent((ModuleType)i));
            DrawSymbol(tile, (ModuleType)i);
            for (int turn = 0; turn < 4; turn++)
            {
                int index = i + turn * ModuleCount;
                int ox = index % Columns * size;
                int oy = index / Columns * size;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        int sx = x, sy = y;
                        for (int r = 0; r < turn; r++)
                        {
                            int oldX = sx;
                            sx = size - 1 - sy;
                            sy = oldX;
                        }
                        pixels[(oy + y) * atlas.width + ox + x] = tile.Pixels[sy * size + sx];
                    }
                var sprite = Sprite.Create(atlas, new Rect(ox, oy, size, size),
                    new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect);
                sprite.name = "PixelModule_" + (ModuleType)i + "_" + turn;
                _modules[i, turn] = sprite;
            }
        }
        atlas.SetPixels32(pixels);
        atlas.Apply(false, false);
    }

    static Color32 Accent(ModuleType type)
    {
        switch (type)
        {
            case ModuleType.IceLaser: case ModuleType.IceAmp: case ModuleType.FrostFreeze:
            case ModuleType.FrostBomb: case ModuleType.FrostCannon: case ModuleType.FrostMushroom:
                return Ice;
            case ModuleType.BlackHole: case ModuleType.Portal: case ModuleType.ArcaneMissile:
            case ModuleType.Fusion: case ModuleType.Fission: case ModuleType.Splitter: case ModuleType.Surprise:
                return Arcane;
            case ModuleType.FlameAmp: case ModuleType.Spark: case ModuleType.FireEnchant:
            case ModuleType.Heatwave: case ModuleType.FlameWall: case ModuleType.FlameBlessing:
            case ModuleType.Projectile: case ModuleType.LaserCannon: case ModuleType.Bomb:
                return Fire;
            case ModuleType.Relay: case ModuleType.Purify: return Mint;
            default: return Gold;
        }
    }

    static void DrawBase(Tile p, Color32 accent)
    {
        // Cut corners, two-tone brass bevel, inset navy face, and four rivets.
        p.R(2, 0, 28, 32, Ink); p.R(0, 2, 32, 28, Ink);
        p.R(2, 2, 28, 28, Brass); p.R(3, 3, 26, 26, Gold);
        p.R(3, 3, 26, 2, Brass); p.R(27, 3, 2, 25, Brass);
        p.R(5, 5, 22, 22, Ink); p.R(6, 6, 20, 20, Recess);
        p.R(4, 27, 23, 1, Light); p.R(4, 4, 1, 23, Gold);
        p.R(2, 13, 2, 6, accent); p.R(28, 13, 2, 6, accent);
        p.R(3, 3, 2, 2, Ink); p.R(27, 3, 2, 2, Ink);
        p.R(3, 27, 2, 2, Ink); p.R(27, 27, 2, 2, Ink);
        p.R(3, 4, 1, 1, Silver); p.R(27, 28, 1, 1, Light);
    }

    static void DrawSymbol(Tile p, ModuleType type)
    {
        switch (type)
        {
            case ModuleType.Redirector:
                p.L(7, 14, 16, 14, 7, Ink); p.L(16, 14, 16, 24, 7, Ink);
                p.L(7, 14, 16, 14, 3, Ice); p.L(16, 14, 16, 24, 3, Ice);
                Arrow(p, 16, 22, 0, PaleIce); p.R(7, 14, 6, 1, Light); break;
            case ModuleType.Projectile:
                Turret(p, Fire, false); p.R(14, 19, 4, 4, Light); break;
            case ModuleType.Bomb:
                Bomb(p, Fire); p.L(19, 23, 21, 25, 1, Gold); Star(p, 23, 24, Gold); break;
            case ModuleType.IceLaser:
                p.B(10, 7, 12, 6, Steel); p.R(13, 13, 6, 6, Silver);
                Snow(p, 16, 20, 6, Ice); p.R(15, 19, 3, 3, PaleIce); break;
            case ModuleType.Miner:
                p.L(11, 8, 22, 22, 5, Ink); p.L(11, 8, 22, 22, 2, Brass);
                p.L(9, 22, 16, 25, 5, Ink); p.L(16, 25, 24, 19, 5, Ink);
                p.L(9, 23, 16, 24, 2, Silver); p.L(16, 24, 24, 19, 2, Light);
                p.B(6, 7, 7, 5, Gold); p.R(8, 10, 3, 1, Light); break;
            case ModuleType.BlackHole:
                p.E(16, 16, 10, 8, Ink); p.E(16, 16, 9, 7, Arcane);
                p.E(16, 16, 6, 5, Ink); p.L(7, 14, 13, 11, 2, PaleArcane);
                p.L(18, 21, 25, 18, 2, PaleArcane); p.R(14, 15, 5, 3, Recess);
                Star(p, 9, 24, Gold); break;
            case ModuleType.FlameAmp:
                Flame(p, 15, 16, 1); p.B(8, 7, 16, 4, Brass);
                Arrow(p, 24, 20, 0, Gold); break;
            case ModuleType.IceAmp:
                p.B(8, 7, 16, 4, Steel); Snow(p, 14, 19, 6, Ice);
                Arrow(p, 24, 20, 0, PaleIce); break;
            case ModuleType.Spark:
                p.B(8, 7, 16, 5, Brass); p.B(11, 12, 10, 4, Steel);
                p.L(16, 15, 19, 21, 5, Ink); p.L(18, 21, 14, 21, 3, Gold);
                p.L(16, 23, 13, 18, 3, Fire); p.L(13, 18, 18, 18, 2, Light);
                Star(p, 24, 24, Fire); break;
            case ModuleType.Splitter:
                p.L(16, 7, 16, 17, 6, Ink); p.L(7, 17, 25, 17, 6, Ink);
                p.L(16, 7, 16, 17, 2, Arcane); p.L(7, 17, 25, 17, 2, PaleArcane);
                Arrow(p, 8, 17, 3, PaleArcane); Arrow(p, 24, 17, 1, PaleArcane); break;
            case ModuleType.Portal:
                p.E(16, 17, 9, 11, Ink); p.E(16, 17, 7, 9, Arcane);
                p.E(16, 17, 4, 7, Ink); p.L(11, 14, 11, 20, 1, PaleArcane);
                p.R(14, 24, 4, 1, PaleArcane); p.B(8, 5, 16, 4, Brass);
                p.R(16, 14, 3, 6, Recess); Star(p, 23, 24, Gold); break;
            case ModuleType.Relay:
                p.B(9, 7, 14, 18, Steel); p.R(12, 10, 8, 12, Ink);
                p.L(17, 21, 14, 16, 3, Mint); p.L(14, 16, 18, 16, 3, Mint);
                p.L(18, 16, 15, 11, 3, PaleIce); p.R(13, 25, 6, 2, Gold); break;
            case ModuleType.Accelerator:
                Chevron(p, 10, Gold); Chevron(p, 17, Light);
                p.R(7, 8, 5, 2, Brass); p.R(8, 23, 4, 1, Gold); break;
            case ModuleType.Fusion:
                Orb(p, 8, 22, 3, Arcane); Orb(p, 24, 22, 3, Arcane);
                p.L(9, 18, 14, 14, 2, PaleArcane); p.L(23, 18, 18, 14, 2, PaleArcane);
                Orb(p, 16, 10, 5, Gold); p.R(14, 11, 2, 2, Light); break;
            case ModuleType.Fission:
                Orb(p, 16, 21, 5, Gold); p.L(15, 18, 9, 13, 2, PaleArcane);
                p.L(18, 18, 23, 13, 2, PaleArcane); p.L(16, 18, 16, 10, 1, Arcane);
                Orb(p, 8, 9, 3, Arcane); Orb(p, 24, 9, 3, Arcane); Orb(p, 16, 7, 2, PaleArcane); break;
            case ModuleType.FireEnchant:
                p.B(7, 7, 18, 5, Brass); p.L(8, 11, 12, 15, 2, Gold);
                p.L(24, 11, 20, 15, 2, Gold); Flame(p, 16, 18, 1); break;
            case ModuleType.Surprise:
                p.B(7, 8, 18, 13, Arcane); p.B(6, 19, 20, 4, PaleArcane);
                p.R(14, 9, 4, 13, Gold); p.L(15, 23, 11, 26, 2, Fire);
                p.L(17, 23, 22, 26, 2, Gold); p.R(10, 24, 3, 2, PaleArcane); break;
            case ModuleType.Heatwave:
                for (int i = 0; i < 3; i++)
                {
                    int x = 9 + i * 7;
                    p.L(x, 7, x + 2, 12, 3, Ember); p.L(x + 2, 12, x, 18, 3, Fire);
                    p.L(x, 18, x + 2, 24, 3, Gold);
                }
                p.R(7, 6, 19, 2, Brass); break;
            case ModuleType.LaserCannon:
                Turret(p, Fire, true); p.R(15, 23, 2, 4, Light);
                p.R(10, 25, 2, 2, Fire); p.R(21, 25, 2, 2, Fire); break;
            case ModuleType.FrostFreeze:
                Snow(p, 16, 16, 9, Ice); p.R(15, 14, 3, 4, PaleIce);
                p.R(7, 7, 3, 2, Silver); p.R(23, 23, 2, 3, Light); break;
            case ModuleType.ArcaneMissile:
                p.L(9, 8, 20, 21, 7, Ink); p.L(11, 10, 20, 21, 3, Arcane);
                p.L(14, 14, 21, 22, 2, PaleArcane); p.L(21, 22, 22, 25, 2, Light);
                p.L(9, 8, 8, 13, 2, Fire); p.L(9, 8, 14, 9, 2, Gold);
                Star(p, 24, 12, Arcane); break;
            case ModuleType.FlameWall:
                p.B(6, 6, 20, 5, Brass); p.B(6, 12, 5, 6, Steel);
                p.B(21, 12, 5, 6, Steel); Flame(p, 10, 19, 0);
                Flame(p, 16, 18, 1); Flame(p, 23, 19, 0); break;
            case ModuleType.FlameBlessing:
                p.L(9, 12, 16, 6, 4, Ink); p.L(16, 6, 23, 12, 4, Ink);
                p.L(9, 12, 16, 8, 2, Gold); p.L(16, 8, 23, 12, 2, Gold);
                Flame(p, 16, 19, 0); p.L(7, 21, 10, 23, 1, Light);
                p.L(22, 23, 25, 21, 1, Light); break;
            case ModuleType.Purify:
                p.L(10, 9, 21, 23, 5, Ink); p.L(10, 9, 21, 23, 2, Silver);
                Star(p, 20, 22, PaleIce); Star(p, 8, 21, Mint); Star(p, 23, 10, Gold); break;
            case ModuleType.FrostMushroom:
                p.B(12, 6, 8, 10, Silver); p.R(14, 7, 3, 8, PaleIce);
                p.E(16, 18, 11, 8, Ink); p.E(16, 19, 9, 6, Ice);
                p.R(6, 15, 20, 3, Ink); p.R(8, 16, 16, 1, PaleIce);
                p.R(11, 21, 3, 3, PaleIce); p.R(20, 19, 3, 3, Silver); break;
            case ModuleType.FrostBomb:
                Bomb(p, Ice); Snow(p, 16, 13, 5, PaleIce);
                p.L(19, 23, 22, 25, 1, Ice); p.R(21, 25, 4, 1, PaleIce); break;
            case ModuleType.FrostCannon:
                Turret(p, Ice, true); p.L(11, 20, 16, 25, 2, PaleIce);
                p.L(21, 20, 16, 25, 2, Ice); break;
        }
    }

    static void Turret(Tile p, Color32 accent, bool heavy)
    {
        p.B(7, 6, 18, 5, Steel); p.R(9, 9, 14, 1, Silver);
        p.E(16, 14, 8, 6, Ink); p.E(16, 14, 6, 4, Brass);
        p.B(heavy ? 11 : 13, 13, heavy ? 10 : 6, 12, Steel);
        p.R(heavy ? 13 : 14, 16, heavy ? 6 : 3, 7, accent);
        p.R(heavy ? 10 : 12, 23, heavy ? 12 : 8, 3, Ink);
        p.R(heavy ? 12 : 14, 24, heavy ? 8 : 4, 1, Light);
        p.R(9, 13, 2, 2, accent); p.R(22, 13, 2, 2, accent);
    }

    static void Bomb(Tile p, Color32 accent)
    {
        p.E(16, 13, 9, 9, Ink); p.E(16, 13, 7, 7, Steel);
        p.E(17, 12, 6, 6, accent); p.L(10, 16, 12, 18, 2, Light);
        p.B(13, 21, 7, 4, Brass); p.R(15, 23, 3, 1, Gold);
    }

    static void Flame(Tile p, int x, int y, int large)
    {
        int w = 3 + large;
        p.L(x - w, y - 3, x - w + 1, y + 3, 3, Ink);
        p.L(x + w, y - 3, x + 1, y + 7, 3, Ink);
        p.E(x, y - 2, w + 2, 5, Ink); p.E(x, y - 2, w, 4, Ember);
        p.L(x - w + 1, y - 1, x, y + 4, 3, Fire);
        p.L(x + 2, y, x + 1, y + 5, 2, Fire);
        p.L(x, y - 4, x + 1, y + 1, 2, Gold); p.R(x, y - 4, 2, 3, Light);
    }

    static void Snow(Tile p, int x, int y, int r, Color32 color)
    {
        p.L(x, y - r, x, y + r, 3, Ink); p.L(x - r, y, x + r, y, 3, Ink);
        p.L(x - r + 1, y - r + 1, x + r - 1, y + r - 1, 3, Ink);
        p.L(x - r + 1, y + r - 1, x + r - 1, y - r + 1, 3, Ink);
        p.L(x, y - r, x, y + r, 1, color); p.L(x - r, y, x + r, y, 1, color);
        p.L(x - r + 1, y - r + 1, x + r - 1, y + r - 1, 1, color);
        p.L(x - r + 1, y + r - 1, x + r - 1, y - r + 1, 1, color);
        if (r >= 6)
        {
            for (int sign = -1; sign <= 1; sign += 2)
            {
                p.L(x, y + sign * (r - 2), x - 2, y + sign * r, 1, color);
                p.L(x, y + sign * (r - 2), x + 2, y + sign * r, 1, color);
                p.L(x + sign * (r - 2), y, x + sign * r, y - 2, 1, color);
                p.L(x + sign * (r - 2), y, x + sign * r, y + 2, 1, color);
            }
        }
        p.R(x - 1, y - 1, 3, 3, PaleIce);
    }

    static void Star(Tile p, int x, int y, Color32 color)
    {
        p.L(x - 3, y, x + 3, y, 1, color); p.L(x, y - 3, x, y + 3, 1, color);
        p.R(x - 1, y - 1, 3, 3, color); p.R(x, y, 1, 1, Light);
    }

    static void Orb(Tile p, int x, int y, int r, Color32 color)
    {
        p.E(x, y, r + 1, r + 1, Ink); p.E(x, y, r, r, color);
        p.R(x - 1, y + 1, 2, 1, Light);
    }

    static void Chevron(Tile p, int x, Color32 color)
    {
        p.L(x, 9, x + 6, 16, 5, Ink); p.L(x + 6, 16, x, 23, 5, Ink);
        p.L(x, 9, x + 6, 16, 2, color); p.L(x + 6, 16, x, 23, 2, color);
    }

    static void Arrow(Tile p, int x, int y, int direction, Color32 color)
    {
        int dx = direction == 1 ? 1 : direction == 3 ? -1 : 0;
        int dy = direction == 0 ? 1 : direction == 2 ? -1 : 0;
        p.L(x, y, x - dx * 4 - dy * 3, y - dy * 4 + dx * 3, 2, color);
        p.L(x, y, x - dx * 4 + dy * 3, y - dy * 4 - dx * 3, 2, color);
    }

    sealed class Tile
    {
        public readonly Color32[] Pixels = new Color32[ModulePixelSize * ModulePixelSize];
        public void R(int x, int y, int w, int h, Color32 color)
        {
            for (int py = Mathf.Max(0, y); py < Mathf.Min(ModulePixelSize, y + h); py++)
                for (int px = Mathf.Max(0, x); px < Mathf.Min(ModulePixelSize, x + w); px++)
                    Pixels[py * ModulePixelSize + px] = color;
        }
        public void B(int x, int y, int w, int h, Color32 fill)
        {
            R(x - 1, y - 1, w + 2, h + 2, Ink); R(x, y, w, h, fill);
            R(x, y + h - 1, w, 1, Silver);
        }
        public void L(int x0, int y0, int x1, int y1, int width, Color32 color)
        {
            int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;
            while (true)
            {
                R(x0 - width / 2, y0 - width / 2, width, width, color);
                if (x0 == x1 && y0 == y1) break;
                int twice = 2 * err;
                if (twice >= dy) { err += dy; x0 += sx; }
                if (twice <= dx) { err += dx; y0 += sy; }
            }
        }
        public void E(int cx, int cy, int rx, int ry, Color32 color)
        {
            for (int y = -ry; y <= ry; y++)
                for (int x = -rx; x <= rx; x++)
                    if (x * x * ry * ry + y * y * rx * rx <= rx * rx * ry * ry)
                        R(cx + x, cy + y, 1, 1, color);
        }
    }
}
