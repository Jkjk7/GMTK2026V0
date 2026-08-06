using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 核聚变：吸收恰好 5 颗球后射出 1 颗（能量=Σ，寿命/速度=平均）。
/// </summary>
public class FusionModule : PathEffectModule
{
    public const int BallsNeeded = 5;

    readonly List<float> _speeds = new List<float>(5);
    readonly List<float> _lives = new List<float>(5);
    readonly List<float> _energies = new List<float>(5);
    SpriteRenderer _hudFill;

    public override ModuleType ModuleType => global::ModuleType.Fusion;
    public int AbsorbedCount => _energies.Count;

    protected override PathShape GetDefaultShape() => PathShape.Straight;

    protected override Color GetBodyTint() => new Color(0.95f, 0.4f, 0.85f, 1f);

    public void ClearEnergy()
    {
        _speeds.Clear();
        _lives.Clear();
        _energies.Clear();
        RefreshVisual();
    }

    protected override bool OnValidBallEnter(EnergyBall ball, GridDirection entrySide, GridDirection defaultExit)
    {
        if (ball == null || Board == null)
        {
            return false;
        }

        _speeds.Add(ball.SpeedCellsPerSecond);
        _lives.Add(ball.RemainingLifetime);
        _energies.Add(ball.Energy);
        ball.Despawn();
        RefreshVisual();

        if (_energies.Count < BallsNeeded)
        {
            return false;
        }

        float sumE = 0f;
        float sumLife = 0f;
        float sumSpeed = 0f;
        for (int i = 0; i < _energies.Count; i++)
        {
            sumE += _energies[i];
            sumLife += _lives[i];
            sumSpeed += _speeds[i];
        }

        float avgLife = sumLife / _energies.Count;
        float avgSpeed = sumSpeed / _energies.Count;
        ClearEnergy();

        EnergyBallManager mgr = FindBallManager();
        if (mgr == null)
        {
            return false;
        }

        Vector3 center = Board.CellToWorld(Cell);
        EnergyBall spawned = mgr.TrySpawnBall(center, defaultExit, avgSpeed, avgLife, sumE);
        if (spawned != null)
        {
            spawned.MarkCellTriggered(Cell);
            spawned.NudgeAlongDirection(0.51f);
        }

        return false;
    }

    public override void RefreshVisual()
    {
        base.RefreshVisual();
        EnsureHud();
        if (_hudFill != null)
        {
            float t = BallsNeeded > 0 ? AbsorbedCount / (float)BallsNeeded : 0f;
            _hudFill.transform.localScale = new Vector3(Mathf.Clamp01(t), 1f, 1f);
        }
    }

    void EnsureHud()
    {
        if (_hudFill != null)
        {
            return;
        }

        var bg = new GameObject("FusionHud");
        bg.transform.SetParent(transform, false);
        bg.transform.localPosition = new Vector3(0f, -0.7f, 0f);
        bg.transform.localScale = new Vector3(1.1f, 0.18f, 1f);
        var bgSr = bg.AddComponent<SpriteRenderer>();
        bgSr.sprite = PrototypeSprites.Square;
        bgSr.color = new Color(0.1f, 0.1f, 0.12f, 0.85f);
        bgSr.sortingOrder = 11;

        var fill = new GameObject("Fill");
        fill.transform.SetParent(bg.transform, false);
        fill.transform.localScale = new Vector3(0f, 1f, 1f);
        _hudFill = fill.AddComponent<SpriteRenderer>();
        _hudFill.sprite = PrototypeSprites.Square;
        _hudFill.color = new Color(1f, 0.4f, 0.9f, 1f);
        _hudFill.sortingOrder = 12;
    }
}
