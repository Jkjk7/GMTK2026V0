using System;
using UnityEngine;

/// <summary>
/// 棋盘格子坐标。(col,row)，约定 (0,0)=左下，col 向右，row 向上。
/// </summary>
[Serializable]
public struct GridCoord : IEquatable<GridCoord>
{
    public int Col;
    public int Row;

    public GridCoord(int col, int row)
    {
        Col = col;
        Row = row;
    }

    public Vector2Int ToVector2Int() => new Vector2Int(Col, Row);

    public bool Equals(GridCoord other) => Col == other.Col && Row == other.Row;

    public override bool Equals(object obj) => obj is GridCoord other && Equals(other);

    public override int GetHashCode() => (Col * 397) ^ Row;

    public override string ToString() => $"({Col},{Row})";

    public static bool operator ==(GridCoord a, GridCoord b) => a.Equals(b);

    public static bool operator !=(GridCoord a, GridCoord b) => !a.Equals(b);
}
