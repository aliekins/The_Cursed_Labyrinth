/// \file PlayerRoomTracker.cs
/// \brief Detects when the player crosses into a different room and notifies the controller.
using UnityEngine;

public sealed class PlayerRoomTracker : MonoBehaviour
{
    [SerializeField] private DungeonController controller;

    public void SetController(DungeonController c) => controller = c;

    private DungeonGrid grid;
    private int currentRoom = int.MinValue;

    private void Start() => grid = controller ? controller.Grid : null;

    private void LateUpdate()
    {
        if (controller == null || grid == null) return;

        Vector2Int cell = controller.ToCell(transform.position);
        if (!grid.InBounds(cell.x, cell.y)) return;

        int rid = grid.RoomId[cell.x, cell.y]; // -1 = corridor
        if (rid != currentRoom)
        {
            currentRoom = rid;
            controller.NotifyRoomEntered(rid);
        }
    }
}