using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 波次：准备 → 3-2-1 → 按固定配额刷怪 → 清场 →（可选解锁草稿）→ 下一波准备。
/// </summary>
public class WaveManager : MonoBehaviour
{
    [Serializable]
    public struct WaveConfig
    {
        public int pointBudget;
        public float spawnInterval;
        public int guaranteedTanks;
    }

    enum Phase
    {
        Preparing,
        Countdown,
        Spawning,
        WaitingClear,
        AwaitingDraft,
        Complete
    }

    [Header("Waves (25)")]
    [SerializeField] WaveConfig[] waves;

    [Header("Countdown")]
    [SerializeField] float countdownStepSeconds = 0.5f;

    readonly List<Enemy> _activeEnemies = new List<Enemy>();
    readonly List<WaveSpawnBudget.SpawnEntry> _spawnQueue = new List<WaveSpawnBudget.SpawnEntry>();

    BattleLane _lane;
    Mage _mage;
    GameSession _session;
    Transform _enemyRoot;
    EnergyBallManager _ballManager;
    ModuleUnlockDirector _unlockDirector;
    EmitterUpgradeDirector _emitterDirector;
    BlessingCurseDirector _blessDirector;

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
    public bool IsAwaitingDraft => _phase == Phase.AwaitingDraft;
    public float PrepRemaining => _phase == Phase.Preparing ? Mathf.Max(0f, _timer) : 0f;
    public float PrepDuration => _prepDuration;
    public int CountdownDigit => _countdownDigit;

    public event Action<int, int> OnWaveChanged;
    public event Action OnAllWavesComplete;
    public event Action<int, float> OnPrepStarted;
    public event Action<float, float> OnPrepTick;
    public event Action<int> OnCountdownDigit;
    public event Action OnCombatStarted;

    public static int FindDisplayWave()
    {
        WaveManager wm = FindObjectOfType<WaveManager>();
        return wm != null ? wm.CurrentWaveDisplay : 1;
    }

    public void Initialize(
        BattleLane lane,
        Mage mage,
        GameSession session,
        Transform enemyRoot,
        EnergyBallManager ballManager = null,
        ModuleUnlockDirector unlockDirector = null,
        EmitterUpgradeDirector emitterDirector = null,
        BlessingCurseDirector blessDirector = null)
    {
        _lane = lane;
        _mage = mage;
        _session = session;
        _enemyRoot = enemyRoot;
        _ballManager = ballManager;
        _unlockDirector = unlockDirector;
        _emitterDirector = emitterDirector;
        _blessDirector = blessDirector;
        EnsureDefaultWaves();
        _waveIndex = 0;
        BeginPrepForWave(0);
    }

    void EnsureDefaultWaves()
    {
        // Roguelike25：始终使用 25 波固定配额表
        waves = new WaveConfig[25];
        for (int i = 0; i < 25; i++)
        {
            int display = i + 1;
            waves[i] = new WaveConfig
            {
                pointBudget = WaveSpawnBudget.GetDefaultBudget(display),
                spawnInterval = WaveSpawnBudget.GetSpawnInterval(display),
                guaranteedTanks = WaveSpawnBudget.GetGuaranteedTanks(display)
            };
        }
    }

    void Update()
    {
        if (_session != null && !_session.IsRunActive && _phase != Phase.Complete && _phase != Phase.AwaitingDraft)
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
        _spawnQueue.Clear();
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
        ClearEnergyOnBoard();
    }

