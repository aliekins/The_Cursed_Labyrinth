/// \file PropManager.cs
/// \brief Spawns sprite-based props per biome with placement filters 
using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PropManager : MonoBehaviour
{
    [SerializeField] private PropRules_SO rules;                  ///< Per-biome prop rules
    [SerializeField] private TilemapVisualizer visualizer;        ///< Grid/tilemap helper (assigned or auto-found)

    private readonly HashSet<Vector2Int> _occupied = new();       ///< Prevents overlaps across rules
    private readonly List<GameObject> _spawned = new();           ///< Spawned props for Clear()

    public void Build(DungeonGrid grid, List<Room> rooms, System.Func<string, int> resolveTier, IReadOnlyList<Vector2Int> doorCells, bool[,] carpetMask)
    {
        Clear();

        if (!rules || grid == null || rooms == null) return;

        if (!visualizer) visualizer = FindAnyObjectByType<TilemapVisualizer>();
        if (!visualizer) 
        {
            Debug.LogWarning("PropManager: TilemapVisualizer not found."); 
            return; 
        }

        HashSet<Vector2Int> doorSet = null;
        if (doorCells != null) doorSet = new HashSet<Vector2Int>(doorCells);

        foreach (var room in rooms)
        {
            var center = room.Center;
            var kind = grid.Kind[center.x, center.y] ?? string.Empty;

            var group = FindBiomeGroup(kind);
            if (group == null) continue;
            if (UnityEngine.Random.value > group.roomPickChance) continue;

            TryPlaceGroupInRoom(grid, room, group, doorSet, carpetMask);
        }
    }
    public void Clear()
    {
        foreach (var go in _spawned)
            if (go) Destroy(go);
        _spawned.Clear();
        _occupied.Clear();
    }

    private PropRules_SO.BiomeGroup FindBiomeGroup(string cellKind)
    {
        if (rules == null || rules.biomes == null) return null;
        foreach (var g in rules.biomes)
        {
            if (!string.IsNullOrEmpty(g.biomeKindPrefix) &&
                cellKind.StartsWith(g.biomeKindPrefix, System.StringComparison.OrdinalIgnoreCase))
                return g;
        }
        return null;
    }

    private void TryPlaceGroupInRoom(
        DungeonGrid grid,
        Room room,
        PropRules_SO.BiomeGroup group,
        HashSet<Vector2Int> doorSet,
        bool[,] carpetMask)
    {
        foreach (var rule in group.rules)
        {
            if (rule == null || !rule.sprite) continue;
            if (UnityEngine.Random.value > rule.chance) continue;

            int toPlace = UnityEngine.Random.Range(rule.minCount, rule.maxCount + 1);
            PlaceForRuleInRoom(grid, room, rule, toPlace, doorSet, carpetMask);
        }
    }

    private static readonly Vector2Int[] Directions = {new(1,0), new(-1,0), new(0,1), new(0,-1)};

    private bool PassesFilters(DungeonGrid grid, Vector2Int cell, PropRules_SO.PropRule rule, HashSet<Vector2Int> doorSet, bool[,] carpetMask)
    {
        string k = grid.Kind[cell.x, cell.y] ?? string.Empty;

        if (rule.avoidCorridor && k.StartsWith("floor_corridor", System.StringComparison.OrdinalIgnoreCase))
            return false;

        if (rule.avoidCarpet && carpetMask != null && carpetMask[cell.x, cell.y])
            return false;

        if (rule.avoidDoorCells && doorSet != null && doorSet.Contains(cell))
            return false;

        return true;
    }

    private void CreatePropGO(PropRules_SO.PropRule rule, Vector2Int cell)
    {
        var parent = visualizer.GridTransform ? visualizer.GridTransform : transform;

        var go = new GameObject($"prop_{rule.kind}_{cell.x}_{cell.y}");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = visualizer.CellCenterLocal(cell.x, cell.y);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = rule.sprite;

        if (rule.overrideMaterial) sr.material = rule.overrideMaterial;
        else
        {
            var lit = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            if (lit) sr.material = new Material(lit);
        }

        if (!string.IsNullOrEmpty(rule.sortingLayer)) sr.sortingLayerName = rule.sortingLayer;
        sr.sortingOrder = rule.orderInLayer;

        if (rule.fitToCell && sr.sprite)
        {
            var size = sr.sprite.bounds.size; 
            float cellSize = visualizer.CellSize;

            if (size.x > 0.0001f && size.y > 0.0001f)
            {
                float sx = cellSize / size.x;
                float sy = cellSize / size.y;
                go.transform.localScale = new Vector3(sx, sy, 1f);
            }
        }

        // Optional - not utilized yet
        AddCollider(go, rule);

        _spawned.Add(go);
    }

    private static void AddCollider(GameObject go, PropRules_SO.PropRule rule)
    {
        switch (rule.collider)
        {
            case PropColliderMode.Box:
                var box = go.AddComponent<BoxCollider2D>();
                box.size = rule.colliderSize;
                box.isTrigger = rule.colliderIsTrigger;
                break;

            case PropColliderMode.Circle:
                var circ = go.AddComponent<CircleCollider2D>();
                circ.radius = rule.colliderRadius;
                circ.isTrigger = rule.colliderIsTrigger;
                break;
        }
    }

    private void PlaceForRuleInRoom(DungeonGrid grid, Room room, PropRules_SO.PropRule rule, int toPlace,  HashSet<Vector2Int> doorSet, bool[,] carpetMask)
    {
        if (toPlace <= 0) return;
        var info = room.Info;

        bool Passes(Vector2Int c)
        {
            if (!grid.InBounds(c.x, c.y)) return false;
            if (_occupied.Contains(c) || !info.IsFree(c)) return false;
            return PassesFilters(grid, c, rule, doorSet, carpetMask);
        }

        if (IsChest(rule))
        {
            var byCenter = RankByCenter(info.Interior, info.Center);
            int placed = 0;
            foreach (var c in byCenter)
            {
                if (info.Entrances.Contains(c)) continue;
                if (!Passes(c)) continue;

                CreatePropGO(rule, c);
                _occupied.Add(c); info.Occupied.Add(c);
                if (++placed >= toPlace) break;
            }
            return;
        }

        int remaining = toPlace;

        remaining -= BlueNoisePlace(rule, PreferCorners(info, grid, rule, doorSet, carpetMask), remaining, 1.0f, info);
        if (remaining <= 0) return;

        remaining -= BlueNoisePlace(rule, PreferEdges(info, grid, rule, doorSet, carpetMask), remaining, 1.0f, info);
        if (remaining <= 0) return;

        BlueNoisePlace(rule, PreferInterior(info, grid, rule, doorSet, carpetMask), remaining, 1.0f, info);
    }

    private List<Vector2Int> PreferCorners(RoomInfo info, DungeonGrid grid, PropRules_SO.PropRule rule, HashSet<Vector2Int> doorSet, bool[,] carpetMask)
    {
        var ranked = new List<(Vector2Int c, int s)>(info.CornerAnchors.Count);

        foreach (var c in info.CornerAnchors)
        {
            if (info.Entrances.Contains(c)) continue;
            if (!PassesFilters(grid, c, rule, doorSet, carpetMask)) continue;

            ranked.Add((c, CornerScore(info, c)));
        }

        ranked.Sort((a, b) => a.s.CompareTo(b.s)); 

        var outList = new List<Vector2Int>(ranked.Count);

        foreach (var (c, _) in ranked) 
            outList.Add(c);

        return outList;
    }

    private List<Vector2Int> PreferEdges(RoomInfo info, DungeonGrid grid, PropRules_SO.PropRule rule, HashSet<Vector2Int> doorSet, bool[,] carpetMask)
    {
        var ranked = new List<(Vector2Int c, int d)>(info.EdgeBand.Count);

        foreach (var c in info.EdgeBand)
        {
            if (info.Entrances.Contains(c)) continue;
            if (!PassesFilters(grid, c, rule, doorSet, carpetMask)) continue;

            ranked.Add((c, WallDistanceMin(info, c)));
        }

        ranked.Sort((a, b) => a.d.CompareTo(b.d)); 

        var outList = new List<Vector2Int>(ranked.Count);

        foreach (var (c, _) in ranked) 
            outList.Add(c);

        return outList;
    }

    private List<Vector2Int> PreferInterior(RoomInfo info, DungeonGrid grid, PropRules_SO.PropRule rule, HashSet<Vector2Int> doorSet, bool[,] carpetMask)
    {
        var outList = new List<Vector2Int>(info.Interior.Count);
        foreach (var c in info.Interior)
            if (PassesFilters(grid, c, rule, doorSet, carpetMask) && !info.Entrances.Contains(c))
                outList.Add(c);
        Shuffle(outList);
        return outList;
    }
    private int BlueNoisePlace(PropRules_SO.PropRule rule, List<Vector2Int> candidates, int count, float rTiles, RoomInfo info)
    {
        if (count <= 0 || candidates == null || candidates.Count == 0) return 0;

        var accepted = new List<Vector2Int>(count);
        int placed = 0;

        foreach (var c in candidates)
        {
            if (_occupied.Contains(c) || !info.IsFree(c)) continue;

            bool tooClose = false;
            foreach (var a in accepted)
            {
                int dx = Mathf.Abs(a.x - c.x);
                int dy = Mathf.Abs(a.y - c.y);
                if (Mathf.Max(dx, dy) < rTiles) { tooClose = true; break; }
            }
            if (tooClose) continue;

            CreatePropGO(rule, c);
            _occupied.Add(c);
            info.Occupied.Add(c);
            accepted.Add(c);
            placed++;
            if (placed >= count) break;
        }
        return placed;
    }
    private static int WallDistanceMin(RoomInfo info, Vector2Int c)
    {
        var b = info.Bounds;
        int dl = c.x - b.xMin;
        int dr = (b.xMax - 1) - c.x;
        int db = c.y - b.yMin;
        int dt = (b.yMax - 1) - c.y;
        return Mathf.Min(Mathf.Min(dl, dr), Mathf.Min(db, dt));
    }
    private static int CornerScore(RoomInfo info, Vector2Int c)
    {
        var b = info.Bounds;
        int dx = Mathf.Min(Mathf.Abs(c.x - b.xMin), Mathf.Abs((b.xMax - 1) - c.x));
        int dy = Mathf.Min(Mathf.Abs(c.y - b.yMin), Mathf.Abs((b.yMax - 1) - c.y));
        return dx + dy;
    }

    private static bool IsChest(PropRules_SO.PropRule rule)
    {
        var k = rule.kind.ToString().ToLowerInvariant();
        return k.Contains("chest");
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; --i)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
    private static List<Vector2Int> RankByCenter(IEnumerable<Vector2Int> cells, Vector2Int target)
    {
        var ranked = new List<(Vector2Int c, int d)>();
        foreach (var c in cells)
            ranked.Add((c, Mathf.Abs(c.x - target.x) + Mathf.Abs(c.y - target.y)));

        ranked.Sort((a, b) => a.d.CompareTo(b.d));

        var result = new List<Vector2Int>(ranked.Count);
        foreach (var (c, _) in ranked) result.Add(c);
        return result;
    }

}