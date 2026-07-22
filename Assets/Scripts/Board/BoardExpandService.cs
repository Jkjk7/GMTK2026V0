using UnityEngine;

/// <summary>
/// 棋盘可建造窗口：逻辑仍 7×7，外围锁定；3→5→7 付费解锁。
/// </summary>
public class BoardExpandService : MonoBehaviour
{
    public const int Size3 = 3;
    public const int Size5 = 5;
    public const int Size7 = 7;

    static readonly Color LockFill = new Color(0.04f, 0.04f, 0.06f, 0.88f);
    static readonly Color LockHatch = new Color(0.55f, 0.18f, 0.22f, 0.55f);
    static readonly Color PlayableWash = new Color(0.28f, 0.72f, 0.42f, 0.16f);

    GridBoard _board;
    int _unlockedSize = Size3;
    SpriteRenderer[] _lockOverlays;
    SpriteRenderer[] _lockHatches;
    SpriteRenderer[] _playableWashes;

    public int UnlockedSize => _unlockedSize;

    public void Initialize(GridBoard board, int startingSize = Size3)
    {
        _board = board;
        _unlockedSize = Mathf.Clamp(startingSize, Size3, Size7);
        if (_unlockedSize % 2 == 0)
        {
            _unlockedSize = Size3;
        }

        RebuildLockOverlays();
        RefreshLockVisuals();
    }

    public bool IsBuildable(GridCoord coord)
    {
        if (_board == null || !_board.IsInside(coord))
        {
            return false;
        }

        GetWindow(out int minCol, out int maxCol, out int minRow, out int maxRow);
        return coord.Col >= minCol && coord.Col <= maxCol &&
               coord.Row >= minRow && coord.Row <= maxRow;
    }

    public int GetNextExpandCost()
    {
        if (_unlockedSize < Size5)
        {
            return ModulePricing.BoardExpandTo5Cost;
        }

        if (_unlockedSize < Size7)
        {
            return ModulePricing.BoardExpandTo7Cost;
        }

        return 0;
    }

    public int GetNextSize()
    {
        if (_unlockedSize < Size5)
        {
            return Size5;
        }

        if (_unlockedSize < Size7)
        {
            return Size7;
        }

        return Size7;
    }

    public bool TryExpand()
    {
        int cost = GetNextExpandCost();
        if (cost <= 0)
        {
            return false;
        }

        if (Economy.Instance != null && !Economy.Instance.TrySpend(cost))
        {
            return false;
        }

        _unlockedSize = GetNextSize();
        RefreshLockVisuals();
        return true;
    }

    void GetWindow(out int minCol, out int maxCol, out int minRow, out int maxRow)
    {
        int size = _unlockedSize;
        int pad = (GridBoard.Width - size) / 2;
        minCol = pad;
        maxCol = pad + size - 1;
        minRow = pad;
        maxRow = pad + size - 1;
    }

    void RebuildLockOverlays()
    {
        if (_board == null)
        {
            return;
        }

        Transform root = _board.transform.Find("LockOverlays");
        if (root != null)
        {
            Destroy(root.gameObject);
        }

        var go = new GameObject("LockOverlays");
        go.transform.SetParent(_board.transform, false);
        int count = GridBoard.Width * GridBoard.Height;
        _lockOverlays = new SpriteRenderer[count];
        _lockHatches = new SpriteRenderer[count];
        _playableWashes = new SpriteRenderer[count];
        float cell = _board.CellSize;
        int i = 0;
        for (int col = 0; col < GridBoard.Width; col++)
        {
            for (int row = 0; row < GridBoard.Height; row++)
            {
                Vector3 pos = _board.CellToWorld(new GridCoord(col, row));

                var cellGo = new GameObject($"Lock_{col}_{row}");
                cellGo.transform.SetParent(go.transform, false);
                cellGo.transform.position = pos;
                cellGo.transform.localScale = Vector3.one * (cell * 0.94f);
                var fill = cellGo.AddComponent<SpriteRenderer>();
                fill.sprite = PrototypeSprites.Square;
                fill.color = LockFill;
                fill.sortingOrder = 2;
                _lockOverlays[i] = fill;

                // 斜杠：锁定格一眼可辨
                var hatchGo = new GameObject("Hatch");
                hatchGo.transform.SetParent(cellGo.transform, false);
                hatchGo.transform.localPosition = Vector3.zero;
                hatchGo.transform.localRotation = Quaternion.Euler(0f, 0f, 38f);
                hatchGo.transform.localScale = new Vector3(1.15f, 0.14f, 1f);
                var hatch = hatchGo.AddComponent<SpriteRenderer>();
                hatch.sprite = PrototypeSprites.Square;
                hatch.color = LockHatch;
                hatch.sortingOrder = 3;
                _lockHatches[i] = hatch;

                var washGo = new GameObject($"Playable_{col}_{row}");
                washGo.transform.SetParent(go.transform, false);
                washGo.transform.position = pos;
                washGo.transform.localScale = Vector3.one * (cell * 0.94f);
                var wash = washGo.AddComponent<SpriteRenderer>();
                wash.sprite = PrototypeSprites.Square;
                wash.color = PlayableWash;
                wash.sortingOrder = 1;
                _playableWashes[i] = wash;

                i++;
            }
        }
    }

    public void RefreshLockVisuals()
    {
        if (_lockOverlays == null)
        {
            return;
        }

        int i = 0;
        for (int col = 0; col < GridBoard.Width; col++)
        {
            for (int row = 0; row < GridBoard.Height; row++)
            {
                bool locked = !IsBuildable(new GridCoord(col, row));
                if (_lockOverlays[i] != null)
                {
                    _lockOverlays[i].enabled = locked;
                }

                if (_lockHatches[i] != null)
                {
                    _lockHatches[i].enabled = locked;
                }

                if (_playableWashes[i] != null)
                {
                    _playableWashes[i].enabled = !locked;
                }

                i++;
            }
        }
    }
}
