using System;
using System.Collections.Generic;
using UnityEngine;

public class PuzzlePlacer : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject part1_SwordRoomPrefab;   // Part 1
    public GameObject part2_LeverRoomPrefab;   // Part 2
    public GameObject part3_HubRoomPrefab;     // Part 3 hub
    public GameObject hintRoomPrefab;          // dead-end hint

    [Header("Room constraints")]
    public Vector2Int minSize = new(8, 8);
    public int maxEntrances = 3;

    [Header("Distance bands (percentiles)")]
    [Range(0f, 1f)] public float p1Max = 0.30f;
    [Range(0f, 1f)] public float p2Min = 0.40f;
    [Range(0f, 1f)] public float p2Max = 0.65f;
    [Range(0f, 1f)] public float p3Min = 0.80f;

    public void Build(DungeonGrid grid, List<Room> rooms, DungeonMapIndex index, int seed)
    {
        if (rooms == null || rooms.Count == 0 || index == null) return;

        var edges = GraphUtils.BuildMstByDistance(rooms);
        float[] dist = GraphUtils.ComputeRoomDistances(rooms, edges, 0);

        // pick rooms for each part
        Room r1 = PickRoom(rooms, dist, 0f, p1Max, index);
        Room r2 = PickRoom(rooms, dist, p2Min, p2Max, index);
        Room rHub = PickRoom(rooms, dist, p3Min, 1f, index);

        if (r1 != null) SpawnPuzzle(part1_SwordRoomPrefab, r1, grid, index);
        if (r2 != null) SpawnPuzzle(part2_LeverRoomPrefab, r2, grid, index);

        if (rHub != null && part3_HubRoomPrefab)
        {
            var hubGO = SpawnPuzzle(part3_HubRoomPrefab, rHub, grid, index);

            // choose 4 DEAD-END rooms closest to the hub
            var hintRooms = PickDeadEnds(rooms, index, rHub, want: 4);
            // assign token order (fixed or shuffled)
            TokenType[] order = { TokenType.Feather, TokenType.Skull, TokenType.Gem, TokenType.Leaf };

            for (int i = 0; i < hintRooms.Count && i < order.Length; i++)
            {
                var hr = hintRooms[i];
                var go = SpawnPuzzle(hintRoomPrefab, hr, grid, index);
                var decor = go.GetComponent<HintRoomDecor>();
                if (decor) { decor.token = order[i]; decor.Init(hr, index.Rooms[hr.Id]); }
            }

            // OPTIONAL: display token icons near each socket in the hub (if you want explicit clues)
            // Or rely on hint-room decor as the clue.
        }
    }

    Room PickRoom(List<Room> rooms, float[] rd, float pMin, float pMax, DungeonMapIndex index)
    {
        var valid = new List<(Room r, float d)>();
        var ds = new List<float>();
        for (int i = 0; i < rooms.Count; i++) if (!float.IsInfinity(rd[i])) ds.Add(rd[i]);
        if (ds.Count == 0) return null;
        ds.Sort();
        float dLo = Lerp(ds, pMin), dHi = Lerp(ds, pMax);

        foreach (var r in rooms)
        {
            float d = rd[r.Id];
            if (d < dLo || d > dHi) continue;
            if (r.Bounds.width < minSize.x || r.Bounds.height < minSize.y) continue;
            if (index.Rooms.TryGetValue(r.Id, out var ri))
                if (ri.Entrances != null && ri.Entrances.Count > maxEntrances) continue;
            valid.Add((r, d));
        }
        if (valid.Count == 0) return null;

        valid.Sort((a, b) => {
            var ra = index.Rooms[a.r.Id]; var rb = index.Rooms[b.r.Id];
            int ea = ra.Entrances?.Count ?? 0, eb = rb.Entrances?.Count ?? 0;
            int cmpE = ea.CompareTo(eb); if (cmpE != 0) return cmpE;
            int sa = a.r.Bounds.width * a.r.Bounds.height;
            int sb = b.r.Bounds.width * b.r.Bounds.height;
            int cmpS = -sa.CompareTo(sb); if (cmpS != 0) return cmpS;
            return a.d.CompareTo(b.d);
        });
        return valid[0].r;
    }

    List<Room> PickDeadEnds(List<Room> rooms, DungeonMapIndex index, Room around, int want)
    {
        var list = new List<(Room r, int dist)>();
        foreach (var r in rooms)
        {
            if (!index.Rooms.TryGetValue(r.Id, out var ri)) continue;
            int entrances = ri.Entrances?.Count ?? 0;
            if (entrances != 1) continue;                       // dead end
            if (r.Id == around.Id) continue;
            int manhattan = Mathf.Abs(r.Center.x - around.Center.x) + Mathf.Abs(r.Center.y - around.Center.y);
            list.Add((r, manhattan));
        }
        list.Sort((a, b) => a.dist.CompareTo(b.dist));

        var res = new List<Room>(Mathf.Min(want, list.Count));
        for (int i = 0; i < res.Capacity; i++) res.Add(list[i].r);
        return res;
    }

    float Lerp(List<float> sorted, float p)
    {
        p = Mathf.Clamp01(p);
        if (sorted.Count == 1) return sorted[0];
        float idx = p * (sorted.Count - 1);
        int lo = Mathf.FloorToInt(idx);
        int hi = Mathf.Min(lo + 1, sorted.Count - 1);
        float t = idx - lo;
        return Mathf.Lerp(sorted[lo], sorted[hi], t);
    }

    GameObject SpawnPuzzle(GameObject prefab, Room r, DungeonGrid grid, DungeonMapIndex index)
    {
        if (!prefab) return null;

        // reserve the whole room so props/traps skip it
        var b = r.Bounds;
        for (int x = b.xMin; x < b.xMax; x++)
            for (int y = b.yMin; y < b.yMax; y++)
                if (grid.RoomId[x, y] == r.Id)
                    r.Info.Occupied.Add(new Vector2Int(x, y));

        var viz = FindAnyObjectByType<TilemapVisualizer>();
        var go = Instantiate(prefab);

        if (viz && viz.GridTransform)
            go.transform.SetParent(viz.GridTransform, false);

        var pos = viz ? viz.CellCenterLocal(r.Center.x, r.Center.y) : new Vector3(r.Center.x, r.Center.y, 0f);
        go.transform.localPosition = pos;

        // hand off room refs to the prefab, if it cares
        var pr = go.GetComponent<IPuzzleRoom>();
        if (pr != null)
            pr.Init(r, index.Rooms[r.Id]);

        return go;
    }
}