using UnityEngine;

/// <summary>
/// 棋盘可建造窗口：逻辑仍 7×7，外围锁定；3→5→7 付费解锁。
/// </summary>
public class BoardExpandService : MonoBehaviour
{
    public const int Size3 = 3;
    public const int Size5 = 5;
    public const int Size7 = 7;

    GridBoard _board;
    int _unlockedSize = Size3;
    SpriteRenderer[] _lockOverlays;

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
        _lockOverlays = new SpriteRenderer[GridBoard.Width * GridBoard.Height];
        int i = 0;
        for (int col = 0; col < GridBoard.Width; col++)
        {
            for (int row = 0; row < GridBoard.Height; row++)
            {
                var cell = new GameObject($"Lock_{col}_{row}");
                cell.transform.SetParent(go.transform, false);
                cell.transform.position = _board.CellToWorld(new GridCoord(col, row));
                cell.transform.localScale = Vector3.one * (_board.CellSize * 0.92f);
                var sr = cell.AddComponent<SpriteRenderer>();
                sr.sprite = PrototypeSprites.Square;
                sr.color = new Color(0.12f, 0.12f, 0.14f, 0.55f);
                sr.sortingOrder = 2;
                _lockOverlays[i++] = sr;
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

                i++;
            }
        }
    }
}
