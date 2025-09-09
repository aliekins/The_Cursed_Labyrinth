//using System.Collections.Generic;
//using UnityEngine;

//public partial class DungeonController
//{
//    #region Generation
//    private void CreateGrid() => grid = new DungeonGrid(width, height);
//    private void ClearVisuals() => tmVisualizer?.Clear();

//    private void GenerateAndCarve()
//    {
//        rooms = GenerateRooms();
//        CarveRooms(rooms);
//        edges = ConnectRooms(rooms);
//        CarveCorridors(rooms, edges);
//    }

//    private List<Room> GenerateRooms()
//    {
//        int innerW = width - 2 * border, innerH = height - 2 * border;
//        var cfg = new BspConfig
//        {
//            MapArea = new RectInt(border, border, innerW, innerH),
//            MinLeafSize = minLeafSize,
//            MinRoomSize = minRoomSize,
//            MaxRoomSize = maxRoomSize
//        };
//        var bsp = BspGenerator.Generate(cfg, new AspectBiasedSplitPolicy(), new PaddedRoomCarver(roomPadding), rng);
//        return bsp.Rooms;
//    }

//    private void CarveRooms(IEnumerable<Room> list)
//    {
//        string fk = biomeSetup ? biomeSetup.floorKind : "floor_entry";
//        foreach (var r in list) grid.CarveRoom(r.Bounds, fk);
//    }

//    private List<(int a, int b)> ConnectRooms(List<Room> list) => GraphUtils.BuildMstByDistance(list);

//    private void CarveCorridors(List<Room> list, List<(int a, int b)> e)
//    {
//        var walkable = new bool[width, height];
//        for (int x = 0; x < width; x++)
//            for (int y = 0; y < height; y++)
//                walkable[x, y] = true;

//        foreach (var (a, b) in e)
//        {
//            var start = list[a].Center;
//            var goal = list[b].Center;
//            var path = AStarPathfinder.FindPath(walkable, start, goal);
//            grid.CarvePath(path, corridorKind, corridorThickness);
//        }
//    }

//    private void BuildRoomInfosAndReserve()
//    {
//        foreach (var r in rooms)
//        {
//            r.Info.BuildFromGrid(
//                r.Id,
//                r.Bounds,
//                grid,
//                edgeBand: 2,
//                corridorKindPrefix: corridorKind
//            );
//            foreach (var e in r.Info.Entrances)
//                r.Info.Occupied.Add(e);
//        }
//    }

//    private void BuildIndices()
//    {
//        mapIndex = DungeonMapIndexBuilder.Build(
//            grid,
//            rooms,
//            new DungeonMapIndexBuilder.Options { CorridorPrefix = corridorKind }
//        );
//    }
//    #endregion

//    #region Rendering
//    private void RenderDungeon() => tmVisualizer.Render(grid);
//    #endregion
//}