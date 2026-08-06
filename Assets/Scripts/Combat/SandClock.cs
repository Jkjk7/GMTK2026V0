using System;
using UnityEngine;

/// <summary>
/// 沙漏时间条：1 粒沙 = 1 毫秒。
/// 补沙仅来自「沙 buff」怪击杀；波结束不再回沙。漏怪罚沙每 5 波加档。
/// </summary>
public class SandClock : MonoBehaviour
{
    public const int InitialSandMs = 100_000;

    public const int BreachPenaltySwarmMs = 3_000;
    public const int BreachPenaltyNormalMs = 10_000;
    public const int BreachPenaltyTankMs = 30_000;

    // 砍半后约 6→17.5s（配合漏怪加档，控制回沙）
    static readonly int[] SandBuffBurstByStage = { 6_000, 8_000, 11_000, 14_000, 17_500 };

    static readonly float[] BreachMultByStage = { 1f, 1.3f, 1.65f, 2.1f, 2.5f };

    public static SandClock Instance { get; private set; }

    GameSession _session;
    int _remainingMs = InitialSandMs;
    int _waveDisplay = 1;

    public int RemainingMs => _remainingMs;
    public int CurrentWaveDisplay => _waveDisplay;

    public event Action<int> OnSandChanged;
    public event Action<int> OnPenalty;
    public event Action<int> OnSandGained;

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

    public void Initialize(GameSession session)
    {
        _session = session;
        _remainingMs = InitialSandMs;
        OnSandChanged?.Invoke(_remainingMs);
    }

    public int TryDrain(int ms)
    {
        if (ms <= 0 || _remainingMs <= 0)
        {
            return 0;
        }

        int drained = Mathf.Min(ms, _remainingMs);
        _remainingMs -= drained;
        OnSandChanged?.Invoke(_remainingMs);
        CheckDefeat();
        return drained;
    }

    public void AddSand(int ms)
    {
        if (ms <= 0 || _remainingMs <= 0)
        {
            return;
        }

        _remainingMs += ms;
        OnSandGained?.Invoke(ms);
        OnSandChanged?.Invoke(_remainingMs);
    }

    public void RemoveSand(int ms, int floorMs = 1000)
    {
        if (ms <= 0 || _remainingMs <= 0)
        {
            return;
        }

        int floor = Mathf.Max(1, floorMs);
        _remainingMs = Mathf.Max(floor, _remainingMs - ms);
        OnSandChanged?.Invoke(_remainingMs);
    }

    public void ApplyBreachPenalty(EnemyGoldType type, bool sandBuff = false)
    {
        if (_remainingMs <= 0)
        {
            return;
        }

        int penalty = GetBreachPenaltyMs(type, _waveDisplay, sandBuff);
        _remainingMs = Mathf.Max(0, _remainingMs - penalty);
        OnPenalty?.Invoke(penalty);
        OnSandChanged?.Invoke(_remainingMs);
        CheckDefeat();
    }

    public static int GetBreachPenaltyMs(EnemyGoldType type, int waveDisplay, bool sandBuff = false)
    {
        int stage = WaveSpawnBudget.GetStage(waveDisplay);
        float mult = BreachMultByStage[Mathf.Clamp(stage, 0, BreachMultByStage.Length - 1)];
        int baseMs;
        switch (type)
        {
            case EnemyGoldType.Swarm:
                baseMs = BreachPenaltySwarmMs;
                break;
            case EnemyGoldType.Tank:
            case EnemyGoldType.Disassembler:
            case EnemyGoldType.ShieldCaster:
                baseMs = BreachPenaltyTankMs;
                break;
            case EnemyGoldType.Boss:
                baseMs = BreachPenaltyTankMs * 3;
                break;
            default:
                baseMs = BreachPenaltyNormalMs;
                break;
        }

        float sandMult = sandBuff ? WaveSpawnBudget.SandBuffPowerMult : 1f;
        return Mathf.RoundToInt(baseMs * mult * sandMult);
    }

    public static int GetBreachPenaltyMs(EnemyGoldType type)
    {
        int wave = Instance != null ? Instance._waveDisplay : 1;
        return GetBreachPenaltyMs(type, wave, false);
    }

    public void BeginWave(int waveDisplay)
    {
        _waveDisplay = Mathf.Max(1, waveDisplay);
    }

    /// <summary>仅沙 buff 怪击杀爆沙；普通怪不反哺。</summary>
    public void GrantKillSand(EnemyGoldType type, bool sandBuff)
    {
        if (!sandBuff)
        {
            return;
        }

        AddSand(GetSandBuffBurstMs(_waveDisplay));
    }

    public static int GetSandBuffBurstMs(int waveDisplay)
    {
        int stage = WaveSpawnBudget.GetStage(waveDisplay);
        return SandBuffBurstByStage[Mathf.Clamp(stage, 0, SandBuffBurstByStage.Length - 1)];
    }

    // 兼容旧签名
    public void GrantKillSand(EnemyGoldType type)
    {
        // 无 buff 信息时不补沙
    }

    /// <summary>Boss 漏入本家：沙漏清零并立刻败北。</summary>
    public void ForceDefeat()
    {
        if (_remainingMs > 0)
        {
            int lost = _remainingMs;
            _remainingMs = 0;
            OnPenalty?.Invoke(lost);
            OnSandChanged?.Invoke(_remainingMs);
        }

        _session?.SetDefeat();
    }

    void CheckDefeat()
    {
        if (_remainingMs <= 0)
        {
            _session?.SetDefeat();
        }
    }
}
