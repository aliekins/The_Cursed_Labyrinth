using System;
using UnityEngine;

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
