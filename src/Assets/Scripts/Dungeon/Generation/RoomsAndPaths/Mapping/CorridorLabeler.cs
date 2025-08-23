using System.Collections.Generic;
using UnityEngine;
using static CellClassifiers;

public static class CorridorLabeler
{
    private static readonly Vector2Int[] N4 = { new(1, 0), new(-1, 0), new(0, 1), new(0, -1) };

    public sealed class Result
    {
        public int[,] CorridorId;
        public List<List<Vector2Int>> Components; // cells per corridor component
    }

    public static Result Build(IDungeonGridView g, string corridorPrefix)
    {
        var id = new int[g.Width, g.Height];

        for (int x = 0; x < g.Width; x++) 
            for (int y = 0; y < g.Height; y++)
                id[x, y] = -1;

        var comps = new List<List<Vector2Int>>();

        for (int x = 0; x < g.Width; x++)
            for (int y = 0; y < g.Height; y++)
            {
                if (id[x, y] != -1) continue;
                if (!IsCorridor(g.GetKind(x, y), corridorPrefix)) continue;

                int cid = comps.Count;
                var bucket = new List<Vector2Int>();
                var q = new Queue<Vector2Int>();
                q.Enqueue(new Vector2Int(x, y));
                id[x, y] = cid;

                while (q.Count > 0)
                {
                    var c = q.Dequeue();
                    bucket.Add(c);
                    foreach (var d in N4)
                    {
                        int nx = c.x + d.x;
                        int ny = c.y + d.y;

                        if (!g.InBounds(nx, ny)) continue;
                        if (id[nx, ny] != -1) continue;
                        if (!IsCorridor(g.GetKind(nx, ny), corridorPrefix)) continue;

                        id[nx, ny] = cid;
                        q.Enqueue(new Vector2Int(nx, ny));
                    }
                }
                comps.Add(bucket);
            }

        return new Result { CorridorId = id, Components = comps };
    }
}