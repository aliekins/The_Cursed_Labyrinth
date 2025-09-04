/// @file Room.cs
/// @brief Carved room representation with bounds, center and derived info.
/// @ingroup Grid
using System.Collections.Generic;
using UnityEngine;

#region Room
/// @class Room
/// @brief Immutable room bounds/center plus a mutable RoomInfo payload.
public sealed class Room
{
    public int Id { get; }
    public RectInt Bounds { get; }
    public Vector2Int Center { get; }
    public RoomInfo Info { get; } = new RoomInfo();
    public Room(int id, RectInt bounds)
    {
        Id = id;
        Bounds = bounds;
        Center = new Vector2Int(bounds.x + bounds.width / 2, bounds.y + bounds.height / 2);
    }
}
#endregion

#region RoomInfo
/// @class RoomInfo
/// @brief Derived room features (interior, edges, corner anchors, entrances, reservations).
public sealed class RoomInfo
{
    public int Id { get; private set; }
    public RectInt Bounds { get; private set; }
    public Vector2Int Center { get; private set; }

    public List<Vector2Int> Interior = new();        ///< (excludes boundary ring)
    public List<Vector2Int> EdgeBand = new();        ///< Cells within EdgeBandDistance of any wall
    public List<Vector2Int> CornerAnchors = new();   ///< Cells adjacent to two perpendicular walls
    public HashSet<Vector2Int> Entrances = new();    ///< Interior cells adjacent to corridor tiles

    public HashSet<Vector2Int> Occupied = new();

    public int EdgeBandDistance { get; private set; } = 2;
    public string CorridorKindPrefix { get; private set; } = "floor_corridor";

    public int Width => Bounds.width;
    public int Height => Bounds.height;
    public int Area => Bounds.width * Bounds.height;

    public void BuildFromGrid(int id, RectInt bounds, DungeonGrid grid, int edgeBand = 2, string corridorKindPrefix = "floor_corridor")
    {
        Id = id; Bounds = bounds;
        Center = new Vector2Int(bounds.x + bounds.width / 2, bounds.y + bounds.height / 2);
        EdgeBandDistance = Mathf.Max(1, edgeBand);
        CorridorKindPrefix = corridorKindPrefix ?? "floor_corridor";

        Interior.Clear();
        EdgeBand.Clear();
        CornerAnchors.Clear();
        Entrances.Clear();
        Occupied.Clear();

        int xMin = Bounds.xMin + 1, xMax = Bounds.xMax - 2;
        int yMin = Bounds.yMin + 1, yMax = Bounds.yMax - 2;
        for (int x = xMin; x <= xMax; x++)
            for (int y = yMin; y <= yMax; y++)
            {
                if (!grid.InBounds(x, y)) continue;
                var k = grid.Kind[x, y];
                if (string.IsNullOrEmpty(k) || !k.StartsWith("floor", System.StringComparison.OrdinalIgnoreCase)) continue;

                var c = new Vector2Int(x, y);
                Interior.Add(c);

                // Edge band
                int dl = x - Bounds.xMin;
                int dr = (Bounds.xMax - 1) - x;
                int db = y - Bounds.yMin;
                int dt = (Bounds.yMax - 1) - y;
                int minD = Mathf.Min(Mathf.Min(dl, dr), Mathf.Min(db, dt));
                if (minD <= EdgeBandDistance) EdgeBand.Add(c);
            }

        // Corner anchors: cells that see two perpendicular "wall" neighbors
        foreach (var c in EdgeBand)
        {
            bool leftWall = IsWall(grid, c.x - 1, c.y);
            bool rightWall = IsWall(grid, c.x + 1, c.y);
            bool bottomWall = IsWall(grid, c.x, c.y - 1);
            bool topWall = IsWall(grid, c.x, c.y + 1);
            int touches = (leftWall ? 1 : 0) + (rightWall ? 1 : 0) + (bottomWall ? 1 : 0) + (topWall ? 1 : 0);
            if (touches >= 2) CornerAnchors.Add(c);
        }

        // Entrances: interior cells adjacent to corridor tiles
        foreach (var c in Interior)
        {
            if (AdjacentToKind(grid, c, CorridorKindPrefix))
                Entrances.Add(c);
        }
    }
    #region helpers
    private static bool IsWall(DungeonGrid grid, int x, int y)
    {
        if (!grid.InBounds(x, y)) return true;
        var k = grid.Kind[x, y];
        return string.IsNullOrEmpty(k) || !k.StartsWith("floor", System.StringComparison.OrdinalIgnoreCase);
    }

    private static bool AdjacentToKind(DungeonGrid grid, Vector2Int p, string kindPrefix)
    {
        var n4 = new[] { new Vector2Int(1, 0), new(-1, 0), new(0, 1), new(0, -1) };
        foreach (var d in n4)
        {
            int nx = p.x + d.x, ny = p.y + d.y;
            if (!grid.InBounds(nx, ny)) continue;
            var kk = grid.Kind[nx, ny] ?? string.Empty;
            if (!string.IsNullOrEmpty(kindPrefix) &&
                kk.StartsWith(kindPrefix, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
    #endregion
}
#endregion