using UnityEngine;

/// <summary>
/// 棋盘外能量球发射器。
/// 职责：按固定间隔向右发射光球，射入入口格 (0,3)。
/// 位置相对棋盘 Transform，随 BoardRoot 移动。
/// </summary>
public class Emitter : MonoBehaviour
{
    [Header("Fire")]
    [SerializeField] float fireInterval = 2f;

    [Tooltip("球生成点相对入口格中心的左侧偏移（以格为单位）。")]
    [SerializeField] float leftOffsetInCells = 0.85f;

    [Header("Entry")]
    [SerializeField] int entryCol = 0;
    [SerializeField] int entryRow = 3;

    GridBoard _board;
    EnergyBallManager _ballManager;
    GameSession _session;
    float _timer;
    SpriteRenderer _visual;
    Vector3 _baseScale = new Vector3(0.55f, 0.55f, 1f);

    public GridCoord EntryCell => new GridCoord(entryCol, entryRow);

    public void Initialize(GridBoard board, EnergyBallManager ballManager, GameSession session)
    {
        _board = board;
        _ballManager = ballManager;
        _session = session;
        _timer = 0f;
        SnapToOffBoardPosition();
        EnsureVisual();
    }

    void Update()
    {
        if (_board == null || _ballManager == null)
        {
            return;
        }

        // 仅战斗阶段发球；准备/胜负停止
        if (_session != null && !_session.IsCombatActive)
        {
            return;
        }

        _timer += Time.deltaTime;
        // 呼吸缩放反馈
        if (_visual != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 2.4f) * 0.06f;
            transform.localScale = _baseScale * pulse;
        }

        float interval = GetFireInterval();
        if (_timer < interval)
        {
            return;
        }

        _timer = 0f;
        TryFire();
    }

    float GetFireInterval()
    {
        if (EmitterRunUpgrades.Instance != null)
        {
            return EmitterRunUpgrades.Instance.FireInterval;
        }

        return fireInterval > 0.01f ? fireInterval : 2f;
    }

    public void SnapToOffBoardPosition()
    {
        if (_board == null)
        {
            return;
        }

        Vector3 entryCenter = _board.CellToWorld(EntryCell);
        Vector3 left = _board.transform.TransformVector(Vector3.left * leftOffsetInCells * _board.CellSize);
        transform.position = entryCenter + left;
    }

    void TryFire()
    {
        _ballManager.TrySpawn(transform.position, GridDirection.Right);
    }

    void EnsureVisual()
    {
        if (_visual != null)
        {
            return;
        }

        _visual = gameObject.GetComponent<SpriteRenderer>();
        if (_visual == null)
        {
            _visual = gameObject.AddComponent<SpriteRenderer>();
        }

        _visual.sprite = PrototypeSprites.Square;
        _visual.color = new Color(0.95f, 0.75f, 0.2f, 1f);
        _visual.sortingOrder = 5;
        _baseScale = new Vector3(0.55f, 0.55f, 1f);
        transform.localScale = _baseScale;

        var arrow = new GameObject("Arrow");
        arrow.transform.SetParent(transform, false);
        arrow.transform.localPosition = new Vector3(0.55f, 0f, 0f);
        arrow.transform.localScale = new Vector3(0.35f, 0.2f, 1f);
        var asr = arrow.AddComponent<SpriteRenderer>();
        asr.sprite = PrototypeSprites.Square;
        asr.color = new Color(1f, 0.9f, 0.4f, 1f);
        asr.sortingOrder = 6;
    }
}
