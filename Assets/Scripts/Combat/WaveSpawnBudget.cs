using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 固定波次表：红/黄/蓝配额；「沙 buff」随机附着在普通怪上（非独立类型）。
/// HP / 漏怪罚沙每 5 波跳档。后期不压数量。
/// </summary>
public static class WaveSpawnBudget
{
    public struct SpawnEntry
    {
        public EnemyGoldType Type;
        public bool SandBuff;
    }

    const int WaveCount = 25;
    public const float SandBuffPowerMult = 1.5f;

    static readonly int[] NormalCounts =
    {
        5, 7, 9, 12, 10,
        14, 16, 18, 20, 24,
        26, 28, 30, 32, 36,
        40, 44, 48, 52, 58,
        62, 66, 72, 78, 88
    };

    static readonly int[] SwarmCounts =
    {
        0, 0, 0, 0, 9,
        12, 16, 20, 26, 36,
        40, 44, 48, 52, 60,
        68, 76, 84, 92, 104,
        112, 120, 130, 140, 160
    };

    static readonly int[] TankCounts =
    {
        0, 0, 0, 0, 0,
        0, 0, 2, 3, 5,
        5, 6, 7, 8, 10,
        11, 12, 14, 16, 18,
        20, 22, 24, 26, 30
    };

    /// <summary>沙 buff 基础数量（实际会 ±1 小幅浮动）。</summary>
    static readonly int[] SandBuffBaseCounts =
    {
        0, 0, 0, 0, 1,
        2, 2, 2, 2, 2,
        3, 3, 3, 3, 3,
        3, 3, 3, 3, 3,
        4, 4, 4, 4, 4
    };

    static readonly int[] NormalHitPoints =
    {
        10, 10, 10, 10, 10,
        25, 25, 25, 25, 25,
        50, 50, 50, 50, 50,
        80, 80, 80, 80, 80,
        130, 130, 130, 130, 130
    };

    static readonly float[] SpawnIntervals =
    {
        0.75f, 0.69f, 0.63f, 0.55f, 0.35f,
        0.50f, 0.42f, 0.34f, 0.26f, 0.16f,
        0.38f, 0.30f, 0.24f, 0.18f, 0.12f,
        0.32f, 0.26f, 0.20f, 0.15f, 0.10f,
        0.28f, 0.22f, 0.16f, 0.12f, 0.08f
    };

    public const int PointRed = 5;
    public const int PointYellow = 1;
    public const int PointBlue = 20;
    public const int BossHitPoints = 100_000;

    public static int GetStage(int waveDisplay)
    {
        return Mathf.Max(0, (Mathf.Max(1, waveDisplay) - 1) / 5);
    }

    public static int GetPointCost(EnemyGoldType type)
    {
        switch (type)
        {
            case EnemyGoldType.Swarm: return PointYellow;
            case EnemyGoldType.Tank: return PointBlue;
            case EnemyGoldType.Boss: return PointBlue * 5;
            default: return PointRed;
        }
    }

    public static List<SpawnEntry> BuildQueue(int waveDisplay)
    {
        int index = GetWaveIndex(waveDisplay);
        int total = NormalCounts[index] + SwarmCounts[index] + TankCounts[index];
        var queue = new List<SpawnEntry>(total + 1);
        AddType(queue, EnemyGoldType.Normal, NormalCounts[index]);
        AddType(queue, EnemyGoldType.Swarm, SwarmCounts[index]);
        AddType(queue, EnemyGoldType.Tank, TankCounts[index]);
        Shuffle(queue);
        ApplySandBuffs(queue, SandBuffBaseCounts[index]);

        if (waveDisplay >= WaveCount)
        {
            queue.Add(new SpawnEntry { Type = EnemyGoldType.Boss, SandBuff = false });
        }

        return queue;
    }

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

        // 小幅浮动：base±1
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
            + TankCounts[index] * PointBlue;
    }

    public static int GetGuaranteedTanks(int waveDisplay) => TankCounts[GetWaveIndex(waveDisplay)];
    public static float GetSpawnInterval(int waveDisplay) => SpawnIntervals[GetWaveIndex(waveDisplay)];
    public static int GetNormalCount(int waveDisplay) => NormalCounts[GetWaveIndex(waveDisplay)];
    public static int GetSwarmCount(int waveDisplay) => SwarmCounts[GetWaveIndex(waveDisplay)];
    public static int GetTankCount(int waveDisplay) => TankCounts[GetWaveIndex(waveDisplay)];
    public static int GetSandBuffBaseCount(int waveDisplay) => SandBuffBaseCounts[GetWaveIndex(waveDisplay)];

    public static int GetEnemyCount(int waveDisplay)
    {
        int index = GetWaveIndex(waveDisplay);
        int n = NormalCounts[index] + SwarmCounts[index] + TankCounts[index];
        if (waveDisplay >= WaveCount)
        {
            n += 1;
        }

        return n;
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
        int index = GetWaveIndex(waveDisplay);
        int normalHp = NormalHitPoints[index];
        int swarmHp = Mathf.CeilToInt(normalHp * 0.5f);
        int tankHp = normalHp * 4;
        // 估算：按基础沙 buff 数 × 平均 1.5 加成摊到红怪上（近似）
        int total = NormalCounts[index] * normalHp
            + SwarmCounts[index] * swarmHp
            + TankCounts[index] * tankHp;
        int sandN = SandBuffBaseCounts[index];
        if (sandN > 0)
        {
            total += Mathf.RoundToInt(normalHp * (SandBuffPowerMult - 1f) * sandN);
        }

        if (waveDisplay >= WaveCount)
        {
            total += BossHitPoints;
        }

        return total;
    }

    static int GetWaveIndex(int waveDisplay)
    {
        return Mathf.Clamp(waveDisplay, 1, WaveCount) - 1;
    }
}
