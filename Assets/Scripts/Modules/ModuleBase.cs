using UnityEngine;

/// <summary>
/// 棋盘模块基类。携带 ModuleCardData（等级/投入金币）。
/// </summary>
public abstract class ModuleBase : MonoBehaviour
{
    protected GridBoard Board { get; private set; }

    public GridCoord Cell { get; private set; }

    public abstract ModuleType ModuleType { get; }

    public ModuleCardData CardData { get; private set; }

    public int ModuleLevel => CardData.Level;

    public void BindToCell(GridBoard board, GridCoord cell)
    {
        Board = board;
        Cell = cell;
        if (CardData.Level < 1)
        {
            ApplyCardData(ModuleCardData.Create(ModuleType, 1, 0));
        }
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
}
