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
        SetupPlayerHP();
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

        // ensure instance exists under a persistent parent
        if (!playerInstance)
            playerInstance = Instantiate(playerPrefab, runtimeRoot ? runtimeRoot : null);

        // compute world position from grid-local
        Vector3 local = tmVisualizer.CellCenterLocal(cell.x, cell.y);
        Vector3 world = local;
        if (tmVisualizer && tmVisualizer.GridTransform)
            world = tmVisualizer.GridTransform.TransformPoint(local);

        // place without parenting to the Grid (stay persistent)
        playerInstance.transform.position = world;

        // tracking, events, UI rebinds
        var tracker = playerInstance.GetComponent<PlayerRoomTracker>() ?? playerInstance.AddComponent<PlayerRoomTracker>();
        tracker.SetController(this);

        int rid = grid.InBounds(cell.x, cell.y) ? grid.RoomId[cell.x, cell.y] : -1;
        NotifyRoomEntered(rid);
        PlayerSpawned?.Invoke(playerInstance.GetComponent<PlayerInventory>());
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

        var light = playerInstance.GetComponentInChildren<UnityEngine.Rendering.Universal.Light2D>(true);
        if (!light) return;

        var follow = light.GetComponent<FollowTarget2D>() ?? light.gameObject.AddComponent<FollowTarget2D>();
        follow.SetTarget(playerInstance.transform);
        follow.SetSmooth(0f);

        var lightAim = light.GetComponent<LightAim>() ?? light.gameObject.AddComponent<LightAim>();
        lightAim.SetPlayer(playerInstance.GetComponent<TopDownController>());
    }

    public void SetupPlayerHP()
    {
        if (playerInstance)
        {
            var hp = playerInstance.GetComponent<PlayerHealth>() ?? playerInstance.AddComponent<PlayerHealth>();
            hp.ResetToFull();
            if (healthUI)
            {
                healthUI.SetHealth(hp.Current, hp.Max);
                hp.Changed -= healthUI.SetHealth;
                hp.Changed += healthUI.SetHealth;
            }
        }
    }
    #endregion

    #region Systems Wiring
    private void WireSystems()
    {
        orderedBiomeKinds.Clear();
        if (sequence && sequence.biomes != null)
        {
            foreach (var b in sequence.biomes)
                if (b != null && b.biomeProfile && !string.IsNullOrEmpty(b.biomeProfile.floorKind))
                    orderedBiomeKinds.Add(b.biomeProfile.floorKind);
        }

        if (trapManager != null)
            trapManager.SetGridContext(tmVisualizer.GridTransform, tmVisualizer);

        trapManager?.Build(grid, rooms, resolveTier: kind => orderedBiomeKinds.IndexOf(kind), tmVisualizer.CarpetMask);
        var trapCells = TrapCellsFromChildren(trapManager, tmVisualizer);

        SetPolicy();
        propPopulator?.Populate(grid, rooms, mapIndex, tmVisualizer.CarpetMask, randomSeed, trapCells);

    }

    private void SetPolicy()
    {
        if (sequence.currentBiomeIndex == 0)
        {
            // BIOME 1
            propPopulator.SetDropPolicy(new PropPopulator.DropPolicy
            {
                allowSwords = true,
                allowBooks = false,
                allowPotions = true,
                guaranteedSwords = 6,
                guaranteedBooks = 0,
                potionChance = 0.1f
            });
            Debug.Log("[DungeonController] SetPolicy 0");
        }
        else if (sequence.currentBiomeIndex == 1)
        { // BIOME 2 
            propPopulator.SetDropPolicy(new PropPopulator.DropPolicy
            {
                allowSwords = false,
                allowBooks = true,
                allowPotions = true,
                guaranteedSwords = 0,
                guaranteedBooks = 5,
                potionChance = 0.2f
            });
        }
        else
        {// BIOME 3
            propPopulator.SetDropPolicy(new PropPopulator.DropPolicy
            {
                allowSwords = false,
                allowBooks = false,
                allowPotions = true,
                guaranteedSwords = 0,
                guaranteedBooks = 0,
                potionChance = 0.2f
            });
        }
    }
    private static HashSet<Vector2Int> TrapCellsFromChildren(TrapManager manager, TilemapVisualizer viz)
    {
        var set = new HashSet<Vector2Int>();
        if (!manager || !viz) 
            return set;

        var gridTr = viz.GridTransform;
        var gridCmp = gridTr ? gridTr.GetComponent<Grid>() : null;

        for (int i = 0; i < manager.transform.childCount; i++)
        {
            var tr = manager.transform.GetChild(i);
            if (!tr) continue;

            var world = tr.position;

            if (gridCmp != null)
            {
                Vector3Int tileCell = gridCmp.WorldToCell(world);
                Vector2Int gridCell = viz.TileToGridCell(tileCell);
                set.Add(gridCell);
            }
            else
            {
                set.Add(new Vector2Int(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.y)));
            }
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