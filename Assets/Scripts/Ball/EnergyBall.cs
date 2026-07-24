using UnityEngine;

/// <summary>
/// 单颗能量光球。
/// 职责：按方向匀速飞行、寿命到期销毁、进入新格时触发模块一次。
/// 能量由发射器质量升级决定（默认 1）。
/// </summary>
public class EnergyBall : MonoBehaviour
{
    /// <summary>未升级时的默认能量。</summary>
    public const int DefaultEnergy = 1;

    [Header("Motion")]
    [Tooltip("飞行速度（格/秒）。实际世界速度 = cellsPerSecond * board.CellSize。")]
    [SerializeField] float cellsPerSecond = 4f;

    [Tooltip("存活时间（秒）；到期销毁。若 EnergyBallManager 传入正数寿命会覆盖此值。")]
    [SerializeField] float lifetimeSeconds = 12f;

    GridBoard _board;
    EnergyBallManager _manager;
    GridDirection _direction;
    float _age;
    GridCoord? _lastTriggeredCell;
    bool _alive = true;
    int _energy = DefaultEnergy;
    SpriteRenderer _visual;
    Vector3 _previousPosition;
    TextMesh _lifeLabel;

    /// <summary>当前飞行方向。</summary>
    public GridDirection Direction => _direction;

    /// <summary>本球携带的能量（受发射器质量升级影响）。</summary>
    public int Energy => _energy;

    /// <summary>是否仍在场上有效。</summary>
    public bool IsAlive => _alive;

    /// <summary>剩余寿命（秒）。</summary>
    public float RemainingLifetime => Mathf.Max(0f, lifetimeSeconds - _age);

    /// <summary>
    /// 由 EnergyBallManager 在生成时调用。
    /// </summary>
    /// <param name="board">棋盘，用于坐标换算与出界。</param>
    /// <param name="manager">管理器，销毁时回调计数。</param>
    /// <param name="worldPosition">生成世界坐标。</param>
    /// <param name="direction">初始方向。</param>
    /// <param name="speedCellsPerSecond">速度覆盖；&lt;=0 则用默认。</param>
    /// <param name="lifetime">寿命覆盖；&lt;=0 则用默认。</param>
    /// <param name="energy">球能量；&lt;=0 则用默认 1。</param>
    public void Initialize(
        GridBoard board,
        EnergyBallManager manager,
        Vector3 worldPosition,
        GridDirection direction,
        float speedCellsPerSecond = -1f,
        float lifetime = -1f,
        int energy = -1)
    {
        _board = board;
        _manager = manager;
        _direction = direction;
        _age = 0f;
        _lastTriggeredCell = null;
        _alive = true;
        _energy = energy > 0 ? energy : DefaultEnergy;

        if (speedCellsPerSecond > 0f)
        {
            cellsPerSecond = speedCellsPerSecond;
        }

        if (lifetime > 0f)
        {
            lifetimeSeconds = lifetime;
        }

        transform.position = worldPosition;
        _previousPosition = worldPosition;
        EnsureVisual();
        EnsureLifeLabel();
        RefreshLifeLabel();
    }

    /// <summary>
    /// 被收束器等模块改写飞行方向。
    /// </summary>
    public void SetDirection(GridDirection direction)
    {
        _direction = direction;
    }

    /// <summary>
    /// 将球对齐到指定世界坐标（触发改向时贴格心用）。
    /// </summary>
    public void SnapTo(Vector3 worldPosition)
    {
        transform.position = worldPosition;
        _previousPosition = worldPosition;
    }

    void Update()
    {
        if (!_alive || _board == null)
        {
            return;
        }

        _age += Time.deltaTime;
        RefreshLifeLabel();
        if (_age >= lifetimeSeconds)
        {
            Despawn();
            return;
        }

        Vector3 previous = transform.position;
        float worldSpeed = cellsPerSecond * _board.CellSize;
        Vector2 delta = GridDirectionUtil.ToWorldVector(_direction) * (worldSpeed * Time.deltaTime);
        transform.position += new Vector3(delta.x, delta.y, 0f);

        // 出界：飞出棋盘包围盒即销毁
        if (!_board.GetWorldBounds().Contains(transform.position))
        {
            if (HasEnteredBoardArea() && !IsInsideBoardExpanded())
            {
                Despawn();
                return;
            }

            if (IsClearlyPastBoard())
            {
                Despawn();
                return;
            }
        }

        TryTriggerCell(previous);
        _previousPosition = transform.position;
    }

