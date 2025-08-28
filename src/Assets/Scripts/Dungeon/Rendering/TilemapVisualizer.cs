using System;
using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class TilemapVisualizer : MonoBehaviour
{
    [Header("Tilemaps")]
    [SerializeField] private Tilemap ground;
    [SerializeField] private Tilemap carpet;
    [SerializeField] private Tilemap walls;

    [Header("Database")]
    [SerializeField] private KindTile_DB db;

    [Header("Carpet Settings")]
    [Range(0f, 1f)][SerializeField] private float coverageRatio = 0.60f;
    [SerializeField] private int minPatchWidth = 1, minPatchHeight = 1;
    [SerializeField] private int maxPatchWidth = 4, maxPatchHeight = 4;
    [SerializeField] private int maxPatchAttempts = 500;
    [Tooltip("Use a fixed seed for reproducible masks; 0 = random each run.")]
    [SerializeField] private int seed = 0;

    [Header("Carpet Mask Controls")]
    [Tooltip("If true, keep an existing CarpetMask instead of rebuilding each Render().")]
    [SerializeField] private bool preserveCarpetMask = true;
    [Tooltip("Skip painting carpet on cells whose kind is corridor-like.")]
    [SerializeField] private bool excludeCorridorsFromCarpet = true;
    [Tooltip("Skip painting carpet for any cell inside the special-room bounds.")]
    [SerializeField] private bool excludePrefabFromCarpet = true;

    // placement offset: tile = grid + cellOffset
    [SerializeField] private Vector2Int cellOffset = Vector2Int.zero;
    private bool[,] prefabSkipMask;
    public void SetPrefabSkipMask(bool[,] mask) => prefabSkipMask = mask;
    public void SetCellOffset(Vector2Int off) => cellOffset = off;
    public Vector2Int TileToGridCell(Vector3Int tileCell)
        => new Vector2Int(tileCell.x - cellOffset.x, tileCell.y - cellOffset.y);

    [SerializeField] private bool limitPrefabSkipToBounds = true;
    private RectInt prefabBoundsGrid;
    private bool hasPrefabBounds = false;

    public bool TryGetTileForKind(string kind, out TileBase tile)
    {
        if (db == null)
        { 
            tile = null; 
            return false;
        }
        return db.TryGet(kind, out tile);
    }
    public void SetPrefabBounds(RectInt boundsGrid)
    { 
        prefabBoundsGrid = boundsGrid;
        hasPrefabBounds = true;
    }

    public Transform GridTransform => ground ? ground.layoutGrid.transform : null;

    public bool[,] CarpetMask { get; private set; }

    private System.Random rng;

    private void Awake()
    {
        rng = (seed != 0) ? new System.Random(seed) : new System.Random();
    }

    public void Clear()
    {
        ground?.ClearAllTiles();
        carpet?.ClearAllTiles();
        walls?.ClearAllTiles();
    }

    /// Force the next Render() to rebuild the carpet mask.
    public void InvalidateCarpetMask() => CarpetMask = null;

    /// Supply an external carpet mask (must match grid size).
    public void SetCarpetMask(bool[,] mask) => CarpetMask = mask;

    public void Render(DungeonGrid grid)
    {
        if (grid == null) throw new ArgumentNullException(nameof(grid));

        // Build the mask only if we don't preserve or dimensions changed
        if (!preserveCarpetMask || !MaskMatchesGrid(CarpetMask, grid))
            CarpetMask = BuildCarpetMask(grid);

        RenderFloors(grid);
        RenderCarpets(grid, CarpetMask);
        RenderWalls(grid);
    }

    public Vector3 CellCenterLocal(int x, int y)
    {
        var g = GridTransform ? GridTransform.GetComponent<Grid>() : null;
        var c = new Vector3Int(x + cellOffset.x, y + cellOffset.y, 0);
        return g ? g.GetCellCenterLocal(c) : new Vector3(c.x + 0.5f, c.y + 0.5f, 0);
    }
    public Vector3 CellCenterWorld(int x, int y)
    {
        var local = CellCenterLocal(x, y); // already includes cellOffset
        return GridTransform ? GridTransform.TransformPoint(local) : local;
    }

    // helpers
    private static bool IsFloor(string k) =>
        !string.IsNullOrEmpty(k) && k.StartsWith("floor", StringComparison.OrdinalIgnoreCase);

    private static bool IsPrefabFloor(string k) =>
        !string.IsNullOrEmpty(k) && k.StartsWith("floor_prefab", StringComparison.OrdinalIgnoreCase);

    private static bool IsCorridorLike(string k)
    {
        if (string.IsNullOrEmpty(k)) 
            return false;
        if (k.IndexOf("corridor", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        return k.StartsWith("floor_corridor", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TouchesFloor8(DungeonGrid g, int x, int y)
    {
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                int nx = x + dx;
                int ny = y + dy;

                if (!g.InBounds(nx, ny)) continue;
                if (IsFloor(g.Kind[nx, ny])) return true;
            }
        return false;
    }
  
    private bool InPrefabHalo(int x, int y)
    {
        if (!hasPrefabBounds || !limitPrefabSkipToBounds) return false;

        var b = new RectInt(prefabBoundsGrid.xMin, prefabBoundsGrid.yMin,
                            prefabBoundsGrid.width, prefabBoundsGrid.height);
        return b.Contains(new Vector2Int(x, y));
    }

    private static bool MaskMatchesGrid(bool[,] mask, DungeonGrid grid)
        => mask != null && mask.GetLength(0) == grid.Width && mask.GetLength(1) == grid.Height;

    private int CountFloors(DungeonGrid g, bool includePrefab = true, bool includeCorridors = true)
    {
        int c = 0;
        for (int x = 0; x < g.Width; x++)
            for (int y = 0; y < g.Height; y++)
            {
                var k = g.Kind[x, y];

                if (!IsFloor(k)) continue;
                if (!includePrefab && IsPrefabFloor(k)) continue;
                if (!includeCorridors && IsCorridorLike(k)) continue;
                c++;
            }
        return c;
    }

    private bool[,] BuildCarpetMask(DungeonGrid grid)
    {
        int W = grid.Width, H = grid.Height;
        var mask = new bool[W, H];

        // Only place carpet on "normal" floors (no prefab, no corridor)
        int floorCount = CountFloors(grid, includePrefab: !excludePrefabFromCarpet, includeCorridors: !excludeCorridorsFromCarpet);

        if (floorCount == 0 || coverageRatio <= 0f) return mask;

        int target = Mathf.Clamp(Mathf.RoundToInt(floorCount * coverageRatio), 1, floorCount);

        int minW = Mathf.Max(1, minPatchWidth);
        int minH = Mathf.Max(1, minPatchHeight);
        int maxW = Mathf.Max(minW, maxPatchWidth);
        int maxH = Mathf.Max(minH, maxPatchHeight);

        int painted = 0, attempts = 0;

        while (painted < target && attempts++ < maxPatchAttempts)
        {
            int rw = rng.Next(minW, maxW + 1);
            int rh = rng.Next(minH, maxH + 1);
            int rx = rng.Next(0, Mathf.Max(1, W - rw + 1));
            int ry = rng.Next(0, Mathf.Max(1, H - rh + 1));

            for (int x = rx; x < rx + rw && painted < target; x++)
                for (int y = ry; y < ry + rh && painted < target; y++)
                {
                    var k = grid.Kind[x, y];
                    if (!IsFloor(k)) continue;
                    if (excludePrefabFromCarpet && IsPrefabFloor(k)) continue;
                    if (excludeCorridorsFromCarpet && IsCorridorLike(k)) continue;
                    if (InPrefabHalo(x, y)) continue; // keep seam area clear

                    if (mask[x, y]) continue;
                    mask[x, y] = true;
                    painted++;
                }
        }

        return mask;
    }

    private void RenderFloors(DungeonGrid grid)
    {
        for (int x = 0; x < grid.Width; x++)
            for (int y = 0; y < grid.Height; y++)
            {
                var kind = grid.Kind[x, y];
                if (!IsFloor(kind)) continue;
                if (!db.TryGet(kind, out var tile)) continue;

                var pos = new Vector3Int(x + cellOffset.x, y + cellOffset.y, 0);
                ground.SetTile(pos, tile);
            }
    }

    private void RenderCarpets(DungeonGrid grid, bool[,] mask)
    {
        if (mask == null) return;
        if (!db.TryGet("carpet", out var t)) return;

        for (int x = 0; x < grid.Width; x++)
            for (int y = 0; y < grid.Height; y++)
            {
                if (!mask[x, y]) continue;
                var pos = new Vector3Int(x + cellOffset.x, y + cellOffset.y, 0);
                if (ground.HasTile(pos)) carpet.SetTile(pos, t);
            }
    }

    private void RenderWalls(DungeonGrid grid)
    {
        if (!db.TryGet("wall", out var wallTile)) return;

        for (int x = 0; x < grid.Width; x++)
            for (int y = 0; y < grid.Height; y++)
            {
                var kind = grid.Kind[x, y];
                // only draw walls into non-floor cells that touch a floor
                if (!string.IsNullOrEmpty(kind) && kind.StartsWith("floor", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                //if (!TouchesFloor4(grid, x, y)) continue;
                if (!TouchesFloor8(grid, x, y)) continue;

                if (prefabSkipMask != null &&
                    x >= 0 && y >= 0 &&
                    x < prefabSkipMask.GetLength(0) &&
                    y < prefabSkipMask.GetLength(1) &&
                    prefabSkipMask[x, y]) continue;

                var pos = new Vector3Int(x + cellOffset.x, y + cellOffset.y, 0);
                walls.SetTile(pos, wallTile);
            }
    }


    public float CellSize
    {
        get
        {
            var g = GridTransform?.GetComponent<Grid>();
            return g ? g.cellSize.x : 1f;
        }
    }
}