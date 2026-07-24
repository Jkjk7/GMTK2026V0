using UnityEngine;

/// <summary>
/// 加速：未加速过的球速度 ×1.5 并打标，只生效一次。
/// </summary>
public class AcceleratorModule : PathEffectModule
{
    public const float SpeedMultiplier = 1.5f;

    public override ModuleType ModuleType => global::ModuleType.Accelerator;

    protected override PathShape GetDefaultShape() => PathShape.Straight;

    protected override Color GetBodyTint() => new Color(1f, 0.85f, 0.25f, 1f);

    protected override bool OnValidBallEnter(EnergyBall ball, GridDirection entrySide, GridDirection defaultExit)
    {
        if (ball == null)
        {
            return false;
        }

        if (!ball.HasAccelerated)
        {
            ball.SetSpeedCellsPerSecond(ball.SpeedCellsPerSecond * SpeedMultiplier);
            ball.MarkAccelerated();
        }

        return true;
    }
}
