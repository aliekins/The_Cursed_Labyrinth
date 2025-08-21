/// \file DungeonController.cs
/// \brief Sets up: BSP rooms, MST, A* corridors, biome bands, tilemap render. Press 'R' to regenerate.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DungeonController : MonoBehaviour
{
    [Header("Map")]
    [SerializeField] private int width = 64;
    [SerializeField] private int height = 48;
    //[SerializeField, Tooltip("Units per cell")] private float cellSize = 1f;

    [Header("Map Borders")]
    [SerializeField] private int border = 1;
    [SerializeField] private int roomPadding = 1;

    [Header("BSP")]
    [SerializeField] private int minLeafSize = 12;
    [SerializeField] private int minRoomSize = 4;
    [SerializeField] private int maxRoomSize = 10;

    [Header("Corridors")] 
    [SerializeField] private int corridorThickness = 1;
    [Tooltip("Corridors + N tiles into a room stay neutral")]
    [SerializeField, Range(0, 6)] private int biomeEntryBuffer = 1;

    [Tooltip("Kind for neutral corridors/buffers between rooms.")]
    [SerializeField] private string corridorKind = "floor_corridor";

    [Serializable]
    public struct BiomeBandDef
    {
        public int maxDistance;
        public string kind;
    }

    [Header("Biome Bands")] 
    [SerializeField]
    private List<BiomeBandDef> biomeBands = new()
    {
        new BiomeBandDef { maxDistance = 80,  kind = "floor_entry"  },
        new BiomeBandDef { maxDistance = 200,  kind = "floor_quarry" },
        new BiomeBandDef { maxDistance = 500, kind = "floor_grove"  },
    };

    [Header("Player")]
    [SerializeField] private GameObject playerPrefab;

    [Header("Gameplay Systems")]
    //[SerializeField] private PuzzleManager puzzleManager;
    [SerializeField] private TrapManager trapManager;
    [SerializeField] private HealthUI healthUI;

    [Header("Visuals")]
    //[SerializeField] private TileRuleDatabase ruleDatabase;
    [SerializeField] private TilemapVisualizer tmVisualizer;

    [Header("Doors")]
    [SerializeField] private DoorManager doorManager;

    [Header("Seed")]
    [SerializeField] private int seed = 12345;
    [SerializeField] private bool randomizeSeedOnStart = true;

    private int currentTier = 0;
    private List<string> _orderedBiomeKinds;
    private List<Room> _rooms;

    //private RuleDrivenVisualizer_SO visualizer;
    private DungeonGrid grid;

    private GameObject playerInstance;

    private System.Random rng;

    private void Start()
    {
        if (!tmVisualizer)
        {
            Debug.LogError("DungeonController: Assign a TilemapVisualizer in the inspector");
            return;
        }

        if (randomizeSeedOnStart)
            seed = Guid.NewGuid().GetHashCode();

        rng = new System.Random(seed);
        _orderedBiomeKinds = biomeBands
           .OrderBy(b => b.maxDistance)
           .Select(b => b.kind)
           .Distinct(StringComparer.OrdinalIgnoreCase)
           .ToList();

        Build();
        RoomEntered += OnRoomEntered;
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
        ClearVisuals();
        CreateGrid();

        _rooms = GenerateRooms();
        CarveRooms(_rooms);

        var edges = ConnectRooms(_rooms);
        CarveCorridors(_rooms, edges);

        var spawn = ChooseSpawn(_rooms);

        AssignBiomes(spawn, _rooms, edges);

        var doorPlans = DoorPlacer.PlanDoors(grid, _rooms, corridorKind, ResolveTierFromRoomKind);
        doorManager?.Spawn(doorPlans);

        RenderDungeon();

        //puzzleManager?.Build(grid, _rooms, ResolveTierFromRoomKind, doorManager);
        trapManager?.Build(grid, _rooms, ResolveTierFromRoomKind, tmVisualizer.CarpetMask);

        PlacePlayer(spawn);
        SetupCamera();
        SetupPlayerLighting();

        if (healthUI && playerInstance)
        {
            var hp = playerInstance.GetComponent<PlayerHealth>();
            if (!hp) hp = playerInstance.AddComponent<PlayerHealth>();
            hp.ResetToFull();

            healthUI.Bind(hp);
        }
    }

    #region helpers
    private void ClearVisuals()
    {
        //visualizer?.Clear();
        tmVisualizer?.Clear();
    }
    private void OnDestroy()
    {
        RoomEntered -= OnRoomEntered;
    }

    private void CreateGrid()
    {
        grid = new DungeonGrid(width, height);
    }

    private List<Room> GenerateRooms()
    {
        int innerW = width - 2 * border;
        int innerH = height - 2 * border;

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
        return bsp.Rooms;
    }

    private void CarveRooms(IEnumerable<Room> rooms)
    {
        foreach (var r in rooms)
            grid.CarveRoom(r.Bounds, "floor_entry");
    }

    private List<(int a, int b)> ConnectRooms(List<Room> rooms)
    {
        return GraphUtils.BuildMstByDistance(rooms);
    }

    private void CarveCorridors(List<Room> rooms, List<(int a, int b)> edges)
    {
        var walkable = new bool[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                walkable[x, y] = true;

        foreach (var (a, b) in edges)
        {
            var start = rooms[a].Center;
            var goal = rooms[b].Center;
            var path = AStarPathfinder.FindPath(walkable, start, goal);
            grid.CarvePath(path, "floor_corridor", corridorThickness);
        }
    }

    private Vector2Int ChooseSpawn(List<Room> rooms)
    {
        if (rooms.Count > 0)
            return rooms[0].Center;

        return new Vector2Int(width / 2, height / 2);
    }

    private void AssignBiomes(Vector2Int spawn, List<Room> rooms, List<(int a, int b)> edges)
    {
        if (biomeBands.Count == 0 || rooms.Count == 0) return;

        float[] rd = GraphUtils.ComputeRoomDistances(rooms, edges, startRoom: 0);
        bool[,] protect = BiomePainter.BuildEntryBufferMask(grid, rooms, corridorKind, biomeEntryBuffer);
        BiomePainter.PaintRoomsByBands(grid, rooms, rd, biomeBands, protect, corridorKind);

        RoomIndexer.StampRoomIds(grid, rooms);
    }


    private void RenderDungeon()
    {
        tmVisualizer.Render(grid);
    }

    private void PlacePlayer(Vector2Int spawn)
    {
        if (playerPrefab == null) return;

        Vector3 spawnLocal = tmVisualizer.CellCenterLocal(spawn.x, spawn.y);
        Transform gridParent = tmVisualizer.GridTransform;

        if (playerInstance == null)
        {
            playerInstance = Instantiate(playerPrefab);
            if (gridParent != null)
                playerInstance.transform.SetParent(gridParent, worldPositionStays: false);
        }

        playerInstance.transform.localPosition = spawnLocal;
        var tracker = playerInstance.GetComponent<PlayerRoomTracker>();

        if (tracker == null) 
        {
            tracker = playerInstance.AddComponent<PlayerRoomTracker>();
        }
        tracker.SetController(this);

        int rid = grid.InBounds(spawn.x, spawn.y) ? grid.RoomId[spawn.x, spawn.y] : -1;
        NotifyRoomEntered(rid);
    }

    private void SetupCamera()
    {
        if (playerInstance == null) return;

        var cam = Camera.main;
        if (!cam) return;

        var follow = cam.GetComponent<FollowTarget2D>();
        if (!follow) follow = cam.gameObject.AddComponent<FollowTarget2D>();

        follow.SetTarget(playerInstance.transform);
        follow.SetSmooth(0f);
    }

    private void SetupPlayerLighting()
    {
        if (playerInstance == null) return;

         var lightObj = FindAnyObjectByType<UnityEngine.Rendering.Universal.Light2D>()?.transform;

        if (!lightObj) return;

        var follow = lightObj.GetComponent<FollowTarget2D>();
        if (!follow) follow = lightObj.gameObject.AddComponent<FollowTarget2D>();

        follow.SetTarget(playerInstance.transform);
        follow.SetSmooth(0f);
    }

    #endregion
    #region MightReorganize 
    private int ResolveTierFromRoomKind(string kind)
    {
        if (string.IsNullOrEmpty(kind)) return 0;
        int idx = _orderedBiomeKinds.FindIndex(k =>
            kind.StartsWith(k, StringComparison.OrdinalIgnoreCase));
        return idx < 0 ? 0 : idx;
    }

    private void OnRoomEntered(int roomId)
    {
        if (roomId < 0 || _rooms == null || roomId >= _rooms.Count) return;

        var center = _rooms[roomId].Center;
        if (!grid.InBounds(center.x, center.y)) return;

        var kind = grid.Kind[center.x, center.y];
        int tier = ResolveTierFromRoomKind(kind);

        if (tier > currentTier)
        {
            currentTier = tier;
            doorManager?.UnlockUpTo(currentTier);
        }
    }
    #endregion

    #region PlayerTracking
    public DungeonGrid Grid => grid;

    public Vector2Int ToCell(Vector3 world)
    {
        var g = tmVisualizer.GridTransform?.GetComponent<Grid>();
        if (g == null) return new Vector2Int(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.y));
        var c = g.WorldToCell(world);
        return new Vector2Int(c.x, c.y);
    }

    public event Action<int> RoomEntered;
    public void NotifyRoomEntered(int roomId)
    {
        RoomEntered?.Invoke(roomId);
    }

    //public bool TryGetPlans(int roomId, out List<PuzzlePlan> plans)
    //{
    //    if (_puzzlePlans != null && _puzzlePlans.TryGetValue(roomId, out plans))
    //        return true;

    //    plans = null;
    //    return false;
    //}

    public Transform GridTransform => tmVisualizer.GridTransform;
    public Vector3 CellCenterLocal(Vector2Int cell) => tmVisualizer.CellCenterLocal(cell.x, cell.y);

    public Transform PlayerTransformOrNull() => playerInstance ? playerInstance.transform : null;

    public void TilesSetDirtyAt(Vector2Int _)
    {
        tmVisualizer.Clear();
        tmVisualizer.Render(grid);
    }

    #endregion
}