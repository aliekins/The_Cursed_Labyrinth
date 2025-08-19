/// \file PuzzleTypes.cs
/// \brief Data-only puzzle enums and plan DTO
using UnityEngine;

public enum PuzzleType { LeverDoor, PressurePlate, SlidingBlock }

[System.Serializable]
public struct PuzzlePlan
{
    public int roomId;            ///< Room index this puzzle belongs to
    public PuzzleType type;       ///< Type of puzzle to spawn
    public Vector2Int cell;       ///< Anchor cell within the room
    public string biomeKind;      ///< Room biome kind (theming)
}
