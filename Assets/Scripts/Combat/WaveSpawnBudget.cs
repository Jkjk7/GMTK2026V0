using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 波次怪物点数：红=5，黄=1，蓝=20（5 黄 = 1 红）。
/// </summary>
public static class WaveSpawnBudget
{
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

    /// <summary>为指定波次生成刷怪队列（waveDisplay 从 1 起）。</summary>
    public static List<EnemyGoldType> BuildQueue(int waveDisplay, int pointBudget, int guaranteedTanks)
    {
        var queue = new List<EnemyGoldType>();
        int remaining = Mathf.Max(0, pointBudget);

        for (int i = 0; i < guaranteedTanks && remaining >= PointBlue; i++)
        {
            queue.Add(EnemyGoldType.Tank);
            remaining -= PointBlue;
        }

        bool allowYellow = waveDisplay >= 5;
        bool allowBlue = waveDisplay >= 8;

        while (remaining > 0)
        {
            EnemyGoldType pick = PickType(waveDisplay, remaining, allowYellow, allowBlue);
            int cost = GetPointCost(pick);
            if (cost > remaining)
            {
                // 剩余不足以放红怪时，尽量塞黄怪
                if (allowYellow && remaining >= PointYellow)
                {
                    queue.Add(EnemyGoldType.Swarm);
                    remaining -= PointYellow;
                    continue;
                }

                break;
            }

            queue.Add(pick);
            remaining -= cost;
        }

        // 打乱顺序，但保底坦克可偏后
        Shuffle(queue);
        return queue;
    }

    static EnemyGoldType PickType(int wave, int remaining, bool allowYellow, bool allowBlue)
    {
        float r = Random.value;
        if (wave <= 4)
        {
            return EnemyGoldType.Normal;
        }

        if (wave <= 7)
        {
            // 红 70% / 黄 30%
            if (allowYellow && r < 0.30f && remaining >= PointYellow)
            {
                return EnemyGoldType.Swarm;
            }

            return remaining >= PointRed ? EnemyGoldType.Normal : EnemyGoldType.Swarm;
        }

        if (wave <= 10)
        {
            // 红50 黄30 蓝20
            if (allowBlue && r < 0.20f && remaining >= PointBlue)
            {
                return EnemyGoldType.Tank;
            }

            if (allowYellow && r < 0.50f && remaining >= PointYellow)
            {
                return EnemyGoldType.Swarm;
            }

            return remaining >= PointRed ? EnemyGoldType.Normal : EnemyGoldType.Swarm;
        }

        // 11-15：红35 黄35 蓝30
        if (allowBlue && r < 0.30f && remaining >= PointBlue)
        {
            return EnemyGoldType.Tank;
        }

        if (allowYellow && r < 0.65f && remaining >= PointYellow)
        {
            return EnemyGoldType.Swarm;
        }

        return remaining >= PointRed ? EnemyGoldType.Normal : EnemyGoldType.Swarm;
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
        int w = Mathf.Clamp(waveDisplay, 1, 15);
        // 渐进：约 20 → 90 点（红怪等价 4 → 18）
        return Mathf.RoundToInt(15f + w * 5f + (w >= 10 ? 10f : 0f));
    }

    public static int GetGuaranteedTanks(int waveDisplay)
    {
        if (waveDisplay == 10 || waveDisplay == 15)
        {
            return 2;
        }

        if (waveDisplay == 8 || waveDisplay == 12)
        {
            return 1;
        }

        return 0;
    }

    public static float GetSpawnInterval(int waveDisplay)
    {
        int w = Mathf.Clamp(waveDisplay, 1, 15);
        return Mathf.Max(0.35f, 1.15f - (w - 1) * 0.05f);
    }
}
