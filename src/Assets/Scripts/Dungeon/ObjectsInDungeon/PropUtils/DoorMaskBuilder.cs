using System.Collections.Generic;
using UnityEngine;

public static class DoorMaskBuilder
{
    public static HashSet<Vector2Int> BuildDoorNoPropMask(DungeonGrid grid, DungeonMapIndex index, Room room, string corridorPrefix, int doorAisleDepth, int wallDoorClearance, int doorwayChebBuffer, System.Func<string, bool> isRoomFloor)
    {
        var blocked = new HashSet<Vector2Int>();

        var entrances = (index.Rooms.TryGetValue(room.Id, out var ri) && ri?.Entrances != null) ? new List<Vector2Int>(ri.Entrances) : new List<Vector2Int>();

        // Fallback
        if (entrances.Count == 0)
        {
            var b = room.Bounds;

            void TryAdd(int x, int y)
            {
                var c = new Vector2Int(x, y);
                if (!isRoomFloor(grid.Kind[x, y])) return;

                foreach (var d in new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down })
                {
                    var n = c + d;
                    if (!grid.InBounds(n.x, n.y)) continue;

                    var kind = grid.Kind[n.x, n.y] ?? "";
                    if (!string.IsNullOrEmpty(corridorPrefix) && kind.StartsWith(corridorPrefix, System.StringComparison.OrdinalIgnoreCase))
                    {
                        entrances.Add(c);
                        break; 
                    }
                }
            }
            for (int x = b.xMin; x < b.xMax; x++)
            {
                TryAdd(x, b.yMax - 1);
                TryAdd(x, b.yMin); 
            }

            for (int y = b.yMin; y < b.yMax; y++)
            {
                TryAdd(b.xMin, y); 
                TryAdd(b.xMax - 1, y);
            }
        }

        if (entrances.Count == 0) 
            return blocked;

        RectInt bounds = room.Bounds;
        foreach (var e in entrances)
        {
            if (bounds.Contains(e))
                blocked.Add(e);

            // corridor just outside door (for internal aisle direction)
            Vector2Int? corridor = null;
            foreach (var d in new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down })
            {
                var n = e + d;
                if (!grid.InBounds(n.x, n.y)) continue;

                var k = grid.Kind[n.x, n.y] ?? "";
                if (!string.IsNullOrEmpty(corridorPrefix) && k.StartsWith(corridorPrefix, System.StringComparison.OrdinalIgnoreCase))
                {
                    corridor = n;
                    break;
                }
            }

            // keep an aisle inside
            if (corridor.HasValue && doorAisleDepth > 0)
            {
                var insideDir = e - corridor.Value;
                var cur = e;

                for (int i = 0; i < doorAisleDepth; i++)
                {
                    cur += insideDir;

                    if (!bounds.Contains(cur)) break;

                    blocked.Add(cur);
                }
            }

            // wall-parallel clearance along door edge
            if (wallDoorClearance > 0)
            {
                bool horizontalWall = (e.y == bounds.yMax - 1 || e.y == bounds.yMin);

                Vector2Int t1 = horizontalWall ? Vector2Int.left : Vector2Int.up;
                Vector2Int t2 = horizontalWall ? Vector2Int.right : Vector2Int.down;

                for (int s = 1; s <= wallDoorClearance; s++)
                {
                    var p1 = e + t1 * s;
                    if (bounds.Contains(p1))
                        blocked.Add(p1);

                    var p2 = e + t2 * s; 
                    if (bounds.Contains(p2)) 
                        blocked.Add(p2);
                }
            }

            // pad right around the doorway
            for (int dx = -doorwayChebBuffer; dx <= doorwayChebBuffer; dx++)
            {
                for (int dy = -doorwayChebBuffer; dy <= doorwayChebBuffer; dy++)
                {
                    var p = new Vector2Int(e.x + dx, e.y + dy);
                    if (!bounds.Contains(p)) continue;

                    var k = grid.Kind[p.x, p.y] ?? "";
                    if (isRoomFloor(k)) blocked.Add(p);
                }
            }
        }
        return blocked;
    }
}