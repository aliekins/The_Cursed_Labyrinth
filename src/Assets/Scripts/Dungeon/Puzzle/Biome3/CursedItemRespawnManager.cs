using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * @file CursedItemRespawnManager.cs
 * @brief Tracks cursed pickups dropped in the world and periodically relocates them.
 * @ingroup Puzzle
 *
 * Automatically creates an instance on first registration.
 * Starts a timer that resets if the player stays near the pickup.
 * Relocates into valid room interiors using DungeonController/MapIndex.
 */
[DisallowMultipleComponent]
public sealed class CursedItemRespawnManager : MonoBehaviour
{
    #region config 
    [Header("Respawn rules")]
    [SerializeField, Min(1f)] private float respawnDelaySeconds = 5f;
    [SerializeField, Min(0f)] private float minDistanceFromPlayer = 4f;
    [SerializeField, Min(0)] private int maxRelocateTries = 60;

    private readonly Dictionary<PickupItem, Coroutine> timers = new();
    #endregion

    #region access
    public static CursedItemRespawnManager Instance { get; private set; }
    private static readonly List<PickupItem> pending = new();

    private void Awake()
    {
        Instance = this;
        if (pending.Count > 0)
        {
            foreach (var p in pending.ToArray())
                if (p)
                    Register(p);

            pending.Clear();
        }
    }

    public static void RegisterPickup(PickupItem pickup)
    {
        if (!pickup)
        {
            Debug.LogWarning("[CursedRespawn] RegisterPickup(null)");
            return;
        }

        if (Instance == null)
        {
            var go = new GameObject("CursedItemRespawnManager");
            DontDestroyOnLoad(go);
            var mgr = go.AddComponent<CursedItemRespawnManager>(); // Awake() sets Instance and drains 'pending'
        }

        Instance.Register(pickup);
    }

    public void Register(PickupItem pickup)
    {
        if (!pickup || !IsCursed(pickup))
        {
            Debug.LogWarning($"[CursedRespawn] Ignored registration: isSpecial={pickup && pickup.isSpecial}, Type={(pickup ? pickup.Type : 0)}");
            return;
        }

        if (timers.TryGetValue(pickup, out var c))
            StopCoroutine(c);

        timers[pickup] = StartCoroutine(RespawnAfterIdle(pickup));
        Debug.Log($"[CursedRespawn] Timer started for {pickup.Type} ({respawnDelaySeconds}s).");
    }


    public static bool ForceRelocateNow(PickupItem pickup)
    {
        if (!pickup) return false;
        if (Instance == null)
        {
            var go = new GameObject("CursedItemRespawnManager");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<CursedItemRespawnManager>(); // Awake sets Instance
        }
        return Instance.TryRelocate(pickup);
    }
    #endregion

    #region helpers
    private IEnumerator RespawnAfterIdle(PickupItem pickup)
    {
        float t = 0f;

        while (pickup && t < respawnDelaySeconds)
        {
            if (!pickup.gameObject.activeInHierarchy) yield break; // picked/destroyed

            var inv = FindFirstObjectByType<PlayerInventory>();
            if (inv && (inv.transform.position - pickup.transform.position).sqrMagnitude <= minDistanceFromPlayer * minDistanceFromPlayer)
                t = 0f;
            else
                t += Time.deltaTime;

            yield return null;
        }

        if (!pickup) yield break;

        if (TryRelocate(pickup))
        {
            // restart the timer so it won’t sit forever in another bad corner
            timers[pickup] = StartCoroutine(RespawnAfterIdle(pickup));
        }
        else
        {
            Debug.LogWarning("[CursedRespawn] Could not find a relocation spot.");
        }
    }

    private static bool IsCursed(PickupItem p)
    {
        var t = p.Type;
        return p.isSpecial && (t == Item.ItemType.SkullDiamond || t == Item.ItemType.HeartDiamond || t == Item.ItemType.Crown);
    }

    private bool TryRelocate(PickupItem p)
    {
        var dc = FindFirstObjectByType<DungeonController>();
        var viz = dc ? dc.Viz : null;
        var grid = dc ? dc.Grid : null;
        var map = dc ? dc.MapIndex : null;

        if (!viz || grid == null) return false;

        // Prefer indexed room interiors
        if (map != null && map.Rooms != null && map.Rooms.Count > 0)
        {
            var list = new List<DungeonMapIndex.RoomIndex>(map.Rooms.Values);
            for (int tries = 0; tries < maxRelocateTries; tries++)
            {
                var ri = list[Random.Range(0, list.Count)];
                if (ri == null || ri.Interior == null || ri.Interior.Count == 0) continue;

                if (!TryPickRandom(ri.Interior, out var cell)) continue;

                if (!grid.InBounds(cell.x, cell.y)) continue;
                var kind = grid.Kind[cell.x, cell.y] ?? "";
                if (kind == "wall") continue;

                MovePickupToGridLocal(p, viz, cell);
                Debug.Log($"[CursedRespawn] Relocated {p.Type} to room {ri.Id} at {cell.x},{cell.y}.");
                return true;
            }
        }

        // Fallback: sample random inner area from controller Rooms
        var rooms = dc ? dc.Rooms : null;
        if (rooms != null && rooms.Count > 0)
        {
            for (int tries = 0; tries < maxRelocateTries; tries++)
            {
                var rm = rooms[Random.Range(0, rooms.Count)];
                var b = rm.Bounds;
                int x = Random.Range(b.xMin + 1, b.xMax - 1);
                int y = Random.Range(b.yMin + 1, b.yMax - 1);
                if (!grid.InBounds(x, y)) continue;
                var kind = grid.Kind[x, y] ?? "";
                if (kind == "wall") continue;

                MovePickupToGridLocal(p, viz, new Vector2Int(x, y));
                Debug.Log($"[CursedRespawn] Relocated {p.Type} to {x},{y}.");
                return true;
            }
        }

        return false;
    }

    private static bool TryPickRandom(ICollection<Vector2Int> set, out Vector2Int value)
    {
        value = default;
        if (set == null || set.Count == 0) return false;

        int skip = Random.Range(0, set.Count);
        int i = 0;
        foreach (var v in set)
        {
            if (i++ == skip)
            {
                value = v;
                return true;
            }
        }
        // Fallback (shouldn’t happen)
        foreach (var v in set)
        {
            value = v;
            return true;
        }

        return false;
    }

    private static void MovePickupToGridLocal(PickupItem p, TilemapVisualizer viz, Vector2Int cell)
    {
        var parent = viz.GridTransform ? viz.GridTransform : null;

        if (parent)
            p.transform.SetParent(parent, false);

        p.transform.localPosition = viz.CellCenterLocal(cell.x, cell.y);
    }
    #endregion
}