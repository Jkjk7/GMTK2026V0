using UnityEngine;

/// <summary>
/// 净化：一次性清除目标格诅咒、锁定与附魔。
/// </summary>
public class PurifyItemModule : ConsumableModule
{
    SpriteRenderer _body;

    public override ModuleType ModuleType => global::ModuleType.Purify;

    public override bool CanConsumeAt(GridBoard board, GridCoord cell, GameSession session)
    {
        return board != null && board.IsInside(cell);
    }

    public override bool ConsumeAt(GridBoard board, GridCoord cell, GameSession session)
    {
        if (!CanConsumeAt(board, cell, session))
        {
            return false;
        }

        board.SetCursed(cell, false);
        board.SetEnchant(cell, CellEnchant.None);
        ModuleBase mod = board.GetModule(cell);
        if (mod != null && mod.IsPermanentlyLocked)
        {
            mod.SetPermanentlyLocked(false);
        }

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
        transform.localScale = Vector3.one * 0.56f;
    }
}
