using UnityEngine;

/// <summary>
/// 棋盘外能量球发射器。
/// 职责：按固定间隔向右发射光球，射入入口格 (0,3)。
/// 不占据任何棋盘格子，绝不写入 GridBoard 占用表。
/// </summary>
public class Emitter : MonoBehaviour
{
    [Header("Fire")]
    [Tooltip("两次发球间隔（秒）。")]
    [SerializeField] float fireInterval = 2f;

    [Tooltip("球生成点相对入口格中心的左侧偏移（以格为单位）。")]
    [SerializeField] float leftOffsetInCells = 0.85f;

    [Header("Entry")]
    [Tooltip("球飞入棋盘的入口格子。计划约定为 (0,3)。")]
    [SerializeField] int entryCol = 0;

    [SerializeField] int entryRow = 3;

    GridBoard _board;
    EnergyBallManager _ballManager;
    float _timer;
    SpriteRenderer _visual;

    /// <summary>入口格子坐标。</summary>
    public GridCoord EntryCell => new GridCoord(entryCol, entryRow);

    /// <summary>
    /// 由 GameBootstrap 注入依赖并摆到棋盘外侧。
    /// </summary>
    public void Initialize(GridBoard board, EnergyBallManager ballManager)
    {
        _board = board;
        _ballManager = ballManager;
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

        _timer += Time.deltaTime;
        if (_timer < fireInterval)
        {
            return;
        }

        _timer = 0f;
        TryFire();
    }

    /// <summary>
    /// 根据入口格中心 + 左侧偏移，把发射器放到棋盘外。
    /// 例：入口 (0,3) 中心在 x0，则发射器在 x0 - leftOffsetInCells * cellSize。
    /// </summary>
    public void SnapToOffBoardPosition()
    {
        if (_board == null)
        {
            return;
        }

        Vector3 entryCenter = _board.CellToWorld(EntryCell);
        float offset = leftOffsetInCells * _board.CellSize;
        transform.position = new Vector3(entryCenter.x - offset, entryCenter.y, 0f);
    }

    /// <summary>
    /// 尝试生成一颗向右飞的光球；若已达全场上限则跳过。
    /// </summary>
    void TryFire()
    {
        Vector3 spawnPos = transform.position;
        bool spawned = _ballManager.TrySpawn(spawnPos, GridDirection.Right);
        if (!spawned)
        {
            // 达上限：本发跳过，符合原型规则。
        }
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
        transform.localScale = new Vector3(0.55f, 0.55f, 1f);

        // 小箭头提示向右
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
