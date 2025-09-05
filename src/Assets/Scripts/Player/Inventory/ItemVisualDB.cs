using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Dungeon/Items/Item Visual DB")]
public sealed class ItemVisualDB : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public Item.ItemType type;
        public Sprite sprite; 
    }

    public Sprite fallback;
    public List<Entry> entries = new();

    private Dictionary<Item.ItemType, Sprite> map;

    private void OnEnable() => Build();
    private void OnValidate() => Build();

    private void Build()
    {
        map = new Dictionary<Item.ItemType, Sprite>();
        if (entries == null) return;

        foreach (var e in entries)
            if (e != null)
                map[e.type] = e.sprite;
    }

    public Sprite GetSprite(Item.ItemType t) => (map != null && map.TryGetValue(t, out var s)) ? s : null;
}