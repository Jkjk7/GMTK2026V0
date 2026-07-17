using UnityEngine;

/// <summary>
/// 棋盘四向枚举与方向换算工具。
/// 约定：世界坐标 X 向右增大，Y 向上增大；与 GridBoard「(0,0)=左下」一致。
/// </summary>
public enum GridDirection
{
    Up = 0,
    Right = 1,
    Down = 2,
    Left = 3
}

/// <summary>
/// GridDirection 的静态辅助方法：向量换算、对向、旋转。
/// </summary>
public static class GridDirectionUtil
{
    /// <summary>
    /// 将方向转为单位格偏移（col,row）。
    /// </summary>
    public static Vector2Int ToCellOffset(GridDirection direction)
    {
        switch (direction)
        {
            case GridDirection.Up: return new Vector2Int(0, 1);
            case GridDirection.Right: return new Vector2Int(1, 0);
            case GridDirection.Down: return new Vector2Int(0, -1);
            case GridDirection.Left: return new Vector2Int(-1, 0);
            default: return Vector2Int.zero;
        }
    }

    /// <summary>
    /// 将方向转为世界空间单位向量。
    /// </summary>
    public static Vector2 ToWorldVector(GridDirection direction)
    {
        Vector2Int offset = ToCellOffset(direction);
        return new Vector2(offset.x, offset.y);
    }

    /// <summary>
    /// 取相反方向（用于“从哪边进入”的推断：飞行方向的反面即入口侧）。
    /// 例：球向右飞进入格子，则从 Left 口进入。
    /// </summary>
    public static GridDirection Opposite(GridDirection direction)
    {
        return (GridDirection)(((int)direction + 2) % 4);
    }

    /// <summary>
    /// 顺时针旋转 90°。
    /// </summary>
    public static GridDirection RotateClockwise(GridDirection direction)
    {
        return (GridDirection)(((int)direction + 1) % 4);
    }

    /// <summary>
    /// 由近似世界向量推断最近的四向（用于调试或从速度反推）。
    /// </summary>
    public static GridDirection FromWorldVector(Vector2 world)
    {
        if (Mathf.Abs(world.x) >= Mathf.Abs(world.y))
        {
            return world.x >= 0f ? GridDirection.Right : GridDirection.Left;
        }

        return world.y >= 0f ? GridDirection.Up : GridDirection.Down;
    }
}
