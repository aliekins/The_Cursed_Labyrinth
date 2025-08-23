using System;
using System.Collections.Generic;
using UnityEngine;
using static DungeonController;

public static class BiomePainter
{
    public static bool[,] BuildEntryBufferMask(DungeonGrid grid, IReadOnlyList<Room> rooms, string corridorKind, int bufferDepth)
    {
        int W = grid.Width;
        int H = grid.Height;
        var protect = new bool[W, H];

        if (bufferDepth <= 0) return protect;

        ReadOnlySpan<Vector2Int> N4 = stackalloc Vector2Int[] { new(1, 0), new(-1, 0), new(0, 1), new(0, -1) };

        foreach (var room in rooms)
        {
            var bounds = room.Bounds;
            var seen = new bool[W, H];
            var q = new Queue<(int x, int y, int d)>();

            // enqueue door tiles (room cells touching a corridor)
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    if (!grid.InBounds(x, y)) continue;

                    var k = grid.Kind[x, y];

                    if (k == null || !k.StartsWith("floor", StringComparison.OrdinalIgnoreCase)) continue;

                    bool touchesCorr = false;

                    foreach (var d in N4)
                    {
                        int nx = x + d.x, ny = y + d.y;
                        if (!grid.InBounds(nx, ny)) continue;

                        if (string.Equals(grid.Kind[nx, ny], corridorKind, StringComparison.OrdinalIgnoreCase))
                        { 
                            touchesCorr = true; 
                            break; 
                        }
                    }

                    if (touchesCorr) { 
                        q.Enqueue((x, y, 0));
                        seen[x, y] = true; 
                    }
                }
            }

            // flood inside the room up to bufferDepth
            while (q.Count > 0)
            {
                var (cx, cy, cd) = q.Dequeue();
                protect[cx, cy] = true;

                if (cd >= bufferDepth - 1) continue;

                foreach (var d in N4)
                {
                    int nx = cx + d.x;
                    int ny = cy + d.y;

                    if (!grid.InBounds(nx, ny)) continue;
                    if (seen[nx, ny]) continue;
                    if (nx < bounds.xMin || nx >= bounds.xMax || ny < bounds.yMin || ny >= bounds.yMax) continue;

                    var k = grid.Kind[nx, ny];
                    if (k == null || !k.StartsWith("floor", StringComparison.OrdinalIgnoreCase)) continue;

                    seen[nx, ny] = true;
                    q.Enqueue((nx, ny, cd + 1));
                }
            }
        }
        return protect;
    }
    public static void PaintRoomsByBands(DungeonGrid grid, List<Room> rooms, float[] roomDist, IReadOnlyList<BiomeBandDef> bands, bool[,] protectMask, string corridorKind)
    {
        var sorted = new List<BiomeBandDef>(bands);

        sorted.Sort((a, b) => a.maxDistance.CompareTo(b.maxDistance));

        string PickKind(float d)
        {
            foreach (var b in sorted)
            {
                if (d <= b.maxDistance)
                {
                    return b.kind;
                }
            }
            return sorted[^1].kind;
        }

        for (int i = 0; i < rooms.Count; i++)
        {
            string roomKind = PickKind(roomDist[i]);
            var b = rooms[i].Bounds;

            for (int x = b.xMin; x < b.xMax; x++){
                for (int y = b.yMin; y < b.yMax; y++)
                {
                    if (!grid.InBounds(x, y)) continue;

                    var k = grid.Kind[x, y];

                    if (k == null || !k.StartsWith("floor", StringComparison.OrdinalIgnoreCase)) continue;

                    grid.Kind[x, y] = protectMask[x, y] ? corridorKind : roomKind;
                }
            }
        }
    }
}
