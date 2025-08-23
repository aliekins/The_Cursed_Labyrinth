/// \file BiomeAssigner.cs
/// \brief Compute BFS distance over the dungeon and assign floor kinds by distance from start.
using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct BiomeBand
{
    public readonly int MaxDistance; // inclusive
    public readonly string Kind;

    public BiomeBand(int maxDistance, string kind)
    {
        MaxDistance = maxDistance;
        Kind = kind ?? throw new ArgumentNullException(nameof(kind));
    }
}

public static class BiomeAssigner
{
    /// <summary>Four-neighbour offsets: right, left, up, down</summary>
    private static readonly Vector2Int[] NeighbourOffsets =
    {
        new(1, 0), new(-1, 0), new(0, 1), new(0, -1)
    };

    /// <summary>
    /// Any cell whose kind starts with "floor"
    /// </summary>
    public static bool DefaultIsFloor(DungeonGrid grid, int x, int y) =>
        grid.Kind[x, y].StartsWith("floor", StringComparison.Ordinal);

    /// <summary>
    /// Compute BFS distances from <paramref name="start"/> across cells that satisfy <paramref name="isPassable"/>.
    /// Returns an <c>int[width,height]</c> map - non-reachable cells are <c>int.MaxValue</c>.
    /// </summary>
    public static int[,] ComputeDistanceMap(DungeonGrid grid, Vector2Int start, Func<DungeonGrid, int, int, bool> isPassable = null)
    {
        // if null assign a default passability predicate
        isPassable ??= DefaultIsFloor;

        int[,] distanceFromStart = new int[grid.Width, grid.Height];

        // init 
        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                distanceFromStart[x, y] = int.MaxValue;
            }
        }

        // if start is not passable - return 
        if (start.x < 0 || start.y < 0 || start.x >= grid.Width || start.y >= grid.Height || !isPassable(grid, start.x, start.y))  return distanceFromStart;

        var queue = new Queue<Vector2Int>();
        distanceFromStart[start.x, start.y] = 0;
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            int curDist = distanceFromStart[current.x, current.y];

            foreach (var offset in NeighbourOffsets)
            {
                var n = current + offset;

                // bounds check
                if (n.x < 0 || n.y < 0 || n.x >= grid.Width || n.y >= grid.Height) continue;

                // only traverse passable cells
                if (!isPassable(grid, n.x, n.y)) continue;

                int proposedDistance = curDist + 1;
                if (proposedDistance < distanceFromStart[n.x, n.y])
                {
                    distanceFromStart[n.x, n.y] = proposedDistance;
                    queue.Enqueue(n);
                }
            }
        }

        return distanceFromStart;
    }

    /// <summary>
    /// Resolve a floor kind string for a given distance using the first match
    /// </summary>
    /// <param name="bands">Bands ordered from nearest to farthest</param>
    /// <param name="distance"><c>int.MaxValue</c> for unreachable</param>
    /// <param name="kind"></param>
    /// <returns><c>true</c> if a band matched; otherwise <c>false</c></returns>
    public static bool TryGetKindForDistance(IReadOnlyList<BiomeBand> bands, int distance, out string kind)
    {
        for (int i = 0; i < bands.Count; i++)
        {
            if (distance <= bands[i].MaxDistance)
            {
                kind = bands[i].Kind;
                return true;
            }
        }
        kind = null;
        return false;
    }

    /// <summary>
    /// Apply kinds to floor cells in <paramref name="grid"/> using BFS distance bands from <paramref name="start"/>.
    /// </summary>
    /// <param name="grid"></param>
    /// <param name="start"></param>
    /// <param name="bands"></param>
    /// <param name="isFloor"></param>
    public static void ApplyBands(DungeonGrid grid, Vector2Int start, IReadOnlyList<BiomeBand> bands, Func<DungeonGrid, int, int, bool> isFloor = null)
    {
        isFloor ??= DefaultIsFloor;

        var distanceMap = ComputeDistanceMap(grid, start, isFloor);

        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                if (!isFloor(grid, x, y)) continue;

                int d = distanceMap[x, y];
                if (TryGetKindForDistance(bands, d, out string kind))
                {
                    grid.Kind[x, y] = kind;
                }
                // else: no band matched - keep existing kind
            }
        }
    }

    /// \brief Compute per-room levels by BFS over the room graph (edges)
    public static int[] ComputeRoomLevels(IReadOnlyList<Room> rooms, IReadOnlyList<(int a, int b)> edges, int startRoom)
    {
        int n = rooms.Count;
        var adj = new List<int>[n];
        for (int i = 0; i < n; i++) adj[i] = new List<int>();
        foreach (var (a, b) in edges) { adj[a].Add(b); adj[b].Add(a); }

        var level = new int[n];
        for (int i = 0; i < n; i++) level[i] = int.MaxValue;

        var q = new Queue<int>();
        level[startRoom] = 0;
        q.Enqueue(startRoom);

        while (q.Count > 0)
        {
            int u = q.Dequeue();
            foreach (int v in adj[u])
            {
                if (level[v] != int.MaxValue) continue;
                level[v] = level[u] + 1;  // next “ring” after a corridor
                q.Enqueue(v);
            }
        }
        return level;
    }

    /// \brief Paint whole rooms with biome kinds based on precomputed levels
    public static void PaintRoomsByLevel(DungeonGrid grid, IReadOnlyList<Room> rooms, IReadOnlyList<int> levels, Func<int, string> kindForLevel, int roundCornerRadius = 0)
    {
        for (int i = 0; i < rooms.Count; i++)
        {
            int lvl = levels[i];
            if (lvl == int.MaxValue) continue; // unreachable room
            string kind = kindForLevel(lvl);
            grid.CarveRoom(rooms[i].Bounds, kind);
        }
    }

}