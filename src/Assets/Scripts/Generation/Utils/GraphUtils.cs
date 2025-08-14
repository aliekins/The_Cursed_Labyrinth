/// \file GraphUtils.cs
/// \brief Minimal spanning tree over room centers.
using System.Collections.Generic;
using UnityEngine;

/// <summary>Graph helpers</summary>
public static class GraphUtils
{
    /// <summary>Build MST by Euclidean distance of room centers</summary>
    public static List<(int a, int b)> BuildMstByDistance(IReadOnlyList<Room> rooms)
    {
        int n = rooms.Count; var inTree = new bool[n]; var edges = new List<(int, int)>(n - 1);
        inTree[0] = true;
        for (int added = 1; added < n; added++)
        {
            float best = float.MaxValue; int bestA = -1, bestB = -1;
            for (int i = 0; i < n; i++) if (inTree[i])
                    for (int j = 0; j < n; j++) if (!inTree[j])
                        {
                            float d = Vector2Int.Distance(rooms[i].Center, rooms[j].Center);
                            if (d < best) { best = d; bestA = i; bestB = j; }
                        }
            edges.Add((bestA, bestB)); inTree[bestB] = true;
        }
        return edges;
    }
}