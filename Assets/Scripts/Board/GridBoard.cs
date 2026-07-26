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
    bool[,] _cursed;
    CellEnchant[,] _enchants;
    Transform _cellsRoot;
    Transform _modulesRoot;
    Transform _curseRoot;
    Transform _enchantRoot;
    SpriteRenderer[,] _curseOverlays;
    SpriteRenderer[,] _enchantOverlays;
    BoardExpandService _expand;

    static readonly Color CurseFill = new Color(0.28f, 0.08f, 0.42f, 0.72f);
    static readonly Color CurseHatch = new Color(0.7f, 0.25f, 0.95f, 0.65f);
    static readonly CellEnchant[] RandomEnchantKinds =
    {
        CellEnchant.Flame,
        CellEnchant.DamageUp,
        CellEnchant.Frost,
        CellEnchant.Shrink,
        CellEnchant.Cooldown
    };

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
        _cursed = new bool[Width, Height];
        _enchants = new CellEnchant[Width, Height];
        _curseOverlays = new SpriteRenderer[Width, Height];
        _enchantOverlays = new SpriteRenderer[Width, Height];

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
        RebuildCurseOverlays();
        RebuildEnchantOverlays();
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

        if (IsCursed(coord))
        {
            return false;
        }

        if (_expand != null && !_expand.IsBuildable(coord))
        {
            return false;
        }

        return true;
    }

    public bool IsCursed(GridCoord coord)
    {
        if (!IsInside(coord) || _cursed == null)
        {
            return false;
        }

        return _cursed[coord.Col, coord.Row];
    }

    public void SetCursed(GridCoord coord, bool cursed)
    {
        if (!IsInside(coord) || _cursed == null)
        {
            return;
        }

        _cursed[coord.Col, coord.Row] = cursed;
        if (cursed)
        {
            SetEnchant(coord, CellEnchant.None);
        }

        RefreshCurseVisual(coord);
    }

    public CellEnchant GetEnchant(GridCoord coord)
    {
        if (!IsInside(coord) || _enchants == null)
        {
            return CellEnchant.None;
        }

        return _enchants[coord.Col, coord.Row];
    }

    public void SetEnchant(GridCoord coord, CellEnchant enchant)
    {
        if (!IsInside(coord) || _enchants == null)
        {
            return;
        }

        if (enchant != CellEnchant.None && IsCursed(coord))
        {
            return;
        }

        _enchants[coord.Col, coord.Row] = enchant;
        RefreshEnchantVisual(coord);
    }

    /// <summary>
    /// 随机选取可建造无诅咒格，每格独立随机一种附魔（可覆盖旧附魔）。
    /// </summary>
    public int EnchantRandomBuildableCells(int count)
    {
        if (count <= 0 || _enchants == null)
        {
            return 0;
        }

        var candidates = new System.Collections.Generic.List<GridCoord>();
        for (int col = 0; col < Width; col++)
        {
            for (int row = 0; row < Height; row++)
            {
                var c = new GridCoord(col, row);
                if (!IsBuildableCell(c) || IsCursed(c))
                {
                    continue;
                }

                candidates.Add(c);
            }
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            int j = Random.Range(i, candidates.Count);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        int take = Mathf.Min(count, candidates.Count);
        for (int i = 0; i < take; i++)
        {
            CellEnchant kind = RandomEnchantKinds[Random.Range(0, RandomEnchantKinds.Length)];
            SetEnchant(candidates[i], kind);
        }

        return take;
    }

    public int CountEnchants()
    {
        if (_enchants == null)
        {
            return 0;
        }

        int n = 0;
        for (int col = 0; col < Width; col++)
        {
            for (int row = 0; row < Height; row++)
            {
                if (_enchants[col, row] != CellEnchant.None)
                {
                    n++;
                }
            }
        }

        return n;
    }

    public static Color GetEnchantColor(CellEnchant enchant)
    {
        switch (enchant)
        {
            case CellEnchant.Flame: return new Color(1f, 0.45f, 0.12f, 0.42f);
            case CellEnchant.DamageUp: return new Color(0.95f, 0.25f, 0.55f, 0.42f);
            case CellEnchant.Frost: return new Color(0.35f, 0.85f, 1f, 0.42f);
            case CellEnchant.Shrink: return new Color(0.65f, 0.35f, 0.95f, 0.42f);
            case CellEnchant.Cooldown: return new Color(0.35f, 0.9f, 0.45f, 0.42f);
            default: return new Color(0f, 0f, 0f, 0f);
        }
    }

    /// <summary>
    /// 随机诅咒已解锁可建造格；若格上有模块则取出（不销毁）返回列表供调用方入手/分解。
    /// </summary>
    public System.Collections.Generic.List<ModuleBase> CurseRandomBuildableCells(int count)
    {
        var result = new System.Collections.Generic.List<ModuleBase>();
        if (count <= 0)
        {
            return result;
        }

        var candidates = new System.Collections.Generic.List<GridCoord>();
        for (int col = 0; col < Width; col++)
        {
            for (int row = 0; row < Height; row++)
            {
                var c = new GridCoord(col, row);
                if (!IsBuildableCell(c) || IsCursed(c))
                {
                    continue;
                }

                candidates.Add(c);
            }
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            int j = Random.Range(i, candidates.Count);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        int take = Mathf.Min(count, candidates.Count);
        for (int i = 0; i < take; i++)
        {
            GridCoord cell = candidates[i];
            if (TryExtractModule(cell, out ModuleBase mod) && mod != null)
            {
                result.Add(mod);
            }

            SetCursed(cell, true);
        }

        return result;
    }

    void RebuildCurseOverlays()
    {
        if (_curseRoot != null)
        {
            Destroy(_curseRoot.gameObject);
        }

        _curseRoot = new GameObject("CurseOverlays").transform;
        _curseRoot.SetParent(transform, false);
        for (int col = 0; col < Width; col++)
        {
            for (int row = 0; row < Height; row++)
            {
                var cell = new GridCoord(col, row);
                var go = new GameObject($"Curse_{col}_{row}");
                go.transform.SetParent(_curseRoot, false);
                go.transform.position = CellToWorld(cell);
                go.transform.localScale = Vector3.one * (cellSize * 0.9f);
                var fill = go.AddComponent<SpriteRenderer>();
                fill.sprite = PrototypeSprites.Square;
                fill.color = CurseFill;
                fill.sortingOrder = 4;
                fill.enabled = false;
                _curseOverlays[col, row] = fill;

                var hatchGo = new GameObject("Hatch");
                hatchGo.transform.SetParent(go.transform, false);
                hatchGo.transform.localRotation = Quaternion.Euler(0f, 0f, -35f);
                hatchGo.transform.localScale = new Vector3(1.2f, 0.12f, 1f);
                var hatch = hatchGo.AddComponent<SpriteRenderer>();
                hatch.sprite = PrototypeSprites.Square;
                hatch.color = CurseHatch;
                hatch.sortingOrder = 5;
                hatch.enabled = false;
            }
        }
    }

    void RefreshCurseVisual(GridCoord coord)
    {
        if (_curseOverlays == null || !IsInside(coord))
        {
            return;
        }

        SpriteRenderer sr = _curseOverlays[coord.Col, coord.Row];
        if (sr == null)
        {
            return;
        }

        bool on = IsCursed(coord);
        sr.enabled = on;
        Transform hatch = sr.transform.childCount > 0 ? sr.transform.GetChild(0) : null;
        if (hatch != null)
        {
            var hsr = hatch.GetComponent<SpriteRenderer>();
            if (hsr != null)
            {
                hsr.enabled = on;
            }
        }
    }

    void RebuildEnchantOverlays()
    {
        if (_enchantRoot != null)
        {
            Destroy(_enchantRoot.gameObject);
        }

        _enchantRoot = new GameObject("EnchantOverlays").transform;
        _enchantRoot.SetParent(transform, false);
        for (int col = 0; col < Width; col++)
        {
            for (int row = 0; row < Height; row++)
            {
                var cell = new GridCoord(col, row);
                var go = new GameObject($"Enchant_{col}_{row}");
                go.transform.SetParent(_enchantRoot, false);
                go.transform.position = CellToWorld(cell);
                go.transform.localScale = Vector3.one * (cellSize * 0.88f);
                var fill = go.AddComponent<SpriteRenderer>();
                fill.sprite = PrototypeSprites.Square;
                fill.color = Color.clear;
                fill.sortingOrder = 3;
                fill.enabled = false;
                _enchantOverlays[col, row] = fill;
            }
        }
    }

    void RefreshEnchantVisual(GridCoord coord)
    {
        if (_enchantOverlays == null || !IsInside(coord))
        {
            return;
        }

        SpriteRenderer sr = _enchantOverlays[coord.Col, coord.Row];
        if (sr == null)
        {
            return;
        }

        CellEnchant enchant = GetEnchant(coord);
        bool on = enchant != CellEnchant.None && !IsCursed(coord);
        sr.enabled = on;
        if (on)
        {
            sr.color = GetEnchantColor(enchant);
        }
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
        Sprite cellSprite = CountdownArtResources.LoadSprite(
            CountdownArtResources.BoardCellPath,
            PrototypeSprites.Square);
        for (int col = 0; col < Width; col++)
        {
            for (int row = 0; row < Height; row++)
            {
                var go = new GameObject($"Cell_{col}_{row}");
                go.transform.SetParent(_cellsRoot, false);
                go.transform.localPosition = LocalCellCenter(new GridCoord(col, row));
                go.transform.localScale = CountdownArtResources.FitScale(
                    cellSprite,
                    cellSize * 0.92f,
                    cellSize * 0.92f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = cellSprite;
                sr.color = ((col + row) % 2 == 0)
                    ? Color.white
                    : new Color(0.92f, 0.88f, 0.8f, 1f);
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
