/// \file RuleDrivenVisualizer_SO.cs
/// \brief Visualizes a DungeonGrid by instantiating SpriteRenderers based on ScriptableObject tile rules.
using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class RuleDrivenVisualizer_SO
{
    private readonly Transform groundRoot;
    private readonly Transform wallsRoot;
    private readonly float cellSize;
    private readonly TileRuleDatabase ruleDatabase;

    /// <summary>
    /// Create a new visualizer that draws under <paramref name="parent"/>
    /// </summary>
    /// <param name="parent">Transform - holds two child containers: GroundRoot and WallsRoot</param>
    /// <param name="database">Database mapping kinds to rule assets</param>
    /// <param name="cellSize"></param>
    public RuleDrivenVisualizer_SO(Transform parent, TileRuleDatabase database, float cellSize = 1f)
    {
        this.ruleDatabase = database;
        this.cellSize = cellSize;

        groundRoot = FindOrCreate(parent, "GroundRoot");
        wallsRoot = FindOrCreate(parent, "WallsRoot");
    }

    /// <summary>
    /// Remove all previously spawned tiles under GroundRoot and WallsRoot
    /// </summary>
    public void Clear()
    {
        static void Nuke(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var child = t.GetChild(i).gameObject;
                if (Application.isPlaying) UnityEngine.Object.Destroy(child);
                else UnityEngine.Object.DestroyImmediate(child);
            }
        }

        Nuke(groundRoot);
        Nuke(wallsRoot);
    }

    /// <summary>
    /// Render the given <paramref name="grid"/> by instantiating sprites according to rules in the database.
    /// </summary>
    public void Render(DungeonGrid grid)
    {
        if (ruleDatabase == null)
        {
            Debug.LogError("RuleDrivenVisualizer_SO: TileRuleDatabase is not assigned.");
            return;
        }

        int width = grid.Width;
        int height = grid.Height;

        // Cache kind
        Dictionary<string, TileRuleAsset> ruleCache = BuildRuleCache(ruleDatabase);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                string kind = grid.Kind[x, y];

                if (string.Equals(kind, "wall", StringComparison.OrdinalIgnoreCase) && !HasAdjacentFloor(grid, x, y))
                {
                    continue; // interior wall - don't render
                }

                if (!TryResolveRule(ruleCache, kind, out TileRuleAsset rule))
                    continue;

                if (!TryPickSprite(rule, x, y, out Sprite sprite))
                    continue;

                Transform parent = IsFloorKind(kind) ? groundRoot : wallsRoot;
                SpawnTile(kind, x, y, rule, sprite, parent);
            }
        }
    }

    #region Helpers
    private static bool IsFloorKind(string kind) => kind != null && kind.StartsWith("floor", StringComparison.Ordinal);

    private static Dictionary<string, TileRuleAsset> BuildRuleCache(TileRuleDatabase database)
    {
        var cache = new Dictionary<string, TileRuleAsset>(
            capacity: database?.entries?.Count ?? 0,
            comparer: StringComparer.OrdinalIgnoreCase);

        if (database?.entries == null) return cache;

        foreach (var entry in database.entries)
        {
            if (entry == null || entry.rule == null) continue;
            if (string.IsNullOrWhiteSpace(entry.kind)) continue;

            // Last one chosen if there are duplicates
            cache[entry.kind] = entry.rule;
        }

        return cache;
    }
    private static bool TryResolveRule(Dictionary<string, TileRuleAsset> cache, string kind, out TileRuleAsset rule)
    {
        if (!cache.TryGetValue(kind, out rule) || rule == null)
        {
            Debug.LogError($"RuleDrivenVisualizer_SO: Missing rule for kind '{kind}'.");
            return false;
        }
        return true;
    }

    private static bool TryPickSprite(TileRuleAsset rule, int x, int y, out Sprite sprite)
    {
        sprite = null;

        Sprite[] sprites = rule.sprites;
        if (sprites == null || sprites.Length == 0)
            return false;

        switch (rule.pick)
        {
            case SpritePickMode.First:
                sprite = sprites[0];
                return sprite != null;

            case SpritePickMode.Random:
                sprite = sprites[UnityEngine.Random.Range(0, sprites.Length)];
                return sprite != null;

            case SpritePickMode.Hash:
            default:
                unchecked
                {
                    int h = (x * 73856093) ^ (y * 19349663);
                    if (h < 0) h = ~h;
                    sprite = sprites[h % sprites.Length];
                    return sprite != null;
                }
        }
    }

    /// <summary>
    /// Instantiate and configure a single tile GameObject
    /// </summary>
    private void SpawnTile(string kind, int x, int y, TileRuleAsset rule, Sprite sprite, Transform parent)
    {
        var go = new GameObject($"{kind}_{x}_{y}");
        var t = go.transform;

        t.SetParent(parent, false);

        t.localPosition = new Vector3(x * cellSize, y * cellSize, rule.z) + (Vector3)rule.offset;
        t.localEulerAngles = new Vector3(0f, 0f, rule.rotation);
        t.localScale = Vector3.one * Mathf.Max(0.0001f, rule.scale);

        // Renderer
        var spriteRenderer = go.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;

        ApplyCollider(go, rule);
    }

    /// <summary>
    /// Attach a 2D collider to a tile GameObject according to the rule (if any)
    /// </summary>
    private static void ApplyCollider(GameObject go, TileRuleAsset rule)
    {
        var cfg = rule.collider;
        if (cfg == null || cfg.type == ColliderKind.None) return;

        switch (cfg.type)
        {
            case ColliderKind.Box:
                {
                    var collider = go.AddComponent<BoxCollider2D>();
                    collider.size = cfg.size;
                    collider.isTrigger = cfg.isTrigger;
                    break;
                }
            case ColliderKind.Circle:
                {
                    var collider = go.AddComponent<CircleCollider2D>();
                    // Interpret size.x as diameter; clamp to avoid zero radius.
                    collider.radius = Mathf.Max(0.0001f, cfg.size.x) * 0.5f;
                    collider.isTrigger = cfg.isTrigger;
                    break;
                }
            case ColliderKind.Capsule:
                {
                    var collider = go.AddComponent<CapsuleCollider2D>();
                    collider.size = cfg.size;
                    collider.isTrigger = cfg.isTrigger;
                    break;
                }
        }
    }

    /// <summary>
    /// Find a child transform by name under <paramref name="parent"/> or create it
    /// </summary>
    private static Transform FindOrCreate(Transform parent, string name)
    {
        var existing = parent.Find(name);
        if (existing) { return existing; }

        var container = new GameObject(name);
        container.transform.SetParent(parent, false);
        return container.transform;
    }

    private static readonly Vector2Int[] Neigh8 = {
        new(1,0), new(-1,0), new(0,1), new(0,-1),
        new(1,1), new(1,-1), new(-1,1), new(-1,-1)
    };

    private static bool HasAdjacentFloor(DungeonGrid grid, int x, int y)
    {
        // Is any 8-neighbour a floor_* kind?
        foreach (var d in Neigh8)
        {
            int nx = x + d.x, ny = y + d.y;

            if (nx < 0 || ny < 0 || nx >= grid.Width || ny >= grid.Height) continue;
            if (IsFloorKind(grid.Kind[nx, ny])) { return true; }
        }
        return false;
    }
}

#endregion