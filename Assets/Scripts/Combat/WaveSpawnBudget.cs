using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 固定波次表：红/黄/蓝/紫/金配额；沙 buff；HP 每 5 波跳档。
/// </summary>
public static class WaveSpawnBudget
{
    public struct SpawnEntry
    {
        public EnemyGoldType Type;
        public bool SandBuff;
    }

    /// <summary>常规怪潮波数（不含终局 Boss 波）。</summary>
    const int RegularWaveCount = 25;
    /// <summary>总波数：25 常规 + 第 26 波终局 Boss。</summary>
    public const int WaveCount = 26;
    public const float SandBuffPowerMult = 1.5f;

    static readonly int[] NormalCounts =
    {
        4, 5, 6, 8, 8,
        8, 11, 14, 18, 23,
        26, 28, 30, 32, 36,
        40, 44, 48, 52, 58,
        62, 66, 72, 78, 88
    };

    static readonly int[] SwarmCounts =
    {
        0, 0, 0, 0, 4,
        7, 11, 16, 23, 34,
        40, 44, 48, 52, 60,
        68, 76, 84, 92, 104,
        112, 120, 130, 140, 160
    };

    static readonly int[] TankCounts =
    {
        0, 0, 0, 0, 0,
        0, 0, 1, 2, 4,
        5, 6, 7, 8, 10,
        11, 12, 14, 16, 18,
        20, 22, 24, 26, 30
    };

    /// <summary>紫拆：波15起；后期频率刻意压低。</summary>
    static readonly int[] DisassemblerCounts =
    {
        0, 0, 0, 0, 0,
        0, 0, 0, 0, 0,
        0, 0, 0, 0, 1,
        1, 1, 1, 2, 2,
        2, 2, 2, 2, 3
    };

    /// <summary>金盾：波21起。</summary>
    static readonly int[] ShieldCasterCounts =
    {
        0, 0, 0, 0, 0,
        0, 0, 0, 0, 0,
        0, 0, 0, 0, 0,
        0, 0, 0, 0, 0,
        1, 1, 1, 2, 2
    };

    static readonly int[] SandBuffBaseCounts =
    {
        0, 0, 0, 0, 0,
        1, 2, 2, 2, 2,
        3, 3, 3, 3, 3,
        3, 3, 3, 3, 3,
        4, 4, 4, 4, 4
    };

    // 后期加血：11–15→60，16–20→120，21–25→200
    static readonly int[] NormalHitPoints =
    {
        10, 10, 10, 10, 10,
        25, 25, 25, 25, 25,
        60, 60, 60, 60, 60,
        120, 120, 120, 120, 120,
        200, 200, 200, 200, 200
    };

    static readonly float[] SpawnIntervals =
    {
        0.95f, 0.88f, 0.80f, 0.72f, 0.58f,
        0.72f, 0.55f, 0.42f, 0.30f, 0.18f,
        0.38f, 0.30f, 0.24f, 0.18f, 0.12f,
        0.32f, 0.26f, 0.20f, 0.15f, 0.10f,
        0.28f, 0.22f, 0.16f, 0.12f, 0.08f
    };

    public const int PointRed = 5;
    public const int PointYellow = 1;
    public const int PointBlue = 20;
    public const int BossHitPoints = 50_000;

    public static int GetStage(int waveDisplay)
    {
        return Mathf.Max(0, (Mathf.Max(1, waveDisplay) - 1) / 5);
    }

    public static int GetPointCost(EnemyGoldType type)
    {
        switch (type)
        {
            case EnemyGoldType.Swarm: return PointYellow;
            case EnemyGoldType.Tank:
            case EnemyGoldType.Disassembler:
            case EnemyGoldType.ShieldCaster:
                return PointBlue;
            case EnemyGoldType.Boss: return PointBlue * 5;
            default: return PointRed;
        }
    }

    public static List<SpawnEntry> BuildQueue(int waveDisplay)
    {
        if (IsBossWave(waveDisplay))
        {
            return new List<SpawnEntry>(1)
            {
                new SpawnEntry { Type = EnemyGoldType.Boss, SandBuff = false }
            };
        }

        int index = GetWaveIndex(waveDisplay);
        int total = NormalCounts[index] + SwarmCounts[index] + TankCounts[index]
                    + DisassemblerCounts[index] + ShieldCasterCounts[index];
        var queue = new List<SpawnEntry>(total);
        AddType(queue, EnemyGoldType.Normal, NormalCounts[index]);
        AddType(queue, EnemyGoldType.Swarm, SwarmCounts[index]);
        AddType(queue, EnemyGoldType.Tank, TankCounts[index]);
        AddType(queue, EnemyGoldType.Disassembler, DisassemblerCounts[index]);
        AddType(queue, EnemyGoldType.ShieldCaster, ShieldCasterCounts[index]);
        Shuffle(queue);
        ApplySandBuffs(queue, SandBuffBaseCounts[index]);
        return queue;
    }

    public static bool IsBossWave(int waveDisplay) => waveDisplay >= WaveCount;

