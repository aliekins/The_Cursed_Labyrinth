using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class WallStrategy : PropStrategyBase
{
    [Header("Wall Controls")]
    [Tooltip("If true, props on the TOP wall will be offset slightly upwards to look like they hang.")]
    [SerializeField] private bool hangOnTopWall = true;
    [SerializeField] private float hangYOffsetWorld = 0.18f;

    public override List<Vector2Int> OrderCandidates(Room room, List<Vector2Int> candidates, System.Random rng)
    {
        candidates.Sort((a, b) =>
        {
            int cmp = b.y.CompareTo(a.y);

            if (cmp != 0)
                return cmp;

            return a.x.CompareTo(b.x);
        });
        return candidates;
    }

    public override List<Vector2Int> FilterCandidatesForRule(Room room, List<Vector2Int> candidates, SimpleProp rule)
    {
        if (rule == null || candidates == null) return candidates;
        if (rule.wallFilter == SimpleProp.WallFilter.Any) return candidates;

        bool WantTop = (rule.wallFilter == SimpleProp.WallFilter.TopOnly);
        int topY = room.Bounds.yMax - 1;

        var list = new List<Vector2Int>(candidates.Count);
        foreach (var c in candidates)
        {
            bool isTop = (c.y == topY);

            if (WantTop && isTop)
                list.Add(c);
            else if (!WantTop && !isTop)
                list.Add(c); // NotTop
        }
        return list;
    }

    public override PlacementMods GetPlacementMods(Vector2Int cell, Room room)
    {
        if (hangOnTopWall && cell.y == room.Bounds.yMax - 1)
        {
            return new PlacementMods
            {
                offset = new Vector2(0f, hangYOffsetWorld),
                scaleToCell = true
            };
        }
        return PlacementMods.Default;
    }

}
