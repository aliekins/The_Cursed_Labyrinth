using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PropPopulator : MonoBehaviour
{
    [Header("Required")]
    [SerializeField] private TilemapVisualizer visualizer;

    [Header("Global Taste")]
    [Range(0, 3)] public int doorAisleDepth = 1;       // clear path in from each door
    [Range(0, 2)] public int wallClamp = 0;            // how tight wall props hug walls
    [Range(0.5f, 3f)] public float densityPer64 = 3f; // interior density per 64 tiles
    [SerializeField] public string corridorPrefix = "floor_corridor";

    [Header("Render")]
    public string sortingLayer = "Props";
    public int sortingOrder = 4; // use int.MinValue for y-sort
    public bool fitToCell = true;

    [Header("Biome: Entry")]
    public List<SimpleProp> entryCorner = new();
    public List<SimpleProp> entryWall = new();
    public List<SimpleProp> entryInterior = new();

    [Header("Biome: Quarry")]
    public List<SimpleProp> quarryCorner = new();
    public List<SimpleProp> quarryWall = new();
    public List<SimpleProp> quarryInterior = new();

    [Header("Biome: Grove")]
    public List<SimpleProp> groveCorner = new();
    public List<SimpleProp> groveWall = new();
    public List<SimpleProp> groveInterior = new();

    [Serializable]
    public class SimpleProp
    {
        public Sprite sprite;
        [Range(0f, 1f)] public float chance = 0.85f;
        public int min = 1, max = 2;
        [Tooltip("Chebyshev spacing for single items / between cluster satellites.")]
        public int separation = 1;
        [Tooltip("Ignore carpet mask for this prop.")]
        public bool allowCarpet = false;

        [Header("Grouping (optional)")]
        public bool useGrouping = false;
        public Vector2Int groupSizeRange = new Vector2Int(2, 4);
        public int groupRadius = 2;                 // satellite radius around cluster center
        public int minDistanceBetweenGroups = 3;    // spacing between clusters
    }

    // runtime
    private readonly HashSet<Vector2Int> occupied = new();
    private readonly List<GameObject> spawned = new();
    private readonly HashSet<Vector2Int> trapCells = new();

    public void Clear()
    {
        foreach (var go in spawned) if (go) Destroy(go);
        spawned.Clear();
        occupied.Clear();
        trapCells.Clear();
    }

    public void Populate(DungeonGrid grid, List<Room> rooms, DungeonMapIndex index, bool[,] carpetMask, int globalSeed, IReadOnlyCollection<Vector2Int> traps = null)
    {
        Clear();

        if (!visualizer)
            visualizer = FindAnyObjectByType<TilemapVisualizer>();

        if (!visualizer || grid == null || rooms == null || rooms.Count == 0) return;

        if (traps != null) foreach (var c in traps) 
                trapCells.Add(c);

        foreach (var room in rooms)
        {
            var info = room.Info;

            if (info == null || info.Interior == null || info.Interior.Count == 0) continue;

            // Pick biome rules by dominant interior kind
            string prefix = DominantRoomKindPrefix(grid, room, corridorPrefix);
            (var cornerRules, var wallRules, var interiorRules) = PickBiomeLists(prefix);

            // Build door-aisle forbidden set
            var forbidden = BuildAisles(index, room, doorAisleDepth);

            // Stable rng per room
            var rng = MakeRng(globalSeed, room.Id);

            // Corners
            var cornerSlots = BestCorners(info, maxCount: 2);
            PlaceFromBucket(grid, room, cornerSlots, cornerRules, index, carpetMask, forbidden, rng);

            // Walls
            var wallSlots = EvenlySpacedWallSlots(info, wallClamp, spacing: 3, jitter: 1, rng);
            PlaceFromBucket(grid, room, wallSlots, wallRules, index, carpetMask, forbidden, rng);

            //  Interior
            var interiorSlots = InteriorSlots(info, carpetMask, allowCarpetDefault: false);
            int area = room.Bounds.width * room.Bounds.height;
            int interiorBudget = Mathf.Max(1, Mathf.FloorToInt(area / 64f * densityPer64));

            TrimSlotsDeterministic(interiorSlots, interiorBudget * 6, rng);
            PlaceFromBucket(grid, room, interiorSlots, interiorRules, index, carpetMask, forbidden, rng);
        }
    }

    private (List<SimpleProp> corner, List<SimpleProp> wall, List<SimpleProp> interior) PickBiomeLists(string prefix)
    {
        if (prefix.StartsWith("floor_quarry", StringComparison.OrdinalIgnoreCase))
            return (quarryCorner, quarryWall, quarryInterior);
        if (prefix.StartsWith("floor_grove", StringComparison.OrdinalIgnoreCase))
            return (groveCorner, groveWall, groveInterior);
        return (entryCorner, entryWall, entryInterior);
    }

    private static string DominantRoomKindPrefix(DungeonGrid grid, Room room, string corridorPrefix)
    {
        var counts = new Dictionary<string, int>(8, StringComparer.OrdinalIgnoreCase);

        foreach (var c in room.Info.Interior)
        {
            var k = grid.Kind[c.x, c.y];
            if (string.IsNullOrEmpty(k)) continue;
            int i = k.IndexOf('_'); int j = (i >= 0) ? k.IndexOf('_', i + 1) : -1;
            string p = (j > 0) ? k.Substring(0, j) : k;
            if (p.StartsWith(corridorPrefix, StringComparison.OrdinalIgnoreCase)) continue;
            counts[p] = counts.TryGetValue(p, out int n) ? n + 1 : 1;
        }
        string best = "floor_entry"; int bestN = 0;
        foreach (var kv in counts) if (kv.Value > bestN) { best = kv.Key; bestN = kv.Value; }
        return best;
    }

    private static List<Vector2Int> BestCorners(RoomInfo info, int maxCount)
    {
        var ranked = new List<(Vector2Int c, int s)>(info.CornerAnchors.Count);
        var b = info.Bounds;

        foreach (var c in info.CornerAnchors)
        {
            if (info.Entrances.Contains(c)) continue;

            int dx = Mathf.Min(Mathf.Abs(c.x - b.xMin), Mathf.Abs((b.xMax - 1) - c.x));
            int dy = Mathf.Min(Mathf.Abs(c.y - b.yMin), Mathf.Abs((b.yMax - 1) - c.y));

            ranked.Add((c, dx + dy));
        }
        ranked.Sort((a, b2) => a.s.CompareTo(b2.s));

        int take = Mathf.Min(maxCount, ranked.Count);
        var list = new List<Vector2Int>(take);

        for (int i = 0; i < take; i++) 
            list.Add(ranked[i].c);

        return list;
    }

    private static List<Vector2Int> EvenlySpacedWallSlots(RoomInfo info, int clamp, int spacing, int jitter, System.Random rng)
    {
        var filtered = new List<Vector2Int>(info.EdgeBand.Count);

        foreach (var c in info.EdgeBand)
            if (WallDistanceMin(info.Bounds, c) <= clamp && !info.Entrances.Contains(c))
                filtered.Add(c);

        var left = new List<Vector2Int>();
        var right = new List<Vector2Int>();
        var top = new List<Vector2Int>();
        var bottom = new List<Vector2Int>();

        var b = info.Bounds;
        foreach (var c in filtered)
        {
            if (c.x == b.xMin) left.Add(c);
            else if (c.x == b.xMax - 1) right.Add(c);
            else if (c.y == b.yMax - 1) top.Add(c);
            else if (c.y == b.yMin) bottom.Add(c);
        }

        left.Sort((a, bb) => a.y.CompareTo(bb.y));
        right.Sort((a, bb) => a.y.CompareTo(bb.y));
        top.Sort((a, bb) => a.x.CompareTo(bb.x));
        bottom.Sort((a, bb) => a.x.CompareTo(bb.x));

        var outList = new List<Vector2Int>();
        SampleLine(left, spacing, jitter, rng, outList);
        SampleLine(right, spacing, jitter, rng, outList);
        SampleLine(top, spacing, jitter, rng, outList);
        SampleLine(bottom, spacing, jitter, rng, outList);

        return outList;
    }

    private static void SampleLine(List<Vector2Int> line, int spacing, int jitter, System.Random rng, List<Vector2Int> outList)
    {
        if (line.Count == 0) return;

        int i = 0;
        while (i < line.Count)
        {
            outList.Add(line[i]);
            int step = Mathf.Clamp(spacing + rng.Next(-jitter, jitter + 1), 1, spacing + jitter);
            i += step;
        }
    }

    private static List<Vector2Int> InteriorSlots(RoomInfo info, bool[,] carpet, bool allowCarpetDefault)
    {
        var list = new List<Vector2Int>(info.Interior.Count);
        foreach (var c in info.Interior)
        {
            if (info.Entrances.Contains(c)) continue;
            if (!allowCarpetDefault && carpet != null && carpet[c.x, c.y]) continue;

            list.Add(c);
        }
        return list;
    }

    private HashSet<Vector2Int> BuildAisles(DungeonMapIndex index, Room room, int depth)
    {
        var blocked = new HashSet<Vector2Int>();
        if (depth <= 0 || index == null) return blocked;

        foreach (var e in room.Info.Entrances)
        {
            blocked.Add(e);
            Vector2Int? corridor = null;
            foreach (var d in new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down })
            {
                var n = e + d;

                if (index.CorridorCells != null && index.CorridorCells.Contains(n)) 
                { 
                    corridor = n; 
                    break;
                }
            }

            if (!corridor.HasValue) continue;

            var dirIn = e - corridor.Value;
            var cur = e;

            for (int i = 0; i < depth; i++)
            {
                cur += dirIn;
                if (!room.Info.Bounds.Contains(cur)) break;
                blocked.Add(cur);
            }
        }
        return blocked;
    }

    private static int WallDistanceMin(RectInt b, Vector2Int c)
    {
        int dl = c.x - b.xMin;
        int dr = (b.xMax - 1) - c.x;
        int db = c.y - b.yMin;
        int dt = (b.yMax - 1) - c.y;

        return Mathf.Min(Mathf.Min(dl, dr), Mathf.Min(db, dt));
    }

    private static void TrimSlotsDeterministic(List<Vector2Int> list, int max, System.Random rng)
    {
        if (list.Count <= max) return;
        ShuffleDeterministic(list, rng);
        list.RemoveRange(max, list.Count - max);
    }

    #region placement
    private void PlaceFromBucket(DungeonGrid grid, Room room, List<Vector2Int> slots, List<SimpleProp> props, DungeonMapIndex index, bool[,] carpetMask, HashSet<Vector2Int> forbidden, System.Random rng)
    {
        if (props == null || props.Count == 0 || slots == null || slots.Count == 0) return;

        var candidates = BuildCandidates(slots, room.Info, carpetMask, forbidden);
        ShuffleDeterministic(candidates, rng);

        if (candidates.Count == 0) return;

        foreach (var rule in props)
        {
            if (!rule?.sprite) continue;
            if (rng.NextDouble() > rule.chance) continue;

            int req = rng.Next(rule.min, rule.max + 1);
            int allowed = CapByDensity(room, req, densityPer64);

            if (allowed <= 0) continue;

            if (rule.useGrouping)
                PlaceGroups(rule, candidates, allowed, rng, grid, room);
            else
                PlaceSingles(rule, candidates, allowed, rng, grid, room, rule.allowCarpet ? null : carpetMask);
        }
    }

    private List<Vector2Int> BuildCandidates(List<Vector2Int> baseSlots, RoomInfo info, bool[,] carpet, HashSet<Vector2Int> forbidden)
    {
        var list = new List<Vector2Int>(baseSlots.Count);

        foreach (var c in baseSlots)
        {
            if (forbidden.Contains(c)) continue;
            if (trapCells.Contains(c)) continue;

            list.Add(c);
        }
        return list;
    }

    private void PlaceSingles(SimpleProp rule, List<Vector2Int> candidates, int allowed, System.Random rng, DungeonGrid grid, Room room, bool[,] carpetMask)
    {
        int placed = 0;
        var accepted = new List<Vector2Int>(allowed);

        foreach (var c in candidates)
        {
            if (placed >= allowed) break;
            if (!grid.InBounds(c.x, c.y)) continue;
            if (occupied.Contains(c) || !room.Info.IsFree(c)) continue;
            if (trapCells.Contains(c)) continue;
            if (carpetMask != null && !rule.allowCarpet && carpetMask[c.x, c.y]) continue;

            if (rule.separation > 0)
            {
                bool tooClose = false;

                foreach (var a in accepted)
                {
                    if (Mathf.Max(Mathf.Abs(a.x - c.x), Mathf.Abs(a.y - c.y)) < rule.separation)
                    {
                        tooClose = true; 
                        break;
                    }
                }
                if (tooClose) continue;
            }

            Spawn(rule.sprite, c);
            accepted.Add(c);
            occupied.Add(c);
            room.Info.Occupied.Add(c);
            placed++;
        }
    }

    private void PlaceGroups(SimpleProp rule, List<Vector2Int> candidates, int allowed, System.Random rng, DungeonGrid grid, Room room)
    {
        int placed = 0;
        var centers = new List<Vector2Int>();

        foreach (var center in candidates)
        {
            if (placed >= allowed) break;
            if (!grid.InBounds(center.x, center.y)) continue;
            if (occupied.Contains(center) || !room.Info.IsFree(center)) continue;
            if (trapCells.Contains(center)) continue;

            // keep cluster centers spaced
            bool clash = false;
            foreach (var cc in centers)
            {
                if (Cheb(center, cc) < rule.minDistanceBetweenGroups)
                {
                    clash = true;
                    break;
                }
            }
            if (clash) continue;

            // place center
            Spawn(rule.sprite, center);
            centers.Add(center);
            occupied.Add(center);
            room.Info.Occupied.Add(center);
            placed++;

            // satellites
            int groupSize = Mathf.Clamp(rng.Next(rule.groupSizeRange.x, rule.groupSizeRange.y + 1), 1, allowed - placed + 1);
            int tries = 0;

            while (groupSize > 1 && placed < allowed && tries < 48)
            {
                tries++;
                int dx = rng.Next(-rule.groupRadius, rule.groupRadius + 1);
                int dy = rng.Next(-rule.groupRadius, rule.groupRadius + 1);
                var p = new Vector2Int(center.x + dx, center.y + dy);

                if (!room.Info.Bounds.Contains(p)) continue;
                if (occupied.Contains(p) || !room.Info.IsFree(p)) continue;
                if (trapCells.Contains(p)) continue;

                bool tooClose = false;
                foreach (var cc in centers)
                {
                    if (Cheb(p, cc) < rule.separation)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;

                Spawn(rule.sprite, p);
                occupied.Add(p);
                room.Info.Occupied.Add(p);
                placed++;
                groupSize--;
            }
        }
    }

    private void Spawn(Sprite sprite, Vector2Int cell)
    {
        var parent = visualizer.GridTransform ? visualizer.GridTransform : transform;
        var go = new GameObject($"prop_{cell.x}_{cell.y}");

        go.transform.SetParent(parent, false);
        go.transform.localPosition = visualizer.CellCenterLocal(cell.x, cell.y);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = (sortingOrder == int.MinValue) ? -cell.y : sortingOrder;

        if (fitToCell && sprite)
        {
            var size = sprite.bounds.size;
            float cs = visualizer.CellSize;
            if (size.x > 0.0001f && size.y > 0.0001f)
            {
                float sx = cs / size.x, sy = cs / size.y;
                go.transform.localScale = new Vector3(sx, sy, 1f);
            }
        }

        spawned.Add(go);
    }
    #endregion
    #region utils
    private static int Cheb(Vector2Int a, Vector2Int b) => Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));

    private static int CapByDensity(Room room, int requested, float per64)
    {
        int area = room.Bounds.width * room.Bounds.height;
        int cap = Mathf.Max(1, Mathf.FloorToInt((area / 64f) * per64));
        return Mathf.Min(requested, cap);
    }

    private static System.Random MakeRng(int baseSeed, int roomId)
    {
        unchecked
        {
            int h = 17;
            h = h * 31 + baseSeed;
            h = h * 31 + roomId;
            return new System.Random(h);
        }
    }

    private static void ShuffleDeterministic<T>(List<T> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; --i)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
    #endregion
}