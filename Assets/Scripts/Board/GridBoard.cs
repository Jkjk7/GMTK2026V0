using UnityEngine;

/// <summary>
/// 7×7 逻辑棋盘。坐标全部相对本物体 Transform，移动 BoardRoot 不会破坏换算。
/// 约定：localOrigin 为棋盘左下角（格 (0,0) 的左下角）；(0,0)=左下。
/// </summary>
public class GridBoard : MonoBehaviour
{
    public const int Width = 7;
    public const int Height = 7;

    [Header("Layout")]
    [SerializeField] float cellSize = 1f;

    [Tooltip("棋盘左下角（格角）在本地空间的位置。")]
    [SerializeField] Vector2 localOrigin = Vector2.zero;

    [Header("Visual")]
    [SerializeField] Color evenCellColor = new Color(0.18f, 0.2f, 0.24f, 1f);
    [SerializeField] Color oddCellColor = new Color(0.22f, 0.25f, 0.3f, 1f);
    [SerializeField] Color gridLineColor = new Color(0.35f, 0.4f, 0.48f, 1f);

    ModuleBase[,] _modules;
    Transform _cellsRoot;
    Transform _modulesRoot;
    BoardExpandService _expand;

    public float CellSize => cellSize;
    public Vector2 LocalOrigin => localOrigin;
    public Transform ModulesRoot => _modulesRoot;

    public void BindExpandService(BoardExpandService expand)
    {
        _expand = expand;
    }

    /// <summary>
    /// 初始化：localOrigin 为本地左下角；可把 ModulesRoot 挂到棋盘下。
    /// </summary>
    public void Initialize(float size, Vector2 originLocal = default)
    {
        cellSize = size;
        localOrigin = originLocal;
        _modules = new ModuleBase[Width, Height];

        if (_cellsRoot != null)
        {
            Destroy(_cellsRoot.gameObject);
        }

        _cellsRoot = new GameObject("Cells").transform;
        _cellsRoot.SetParent(transform, false);

        if (_modulesRoot == null)
        {
            var modulesGo = new GameObject("Modules");
            modulesGo.transform.SetParent(transform, false);
            _modulesRoot = modulesGo.transform;
        }

        BuildCellVisuals();
    }

    /// <summary>格子中心（本地坐标）。</summary>
    public Vector3 LocalCellCenter(GridCoord coord)
    {
        return new Vector3(
            localOrigin.x + (coord.Col + 0.5f) * cellSize,
            localOrigin.y + (coord.Row + 0.5f) * cellSize,
            0f);
    }

    /// <summary>格子中心的世界坐标。</summary>
    public Vector3 CellToWorld(GridCoord coord)
    {
        return transform.TransformPoint(LocalCellCenter(coord));
    }

    /// <summary>世界坐标 → 格子；越界返回 false。</summary>
    public bool TryWorldToCell(Vector3 worldPosition, out GridCoord cell)
    {
        Vector3 local = transform.InverseTransformPoint(worldPosition);
        float relativeX = local.x - localOrigin.x;
        float relativeY = local.y - localOrigin.y;

        int col = Mathf.FloorToInt(relativeX / cellSize);
        int row = Mathf.FloorToInt(relativeY / cellSize);

        cell = new GridCoord(col, row);
        if (!IsInside(cell))
        {
            cell = default;
            return false;
        }

        return true;
    }

    public bool IsInside(GridCoord coord)
    {
        return coord.Col >= 0 && coord.Col < Width && coord.Row >= 0 && coord.Row < Height;
    }

    public ModuleBase GetModule(GridCoord coord)
    {
        if (!IsInside(coord) || _modules == null)
        {
            return null;
        }

        return _modules[coord.Col, coord.Row];
    }

    public bool CanPlace(GridCoord coord)
    {
        if (!IsInside(coord) || GetModule(coord) != null)
        {
            return false;
        }

        if (_expand != null && !_expand.IsBuildable(coord))
        {
            return false;
        }

        return true;
    }

    public bool IsBuildableCell(GridCoord coord)
    {
        if (!IsInside(coord))
        {
            return false;
        }

        return _expand == null || _expand.IsBuildable(coord);
    }

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

    public void ClearCell(GridCoord coord)
    {
        TryRemoveModule(coord, out ModuleType _);
    }

    /// <summary>取出模块但不销毁（挪位/合成用）。</summary>
    public bool TryExtractModule(GridCoord coord, out ModuleBase module)
    {
        module = null;
        if (!IsInside(coord) || _modules == null)
        {
            return false;
        }

        module = _modules[coord.Col, coord.Row];
        if (module == null)
        {
            return false;
        }

        _modules[coord.Col, coord.Row] = null;
        return true;
    }

    public bool TryRemoveModule(GridCoord coord, out ModuleType moduleType)
    {
        moduleType = default;
        if (!TryExtractModule(coord, out ModuleBase existing))
        {
            return false;
        }

        moduleType = existing.ModuleType;
        Destroy(existing.gameObject);
        return true;
    }

    public bool TryRemoveModule(GridCoord coord, out ModuleCardData card)
    {
        card = default;
        if (!TryExtractModule(coord, out ModuleBase existing))
        {
            return false;
        }

        card = existing.CardData;
        Destroy(existing.gameObject);
        return true;
    }

    public Bounds GetWorldBounds()
    {
        Vector3 min = CellToWorld(new GridCoord(0, 0)) - transform.TransformVector(new Vector3(cellSize * 0.5f, cellSize * 0.5f, 0f));
        Vector3 max = CellToWorld(new GridCoord(Width - 1, Height - 1)) + transform.TransformVector(new Vector3(cellSize * 0.5f, cellSize * 0.5f, 0f));
        // 无旋转时 TransformVector 与世界轴对齐；简化用轴对齐盒
        Vector3 localMin = LocalCellCenter(new GridCoord(0, 0)) - new Vector3(cellSize * 0.5f, cellSize * 0.5f, 0f);
        Vector3 localMax = LocalCellCenter(new GridCoord(Width - 1, Height - 1)) + new Vector3(cellSize * 0.5f, cellSize * 0.5f, 0f);
        min = transform.TransformPoint(localMin);
        max = transform.TransformPoint(localMax);
        Vector3 center = (min + max) * 0.5f;
        return new Bounds(center, max - min);
    }

    void BuildCellVisuals()
    {
        for (int col = 0; col < Width; col++)
        {
            for (int row = 0; row < Height; row++)
            {
                var go = new GameObject($"Cell_{col}_{row}");
                go.transform.SetParent(_cellsRoot, false);
                go.transform.localPosition = LocalCellCenter(new GridCoord(col, row));
                go.transform.localScale = Vector3.one * (cellSize * 0.92f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = PrototypeSprites.Square;
                sr.color = ((col + row) % 2 == 0) ? evenCellColor : oddCellColor;
                sr.sortingOrder = 0;

                var cellView = go.AddComponent<GridCellView>();
                cellView.Bind(sr);
            }
        }

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

    void CreateBorderBar(string name, Vector3 worldPos, Vector3 scale)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_cellsRoot, false);
        go.transform.position = worldPos;
        go.transform.localScale = scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PrototypeSprites.Square;
        sr.color = gridLineColor;
        sr.sortingOrder = 1;
    }
}
