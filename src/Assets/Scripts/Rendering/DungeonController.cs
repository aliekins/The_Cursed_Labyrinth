/// \file DungeonController.cs
/// \brief Sets up: BSP rooms, MST, A* corridors, biome bands, tilemap render. Press 'R' to regenerate.
using System;
using System.Collections.Generic;
using UnityEngine;

public class DungeonController : MonoBehaviour
{
    [Header("Map")]
    [SerializeField] private int width = 64;
    [SerializeField] private int height = 48;
    [SerializeField, Tooltip("Units per cell")] private float cellSize = 1f;

    [Header("Map Borders")]
    [SerializeField] private int border = 1;
    [SerializeField] private int roomPadding = 1;

    [Header("BSP")]
    [SerializeField] private int minLeafSize = 12;
    [SerializeField] private int minRoomSize = 4;
    [SerializeField] private int maxRoomSize = 10;

    [Header("Corridors")]
    [SerializeField] private int corridorThickness = 1;

    [Header("Rules / Visuals")]
    [SerializeField] private TileRuleDatabase ruleDatabase;
    [SerializeField] private TilemapVisualizer tmVisualizer; 

    [Header("Seed")]
    [SerializeField] private int seed = 12345;
    [SerializeField] private bool randomizeSeedOnStart = true;

    [Header("Player")]
    [SerializeField] private GameObject playerPrefab;

    private RuleDrivenVisualizer_SO visualizer;
    private DungeonGrid grid;
    private GameObject playerInstance;
    private System.Random rng;

    private void Start()
    {
        if (!ruleDatabase)
        {
            Debug.LogError("DungeonController: Assign a TileRuleDatabase in the inspector");
            return;
        }
        if (!tmVisualizer)
        {
            Debug.LogError("DungeonController: Assign a TilemapVisualizer in the inspector");
            return;
        }

        visualizer ??= new RuleDrivenVisualizer_SO(transform, ruleDatabase, cellSize);

        if (randomizeSeedOnStart)
            seed = Guid.NewGuid().GetHashCode();

        rng = new System.Random(seed);
        Build();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            seed = Guid.NewGuid().GetHashCode();
            rng = new System.Random(seed);
            Build();
        }
    }

    private void Build()
    {
        // Clear previous render
        visualizer.Clear();
        tmVisualizer.Clear();

        // Create an empty grid (default = wall)
        grid = new DungeonGrid(width, height);

        int innerW = width - 2 * border;
        int innerH = height - 2 * border;

        // BSP - rooms 
        var cfg = new BspConfig
        {
            MapArea = new RectInt(border, border, innerW, innerH),
            MinLeafSize = minLeafSize,
            MinRoomSize = minRoomSize,
            MaxRoomSize = maxRoomSize
        };

        var split = new AspectBiasedSplitPolicy();
        var carver = new PaddedRoomCarver(roomPadding);

        var bsp = BspGenerator.Generate(cfg, split, carver, rng);
        var rooms = bsp.Rooms;

        // Carve each room as initial floor
        foreach (var r in rooms)
            grid.CarveRoom(r.Bounds, "floor_entry");

        // MST over centers - connect
        var edges = GraphUtils.BuildMstByDistance(rooms);

        // A* corridors between room centers 
        var walkableForPlan = new bool[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                walkableForPlan[x, y] = true;

        foreach (var (a, b) in edges)
        {
            var start = rooms[a].Center;
            var goal = rooms[b].Center;

            var path = AStarPathfinder.FindPath(walkableForPlan, start, goal);
            grid.CarvePath(path, "floor_entry", corridorThickness);
        }

        Vector2Int spawn = rooms.Count > 0 ? rooms[0].Center : new Vector2Int(width / 2, height / 2);

        // Biome bands
        var bands = new List<BiomeBand>
        {
            new BiomeBand(12,  "floor_entry"),
            new BiomeBand(28,  "floor_quarry"),
            new BiomeBand(999, "floor_grove"),
        };
        BiomeAssigner.ApplyBands(grid, spawn, bands);

        // Render via TilemapVisualizer
        tmVisualizer.Render(grid);

        Vector3 spawnLocal = tmVisualizer.CellCenterLocal(spawn.x, spawn.y);
        Transform gridParent = tmVisualizer.GridTransform;

        // Place player
        if (playerPrefab != null)
        {
            if (playerInstance == null)
            {
                playerInstance = Instantiate(playerPrefab);
                if (gridParent != null) playerInstance.transform.SetParent(gridParent, worldPositionStays: false);
            }
            playerInstance.transform.localPosition = spawnLocal;
        }

        var camFollow = Camera.main.GetComponent<CameraFollow>();
        if (camFollow != null)
        {
            camFollow.SetTarget(playerInstance.transform);
        }
    }
}