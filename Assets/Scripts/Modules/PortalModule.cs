using UnityEngine;

/// <summary>
/// 传送门：场上最多 2 个；成对时保持世界方向传送到另一门格心。
/// </summary>
public class PortalModule : PathEffectModule
{
    static int _teleportFrame = -1;
    static EnergyBall _teleportBall;

    public override ModuleType ModuleType => global::ModuleType.Portal;

    protected override PathShape GetDefaultShape() => PathShape.Straight;

    protected override Color GetBodyTint() => new Color(0.45f, 0.35f, 0.95f, 1f);

    protected override bool OnValidBallEnter(EnergyBall ball, GridDirection entrySide, GridDirection defaultExit)
    {
        if (ball == null || Board == null)
        {
            return false;
        }

        // 同帧防环：刚被传送的球不再进门
        if (_teleportBall == ball && _teleportFrame == Time.frameCount)
        {
            ball.SetDirection(defaultExit);
            return false;
        }

        PortalModule other = FindPartner();
        if (other == null || other.Board == null)
        {
            // 单门：直通改向
            return true;
        }

        Vector3 dest = other.Board.CellToWorld(other.Cell);
        GridDirection keepDir = ball.Direction;
        ball.SnapTo(dest);
        ball.SetDirection(keepDir);
        ball.ClearLastTriggeredCell();
        ball.MarkCellTriggered(other.Cell);
        ball.NudgeAlongDirection(0.51f);

        _teleportBall = ball;
        _teleportFrame = Time.frameCount;
        return false;
    }

    PortalModule FindPartner()
    {
        PortalModule[] portals = FindObjectsOfType<PortalModule>();
        for (int i = 0; i < portals.Length; i++)
        {
            PortalModule p = portals[i];
            if (p == null || p == this || p.BoundBoard == null)
            {
                continue;
            }

            return p;
        }

        return null;
    }

    public static int CountOnBoard()
    {
        int n = 0;
        PortalModule[] portals = FindObjectsOfType<PortalModule>();
        for (int i = 0; i < portals.Length; i++)
        {
            if (portals[i] != null && portals[i].BoundBoard != null)
            {
                n++;
            }
        }

        return n;
    }
}
