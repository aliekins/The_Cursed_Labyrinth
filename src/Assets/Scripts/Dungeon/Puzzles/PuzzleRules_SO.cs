/// \file PuzzleRules_SO.cs
/// \brief Tweakable rules for puzzle planning (per-biome).
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Dungeon/Puzzle Rules", fileName = "PuzzleRules")]
public sealed class PuzzleRules_SO : ScriptableObject
{
    [System.Serializable] public sealed class Weight { public PuzzleType type; public int weight = 1; }
    [System.Serializable]
    public sealed class BiomeProfile
    {
        public string biomeKind = "floor_entry";
        public List<Weight> weights = new();
    }

    [Header("Constraints")]
    public int minRoomArea = 20;

    [Header("Per-biome type weights")]
    public List<BiomeProfile> profiles = new();

    public BiomeProfile GetProfileForBiome(string kind)
        => profiles.Find(p => string.Equals(p.biomeKind, kind, System.StringComparison.OrdinalIgnoreCase));
}
