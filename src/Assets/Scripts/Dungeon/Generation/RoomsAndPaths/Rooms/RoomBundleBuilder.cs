using System.Collections.Generic;
using UnityEngine;

public static class RoomBundleBuilder
{
    public static Dictionary<int, DungeonMapIndex.RoomIndex> Build(IDungeonGridView g, IReadOnlyList<Room> rooms, System.Func<int, string> specialOfOrNull)
    {
        var dict = new Dictionary<int, DungeonMapIndex.RoomIndex>(rooms.Count);

        foreach (var r in rooms)
        {
            var entry = new DungeonMapIndex.RoomIndex
            {
                Id = r.Id,
                Bounds = r.Bounds,
                Center = r.Center,
                Interior = r.Info?.Interior ?? new List<Vector2Int>(),
                EdgeBand = r.Info?.EdgeBand ?? new List<Vector2Int>(),
                CornerAnchors = r.Info?.CornerAnchors ?? new List<Vector2Int>(),
                Entrances = r.Info?.Entrances ?? new HashSet<Vector2Int>(),
                WallRing = ComputeWallRing(g, r.Bounds),
                SpecialId = specialOfOrNull?.Invoke(r.Id)
            };
            dict[r.Id] = entry;
        }
        return dict;
    }

    static List<Vector2Int> ComputeWallRing(IDungeonGridView g, RectInt b)
    {
        var list = new List<Vector2Int>();
        void TryAdd(int x, int y)
        {
            if (!g.InBounds(x, y)) return;

            var k = g.GetKind(x, y);

            if (string.IsNullOrEmpty(k) || !CellClassifiers.IsFloor(k))
                list.Add(new Vector2Int(x, y));
        }

        for (int x = b.xMin - 1; x <= b.xMax; x++) 
        { 
            TryAdd(x, b.yMin - 1); 
            TryAdd(x, b.yMax); 
        }

        for (int y = b.yMin - 1; y <= b.yMax; y++) 
        { 
            TryAdd(b.xMin - 1, y); 
            TryAdd(b.xMax, y); 
        }
        return list;
    }
}