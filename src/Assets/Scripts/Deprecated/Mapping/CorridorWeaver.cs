//using System.Collections.Generic;
//using UnityEngine;

//public static class CorridorWeaver
//{
//    public static void CarveAllMstEdges(DungeonGrid grid, List<Room> rooms, List<(int a, int b)> edges, string corridorKind, int thickness)
//    {
//        var walkable = new bool[grid.Width, grid.Height];
//        for (int x = 0; x < grid.Width; x++)
//            for (int y = 0; y < grid.Height; y++)
//                walkable[x, y] = true;

//        foreach (var (a, b) in edges)
//        {
//            var from = rooms[a].Center;
//            var to = rooms[b].Center;
//            var path = AStarPathfinder.FindPath(walkable, from, to);
//            grid.CarvePath(path, corridorKind, thickness);
//        }
//    }
//}