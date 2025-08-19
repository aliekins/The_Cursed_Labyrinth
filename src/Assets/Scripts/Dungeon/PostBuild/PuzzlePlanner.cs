/// \file PuzzlePlanner.cs
/// \brief Produces data-only PuzzlePlan lists per room based on size/biome/rules.
using System.Collections.Generic;
using UnityEngine;

public static class PuzzlePlanner
{
    public static Dictionary<int, List<PuzzlePlan>> Plan(DungeonGrid grid, IReadOnlyList<Room> rooms, PuzzleRules_SO rules, System.Random rng)
    {
        var dict = new Dictionary<int, List<PuzzlePlan>>();

        for (int i = 0; i < rooms.Count; i++)
        {
            var room = rooms[i];
            var b = room.Bounds;
            int area = b.width * b.height;

            if (rules != null && area < rules.minRoomArea) continue;

            // Center anchor
            var c = room.Center;
            string biome = grid.Kind[c.x, c.y] ?? "floor_entry";

            // Pick type by biome weights
            var type = ChooseTypeForBiome(biome, rules, rng);
            var plan = new PuzzlePlan { roomId = i, type = type, cell = c, biomeKind = biome };

            if (!dict.TryGetValue(i, out var list)) 
            { 
                list = new List<PuzzlePlan>(); 
                dict[i] = list; 
            }

            list.Add(plan);
        }

        return dict;
    }

    private static PuzzleType ChooseTypeForBiome(string biome, PuzzleRules_SO rules, System.Random rng)
    {
        if (rules == null) return PuzzleType.LeverDoor; // default

        var profile = rules.GetProfileForBiome(biome);

        if (profile == null || profile.weights == null || profile.weights.Count == 0)
            return PuzzleType.LeverDoor;

        int total = 0; 
        foreach (var w in profile.weights)
        {
            total += Mathf.Max(0, w.weight);
        }

        if (total <= 0) return PuzzleType.LeverDoor;

        int roll = rng.Next(0, total);

        foreach (var w in profile.weights)
        {
            roll -= Mathf.Max(0, w.weight);
            if (roll < 0) 
                return w.type;
        }

        return PuzzleType.LeverDoor;
    }
}