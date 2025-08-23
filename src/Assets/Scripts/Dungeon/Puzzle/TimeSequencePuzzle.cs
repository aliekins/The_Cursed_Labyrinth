using System.Collections.Generic;
using System;
using UnityEngine;

public class Lever : MonoBehaviour
{
    public int id = 1;
    public event Action<int> Pulled;

    public void OnInteract() { Pulled?.Invoke(id); /* anim/sfx */ }
}

public class TimedSequencePuzzle : MonoBehaviour, IPuzzleRoom
{
    [SerializeField] List<Lever> levers;
    [SerializeField] List<int> requiredOrder;
    [SerializeField] float windowSeconds = 10f;
    [SerializeField] GateDoor gateDoor;

    int progress = 0;
    float windowEnd = 0;

    public void Init(Room room, DungeonMapIndex.RoomIndex ri)
    {
        foreach (var l in levers) l.Pulled += OnPulled;
        ResetWindow();
    }

    void OnPulled(int id)
    {
        if (Time.time > windowEnd) ResetWindow(); // expired

        if (requiredOrder[progress] == id)
        {
            progress++;
            windowEnd = Time.time + windowSeconds;

            if (progress >= requiredOrder.Count) 
            {
                gateDoor.SetOpen(true);
            }
        }
        else
        {
            // wrong lever - reset
            ResetWindow();
        }
    }

    void ResetWindow()
    {
        progress = 0;
        windowEnd = Time.time + windowSeconds;
        // reset lever visuals if needed
    }
}
