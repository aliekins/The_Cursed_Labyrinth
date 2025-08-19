/// \file GraphUtils.cs
/// \brief Minimal spanning tree over room centers.
using System.Collections.Generic;
using UnityEngine;

/// <summary>Graph helpers</summary>
public static class GraphUtils
{
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

    public static float[] ComputeRoomDistances(List<Room> rooms, List<(int a, int b)> edges, int startRoom)
    {
        int n = rooms.Count;
        var adj = new List<(int v, float w)>[n];
        for (int i = 0; i < n; i++) adj[i] = new();

        foreach (var (a, b) in edges)
        {
            float w = Vector2Int.Distance(rooms[a].Center, rooms[b].Center);
            adj[a].Add((b, w));
            adj[b].Add((a, w));
        }

        var dist = new float[n];
        for (int i = 0; i < n; i++) dist[i] = float.PositiveInfinity;

        // Dijkstra 
        var pq = new PriorityQueue<int, float>();
        dist[startRoom] = 0f; pq.Enqueue(startRoom, 0f);

        while (pq.Count > 0)
        {
            int u = pq.Dequeue();
            float du = dist[u];

            foreach (var (v, w) in adj[u])
            {
                float cand = du + w;
                if (cand < dist[v])
                {
                    dist[v] = cand;
                    pq.Enqueue(v, cand);
                }
            }
        }
        return dist;
    }
}