using System;
using UnityEngine;

public class SwordRoomSolver : MonoBehaviour, ISpecialSolver
{
    public event Action OnSolved;

    private int swordsUsed = 0;

    public void UseSword()
    {
        swordsUsed++;
        if (swordsUsed >= 4)
        {
            Debug.Log("Sword room puzzle solved!");
            OnSolved?.Invoke();
        }
    }
}