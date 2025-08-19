/// \file Room.cs
/// \brief Carved room representation with bounds and center.
using UnityEngine;

public sealed class Room
{
    public int Id { get; }
    public RectInt Bounds { get; }
    public Vector2Int Center { get; }
    public Room(int id, RectInt bounds)
    {
        Id = id; Bounds = bounds;
        Center = new Vector2Int(bounds.x + bounds.width / 2, bounds.y + bounds.height / 2);
    }
}