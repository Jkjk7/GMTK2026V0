using UnityEngine;

/// <summary>
/// 火焰祝福：一次性把目标格变成火焰附魔。
/// </summary>
public class FlameBlessingItemModule : ConsumableModule
{
    SpriteRenderer _body;

    public override ModuleType ModuleType => global::ModuleType.FlameBlessing;

    public override bool CanConsumeAt(GridBoard board, GridCoord cell, GameSession session)
    {
        return board != null
            && board.IsInside(cell)
            && board.IsBuildableCell(cell)
            && !board.IsCursed(cell);
    }

    public override bool ConsumeAt(GridBoard board, GridCoord cell, GameSession session)
    {
        if (!CanConsumeAt(board, cell, session))
        {
            return false;
        }

        board.SetEnchant(cell, CellEnchant.Flame);
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

        _body.sprite = PrototypeSprites.Square;
        _body.sortingOrder = 8;
        transform.localScale = Vector3.one * 0.52f;
    }
}
