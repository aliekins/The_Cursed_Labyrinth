using UnityEngine;
using System;

public class SpecialRoomCompleteBroadcaster : MonoBehaviour
{
    public event Action OnSolved;

    public void TriggerSolved()
    {
        Debug.Log("[SpecialRoom] Solved");
        OnSolved?.Invoke();
    }
}