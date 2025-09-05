using UnityEngine;
using System;
using System.Collections.Generic;

/**
 * @file GhostHintDB.cs
 * @brief ScriptableObject database of hint lines per biome, with (optional) per line tags.
 * @ingroup Ghost
 */
[CreateAssetMenu(menuName = "Dungeon/Hints/Ghost Hint DB", fileName = "GhostHintDB")]
public sealed class GhostHintDB : ScriptableObject
{
    #region setup
    [Serializable]
    public class Hint
    {
        [Tooltip("(Optional)")]
        public string tag;
        [TextArea(2, 4)] public string text;
        public AudioClip voice;
    }

    [Serializable]
    public class BiomeHints
    {
        public string name = "Biome";
        public List<Hint> lines = new();
        public bool shuffleOnReset = false;
    }

    [Tooltip("Index 0 = first biome, 1 = second biome,...")]
    public List<BiomeHints> biomes = new();
    #endregion

    #region runtime
    [NonSerialized] private readonly Dictionary<int, int> _nextIndex = new();
    [NonSerialized] private readonly Dictionary<int, List<int>> _order = new();

    [NonSerialized] private readonly Dictionary<(int biome, string tag), int> _nextIndexByTag = new();
    [NonSerialized] private readonly Dictionary<(int biome, string tag), List<int>> _orderByTag = new();
    #endregion

    #region public API
    public Hint GetNextHint(int biomeIndex)
    {
        return GetNextHintTagged(biomeIndex, null);
    }

    public Hint GetNextHintTagged(int biomeIndex, string tag)
    {
        if (!TryBuildOrder(biomeIndex, tag, out var order)) return null;

        var key = (biomeIndex, Canon(tag));
        int cursor = _nextIndexByTag.TryGetValue(key, out var c) ? c : 0;
        cursor = Mathf.Clamp(cursor, 0, order.Count - 1);

        var b = biomes[biomeIndex];
        var hint = b.lines[order[cursor]];

        cursor = (cursor + 1) % order.Count;
        _nextIndexByTag[key] = cursor;

        return hint;
    }

    public Hint GetHintAtTagged(int biomeIndex, string tag, int orderIndex)
    {
        if (!TryBuildOrder(biomeIndex, tag, out var order)) return null;
        if (orderIndex < 0 || orderIndex >= order.Count) return null;

        var b = biomes[biomeIndex];
        return b.lines[order[orderIndex]];
    }

    public void ResetBiome(int biomeIndex)
    {
        if (biomeIndex < 0 || biomeIndex >= biomes.Count) return;

        _nextIndex.Remove(biomeIndex);
        _order.Remove(biomeIndex);

        // Remove all tag entries for this biome
        var toClear = new List<(int, string)>();
        foreach (var k in _orderByTag.Keys)
            if (k.biome == biomeIndex)
                toClear.Add(k);

        foreach (var k in toClear)
        {
            _orderByTag.Remove(k);
            _nextIndexByTag.Remove(k);
        }
    }

    public void ResetAll()
    {
        _nextIndex.Clear();
        _order.Clear();
        _nextIndexByTag.Clear();
        _orderByTag.Clear();
    }
    #endregion

    #region internals
    private static string Canon(string tag) => string.IsNullOrWhiteSpace(tag) ? "" : tag.Trim();

    private bool TryBuildOrder(int biomeIndex, string tag, out List<int> order)
    {
        order = null;

        if (biomeIndex < 0 || biomeIndex >= biomes.Count) return false;
        var b = biomes[biomeIndex];
        if (b == null || b.lines == null || b.lines.Count == 0) return false;

        string keyTag = Canon(tag);
        var key = (biomeIndex, keyTag);

        if (!_orderByTag.TryGetValue(key, out order) || order == null)
        {
            // Build list of indices for this tag (or all if tag empty)
            order = new List<int>(b.lines.Count);
            for (int i = 0; i < b.lines.Count; i++)
            {
                string lt = Canon(b.lines[i].tag);
                if (keyTag == "" || lt == keyTag)
                    order.Add(i);
            }

            if (order.Count == 0)
                return false; // no lines with this tag

            if (b.shuffleOnReset)
                Shuffle(order);

            _orderByTag[key] = order;
            _nextIndexByTag[key] = 0;
        }

        return true;
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
    #endregion
}