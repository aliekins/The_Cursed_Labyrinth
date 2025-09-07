using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * @file CursedItemRespawnManager.cs
 * @brief One-shot relocation per registration for cursed pickups.
 * @ingroup Puzzle
 */
[DisallowMultipleComponent]
public sealed class CursedItemRespawnManager : MonoBehaviour
{
    #region Config
    [Header("Respawn rules")]
    [SerializeField, Min(0.25f)] private float respawnDelaySeconds = 0f;
    [SerializeField, Min(0f)] private float minDistanceFromPlayer = 8f;
    [SerializeField, Min(1)] private int maxRelocateTries = 60;
    #endregion

    #region State
    private readonly Dictionary<PickupItem, Coroutine> timers = new();
    private readonly Dictionary<PickupItem, int> generations = new();
    #endregion

    #region Singleton
    public static CursedItemRespawnManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance && Instance != this) 
        { 
            Destroy(gameObject);
            return; 
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void RegisterPickup(PickupItem pickup)
    {
        if (!pickup)
        {
            Debug.LogWarning("[CursedRespawn] RegisterPickup(null)");
            return;
        }
        if (!Instance)
        {
            var go = new GameObject("CursedItemRespawnManager");

            DontDestroyOnLoad(go);
            go.AddComponent<CursedItemRespawnManager>();
        }
        Instance.Register(pickup);
    }
    #endregion

    #region API
    public void Register(PickupItem pickup)
    {
        if (!pickup) return;
        if (!IsCursed(pickup)) return;

        if (timers.TryGetValue(pickup, out var co) && co != null)
            StopCoroutine(co);

        int next = generations.TryGetValue(pickup, out var g) ? g + 1 : 1;
        generations[pickup] = next;

        timers[pickup] = StartCoroutine(RespawnAfterIdle(pickup, next));
    }

    public void Unregister(PickupItem pickup)
    {
        if (!pickup) return;
        if (timers.TryGetValue(pickup, out var co) && co != null)
            StopCoroutine(co);

        timers.Remove(pickup);
    }

    public static bool ForceRelocateNow(PickupItem pickup)
    {
        if (!pickup) return false;

        if (!Instance)
        {
            var go = new GameObject("CursedItemRespawnManager");

            DontDestroyOnLoad(go);
            go.AddComponent<CursedItemRespawnManager>();
        }

        if (Instance.timers.TryGetValue(pickup, out var co) && co != null)
            Instance.StopCoroutine(co);

        Instance.timers.Remove(pickup);

        return Instance.TryRelocate(pickup);
    }
    #endregion

    #region core
    private IEnumerator RespawnAfterIdle(PickupItem pickup, int epoch)
    {
        float t = 0f;
        var player = FindFirstObjectByType<PlayerInventory>()?.transform;
        float minDistSqr = minDistanceFromPlayer * minDistanceFromPlayer;

        while (pickup && t < respawnDelaySeconds)
        {
            if (!pickup.gameObject.activeInHierarchy) // picked up / disabled / destroyed
            {
                Cleanup(pickup);
                yield break;
            }

            if (!IsCurrentEpoch(pickup, epoch))
                yield break;

            if (player)
            {
                float sqr = (player.position - pickup.transform.position).sqrMagnitude;
                if (sqr >= minDistSqr) t += Time.deltaTime;
            }
            else
            {
                t += Time.deltaTime;
            }

            yield return null;
        }

        if (!pickup) yield break;
        if (!IsCurrentEpoch(pickup, epoch)) yield break;

        bool moved = TryRelocate(pickup);
        if (!moved)
            Debug.LogWarning("[CursedRespawn] Could not find a relocation spot.");

        Cleanup(pickup); // stop tracking; next drop must call Register again
    }

    private bool IsCurrentEpoch(PickupItem p, int epoch)
        => generations.TryGetValue(p, out var cur) && cur == epoch;

    private void Cleanup(PickupItem p)
    {
        if (timers.TryGetValue(p, out var co) && co != null)
            StopCoroutine(co);
        timers.Remove(p);
    }
    #endregion

    #region relocation
    private static bool IsCursed(PickupItem p)
    {
        if (!p || !p.isSpecial) return false;
        var t = p.Type;
        return t == Item.ItemType.SkullDiamond
            || t == Item.ItemType.HeartDiamond
            || t == Item.ItemType.Crown;
    }

    private bool TryRelocate(PickupItem p)
    {
        var dc = FindFirstObjectByType<DungeonController>();
        var viz = dc ? dc.Viz : null;
        var grid = dc ? dc.Grid : null;
        var map = dc ? dc.MapIndex : null;

        if (!viz || grid == null) return false;

        // Prefer indexed room interiors (accurate placement)
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

        // Fallback: random inner area of a room
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
            if (i++ == skip) { value = v; return true; }
        }
        foreach (var v in set) { value = v; return true; } // ultra-fallback
        return false;
    }

    private static void MovePickupToGridLocal(PickupItem p, TilemapVisualizer viz, Vector2Int cell)
    {
        var parent = viz.GridTransform ? viz.GridTransform : null;
        if (parent) p.transform.SetParent(parent, false);
        p.transform.localPosition = viz.CellCenterLocal(cell.x, cell.y);
    }
    #endregion
}