    /// <summary>
    /// 仅当本帧运动越过格子中心时触发模块；触发前把球贴到格心，避免提前拐弯。
    /// </summary>
    void TryTriggerCell(Vector3 previousPosition)
    {
        if (!_board.TryWorldToCell(transform.position, out GridCoord cell))
        {
            return;
        }

        if (_lastTriggeredCell.HasValue && _lastTriggeredCell.Value == cell)
        {
            return;
        }

        Vector3 center = _board.CellToWorld(cell);
        Vector2 travel = GridDirectionUtil.ToWorldVector(_direction);

        // 沿飞行方向：中心前方为负，越过中心后为正
        float prevAlong = Vector2.Dot((Vector2)(previousPosition - center), travel);
        float currAlong = Vector2.Dot((Vector2)(transform.position - center), travel);
        bool crossedCenter = prevAlong < 0f && currAlong >= 0f;
        if (!crossedCenter)
        {
            return;
        }

        // 横向偏移过大则仍算未对准该格通道（斜穿边缘时不触发）
        Vector2 lateral = (Vector2)(transform.position - center) - travel * currAlong;
        if (lateral.magnitude > _board.CellSize * 0.45f)
        {
            return;
        }

        _lastTriggeredCell = cell;
        SnapTo(center);

        ModuleBase module = _board.GetModule(cell);
        if (module != null)
        {
            module.OnBallEnter(this);
        }
    }

    bool HasEnteredBoardArea()
    {
        return _board.TryWorldToCell(transform.position, out _);
    }

    bool IsInsideBoardExpanded()
    {
        Bounds b = _board.GetWorldBounds();
        // 左侧多留一格，容纳发射器到入口的飞行段
        b.Expand(new Vector3(_board.CellSize * 2f, _board.CellSize * 0.5f, 0f));
        return b.Contains(transform.position);
    }

    bool IsClearlyPastBoard()
    {
        Bounds b = _board.GetWorldBounds();
        Vector3 p = transform.position;
        // 右/上/下越界，或左侧重度越界（超过发射点更左）
        if (p.x > b.max.x + 0.05f) return true;
        if (p.y > b.max.y + 0.05f) return true;
        if (p.y < b.min.y - 0.05f) return true;
        if (p.x < b.min.x - _board.CellSize * 1.5f) return true;
        return false;
    }

    /// <summary>
    /// 销毁自身并通知管理器。
    /// </summary>
    public void Despawn()
    {
        if (!_alive)
        {
            return;
        }

        _alive = false;
        if (_manager != null)
        {
            _manager.NotifyDespawned(this);
        }

        Destroy(gameObject);
    }

    void EnsureVisual()
    {
        if (_visual == null)
        {
            _visual = GetComponent<SpriteRenderer>();
            if (_visual == null)
            {
                _visual = gameObject.AddComponent<SpriteRenderer>();
            }
        }

        _visual.sprite = PrototypeSprites.Circle;
        // 质量越高略偏暖、略大（质量档 1/2/3/4）
        float t = Mathf.Clamp01((_energy - 1) / 3f);
        _visual.color = Color.Lerp(new Color(0.55f, 0.95f, 1f, 1f), new Color(1f, 0.82f, 0.35f, 1f), t);
        _visual.sortingOrder = 20;
        float scale = 0.32f + 0.07f * Mathf.Clamp(_energy, 1, 4);
        transform.localScale = Vector3.one * scale;
    }

    void EnsureLifeLabel()
    {
        if (_lifeLabel != null)
        {
            return;
        }

        var go = new GameObject("LifeLabel");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0.85f, 0f);
        float inv = 1f / Mathf.Max(0.05f, transform.localScale.x);
        go.transform.localScale = new Vector3(0.09f * inv, 0.09f * inv, 1f);
        _lifeLabel = go.AddComponent<TextMesh>();
        _lifeLabel.anchor = TextAnchor.MiddleCenter;
        _lifeLabel.alignment = TextAlignment.Center;
        _lifeLabel.fontSize = 42;
        _lifeLabel.color = new Color(1f, 0.95f, 0.75f, 1f);
        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sortingOrder = 25;
        }
    }

    void RefreshLifeLabel()
    {
        EnsureLifeLabel();
        if (_lifeLabel == null)
        {
            return;
        }

        float rem = RemainingLifetime;
        _lifeLabel.text = rem >= 10f ? rem.ToString("0") : rem.ToString("0.0");
        _lifeLabel.color = rem <= 3f
            ? new Color(1f, 0.45f, 0.35f, 1f)
            : new Color(1f, 0.95f, 0.75f, 1f);
    }
}