    public static List<SpawnEntry> BuildQueue(int waveDisplay, int pointBudget, int guaranteedTanks)
    {
        return BuildQueue(waveDisplay);
    }

    static void ApplySandBuffs(List<SpawnEntry> queue, int baseCount)
    {
        if (queue == null || queue.Count == 0 || baseCount <= 0)
        {
            return;
        }

        int count = baseCount + Random.Range(-1, 2);
        count = Mathf.Clamp(count, 0, queue.Count);
        if (count <= 0)
        {
            return;
        }

        var eligible = new List<int>(queue.Count);
        for (int i = 0; i < queue.Count; i++)
        {
            if (queue[i].Type == EnemyGoldType.Boss)
            {
                continue;
            }

            eligible.Add(i);
        }

        count = Mathf.Min(count, eligible.Count);
        for (int i = 0; i < eligible.Count; i++)
        {
            int j = Random.Range(i, eligible.Count);
            (eligible[i], eligible[j]) = (eligible[j], eligible[i]);
        }

        for (int i = 0; i < count; i++)
        {
            int idx = eligible[i];
            SpawnEntry e = queue[idx];
            e.SandBuff = true;
            queue[idx] = e;
        }
    }

    static void AddType(List<SpawnEntry> queue, EnemyGoldType type, int count)
    {
        for (int i = 0; i < count; i++)
        {
            queue.Add(new SpawnEntry { Type = type, SandBuff = false });
        }
    }

    static void Shuffle(List<SpawnEntry> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = Random.Range(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public static int GetDefaultBudget(int waveDisplay)
    {
        int index = GetWaveIndex(waveDisplay);
        return NormalCounts[index] * PointRed
            + SwarmCounts[index] * PointYellow
            + TankCounts[index] * PointBlue
            + DisassemblerCounts[index] * PointBlue
            + ShieldCasterCounts[index] * PointBlue;
    }

    public static int GetGuaranteedTanks(int waveDisplay) => TankCounts[GetWaveIndex(waveDisplay)];
    public static float GetSpawnInterval(int waveDisplay) => SpawnIntervals[GetWaveIndex(waveDisplay)];
    public static int GetNormalCount(int waveDisplay) => NormalCounts[GetWaveIndex(waveDisplay)];
    public static int GetSwarmCount(int waveDisplay) => SwarmCounts[GetWaveIndex(waveDisplay)];
    public static int GetTankCount(int waveDisplay) => TankCounts[GetWaveIndex(waveDisplay)];
    public static int GetDisassemblerCount(int waveDisplay) => DisassemblerCounts[GetWaveIndex(waveDisplay)];
    public static int GetShieldCasterCount(int waveDisplay) => ShieldCasterCounts[GetWaveIndex(waveDisplay)];
    public static int GetSandBuffBaseCount(int waveDisplay) => SandBuffBaseCounts[GetWaveIndex(waveDisplay)];

    public static int GetEnemyCount(int waveDisplay)
    {
        if (IsBossWave(waveDisplay))
        {
            return 1;
        }

        int index = GetWaveIndex(waveDisplay);
        return NormalCounts[index] + SwarmCounts[index] + TankCounts[index]
               + DisassemblerCounts[index] + ShieldCasterCounts[index];
    }

    public static int GetHitPoints(int waveDisplay, EnemyGoldType type, bool sandBuff = false)
    {
        if (type == EnemyGoldType.Boss)
        {
            return BossHitPoints;
        }

        int normalHp = NormalHitPoints[GetWaveIndex(waveDisplay)];
        int hp;
        switch (type)
        {
            case EnemyGoldType.Swarm:
                hp = Mathf.CeilToInt(normalHp * 0.5f);
                break;
            case EnemyGoldType.Tank:
            case EnemyGoldType.Disassembler:
            case EnemyGoldType.ShieldCaster:
                hp = normalHp * 4;
                break;
            default:
                hp = normalHp;
                break;
        }

        if (sandBuff)
        {
            hp = Mathf.CeilToInt(hp * SandBuffPowerMult);
        }

        return hp;
    }

    public static int GetTotalHitPoints(int waveDisplay)
    {
        if (IsBossWave(waveDisplay))
        {
            return BossHitPoints;
        }

        int index = GetWaveIndex(waveDisplay);
        int normalHp = NormalHitPoints[index];
        int swarmHp = Mathf.CeilToInt(normalHp * 0.5f);
        int tankHp = normalHp * 4;
        int total = NormalCounts[index] * normalHp
            + SwarmCounts[index] * swarmHp
            + TankCounts[index] * tankHp
            + DisassemblerCounts[index] * tankHp
            + ShieldCasterCounts[index] * tankHp;
        int sandN = SandBuffBaseCounts[index];
        if (sandN > 0)
        {
            total += Mathf.RoundToInt(normalHp * (SandBuffPowerMult - 1f) * sandN);
        }

        return total;
    }

    static int GetWaveIndex(int waveDisplay)
    {
        // Boss 波复用第 25 波表行（仅作间隔等非配额查询兜底）。
        return Mathf.Clamp(waveDisplay, 1, RegularWaveCount) - 1;
    }
}
