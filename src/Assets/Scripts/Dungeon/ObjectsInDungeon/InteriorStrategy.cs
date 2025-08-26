using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class InteriorStrategy : PropStrategyBase
{
    [Header("Ordering")]
    [Tooltip("Place farther from center first ('fill' towards the middle).")]
    [SerializeField] private bool farToNear = true;

    public override List<Vector2Int> OrderCandidates(Room room, List<Vector2Int> candidates, System.Random rng)
    {
        if (!farToNear) return candidates;
        candidates.Sort((a, b) =>
        {
            int da = Mathf.Abs(a.x - room.Center.x) + Mathf.Abs(a.y - room.Center.y);
            int db = Mathf.Abs(b.x - room.Center.x) + Mathf.Abs(b.y - room.Center.y);
            return db.CompareTo(da); // far first
        });
        return candidates;
    }
}