/// \file DungeonGrid.cs
/// \brief Grid model that stores a tile kind string per cell.
using UnityEngine;

public sealed class DungeonGrid
{
    public readonly int Width;
    public readonly int Height;
    public readonly string[,] Kind;

    /// \brief Create a grid initialized to wall
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

    /// \brief Carve a rectangular area as a spec floor kind
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

    /// \brief Carve a path of cells as a spec floor kind (optional thickness)
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

    /// \brief Check bounds
    public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

    /// \brief Integer room index per cell. -1 = corridor/none. Sized [Width,Height]
    public int[,] RoomId { get; private set; }

    /// \brief Allocate/clear RoomId map
    public void EnsureRoomId()
    {
        RoomId ??= new int[Width, Height];
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                RoomId[x, y] = -1;
            }
        }
    }

}