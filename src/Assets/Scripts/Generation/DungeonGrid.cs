/// \file DungeonGrid.cs
/// \brief Grid model that stores a tile kind string per cell.
using UnityEngine;

/// <summary>default = wall</summary>
public sealed class DungeonGrid
{
    public readonly int Width;
    public readonly int Height;
    public readonly string[,] Kind;

    /// <summary>Create a grid initialized to wall</summary>
    public DungeonGrid(int w, int h)
    {
        Width = w; 
        Height = h;
        Kind = new string[w, h];

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            { 
                Kind[x, y] = "wall";
            }
        }
    }

    /// <summary>Carve a rectangular area as a spec floor kind</summary>
    public void CarveRoom(RectInt r, string floorKind)
    {
        for (int x = r.xMin; x < r.xMax; x++)
        {
            for (int y = r.yMin; y < r.yMax; y++)
            {
                if (InBounds(x, y)) { Kind[x, y] = floorKind; }
            }
        }
    }

    /// <summary>Carve a path of cells as a spec floor kind (optional thickness)</summary>
    public void CarvePath(System.Collections.Generic.IEnumerable<Vector2Int> cells, string floorKind, int thickness = 1)
    {
        foreach (var c in cells)
        {
            for (int dx = -thickness + 1; dx <= thickness - 1; dx++)
            {
                for (int dy = -thickness + 1; dy <= thickness - 1; dy++)
                {
                    int x = c.x + dx, y = c.y + dy;
                    if (InBounds(x, y)){ Kind[x, y] = floorKind; }
                }
            }
        }
    }

    /// <summary>Check bounds</summary>
    public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
}