/// @file DungeonGrid.cs
/// @brief Grid model that stores a tile kind per cell and optional room ids.
/// @ingroup Grid
using UnityEngine;

/// @class DungeonGrid
/// @brief Backing 2D grid for dungeon generation and queries.
/// @brief Backing 2D grid for dungeon generation and queries.
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

    /// \brief Is every cell in rect still solid "wall"? 
    public bool IsRectClearWithMargin(RectInt r, int margin)
    {
        var ex = new RectInt(r.xMin - margin, r.yMin - margin, r.width + 2 * margin, r.height + 2 * margin);
        for (int x = ex.xMin; x < ex.xMax; x++)
            for (int y = ex.yMin; y < ex.yMax; y++)
            {
                if (!InBounds(x, y)) return false;
                if (!string.Equals(Kind[x, y], "wall", System.StringComparison.OrdinalIgnoreCase))
                    return false;
            }
        return true;
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