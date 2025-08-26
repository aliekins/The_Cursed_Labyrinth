using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public partial class DungeonController : MonoBehaviour
{
    [Header("Sequential Growth")]
    [SerializeField] private int sequentialRoomCount = 14;            // fallback if range == (0,0)
    [SerializeField] private Vector2Int randomRoomCountRange = new Vector2Int(20, 32);
    [SerializeField] private int maxPlacementTriesPerStep = 8;

    [Header("Openings and Branching")]
    [SerializeField] private bool shuffleOpenings = true;
    [SerializeField, Range(0f, 1f)] private float openingKeepChance = 0.75f;
    [SerializeField] private bool depthFirstLike = false;             // snakier feel when true

    [Header("Room Size Bias")]
    [SerializeField, Range(0f, 1f)] private float biasSmallRooms = 0.6f;
    [SerializeField, Range(0f, 1f)] private float largeRoomChance = 0.2f;

    [Header("Corridor Length (Growth)")]
    [SerializeField] private int minGrowCorridorLen = 2;
    [SerializeField] private int maxGrowCorridorLen = 6;

    [Header("Corridor Length Bias")]
    [SerializeField, Range(0f, 1f)] private float biasShortCorridors = 0.6f;

    [Header("Doors and Bends")]
    [SerializeField] private int doorInsetFromCorner = 1;             // keep off corners
    [SerializeField] private bool alignDoorToAnchor = true;           // try to align along entry edge
    [SerializeField] private int doorJitterMax = 3;                   // slide door along edge for variety

    // caches
    private RectInt specialRoomRectGrid;       // prefab footprint in GRID coords
    private Vector2Int entranceInsideGrid;     // prefab corridor tile
    private Vector2Int entranceOutsideGrid;    // one cell outside (y-1)
    private bool[,] noDigMask;                 // special

    #region build
    internal void GenerateFromSeed(BiomeSetup_SO profile, SpecialRoomSeeder.SeedInfo seedInfo)
    {
        if (!profile || seedInfo == null)
        {
            Debug.LogError("Seeded build: missing profile/seed");
            return;
        }
        biomeSetup = profile;

        if (rng == null) 
            rng = (randomSeed != 0) ? new System.Random(randomSeed) : new System.Random();

        // Point painter/visualizer at prefab tilemaps
        var painter = FindAnyObjectByType<BiomePainter>();
        if (painter)
            painter.OverrideTilemaps(seedInfo.ground, seedInfo.carpet, seedInfo.wall);

        // Offset: place prefab near middle/top (tile = grid + offset)
        var occTile = seedInfo.occupiedRectInt; // TILE coords of prefab
        var desiredMin = new Vector2Int(
            Mathf.Max(1, (profile.width - occTile.width) / 2),
            Mathf.Max(1, (profile.height - occTile.height) / 2)
        );

        var cellOffset = occTile.min - desiredMin;
        tmVisualizer.SetCellOffset(cellOffset);

        // Prefab footprint in GRID coords
        specialRoomRectGrid = new RectInt(desiredMin.x, desiredMin.y, occTile.width, occTile.height);

        // Grid and stamp actual prefab ground
        CreateGrid();
        rooms = new List<Room>();
        StampPrefabGroundAsFloor(seedInfo.ground);

        // Cache entrance 
        if (!TryCacheEntranceFromGround(seedInfo, out entranceInsideGrid))
        {
            entranceInsideGrid = new Vector2Int(specialRoomRectGrid.xMin + specialRoomRectGrid.width / 2, specialRoomRectGrid.yMin);
            Debug.LogWarning("[Seeded] No floor_corridor tile in prefab; using bottom-center inside.");
        }
        entranceOutsideGrid = new Vector2Int(entranceInsideGrid.x, entranceInsideGrid.y - 1);

        if (grid.InBounds(entranceInsideGrid.x, entranceInsideGrid.y))
            grid.Kind[entranceInsideGrid.x, entranceInsideGrid.y] = corridorKind;
        if (grid.InBounds(entranceOutsideGrid.x, entranceOutsideGrid.y))
            grid.Kind[entranceOutsideGrid.x, entranceOutsideGrid.y] = corridorKind;

        // Build masks for renderer and carving
        var exactWallMask = BuildExactWallMaskFromWall(seedInfo, grid.Width, grid.Height);
        tmVisualizer.SetPrefabSkipMask(exactWallMask);

        noDigMask = BuildNoDigMask(specialRoomRectGrid, grid.Width, grid.Height, 2); 

        // Allow the entrance seam through the mask
        if (grid.InBounds(entranceInsideGrid.x, entranceInsideGrid.y)) noDigMask[entranceInsideGrid.x, entranceInsideGrid.y] = false;
        if (grid.InBounds(entranceOutsideGrid.x, entranceOutsideGrid.y)) noDigMask[entranceOutsideGrid.x, entranceOutsideGrid.y] = false;

        // Grow from the seam (never upwards)
        GrowSequential(entranceOutsideGrid + Vector2Int.down, Vector2Int.down);

        CleanupDanglingCorridors();
    }
    #endregion
    #region growth
    private void GrowSequential(Vector2Int firstAnchor, Vector2Int firstDir)
    {
        var deque = new List<(Vector2Int anchor, Vector2Int dir)>();
        deque.Add((firstAnchor, firstDir));

        int targetCount = (randomRoomCountRange == Vector2Int.zero) ? sequentialRoomCount : Rand(randomRoomCountRange.x, randomRoomCountRange.y);

        int nextRoomId = 0;

        while (deque.Count > 0 && rooms.Count < targetCount)
        {
            var idx = depthFirstLike ? deque.Count - 1 : 0;
            var (anchor, dir) = deque[idx];

            deque.RemoveAt(idx);

            if (dir == Vector2Int.up) continue; 

            bool placed = false;
            for (int attempt = 0; attempt < maxPlacementTriesPerStep && !placed; attempt++)
            {
                int corridorLen = SampleCorridorLen();
                int rw = SampleRoomSize();
                int rh = SampleRoomSize();

                // proposed room center straight down the corridor
                var roomCenter = anchor + dir * (corridorLen + rh / 2 + 1);
                var rect = CenterToRect(roomCenter, rw, rh);

                // Validate with margin
                if (!RectInsideWithBorder(rect, width, height, 2)) continue;
                if (rect.Overlaps(specialRoomRectGrid)) continue;
                if (!grid.IsRectClearWithMargin(rect, 2)) continue;

                // pick an entry door on the opposite edge, try align to anchor + jitter
                var (doorInside, doorOutside) = PickEntryDoor(rect, dir, anchor);

                // route corridor to the door-outside while avoiding: special room (and surrounding) + existing rooms + new room interior
                var path = FindCorridorPath(anchor, doorOutside, rect);
                if (path == null || path.Count == 0) continue;

                // carve the path, punch the doorway seam
                CarvePath(path, corridorKind);
                if (grid.InBounds(doorOutside.x, doorOutside.y))
                    grid.Kind[doorOutside.x, doorOutside.y] = corridorKind;
                if (grid.InBounds(doorInside.x, doorInside.y)) 
                    grid.Kind[doorInside.x, doorInside.y] = corridorKind;
                if (grid.InBounds(anchor.x, anchor.y))
                    grid.Kind[anchor.x, anchor.y] = corridorKind;

                // if start == goal (path.Count == 0) the seam is isolated; extend 1 step away from the room
                if (path.Count == 0)
                {
                    var extend = doorOutside - dir; // step opposite to entry direction
                    if (grid.InBounds(extend.x, extend.y) &&
                        !IsRoomFloor(grid.Kind[extend.x, extend.y]) &&
                        (noDigMask == null || !noDigMask[extend.x, extend.y]))
                    {
                        grid.Kind[extend.x, extend.y] = corridorKind;
                    }
                }

                // carve the room
                string floorK = biomeSetup ? biomeSetup.floorKind : "floor_entry";
                grid.CarveRoom(rect, floorK);

                // register
                var room = new Room(nextRoomId++, rect);
                rooms.Add(room);
                RoomReady?.Invoke(room);

                // enqueue outgoing openings (L/R/Down only)
                foreach (var f in OpeningsFor(rect, dir))
                {
                    if (depthFirstLike && rng.NextDouble() < 0.6) deque.Add(f);
                    else deque.Insert(0, f);
                }

                placed = true;
            }
        }
    }

    private IEnumerable<(Vector2Int anchor, Vector2Int dir)> OpeningsFor(RectInt rect, Vector2Int cameDir)
    {
        var list = new List<(Vector2Int, Vector2Int)>(3);

        var downIn = new Vector2Int(rect.xMin + rect.width / 2, rect.yMin);
        var leftIn = new Vector2Int(rect.xMin, rect.yMin + rect.height / 2);
        var rightIn = new Vector2Int(rect.xMax - 1, rect.yMin + rect.height / 2);

        if (cameDir != Vector2Int.down)
            list.Add((downIn + Vector2Int.down, Vector2Int.down));
        if (cameDir != Vector2Int.left)
            list.Add((leftIn + Vector2Int.left, Vector2Int.left));
        if (cameDir != Vector2Int.right)
            list.Add((rightIn + Vector2Int.right, Vector2Int.right));

        if (shuffleOpenings) Shuffle(list, rng);

        foreach (var f in list)
            if (rng.NextDouble() < openingKeepChance)
                yield return f;
    }
    #endregion
    #region corridour routing
    private List<Vector2Int> FindCorridorPath(Vector2Int start, Vector2Int goal, RectInt forbiddenRect)
    {
        if (start == goal) return new List<Vector2Int>();

        var q = new Queue<Vector2Int>();
        var seen = new bool[grid.Width, grid.Height];
        var came = new Dictionary<Vector2Int, Vector2Int>();

        bool InForbidden(int x, int y) => x >= forbiddenRect.xMin && x < forbiddenRect.xMax && y >= forbiddenRect.yMin && y < forbiddenRect.yMax;

        if (!grid.InBounds(start.x, start.y) || !grid.InBounds(goal.x, goal.y))
            return null;

        seen[start.x, start.y] = true;
        q.Enqueue(start);

        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };

        while (q.Count > 0)
        {
            var u = q.Dequeue();
            if (u == goal) break;

            for (int i = 0; i < 4; i++)
            {
                int nx = u.x + dx[i];
                int ny = u.y + dy[i];

                if (!grid.InBounds(nx, ny)) continue;
                if (seen[nx, ny]) continue;

                // allow the goal even if "blocked"
                if (!(nx == goal.x && ny == goal.y))
                {
                    if (InForbidden(nx, ny)) continue;
                    if (noDigMask != null && noDigMask[nx, ny]) continue; 

                    var k = grid.Kind[nx, ny];
                    if (IsRoomFloor(k)) continue;
                }

                seen[nx, ny] = true;
                var v = new Vector2Int(nx, ny);
                came[v] = u;
                q.Enqueue(v);
            }
        }

        if (!came.ContainsKey(goal))
            return null;

        var path = new List<Vector2Int>();

        for (var c = goal; c != start; c = came[c])
            path.Add(c);

        path.Reverse();
        return path;
    }
    private void CarvePath(List<Vector2Int> path, string kind)
    {
        if (path == null) return;
        foreach (var c in path)
            if (grid.InBounds(c.x, c.y)) grid.Kind[c.x, c.y] = kind;
    }
    #endregion
    #region helpers
    private int SampleRoomSize()
    {
        int a = Rand(minRoomSize, maxRoomSize);

        if (rng.NextDouble() < biasSmallRooms)
            a = Mathf.Min(a, Rand(minRoomSize, maxRoomSize));
        if (rng.NextDouble() < largeRoomChance)
            a = Rand((minRoomSize + maxRoomSize) / 2, maxRoomSize);

        return a;
    }

    private int SampleCorridorLen()
    {
        int a = Rand(minGrowCorridorLen, maxGrowCorridorLen);

        if (rng.NextDouble() < biasShortCorridors)
            a = Mathf.Min(a, Rand(minGrowCorridorLen, maxGrowCorridorLen));

        return a;
    }

    private static void Shuffle<T>(IList<T> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private (Vector2Int inside, Vector2Int outside) PickEntryDoor(RectInt rect, Vector2Int dir, Vector2Int anchor)
    {
        Vector2Int inside, outside, deltaOutside = -dir;

        if (dir == Vector2Int.down || dir == Vector2Int.up)
        {
            int yInside = (dir == Vector2Int.down) ? rect.yMax - 1 : rect.yMin;
            int xMin = rect.xMin + doorInsetFromCorner;
            int xMax = rect.xMax - 1 - doorInsetFromCorner;
            int xDoor = alignDoorToAnchor ? Mathf.Clamp(anchor.x, xMin, xMax)
                                          : rect.xMin + rect.width / 2;

            if (doorJitterMax > 0) 
                xDoor = Mathf.Clamp(xDoor + Rand(-doorJitterMax, doorJitterMax), xMin, xMax);

            inside = new Vector2Int(xDoor, yInside);
        }
        else
        {
            int xInside = (dir == Vector2Int.left) ? rect.xMax - 1 : rect.xMin;
            int yMin = rect.yMin + doorInsetFromCorner;
            int yMax = rect.yMax - 1 - doorInsetFromCorner;
            int yDoor = alignDoorToAnchor ? Mathf.Clamp(anchor.y, yMin, yMax)
                                          : rect.yMin + rect.height / 2;

            if (doorJitterMax > 0) 
                yDoor = Mathf.Clamp(yDoor + Rand(-doorJitterMax, doorJitterMax), yMin, yMax);

            inside = new Vector2Int(xInside, yDoor);
        }

        outside = inside + deltaOutside;
        return (inside, outside);
    }

    private bool[,] BuildNoDigMask(RectInt r, int W, int H, int halo)
    {
        var m = new bool[W, H];
        if (r.width <= 0 || r.height <= 0) return m;

        var ex = new RectInt(r.xMin - halo, r.yMin - halo, r.width + 2 * halo, r.height + 2 * halo);
        int xmin = Mathf.Max(0, ex.xMin), ymin = Mathf.Max(0, ex.yMin);
        int xmax = Mathf.Min(W, ex.xMax), ymax = Mathf.Min(H, ex.yMax);

        for (int x = xmin; x < xmax; x++)
            for (int y = ymin; y < ymax; y++)
                m[x, y] = true;

        return m;
    }

    private static RectInt CenterToRect(Vector2Int center, int w, int h)
    {
        int x = center.x - w / 2;
        int y = center.y - h / 2;

        return new RectInt(x, y, w, h);
    }

    private static bool RectInsideWithBorder(RectInt r, int W, int H, int margin) => r.xMin - margin >= 0 && r.yMin - margin >= 0 && r.xMax + margin <= W && r.yMax + margin <= H;

    private int Rand(int a, int b) => (a <= b) ? rng.Next(a, b + 1) : a;

    private bool IsRoomFloor(string k)
    {
        if (string.IsNullOrEmpty(k)) return false;
        if (string.Equals(k, "wall", StringComparison.OrdinalIgnoreCase)) return false;
        if (string.Equals(k, corridorKind, StringComparison.OrdinalIgnoreCase)) return false; // corridors allowed

        return true; // floor_entry, floor_prefab, carpets, etc. are rooms
    }

    private bool TryCacheEntranceFromGround(SpecialRoomSeeder.SeedInfo seed, out Vector2Int insideGrid)
    {
        insideGrid = default;

        if (!tmVisualizer || seed.ground == null) return false;

        var bounds = seed.ground.cellBounds;
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                var tc = new Vector3Int(x, y, 0);
                if (!seed.ground.HasTile(tc)) continue;

                if (!tmVisualizer.TryGetTileForKind(corridorKind, out var corridorTile)) continue;

                var t = seed.ground.GetTile(tc);
                if (t == corridorTile)
                {
                    insideGrid = tmVisualizer.TileToGridCell(tc); // TILE -> GRID
                    return true;
                }
            }
        }
        return false;
    }

    private void StampPrefabGroundAsFloor(Tilemap prefabGround)
    {
        if (prefabGround == null) return;

        var b = prefabGround.cellBounds;

        for (int x = b.xMin; x < b.xMax; x++)
        {
            for (int y = b.yMin; y < b.yMax; y++)
            {
                var tc = new Vector3Int(x, y, 0);

                if (!prefabGround.HasTile(tc)) continue;

                var g = tmVisualizer.TileToGridCell(tc);

                if (grid.InBounds(g.x, g.y))
                    grid.Kind[g.x, g.y] = "floor_prefab";
            }
        }
    }

    private bool[,] BuildExactWallMaskFromWall(SpecialRoomSeeder.SeedInfo seed, int W, int H)
    {
        var mask = new bool[W, H];
        if (seed.wall == null) 
            return mask;

        var b = seed.wall.cellBounds;
        for (int x = b.xMin; x < b.xMax; x++)
        {
            for (int y = b.yMin; y < b.yMax; y++)
            {
                var tc = new Vector3Int(x, y, 0);

                if (!seed.wall.HasTile(tc)) continue;

                var g = tmVisualizer.TileToGridCell(tc);

                if (g.x >= 0 && g.y >= 0 && g.x < W && g.y < H)
                    mask[g.x, g.y] = true;
            }
        }
        return mask;
    }

    private void CleanupDanglingCorridors()
    {
        bool changed;
        do
        {
            changed = false;
            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    if (!string.Equals(grid.Kind[x, y], corridorKind, StringComparison.OrdinalIgnoreCase)) continue;

                    int c = 0; 
                    bool roomAdj = false;

                    void Check(int ix, int iy)
                    {
                        if (!grid.InBounds(ix, iy)) return;

                        var k = grid.Kind[ix, iy];

                        if (string.Equals(k, corridorKind, StringComparison.OrdinalIgnoreCase))
                            c++;
                        else if (IsRoomFloor(k))
                            roomAdj = true;
                    }
                    Check(x + 1, y);
                    Check(x - 1, y); 
                    Check(x, y + 1); 
                    Check(x, y - 1);

                    if (c == 0 && !roomAdj)
                    {
                        grid.Kind[x, y] = "wall";
                        changed = true;
                    }

                }
            }
        } while (changed);
    }
    #endregion
}