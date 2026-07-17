using UnityEngine;

/// <summary>
/// 收束器（Redirector）：可旋转的 90° 直角弯道模块。
///
/// 语义：像一段直角管道，连通两个相邻方向口。
/// 球从任一连通口进入，从另一连通口出去；从非连通方向进入则不改向。
///
/// orientation 0..3 映射表（写死，方便队友核对）：
///   0: Left  ↔ Up
///   1: Up    ↔ Right
///   2: Right ↔ Down
///   3: Down  ↔ Left
///
/// 例：orientation=0 时，球向右飞（从 Left 口进入）→ 改为向上飞出。
/// </summary>
public class RedirectorModule : ModuleBase
{
    [Header("Orientation")]
    [Tooltip("0=Left↔Up, 1=Up↔Right, 2=Right↔Down, 3=Down↔Left")]
    [SerializeField] int orientation;

    SpriteRenderer _body;
    SpriteRenderer _armA;
    SpriteRenderer _armB;

    public override ModuleType ModuleType => global::ModuleType.Redirector;

    /// <summary>当前朝向 0..3。</summary>
    public int Orientation => orientation;

    /// <summary>
    /// 设置朝向（取模到 0..3）并刷新 L 形视觉。
    /// </summary>
    public void SetOrientation(int value)
    {
        orientation = ((value % 4) + 4) % 4;
        RefreshVisual();
    }

    /// <summary>
    /// 顺时针旋转 90°（放置预览或已放置后按 R）。
    /// </summary>
    public void RotateClockwise()
    {
        SetOrientation(orientation + 1);
    }

    void Awake()
    {
        EnsureVisual();
        RefreshVisual();
    }

    /// <summary>
    /// 球进入本格：若入口方向属于当前直角的连通口，则改为另一口对应的飞出方向。
    /// </summary>
    public override void OnBallEnter(EnergyBall ball)
    {
        if (ball == null)
        {
            return;
        }

        // 球的飞行方向的反面 = 它从哪一侧进入本格
        GridDirection entrySide = GridDirectionUtil.Opposite(ball.Direction);

        if (!TryGetExitDirection(orientation, entrySide, out GridDirection exitDirection))
        {
            // 非连通口：不改向，球原样穿过
            return;
        }

        ball.SetDirection(exitDirection);
    }

    /// <summary>
    /// 根据朝向与入口侧，解析出口飞行方向。
    /// </summary>
    /// <param name="orient">0..3</param>
    /// <param name="entrySide">球从哪一侧进入格子</param>
    /// <param name="exitDirection">成功时为新的飞行方向</param>
    /// <returns>入口是否属于该朝向的连通口</returns>
    public static bool TryGetExitDirection(int orient, GridDirection entrySide, out GridDirection exitDirection)
    {
        GetPorts(orient, out GridDirection portA, out GridDirection portB);

        if (entrySide == portA)
        {
            // 从 portA 进 → 朝 portB 的外侧飞出，即飞行方向 = portB
            exitDirection = portB;
            return true;
        }

        if (entrySide == portB)
        {
            exitDirection = portA;
            return true;
        }

        exitDirection = entrySide;
        return false;
    }

    /// <summary>
    /// 返回某朝向连通的两个口。
    /// </summary>
    public static void GetPorts(int orient, out GridDirection portA, out GridDirection portB)
    {
        switch (((orient % 4) + 4) % 4)
        {
            case 0:
                portA = GridDirection.Left;
                portB = GridDirection.Up;
                break;
            case 1:
                portA = GridDirection.Up;
                portB = GridDirection.Right;
                break;
            case 2:
                portA = GridDirection.Right;
                portB = GridDirection.Down;
                break;
            default:
                portA = GridDirection.Down;
                portB = GridDirection.Left;
                break;
        }
    }

    public override void RefreshVisual()
    {
        EnsureVisual();
        GetPorts(orientation, out GridDirection portA, out GridDirection portB);

        _body.color = new Color(0.4f, 0.75f, 0.95f, 1f);
        PlaceArm(_armA, portA);
        PlaceArm(_armB, portB);
    }

    void EnsureVisual()
    {
        if (_body != null)
        {
            return;
        }

        _body = gameObject.GetComponent<SpriteRenderer>();
        if (_body == null)
        {
            _body = gameObject.AddComponent<SpriteRenderer>();
        }

        _body.sprite = PrototypeSprites.Square;
        _body.sortingOrder = 8;
        transform.localScale = Vector3.one * 0.55f;

        _armA = CreateArm("ArmA");
        _armB = CreateArm("ArmB");
    }

    SpriteRenderer CreateArm(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PrototypeSprites.Square;
        sr.color = new Color(0.55f, 0.9f, 1f, 1f);
        sr.sortingOrder = 9;
        return sr;
    }

    /// <summary>
    /// 在模块本地空间沿某口方向伸出一小臂，组成 L 形提示。
    /// </summary>
    void PlaceArm(SpriteRenderer arm, GridDirection port)
    {
        Vector2 dir = GridDirectionUtil.ToWorldVector(port);
        arm.transform.localPosition = new Vector3(dir.x * 0.55f, dir.y * 0.55f, 0f);
        if (Mathf.Abs(dir.x) > 0.1f)
        {
            arm.transform.localScale = new Vector3(0.7f, 0.35f, 1f);
        }
        else
        {
            arm.transform.localScale = new Vector3(0.35f, 0.7f, 1f);
        }
    }
}
