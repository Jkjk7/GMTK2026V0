using UnityEngine;

/// <summary>
/// 棋盘模块基类。
/// 所有可放置模块继承此类，并实现 OnBallEnter。
/// 不负责：手牌、商店、战斗区坐标。
/// </summary>
public abstract class ModuleBase : MonoBehaviour
{
    protected GridBoard Board { get; private set; }

    /// <summary>当前所在格子；未放置时无效。</summary>
    public GridCoord Cell { get; private set; }

    /// <summary>模块种类，供手牌/放置使用。</summary>
    public abstract ModuleType ModuleType { get; }

    /// <summary>
    /// 由 GridBoard.TryPlaceModule 调用，绑定格子。
    /// </summary>
    public void BindToCell(GridBoard board, GridCoord cell)
    {
        Board = board;
        Cell = cell;
    }

    /// <summary>
    /// 光球首次进入本格中心附近时调用（同球同格不会连触）。
    /// 子类在此吸能、改向等。
    /// </summary>
    public abstract void OnBallEnter(EnergyBall ball);

    /// <summary>
    /// 可选：放置前/后刷新朝向视觉。默认空实现。
    /// </summary>
    public virtual void RefreshVisual()
    {
    }
}
