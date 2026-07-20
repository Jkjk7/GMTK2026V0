using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 波次：每波正式准备 → 3-2-1 → 刷怪 → 清场结算 → 下一波准备。
/// </summary>
public class WaveManager : MonoBehaviour
{
    [Serializable]
    public struct WaveConfig
    {
        public int count;
        public float spawnInterval;
    }

    enum Phase
    {
        Preparing,
        Countdown,
        Spawning,
        WaitingClear,
        Complete
    }

    [Header("Waves")]
    [SerializeField] WaveConfig[] waves =
    {
        new WaveConfig { count = 4, spawnInterval = 1.2f },
        new WaveConfig { count = 6, spawnInterval = 1.0f },
        new WaveConfig { count = 8, spawnInterval = 0.9f },
        new WaveConfig { count = 10, spawnInterval = 0.8f },
        new WaveConfig { count = 12, spawnInterval = 0.7f }
    };

    [Header("Countdown")]
    [SerializeField] float countdownStepSeconds = 0.5f;

    readonly List<Enemy> _activeEnemies = new List<Enemy>();

    BattleLane _lane;
    Mage _mage;
    GameSession _session;
    Transform _enemyRoot;
    EnergyBallManager _ballManager;

    Phase _phase = Phase.Preparing;
    int _waveIndex;
    int _spawnedThisWave;
    float _timer;
    float _prepDuration;
    int _countdownDigit = 3;
    bool _waveRewardsGranted;

    public int CurrentWaveDisplay => Mathf.Clamp(_waveIndex + 1, 1, Mathf.Max(1, TotalWaves));
    public int TotalWaves => waves != null ? waves.Length : 0;
    public int ActiveEnemyCount => _activeEnemies.Count;
    public bool IsSessionActive => _phase != Phase.Complete;
    public bool IsCombatPhase => _phase == Phase.Spawning || _phase == Phase.WaitingClear;
    public bool IsPreparingPhase => _phase == Phase.Preparing;
    public bool IsCountdownPhase => _phase == Phase.Countdown;
    public float PrepRemaining => _phase == Phase.Preparing ? Mathf.Max(0f, _timer) : 0f;
    public float PrepDuration => _prepDuration;
    public int CountdownDigit => _countdownDigit;

    public event Action<int, int> OnWaveChanged;
    public event Action OnAllWavesComplete;
    public event Action<int, float> OnPrepStarted;
    public event Action<float, float> OnPrepTick;
    public event Action<int> OnCountdownDigit;
    public event Action OnCombatStarted;

    public void Initialize(
        BattleLane lane,
        Mage mage,
        GameSession session,
        Transform enemyRoot,
        EnergyBallManager ballManager = null)
    {
        _lane = lane;
        _mage = mage;
        _session = session;
        _enemyRoot = enemyRoot;
        _ballManager = ballManager;
        _waveIndex = 0;
        BeginPrepForWave(0);
    }

    void Update()
    {
        if (_session != null && !_session.IsRunActive && _phase != Phase.Complete)
        {
            return;
        }

        switch (_phase)
        {
            case Phase.Preparing:
                TickPreparing();
                break;
            case Phase.Countdown:
                TickCountdown();
                break;
            case Phase.Spawning:
                TickSpawning();
                break;
            case Phase.WaitingClear:
                TickWaitingClear();
                break;
        }
    }

    void BeginPrepForWave(int index)
    {
        if (waves == null || index >= waves.Length)
        {
            CompleteAllWaves();
            return;
        }

        _waveIndex = index;
        _spawnedThisWave = 0;
        _waveRewardsGranted = false;
        _phase = Phase.Preparing;
        _prepDuration = ModulePricing.GetPrepSeconds(CurrentWaveDisplay);
        _timer = _prepDuration;

        _session?.EnterPreparing();
        PrepareBoardForPrepPhase();

        OnWaveChanged?.Invoke(CurrentWaveDisplay, TotalWaves);
        OnPrepStarted?.Invoke(CurrentWaveDisplay, _prepDuration);
        OnPrepTick?.Invoke(_timer, _prepDuration);
    }

    void PrepareBoardForPrepPhase()
    {
        _ballManager?.ClearAllBalls();
        ProjectileModule[] turrets = FindObjectsOfType<ProjectileModule>();
        for (int i = 0; i < turrets.Length; i++)
        {
            if (turrets[i] != null && turrets[i].enabled)
            {
                turrets[i].ClearEnergy();
            }
        }
    }

    void TickPreparing()
    {
        _timer -= Time.deltaTime;
        OnPrepTick?.Invoke(Mathf.Max(0f, _timer), _prepDuration);
        if (_timer > 0f)
        {
            return;
        }

        StartCountdown();
    }

    /// <summary>准备完毕：跳过剩余准备，进入 3-2-1。</summary>
    public void RequestReady()
    {
        if (_phase != Phase.Preparing)
        {
            return;
        }

        StartCountdown();
    }

    void StartCountdown()
    {
        _phase = Phase.Countdown;
        _countdownDigit = 3;
        _timer = countdownStepSeconds;
        OnCountdownDigit?.Invoke(_countdownDigit);
    }

    void TickCountdown()
    {
        _timer -= Time.deltaTime;
        if (_timer > 0f)
        {
            return;
        }

        _countdownDigit--;
        if (_countdownDigit >= 1)
        {
            _timer = countdownStepSeconds;
            OnCountdownDigit?.Invoke(_countdownDigit);
            return;
        }

        OnCountdownDigit?.Invoke(0);
        BeginCombatSpawn();
    }

    void BeginCombatSpawn()
    {
        _phase = Phase.Spawning;
        _timer = 0f;
        _session?.EnterCombat();
        WaveConfig config = waves[_waveIndex];
        WaveGoldBudget.Instance?.BeginWave(CurrentWaveDisplay, config.count, EnemyGoldType.Normal);
        OnCombatStarted?.Invoke();
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
        if (_spawnedThisWave < waves[_waveIndex].count || _activeEnemies.Count > 0)
        {
            return;
        }

        GrantWaveEndRewards();

        if (_waveIndex >= waves.Length - 1)
        {
            CompleteAllWaves();
            return;
        }

        BeginPrepForWave(_waveIndex + 1);
    }

    void GrantWaveEndRewards()
    {
        if (_waveRewardsGranted || WaveGoldBudget.Instance == null)
        {
            return;
        }

        _waveRewardsGranted = true;
        Vector3 from = _mage != null
            ? _mage.transform.position
            : (_lane != null ? _lane.GetSpawnPosition() : Vector3.zero);
        int clear = WaveGoldBudget.Instance.TakeWaveClearReward();
        if (clear > 0 && GoldDropService.Instance != null)
        {
            GoldDropService.Instance.GrantGoldWithFly(clear, from);
        }

        int perfect = WaveGoldBudget.Instance.TakePerfectRewardIfEligible();
        if (perfect > 0 && GoldDropService.Instance != null)
        {
            GoldDropService.Instance.GrantGoldWithFly(perfect, from + Vector3.up * 0.4f);
        }
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
        enemy.Initialize(_lane, _mage, this, EnemyGoldType.Normal);
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
