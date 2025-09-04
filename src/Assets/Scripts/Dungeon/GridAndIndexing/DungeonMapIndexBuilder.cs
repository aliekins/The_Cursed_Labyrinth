/// @file DungeonMapIndexBuilder.cs
/// @brief Builds a DungeonMapIndex from the carved grid and room info.
/// @ingroup Grid
using System.Collections.Generic;
using UnityEngine;

/// @class DungeonMapIndexBuilder
/// @brief Static builder that fills indices (rooms, interiors, doors, corridors).
public static class DungeonMapIndexBuilder
{
    public sealed class Options
    {
        public string CorridorPrefix = "floor_corridor";
    }
    public static DungeonMapIndex Build(DungeonGrid grid, List<Room> rooms, Options opt = null)
    {
        opt ??= new Options();
        var idx = new DungeonMapIndex();

        grid.EnsureRoomId();

        // Rooms
        foreach (var r in rooms)
        {
            var ri = idx.GetOrCreateRoomIndex(r);
            // Map interior cells to room id
            foreach (var c in r.Info.Interior)
            {
                idx.SetCellRoom(c, r.Id);
                grid.RoomId[c.x, c.y] = r.Id;
                ri.Interior.Add(c);
            }
            // Entrances
            foreach (var e in r.Info.Entrances)
                idx.AddEntrance(e, r.Id);
        }

        // Corridors (cells whose kind starts with the corridor prefix)
        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                var k = grid.Kind[x, y] ?? string.Empty;
                if (!string.IsNullOrEmpty(opt.CorridorPrefix) &&
                    k.StartsWith(opt.CorridorPrefix, System.StringComparison.OrdinalIgnoreCase))
                {
                    idx.AddCorridor(new Vector2Int(x, y));
                }
            }
        }
        return idx;
    }
}