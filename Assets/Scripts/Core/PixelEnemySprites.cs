using System;
using UnityEngine;

/// <summary>One shared 48px atlas, decoded once. Never consumes gameplay randomness.</summary>
public static class PixelEnemySprites
{
    static Sprite[] _sprites;

    public static Sprite Get(EnemyGoldType type)
    {
        if (_sprites == null || _sprites[0] == null)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.name = "TimeSands_PixelEnemies";
            texture.LoadImage(Convert.FromBase64String(PixelEnemyData.Png), false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            _sprites = new Sprite[7];
            for (int i = 0; i < _sprites.Length; i++)
            {
                _sprites[i] = Sprite.Create(texture, new Rect(i * 48, 0, 48, 48),
                    new Vector2(0.5f, 0.5f), 48f, 0, SpriteMeshType.FullRect);
                _sprites[i].name = "PixelEnemy_" + i;
            }
        }
        int index;
        switch (type)
        {
            case EnemyGoldType.Swarm: index = 0; break;
            case EnemyGoldType.Tank: index = 2; break;
            case EnemyGoldType.Elite: index = 3; break;
            case EnemyGoldType.Boss: index = 4; break;
            case EnemyGoldType.Disassembler: index = 5; break;
            case EnemyGoldType.ShieldCaster: index = 6; break;
            default: index = 1; break;
        }
        return _sprites[index];
    }
}
