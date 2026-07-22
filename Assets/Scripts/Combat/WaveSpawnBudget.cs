using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// V0.1 固定波次表：每波红/黄/蓝配额固定，仅随机出场顺序。
/// HP 每 5 波提升一档；数量与短间隔制造怪潮，避免循环激光逐个消化。
/// </summary>
public static class WaveSpawnBudget
{
    const int WaveCount = 15;

    static readonly int[] NormalCounts =
    {
        6, 8, 10, 14, 12,
        14, 16, 18, 20, 24,
        26, 28, 30, 32, 36
    };

    static readonly int[] SwarmCounts =
    {
        0, 0, 0, 0, 10,
        12, 16, 20, 26, 36,
        40, 44, 48, 52, 60
    };

    static readonly int[] TankCounts =
    {
        0, 0, 0, 0, 0,
        0, 0, 2, 3, 5,
        5, 6, 7, 8, 10
    };

    static readonly int[] NormalHitPoints =
    {
        10, 10, 10, 10, 10,
        25, 25, 25, 25, 25,
        50, 50, 50, 50, 50
    };

    static readonly float[] SpawnIntervals =
    {
        0.65f, 0.60f, 0.55f, 0.48f, 0.30f,
        0.50f, 0.42f, 0.34f, 0.26f, 0.16f,
        0.38f, 0.30f, 0.24f, 0.18f, 0.12f
    };

    // 保留点数常量供旧存档/调试显示使用；刷怪队列不再按点数随机抽型。
    public const int PointRed = 5;
    public const int PointYellow = 1;
    public const int PointBlue = 20;

    public static int GetPointCost(EnemyGoldType type)
    {
        switch (type)
        {
            case EnemyGoldType.Swarm: return PointYellow;
            case EnemyGoldType.Tank: return PointBlue;
            default: return PointRed;
        }
    }

    /// <summary>为指定波次生成固定配额队列（waveDisplay 从 1 起）。</summary>
    public static List<EnemyGoldType> BuildQueue(int waveDisplay)
    {
        int index = GetWaveIndex(waveDisplay);
        int total = NormalCounts[index] + SwarmCounts[index] + TankCounts[index];
        var queue = new List<EnemyGoldType>(total);
        AddType(queue, EnemyGoldType.Normal, NormalCounts[index]);
        AddType(queue, EnemyGoldType.Swarm, SwarmCounts[index]);
        AddType(queue, EnemyGoldType.Tank, TankCounts[index]);
        Shuffle(queue);
        return queue;
    }

    /// <summary>兼容旧调用；V0.1 固定表会忽略点数预算和保底蓝参数。</summary>
    public static List<EnemyGoldType> BuildQueue(int waveDisplay, int pointBudget, int guaranteedTanks)
    {
        return BuildQueue(waveDisplay);
    }

    static void AddType(List<EnemyGoldType> queue, EnemyGoldType type, int count)
    {
        for (int i = 0; i < count; i++)
        {
            queue.Add(type);
        }
    }

    static void Shuffle(List<EnemyGoldType> list)
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

    public static int GetGuaranteedTanks(int waveDisplay)
    {
        return TankCounts[GetWaveIndex(waveDisplay)];
    }

    public static float GetSpawnInterval(int waveDisplay)
    {
        return SpawnIntervals[GetWaveIndex(waveDisplay)];
    }

    public static int GetNormalCount(int waveDisplay)
    {
        return NormalCounts[GetWaveIndex(waveDisplay)];
    }

    public static int GetSwarmCount(int waveDisplay)
    {
        return SwarmCounts[GetWaveIndex(waveDisplay)];
    }

    public static int GetTankCount(int waveDisplay)
    {
        return TankCounts[GetWaveIndex(waveDisplay)];
    }

    public static int GetEnemyCount(int waveDisplay)
    {
        int index = GetWaveIndex(waveDisplay);
        return NormalCounts[index] + SwarmCounts[index] + TankCounts[index];
    }

    public static int GetHitPoints(int waveDisplay, EnemyGoldType type)
    {
        int normalHp = NormalHitPoints[GetWaveIndex(waveDisplay)];
        switch (type)
        {
            case EnemyGoldType.Swarm:
                return Mathf.CeilToInt(normalHp * 0.5f);
            case EnemyGoldType.Tank:
                return normalHp * 4;
            default:
                return normalHp;
        }
    }

    public static int GetTotalHitPoints(int waveDisplay)
    {
        int index = GetWaveIndex(waveDisplay);
        int normalHp = NormalHitPoints[index];
        int swarmHp = Mathf.CeilToInt(normalHp * 0.5f);
        int tankHp = normalHp * 4;
        return NormalCounts[index] * normalHp
            + SwarmCounts[index] * swarmHp
            + TankCounts[index] * tankHp;
    }

    static int GetWaveIndex(int waveDisplay)
    {
        return Mathf.Clamp(waveDisplay, 1, WaveCount) - 1;
    }
}
