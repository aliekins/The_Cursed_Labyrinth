/// \file AStarPathfinder.cs
/// \brief Four-neighbour A* pathfinding on a boolean grid.
using System.Collections.Generic;
using UnityEngine;
#nullable enable

/// <summary>
/// Works on a boolean[,] grid - true = walkable; false = blocked
/// </summary>
public static class AStarPathfinder
{
    private static readonly Vector2Int[] NeighbourOffsets =
    {
        new Vector2Int(1, 0),  // right
        new Vector2Int(-1, 0), // left
        new Vector2Int(0, 1),  // up
        new Vector2Int(0, -1)  // down
    };

    /// <summary>
    /// Finds the shortest path between <paramref name="startPosition"/> and <paramref name="goalPosition"/>
    /// </summary>
    /// <param name="walkableGrid">2D grid</param>
    /// <param name="startPosition">Start coordinates on the grid</param>
    /// <param name="goalPosition">Goal coordinates on the grid</param>
    /// <returns>List of positions from start to goal (inclusive). Empty if no path exists.</returns>
    public static List<Vector2Int> FindPath(bool[,] walkableGrid, Vector2Int startPosition, Vector2Int goalPosition)
    {
        int gridWidth = walkableGrid.GetLength(0);
        int gridHeight = walkableGrid.GetLength(1);

        // Nodes to be evaluated
        var openSet = new PriorityQueue<Vector2Int, int>();

        // Map of nodes - previous node (to reconstruct the path)
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();

        // Map of nodes - distance from start
        var dFromStart = new Dictionary<Vector2Int, int>();

        // Manhattan distance between two points
        int Heuristic(Vector2Int a) => Mathf.Abs(a.x - goalPosition.x) + Mathf.Abs(a.y - goalPosition.y);

        dFromStart[startPosition] = 0;
        openSet.Enqueue(startPosition, Heuristic(startPosition));

        // Main A* loop
        while (openSet.Count > 0)
        {
            var currentPosition = openSet.Dequeue();

            // Goal reached - reconstruct and return found path
            if (currentPosition == goalPosition)
                return ReconstructPath(cameFrom, currentPosition);

            // Explore all four neighbours
            foreach (var offset in NeighbourOffsets)
            {
                var neighbour = currentPosition + offset;

                // Skip neighbours outside the grid
                if (neighbour.x < 0 || neighbour.y < 0 || neighbour.x >= gridWidth || neighbour.y >= gridHeight) continue;

                // Skip blocked cells
                if (!walkableGrid[neighbour.x, neighbour.y]) continue;

                // Check possible step
                int potentialPathLength = dFromStart[currentPosition] + 1;

                // If this path is shorter or neighbour has not been visited yet - update
                if (!dFromStart.TryGetValue(neighbour, out int existingGCost) || potentialPathLength < existingGCost)
                {
                    dFromStart[neighbour] = potentialPathLength;
                    cameFrom[neighbour] = currentPosition;

                    int estimatedTotalD = potentialPathLength + Heuristic(neighbour);
                    openSet.Enqueue(neighbour, estimatedTotalD);
                }
            }
        }

        // No path found 
        return new List<Vector2Int>();
    }

    /// <summary>
    /// Reconstructs the path from the start to the current position
    /// </summary>
    private static List<Vector2Int> ReconstructPath(
        Dictionary<Vector2Int, Vector2Int> cameFrom,
        Vector2Int currentPosition)
    {
        var path = new List<Vector2Int> { currentPosition };

        // Step back until we reach the start
        while (cameFrom.TryGetValue(currentPosition, out var previousPosition))
        {
            currentPosition = previousPosition;
            path.Add(currentPosition);
        }

        path.Reverse();
        return path;
    }
}