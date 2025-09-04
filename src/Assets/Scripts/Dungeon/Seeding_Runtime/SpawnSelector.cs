using UnityEngine;
using System.Collections.Generic;
/**
 * @file SpawnSelector.cs
 * @brief Picks a spawn cell farthest from the special room center.
 * @ingroup SeedingRT
 */
public class SpawnSelector : MonoBehaviour
{
    public static Vector2Int ChooseFarthestFrom(Vector2Int specialCenter, List<Room> rooms)
    {
        if (rooms == null || rooms.Count == 0) return specialCenter;

        float bestDist = float.NegativeInfinity;
        Vector2Int bestCell = specialCenter;

        foreach (var r in rooms)
        {
            // center of room as int cell
            var c = r.Bounds.center;
            var cell = new Vector2Int(Mathf.RoundToInt(c.x), Mathf.RoundToInt(c.y));
            var d = Vector2Int.Distance(cell, specialCenter);

            if (d > bestDist)
            {
                bestDist = d;
                bestCell = cell;
            }
        }

        return bestCell;
    }
}