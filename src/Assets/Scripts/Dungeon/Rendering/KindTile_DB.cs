/// \file KindTile_DB.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[Serializable] public sealed class KindTile { public string kind; public TileBase tile; }

[CreateAssetMenu(fileName = "KindTile_DB", menuName = "Dungeon/Kind-Tile DB", order = 12)]
public sealed class KindTile_DB : ScriptableObject
{
    public List<KindTile> entries = new();
    public bool TryGet(string kind, out TileBase tile)
    {
        foreach (var e in entries)
            if (!string.IsNullOrWhiteSpace(e.kind) && string.Equals(e.kind, kind, StringComparison.OrdinalIgnoreCase) && e.tile != null)
            { 
                tile = e.tile;
                return true; 
            }

        tile = null;
        return false;
    }
}