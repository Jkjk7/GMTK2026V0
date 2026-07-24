using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 能量球管理器。
/// 职责：生成光球、维护存活数量上限、回收时更新计数。
/// 与 Emitter 协作：发射器只请求 TrySpawn，不直接 Instantiate。
/// </summary>
public class EnergyBallManager : MonoBehaviour
{
    [Header("Limits")]
    [Tooltip("全场同时存在的光球上限；达上限时 TrySpawn 失败。")]
    [SerializeField] int maxBalls = 40;

    [Header("Defaults")]
    [Tooltip("新生球速度（格/秒）。<=0 则使用 EnergyBall 组件上的默认值。")]
    [SerializeField] float defaultSpeedCellsPerSecond = 4f;

    [Tooltip("新生球寿命（秒）。<=0 则使用 EnergyBall 组件上的 lifetimeSeconds（改球寿命请改那边，或在此填正数覆盖）。")]
    [SerializeField] float defaultLifetimeSeconds = -1f;

    GridBoard _board;
    readonly List<EnergyBall> _active = new List<EnergyBall>();
    Transform _ballRoot;

    /// <summary>当前存活球数。</summary>
    public int ActiveCount => _active.Count;

    /// <summary>全场球上限。</summary>
    public int MaxBalls => maxBalls;

    /// <summary>
    /// 注入棋盘引用。应在发球前由 GameBootstrap 调用。
    /// </summary>
    public void Initialize(GridBoard board)
    {
        _board = board;
        if (_ballRoot == null)
        {
            _ballRoot = new GameObject("Balls").transform;
            _ballRoot.SetParent(transform, false);
        }
    }

    /// <summary>
    /// 尝试在指定位置生成一颗球（使用发射器本局升级默认参数）。
    /// </summary>
    public bool TrySpawn(Vector3 worldPosition, GridDirection direction)
    {
        float speed = defaultSpeedCellsPerSecond;
        float life = defaultLifetimeSeconds;
        int energy = EnergyBall.DefaultEnergy;
        if (EmitterRunUpgrades.Instance != null)
        {
            speed = EmitterRunUpgrades.Instance.BallSpeed;
            life = EmitterRunUpgrades.Instance.Lifetime;
            energy = EmitterRunUpgrades.Instance.Mass;
        }

        return TrySpawn(worldPosition, direction, speed, life, energy);
    }

    /// <summary>
    /// 尝试生成指定参数的球。speed/life/energy &lt;=0 时用组件或默认值。
    /// </summary>
    /// <returns>成功生成的球；达上限或未初始化返回 null。</returns>
    public EnergyBall TrySpawnBall(
        Vector3 worldPosition,
        GridDirection direction,
        float speedCellsPerSecond = -1f,
        float lifetimeSeconds = -1f,
        int energy = -1)
    {
        if (_board == null)
        {
            Debug.LogWarning("[EnergyBallManager] Board not initialized.");
            return null;
        }

        _active.RemoveAll(b => b == null);
        if (_active.Count >= maxBalls)
        {
            return null;
        }

        if (_ballRoot == null)
        {
            _ballRoot = new GameObject("Balls").transform;
            _ballRoot.SetParent(transform, false);
        }

        var go = new GameObject("EnergyBall");
        go.transform.SetParent(_ballRoot, false);
        var ball = go.AddComponent<EnergyBall>();
        ball.Initialize(
            _board,
            this,
            worldPosition,
            direction,
            speedCellsPerSecond,
            lifetimeSeconds,
            energy);
        _active.Add(ball);
        return ball;
    }

    /// <summary>
    /// 尝试在指定位置生成一颗球。
    /// </summary>
    /// <returns>成功生成返回 true；达上限或未初始化返回 false。</returns>
    public bool TrySpawn(
        Vector3 worldPosition,
        GridDirection direction,
        float speedCellsPerSecond,
        float lifetimeSeconds,
        int energy)
    {
        return TrySpawnBall(worldPosition, direction, speedCellsPerSecond, lifetimeSeconds, energy) != null;
    }

    /// <summary>
    /// 球自行 Despawn 时回调，更新计数列表。
    /// </summary>
    public void NotifyDespawned(EnergyBall ball)
    {
        _active.Remove(ball);
    }

    /// <summary>准备阶段：清除场上全部能量球。</summary>
    public void ClearAllBalls()
    {
        _active.RemoveAll(b => b == null);
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            EnergyBall ball = _active[i];
            if (ball != null)
            {
                Destroy(ball.gameObject);
            }
        }

        _active.Clear();
    }
}
