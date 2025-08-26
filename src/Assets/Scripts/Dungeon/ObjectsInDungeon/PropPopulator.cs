using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PropPopulator : MonoBehaviour
{
    [Header("Required")]
    [SerializeField] private TilemapVisualizer visualizer;

    [Header("Global")]
    [SerializeField] public string corridorPrefix = "floor_corridor";
    [SerializeField, Range(0, 3)] private int doorAisleDepth = 1;       
    [SerializeField] private string sortingLayer = "Props";
    [SerializeField] private int sortingOrder = 4;
    [SerializeField] private bool fitToCell = true;

    [Header("Props")]
    public List<SimpleProp> cornerProps = new();   // placed on cells touching 2 room edges
    public List<SimpleProp> wallProps = new();   // placed on cells touching 1 room edge
    public List<SimpleProp> interiorProps = new(); // elsewhere

    [Serializable]
    public class SimpleProp
    {
        public Sprite sprite;
        [Range(0f, 1f)] public float chance = 0.85f;      // chance to try placing this rule in a room
        public int min = 1, max = 2;                      // how many to place (per room) if chosen
        [Tooltip("Chebyshev spacing (tiles) from any other placed prop.")]
        public int separation = 1;
    }

    // runtime caches (cleared on Populate)
    private readonly List<GameObject> spawned = new();
    private readonly HashSet<Vector2Int> trapCells = new();

    public void Clear()
    {
        foreach (var go in spawned) if (go) Destroy(go);
        spawned.Clear();
        trapCells.Clear();
    }

    public void Populate(DungeonGrid grid, List<Room> rooms, DungeonMapIndex index, bool[,] _ignoredCarpetMask, int globalSeed, IReadOnlyCollection<Vector2Int> traps = null)
    {
        Clear();

        if (!visualizer) 
            visualizer = FindAnyObjectByType<TilemapVisualizer>();

        if (!visualizer || grid == null || rooms == null || rooms.Count == 0 || index == null) return;

        if (traps != null) 
            foreach (var c in traps) 
                trapCells.Add(c);

        foreach (var room in rooms)
        {
            PopulateRoom(grid, room, index, globalSeed);
        }
    }

    #region population_perRoom

    private void PopulateRoom(DungeonGrid grid, Room room, DungeonMapIndex idx, int globalSeed)
    {
        var rng = MakeRng(globalSeed, room.Id);

        // Build blocked set: doors, an aisle inside the room from each door (traps if any)
        var blocked = BuildDoorAisles(idx, room, doorAisleDepth);
        foreach (var t in trapCells) 
            blocked.Add(t);

        // Build category candidate lists (corners, walls, interior)
        var corners = new List<Vector2Int>();
        var walls = new List<Vector2Int>();
        var interior = new List<Vector2Int>();

        ClassifyRoomCells(grid, room.Bounds, corners, walls, interior);

        // Place in priority order corners, walls, interior
        var occupied = new List<Vector2Int>(16);

        PlaceCategory(corners, cornerProps, grid, room, blocked, occupied, rng);
        PlaceCategory(walls, wallProps, grid, room, blocked, occupied, rng);

        SortByDistanceFrom(room.Center, interior, descending: true);
        PlaceCategory(interior, interiorProps, grid, room, blocked, occupied, rng);
    }

    // Classify by how many room edges the cell touches
    private void ClassifyRoomCells(DungeonGrid grid, RectInt b,
        List<Vector2Int> corners, List<Vector2Int> walls, List<Vector2Int> interior)
    {
        for (int x = b.xMin; x < b.xMax; x++)
        {
            for (int y = b.yMin; y < b.yMax; y++)
            {
                var k = grid.Kind[x, y];
                if (!IsRoomFloor(k)) continue;

                int touches = 0;
                if (x == b.xMin) touches++;
                if (x == b.xMax - 1) touches++;
                if (y == b.yMin) touches++;
                if (y == b.yMax - 1) touches++;

                var c = new Vector2Int(x, y);
                if (touches >= 2) corners.Add(c);
                else if (touches == 1) walls.Add(c);
                else interior.Add(c);
            }
        }
    }

    private HashSet<Vector2Int> BuildDoorAisles(DungeonMapIndex idx, Room room, int depth)
    {
        var blocked = new HashSet<Vector2Int>();

        if (depth <= 0) 
            return blocked;
        if (!idx.Rooms.TryGetValue(room.Id, out var ri) || ri == null)
            return blocked;

        foreach (var e in ri.Entrances)
        {
            blocked.Add(e);

            Vector2Int? corridor = null;
            foreach (var d in new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down })
            {
                var n = e + d;
                if (idx.CorridorCells.Contains(n)) { corridor = n; break; }
            }
            if (!corridor.HasValue) continue;

            var insideDir = e - corridor.Value;
            var cur = e;

            for (int i = 0; i < depth; i++)
            {
                cur += insideDir;

                if (!room.Bounds.Contains(cur)) break;

                blocked.Add(cur);
            }
        }
        return blocked;
    }

    private void PlaceCategory(List<Vector2Int> candidates, List<SimpleProp> rules, DungeonGrid grid, Room room, HashSet<Vector2Int> blocked, List<Vector2Int> occupied, System.Random rng)
    {
        if (rules == null || rules.Count == 0 || candidates == null || candidates.Count == 0) return;

        Shuffle(candidates, rng);

        foreach (var rule in rules)
        {
            if (!rule?.sprite) continue;
            if (rng.NextDouble() > rule.chance) continue;

            int toPlace = Mathf.Clamp(rng.Next(rule.min, rule.max + 1), 0, candidates.Count);
            if (toPlace <= 0) continue;

            for (int i = 0, tries = 0; i < candidates.Count && toPlace > 0 && tries < candidates.Count * 2; i++, tries++)
            {
                var c = candidates[i];

                if (!grid.InBounds(c.x, c.y)) continue;
                if (blocked.Contains(c)) continue;
                if (!IsRoomFloor(grid.Kind[c.x, c.y])) continue;

                // separation against everything already placed in this room
                if (rule.separation > 0)
                {
                    bool tooClose = false;
                    for (int j = 0; j < occupied.Count; j++)
                    {
                        if (Cheb(c, occupied[j]) < rule.separation) { tooClose = true; break; }
                    }
                    if (tooClose) continue;
                }

                Spawn(rule.sprite, c);
                occupied.Add(c);
                room.Info.Occupied.Add(c);
                toPlace--;
            }
        }
    }
    #endregion
    #region utilities

    private bool IsRoomFloor(string k)
    {
        if (string.IsNullOrEmpty(k)) return false;
        if (string.Equals(k, "wall", StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.IsNullOrEmpty(corridorPrefix) && k.StartsWith(corridorPrefix, StringComparison.OrdinalIgnoreCase)) return false;

        return true;
    }

    private void SortByDistanceFrom(Vector2Int pivot, List<Vector2Int> list, bool descending)
    {
        list.Sort((a, b) =>
        {
            int da = Mathf.Abs(a.x - pivot.x) + Mathf.Abs(a.y - pivot.y);
            int db = Mathf.Abs(b.x - pivot.x) + Mathf.Abs(b.y - pivot.y);

            return descending ? db.CompareTo(da) : da.CompareTo(db);
        });
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
            float cs = visualizer.CellSize;
            var s = sprite.bounds.size;

            if (s.x > 0.0001f && s.y > 0.0001f)
                go.transform.localScale = new Vector3(cs / s.x, cs / s.y, 1f);
        }
        spawned.Add(go);
    }

    private static int Cheb(Vector2Int a, Vector2Int b) => Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));

    private static void Shuffle<T>(IList<T> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; --i)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
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
    #endregion
}
