using UnityEngine;

/// <summary>
/// 寒冰菇：仅战斗中可用；全体冻结 1 秒并附加 2 秒寒冷。
/// </summary>
public class FrostMushroomItemModule : ConsumableModule
{
    const float FreezeSeconds = 1f;
    const float ChillSeconds = 2f;

    SpriteRenderer _body;

    public override ModuleType ModuleType => global::ModuleType.FrostMushroom;

    public override bool CanConsumeAt(GridBoard board, GridCoord cell, GameSession session)
    {
        return board != null
            && board.IsInside(cell)
            && session != null
            && session.IsCombatActive;
    }

    public override bool ConsumeAt(GridBoard board, GridCoord cell, GameSession session)
    {
        if (!CanConsumeAt(board, cell, session))
        {
            return false;
        }

        float chillPct = RunModifiers.Instance != null
            ? RunModifiers.Instance.GetEffectiveChillSlowPercent()
            : ModuleCatalog.IceSlowPercent;
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        for (int i = 0; i < enemies.Length; i++)
        {
            Enemy e = enemies[i];
            if (e == null || !e.IsAlive)
            {
                continue;
            }

            e.ApplyFreeze(FreezeSeconds);
            e.ApplySlow(chillPct, ChillSeconds);
        }

        FrostFreezeFlash.Play();
        return true;
    }

    public override void RefreshVisual()
    {
        EnsureVisual();
        if (_body != null)
        {
            _body.color = ModuleCatalog.GetDisplayColor(ModuleType);
        }
    }

    void EnsureVisual()
    {
        if (_body != null)
        {
            return;
        }

        _body = GetComponent<SpriteRenderer>();
        if (_body == null)
        {
            _body = gameObject.AddComponent<SpriteRenderer>();
        }

        _body.sprite = PrototypeSprites.Circle;
        _body.sortingOrder = 8;
        transform.localScale = Vector3.one * 0.6f;
    }
}
