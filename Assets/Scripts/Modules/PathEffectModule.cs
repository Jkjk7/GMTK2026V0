using UnityEngine;

/// <summary>
/// 路径功能模块基类：可旋转，持有 PathShape（Straight/Bent/Tee）与朝向。
/// OnBallEnter 校验入口后交给子类处理效果与改向。
/// </summary>
public abstract class PathEffectModule : ModuleBase
{
    [SerializeField] int orientation;
    [SerializeField] PathShape shape = PathShape.Straight;

    SpriteRenderer _body;
    SpriteRenderer _armA;
    SpriteRenderer _armB;
    SpriteRenderer _armC;

    public override bool CanRotate => true;

    public override int OrientationIndex => orientation;

    public PathShape Shape => shape;

    void Awake()
    {
        shape = GetDefaultShape();
        RefreshVisual();
    }

    public override void ApplyCardData(ModuleCardData data)
    {
        base.ApplyCardData(data);
        // Tee（分裂器）不可拐弯；其余按卡牌 Bent 切换
        if (GetDefaultShape() != PathShape.Tee)
        {
            shape = data.Bent ? PathShape.Bent : PathShape.Straight;
        }
        else
        {
            shape = PathShape.Tee;
        }

        RefreshVisual();
    }

    public override void RotateClockwise()
    {
        orientation = (orientation + 1) % 4;
        RefreshVisual();
    }

    public override void SetOrientationIndex(int value)
    {
        orientation = ((value % 4) + 4) % 4;
        RefreshVisual();
    }

    /// <summary>子类默认形状（未 Bent 时）。</summary>
    protected abstract PathShape GetDefaultShape();

    /// <summary>
    /// 球合法进入后执行效果。返回 true 表示球仍存活且应由基类按出口改向；
    /// 返回 false 表示子类已自行处理球（销毁/传送/分裂等）。
    /// </summary>
    protected abstract bool OnValidBallEnter(EnergyBall ball, GridDirection entrySide, GridDirection defaultExit);

    public override void OnBallEnter(EnergyBall ball)
    {
        if (ball == null || !ball.IsAlive)
        {
            return;
        }

        GridDirection entrySide = GridDirectionUtil.Opposite(ball.Direction);
        if (!PathGeometry.TryResolveExit(shape, orientation, entrySide, out GridDirection exitDir))
        {
            return;
        }

        if (!OnValidBallEnter(ball, entrySide, exitDir))
        {
            return;
        }

        if (ball != null && ball.IsAlive && shape != PathShape.Tee)
        {
            ball.SetDirection(exitDir);
        }
    }

    public override void RefreshVisual()
    {
        EnsureVisual();
        Color tint = GetBodyTint();
        _body.color = tint;

        if (shape == PathShape.Tee)
        {
            PathGeometry.GetTeePorts(orientation, out GridDirection stem, out GridDirection a, out GridDirection b);
            PlaceArm(_armA, stem, tint);
            PlaceArm(_armB, a, tint);
            PlaceArm(_armC, b, tint);
            _armC.enabled = true;
            _body.transform.localScale = Vector3.one * 0.35f;
        }
        else if (shape == PathShape.Bent)
        {
            RedirectorModule.GetPorts(orientation, out GridDirection a, out GridDirection b);
            PlaceArm(_armA, a, tint);
            PlaceArm(_armB, b, tint);
            _armC.enabled = false;
            _body.transform.localScale = Vector3.one * 0.4f;
        }
        else
        {
            PathGeometry.GetStraightPorts(orientation, out GridDirection a, out GridDirection b);
            PlaceArm(_armA, a, tint);
            PlaceArm(_armB, b, tint);
            _armC.enabled = false;
            _body.transform.localScale = Vector3.one * 0.45f;
        }
    }

    protected virtual Color GetBodyTint()
    {
        return new Color(0.55f, 0.85f, 0.7f, 1f);
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
        _armC = CreateArm("ArmC");
        _armC.enabled = false;
    }

    SpriteRenderer CreateArm(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PrototypeSprites.Square;
        sr.color = new Color(0.7f, 0.95f, 0.85f, 1f);
        sr.sortingOrder = 9;
        return sr;
    }

    void PlaceArm(SpriteRenderer arm, GridDirection port, Color tint)
    {
        arm.enabled = true;
        Vector2 dir = GridDirectionUtil.ToWorldVector(port);
        arm.transform.localPosition = new Vector3(dir.x * 0.55f, dir.y * 0.55f, 0f);
        arm.color = Color.Lerp(tint, Color.white, 0.25f);
        if (Mathf.Abs(dir.x) > 0.1f)
        {
            arm.transform.localScale = new Vector3(0.7f, 0.35f, 1f);
        }
        else
        {
            arm.transform.localScale = new Vector3(0.35f, 0.7f, 1f);
        }
    }

    protected EnergyBallManager FindBallManager()
    {
        return FindObjectOfType<EnergyBallManager>();
    }
}
