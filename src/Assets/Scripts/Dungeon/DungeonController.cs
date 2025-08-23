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
    [SerializeField] private TrapManager trapManager;
    [SerializeField] private HeartsBar healthUI;

    [Header("Visuals")]
    [SerializeField] private TilemapVisualizer tmVisualizer;

    [Header("Props")]
    [SerializeField] private PropPopulator propPopulator;

    [Header("Puzzles")]
    [SerializeField] private PuzzlePlacer puzzlePlacer;

    [Header("Seed")]
    [SerializeField] private int seed = 12345;
    [SerializeField] private bool randomizeSeedOnStart = true;

    private int currentTier = 0;
    private List<string> _orderedBiomeKinds;
    private List<Room> _rooms;
    private DungeonGrid grid;
    private GameObject playerInstance;
    private System.Random rng;
    private List<(int a, int b)> _edges;         
    private DungeonMapIndex mapIndex;

    private void Start()
    {
        if (!tmVisualizer)
        { 
            Debug.LogError("Assign TilemapVisualizer"); 
            return;
        }

        if (randomizeSeedOnStart) seed = Guid.NewGuid().GetHashCode();

        rng = new System.Random(seed);
        _orderedBiomeKinds = biomeBands.OrderBy(b => b.maxDistance).Select(b => b.kind)
                                       .Distinct(StringComparer.OrdinalIgnoreCase).ToList();

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

        GenerateAndCarve();                // rooms + corridors
        AssignBiomesAndRoomInfo();         // biome bands + Room.Info
        BuildIndices();                   

        RenderDungeon();

        SpawnPlayerAndCamera();

        WireSystems();                     // props/traps/health UI
    }
    #region Main Build Steps
    private void GenerateAndCarve()
    {
        _rooms = GenerateRooms();
        CarveRooms(_rooms);
        _edges = ConnectRooms(_rooms);
        CarveCorridors(_rooms, _edges);
    }

    private void AssignBiomesAndRoomInfo()
    {
        var spawn = ChooseSpawn(_rooms);
        AssignBiomes(spawn, _rooms, _edges);
        BuildRoomInfosAndReserve();
    }


    private void BuildIndices()
    {
        mapIndex = DungeonMapIndexBuilder.Build(
            grid,
            _rooms,
            new DungeonMapIndexBuilder.Options
            {
                CorridorPrefix = corridorKind
            }
        );
    }

    private void SpawnPlayerAndCamera()
    {
        var spawn = ChooseSpawn(_rooms);
        PlacePlayer(spawn);
        SetupCamera();
        SetupPlayerLighting();
    }

    private void WireSystems()
    {
        puzzlePlacer.Build(grid, _rooms, mapIndex, seed);

        trapManager?.Build(grid, _rooms, ResolveTierFromRoomKind, tmVisualizer.CarpetMask);

        var trapCells = TrapCellsFromChildren(trapManager, tmVisualizer);
        propPopulator.Populate(grid, _rooms, mapIndex, tmVisualizer.CarpetMask, seed, trapCells);

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

    #region helpers
    private void BuildRoomInfosAndReserve()
    {
        HashSet<Vector2Int> doorCells = null;

        foreach (var r in _rooms)
        {
            r.Info.BuildFromGrid(
                r.Id,
                r.Bounds,
                grid,
                edgeBand: 2,
                corridorKindPrefix: "floor_corridor"
            );

            if (doorCells != null)
            {
                foreach (var d in doorCells)
                    if (grid.InBounds(d.x, d.y) && grid.RoomId[d.x, d.y] == r.Id)
                        r.Info.Occupied.Add(d);
            }

            foreach (var e in r.Info.Entrances)
                r.Info.Occupied.Add(e);
        }

    }
    private void ClearVisuals() => tmVisualizer?.Clear();
    private void OnDestroy() => RoomEntered -= OnRoomEntered;
    private void CreateGrid() => grid = new DungeonGrid(width, height);

    private List<Room> GenerateRooms()
    {
        int innerW = width - 2 * border, innerH = height - 2 * border;
        var cfg = new BspConfig
        {
            MapArea = new RectInt(border, border, innerW, innerH),
            MinLeafSize = minLeafSize,
            MinRoomSize = minRoomSize,
            MaxRoomSize = maxRoomSize
        };
        var bsp = BspGenerator.Generate(cfg, new AspectBiasedSplitPolicy(), new PaddedRoomCarver(roomPadding), rng);
        return bsp.Rooms;
    }

    private void CarveRooms(IEnumerable<Room> rooms)
    {
        foreach (var r in rooms)
            grid.CarveRoom(r.Bounds, "floor_entry");
    }
    private List<(int a, int b)> ConnectRooms(List<Room> rooms) => GraphUtils.BuildMstByDistance(rooms);

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

    private Vector2Int ChooseSpawn(List<Room> rooms) => rooms.Count > 0 ? rooms[0].Center : new Vector2Int(width / 2, height / 2);

    private void AssignBiomes(Vector2Int spawn, List<Room> rooms, List<(int a, int b)> edges)
    {
        if (rooms == null || rooms.Count == 0) return;

        // Distances by room graph
        float[] rd = GraphUtils.ComputeRoomDistances(rooms, edges, startRoom: 0);

        // Gather finite distances (ignore unreachable rooms)
        var dlist = new List<float>(rooms.Count);
        for (int i = 0; i < rd.Length; i++)
            if (!float.IsInfinity(rd[i]) && !float.IsNaN(rd[i]))
                dlist.Add(rd[i]);

        if (dlist.Count == 0) return;

        dlist.Sort();

        // Pick percentile cutoffs
        float cut1 = Percentile(dlist, 0.25f);  // end of Biome 1
        float cut2 = Percentile(dlist, 0.64f);  // end of Biome 2 (Biome 3 is the rest)

        // Optional: small safety spread to avoid ties on exact cut values
        cut1 += 0.001f;
        cut2 += 0.001f;

        // Build protection mask near corridors 
        bool[,] protect = BiomePainter.BuildEntryBufferMask(grid, rooms, corridorKind, biomeEntryBuffer);

        // Build dynamic bands & paint
        var dynamicBands = new List<BiomeBandDef>
    {
        new BiomeBandDef { maxDistance = Mathf.RoundToInt(cut1), kind = "floor_entry"  },
        new BiomeBandDef { maxDistance = Mathf.RoundToInt(cut2), kind = "floor_quarry" },
        new BiomeBandDef { maxDistance = int.MaxValue,           kind = "floor_grove"  },
    };

        BiomePainter.PaintRoomsByBands(grid, rooms, rd, dynamicBands, protect, corridorKind);
        RoomIndexer.StampRoomIds(grid, rooms); // keep your room id stamps if you rely on them

        static float Percentile(List<float> sorted, float p)
        {
            p = Mathf.Clamp01(p);
            if (sorted.Count == 1) return sorted[0];
            float idx = p * (sorted.Count - 1);
            int lo = Mathf.FloorToInt(idx);
            int hi = Mathf.Min(lo + 1, sorted.Count - 1);
            float t = idx - lo;
            return Mathf.Lerp(sorted[lo], sorted[hi], t);
        }
    }

    private void RenderDungeon() => tmVisualizer.Render(grid);
    #endregion

    #region player/camera/light
    private void PlacePlayer(Vector2Int spawn)
    {
        if (!playerPrefab) return;

        Vector3 spawnLocal = tmVisualizer.CellCenterLocal(spawn.x, spawn.y);
        var gridParent = tmVisualizer.GridTransform;

        if (!playerInstance)
        {
            playerInstance = Instantiate(playerPrefab);
            if (gridParent) playerInstance.transform.SetParent(gridParent, false);
        }
        playerInstance.transform.localPosition = spawnLocal;

        var tracker = playerInstance.GetComponent<PlayerRoomTracker>() ?? playerInstance.AddComponent<PlayerRoomTracker>();
        tracker.SetController(this);

        int rid = grid.InBounds(spawn.x, spawn.y) ? grid.RoomId[spawn.x, spawn.y] : -1;
        NotifyRoomEntered(rid);
    }
    private void SetupCamera()
    {
        if (!playerInstance) return;

        var cam = Camera.main; 
        if (!cam) return;

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

    #region tiers + events
    private int ResolveTierFromRoomKind(string kind)
    {
        if (string.IsNullOrEmpty(kind))
            return 0;

        int idx = _orderedBiomeKinds.FindIndex(k => kind.StartsWith(k, StringComparison.OrdinalIgnoreCase));

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
        }
    }

    public event Action<int> RoomEntered;
    public void NotifyRoomEntered(int roomId) => RoomEntered?.Invoke(roomId);
    #endregion

    #region utilities
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

    public DungeonGrid Grid => grid;
    public Vector2Int ToCell(Vector3 world)
    {
        var g = tmVisualizer.GridTransform?.GetComponent<Grid>();

        if (!g) 
            return new Vector2Int(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.y));

        var c = g.WorldToCell(world);

        return new Vector2Int(c.x, c.y);
    }
    //public Vector3 CellCenterLocal(Vector2Int c) => tmVisualizer.CellCenterLocal(c.x, c.y);
    //public Transform PlayerTransformOrNull() => playerInstance ? playerInstance.transform : null;
    //public void TilesSetDirtyAt(Vector2Int _)
    //{
    //    tmVisualizer.Clear();
    //    tmVisualizer.Render(grid);
    //}
    #endregion
}