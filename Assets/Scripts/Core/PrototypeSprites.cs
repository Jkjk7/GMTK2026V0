using UnityEngine;

/// <summary>
/// 运行时生成简单白底 Sprite，供占位色块/圆球使用。
/// 不依赖外部美术资源；原型阶段足够。
/// </summary>
public static class PrototypeSprites
{
    static Sprite s_Square;
    static Sprite s_Circle;

    /// <summary>
    /// 1x1 白色方块 Sprite（可被 SpriteRenderer.color 染色）。
    /// </summary>
    public static Sprite Square
    {
        get
        {
            if (s_Square == null)
            {
                Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                tex.filterMode = FilterMode.Point;
                s_Square = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            }

            return s_Square;
        }
    }

    /// <summary>
    /// 简单圆形 Sprite（用于光球）。
    /// </summary>
    public static Sprite Circle
    {
        get
        {
            if (s_Circle == null)
            {
                const int size = 32;
                Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                float r = (size - 1) * 0.5f;
                Vector2 center = new Vector2(r, r);
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), center);
                        tex.SetPixel(x, y, dist <= r * 0.9f ? Color.white : Color.clear);
                    }
                }

                tex.Apply();
                tex.filterMode = FilterMode.Bilinear;
                s_Circle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            }

            return s_Circle;
        }
    }
}