    static void ClearEnergyOnBoard()
    {
        ProjectileModule[] lasers = FindObjectsOfType<ProjectileModule>();
        for (int i = 0; i < lasers.Length; i++)
        {
            lasers[i]?.ClearEnergy();
        }

        BombModule[] bombs = FindObjectsOfType<BombModule>();
        for (int i = 0; i < bombs.Length; i++)
        {
            bombs[i]?.ClearEnergy();
        }

        IceLaserModule[] ices = FindObjectsOfType<IceLaserModule>();
        for (int i = 0; i < ices.Length; i++)
        {
            ices[i]?.ClearEnergy();
        }

        MinerModule[] miners = FindObjectsOfType<MinerModule>();
        for (int i = 0; i < miners.Length; i++)
        {
            miners[i]?.ClearEnergy();
        }

        BlackHoleModule[] holes = FindObjectsOfType<BlackHoleModule>();
        for (int i = 0; i < holes.Length; i++)
        {
            holes[i]?.ClearEnergy();
        }

        SparkModule[] sparks = FindObjectsOfType<SparkModule>();
        for (int i = 0; i < sparks.Length; i++)
        {
            sparks[i]?.ClearEnergy();
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
        _spawnedThisWave = 0;
        _spawnQueue.Clear();
        _spawnQueue.AddRange(WaveSpawnBudget.BuildQueue(CurrentWaveDisplay));

        _session?.EnterCombat();
        WaveGoldBudget.Instance?.BeginWave(CurrentWaveDisplay, _spawnQueue);
        SandClock.Instance?.BeginWave(CurrentWaveDisplay);
        OnCombatStarted?.Invoke();
    }

    void TickSpawning()
    {
        if (_spawnedThisWave >= _spawnQueue.Count)
        {
            _phase = Phase.WaitingClear;
            return;
        }

        _timer -= Time.deltaTime;
        if (_timer > 0f)
        {
            return;
        }

        SpawnEnemy(_spawnQueue[_spawnedThisWave]);
        _spawnedThisWave++;
        float interval = waves[_waveIndex].spawnInterval;
        if (interval <= 0.01f)
        {
            interval = WaveSpawnBudget.GetSpawnInterval(CurrentWaveDisplay);
        }

        // 黄潮稍密
        if (_spawnedThisWave > 0 && _spawnedThisWave <= _spawnQueue.Count &&
            _spawnQueue[_spawnedThisWave - 1].Type == EnemyGoldType.Swarm)
        {
            interval *= 0.55f;
        }

        _timer = interval;
    }

    void TickWaitingClear()
    {
        if (_spawnedThisWave < _spawnQueue.Count || _activeEnemies.Count > 0)
        {
            return;
        }

        GrantWaveEndRewards();

        if (_waveIndex >= waves.Length - 1)
        {
            CompleteAllWaves();
            return;
        }

        int finishedWave = CurrentWaveDisplay;
        if (TryBeginDraftChain(finishedWave))
        {
            return;
        }

        BeginPrepForWave(_waveIndex + 1);
    }

    bool TryBeginDraftChain(int finishedWave)
    {
        bool module = _unlockDirector != null && _unlockDirector.ShouldOfferAfterWave(finishedWave);
        bool emitter = _emitterDirector != null && _emitterDirector.ShouldOfferAfterWave(finishedWave);
        bool bless = _blessDirector != null && _blessDirector.ShouldOfferAfterWave(finishedWave);
        if (!module && !emitter && !bless)
        {
            return false;
        }

        _session?.EnterPreparing();
        PrepareBoardForPrepPhase();
        _phase = Phase.AwaitingDraft;

        if (module)
        {
            _unlockDirector.BeginDraft(finishedWave, OnModuleDraftFinished);
            return true;
        }

        if (emitter)
        {
            _emitterDirector.BeginDraft(finishedWave, OnEmitterDraftFinished);
            return true;
        }

        _blessDirector.BeginDraft(finishedWave, OnDraftFinished);
        return true;
    }

    void OnModuleDraftFinished()
    {
        if (_phase != Phase.AwaitingDraft)
        {
            return;
        }

        int finishedWave = CurrentWaveDisplay;
        if (_emitterDirector != null && _emitterDirector.ShouldOfferAfterWave(finishedWave))
        {
            _emitterDirector.BeginDraft(finishedWave, OnEmitterDraftFinished);
            return;
        }

        if (_blessDirector != null && _blessDirector.ShouldOfferAfterWave(finishedWave))
        {
            _blessDirector.BeginDraft(finishedWave, OnDraftFinished);
            return;
        }

        BeginPrepForWave(_waveIndex + 1);
    }

    void OnEmitterDraftFinished()
    {
        if (_phase != Phase.AwaitingDraft)
        {
            return;
        }

        int finishedWave = CurrentWaveDisplay;
        if (_blessDirector != null && _blessDirector.ShouldOfferAfterWave(finishedWave))
        {
            _blessDirector.BeginDraft(finishedWave, OnDraftFinished);
            return;
        }

        BeginPrepForWave(_waveIndex + 1);
    }

    void OnDraftFinished()
    {
        if (_phase != Phase.AwaitingDraft)
        {
            return;
        }

        BeginPrepForWave(_waveIndex + 1);
    }

    void GrantWaveEndRewards()
    {
        if (_waveRewardsGranted)
        {
            return;
        }

        _waveRewardsGranted = true;
        SandClock.Instance?.GrantWaveClearReward();

        if (WaveGoldBudget.Instance == null)
        {
            return;
        }
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
        if (SandClock.Instance == null || SandClock.Instance.RemainingMs > 0)
        {
            _session?.SetVictory();
        }
    }

    void SpawnEnemy(WaveSpawnBudget.SpawnEntry entry)
    {
        if (_lane == null || _enemyRoot == null)
        {
            return;
        }

        string tag = entry.SandBuff ? $"{entry.Type}_Sand" : entry.Type.ToString();
        var go = new GameObject($"Enemy_W{_waveIndex + 1}_{_spawnedThisWave + 1}_{tag}");
        go.transform.SetParent(_enemyRoot, false);
        go.transform.position = _lane.GetSpawnPosition();
        var enemy = go.AddComponent<Enemy>();
        enemy.Initialize(_lane, _mage, this, CurrentWaveDisplay, entry.Type, entry.SandBuff);
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
