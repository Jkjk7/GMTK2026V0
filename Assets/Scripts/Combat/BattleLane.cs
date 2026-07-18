using UnityEngine;

/// <summary>
/// 战斗区单路线：由独立锚点定义出生点与终点，不再依赖棋盘 Bounds。
/// </summary>
public class BattleLane : MonoBehaviour
{
    public float SpawnX { get; private set; }
    public float EndX { get; private set; }
    public float LaneY { get; private set; }

    Transform _spawnAnchor;
    Transform _endAnchor;

    /// <summary>
    /// 使用场景/运行时锚点初始化路线。
    /// </summary>
    public void Initialize(Transform spawnAnchor, Transform endAnchor)
    {
        _spawnAnchor = spawnAnchor;
        _endAnchor = endAnchor;
        RefreshFromAnchors();
    }

    /// <summary>
    /// 锚点被移动后可再次调用以刷新路线坐标。
    /// </summary>
    public void RefreshFromAnchors()
    {
        if (_spawnAnchor == null || _endAnchor == null)
        {
            return;
        }

        SpawnX = _spawnAnchor.position.x;
        EndX = _endAnchor.position.x;
        LaneY = _spawnAnchor.position.y;
    }

    public Vector3 GetSpawnPosition() =>
        _spawnAnchor != null ? _spawnAnchor.position : new Vector3(SpawnX, LaneY, 0f);

    public Vector3 GetEndPosition() =>
        _endAnchor != null ? _endAnchor.position : new Vector3(EndX, LaneY, 0f);
}
