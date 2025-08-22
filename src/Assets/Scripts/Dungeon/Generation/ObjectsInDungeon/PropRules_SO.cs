/// \file PropRules_SO.cs
/// \brief Sprite-based prop rules organized per biome band

using System;
using System.Collections.Generic;
using UnityEngine;

public enum PropKind { Barrel, Vase, Chest, Decor }
public enum PropColliderMode { None, Box, Circle }

[CreateAssetMenu(menuName = "Dungeon/Prop Rules (Sprites)")]
public sealed class PropRules_SO : ScriptableObject
{
    [Serializable]
    public sealed class PropRule
    {
        [Header("What")]
        public PropKind kind;
        public Sprite sprite;

        [Header("Spawn chance & counts (per chosen room)")]
        [Range(0f, 1f)] public float chance = 0.6f;
        public int minCount = 1;                   
        public int maxCount = 3;                  

        [Header("Placement filters")]
        public bool avoidCarpet = true;
        public bool avoidCorridor = true;
        public bool avoidDoorCells = true;
        public int separation = 1;                    

        [Header("Visuals")]
        public string sortingLayer = "Default";
        public int orderInLayer = 4;
        public bool fitToCell = true;                 

        [Header("Collider")]
        public PropColliderMode collider = PropColliderMode.None;
        public bool colliderIsTrigger = false;
        public Vector2 colliderSize = Vector2.one;
        public float colliderRadius = 0.45f;

        [Header("Lighting/Material (optional)")]
        public Material overrideMaterial = null;      ///< If null, auto-picks URP 2D lit material if available
    }

    [Serializable]
    public sealed class BiomeGroup
    {
        [Tooltip("Matches floor kind by StartsWith (e.g. \"floor_entry\")")]
        public string biomeKindPrefix = "floor_entry";
        public float roomPickChance = 1.0f;
        public List<PropRule> rules = new();
    }

    [Tooltip("Per-biome groups of prop rules")]
    public List<BiomeGroup> biomes = new();
}