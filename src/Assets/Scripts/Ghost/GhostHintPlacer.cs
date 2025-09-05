using UnityEngine;
using System.Collections.Generic;

/**
 * @file GhostHintPlacer.cs
 * @brief Places ghost hint triggers by explicit location presets (spawn room doors, special room doors, inside special, random doors).
 */
public sealed class GhostHintPlacer : MonoBehaviour
{
    #region config
    public enum PlacementMode { SpawnRoomDoors, SpecialRoomDoors, InsideSpecialRoom, RandomDungeonDoors }

    [Header("Placement")]
    [SerializeField] private PlacementMode placementMode = PlacementMode.SpawnRoomDoors;
    [SerializeField, Min(1)] private int countPerBiome = 3;
    [SerializeField, Min(0)] private int minSeparation = 6;

    [Header("Hint Tag for this placer")]
    [SerializeField] private string hintTagForThisPlacer = "";

    [Header("Prefabs and Data")]
    [SerializeField] private GameObject ghostHintTriggerPrefab;
    [SerializeField] private GhostHintDB hintDB;
    [SerializeField] private GhostPrefabSet prefabSet;
    #endregion

    #region main
    public void PlaceForCurrentBiome()
    {
        var dc = FindFirstObjectByType<DungeonController>();
        var seq = FindFirstObjectByType<BiomeSequenceController>();

        if (!dc || dc.MapIndex == null || dc.Grid == null || !dc.Viz || ghostHintTriggerPrefab == null || !hintDB || !prefabSet)
        {
            Debug.LogWarning("[GhostHintPlacer] Missing deps (dc/map/grid/viz/prefab/db/set). Aborting.");
            return;
        }

        List<Vector2Int> poolPrimary = placementMode switch
        {
            PlacementMode.SpawnRoomDoors => Cells_SpawnRoomDoors(dc),
            PlacementMode.SpecialRoomDoors => Cells_SpecialRoomDoors(dc),
            PlacementMode.InsideSpecialRoom => Cells_InsideSpecialRoom(dc),
            PlacementMode.RandomDungeonDoors => Cells_RandomDungeonDoors(dc),
            _ => new List<Vector2Int>()
        };

        var pools = new List<List<Vector2Int>>();
        pools.Add(poolPrimary);

        if (placementMode == PlacementMode.SpawnRoomDoors) pools.Add(Cells_SpecialRoomDoors(dc));
        if (placementMode != PlacementMode.RandomDungeonDoors) pools.Add(Cells_RandomDungeonDoors(dc));
        pools.Add(Cells_FarthestCorridors(dc)); 


        var picked = new List<Vector2Int>(countPerBiome);
        foreach (var pool in pools)
        {
            if (pool == null || pool.Count == 0) continue;

            foreach (var c in Shuffle(pool))
            {
                bool ok = true;
                for (int i = 0; i < picked.Count; i++)
                {
                    if (Cheb(picked[i], c) < minSeparation) { ok = false; break; }
                }
                if (!ok) continue;

                if (!picked.Contains(c))
                    picked.Add(c);

                if (picked.Count >= countPerBiome) break;
            }
            if (picked.Count >= countPerBiome) break;
        }

        int placed = 0;
        foreach (var cell in picked)
        {
            var world = dc.Viz.CellCenterWorld(cell.x, cell.y);
            var go = Instantiate(ghostHintTriggerPrefab, world, Quaternion.identity, dc.Viz.GridTransform);
            var trig = go.GetComponent<GhostHintTrigger>();

            if (trig)
                trig.Configure(hintDB, prefabSet, hintTagForThisPlacer);

            placed++;
        }

        if (placed < countPerBiome && picked.Count > 0)
        {
            var anchor = picked[0];
            var anchorPos = dc.Viz.CellCenterWorld(anchor.x, anchor.y);

            float baseRadius = 0.35f;

            for (int i = placed; i < countPerBiome; i++)
            {
                float ang = (i - placed + 1) * 40f * Mathf.Deg2Rad;
                var off = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * baseRadius;

                var go = Instantiate(ghostHintTriggerPrefab, anchorPos + off, Quaternion.identity, dc.Viz.GridTransform);
                var trig = go.GetComponent<GhostHintTrigger>();

                if (trig) 
                    trig.Configure(hintDB, prefabSet, hintTagForThisPlacer);
            }

            placed = countPerBiome;
        }

        Debug.Log($"[GhostHintPlacer] Mode={placementMode} Tag='{hintTagForThisPlacer}' " +
                  $"Placed {placed}/{countPerBiome} (primary={poolPrimary.Count}, sep={minSeparation}).");
    }
    #endregion

