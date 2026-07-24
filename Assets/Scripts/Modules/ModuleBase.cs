using UnityEngine;

/// <summary>
/// 棋盘模块基类。携带 ModuleCardData（等级/投入金币）。
/// </summary>
public abstract class ModuleBase : MonoBehaviour
{
    protected GridBoard Board { get; private set; }

    public GridBoard BoundBoard => Board;

    public GridCoord Cell { get; private set; }

    public abstract ModuleType ModuleType { get; }

    public ModuleCardData CardData { get; private set; }

    public int ModuleLevel => CardData.Level;

    public CellEnchant CellEnchant =>
        Board != null ? Board.GetEnchant(Cell) : CellEnchant.None;

    /// <summary>缩小附魔：射速翻倍 → 间隔 ×0.5。</summary>
    public float EnchantFireIntervalMultiplier =>
        CellEnchant == CellEnchant.Shrink ? 0.5f : 1f;

    /// <summary>冷却附魔：有冷却的模块减半。</summary>
    public float EnchantCooldownMultiplier =>
        CellEnchant == CellEnchant.Cooldown ? 0.5f : 1f;

    /// <summary>祝福束缚：永久锁定，不可移动/拆除/合成，仍可开火。</summary>
    public bool IsPermanentlyLocked { get; private set; }

    SpriteRenderer _lockMark;

    public virtual void BindToCell(GridBoard board, GridCoord cell)
    {
        Board = board;
        Cell = cell;
        if (CardData.Level < 1)
        {
            ApplyCardData(ModuleCardData.Create(ModuleType, 1, 0));
        }
    }

    public void SetPermanentlyLocked(bool locked)
    {
        IsPermanentlyLocked = locked;
        EnsureLockMark();
        if (_lockMark != null)
        {
            _lockMark.enabled = locked;
        }
    }

    void EnsureLockMark()
    {
        if (_lockMark != null)
        {
            return;
        }

        var go = new GameObject("LockMark");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0.28f, 0.28f, 0f);
        go.transform.localScale = Vector3.one * 0.28f;
        _lockMark = go.AddComponent<SpriteRenderer>();
        _lockMark.sprite = PrototypeSprites.Square;
        _lockMark.color = new Color(0.55f, 0.25f, 0.85f, 0.95f);
        _lockMark.sortingOrder = 14;
        _lockMark.enabled = IsPermanentlyLocked;
    }

    public virtual void ApplyCardData(ModuleCardData data)
    {
        CardData = data;
        RefreshVisual();
    }

    public abstract void OnBallEnter(EnergyBall ball);

    public virtual void RefreshVisual()
    {
    }

    /// <summary>拿起/预览时是否可用 R 旋转（收束器等）。</summary>
    public virtual bool CanRotate => false;

    public virtual int OrientationIndex => 0;

    public virtual void RotateClockwise()
    {
    }

    public virtual void SetOrientationIndex(int value)
    {
    }
}
