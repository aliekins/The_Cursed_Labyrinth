/// \file DoorPlacer.cs
/// \brief Computes one door per corridor link between rooms. Doors gate the higher-tier side.
using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct DoorPlan
{
    public readonly Vector2Int pos;
    public readonly int requiredTier;

    public DoorPlan(Vector2Int pos, int requiredTier)
    {
        this.pos = pos;
        this.requiredTier = requiredTier;
    }
}

public static class DoorPlacer
{
    private static readonly Vector2Int[] N4 = new[]
    {
        new Vector2Int( 1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int( 0, 1),
        new Vector2Int( 0,-1),
    };

    /// <summary>
    /// Produces a DoorPlan for each corridor component that borders >=2 rooms.
    /// The door is placed on the side of the higher-tier room, at a corridor "mouth"
    /// (a corridor cell with exactly one corridor neighbour inside its component).
    /// </summary>
    public static List<DoorPlan> PlanDoors(
        DungeonGrid grid,
        List<Room> rooms,
        string corridorKind,
        Func<string, int> resolveTierFromRoomKind)
    {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        if (rooms == null) throw new ArgumentNullException(nameof(rooms));
        if (resolveTierFromRoomKind == null) throw new ArgumentNullException(nameof(resolveTierFromRoomKind));

        int W = grid.Width;
        int H = grid.Height;

        // 1) Corridor mask based on exact kind match (your corridors use a specific kind).
        bool[,] isCorr = new bool[W, H];
        for (int x = 0; x < W; x++)
        {
            for (int y = 0; y < H; y++)
            {
                string k = grid.Kind[x, y];
                isCorr[x, y] = k != null && string.Equals(k, corridorKind, StringComparison.OrdinalIgnoreCase);
            }
        }

        // 2) Label corridor components.
        int[,] comp = new int[W, H];
        for (int x = 0; x < W; x++)
        {
            for (int y = 0; y < H; y++)
            {
                comp[x, y] = -1;
            }
        }

        var plans = new List<DoorPlan>();
        int compId = 0;

        for (int sx = 0; sx < W; sx++)
        {
            for (int sy = 0; sy < H; sy++)
            {
                if (!isCorr[sx, sy] || comp[sx, sy] != -1) continue;

                var q = new Queue<Vector2Int>();
                var cells = new List<Vector2Int>();
                q.Enqueue(new Vector2Int(sx, sy));
                comp[sx, sy] = compId;

                // For this component: map roomId -> corridor boundary cells touching that room.
                var boundaryByRoom = new Dictionary<int, List<Vector2Int>>();

                while (q.Count > 0)
                {
                    var c = q.Dequeue();
                    cells.Add(c);

                    foreach (var d in N4)
                    {
                        int nx = c.x + d.x, ny = c.y + d.y;
                        if (nx < 0 || ny < 0 || nx >= W || ny >= H) continue;

                        if (isCorr[nx, ny])
                        {
                            if (comp[nx, ny] == -1)
                            {
                                comp[nx, ny] = compId;
                                q.Enqueue(new Vector2Int(nx, ny));
                            }
                        }
                        else
                        {
                            int rid = grid.RoomId[nx, ny]; // corridors are -1; rooms >= 0 (after StampRoomIds)
                            if (rid >= 0)
                            {
                                if (!boundaryByRoom.TryGetValue(rid, out var list))
                                {
                                    list = new List<Vector2Int>();
                                    boundaryByRoom[rid] = list;
                                }
                                // c is a corridor cell touching room rid
                                list.Add(c);
                            }
                        }
                    }
                }

                // Need at least two distinct rooms to be a link.
                if (boundaryByRoom.Count < 2)
                {
                    compId++;
                    continue;
                }

                // Pick the two rooms with the largest boundary contact (stable, simple).
                int rA = -1, rB = -1, cA = -1, cB = -1;
                foreach (var kv in boundaryByRoom)
                {
                    int count = kv.Value.Count;
                    if (count > cA)
                    {
                        rB = rA; cB = cA;
                        rA = kv.Key; cA = count;
                    }
                    else if (count > cB)
                    {
                        rB = kv.Key; cB = count;
                    }
                }
                if (rA < 0 || rB < 0)
                {
                    compId++;
                    continue;
                }

                // Determine tiers by the room center's floor kind (matches your banding).
                int tA = ResolveRoomTier(rooms, grid, rA, resolveTierFromRoomKind);
                int tB = ResolveRoomTier(rooms, grid, rB, resolveTierFromRoomKind);
                int requiredTier = Math.Max(tA, tB);

                // Target the higher-tier room; place door at a corridor "mouth" touching it.
                int targetRoom = (tA >= tB) ? rA : rB;
                var candidates = boundaryByRoom[targetRoom];

                Vector2Int pick = candidates[0];
                int bestScore = int.MaxValue;

                foreach (var c in candidates)
                {
                    int score = CorridorMouthScore(comp, c.x, c.y, compId); // 1 neighbour == ideal “mouth”
                    if (score < bestScore)
                    {
                        bestScore = score;
                        pick = c;
                        if (score == 1) break;
                    }
                }

                plans.Add(new DoorPlan(pick, requiredTier));
                compId++;
            }
        }

        return plans;
    }

    private static int ResolveRoomTier(
        List<Room> rooms,
        DungeonGrid grid,
        int roomId,
        Func<string, int> resolveTierFromRoomKind)
    {
        var center = rooms[roomId].Center;
        string kind = grid.Kind[center.x, center.y] ?? string.Empty;
        return resolveTierFromRoomKind(kind);
    }

    private static int CorridorMouthScore(int[,] comp, int x, int y, int id)
    {
        int W = comp.GetLength(0), H = comp.GetLength(1);
        int nCorr = 0;
        foreach (var d in N4)
        {
            int nx = x + d.x, ny = y + d.y;
            if (nx < 0 || ny < 0 || nx >= W || ny >= H) continue;
            if (comp[nx, ny] == id) nCorr++;
        }
        // Lower is better. 1 == perfect mouth; 2+ == deeper in the corridor.
        return nCorr;
    }
}