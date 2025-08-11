/// \file GridPos.cs
/// \brief Simple integer grid coordinate helper.
using UnityEngine;

public readonly struct GridPos
{
    public readonly int x, y;
    public GridPos(int x, int y) { this.x = x; this.y = y; }
    public Vector3 ToWorld(float cellSize = 1f) => new Vector3(x * cellSize, y * cellSize, 0f);
    public static implicit operator Vector2Int(GridPos p) => new Vector2Int(p.x, p.y);
    public static implicit operator GridPos(Vector2Int v) => new GridPos(v.x, v.y);
}