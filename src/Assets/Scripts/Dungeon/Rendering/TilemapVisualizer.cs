/// \file TilemapVisualizer.cs
/// \brief Renders a DungeonGrid to Unity Tilemaps (floors, walls), placing carpet as rectangular patches.
using System;
using UnityEngine.Tilemaps;
using UnityEngine;
using static UnityEngine.InputManagerEntry;

public sealed class TilemapVisualizer : MonoBehaviour
{
    [Header("Tilemaps")]
    [SerializeField] private Tilemap ground;
    [SerializeField] private Tilemap carpet;
    [SerializeField] private Tilemap walls;

    [Header("Database")]
    [SerializeField] private KindTile_DB db;

    [Header("Carpet Settings")]
    [Range(0f, 1f)][SerializeField] private float coverageRatio = 0.20f;
    [SerializeField] private int minPatchWidth = 2, minPatchHeight = 2;
    [SerializeField] private int maxPatchWidth = 6, maxPatchHeight = 6;
    [SerializeField] private int maxPatchAttempts = 500;
    [SerializeField] private int seed = 0;

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

    public void Render(DungeonGrid grid)
    {
        if (grid == null) throw new ArgumentNullException(nameof(grid));

        CarpetMask = BuildCarpetMask(grid); 
        RenderFloors(grid);
        RenderCarpets(grid, CarpetMask);
        RenderWalls(grid);
    }

    /// Returns the world-space center of a cell using the ground Tilemap
    public Vector3 CellCenterWorld(int x, int y)
    {
        if (ground == null) return new Vector3(x, y, 0f);
        return ground.GetCellCenterWorld(new Vector3Int(x, y, 0));
    }

    /// Returns the local-space center of a cell relative to the Grid (parent of the tilemaps)
    public Vector3 CellCenterLocal(int x, int y)
    {
        var grid = ground != null ? ground.layoutGrid : null;
        if (grid == null) return new Vector3(x + 0.5f, y + 0.5f, 0f);
        // Local center via Grid API; 0.5 puts us at cell center.
        return grid.CellToLocalInterpolated(new Vector3(x + 0.5f, y + 0.5f, 0));
    }

    /// Parent Transform of the Grid that owns the tilemaps
    public Transform GridTransform => ground != null ? ground.layoutGrid.transform : null;

    #region helpers
    private bool[,] BuildCarpetMask(DungeonGrid grid)
    {
        int W = grid.Width, H = grid.Height;
        var mask = new bool[W, H];

        int floorCount = CountFloors(grid);
        if (floorCount == 0 || coverageRatio <= 0f)
            return mask;

        int target = Mathf.Clamp(Mathf.RoundToInt(floorCount * coverageRatio), 1, floorCount);

        int minW = Mathf.Max(1, minPatchWidth);
        int minH = Mathf.Max(1, minPatchHeight);
        int maxW = Mathf.Max(minW, maxPatchWidth);
        int maxH = Mathf.Max(minH, maxPatchHeight);

        int painted = 0, attempts = 0;

        while (painted < target && attempts < maxPatchAttempts)
        {
            attempts++;
            painted += TryPaintRandomRect(grid, mask, minW, maxW, minH, maxH, target - painted);
        }

        return mask;
    }
    private int TryPaintRandomRect(DungeonGrid grid, bool[,] mask,
                               int minW, int maxW, int minH, int maxH, int quota)
    {
        int W = grid.Width, H = grid.Height;
        int rw = rng.Next(minW, maxW + 1);
        int rh = rng.Next(minH, maxH + 1);
        int rx = rng.Next(0, Mathf.Max(1, W - rw + 1));
        int ry = rng.Next(0, Mathf.Max(1, H - rh + 1));

        int added = 0;
        for (int x = rx; x < rx + rw; x++)
        {
            for (int y = ry; y < ry + rh; y++)
            {
                if (!IsFloor(grid.Kind[x, y])) continue;
                if (mask[x, y]) continue;

                mask[x, y] = true;
                added++;
                if (added >= quota) return added;
            }
        }
        return added;
    }
    private void RenderFloors(DungeonGrid grid)
    {
        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                if (!IsFloor(grid.Kind[x, y])) continue;
                if (!db.TryGet(grid.Kind[x, y], out var floorTile)) continue;

                var pos = new Vector3Int(x, y, 0);
                ground.SetTile(pos, floorTile);
            }
        }
    }
    private void RenderCarpets(DungeonGrid grid, bool[,] carpetMask)
    {
        if (!db.TryGet("carpet", out var carpetTile)) return;

        for (int x = 0; x < grid.Width; x++)
            for (int y = 0; y < grid.Height; y++)
            {
                if (!carpetMask[x, y]) continue;

                var p = new Vector3Int(x, y, 0);
                if (ground.HasTile(p))          
                    carpet.SetTile(p, carpetTile);
            }
    }
    private void RenderWalls(DungeonGrid grid)
    {
        if (!db.TryGet("wall", out var wallTile)) return;

        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                if (IsFloor(grid.Kind[x, y])) continue;
                if (TouchesFloor8(grid, x, y))
                    walls.SetTile(new Vector3Int(x, y, 0), wallTile);
            }
        }
    }
#endregion
    #region utilities
    private static bool IsFloor(string k) =>
        k != null && k.StartsWith("floor", StringComparison.OrdinalIgnoreCase);

    private static int CountFloors(DungeonGrid g)
    {
        int c = 0;
        for (int x = 0; x < g.Width; x++)
            for (int y = 0; y < g.Height; y++)
                if (IsFloor(g.Kind[x, y])) c++;
        return c;
    }

    private static readonly Vector2Int[] N8 = {
    new( 1, 0), new(-1, 0), new(0, 1), new(0,-1),
    new( 1, 1), new( 1,-1), new(-1, 1), new(-1,-1)
};

    private static bool TouchesFloor8(DungeonGrid g, int x, int y)
    {
        foreach (var d in N8)
        {
            int nx = x + d.x, ny = y + d.y;
            if (nx < 0 || ny < 0 || nx >= g.Width || ny >= g.Height) continue;
            if (IsFloor(g.Kind[nx, ny])) return true;
        }
        return false;
    }
    public float CellSize
    {
        get
        {
            var g = GridTransform?.GetComponent<Grid>();
            return g ? g.cellSize.x : 1f;
        }
    }
    #endregion
}