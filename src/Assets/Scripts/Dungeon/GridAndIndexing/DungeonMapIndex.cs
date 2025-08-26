using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lookup/index data for a built dungeon.
/// Acts as a shared container produced by DungeonMapIndexerBuilder (or equivalent).
/// </summary>
public class DungeonMapIndex
{
    /// <summary>
    /// Per-room derived info used by adapters/puzzles.
    /// </summary>
    public class RoomIndex
    {
        public int Id;
        public Room Room;                         // reference to the Room model
        public RectInt Bounds;                    // convenience mirror
        public Vector2Int Center;                 // convenience mirror
        public List<Vector2Int> Entrances = new List<Vector2Int>(); // door cells
        public HashSet<Vector2Int> Interior = new HashSet<Vector2Int>(); // all interior floor cells (optional)

        public RoomIndex(int id, Room room)
        {
            Id = id;
            Room = room;
            Bounds = room.Bounds;
            Center = room.Center;
        }
    }

    /// Map from room id to index info
    public Dictionary<int, RoomIndex> Rooms { get; private set; } = new Dictionary<int, RoomIndex>();

    /// Quick lookup: which room id (or -1) a given cell belongs to
    public Dictionary<Vector2Int, int> CellToRoom { get; private set; } = new Dictionary<Vector2Int, int>();

    /// Corridor cells across the map (optional but useful for placement)
    public HashSet<Vector2Int> CorridorCells { get; private set; } = new HashSet<Vector2Int>();

    /// Entrance cells (doors) across the map
    public HashSet<Vector2Int> EntranceCells { get; private set; } = new HashSet<Vector2Int>();

    public bool TryGetRoomAt(Vector2Int cell, out Room room)
    {
        room = null;
        if (CellToRoom.TryGetValue(cell, out var id))
        {
            if (Rooms.TryGetValue(id, out var ri) && ri != null)
            {
                room = ri.Room;
                return true;
            }
        }
        return false;
    }

    #region Builder helpers (optional convenience for your DungeonMapIndexerBuilder)
    public RoomIndex GetOrCreateRoomIndex(Room r)
    {
        if (r == null) return null;
        if (!Rooms.TryGetValue(r.Id, out var ri) || ri == null)
        {
            ri = new RoomIndex(r.Id, r);
            Rooms[r.Id] = ri;
        }
        return ri;
    }

    public void SetCellRoom(Vector2Int cell, int roomId)
    {
        CellToRoom[cell] = roomId;
    }

    public void AddEntrance(Vector2Int cell, int roomId)
    {
        EntranceCells.Add(cell);
        if (Rooms.TryGetValue(roomId, out var ri) && ri != null)
            ri.Entrances.Add(cell);
    }

    public void AddInterior(Vector2Int cell, int roomId)
    {
        if (Rooms.TryGetValue(roomId, out var ri) && ri != null)
            ri.Interior.Add(cell);
    }

    public void AddCorridor(Vector2Int cell)
    {
        CorridorCells.Add(cell);
    }
    #endregion
}