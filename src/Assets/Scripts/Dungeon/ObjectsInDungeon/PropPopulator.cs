using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PropPopulator : MonoBehaviour
{
    #region helperStructs
    public struct DropPolicy
    {
        public bool allowPotions;
        [Range(0f, 1f)] public float potionChance;

        public bool allowSwords;
        public int guaranteedSwords;

        public bool allowBooks;
        public int guaranteedBooks;

        public bool allowCursed;     // biome 3 (skull/heart/crown)
        public int guaranteedCursed; // should be 3
    }
    #endregion

    #region config
    [Header("Required")]
    [SerializeField] private TilemapVisualizer visualizer;

    [Header("Avoid corridors / doors")]
    [SerializeField] public string corridorPrefix = "floor_corridor";
    [SerializeField, Range(0, 3)] private int doorAisleDepth = 1;
    [SerializeField, Range(0, 3)] private int wallDoorClearance = 1;
    [SerializeField, Range(0, 2)] private int doorwayChebBuffer = 1;

    [Header("Placement visuals")]
    [SerializeField] private bool fitToCell = true;

    [Header("Strategies (auto-found if null)")]
    [SerializeField] private CornerStrategy cornerStrategy;
    [SerializeField] private WallStrategy wallStrategy;
    [SerializeField] private InteriorStrategy interiorStrategy;

    [Header("Drop Policy (assigned by controller per biome)")]
    [SerializeField] private DropPolicy dropPolicy;
    public void SetDropPolicy(DropPolicy policy) => dropPolicy = policy;

    // runtime caches
    private readonly List<GameObject> spawned = new();
    private readonly HashSet<Vector2Int> trapCells = new();
    private readonly List<BreakableProp> breakables = new();
    #endregion

    #region lifecycle
    public DropPolicy GetDropPolicy() => dropPolicy;
    public void Clear()
    {
        foreach (var go in spawned) 
            if (go)
                Destroy(go);

        spawned.Clear();
        trapCells.Clear();
        breakables.Clear();
    }

    public void Populate(DungeonGrid grid, List<Room> rooms, DungeonMapIndex index, bool[,] _unusedCarpetMask, int globalSeed, IReadOnlyCollection<Vector2Int> traps = null)
    {
        Clear();

        if (!visualizer)
            visualizer = FindFirstObjectByType<TilemapVisualizer>();

        if (!visualizer || grid == null || rooms == null || rooms.Count == 0 || index == null) return;

        if (!cornerStrategy) cornerStrategy = GetComponent<CornerStrategy>();
        if (!wallStrategy) wallStrategy = GetComponent<WallStrategy>();
        if (!interiorStrategy) interiorStrategy = GetComponent<InteriorStrategy>();

        if (traps != null)
            foreach (var c in traps)
                trapCells.Add(c);

        foreach (var room in rooms)
            PopulateRoom(grid, room, index, globalSeed);

        // Drops
        var rng = SaltedRng(globalSeed, 13579);
        DropAssigner.Assign(GetDropPolicy(),visualizer, grid, rooms, spawned, breakables, rng);
    }
    #endregion

    #region perRoom_population
    private void PopulateRoom(DungeonGrid grid, Room room, DungeonMapIndex idx, int globalSeed)
    {
        var rng = SaltedRng(globalSeed, room.Id);

        var blocked = DoorMaskBuilder.BuildDoorNoPropMask(grid, idx, room, corridorPrefix, doorAisleDepth, wallDoorClearance, doorwayChebBuffer, IsRoomFloor);

        foreach (var t in trapCells) 
            blocked.Add(t);

        blocked.Add(room.Center);

        var corners = new List<Vector2Int>();
        var walls = new List<Vector2Int>();
        var interior = new List<Vector2Int>();

        RoomClassifier.ClassifyRoomCells(grid, room.Bounds, corners, walls, interior, IsRoomFloor);

        var occupied = new HashSet<Vector2Int>();

        PlaceByStrategy(cornerStrategy, corners, grid, room, blocked, occupied, rng);
        PlaceByStrategy(wallStrategy, walls, grid, room, blocked, occupied, rng);
        PlaceByStrategy(interiorStrategy, interior, grid, room, blocked, occupied, rng);
    }

    private void PlaceByStrategy(PropStrategyBase strategy, List<Vector2Int> candidates, DungeonGrid grid, Room room, HashSet<Vector2Int> blocked, HashSet<Vector2Int> occupied, System.Random rng)
    {
        if (!strategy || !strategy.HasRules || candidates == null || candidates.Count == 0) return;

        foreach (var rule in strategy.Rules)
        {
            if (rule == null || (rule.sprite == null && rule.prefab == null)) continue;
            if (rng.NextDouble() > rule.chance) continue;

            var perRule = strategy.FilterCandidatesForRule(room, candidates, rule);
            if (perRule == null || perRule.Count == 0) continue;

            int want = Mathf.Clamp(rng.Next(rule.min, rule.max + 1), 0, perRule.Count);
            if (want <= 0) continue;

            int tries = 0;
            for (int i = 0; i < perRule.Count && want > 0 && tries < perRule.Count * 2; i++, tries++)
            {
                var c = perRule[i];

                if (!grid.InBounds(c.x, c.y)) continue;
                if (!IsRoomFloor(grid.Kind[c.x, c.y])) continue;
                if (blocked.Contains(c)) continue;
                if (occupied.Contains(c)) continue;
                if (rule.separation > 0 && TooClose(c, occupied, rule.separation)) continue;

                var mods = strategy.GetPlacementMods(c, room);
                var go = PropSpawner.Spawn(visualizer, rule, c, mods, fitToCell);

                occupied.Add(c);
                room.Info.Occupied.Add(c);
                spawned.Add(go);

                if (go && rule.breakable)
                {
                    var bp = go.GetComponent<BreakableProp>() ?? go.AddComponent<BreakableProp>();

                    bp.Configure(rule.breakSfx, rule.breakSfxVolume, rule.breakVfxPrefab);
                    breakables.Add(bp);
                }

                want--;
            }
        }
    }
    #endregion

    #region utils
    private bool IsRoomFloor(string k)
    {
        if (string.IsNullOrEmpty(k)) return false;
        if (string.Equals(k, "wall", StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.IsNullOrEmpty(corridorPrefix) && k.StartsWith(corridorPrefix, StringComparison.OrdinalIgnoreCase)) return false;

        return true;
    }

    private static bool TooClose(Vector2Int p, HashSet<Vector2Int> used, int sep)
    {
        foreach (var u in used)
            if (Mathf.Max(Mathf.Abs(p.x - u.x), Mathf.Abs(p.y - u.y)) < sep)
                return true;

        return false;
    }

    private static System.Random SaltedRng(int baseSeed, int salt)
    {
        unchecked
        {
            int h = 17;
            h = h * 31 + baseSeed;
            h = h * 31 + salt;

            return new System.Random(h);
        }
    }
    #endregion
}