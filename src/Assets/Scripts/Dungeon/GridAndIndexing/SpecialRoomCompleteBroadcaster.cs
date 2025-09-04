using UnityEngine;
using System;

/// @file SpecialRoomCompleteBroadcaster.cs
/// @brief Emits a "special room solved" event for listeners.
/// @ingroup Grid
public class SpecialRoomCompleteBroadcaster : MonoBehaviour
{
    public event Action OnSolved;

    public void TriggerSolved()
    {
        Debug.Log("[SpecialRoom] Solved");
        OnSolved?.Invoke();
    }
}