using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 波次刷怪：5 波配置、漏怪后清屏不重置波次、全波完成触发胜利。
/// </summary>
public class WaveManager : MonoBehaviour
{
    [Serializable]
    public struct WaveConfig
    {
        public int count;
        public float spawnInterval;
        [Tooltip("本波清空后等待下一波的时间（秒）。")]
        public float postWaveDelay;
    }

    enum Phase
    {
        Preparing,
        Spawning,
        WaitingClear,
        BetweenWaves,
        Complete
    }

    [Header("Timing")]
    [SerializeField] float prepareTimeSeconds = 10f;

    [Header("Waves")]
    [SerializeField] WaveConfig[] waves =
    {
        new WaveConfig { count = 4, spawnInterval = 1.2f, postWaveDelay = 6f },
        new WaveConfig { count = 6, spawnInterval = 1.0f, postWaveDelay = 6f },
        new WaveConfig { count = 8, spawnInterval = 0.9f, postWaveDelay = 6f },
        new WaveConfig { count = 10, spawnInterval = 0.8f, postWaveDelay = 6f },
        new WaveConfig { count = 12, spawnInterval = 0.7f, postWaveDelay = 0f }
    };

    readonly List<Enemy> _activeEnemies = new List<Enemy>();

    BattleLane _lane;
    Mage _mage;
    GameSession _session;
    Transform _enemyRoot;

    Phase _phase = Phase.Preparing;
    int _waveIndex;
    int _spawnedThisWave;
    float _timer;

    public int CurrentWaveDisplay => Mathf.Clamp(_waveIndex + 1, 1, TotalWaves);
    public int TotalWaves => waves != null ? waves.Length : 0;
    public int ActiveEnemyCount => _activeEnemies.Count;
    public bool IsSessionActive => _phase != Phase.Complete;

    public event Action<int, int> OnWaveChanged;
    public event Action OnAllWavesComplete;

    public void Initialize(BattleLane lane, Mage mage, GameSession session, Transform enemyRoot)
    {
        _lane = lane;
        _mage = mage;
        _session = session;
        _enemyRoot = enemyRoot;
        _phase = Phase.Preparing;
        _waveIndex = 0;
        _spawnedThisWave = 0;
        _timer = prepareTimeSeconds;
        OnWaveChanged?.Invoke(0, TotalWaves);
    }

    void Update()
    {
        if (_session != null && !_session.IsPlaying)
        {
            return;
        }

        switch (_phase)
        {
            case Phase.Preparing:
                TickPreparing();
                break;
            case Phase.Spawning:
                TickSpawning();
                break;
            case Phase.WaitingClear:
                TickWaitingClear();
                break;
            case Phase.BetweenWaves:
                TickBetweenWaves();
                break;
        }
    }

    void TickPreparing()
    {
        _timer -= Time.deltaTime;
        if (_timer > 0f)
        {
            return;
        }

        _session?.BeginPlaying();
        StartWave(0);
    }

    void StartWave(int index)
    {
        if (waves == null || index >= waves.Length)
        {
            CompleteAllWaves();
            return;
        }

        _waveIndex = index;
        _spawnedThisWave = 0;
        _phase = Phase.Spawning;
        _timer = 0f;
        OnWaveChanged?.Invoke(CurrentWaveDisplay, TotalWaves);
    }

    void TickSpawning()
    {
        if (waves == null || _waveIndex >= waves.Length)
        {
            return;
        }

        WaveConfig config = waves[_waveIndex];
        if (_spawnedThisWave >= config.count)
        {
            _phase = Phase.WaitingClear;
            return;
        }

        _timer -= Time.deltaTime;
        if (_timer > 0f)
        {
            return;
        }

        SpawnEnemy();
        _spawnedThisWave++;
        _timer = config.spawnInterval;
    }

    void TickWaitingClear()
    {
        if (waves == null || _waveIndex >= waves.Length)
        {
            return;
        }

        WaveConfig config = waves[_waveIndex];
        if (_spawnedThisWave < config.count || _activeEnemies.Count > 0)
        {
            return;
        }

        if (_waveIndex >= waves.Length - 1)
        {
            CompleteAllWaves();
            return;
        }

        _phase = Phase.BetweenWaves;
        _timer = config.postWaveDelay;
    }

    void TickBetweenWaves()
    {
        _timer -= Time.deltaTime;
        if (_timer > 0f)
        {
            return;
        }

        StartWave(_waveIndex + 1);
    }

    void CompleteAllWaves()
    {
        _phase = Phase.Complete;
        OnAllWavesComplete?.Invoke();
        if (_mage != null && _mage.LivesRemaining > 0)
        {
            _session?.SetVictory();
        }
    }

    void SpawnEnemy()
    {
        if (_lane == null || _enemyRoot == null)
        {
            return;
        }

        var go = new GameObject($"Enemy_W{_waveIndex + 1}_{_spawnedThisWave + 1}");
        go.transform.SetParent(_enemyRoot, false);
        go.transform.position = _lane.GetSpawnPosition();
        var enemy = go.AddComponent<Enemy>();
        enemy.Initialize(_lane, _mage, this);
        RegisterEnemy(enemy);
    }

    public void RegisterEnemy(Enemy enemy)
    {
        if (enemy != null && !_activeEnemies.Contains(enemy))
        {
            _activeEnemies.Add(enemy);
        }
    }

    public void UnregisterEnemy(Enemy enemy)
    {
        _activeEnemies.Remove(enemy);
    }

    /// <summary>
    /// 漏怪惩罚：销毁场上全部敌人，波次进度不重置。
    /// </summary>
    public void ClearAllEnemies()
    {
        for (int i = _activeEnemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = _activeEnemies[i];
            if (enemy != null)
            {
                enemy.ForceDespawn();
            }
        }

        _activeEnemies.Clear();
    }
}
