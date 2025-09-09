using System;
using System.Collections.Generic;
using UnityEngine;

/**
 * @file DungeonController.Core.cs
 * @brief Core orchestrator for dungeon generation, rendering and high-level events.
 * @ingroup Controller
 * 
 * Owns the grid, rooms list, indices, visualizer, and player instance. Focuses on startup, rebuild entry points, and public accessors.
 */

/**
 * @class DungeonController
 * @brief Central owner that builds the dungeon, wires systems, and raises runtime events.
 */

public partial class DungeonController : MonoBehaviour
{
    #region Config
    [Header("Map")]
    [SerializeField] private Transform runtimeRoot;
    [SerializeField] private int width = 64;
    [SerializeField] private int height = 48;

    [Header("Rooms")]
    [SerializeField] private int minRoomSize = 4;
    [SerializeField] private int maxRoomSize = 10;

    [Header("Biome Setup")]
    [SerializeField] private BiomeSetup_SO biomeSetup;
    [SerializeField] private BiomeSequenceController sequence;

    [Header("Player")]
    [SerializeField] private GameObject playerPrefab;

    [Header("Gameplay Systems")]
    [SerializeField] private TrapManager trapManager;
    [SerializeField] private HealthUI healthUI;
    [SerializeField] private TilemapClearer clearer;           

    [Header("Visuals")]
    [SerializeField] private TilemapVisualizer tmVisualizer;

    [Header("Props")]
    [SerializeField] private PropPopulator propPopulator;

    [Header("Seed")]
    [SerializeField] private int randomSeed = 12345;
    [SerializeField] private bool randomizeSeedOnStart = true;
    [SerializeField] private bool buildOnStart = false;
    #endregion

    #region Runtime State
    private System.Random rng;
    private DungeonGrid grid;
    private List<Room> rooms = new();
    private DungeonMapIndex mapIndex;
    private GameObject playerInstance;
    private int currentTier = 0;
    private readonly List<string> orderedBiomeKinds = new();
    private string corridorKind = "floor_corridor";
    #endregion

    #region Lifecycle
    private void Start()
    {
        // RNG
        if (randomizeSeedOnStart) randomSeed = System.Guid.NewGuid().GetHashCode();
        rng = new System.Random(randomSeed);

        // gameplay events
        RoomEntered += OnRoomEntered;

        if (buildOnStart)
        {
            clearer?.ClearAll();

            if (sequence != null) sequence.StartFirstBiome();
            else Debug.LogWarning("[DungeonController] No BiomeSequenceController assigned.");
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            // New seed
            randomSeed = System.Guid.NewGuid().GetHashCode();
            rng = new System.Random(randomSeed);

            clearer?.ClearAll();

            if (sequence != null)
            {
                sequence.TryRestartCurrentBiome();
                return;
                //sequence.StartFirstBiome();
            }
            else
            {
                Debug.LogWarning("[DungeonController] Pressed R but no BiomeSequenceController is assigned.");
            }
        }
    }
    private void OnDestroy() => RoomEntered -= OnRoomEntered;
    #endregion

    #region Build Entry
    /**
     * @brief Build the dungeon for a biome profile and special room seed
     * @param profile Biome setup asset (sizes, kinds)
     * @param seedInfo Prefab/tiles info for the special room
     */
    public void Build(BiomeSetup_SO profile, SpecialRoomSeeder.SeedInfo seedInfo)
    {
        // Apply biome overrides (size/kinds) and RNG
        if (profile)
        {
            width = profile.width;
            height = profile.height;

            if (!string.IsNullOrEmpty(profile.corridorKind))
                corridorKind = profile.corridorKind;
        }

        // music
        if (profile != null)
        {
            // Prefer exact kind y
            var musicId = !string.IsNullOrEmpty(profile.floorKind) ? profile.floorKind : profile.name;
            MusicPlayer.SetBiome(musicId);
        }

        if (randomizeSeedOnStart)
            randomSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

        rng = new System.Random(randomSeed);

        // Generate grid and rooms from the prefab seed
        GenerateFromSeed(profile, seedInfo);

        // Build derived data and render
        BuildRoomInfosAndReserve();
        BuildIndices();
        RenderDungeon();

        // Spawn and systems wiring
        WireSystems();
        SpawnPlayerAndCamera();
    }

    #region helpers
    private void CreateGrid() => grid = new DungeonGrid(width, height);

    // room metadata (entrances/occupied) so indices/props can use it
    private void BuildRoomInfosAndReserve()
    {
        if (rooms == null) return;
        string corridorPrefix = corridorKind;
        foreach (var r in rooms)
        {
            r.Info.BuildFromGrid(
                r.Id,
                r.Bounds,
                grid,
                edgeBand: 2,
                corridorKindPrefix: corridorPrefix
            );
            foreach (var e in r.Info.Entrances)
                r.Info.Occupied.Add(e);
        }
    }

    // fast lookup for gameplay
    private void BuildIndices()
    {
        mapIndex = DungeonMapIndexBuilder.Build(
            grid,
            rooms,
            new DungeonMapIndexBuilder.Options { CorridorPrefix = corridorKind }
        );
    }

    // Draw
    private void RenderDungeon() => tmVisualizer?.Render(grid);

    #endregion

    #endregion

    #region Events
    public event Action<Room> RoomReady;
    public event Action<int> RoomEntered;
    public event Action<PlayerInventory> PlayerSpawned;
    public void NotifyRoomEntered(int roomId) => RoomEntered?.Invoke(roomId);
    #endregion

    #region Accessors
    public IReadOnlyList<Room> Rooms => rooms;
    public DungeonGrid Grid => grid;
    public TilemapVisualizer Viz => tmVisualizer;
    public DungeonMapIndex MapIndex => mapIndex;
    public Vector2Int ToCell(Vector3 world)
    {
        var g = tmVisualizer.GridTransform?.GetComponent<Grid>();
        if (!g) return new Vector2Int(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.y));
        var c = g.WorldToCell(world);
        return new Vector2Int(c.x, c.y);
    }
    #endregion
}