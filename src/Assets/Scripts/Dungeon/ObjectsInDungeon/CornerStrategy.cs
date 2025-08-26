using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CornerStrategy : PropStrategyBase
{
    public override List<Vector2Int> OrderCandidates(Room room, List<Vector2Int> candidates, System.Random rng)
    {
        // deterministic shuffle for variety
        //Shuffle(candidates, rng);
        return candidates;
    }

    private static void Shuffle<T>(IList<T> list, System.Random rng)
    {
        if (list == null) return;
        for (int i = list.Count - 1; i > 0; --i)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}