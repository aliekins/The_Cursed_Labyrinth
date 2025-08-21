/// \file RoomIndexer.cs
/// \brief Stamps per-cell room indices after generation; corridor stays -1
using System.Collections.Generic;

public static class RoomIndexer
{
    public static void StampRoomIds(DungeonGrid grid, List<Room> rooms)
    {
        grid.EnsureRoomId();
        for (int i = 0; i < rooms.Count; i++)
        {
            var b = rooms[i].Bounds;
            for (int x = b.xMin; x < b.xMax; x++)
                for (int y = b.yMin; y < b.yMax; y++)
                {
                    if (!grid.InBounds(x, y)) continue;
                    var k = grid.Kind[x, y];
                    if (k != null && k.StartsWith("floor", System.StringComparison.OrdinalIgnoreCase))
                        grid.RoomId[x, y] = i;
                }
        }
    }
}