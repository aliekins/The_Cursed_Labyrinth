using UnityEngine;
using UnityEngine.Tilemaps;

/**
 * @file TilemapClearer.cs
 * @brief Clears Tilemaps under given roots; (optional) destroys tagged spawned objects.
 * @ingroup SeedingRT
 */

public class TilemapClearer : MonoBehaviour
{
    [Header("Roots to clean")]
    public Transform[] roots;

    [Header("Also destroy objects with this tag (optional)")]
    public string destroyTag = "DungeonSpawned";

    public void ClearAll()
    {
        if (roots != null)
        {
            foreach (var root in roots)
            {
                if (root == null) continue;
                var maps = root.GetComponentsInChildren<Tilemap>(true);
                foreach (var tm in maps)
                {
                    tm.ClearAllTiles();
                }
            }
        }

        if (!string.IsNullOrEmpty(destroyTag))
        {
            var spawned = GameObject.FindGameObjectsWithTag(destroyTag);
            foreach (var go in spawned)
            {
                Destroy(go);
            }
        }
    }
    public void ClearUnder(Transform root, bool alsoDestroyTagged = true, string destroyTag = "DungeonSpawned")
    {
        if (!root) return;

        var maps = root.GetComponentsInChildren<Tilemap>(true);
        for (int i = 0; i < maps.Length; i++)
            maps[i].ClearAllTiles();

        if (alsoDestroyTagged && !string.IsNullOrEmpty(destroyTag))
        {
            var all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                var tr = all[i];
                if (tr && tr.CompareTag(destroyTag))
                    Destroy(tr.gameObject);
            }
        }
    }
}