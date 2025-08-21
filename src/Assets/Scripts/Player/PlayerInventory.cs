using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    private readonly HashSet<int> keys = new();

    public bool HasKey(int tier) => keys.Contains(tier);

    public void AddKey(int tier)
    {
        keys.Add(tier);
        Debug.Log($"Key for tier {tier} collected.");
    }

    public void UseKey(int tier)
    {
        if (keys.Remove(tier))
            Debug.Log($"Key for tier {tier} used.");
    }
}