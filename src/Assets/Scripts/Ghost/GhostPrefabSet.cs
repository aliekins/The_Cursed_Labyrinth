using UnityEngine;
using System;
using System.Collections.Generic;

/// @file GhostPrefabSet.cs
/// @brief ScriptableObject mapping biome index to a ghost prefab.
/// @ingroup Ghost
[CreateAssetMenu(menuName = "Dungeon/Hints/Ghost Prefab Set")]
public sealed class GhostPrefabSet : ScriptableObject
{
    [Serializable] public class Entry { public string label; public GameObject prefab; }

    [Tooltip("Order must match BiomeSequenceController.currentBiomeIndex (0..N-1).")]
    public List<Entry> byBiome = new();

    /// @brief Get a prefab for a biome index; clamps and skips nulls.
    public GameObject GetForBiome(int biomeIndex)
    {
        if (byBiome == null || byBiome.Count == 0) return null;

        int idx = Mathf.Clamp(biomeIndex, 0, byBiome.Count - 1);
        // prefer exact match; if null, fall back to first non-null
        var p = byBiome[idx]?.prefab;
        if (p) return p;

        for (int i = 0; i < byBiome.Count; i++)
            if (byBiome[i]?.prefab) return byBiome[i].prefab;

        return null;
    }
}
