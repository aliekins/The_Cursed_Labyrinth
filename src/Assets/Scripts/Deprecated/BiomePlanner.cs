//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.SceneManagement;

//public class BiomePlanner : MonoBehaviour
//{
//    [SerializeField] private BiomeSetup_SO profile; 
//    [SerializeField] private bool useSeededSpecial = true;

//    public BiomeSetup_SO Profile => profile;

//    // Called by DungeonController after rooms, MST, and indices exist
//    public void Build(DungeonGrid grid, List<Room> rooms, List<(int a, int b)> mstEdges,
//                      TilemapVisualizer viz, DungeonMapIndex mapIndex,
//                      string corridorKind, int corridorThickness, System.Random rng)
//    {
//        if (!profile) { Debug.LogWarning("BiomePlanner has no profile assigned."); return; }

//        //  Repaint all rooms to the biome's floor kind (simple single-biome scenes)
//        PaintAllRoomsAsKind(grid, rooms, profile.floorKind);

//        // Pick Start and Special (farthest along MST)
//        var (start, special) = PickFarApart(rooms, mstEdges, Mathf.Max(0, profile.minPathCells), rng);
//        if (start == null || special == null) return;

//        // Carve ALL MST edges wall-to-wall (ensures connectivity)
//        CorridorWeaver.CarveAllMstEdges(grid, rooms, mstEdges, profile.corridorKind, corridorThickness);

//        // Spawn special prefab in its host room & punch doorway
//        if (!useSeededSpecial)
//        {       
//            var go = SpawnSpecialInHost(profile.specialRoomPrefab, special, viz, grid);
//            PunchDoorwayAtPort(grid, special, profile.specialEntrance, profile.corridorKind);
//        }

//        // Place player in start room
//        var dc = FindAnyObjectByType<DungeonController>();
//        if (dc) dc.PlacePlayer(start.Center);

//    }

//    #region helpers
//    static void PaintAllRoomsAsKind(DungeonGrid grid, List<Room> rooms, string kind)
//    {
//        foreach (var r in rooms)
//        {
//            var b = r.Bounds;
//            for (int x = b.xMin; x < b.xMax; x++)
//                for (int y = b.yMin; y < b.yMax; y++)
//                    if (grid.InBounds(x, y)) grid.Kind[x, y] = kind;
//        }
//        // keep RoomId as is; your visualizer expects it to be stamped already
//    }

//    static (Room start, Room special) PickFarApart(List<Room> rooms, List<(int a, int b)> edges, int minPathCells, System.Random rng)
//    {
//        if (rooms == null || rooms.Count < 2) return (null, null);
//        int sIdx = rng.Next(rooms.Count);
//        float[] dist = GraphUtils.ComputeRoomDistances(rooms, edges, sIdx);
//        int far = sIdx; float best = -1f;
//        for (int i = 0; i < dist.Length; i++)
//        {
//            if (float.IsInfinity(dist[i])) continue;
//            if (dist[i] > best) { best = dist[i]; far = i; }
//        }
//        // If too close, you can re-pick or just accept
//        return (rooms[sIdx], rooms[far]);
//    }

//    static GameObject SpawnSpecialInHost(GameObject prefab, Room host, TilemapVisualizer viz, DungeonGrid grid)
//    {
//        if (!prefab || host == null) return null;
//        // Mute host interior so prefab art shows
//        var b = host.Bounds;
//        for (int x = b.xMin; x < b.xMax; x++)
//            for (int y = b.yMin; y < b.yMax; y++)
//                if (grid.InBounds(x, y)) grid.Kind[x, y] = "floor_prefab";

//        var go = Object.Instantiate(prefab);
//        if (viz && viz.GridTransform) go.transform.SetParent(viz.GridTransform, false);
//        go.transform.localPosition = viz.CellCenterLocal(host.Center.x, host.Center.y);

//        // reserve so props/traps skip
//        host.Info.Occupied ??= new HashSet<Vector2Int>();
//        for (int x = b.xMin; x < b.xMax; x++)
//            for (int y = b.yMin; y < b.yMax; y++)
//                host.Info.Occupied.Add(new Vector2Int(x, y));
//        return go;
//    }

//    static void PunchDoorwayAtPort(DungeonGrid grid, Room r, SpecialPort p, string corridorKind)
//    {
//        var b = r.Bounds;
//        Vector2Int c = p switch
//        {
//            SpecialPort.Bottom => new((b.xMin + b.xMax - 1) / 2, b.yMin),
//            SpecialPort.Top => new((b.xMin + b.xMax - 1) / 2, b.yMax - 1),
//            SpecialPort.Left => new(b.xMin, (b.yMin + b.yMax - 1) / 2),
//            _ => new(b.xMax - 1, (b.yMin + b.yMax - 1) / 2),
//        };
//        if (grid.InBounds(c.x, c.y)) grid.Kind[c.x, c.y] = corridorKind;
//    }
//    #endregion
//}