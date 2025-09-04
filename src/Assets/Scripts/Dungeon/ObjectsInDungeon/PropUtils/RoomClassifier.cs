using System.Collections.Generic;
using UnityEngine;
/**
 * @file RoomClassifier.cs
 * @brief Classifies room cells into corners, walls, and interior.
 * @ingroup PropUtils
 */
public static class RoomClassifier
{
    public static void ClassifyRoomCells(DungeonGrid grid, RectInt bounds, List<Vector2Int> corners, List<Vector2Int> walls, List<Vector2Int> interior, System.Func<string, bool> isRoomFloor)
    {
        corners.Clear(); walls.Clear(); interior.Clear();

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                var k = grid.Kind[x, y];
                if (!isRoomFloor(k)) continue;

                int touches = 0;
                if (x == bounds.xMin) touches++;
                if (x == bounds.xMax - 1) touches++;
                if (y == bounds.yMin) touches++;
                if (y == bounds.yMax - 1) touches++;

                var c = new Vector2Int(x, y);
                if (touches >= 2) corners.Add(c);
                else if (touches == 1) walls.Add(c);
                else interior.Add(c);
            }
        }
    }
}