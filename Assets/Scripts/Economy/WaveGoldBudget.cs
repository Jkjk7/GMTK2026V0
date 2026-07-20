using UnityEngine;

/// <summary>
/// 敌人金币类型（影响掉落权重与夹取区间）。
/// </summary>
public enum EnemyGoldType
{
    Swarm = 0,
    Normal = 1,
    Tank = 2,
    Elite = 3,
    Boss = 4
}

/// <summary>
/// 波次金币预算与击杀分摊。
/// </summary>
public class WaveGoldBudget : MonoBehaviour
{
    public static WaveGoldBudget Instance { get; private set; }

    int _waveNumber = 1;
    int _budget;
    int _killPoolRemaining;
    int _waveClearReward;
    int _perfectReward;
    float _totalWeightPlanned;
    bool _breachThisWave;
    int _enemiesPlanned;

    public int CurrentWaveNumber => _waveNumber;
    public int WaveClearReward => _waveClearReward;
    public int PerfectReward => _perfectReward;
    public bool BreachThisWave => _breachThisWave;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public static int ComputeBudget(int waveNumber)
    {
        int w = Mathf.Max(1, waveNumber);
        return Mathf.RoundToInt(14f * Mathf.Pow(1.065f, w - 1) + 6f);
    }

    public static float GetWeight(EnemyGoldType type)
    {
        switch (type)
        {
            case EnemyGoldType.Swarm: return 0.25f;
            case EnemyGoldType.Normal: return 1f;
            case EnemyGoldType.Tank: return 1.6f;
            case EnemyGoldType.Elite: return 3f;
            case EnemyGoldType.Boss: return 10f;
            default: return 1f;
        }
    }

    public void BeginWave(int waveNumber, int enemyCount, EnemyGoldType defaultType = EnemyGoldType.Normal)
    {
        _waveNumber = Mathf.Max(1, waveNumber);
        _breachThisWave = false;
        _enemiesPlanned = Mathf.Max(1, enemyCount);
        _budget = ComputeBudget(_waveNumber);
        _killPoolRemaining = Mathf.RoundToInt(_budget * 0.70f);
        _waveClearReward = Mathf.RoundToInt(_budget * 0.20f);
        _perfectReward = Mathf.Max(0, _budget - _killPoolRemaining - _waveClearReward);
        _totalWeightPlanned = GetWeight(defaultType) * _enemiesPlanned;
    }

    public void NotifyBreach()
    {
        _breachThisWave = true;
    }

    /// <summary>
    /// 击杀掉落金额（清屏怪应不调用）。0 表示不掉。
    /// </summary>
    public int RollKillGold(EnemyGoldType type)
    {
        if (_killPoolRemaining <= 0 || _totalWeightPlanned <= 0f)
        {
            return ClampForType(type, 0);
        }

        float weight = GetWeight(type);
        float expected = _killPoolRemaining * (weight / Mathf.Max(0.01f, _totalWeightPlanned));
        // 消耗规划权重，避免后期怪吃光预算时期望失真
        _totalWeightPlanned = Mathf.Max(0.01f, _totalWeightPlanned - weight);

        int roll = Mathf.RoundToInt(expected + Random.Range(-1.2f, 1.2f));
        roll = ClampForType(type, roll);
        roll = Mathf.Clamp(roll, 0, _killPoolRemaining);
        _killPoolRemaining -= roll;
        return roll;
    }

    static int ClampForType(EnemyGoldType type, int value)
    {
        switch (type)
        {
            case EnemyGoldType.Swarm:
                return Mathf.Clamp(value, 0, 1);
            case EnemyGoldType.Normal:
                return Mathf.Max(0, value);
            default:
                return Mathf.Max(0, value);
        }
    }

    public int TakeWaveClearReward()
    {
        int v = _waveClearReward;
        _waveClearReward = 0;
        return v;
    }

    public int TakePerfectRewardIfEligible()
    {
        if (_breachThisWave)
        {
            return 0;
        }

        int v = _perfectReward;
        _perfectReward = 0;
        return v;
    }
}
