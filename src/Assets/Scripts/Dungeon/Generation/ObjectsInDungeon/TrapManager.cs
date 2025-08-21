/// \file TrapManager.cs
/// \brief Plans & spawns traps in eligible rooms/tiles (Quarry biome)
using System.Collections.Generic;
using UnityEngine;

public sealed class TrapManager : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject spikeTrapPrefab; 

    [Header("Placement")]
    [SerializeField, Range(0f, 1f)] private float roomTrapChance = 0.65f; 
    [SerializeField, Min(1)] private int trapsPerRoomMin = 1;
    [SerializeField, Min(1)] private int trapsPerRoomMax = 3;

    [SerializeField] private List<string> forbiddenSurfaceKinds = new() { "floor_carpet" };

    /// \brief If true, never place traps on corridor tiles
    [SerializeField] private bool disallowCorridors = true;

    /// \brief Build traps for current dungeon; only Quarry gets traps
    /// \param carpetMask Optional mask where true = carpet; if provided, traps won't spawn there.
    public void Build(DungeonGrid grid, List<Room> rooms, System.Func<string, int> resolveTier, bool[,] carpetMask = null)
    {
        if (!spikeTrapPrefab || grid == null || rooms == null) return;

        ClearChildren();

        for (int i = 0; i < rooms.Count; i++)
        {
            var center = rooms[i].Center;
            string kind = grid.Kind[center.x, center.y];
            int tier = resolveTier(kind);

            if (tier != 1) continue;

            if (Random.value > roomTrapChance) continue;
            int count = Random.Range(trapsPerRoomMin, trapsPerRoomMax + 1);

            var rect = rooms[i].Bounds;
            int placed = 0, guard = 0;

            while (placed < count && guard++ < 200)
            {
                var pos = new Vector2Int(
                    Random.Range(rect.xMin + 1, rect.xMax - 1),
                    Random.Range(rect.yMin + 1, rect.yMax - 1)
                );
                if (!grid.InBounds(pos.x, pos.y)) continue;

                if (carpetMask != null && carpetMask[pos.x, pos.y]) continue;

                var cellKind = grid.Kind[pos.x, pos.y] ?? string.Empty;
                if (!CanPlaceOn(cellKind)) continue;

                var go = Instantiate(spikeTrapPrefab, transform);
                go.transform.position = GridToWorld(pos);
                placed++;
            }
        }
    }

    private bool CanPlaceOn(string cellKind)
    {
        if (string.IsNullOrEmpty(cellKind)) return false;

        // Forbid corridors 
        if (disallowCorridors && cellKind.StartsWith("floor_corridor", System.StringComparison.OrdinalIgnoreCase))
            return false;

        // Forbid any explicitly disallowed kind
        foreach (var k in forbiddenSurfaceKinds)
        {
            if (!string.IsNullOrEmpty(k) && cellKind.StartsWith(k, System.StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    private void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
    }
    private Vector3 GridToWorld(Vector2Int cell) => new Vector3(cell.x, cell.y, 0);
}