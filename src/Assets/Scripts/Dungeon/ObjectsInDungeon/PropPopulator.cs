using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PropPopulator : MonoBehaviour
{
    [Header("Required")]
    [SerializeField] private TilemapVisualizer visualizer;

    [Header("Avoid corridors / doors")]
    [SerializeField] public string corridorPrefix = "floor_corridor";
    [SerializeField, Range(0, 3)] private int doorAisleDepth = 1;
    [SerializeField, Range(0, 3)] private int wallDoorClearance = 1;
    [SerializeField, Range(0, 2)] private int doorwayChebBuffer = 1;

    [Header("Placement visuals")]
    [SerializeField] private bool fitToCell = true;

    [Header("Strategies (auto-find if null)")]
    [SerializeField] private CornerStrategy cornerStrategy;
    [SerializeField] private WallStrategy wallStrategy;
    [SerializeField] private InteriorStrategy interiorStrategy;

    [Serializable]
    public struct DropPolicy
    {
        public bool allowSwords;
        public int guaranteedSwords;
        public bool allowBooks;
        public int guaranteedBooks;
        public bool allowPotions;
        [Range(0f, 1f)] public float potionChance;
    }

    [Header("Drop Policy (assigned by controller per biome)")]
    [SerializeField] private DropPolicy dropPolicy;
    public void SetDropPolicy(DropPolicy policy) => dropPolicy = policy;

    // runtime caches
    private readonly List<GameObject> spawned = new();
    private readonly HashSet<Vector2Int> trapCells = new();
    private readonly List<BreakableProp> breakables = new();

    // counters
    private int swordsAssigned, booksAssigned;

    private const int _sortingOrder = 2; // above floor, below walls

    public sealed class PropDropTags : MonoBehaviour
    {
        public bool sword;
        public bool book;
        public bool potion;
    }

    public void Clear()
    {
        foreach (var go in spawned)
            if (go) 
                Destroy(go);

        spawned.Clear();
        trapCells.Clear();
        breakables.Clear();
        swordsAssigned = booksAssigned = 0;
    }

    public void Populate(DungeonGrid grid, List<Room> rooms, DungeonMapIndex index, bool[,] _ignoredCarpetMask, int globalSeed, IReadOnlyCollection<Vector2Int> traps = null)
    {
        Clear();

        if (!visualizer) visualizer = FindFirstObjectByType<TilemapVisualizer>();
        if (!visualizer || grid == null || rooms == null || rooms.Count == 0 || index == null) return;

        if (!cornerStrategy) cornerStrategy = GetComponent<CornerStrategy>();
        if (!wallStrategy) wallStrategy = GetComponent<WallStrategy>();
        if (!interiorStrategy) interiorStrategy = GetComponent<InteriorStrategy>();

        if (traps != null) foreach (var c in traps)
                trapCells.Add(c);

        foreach (var room in rooms)
            PopulateRoom(grid, room, index, globalSeed);

        // Assign hidden drops to a subset of placed/promotion breakables
        AssignDrops(grid, rooms, globalSeed);
    }

    #region populate_perRoom
    private void PopulateRoom(DungeonGrid grid, Room room, DungeonMapIndex idx, int globalSeed)
    {
        var rng = MakeRng(globalSeed, room.Id);

        var blocked = BuildDoorNoPropMask(grid, idx, room);

        foreach (var t in trapCells)
            blocked.Add(t);
        blocked.Add(room.Center);

        var corners = new List<Vector2Int>();
        var walls = new List<Vector2Int>();
        var interior = new List<Vector2Int>();
        ClassifyRoomCells(grid, room.Bounds, corners, walls, interior);

        var occupied = new HashSet<Vector2Int>();

        if (cornerStrategy && cornerStrategy.HasRules)
            PlaceByStrategy(cornerStrategy, corners, grid, room, blocked, occupied, rng);

        if (wallStrategy && wallStrategy.HasRules)
            PlaceByStrategy(wallStrategy, walls, grid, room, blocked, occupied, rng);

        if (interiorStrategy && interiorStrategy.HasRules)
            PlaceByStrategy(interiorStrategy, interior, grid, room, blocked, occupied, rng);
    }

    private void PlaceByStrategy(PropStrategyBase strategy, List<Vector2Int> candidates, DungeonGrid grid, Room room, HashSet<Vector2Int> blocked, HashSet<Vector2Int> occupied, System.Random rng)
    {
        if (candidates == null || candidates.Count == 0) return;

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
                var go = Spawn(rule, c, mods);
                occupied.Add(c);
                room.Info.Occupied.Add(c);

                // If the rule says it’s breakable, wire it now (drops assigned later)
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
    #region drops
    private void AssignDrops(DungeonGrid grid, List<Room> rooms, int seed)
    {
        var rng = MakeRng(seed, 13579);

        // Split breakables by tag
        var swordHolders = new List<BreakableProp>();
        var bookHolders = new List<BreakableProp>();
        var potionHolders = new List<BreakableProp>();

        foreach (var bp in breakables)
        {
            if (!bp) continue;
            var t = bp.GetComponent<PropDropTags>(); if (!t) continue;
            if (t.sword) swordHolders.Add(bp);
            if (t.book) bookHolders.Add(bp);
            if (t.potion) potionHolders.Add(bp);
        }

        // Promote more holders if not enough
        if (dropPolicy.allowSwords && swordHolders.Count < dropPolicy.guaranteedSwords)
            PromoteByTag(grid, rooms, rng, need: dropPolicy.guaranteedSwords - swordHolders.Count, wantSword: true, wantBook: false, wantPotion: false, out swordHolders);

        if (dropPolicy.allowBooks && bookHolders.Count < dropPolicy.guaranteedBooks)
            PromoteByTag(grid, rooms, rng, need: dropPolicy.guaranteedBooks - bookHolders.Count, wantSword: false, wantBook: true, wantPotion: false, out bookHolders);

        // Randomize lists
        Shuffle(swordHolders, rng);
        Shuffle(bookHolders, rng);
        Shuffle(potionHolders, rng);

        // don't double-assign to the same prop
        var used = new HashSet<BreakableProp>();

        // Swords 
        if (dropPolicy.allowSwords)
        {
            int need = Mathf.Max(0, dropPolicy.guaranteedSwords);
            for (int i = 0; i < swordHolders.Count && swordsAssigned < need; i++)
            {
                var bp = swordHolders[i]; if (!bp || used.Contains(bp)) continue;
                bp.ConfigureDrop(Item.ItemType.Sword, 1);
                used.Add(bp); 
                Debug.Log($"[PropPopulator] Assigned sword drop to {bp.name}, {bp.transform.position}", this);
                swordsAssigned++;
                Debug.Log($"[PropPopulator] Assigned sword drop count {swordsAssigned}", this);
            }
        }

        // Books
        if (dropPolicy.allowBooks)
        {
            int need = Mathf.Max(0, dropPolicy.guaranteedBooks);
            for (int i = 0; i < bookHolders.Count && booksAssigned < need; i++)
            {
                var bp = bookHolders[i]; if (!bp || used.Contains(bp)) continue;
                int r = rng.Next(1, 6);
                var t = (Item.ItemType)System.Enum.Parse(typeof(Item.ItemType), $"Book{r}");
                bp.ConfigureDrop(t, 1);
                used.Add(bp);
                Debug.Log($"[PropPopulator] Assigned book drop to {bp.name}, {bp.transform.position}", this);
                booksAssigned++;
                Debug.Log($"[PropPopulator] Assigned book drop count {booksAssigned}", this);
            }
        }

        // Potions (random chance on remaining potion-eligible breakables)
        if (dropPolicy.allowPotions && dropPolicy.potionChance > 0f)
        {
            for (int i = 0; i < potionHolders.Count; i++)
            {
                var bp = potionHolders[i]; if (!bp || used.Contains(bp)) continue;
                if (rng.NextDouble() < dropPolicy.potionChance)
                {
                    bp.ConfigureDrop(Item.ItemType.HealthPotion, 1);
                    used.Add(bp);
                    Debug.Log($"[PropPopulator] Assigned potion drop to {bp.name}", this);
                }
            }
        }
    }
    private void PromoteByTag(DungeonGrid grid, List<Room> rooms, System.Random rng, int need, bool wantSword, bool wantBook, bool wantPotion, out List<BreakableProp> holdersOut)
    {
        holdersOut = new List<BreakableProp>();
        var candidates = new List<GameObject>();

        foreach (var go in spawned)
        {
            if (!go) continue;
            if (go.GetComponent<BreakableProp>()) continue;

            var tag = go.GetComponent<PropDropTags>();
            if (!tag) continue;

            if ((wantSword && tag.sword) || (wantBook && tag.book) || (wantPotion && tag.potion))
            {
                if (go.GetComponent<Collider2D>() || go.GetComponentInChildren<Collider2D>())
                    candidates.Add(go);
            }
        }

        Shuffle(candidates, rng);

        for (int i = 0; i < candidates.Count && need > 0; i++)
        {
            var go = candidates[i];
            var bp = go.GetComponent<BreakableProp>() ?? go.AddComponent<BreakableProp>();
            
            bp.Configure(null, 1f, null, 0f);
            breakables.Add(bp);
            holdersOut.Add(bp);
            need--;
        }

        // Also include any already-existing breakables with the tag
        foreach (var bp in breakables)
        {
            if (!bp || holdersOut.Contains(bp)) continue;

            var t = bp.GetComponent<PropDropTags>(); 
            if (!t) continue;
            if ((wantSword && t.sword) || (wantBook && t.book) || (wantPotion && t.potion))
                holdersOut.Add(bp);
        }
    }
    #endregion
    #region spawn
    private GameObject Spawn(PropStrategyBase.SimpleProp rule, Vector2Int cell, PlacementMods mods)
    {
        var parent = visualizer.GridTransform ? visualizer.GridTransform : transform;
        GameObject go;

        if (rule.prefab)
        {
            go = Instantiate(rule.prefab, parent, false);
            go.transform.localPosition = visualizer.CellCenterLocal(cell.x, cell.y) + (Vector3)mods.offset;
            EnsureCollider(go, rule);
        }
        else
        {
            go = new GameObject($"prop_{cell.x}_{cell.y}");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = visualizer.CellCenterLocal(cell.x, cell.y) + (Vector3)mods.offset;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = rule.sprite;
            sr.sortingOrder = _sortingOrder; // above floor, below walls

            if (fitToCell && rule.sprite && mods.scaleToCell)
            {
                float cs = visualizer.CellSize;
                var s = rule.sprite.bounds.size;
                if (s.x > 0.0001f && s.y > 0.0001f)
                    go.transform.localScale = new Vector3(cs / s.x, cs / s.y, 1f);
            }
            EnsureCollider(go, rule);
        }

        var tag = go.GetComponent<PropDropTags>() ?? go.AddComponent<PropDropTags>();
        tag.sword = rule.holdsSwords;
        tag.book = rule.holdsBooks;
        tag.potion = rule.holdsPotions;

        spawned.Add(go);
        return go;
    }

    private void EnsureCollider(GameObject go, PropStrategyBase.SimpleProp rule)
    {
        if (rule.colliderMode == PropStrategyBase.SimpleProp.ColliderMode.None) return;
        if (go.GetComponent<Collider2D>()) return; // root only

        float cell = visualizer.CellSize;
        Vector2 size = new Vector2(cell * rule.colliderSizeScale.x, cell * rule.colliderSizeScale.y);
        Vector2 offset = rule.colliderOffset;

        switch (rule.colliderMode)
        {
            case PropStrategyBase.SimpleProp.ColliderMode.Box:
                var bc = go.AddComponent<BoxCollider2D>(); bc.isTrigger = rule.colliderIsTrigger; bc.size = size; bc.offset = offset; break;
            case PropStrategyBase.SimpleProp.ColliderMode.Circle:
                var cc = go.AddComponent<CircleCollider2D>(); cc.isTrigger = rule.colliderIsTrigger; cc.radius = Mathf.Min(size.x, size.y) * 0.5f; cc.offset = offset; break;
            case PropStrategyBase.SimpleProp.ColliderMode.Capsule:
                var cap = go.AddComponent<CapsuleCollider2D>(); cap.isTrigger = rule.colliderIsTrigger; cap.size = size;
                cap.direction = (size.x >= size.y) ? CapsuleDirection2D.Horizontal : CapsuleDirection2D.Vertical; cap.offset = offset; break;
        }
    }

    private void ClassifyRoomCells(DungeonGrid grid, RectInt b, List<Vector2Int> corners, List<Vector2Int> walls, List<Vector2Int> interior)
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

    private HashSet<Vector2Int> BuildDoorNoPropMask(DungeonGrid grid, DungeonMapIndex idx, Room room)
    {
        var blocked = new HashSet<Vector2Int>();
        var entrances = (idx.Rooms.TryGetValue(room.Id, out var ri) && ri?.Entrances != null)
            ? new List<Vector2Int>(ri.Entrances)
            : new List<Vector2Int>();

        if (entrances.Count == 0)
        {
            var b = room.Bounds;
            void TryAdd(int x, int y)
            {
                var c = new Vector2Int(x, y);
                if (!IsRoomFloor(grid.Kind[x, y])) return;
                foreach (var d in new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down })
                {
                    var n = c + d;
                    if (!grid.InBounds(n.x, n.y)) continue;
                    var kind = grid.Kind[n.x, n.y] ?? "";
                    if (!string.IsNullOrEmpty(corridorPrefix) && kind.StartsWith(corridorPrefix, StringComparison.OrdinalIgnoreCase))
                    { entrances.Add(c); break; }
                }
            }
            var bnd = room.Bounds;
            for (int x = bnd.xMin; x < bnd.xMax; x++) 
            {
                TryAdd(x, bnd.yMax - 1);
                TryAdd(x, bnd.yMin); 
            }
            for (int y = bnd.yMin; y < bnd.yMax; y++) 
            { 
                TryAdd(bnd.xMin, y);
                TryAdd(bnd.xMax - 1, y);
            }
        }

        if (entrances.Count == 0) return blocked;

        RectInt bounds = room.Bounds;
        foreach (var e in entrances)
        {
            if (bounds.Contains(e)) blocked.Add(e);

            Vector2Int? corridor = null;
            foreach (var d in new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down })
            {
                var n = e + d;
                if (grid.InBounds(n.x, n.y))
                {
                    var k = grid.Kind[n.x, n.y] ?? "";
                    if (!string.IsNullOrEmpty(corridorPrefix) && k.StartsWith(corridorPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        corridor = n;
                        break;
                    }
                }
            }

            if (corridor.HasValue && doorAisleDepth > 0)
            {
                var insideDir = e - corridor.Value;
                var cur = e;
                for (int i = 0; i < doorAisleDepth; i++)
                {
                    cur += insideDir;
                    if (!bounds.Contains(cur)) break;
                    blocked.Add(cur);
                }
            }

            if (wallDoorClearance > 0)
            {
                bool horizontalWall = (e.y == bounds.yMax - 1 || e.y == bounds.yMin);
                Vector2Int t1 = horizontalWall ? Vector2Int.left : Vector2Int.up;
                Vector2Int t2 = horizontalWall ? Vector2Int.right : Vector2Int.down;
                for (int s = 1; s <= wallDoorClearance; s++)
                {
                    var p1 = e + t1 * s;
                    if (bounds.Contains(p1))
                        blocked.Add(p1);

                    var p2 = e + t2 * s;
                    if (bounds.Contains(p2))
                        blocked.Add(p2);
                }
            }

            for (int dx = -doorwayChebBuffer; dx <= doorwayChebBuffer; dx++)
            {
                for (int dy = -doorwayChebBuffer; dy <= doorwayChebBuffer; dy++)
                {
                    var p = new Vector2Int(e.x + dx, e.y + dy);
                    if (!bounds.Contains(p)) continue;

                    var k = grid.Kind[p.x, p.y] ?? "";
                    if (IsRoomFloor(k)) blocked.Add(p);
                }
            }
        }
        return blocked;
    }
    #endregion
    #region utils
    private static bool TooClose(Vector2Int p, HashSet<Vector2Int> used, int sep)
    {
        foreach (var u in used) 
            if (Mathf.Max(Mathf.Abs(p.x - u.x), Mathf.Abs(p.y - u.y)) < sep) 
                return true;

        return false;
    }

    private bool IsRoomFloor(string k)
    {
        if (string.IsNullOrEmpty(k)) return false;
        if (string.Equals(k, "wall", StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.IsNullOrEmpty(corridorPrefix) && k.StartsWith(corridorPrefix, StringComparison.OrdinalIgnoreCase)) return false;

        return true;
    }

    private static System.Random MakeRng(int baseSeed, int salt)
    {
        unchecked { 
            int h = 17;
            h = h * 31 + baseSeed;
            h = h * 31 + salt;
            
            return new System.Random(h);
        }
    }

    private static void Shuffle<T>(IList<T> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
    #endregion
}