using System.Collections.Generic;
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
/// V0.1：预算随波指数上涨以匹配怪潮；击杀按类型在固定区间内随机，再受击杀池封顶。
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
    public int Budget => _budget;

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

    /// <summary>
    /// V0.1 波金币总预算。约：波1=20、波5=40、波10=100、波15=250、波25≈1635（5 的倍数）。
    /// 与怪潮、商店每 5 波跳价对齐（25 波）。
    /// </summary>
    public static int ComputeBudget(int waveNumber)
    {
        int w = Mathf.Clamp(waveNumber, 1, 25);
        float raw = 18f * Mathf.Pow(1.205f, w - 1f);
        return Mathf.Max(20, ModulePricing.RoundToFive(Mathf.RoundToInt(raw)));
    }

    public static float GetWeight(EnemyGoldType type)
    {
        switch (type)
        {
            case EnemyGoldType.Swarm: return 0.35f;
            case EnemyGoldType.Normal: return 1f;
            case EnemyGoldType.Tank: return 2.2f;
            case EnemyGoldType.Elite: return 3f;
            case EnemyGoldType.Boss: return 10f;
            default: return 1f;
        }
    }

    /// <summary>类型掉落闭区间 [min,max]（含端点）。</summary>
    public static void GetDropRange(EnemyGoldType type, out int min, out int max)
    {
        switch (type)
        {
            case EnemyGoldType.Swarm:
                min = 0;
                max = 1;
                break;
            case EnemyGoldType.Tank:
                min = 3;
                max = 7;
                break;
            case EnemyGoldType.Elite:
                min = 5;
                max = 12;
                break;
            case EnemyGoldType.Boss:
                min = 15;
                max = 30;
                break;
            default: // Normal
                min = 1;
                max = 3;
                break;
        }
    }

    public void BeginWave(int waveNumber, int enemyCount, EnemyGoldType defaultType = EnemyGoldType.Normal)
    {
        _waveNumber = Mathf.Max(1, waveNumber);
        _breachThisWave = false;
        _enemiesPlanned = Mathf.Max(1, enemyCount);
        ApplyBudgetSplit();
        _totalWeightPlanned = GetWeight(defaultType) * _enemiesPlanned;
    }

    /// <summary>按实际刷怪队列规划权重。</summary>
    public void BeginWave(int waveNumber, IList<WaveSpawnBudget.SpawnEntry> spawnQueue)
    {
        _waveNumber = Mathf.Max(1, waveNumber);
        _breachThisWave = false;
        _enemiesPlanned = spawnQueue != null ? Mathf.Max(1, spawnQueue.Count) : 1;
        ApplyBudgetSplit();

        float totalWeight = 0f;
        if (spawnQueue != null)
        {
            for (int i = 0; i < spawnQueue.Count; i++)
            {
                totalWeight += GetWeight(spawnQueue[i].Type);
            }
        }

        _totalWeightPlanned = Mathf.Max(0.01f, totalWeight);
    }

    public void BeginWave(int waveNumber, IList<EnemyGoldType> spawnQueue)
    {
        _waveNumber = Mathf.Max(1, waveNumber);
        _breachThisWave = false;
        _enemiesPlanned = spawnQueue != null ? Mathf.Max(1, spawnQueue.Count) : 1;
        ApplyBudgetSplit();

        float totalWeight = 0f;
        if (spawnQueue != null)
        {
            for (int i = 0; i < spawnQueue.Count; i++)
            {
                totalWeight += GetWeight(spawnQueue[i]);
            }
        }

        _totalWeightPlanned = Mathf.Max(0.01f, totalWeight);
    }

    void ApplyBudgetSplit()
    {
        _budget = ComputeBudget(_waveNumber);
        _killPoolRemaining = Mathf.RoundToInt(_budget * 0.70f);
        _waveClearReward = Mathf.RoundToInt(_budget * 0.20f);
        _perfectReward = Mathf.Max(0, _budget - _killPoolRemaining - _waveClearReward);
    }

    public void NotifyBreach()
    {
        _breachThisWave = true;
    }

    /// <summary>
    /// 击杀掉落：先在类型区间内随机，再与击杀池取小；池尽则 0。
    /// 清屏怪应不调用。
    /// </summary>
    public int RollKillGold(EnemyGoldType type)
    {
        if (_killPoolRemaining <= 0)
        {
            return 0;
        }

        GetDropRange(type, out int min, out int max);
        int roll = Random.Range(min, max + 1);

        // 池将尽时略向期望靠拢，避免最后几只全被钳成 0 或打穿预算观感
        if (_totalWeightPlanned > 0.01f)
        {
            float weight = GetWeight(type);
            float expected = _killPoolRemaining * (weight / _totalWeightPlanned);
            _totalWeightPlanned = Mathf.Max(0.01f, _totalWeightPlanned - weight);
            // 区间随机为主，期望只做轻微牵引
            roll = Mathf.RoundToInt(Mathf.Lerp(roll, expected, 0.25f));
            roll = Mathf.Clamp(roll, min, max);
        }

        roll = Mathf.Clamp(roll, 0, _killPoolRemaining);
        _killPoolRemaining -= roll;
        return roll;
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
