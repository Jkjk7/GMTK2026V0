using UnityEngine;

/// <summary>
/// 战斗区单路线坐标：刷怪点、魔法师终点、路线 Y。
/// </summary>
public class BattleLane : MonoBehaviour
{
    public float SpawnX { get; private set; }
    public float EndX { get; private set; }
    public float LaneY { get; private set; }

    /// <summary>
    /// 根据棋盘包围盒计算战斗区坐标。
    /// </summary>
    public void Initialize(Bounds boardBounds, float cellSize)
    {
        LaneY = boardBounds.max.y + cellSize * 2.2f;
        SpawnX = boardBounds.max.x + cellSize * 1.5f;
        EndX = boardBounds.min.x - cellSize * 0.5f;
    }

    public Vector3 GetSpawnPosition() => new Vector3(SpawnX, LaneY, 0f);

    public Vector3 GetEndPosition() => new Vector3(EndX, LaneY, 0f);
}
