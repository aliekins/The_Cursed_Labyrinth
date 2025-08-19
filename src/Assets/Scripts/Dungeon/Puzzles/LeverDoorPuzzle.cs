/// \file LeverDoorRuntime.cs
/// \brief Simple lever puzzle: on interact, opens/closes a 1-tile doorway in the nearest wall along the room edge.
using UnityEngine;

public sealed class LeverDoorRuntime : MonoBehaviour, IPuzzleManager
{
    [Header("Interact")]
    [SerializeField] private float interactRadius = 1.2f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private PuzzlePlan plan;
    private DungeonController controller;
    private Vector2Int doorwayCell;
    private bool hasDoorway;
    private string wallKind = "wall";
    private string floorKindForDoor = "floor_corridor"; // use corridor floor for the gap

    public void Init(PuzzlePlan plan, DungeonController controller)
    {
        this.plan = plan;
        this.controller = controller;
        FindNearestWallCell();
    }

    private void Update()
    {
        if (controller == null) return;
        var player = controller.PlayerTransformOrNull();
        if (player == null) return;

        if (Vector3.Distance(player.position, transform.position) <= interactRadius &&
            Input.GetKeyDown(interactKey))
        {
            ToggleDoorway();
        }
    }

    private void FindNearestWallCell()
    {
        var grid = controller.Grid;
        var roomId = plan.roomId;
        int bestDist = int.MaxValue;

        // Check 4-neighbour ring expansions from anchor until we hit a wall owned by this room boundary
        for (int r = 1; r <= 8; r++)
        {
            // diamond perimeter
            for (int dx = -r; dx <= r; dx++)
            {
                int dy1 = r - Mathf.Abs(dx);
                int dy2 = -dy1;

                TryConsider(plan.cell.x + dx, plan.cell.y + dy1, r);
                TryConsider(plan.cell.x + dx, plan.cell.y + dy2, r);
            }

            void TryConsider(int x, int y, int dist)
            {
                if (!grid.InBounds(x, y)) return;

                // Doorway should be on a wall tile adjacent to this room
                if (grid.Kind[x, y] == wallKind && HasNeighbourRoomCell(grid, x, y, roomId))
                {
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        doorwayCell = new Vector2Int(x, y);
                    }
                }
            }

            if (bestDist != int.MaxValue) break;
        }
    }

    private static readonly Vector2Int[] N4 = { new(1, 0), new(-1, 0), new(0, 1), new(0, -1) };
    private static bool HasNeighbourRoomCell(DungeonGrid g, int x, int y, int roomId)
    {
        foreach (var d in N4)
        {
            int nx = x + d.x, ny = y + d.y;

            if (!g.InBounds(nx, ny)) continue;

            if (g.RoomId[nx, ny] == roomId) return true;
        }
        return false;
    }

    private void ToggleDoorway()
    {
        if (!controller.Grid.InBounds(doorwayCell.x, doorwayCell.y)) return;

        if (!hasDoorway)
        {
            controller.Grid.Kind[doorwayCell.x, doorwayCell.y] = floorKindForDoor;
            hasDoorway = true;
        }
        else
        {
            controller.Grid.Kind[doorwayCell.x, doorwayCell.y] = wallKind;
            hasDoorway = false;
        }

        controller.TilesSetDirtyAt(doorwayCell);
        Debug.Log($"[LeverDoor] Toggled doorway at {doorwayCell} -> {(hasDoorway ? "OPEN" : "CLOSED")}");
    }
}