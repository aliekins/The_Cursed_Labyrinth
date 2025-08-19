using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DoorManager : MonoBehaviour
{
    [SerializeField] private Tilemap doorTilemap;

    [Header("Tier 0")]
    [SerializeField] private TileBase doorClosedTier0;
    [SerializeField] private TileBase doorOpenTier0;

    [Header("Tier 1")]
    [SerializeField] private TileBase doorClosedTier1;
    [SerializeField] private TileBase doorOpenTier1;

    [Header("Tier 2")]
    [SerializeField] private TileBase doorClosedTier2;
    [SerializeField] private TileBase doorOpenTier2;

    private readonly List<DoorRuntime> _doors = new();
    private int _currentTier = 0;

    public void Clear()
    {
        _doors.Clear();
        doorTilemap?.ClearAllTiles();
    }

    /// <summary>Spawn closed doors for each plan</summary>
    public void Spawn(List<DoorPlan> plans)
    {
        _doors.Clear();
        foreach (var plan in plans)
        {
            Vector3Int cell = new Vector3Int(plan.pos.x, plan.pos.y, 0);
            var runtime = new DoorRuntime(plan.requiredTier, cell, this);
            _doors.Add(runtime);
            doorTilemap.SetTile(cell, GetClosedTile(plan.requiredTier));
        }
    }

    /// <summary>Open all doors whose requiredTier <= maxTier</summary>
    public void UnlockUpTo(int maxTier)
    {
        foreach (var d in _doors)
            if (d.RequiredTier <= maxTier)
                d.Open();
    }

    internal TileBase GetClosedTile(int tier) => tier switch
    {
        0 => doorClosedTier0,
        1 => doorClosedTier1,
        2 => doorClosedTier2,
        _ => doorClosedTier0
    };

    internal TileBase GetOpenTile(int tier) => tier switch
    {
        0 => doorOpenTier0,
        1 => doorOpenTier1,
        2 => doorOpenTier2,
        _ => doorOpenTier0
    };

    internal void SetTile(Vector3Int cell, TileBase tile) => doorTilemap.SetTile(cell, tile);

    public void ClearDoors()
    {
        foreach (var d in _doors)
        {
            // Reset to ground by setting null; the visualizer renders floors/walls on other tilemaps.
            SetTile(d.Cell, null);
        }
        _doors.Clear();
    }

    public void BuildDoors(IEnumerable<(Vector3Int cell, int requiredTier)> defs)
    {
        ClearDoors();
        foreach (var (cell, tier) in defs)
        {
            var dr = new DoorRuntime(tier, cell, this);
            _doors.Add(dr);
            if (tier <= _currentTier) dr.Open(); else dr.Close();
        }
    }

    public void SetProgressTier(int tier)
    {
        if (tier == _currentTier) return;
        _currentTier = tier;
        UpdateForTier(_currentTier);
    }

    public void UpdateForTier(int tier)
    {
        foreach (var d in _doors)
        {
            if (d.RequiredTier <= tier) d.Open();
            else d.Close();
        }
    }
}