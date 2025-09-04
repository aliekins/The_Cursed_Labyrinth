/// @file TrapManager.cs
/// @brief Places spike traps in eligible rooms for a given biome tier.
/// @ingroup Objects

using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// @class TrapManager
/// @brief Spawns and positions traps per room using filters and counts.
public sealed class TrapManager : MonoBehaviour
{
    #region config
    [Header("Prefabs")]
    [SerializeField] private GameObject spikeTrapPrefab;

    [Header("Rooms / Count")]
    [SerializeField, Range(0f, 1f)] private float roomTrapChance = 0.65f;
    [SerializeField, Min(1)] private int trapsPerRoomMin = 1;
    [SerializeField, Min(1)] private int trapsPerRoomMax = 3;

    [Header("Filters")]
    [SerializeField] private List<string> forbiddenSurfaceKinds = new() { "floor_carpet" };
    [SerializeField] private bool disallowCorridors = true;

    private Transform gridTransform;              // set by controller
    private TilemapVisualizer viz;               // set by controller
    #endregion

    #region API
    public void SetGridContext(Transform gr, TilemapVisualizer visualizer)
    {
        gridTransform = gr;
        viz = visualizer;
    }

    /**
     * @brief Build traps for the current dungeon build.
     * @param grid Source grid
     * @param rooms Rooms to consider for placement
     * @param resolveTier Maps a floor kind string to a biome tier index
     * @param carpetMask Optional mask that excludes cells from placement
     *
     * Only rooms in the desired tier are considered. Within a room,
     * candidates are chosen from edge/interior cells that pass @ref CanPlaceOn.
     */
    public void Build(DungeonGrid grid, List<Room> rooms, Func<string, int> resolveTier, bool[,] carpetMask = null)
    {
        if (!spikeTrapPrefab || grid == null || rooms == null) return;

        ClearChildren();

        foreach (var room in rooms)
        {
            var centerKind = grid.Kind[room.Center.x, room.Center.y] ?? string.Empty;
            int tier = resolveTier(centerKind);

            // only biome 2 (tier==1) gets traps
            if (tier != 1) continue;

            if (UnityEngine.Random.value > roomTrapChance) continue;

            int target = UnityEngine.Random.Range(trapsPerRoomMin, trapsPerRoomMax + 1);
            var info = room.Info;

            int placed = 0, guard = 0;
            while (placed < target && guard++ < 400)
            {
                var cell = PickCandidate(info.EdgeBand, info, grid, carpetMask)
                           ?? PickCandidate(info.Interior, info, grid, carpetMask);
                if (cell == null) break;

                var pos = cell.Value;

                var go = Instantiate(spikeTrapPrefab, transform);

                if (viz != null)
                    go.transform.position = viz.CellCenterWorld(pos.x, pos.y);
                else
                    go.transform.position = new Vector3(pos.x + 0.5f, pos.y + 0.5f, 0f);

                info.Occupied.Add(pos);
                placed++;
            }
        }
    }
    #endregion

    #region helpers
    private Vector2Int? PickCandidate(List<Vector2Int> pool, RoomInfo info, DungeonGrid grid, bool[,] carpetMask)
    {
        if (pool == null || pool.Count == 0) return null;

        for (int tries = 0; tries < 32; tries++)
        {
            var p = pool[Random.Range(0, pool.Count)];

            if (info.Occupied.Contains(p)) continue;
            if (info.Entrances.Contains(p)) continue; 
            if (!grid.InBounds(p.x, p.y)) continue;

            var k = grid.Kind[p.x, p.y] ?? string.Empty;

            if (!CanPlaceOn(k)) continue;
            if (carpetMask != null && carpetMask[p.x, p.y]) continue;

            return p;
        }
        return null;
    }

    private bool CanPlaceOn(string cellKind)
    {
        if (string.IsNullOrEmpty(cellKind)) return false;
        if (disallowCorridors && cellKind.StartsWith("floor_corridor", System.StringComparison.OrdinalIgnoreCase))
            return false;

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
    #endregion
}