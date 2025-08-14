/// \file TileRuleDatabase.cs
/// \brief Database mapping kind string - TileRule asset.
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public sealed class TileRuleEntry { public string kind; public TileRuleAsset rule; }

[CreateAssetMenu(fileName = "TileRuleDatabase", menuName = "Dungeon/Tile Rule Database", order = 11)]
public sealed class TileRuleDatabase : ScriptableObject
{
    public List<TileRuleEntry> entries = new List<TileRuleEntry>();
    public bool TryGet(string kind, out TileRuleAsset rule)
    {
        foreach (var e in entries)
        {
            if (e != null && e.rule != null && string.Equals(e.kind, kind, System.StringComparison.OrdinalIgnoreCase))
            { 
                rule = e.rule;
                return true; 
            }
        }

        rule = null;
        return false;
    }
}