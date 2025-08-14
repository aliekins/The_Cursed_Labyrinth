/// \file IRoomCarver.cs
/// \brief Interface for placing a room inside a BSP leaf area.
using UnityEngine;
#nullable enable

public interface IRoomCarver
{
    /// <summary>
    /// Create a room rect inside <paramref name="leafArea"/> or return <c>null</c> to skip
    /// </summary>
    /// <param name="leafArea">Leaf area</param>
    /// <param name="minRoomSize"></param>
    /// <param name="maxRoomSize"></param>
    /// <param name="rng">Random</param>
    /// <returns>Room rectangle, or <c>null</c> if no room should be carved</returns>
    RectInt? CreateRoom(RectInt leafArea, int minRoomSize, int maxRoomSize, System.Random rng);
}