    #region mode components

    // Doors of the room where the PLAYER SPAWNS
    private List<Vector2Int> Cells_SpawnRoomDoors(DungeonController dc)
    {
        var player = FindFirstObjectByType<PlayerInventory>();

        if (!player)
        {
            Debug.LogWarning("[GhostHintPlacer] SpawnRoomDoors: player not found yet; falling back to RandomDungeonDoors.");
            return Cells_RandomDungeonDoors(dc);
        }

        Vector2Int cell = dc.ToCell(player.transform.position);

        if (!dc.Grid.InBounds(cell.x, cell.y))
        {
            Debug.LogWarning("[GhostHintPlacer] SpawnRoomDoors: player cell out of bounds; fallback.");
            return Cells_RandomDungeonDoors(dc);
        }

        int rid = dc.Grid.RoomId[cell.x, cell.y]; // -1 for corridor

        if (rid < 0 || !dc.MapIndex.Rooms.TryGetValue(rid, out var ri) || ri.Entrances == null || ri.Entrances.Count == 0)
        {
            Debug.LogWarning("[GhostHintPlacer] SpawnRoomDoors: no entrances for player's room; fallback.");
            return Cells_RandomDungeonDoors(dc);
        }

        return new List<Vector2Int>(ri.Entrances);
    }

    // Doors of the SPECIAL ROOM
    private List<Vector2Int> Cells_SpecialRoomDoors(DungeonController dc)
    {
        var res = new HashSet<Vector2Int>();
        var map = dc.MapIndex;
        var grid = dc.Grid;
        int W = grid.Width, H = grid.Height;

        var dirs = new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        for (int x = 0; x < W; x++)
        {
            for (int y = 0; y < H; y++)
            {
                string k = grid.Kind[x, y];
                if (!IsPrefabFloor(k)) continue;

                foreach (var d in dirs)
                {
                    int nx = x + d.x, ny = y + d.y;
                    if (!grid.InBounds(nx, ny)) continue;

                    var c = new Vector2Int(nx, ny);
                    if (map.CorridorCells.Contains(c))
                        res.Add(c);
                }
            }
        }
        return new List<Vector2Int>(res);
    }

    // Inside the SPECIAL ROOM
    private List<Vector2Int> Cells_InsideSpecialRoom(DungeonController dc)
    {
        var list = new List<Vector2Int>();
        var grid = dc.Grid;
        int W = grid.Width, H = grid.Height;

        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
                if (IsPrefabFloor(grid.Kind[x, y]))
                    list.Add(new Vector2Int(x, y));

        // Prefer center points
        if (list.Count > 0)
        {
            var centroid = Centroid(list);
            list.Sort((a, b) => SqrDist(a, centroid).CompareTo(SqrDist(b, centroid)));
        }
        return list;
    }

    // Random DOORS across the dungeon
    private List<Vector2Int> Cells_RandomDungeonDoors(DungeonController dc)
    {
        return new List<Vector2Int>(dc.MapIndex.EntranceCells);
    }

    // Fallback: farthest corridors from an entrance near special (spacing applied later)
    private List<Vector2Int> Cells_FarthestCorridors(DungeonController dc)
    {
        var seed = GuessSpecialEntranceCell(dc);
        var dist = BfsCorridorDistances(dc.MapIndex, seed);
        var all = new List<Vector2Int>(dist.Keys);
        all.Sort((a, b) => dist[b].CompareTo(dist[a])); // farthest first
        return all;
    }
    #endregion

    #region helpers
    private static bool IsPrefabFloor(string k) => !string.IsNullOrEmpty(k) && k.StartsWith("floor_prefab", System.StringComparison.OrdinalIgnoreCase);

    private static Vector2Int Centroid(List<Vector2Int> pts)
    {
        long sx = 0, sy = 0;

        for (int i = 0; i < pts.Count; i++)
        {
            sx += pts[i].x;
            sy += pts[i].y; 
        }

        return new Vector2Int((int)(sx / pts.Count), (int)(sy / pts.Count));
    }

    private static int SqrDist(Vector2Int a, Vector2Int b)
    {
        int dx = a.x - b.x, dy = a.y - b.y;
        return dx * dx + dy * dy;
    }

