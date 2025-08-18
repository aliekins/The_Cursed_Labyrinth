/// \file DungeonController.cs
/// \brief Sets up: BSP rooms, MST, A* corridors, biome bands, tilemap render. Press 'R' to regenerate.
using System;
using System.Collections.Generic;
using UnityEditor;
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

    [Tooltip("Corridors + N tiles into a room stay neutral")] 
    [SerializeField, Range(0, 6)] private int biomeEntryBuffer = 1;

    [Tooltip("Kind for neutral corridors/buffers between rooms.")]
    [SerializeField] private string corridorKind = "floor_corridor";

    [Header("Player")]
    [SerializeField] private GameObject playerPrefab;

    [Header("Rules / Visuals")]
    [SerializeField] private TileRuleDatabase ruleDatabase;
    [SerializeField] private TilemapVisualizer tmVisualizer;

    [Header("Seed")]
    [SerializeField] private int seed = 12345;
    [SerializeField] private bool randomizeSeedOnStart = true;

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
        ClearVisuals();
        CreateGrid();

        var rooms = GenerateRooms();
        CarveRooms(rooms);

        var edges = ConnectRooms(rooms);
        CarveCorridors(rooms, edges);

        var spawn = ChooseSpawn(rooms);

        AssignBiomes(rooms, edges);

        RenderDungeon();

        PlacePlayer(spawn);
        SetupCamera();
    }

    #region helpers
    private void ClearVisuals()
    {
        visualizer?.Clear();
        tmVisualizer?.Clear();
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

    private void AssignBiomes(List<Room> rooms, List<(int a, int b)> edges)
    {
        if (grid == null || rooms == null || rooms.Count == 0) return;
        if (biomeBands == null || biomeBands.Count == 0) return;

        float[] roomDist = ComputeRoomDistances(rooms, edges, startRoom: 0);
        bool[,] protect = BuildEntryBufferMask(grid, rooms, corridorKind, biomeEntryBuffer);

        PaintRoomsByBands(grid, rooms, roomDist, biomeBands, protect, corridorKind);
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
    }

    private void SetupCamera()
    {
        var camFollow = Camera.main?.GetComponent<CameraFollow>();
        if (camFollow != null && playerInstance != null)
            camFollow.SetTarget(playerInstance.transform);
    }
    #endregion

    #region utilities
    private static float[] ComputeRoomDistances(List<Room> rooms, List<(int a, int b)> edges, int startRoom)
    {
        int n = rooms.Count;
        var adj = new List<(int v, float w)>[n];
        for (int i = 0; i < n; i++) adj[i] = new();

        foreach (var (a, b) in edges)
        {
            float w = Vector2Int.Distance(rooms[a].Center, rooms[b].Center);
            adj[a].Add((b, w));
            adj[b].Add((a, w));
        }

        var dist = new float[n];
        for (int i = 0; i < n; i++) dist[i] = float.PositiveInfinity;

        // Dijkstra 
        var pq = new PriorityQueue<int, float>();
        dist[startRoom] = 0f; pq.Enqueue(startRoom, 0f);

        while (pq.Count > 0)
        {
            int u = pq.Dequeue();
            float du = dist[u];

            foreach (var (v, w) in adj[u])
            {
                float cand = du + w;
                if (cand < dist[v])
                {
                    dist[v] = cand;
                    pq.Enqueue(v, cand);
                }
            }
        }
        return dist;
    }

    private static bool[,] BuildEntryBufferMask(DungeonGrid grid, IReadOnlyList<Room> rooms, string corridorKind, int bufferDepth)
    {
        int W = grid.Width, H = grid.Height;
        var protect = new bool[W, H];
        if (bufferDepth <= 0) return protect;

        ReadOnlySpan<Vector2Int> N4 = stackalloc Vector2Int[] { new(1, 0), new(-1, 0), new(0, 1), new(0, -1) };

        foreach (var room in rooms)
        {
            var bounds = room.Bounds;
            var seen = new bool[W, H];
            var q = new Queue<(int x, int y, int d)>();

            // enqueue door tiles (room cells touching a corridor)
            for (int x = bounds.xMin; x < bounds.xMax; x++)
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    if (!grid.InBounds(x, y)) continue;
                    var k = grid.Kind[x, y];
                    if (k == null || !k.StartsWith("floor", StringComparison.OrdinalIgnoreCase)) continue;

                    bool touchesCorr = false;
                    foreach (var d in N4)
                    {
                        int nx = x + d.x, ny = y + d.y;
                        if (!grid.InBounds(nx, ny)) continue;
                        if (string.Equals(grid.Kind[nx, ny], corridorKind, StringComparison.OrdinalIgnoreCase))
                        { touchesCorr = true; break; }
                    }
                    if (touchesCorr) { q.Enqueue((x, y, 0)); seen[x, y] = true; }
                }

            // flood inside the room up to bufferDepth
            while (q.Count > 0)
            {
                var (cx, cy, cd) = q.Dequeue();
                protect[cx, cy] = true;
                if (cd >= bufferDepth - 1) continue;

                foreach (var d in N4)
                {
                    int nx = cx + d.x, ny = cy + d.y;
                    if (!grid.InBounds(nx, ny)) continue;
                    if (seen[nx, ny]) continue;
                    if (nx < bounds.xMin || nx >= bounds.xMax || ny < bounds.yMin || ny >= bounds.yMax) continue;

                    var k = grid.Kind[nx, ny];
                    if (k == null || !k.StartsWith("floor", StringComparison.OrdinalIgnoreCase)) continue;

                    seen[nx, ny] = true;
                    q.Enqueue((nx, ny, cd + 1));
                }
            }
        }
        return protect;
    }
    private void PaintRoomsByBands(
        DungeonGrid grid,
        List<Room> rooms,
        float[] roomDist,
        IReadOnlyList<BiomeBandDef> bands,
        bool[,] protectMask,
        string corridorKind)
    {
        var sorted = new List<BiomeBandDef>(bands);
        sorted.Sort((a, b) => a.maxDistance.CompareTo(b.maxDistance));

        string PickKind(float d)
        {
            foreach (var b in sorted)
                if (d <= b.maxDistance) return b.kind;
            return sorted[^1].kind;
        }

        for (int i = 0; i < rooms.Count; i++)
        {
            string roomKind = PickKind(roomDist[i]);
            var b = rooms[i].Bounds;

            for (int x = b.xMin; x < b.xMax; x++)
                for (int y = b.yMin; y < b.yMax; y++)
                {
                    if (!grid.InBounds(x, y)) continue;
                    var k = grid.Kind[x, y];
                    if (k == null || !k.StartsWith("floor", StringComparison.OrdinalIgnoreCase)) continue;

                    grid.Kind[x, y] = protectMask[x, y] ? corridorKind : roomKind;
                }
        }
    }
    #endregion
}