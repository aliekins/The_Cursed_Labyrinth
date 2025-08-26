using System;
using System.Collections.Generic;
using UnityEngine;

public class SpecialRoomPlanner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject part1_SwordRoomPrefab;   // Biome 1
    public GameObject part2_LeverRoomPrefab;   // Biome 2
    public GameObject part3_FourwayHubPrefab;  // Biome 3 hub

    [Tooltip("Hub neighbors (Biome 3): right=Green, top=Red, left=Purple/Metal")]
    public GameObject hubRight_GreenPrefab;
    public GameObject hubTop_RedPrefab;
    public GameObject hubLeft_PurplePrefab;

    [Header("Selection (percentiles of distance from Room 0)")]
    [Range(0, 1)] public float part1Pick = 0.60f;
    [Range(0, 1)] public float part2Pick = 0.50f;
    [Range(0, 1)] public float hubPick = 0.70f;   

    [Header("Room constraints")]
    public Vector2Int minSize = new(8, 8);
    public int maxEntrances = 3;

    private Room injectedGate1;
    private Room injectedGate2;
    private Room injectedHub;
    private Room injectedRight;  // green
    private Room injectedTop;    // red
    private Room injectedLeft;   // purple
    public enum Side { Left, Right, Top, Bottom }

    public sealed class ForcedLink
    {
        public Room A;
        public Side APort; 
        public Room B;
        public Side BPort;
        public ForcedLink(Room a, Side ap, Room b, Side bp) 
        {
            A = a;
            APort = ap;
            B = b;
            BPort = bp;
        }
    }

    public sealed class Plan
    {
        public Room gate1, gate2, hub;
        public List<(Room room, GameObject prefab)> hubNeighbors = new();
        public List<ForcedLink> links = new();
    }
    #region publicPoints
    public List<Room> InjectSpecialRooms(DungeonGrid grid, List<Room> bspRooms)
    {
        var specials = new List<Room>();

        var sizeGate = new Vector2Int(12, 10);
        var sizeLever = new Vector2Int(12, 10);
        var sizeHub = new Vector2Int(14, 14);
        var sizeSide = new Vector2Int(10, 10);

        RectInt R(int x, int y, Vector2Int sz) => new RectInt(x, y, sz.x, sz.y);

        injectedGate1 = new Room(bspRooms.Count + specials.Count, R(8, 8, sizeGate)); specials.Add(injectedGate1);
        grid.CarveRoom(injectedGate1.Bounds, "floor_prefab");

        injectedGate2 = new Room(bspRooms.Count + specials.Count, R(40, 20, sizeLever)); specials.Add(injectedGate2);
        grid.CarveRoom(injectedGate2.Bounds, "floor_prefab");

        injectedHub = new Room(bspRooms.Count + specials.Count, R(22, 26, sizeHub)); specials.Add(injectedHub);
        grid.CarveRoom(injectedHub.Bounds, "floor_prefab");

        injectedRight = new Room(bspRooms.Count + specials.Count, R(injectedHub.Bounds.xMax + 4, injectedHub.Bounds.yMin, sizeSide)); specials.Add(injectedRight);
        grid.CarveRoom(injectedRight.Bounds, "floor_prefab");

        injectedTop = new Room(bspRooms.Count + specials.Count, R(injectedHub.Bounds.xMin, injectedHub.Bounds.yMax + 4, sizeSide)); specials.Add(injectedTop);
        grid.CarveRoom(injectedTop.Bounds, "floor_prefab");

        injectedLeft = new Room(bspRooms.Count + specials.Count, R(injectedHub.Bounds.xMin - sizeSide.x - 4, injectedHub.Bounds.yMin, sizeSide)); specials.Add(injectedLeft);
        grid.CarveRoom(injectedLeft.Bounds, "floor_prefab");

        bspRooms.AddRange(specials);
        return specials;
    }

    public Plan BuildPlan(DungeonGrid grid, List<Room> rooms, List<(int a, int b)> mstEdges)
    {
        var plan = new Plan();

        // gates/hub: prefer injected rooms; do NOT pick random BSP rooms here
        plan.gate1 = injectedGate1 ?? plan.gate1;
        plan.gate2 = injectedGate2 ?? plan.gate2;
        plan.hub = injectedHub ?? plan.hub;

        if (plan.gate1 == null || plan.gate2 == null || plan.hub == null)
        {
            if (rooms == null || rooms.Count == 0) return null;

            float[] rd = GraphUtils.ComputeRoomDistances(rooms, mstEdges, 0);
            var (b1, b2, b3) = SplitRoomsByBiome(grid, rooms);

            plan.gate1 ??= PickByPercentile(rd, b1, part1Pick);
            plan.gate2 ??= PickByPercentile(rd, b2, part2Pick);
            plan.hub ??= PickByPercentile(rd, b3, hubPick);
        }

        // forced links for biomes
        if (plan.gate1 != null && plan.gate2 != null)
            plan.links.Add(new ForcedLink(plan.gate1, Side.Top, plan.gate2, Side.Bottom));

        // hub neighbors: prefer injected side rooms; else fallback to closest-by-direction
        Room right = injectedRight, top = injectedTop, left = injectedLeft;

        if (plan.hub != null)
        {
            if (right == null) 
                right = ClosestInDirection(plan.hub, rooms, Side.Right);

            if (top == null)
                top = ClosestInDirection(plan.hub, rooms, Side.Top);

            if (left == null)
                left = ClosestInDirection(plan.hub, rooms, Side.Left);

            if (right != null && hubRight_GreenPrefab) 
            {
                plan.links.Add(new ForcedLink(plan.hub, Side.Right, right, Side.Left));
                plan.hubNeighbors.Add((right, hubRight_GreenPrefab));
            }
            if (top != null && hubTop_RedPrefab)
            {
                plan.links.Add(new ForcedLink(plan.hub, Side.Top, top, Side.Bottom));
                plan.hubNeighbors.Add((top, hubTop_RedPrefab));
            }
            if (left != null && hubLeft_PurplePrefab)
            {
                plan.links.Add(new ForcedLink(plan.hub, Side.Left, left, Side.Right));
                plan.hubNeighbors.Add((left, hubLeft_PurplePrefab));
            }
        }

        return plan;
    }

    public void SpawnPrefabs(Plan plan, TilemapVisualizer viz, DungeonGrid grid, DungeonMapIndex mapIndex)
    {
        if (plan == null || viz == null) return;

        void Reserve(Room r)
        {
            var b = r.Bounds;
            for (int x = b.xMin; x < b.xMax; x++)
                for (int y = b.yMin; y < b.yMax; y++)
                    if (grid.RoomId[x, y] == r.Id)
                        r.Info.Occupied.Add(new Vector2Int(x, y));
        }

        void Spawn(GameObject prefab, Room r)
        {
            if (!prefab || r == null) return;
            Reserve(r);
            var go = Instantiate(prefab);
            if (viz.GridTransform) go.transform.SetParent(viz.GridTransform, false);
            go.transform.localPosition = viz.CellCenterLocal(r.Center.x, r.Center.y);

            // adapter to align doors/anchors to entrances/edges
            var adapter = go.GetComponent<RoomAdapter>();
            if (adapter && mapIndex.Rooms.TryGetValue(r.Id, out var ri))
                adapter.FitTo(r, ri, viz);

            //var pr = go.GetComponent<IPuzzleRoom>();
            //if (pr != null && mapIndex.Rooms.TryGetValue(r.Id, out var ri2))
            //    pr.Init(r, ri2);
        }

        if (plan.gate1 != null) Spawn(part1_SwordRoomPrefab, plan.gate1);
        if (plan.gate2 != null) Spawn(part2_LeverRoomPrefab, plan.gate2);
        if (plan.hub != null) Spawn(part3_FourwayHubPrefab, plan.hub);

        foreach (var (room, prefab) in plan.hubNeighbors)
            Spawn(prefab, room);
    }

    public void MuteRoomsTiles(Plan plan, DungeonGrid grid, string muteKind = "floor_prefab")
    {
        if (plan == null) return;

        void Mute(Room r)
        {
            if (r == null) return;

            var b = r.Bounds;

            for (int x = b.xMin; x < b.xMax; x++)
                for (int y = b.yMin; y < b.yMax; y++)
                    if (grid.RoomId[x, y] == r.Id)
                        grid.Kind[x, y] = muteKind;
        }

        Mute(plan.gate1);
        Mute(plan.gate2);
        Mute(plan.hub);

        foreach (var (room, _) in plan.hubNeighbors)
            Mute(room);
    }

    #endregion
    #region helpers

    static (List<Room> b1, List<Room> b2, List<Room> b3) SplitRoomsByBiome(DungeonGrid grid, List<Room> rooms)
    {
        var b1 = new List<Room>(); var b2 = new List<Room>(); var b3 = new List<Room>();
        foreach (var r in rooms)
        {
            var k = grid.Kind[r.Center.x, r.Center.y] ?? "";

            if (k.StartsWith("floor_entry", StringComparison.OrdinalIgnoreCase))
                b1.Add(r);
            else if (k.StartsWith("floor_quarry", StringComparison.OrdinalIgnoreCase))
                b2.Add(r);
            else
                b3.Add(r);
        }
        return (b1, b2, b3);
    }

    Room PickByPercentile(float[] rd, List<Room> set, float p)
    {
        if (set == null || set.Count == 0) return null;

        // todo: adapt to DungeonMapIndex
        set.RemoveAll(r => r.Bounds.width < minSize.x || r.Bounds.height < minSize.y);

        var sorted = new List<Room>(set);
        sorted.Sort((a, b) => rd[a.Id].CompareTo(rd[b.Id]));

        int idx = Mathf.Clamp(Mathf.RoundToInt((sorted.Count - 1) * Mathf.Clamp01(p)), 0, sorted.Count - 1);

        return (sorted.Count > 0) ? sorted[idx] : null;
    }

    static bool IsInDirection(Room from, Room candidate, Side dir)
    {
        var a = from.Center; var b = candidate.Center;

        return dir switch
        {
            Side.Right => b.x > a.x && Mathf.Abs(b.y - a.y) <= Mathf.Abs(b.x - a.x) * 2,
            Side.Left => b.x < a.x && Mathf.Abs(b.y - a.y) <= Mathf.Abs(b.x - a.x) * 2,
            Side.Top => b.y > a.y && Mathf.Abs(b.x - a.x) <= Mathf.Abs(b.y - a.y) * 2,
            Side.Bottom => b.y < a.y && Mathf.Abs(b.x - a.x) <= Mathf.Abs(b.y - a.y) * 2,
            _ => false
        };
    }

    Room ClosestInDirection(Room hub, List<Room> set, Side dir)
    {
        Room pick = null;
        int best = int.MaxValue;

        foreach (var r in set)
        {
            if (r == hub) continue;
            if (!IsInDirection(hub, r, dir)) continue;

            int manhattan = Mathf.Abs(r.Center.x - hub.Center.x) + Mathf.Abs(r.Center.y - hub.Center.y);

            if (manhattan < best) 
            {
                best = manhattan;
                pick = r;
            }
        }
        return pick;
    }
}
#endregion
// Optional adapter for the prefab to align anchors/doors to the room edges
public class RoomAdapter : MonoBehaviour
{
    public Vector2Int minSize = new(8, 8);
    public void FitTo(Room room, DungeonMapIndex.RoomIndex ri, TilemapVisualizer viz)
    {
        // Align root
        transform.localPosition = viz.CellCenterLocal(room.Center.x, room.Center.y);
        // TODO: optionally move child anchors to ri.Entrances or to edge midpoints
    }
}