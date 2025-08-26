using System;
using System.Collections.Generic;
using UnityEngine;

public partial class DungeonController
{
    #region Spawn
    private void SpawnPlayerAndCamera()
    {
        var spawn = ChooseSpawn(rooms);
        PlacePlayer(spawn);
        SetupCamera();
        SetupPlayerLighting();
    }

    private Vector2Int ChooseSpawn(List<Room> list)
    {
        if (list == null || list.Count == 0)
            return new Vector2Int(width / 2, height / 2);

        return SpawnSelector.ChooseFarthestFrom(entranceInsideGrid, list);
    }

    public void PlacePlayer(Vector2Int cell)
    {
        if (!playerPrefab) return;

        Vector3 spawnLocal = tmVisualizer.CellCenterLocal(cell.x, cell.y);
        var gridParent = tmVisualizer.GridTransform;

        if (!playerInstance)
        {
            playerInstance = Instantiate(playerPrefab);
            if (gridParent) playerInstance.transform.SetParent(gridParent, false);
        }
        playerInstance.transform.localPosition = spawnLocal;

        var tracker = playerInstance.GetComponent<PlayerRoomTracker>()
                   ?? playerInstance.AddComponent<PlayerRoomTracker>();
        tracker.SetController(this);

        var inventory = playerInstance.GetComponent<PlayerInventory>()
                     ?? playerInstance.AddComponent<PlayerInventory>();

        PlayerSpawned?.Invoke(inventory);

        int rid = grid.InBounds(cell.x, cell.y) ? grid.RoomId[cell.x, cell.y] : -1;
        NotifyRoomEntered(rid);
    }

    private void SetupCamera()
    {
        if (!playerInstance) return;
        var cam = Camera.main; if (!cam) return;
        var follow = cam.GetComponent<FollowTarget2D>() ?? cam.gameObject.AddComponent<FollowTarget2D>();
        follow.SetTarget(playerInstance.transform);
        follow.SetSmooth(0f);
    }

    private void SetupPlayerLighting()
    {
        if (!playerInstance) return;
        var lightTr = FindAnyObjectByType<UnityEngine.Rendering.Universal.Light2D>()?.transform;
        var lightAim = FindAnyObjectByType<LightAim>();
        if (lightAim) lightAim.SetPlayer(playerInstance.GetComponent<TopDownController>());
        if (!lightTr) return;
        var follow = lightTr.GetComponent<FollowTarget2D>() ?? lightTr.gameObject.AddComponent<FollowTarget2D>();
        follow.SetTarget(playerInstance.transform);
        follow.SetSmooth(0f);
    }
    #endregion

    #region Systems Wiring
    private void WireSystems()
    {
        //biomePlanner?.Build(grid, rooms, edges, tmVisualizer, mapIndex, corridorKind, corridorThickness, rng);

        //trapManager?.Build(grid, rooms, ResolveTierFromRoomKind, tmVisualizer.CarpetMask);

        var trapCells = TrapCellsFromChildren(trapManager, tmVisualizer);
        propPopulator?.Populate(grid, rooms, mapIndex, tmVisualizer.CarpetMask, randomSeed, trapCells);

        //if (playerInstance)
        //{
        //    var hp = playerInstance.GetComponent<PlayerHealth>() ?? playerInstance.AddComponent<PlayerHealth>();
        //    hp.ResetToFull();
        //    if (healthUI)
        //    {
        //        healthUI.SetHealth(hp.Current, hp.Max);
        //        hp.Changed -= healthUI.SetHealth;
        //        hp.Changed += healthUI.SetHealth;
        //    }
        //}
    }

    private static HashSet<Vector2Int> TrapCellsFromChildren(TrapManager manager, TilemapVisualizer viz)
    {
        var set = new HashSet<Vector2Int>();
        if (!manager || !viz) return set;
        var gridTr = viz.GridTransform;
        var grid = gridTr ? gridTr.GetComponent<Grid>() : null;

        for (int i = 0; i < manager.transform.childCount; i++)
        {
            var tr = manager.transform.GetChild(i);
            var world = tr.position;
            Vector3Int cell;
            if (grid) cell = grid.WorldToCell(world);
            else cell = new Vector3Int(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.y), 0);
            set.Add(new Vector2Int(cell.x, cell.y));
        }
        return set;
    }
    #endregion

    #region Tiers & Events
    private int ResolveTierFromRoomKind(string kind)
    {
        if (string.IsNullOrEmpty(kind)) return 0;
        int idx = orderedBiomeKinds.FindIndex(k => kind.StartsWith(k, StringComparison.OrdinalIgnoreCase));
        return idx < 0 ? 0 : idx;
    }

    private void OnRoomEntered(int roomId)
    {
        if (roomId < 0 || rooms == null || roomId >= rooms.Count) return;
        var center = rooms[roomId].Center;
        if (!grid.InBounds(center.x, center.y)) return;
        var kind = grid.Kind[center.x, center.y];
        int tier = ResolveTierFromRoomKind(kind);
        if (tier > currentTier) currentTier = tier;
    }
    #endregion
}