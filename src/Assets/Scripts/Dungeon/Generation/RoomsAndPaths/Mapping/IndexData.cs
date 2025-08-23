using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class DungeonMapIndex
{
    // Fast cell lookups
    public readonly int[,] RoomId;        // -1 = not in room
    public readonly int[,] CorridorId;    // -1 = not corridor
    public readonly HashSet<Vector2Int> Walls; // non-floor cells 

    // Component lists
    public readonly List<List<Vector2Int>> CorridorComponents;

    // Per-room bundle
    public readonly Dictionary<int, RoomIndex> Rooms;

    public DungeonMapIndex(int[,] roomId, int[,] corridorId, HashSet<Vector2Int> walls, List<List<Vector2Int>> corridorComps, Dictionary<int, RoomIndex> rooms)
    {
        RoomId = roomId;
        CorridorId = corridorId;
        Walls = walls;
        CorridorComponents = corridorComps;
        Rooms = rooms;
    }

    public sealed class RoomIndex
    {
        public int Id;
        public RectInt Bounds;
        public Vector2Int Center;
        public List<Vector2Int> Interior;     
        public List<Vector2Int> EdgeBand;     
        public List<Vector2Int> CornerAnchors;
        public HashSet<Vector2Int> Entrances;  
        public List<Vector2Int> WallRing;      // computed ring just outside bounds
        public string SpecialId;             
    }
}
public static class RoomIdMapBuilder
{
    public static int[,] Build(IDungeonGridView g)
    {
        int[,] map = new int[g.Width, g.Height];
        for (int x = 0; x < g.Width; x++)
            for (int y = 0; y < g.Height; y++)
                map[x, y] = g.GetRoomId(x, y); 
        return map;
    }
}

public static class CellClassifiers
{
    public static bool IsFloor(string kind) =>
        !string.IsNullOrEmpty(kind) && kind.StartsWith("floor", StringComparison.OrdinalIgnoreCase);

    public static bool IsCorridor(string kind, string corridorPrefix) =>
        !string.IsNullOrEmpty(kind) && kind.StartsWith(corridorPrefix, StringComparison.OrdinalIgnoreCase);
}