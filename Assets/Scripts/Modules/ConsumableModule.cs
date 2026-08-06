using UnityEngine;

/// <summary>
/// 一次性道具：不会常驻在棋盘上，由放置控制器在目标格直接触发并销毁。
/// </summary>
public abstract class ConsumableModule : ModuleBase
{
    public sealed override void OnBallEnter(EnergyBall ball)
    {
    }

    public abstract bool CanConsumeAt(GridBoard board, GridCoord cell, GameSession session);

    public abstract bool ConsumeAt(GridBoard board, GridCoord cell, GameSession session);
}