    private static List<Vector2Int> TryFillWith(List<Vector2Int> picked, List<Vector2Int> pool)
    {
        var need = picked.Capacity > 0 ? picked.Capacity : int.MaxValue;

        return TryFillWith(picked, pool, 6, need);
    }
    private static List<Vector2Int> TryFillWith(List<Vector2Int> picked, List<Vector2Int> pool, int separation, int need)
    {
        var outList = new List<Vector2Int>(picked);
        if (pool == null || pool.Count == 0) return outList;

        foreach (var c in Shuffle(pool))
        {
            bool ok = true;
            foreach (var p in outList)
                if (Cheb(p, c) < separation)
                {
                    ok = false;
                    break; 
                }

            if (!ok) continue;

            outList.Add(c);
            if (outList.Count >= need) break;
        }
        return outList;
    }

    private static List<Vector2Int> GreedySeparate(IEnumerable<Vector2Int> src, int separation, int maxCount)
    {
        var outList = new List<Vector2Int>(maxCount);
        foreach (var c in src)
        {
            bool ok = true;
            foreach (var p in outList) 
                if (Cheb(p, c) < separation)
                { 
                    ok = false;
                    break;
                }

            if (!ok) continue;

            outList.Add(c);
            if (outList.Count >= maxCount) break;
        }
        return outList;
    }

    private static IEnumerable<Vector2Int> Shuffle(List<Vector2Int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = Random.Range(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);

            yield return list[i];
        }
    }

    private static int Cheb(Vector2Int a, Vector2Int b) => Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));

    private static Dictionary<Vector2Int, int> BfsCorridorDistances(DungeonMapIndex map, Vector2Int seed)
    {
        var dist = new Dictionary<Vector2Int, int>();
        var q = new Queue<Vector2Int>();

        if (!map.CorridorCells.Contains(seed))
            seed = NearestCorridor(map, seed);

        dist[seed] = 0; q.Enqueue(seed);
        var dirs = new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        while (q.Count > 0)
        {
            var u = q.Dequeue();
            foreach (var d in dirs)
            {
                var v = u + d;

                if (!map.CorridorCells.Contains(v)) continue;
                if (dist.ContainsKey(v)) continue;

                dist[v] = dist[u] + 1;
                q.Enqueue(v);
            }
        }
        return dist;
    }

    private static Vector2Int NearestCorridor(DungeonMapIndex map, Vector2Int from)
    {
        for (int r = 0; r < 64; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    var c = new Vector2Int(from.x + dx, from.y + dy);

                    if (map.CorridorCells.Contains(c)) return c;
                }
            }
        }

        foreach (var c in map.CorridorCells)
            return c;

        return from;
    }

    private static Vector2Int GuessSpecialEntranceCell(DungeonController dc)
    {
        var grid = dc.Grid;
        var map = dc.MapIndex;

        // Prefer an entrance that is directly adjacent to any prefab floor tile
        var dirs = new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
        foreach (var e in map.EntranceCells)
        {
            foreach (var d in dirs)
            {
                var n = e + d;
                if (grid.InBounds(n.x, n.y) && IsPrefabFloor(grid.Kind[n.x, n.y]))
                    return e; // first door touching the special room
            }
        }

        // Build list of prefab floor cells
        var prefabFloors = new List<Vector2Int>();
        for (int x = 0; x < grid.Width; x++)
            for (int y = 0; y < grid.Height; y++)
                if (IsPrefabFloor(grid.Kind[x, y]))
                    prefabFloors.Add(new Vector2Int(x, y));

        //  pick the entrance closest to the prefab area
        if (prefabFloors.Count > 0 && map.EntranceCells.Count > 0)
        {
            var best = default(Vector2Int);
            var bestSqr = int.MaxValue;

            foreach (var e in map.EntranceCells)
            {
                foreach (var pf in prefabFloors)
                {
                    int ds = SqrDist(e, pf);
                    if (ds < bestSqr)
                    { 
                        bestSqr = ds;
                        best = e;
                    }
                }
            }
            if (bestSqr != int.MaxValue) return best;
        }

        // Fall back to "most connected" room entrance
        int bestScore = -1;
        Vector2Int bestEntrance = default;
        foreach (var ri in map.Rooms.Values)
        {
            int eCount = ri.Entrances?.Count ?? 0;

            if (eCount > bestScore && eCount > 0)
            {
                bestScore = eCount;
                bestEntrance = ri.Entrances[0];
            }
        }
        if (bestScore >= 0) return bestEntrance;

        // Any entrance at all
        foreach (var e in map.EntranceCells)
            return e;

        // pick a corridor near the grid center
        var approx = new Vector2Int(grid.Width / 2, grid.Height / 2);
        return NearestCorridor(map, approx);
    }
    #endregion
}