using System;
using UnityEngine;
using UnityEngine.Tilemaps;

/**
 * @file SpecialRoomSeeder.cs
 * @brief Instantiates the special room prefab and exposes its tilemaps/bounds for generation.
 * @ingroup SeedingRT
 */

public class SpecialRoomSeeder : MonoBehaviour
{
    #region config
    /**
     * @struct SeedInfo
     * @brief Data returned after seeding: tilemaps, occupied rect and (optional) broadcaster.
     */
    [Serializable]
    public class SeedInfo
    {
        public Transform root;                             // instance root (for convenience)
        public Tilemap ground;                             // prefab Ground tilemap
        public Tilemap carpet;                             // prefab Carpet tilemap
        public Tilemap wall;                               // prefab Wall tilemap
        public RectInt occupiedRectInt;                    // union of ground+wall tile bounds (TILE coords)
        public SpecialRoomCompleteBroadcaster broadcaster; // optional
    }

    [Header("Instancing")]
    [SerializeField] private Transform parent;
    [SerializeField] private string spawnedTag = "DungeonSpawned";

    private GameObject currentInstance;
    #endregion

    #region (un)seeed
    /// \brief Instantiate the special room prefab and return its tilemaps/bounds as SeedInfo
    public SeedInfo Seed(GameObject prefab)
    {
        if (!prefab) { Debug.LogError("[Seeder] No prefab provided."); return null; }

        Unseed(); // remove any previous instance

        currentInstance = Instantiate(prefab, parent ? parent : transform);
        currentInstance.transform.localPosition = Vector3.zero;
        currentInstance.name = $"{prefab.name}_Seed";

        if (!string.IsNullOrEmpty(spawnedTag))
            TagRecursively(currentInstance, spawnedTag);

        // Find tilemaps (expect names that contain "Ground", "Carpet", "Wall")
        var maps = currentInstance.GetComponentsInChildren<Tilemap>(true);
        Tilemap ground = null, carpet = null, wall = null;

        foreach (var tm in maps)
        {
            var n = tm.name.ToLowerInvariant();
            if (ground == null && n.Contains("ground")) ground = tm;
            else if (carpet == null && n.Contains("carpet")) carpet = tm;
            else if (wall == null && n.Contains("wall")) wall = tm;
        }

        if (!ground || !wall)
        {
            Debug.LogError("[Seeder] Prefab is missing required Ground/Wall tilemaps. " +
                           "Make sure child tilemaps are named with 'Ground', 'Carpet', 'Wall'.");
            return null;
        }

        // Compute occupied rect in TILE coordinates (union of ground + wall bounds)
        var occ = UnionBounds(ground.cellBounds, wall.cellBounds);

        var info = new SeedInfo
        {
            root = currentInstance.transform,
            ground = ground,
            carpet = carpet,
            wall = wall,
            occupiedRectInt = new RectInt(occ.xMin, occ.yMin, occ.size.x, occ.size.y),
            broadcaster = currentInstance.GetComponentInChildren<SpecialRoomCompleteBroadcaster>(true)
        };

        return info;
    }

    /// \brief Destroy current instance (used between biomes/rebuilds)
    public void Unseed()
    {
        if (!currentInstance) return;
        if (Application.isPlaying) Destroy(currentInstance);
        else DestroyImmediate(currentInstance);
        currentInstance = null;
    }
    #endregion

    #region helpers
    private static void TagRecursively(GameObject go, string tag)
    {
        void Recurse(Transform t)
        {
            var g = t.gameObject;
            g.tag = tag;
            for (int i = 0; i < t.childCount; i++) Recurse(t.GetChild(i));
        }
        Recurse(go.transform);
    }

    private static BoundsInt UnionBounds(BoundsInt a, BoundsInt b)
    {
        int xmin = Math.Min(a.xMin, b.xMin);
        int ymin = Math.Min(a.yMin, b.yMin);
        int xmax = Math.Max(a.xMax, b.xMax);
        int ymax = Math.Max(a.yMax, b.yMax);
        return new BoundsInt(xmin, ymin, 0, xmax - xmin, ymax - ymin, 1);
    }
    #endregion
}
