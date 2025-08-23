using System.Collections.Generic;
using UnityEngine;
using static CellClassifiers;

public static class WallScanner
{
    public static HashSet<Vector2Int> Build(IDungeonGridView g)
    {
        var walls = new HashSet<Vector2Int>();
        for (int x = 0; x < g.Width; x++)
            for (int y = 0; y < g.Height; y++)
                if (!IsFloor(g.GetKind(x, y)))
                    walls.Add(new Vector2Int(x, y));
        return walls;
    }
}