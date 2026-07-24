/// <summary>
/// 路径模块几何形状。
/// </summary>
public enum PathShape
{
    /// <summary>直通：入口 ↔ 对侧出口。</summary>
    Straight = 0,

    /// <summary>L 形拐弯：与收束器同端口表。</summary>
    Bent = 1,

    /// <summary>T 形：一柄入口 + 左右两臂出口。</summary>
    Tee = 2
}

/// <summary>
/// 路径端口解析：Straight / Bent / Tee。
/// </summary>
public static class PathGeometry
{
    /// <summary>
    /// Straight：orientation 0/2 = Left↔Right；1/3 = Up↔Down。
    /// </summary>
    public static void GetStraightPorts(int orient, out GridDirection portA, out GridDirection portB)
    {
        if ((((orient % 4) + 4) % 4) % 2 == 0)
        {
            portA = GridDirection.Left;
            portB = GridDirection.Right;
        }
        else
        {
            portA = GridDirection.Up;
            portB = GridDirection.Down;
        }
    }

    /// <summary>
    /// Tee：柄为入口侧，两臂为左右出口。
    /// orient 0：柄 Down；1：柄 Left；2：柄 Up；3：柄 Right。
    /// </summary>
    public static void GetTeePorts(
        int orient,
        out GridDirection stem,
        out GridDirection armA,
        out GridDirection armB)
    {
        switch (((orient % 4) + 4) % 4)
        {
            case 0:
                stem = GridDirection.Down;
                armA = GridDirection.Left;
                armB = GridDirection.Right;
                break;
            case 1:
                stem = GridDirection.Left;
                armA = GridDirection.Up;
                armB = GridDirection.Down;
                break;
            case 2:
                stem = GridDirection.Up;
                armA = GridDirection.Right;
                armB = GridDirection.Left;
                break;
            default:
                stem = GridDirection.Right;
                armA = GridDirection.Down;
                armB = GridDirection.Up;
                break;
        }
    }

    /// <summary>
    /// 入口侧是否合法；若合法则给出默认出口飞行方向（Straight/Bent 单出口；Tee 无效时返回 false）。
    /// </summary>
    public static bool TryResolveExit(
        PathShape shape,
        int orient,
        GridDirection entrySide,
        out GridDirection exitDirection)
    {
        exitDirection = entrySide;
        switch (shape)
        {
            case PathShape.Straight:
            {
                GetStraightPorts(orient, out GridDirection a, out GridDirection b);
                if (entrySide == a)
                {
                    exitDirection = b;
                    return true;
                }

                if (entrySide == b)
                {
                    exitDirection = a;
                    return true;
                }

                return false;
            }
            case PathShape.Bent:
                return RedirectorModule.TryGetExitDirection(orient, entrySide, out exitDirection);
            case PathShape.Tee:
            {
                GetTeePorts(orient, out GridDirection stem, out _, out _);
                if (entrySide != stem)
                {
                    return false;
                }

                // Tee 默认不给单出口；由分裂器自行取两臂
                exitDirection = GridDirectionUtil.Opposite(stem);
                return true;
            }
            default:
                return false;
        }
    }

    public static bool IsValidEntry(PathShape shape, int orient, GridDirection entrySide)
    {
        return TryResolveExit(shape, orient, entrySide, out _);
    }
}
