using UnityEngine;
using UnityEngine.Tilemaps;

/// \brief Clears all Tilemaps under this object; also destroys leftover spawned props/traps/enemies tagged by a marker
public class TilemapClearer : MonoBehaviour
{
    [Header("Roots to clean (e.g. the prefab instance parent)")]
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
}