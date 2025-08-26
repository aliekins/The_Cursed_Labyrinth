using System;
using UnityEngine;

public class SwordRoomSolver : MonoBehaviour, ISpecialSolver
{
    public event Action OnSolved;

    [SerializeField] private int requiredSwords = 6;
    private int swordsUsed = 0;

    public int Required => requiredSwords;
    public int Used => swordsUsed;

    public void UseSword()
    {
        if (swordsUsed >= requiredSwords) return;
        swordsUsed++;

        if (swordsUsed >= requiredSwords)
        {
            Debug.Log("Sword room puzzle solved!");
            OnSolved?.Invoke();
        }
    }

    public void ResetProgress() => swordsUsed = 0;
}