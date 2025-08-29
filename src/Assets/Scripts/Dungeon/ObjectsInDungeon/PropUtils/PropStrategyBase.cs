using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct PlacementMods
{
    public Vector2 offset;
    public bool scaleToCell;
    public static PlacementMods Default => new PlacementMods { offset = Vector2.zero, scaleToCell = true };
}

public abstract class PropStrategyBase : MonoBehaviour
{
    [Header("Rules")]
    public List<SimpleProp> rules = new();

    [Serializable]
    public class SimpleProp
    {
        [Header("Visual")]
        public Sprite sprite;                 // used if prefab == null
        public GameObject prefab;             // spawned if set

        [Header("Placement")]
        [Range(0f, 1f)] public float chance = 0.85f;
        public int min = 1, max = 2;
        [Tooltip("Spacing in tiles vs. other placed props.")]
        public int separation = 1;

        public enum ColliderMode { None, Box, Circle, Capsule }
        [Header("Collider (auto-add on spawn if missing)")]
        public ColliderMode colliderMode = ColliderMode.None;
        public bool colliderIsTrigger = false;
        [Tooltip("Size as a fraction of cell size (1 = full cell).")]
        public Vector2 colliderSizeScale = new Vector2(0.9f, 0.9f);
        public Vector2 colliderOffset = Vector2.zero;

        // WallStrategy uses this
        public enum WallFilter { Any, TopOnly, NotTop }
        public WallFilter wallFilter = WallFilter.Any;

        [Header("Breakable")]
        public bool breakable = false;
        public AudioClip breakSfx;
        [Range(0f, 1f)] public float breakSfxVolume = 1f;
        public GameObject breakVfxPrefab;

        [Header("Drop Eligibility")]
        public bool holdsPotions = false;

        public bool holdsSwords = false;

        public bool holdsBooks = false;

        public bool holdsSkull = false;
        public bool holdsHeart = false;
        public bool holdsCrown = false;
    }

    public IReadOnlyList<SimpleProp> Rules => rules;
    public bool HasRules => rules != null && rules.Count > 0;

    public virtual List<Vector2Int> OrderCandidates(Room room, List<Vector2Int> candidates, System.Random rng)
        => candidates ?? new List<Vector2Int>();

    public virtual List<Vector2Int> FilterCandidatesForRule(Room room, List<Vector2Int> candidates, SimpleProp rule)
        => candidates ?? new List<Vector2Int>();

    public virtual PlacementMods GetPlacementMods(Vector2Int cell, Room room) => PlacementMods.Default;
}