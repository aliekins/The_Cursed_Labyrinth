using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class DoorRuntime
{
    public int RequiredTier { get; }
    public Vector3Int Cell { get; }
    private readonly DoorManager manager;
    private bool isOpen;

    public DoorRuntime(int requiredTier, Vector3Int cell, DoorManager manager)
    {
        RequiredTier = requiredTier;
        Cell = cell;
        this.manager = manager;
        isOpen = false;
    }

    public void Open()
    {
        if (isOpen) return;
        isOpen = true;
        manager.SetTile(Cell, manager.GetOpenTile(RequiredTier));
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        manager.SetTile(Cell, manager.GetClosedTile(RequiredTier));
    }
}