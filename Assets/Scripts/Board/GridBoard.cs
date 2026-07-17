using UnityEngine;

/// <summary>
/// 7×7 逻辑棋盘。
/// 职责：格子占用、世界坐标 ↔ 格子换算、放置/查询模块。
/// 不负责：发射器（棋盘外）、战斗区、手牌 UI。
/// 约定：(0,0)=左下；col 向右 0..6；row 向上 0..6。
/// </summary>
public class GridBoard : MonoBehaviour
{
    public const int Width = 7;
    public const int Height = 7;

    [Header("Layout")]
    [Tooltip("每格世界边长。")]
    [SerializeField] float cellSize = 1f;

    [Tooltip("棋盘左下角格子中心的世界坐标（即 (0,0) 中心）。")]
    [SerializeField] Vector2 originCellCenter = Vector2.zero;

    [Header("Visual")]
    [SerializeField] Color evenCellColor = new Color(0.18f, 0.2f, 0.24f, 1f);
    [SerializeField] Color oddCellColor = new Color(0.22f, 0.25f, 0.3f, 1f);
    [SerializeField] Color gridLineColor = new Color(0.35f, 0.4f, 0.48f, 1f);

    ModuleBase[,] _modules;
    Transform _cellsRoot;

    /// <summary>每格边长（世界单位）。</summary>
    public float CellSize => cellSize;

    /// <summary>棋盘左下角格子中心世界坐标。</summary>
    public Vector2 OriginCellCenter => originCellCenter;

    /// <summary>
    /// 初始化占用表并绘制格子底板。应在 GameBootstrap 配置 origin 后调用。
    /// </summary>
    public void Initialize(Vector2 originCenter, float size)
    {
        originCellCenter = originCenter;
        cellSize = size;
        _modules = new ModuleBase[Width, Height];

        if (_cellsRoot != null)
        {
            Destroy(_cellsRoot.gameObject);
        }

        _cellsRoot = new GameObject("Cells").transform;
        _cellsRoot.SetParent(transform, false);
        BuildCellVisuals();
    }

    /// <summary>
    /// 格子中心的世界坐标。
    /// </summary>
    public Vector3 CellToWorld(GridCoord coord)
    {
        float x = originCellCenter.x + coord.Col * cellSize;
        float y = originCellCenter.y + coord.Row * cellSize;
        return new Vector3(x, y, 0f);
    }

    /// <summary>
    /// 世界坐标落点对应的格子。越界时返回 false。
    /// </summary>
    public bool TryWorldToCell(Vector3 world, out GridCoord coord)
    {
        float localX = (world.x - originCellCenter.x) / cellSize;
        float localY = (world.y - originCellCenter.y) / cellSize;
        int col = Mathf.RoundToInt(localX);
        int row = Mathf.RoundToInt(localY);
        coord = new GridCoord(col, row);
        return IsInside(coord);
    }

    /// <summary>
    /// 是否在 7×7 范围内。
    /// </summary>
    public bool IsInside(GridCoord coord)
    {
        return coord.Col >= 0 && coord.Col < Width && coord.Row >= 0 && coord.Row < Height;
    }

    /// <summary>
    /// 查询某格模块；空格返回 null。
    /// </summary>
    public ModuleBase GetModule(GridCoord coord)
    {
        if (!IsInside(coord) || _modules == null)
        {
            return null;
        }

        return _modules[coord.Col, coord.Row];
    }

    /// <summary>
    /// 该格是否可放置（在界内且当前无模块）。
    /// </summary>
    public bool CanPlace(GridCoord coord)
    {
        return IsInside(coord) && GetModule(coord) == null;
    }

    /// <summary>
    /// 将模块登记到格子。调用方负责把模块 transform 摆到格子中心。
    /// </summary>
    public bool TryPlaceModule(GridCoord coord, ModuleBase module)
    {
        if (!CanPlace(coord) || module == null)
        {
            return false;
        }

        _modules[coord.Col, coord.Row] = module;
        module.BindToCell(this, coord);
        module.transform.position = CellToWorld(coord);
        return true;
    }

    /// <summary>
    /// 移除并销毁某格模块。
    /// </summary>
    public void ClearCell(GridCoord coord)
    {
        TryRemoveModule(coord, out _);
    }

    /// <summary>
    /// 拆除模块并返回其类型，供手牌回收。
    /// </summary>
    public bool TryRemoveModule(GridCoord coord, out ModuleType moduleType)
    {
        moduleType = default;
        if (!IsInside(coord) || _modules == null)
        {
            return false;
        }

        ModuleBase existing = _modules[coord.Col, coord.Row];
        if (existing == null)
        {
            return false;
        }

        moduleType = existing.ModuleType;
        Destroy(existing.gameObject);
        _modules[coord.Col, coord.Row] = null;
        return true;
    }

    /// <summary>
    /// 棋盘外扩半格的世界包围盒，用于球出界判定。
    /// </summary>
    public Bounds GetWorldBounds()
    {
        Vector3 min = CellToWorld(new GridCoord(0, 0)) - new Vector3(cellSize * 0.5f, cellSize * 0.5f, 0f);
        Vector3 max = CellToWorld(new GridCoord(Width - 1, Height - 1)) + new Vector3(cellSize * 0.5f, cellSize * 0.5f, 0f);
        Vector3 center = (min + max) * 0.5f;
        Vector3 size = max - min;
        return new Bounds(center, size);
    }

    void BuildCellVisuals()
    {
        for (int col = 0; col < Width; col++)
        {
            for (int row = 0; row < Height; row++)
            {
                var go = new GameObject($"Cell_{col}_{row}");
                go.transform.SetParent(_cellsRoot, false);
                go.transform.position = CellToWorld(new GridCoord(col, row));
                go.transform.localScale = Vector3.one * (cellSize * 0.92f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = PrototypeSprites.Square;
                sr.color = ((col + row) % 2 == 0) ? evenCellColor : oddCellColor;
                sr.sortingOrder = 0;
            }
        }

        // 外框线（四个边用细长方块）
        DrawBorder();
    }

    void DrawBorder()
    {
        Bounds b = GetWorldBounds();
        CreateBorderBar("Border_Bottom", new Vector3(b.center.x, b.min.y, 0f), new Vector3(b.size.x, 0.06f, 1f));
        CreateBorderBar("Border_Top", new Vector3(b.center.x, b.max.y, 0f), new Vector3(b.size.x, 0.06f, 1f));
        CreateBorderBar("Border_Left", new Vector3(b.min.x, b.center.y, 0f), new Vector3(0.06f, b.size.y, 1f));
        CreateBorderBar("Border_Right", new Vector3(b.max.x, b.center.y, 0f), new Vector3(0.06f, b.size.y, 1f));
    }

    void CreateBorderBar(string name, Vector3 pos, Vector3 scale)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_cellsRoot, false);
        go.transform.position = pos;
        go.transform.localScale = scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PrototypeSprites.Square;
        sr.color = gridLineColor;
        sr.sortingOrder = 1;
    }
}
