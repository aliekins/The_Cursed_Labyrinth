using System;
using UnityEngine;
/**
 * @file SwordRoomSolver.cs
 * @brief Counter puzzle: feed N swords to open the door.
 * @ingroup Puzzle
 */
public sealed class SwordRoomSolver : MonoBehaviour, ISpecialSolver
{
    public event Action OnSolved;

    [Header("Puzzle")]
    [SerializeField] private int requiredSwords = 6;

    public int Required => requiredSwords;
    public int Used { get; private set; }
    public bool IsSolved => Used >= Required;

    public void UseSword()
    {
        if (IsSolved) return;
        Used++;

        if (IsSolved)
        {
            Debug.Log("[SwordRoomSolver] SOLVED");
            OnSolved?.Invoke();
        }
    }

    public void ResetProgress() => Used = 0;
}
