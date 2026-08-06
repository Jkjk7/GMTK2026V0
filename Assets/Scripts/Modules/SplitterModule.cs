using UnityEngine;

/// <summary>
/// 分裂器：T 形；原球销毁，左右口各生成一球（同能量、剩余寿命×0.5）。
/// 不可与收束器合成拐弯。
/// </summary>
public class SplitterModule : PathEffectModule
{
    public override ModuleType ModuleType => global::ModuleType.Splitter;

    protected override PathShape GetDefaultShape() => PathShape.Tee;

    protected override Color GetBodyTint() => new Color(0.85f, 0.45f, 0.95f, 1f);

    protected override bool OnValidBallEnter(EnergyBall ball, GridDirection entrySide, GridDirection defaultExit)
    {
        if (ball == null || Board == null)
        {
            return false;
        }

        float energy = ball.Energy;
        float remLife = ball.RemainingLifetime * 0.5f;
        float speed = ball.SpeedCellsPerSecond;
        Vector3 center = Board.CellToWorld(Cell);

        PathGeometry.GetTeePorts(OrientationIndex, out _, out GridDirection armA, out GridDirection armB);
        ball.Despawn();

        EnergyBallManager mgr = FindBallManager();
        if (mgr == null)
        {
            return false;
        }

        SpawnSplit(mgr, center, armA, energy, remLife, speed);
        SpawnSplit(mgr, center, armB, energy, remLife, speed);
        return false;
    }

    void SpawnSplit(
        EnergyBallManager mgr,
        Vector3 center,
        GridDirection dir,
        float energy,
        float life,
        float speed)
    {
        EnergyBall spawned = mgr.TrySpawnBall(center, dir, speed, life, energy);
        if (spawned == null)
        {
            return;
        }

        spawned.MarkCellTriggered(Cell);
        spawned.NudgeAlongDirection(0.51f);
    }
}
