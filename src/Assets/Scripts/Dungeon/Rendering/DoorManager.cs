using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DoorManager : MonoBehaviour
{
    [Header("Tilemap")]
    [SerializeField] private Tilemap doorTilemap;

    [Header("Door Rules (RuleAssets only)")]
    [SerializeField] private TileRuleAsset doorClosedEntryRule;    // tier 0
    [SerializeField] private TileRuleAsset doorClosedGroveRule;    // tier 1
    [SerializeField] private TileRuleAsset doorClosedQuarryRule;   // tier 2
    [SerializeField] private TileRuleAsset doorOpenRule;           // open for all tiers

    private TileBase _closed0;
    private TileBase _closed1;
    private TileBase _closed2;
    private TileBase _open;

    private readonly List<DoorRuntime> _doors = new();
    private readonly List<Vector2Int> _doorCells = new(); 

    private void Awake()
    {
        _closed0 = CreateTileFromRule(doorClosedEntryRule, colliderOn: true);
        _closed1 = CreateTileFromRule(doorClosedGroveRule, colliderOn: true);
        _closed2 = CreateTileFromRule(doorClosedQuarryRule, colliderOn: true);
        _open = CreateTileFromRule(doorOpenRule, colliderOn: false);

        if (!doorTilemap)
        {
            Debug.LogError("DoorManager: Assign a Door Tilemap in the Inspector.");
        }
    }

    private static TileBase CreateTileFromRule(TileRuleAsset rule, bool colliderOn)
    {
        if (!rule) return null;

        var t = ScriptableObject.CreateInstance<Tile>();
        if (rule.sprites != null && rule.sprites.Length > 0)
        {
            t.sprite = rule.sprites[0];
        }
        t.colliderType = colliderOn ? Tile.ColliderType.Sprite : Tile.ColliderType.None;
        return t;
    }
    #region public API
    public void Clear()
    {
        _doors.Clear();
        _doorCells.Clear();
        doorTilemap?.ClearAllTiles();
    }

    /// <summary>Spawn closed doors for each plan</summary>
    public void Spawn(List<DoorPlan> plans)
    {
        _doors.Clear();
        _doorCells.Clear();

        if (!doorTilemap) return;

        foreach (var plan in plans)
        {
            var cell = new Vector3Int(plan.pos.x, plan.pos.y, 0);
            _doorCells.Add(plan.pos);

            var runtime = new DoorRuntime(plan.requiredTier, cell, this);
            _doors.Add(runtime);

            doorTilemap.SetTile(cell, GetClosedTile(plan.requiredTier));
        }
    }

    /// <summary>Open all doors whose requiredTier <= maxTier</summary>
    public void UnlockUpTo(int maxTier)
    {
        foreach (var d in _doors)
        {
            if (d.RequiredTier <= maxTier)
            {
                d.Open();
            }
        }
    }

    /// <summary>Force-open a specific door cell (e.g., puzzle key), regardless of tier</summary>
    public void ForceOpenAt(Vector2Int cell)
    {
        var v3 = new Vector3Int(cell.x, cell.y, 0);
        doorTilemap.SetTile(v3, _open ?? _closed0 ?? _closed1 ?? _closed2);
        for (int i = 0; i < _doors.Count; i++)
        {
            if (_doors[i].Cell == v3)
            {
                _doors[i].Open();
                break;
            }
        }
    }

    public IReadOnlyList<Vector2Int> AllDoorCells => _doorCells;
    #endregion

    internal TileBase GetClosedTile(int tier)
    {
        return tier switch
        {
            0 => _closed0 ?? _closed1 ?? _closed2 ?? _open,
            1 => _closed1 ?? _closed0 ?? _closed2 ?? _open,
            2 => _closed2 ?? _closed1 ?? _closed0 ?? _open,
            _ => _closed0 ?? _closed1 ?? _closed2 ?? _open
        };
    }

    internal TileBase GetOpenTile(int _tierIgnored) 
    {
        return _open ?? _closed0 ?? _closed1 ?? _closed2;
    }

    internal void SetTile(Vector3Int cell, TileBase tile)
    {
        if (doorTilemap) doorTilemap.SetTile(cell, tile);
    }
}