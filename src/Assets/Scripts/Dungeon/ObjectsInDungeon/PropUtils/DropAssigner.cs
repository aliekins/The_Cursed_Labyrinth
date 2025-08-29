using System.Collections.Generic;
using UnityEngine;

public static class DropAssigner
{
    public static void Assign(PropPopulator.DropPolicy policy, TilemapVisualizer viz, DungeonGrid grid, List<Room> rooms, List<GameObject> spawned, List<BreakableProp> breakables, System.Random rng)
    {
        if (policy.Equals(default(PropPopulator.DropPolicy))) return;

        // Tag-split
        var swordHolders = new List<BreakableProp>();
        var bookHolders = new List<BreakableProp>();
        var potionHolders = new List<BreakableProp>();
        var skullHolders = new List<BreakableProp>();
        var heartHolders = new List<BreakableProp>();
        var crownHolders = new List<BreakableProp>();

        foreach (var bp in breakables)
        {
            if (!bp) continue;

            var t = bp.GetComponent<PropDropTags>(); 
            if (!t) continue;

            if (t.sword) swordHolders.Add(bp);
            if (t.book) bookHolders.Add(bp);
            if (t.potion) potionHolders.Add(bp);
            if (t.skull) skullHolders.Add(bp);
            if (t.heart) heartHolders.Add(bp);
            if (t.crown) crownHolders.Add(bp);
        }

        if (policy.allowSwords && swordHolders.Count < policy.guaranteedSwords)
            PromoteByTag(spawned, need: policy.guaranteedSwords - swordHolders.Count, out swordHolders, want: t => t.sword, breakables);

        if (policy.allowBooks && bookHolders.Count < policy.guaranteedBooks)
            PromoteByTag(spawned, need: policy.guaranteedBooks - bookHolders.Count, out bookHolders, want: t => t.book, breakables);

        // Shuffle
        Shuffle(swordHolders, rng);
        Shuffle(bookHolders, rng);
        Shuffle(potionHolders, rng);
        Shuffle(skullHolders, rng);
        Shuffle(heartHolders, rng);
        Shuffle(crownHolders, rng);

        var used = new HashSet<BreakableProp>();

        // Swords
        if (policy.allowSwords)
        {
            int need = Mathf.Max(0, policy.guaranteedSwords);

            for (int i = 0; i < swordHolders.Count && need > 0; i++)
            {
                var bp = swordHolders[i]; 

                if (!bp || used.Contains(bp)) continue;

                bp.ConfigureDrop(Item.ItemType.Sword, 1);
                used.Add(bp);
                need--;
            }
        }

        // Books (unique set)
        if (policy.allowBooks)
        {
            int need = Mathf.Max(0, policy.guaranteedBooks);
            Item.ItemType[] unique =
            {
                Item.ItemType.Book1, Item.ItemType.Book2, Item.ItemType.Book3, Item.ItemType.Book4, Item.ItemType.Book5
            };

            int uCount = Mathf.Min(need, unique.Length, bookHolders.Count);
            for (int u = 0, i = 0; i < bookHolders.Count && u < uCount; i++)
            {
                var bp = bookHolders[i]; 

                if (!bp || used.Contains(bp)) continue;

                bp.ConfigureDrop(unique[u], 1);
                used.Add(bp);
                u++;
            }
        }

        // Potions
        if (policy.allowPotions && policy.potionChance > 0f)
        {
            for (int i = 0; i < potionHolders.Count; i++)
            {
                var bp = potionHolders[i]; if (!bp || used.Contains(bp)) continue;
                if (rng.NextDouble() < policy.potionChance)
                {
                    bp.ConfigureDrop(Item.ItemType.HealthPotion, 1);
                    used.Add(bp);
                }
            }
        }

        // Cursed
        if (policy.allowCursed && policy.guaranteedCursed > 0)
        {
            bool sOk = TryAssign(skullHolders, Item.ItemType.SkullDiamond, used);
            bool hOk = TryAssign(heartHolders, Item.ItemType.HeartDiamond, used);
            bool cOk = TryAssign(crownHolders, Item.ItemType.Crown, used);

            if (!sOk || !hOk || !cOk)
                FallbackSpawnCursedOnFloor(viz, grid, rooms, rng, sOk, hOk, cOk);
        }
    }

    private static bool TryAssign(List<BreakableProp> list, Item.ItemType t, HashSet<BreakableProp> used)
    {
        foreach (var bp in list)
        {
            if (!bp || used.Contains(bp)) continue;

            bp.ConfigureDrop(t, 1);
            used.Add(bp);

            return true;
        }
        return false;
    }

    private static void PromoteByTag(List<GameObject> spawned, int need, out List<BreakableProp> holdersOut, System.Func<PropDropTags, bool> want, List<BreakableProp> breakables)
    {
        holdersOut = new List<BreakableProp>();
        var candidates = new List<GameObject>();

        foreach (var go in spawned)
        {
            if (!go) continue;
            if (go.GetComponent<BreakableProp>()) continue;

            var tag = go.GetComponent<PropDropTags>(); if (!tag) continue;

            if (want(tag) && (go.GetComponent<Collider2D>() || go.GetComponentInChildren<Collider2D>()))
                candidates.Add(go);
        }

        for (int i = 0; i < candidates.Count && need > 0; i++)
        {
            var go = candidates[i];
            var bp = go.GetComponent<BreakableProp>() ?? go.AddComponent<BreakableProp>();

            bp.Configure(null, 1f, null, 0f);

            breakables.Add(bp);
            holdersOut.Add(bp);
            need--;
        }

        // include any existing breakables with that tag
        foreach (var bp in breakables)
        {
            if (!bp || holdersOut.Contains(bp)) continue;

            var t = bp.GetComponent<PropDropTags>();

            if (!t) continue;
            if (want(t)) 
                holdersOut.Add(bp);
        }
    }

    private static void FallbackSpawnCursedOnFloor(TilemapVisualizer viz, DungeonGrid grid, List<Room> rooms, System.Random rng, bool hasSkull, bool hasHeart, bool hasCrown)
    {
        var parent = viz.GridTransform ? viz.GridTransform : null;
        var missing = new List<Item.ItemType>(3);

        if (!hasSkull)
            missing.Add(Item.ItemType.SkullDiamond);
        if (!hasHeart) 
            missing.Add(Item.ItemType.HeartDiamond);
        if (!hasCrown) 
            missing.Add(Item.ItemType.Crown);

        foreach (var m in missing)
        {
            for (int tries = 0; tries < 50; tries++)
            {
                var rm = rooms[rng.Next(0, rooms.Count)];
                var b = rm.Bounds;

                int x = rng.Next(b.xMin + 1, b.xMax - 1);
                int y = rng.Next(b.yMin + 1, b.yMax - 1);

                if (!grid.InBounds(x, y)) continue;

                var k = grid.Kind[x, y] ?? "";

                if (k == "wall") continue;

                var pos = viz.CellCenterLocal(x, y);
                var go = new GameObject($"pickup_{m}");

                go.transform.SetParent(parent, false);
                go.transform.localPosition = pos;

                var sr = go.AddComponent<SpriteRenderer>(); 
                sr.sortingOrder = 2;

                var col = go.AddComponent<CircleCollider2D>();
                col.isTrigger = true; col.radius = viz.CellSize * 0.3f;

                var pu = go.AddComponent<PickupItem>();

                pu.Type = m;
                pu.Quantity = 1;
                pu.autoPickup = false;
                pu.isSpecial = true;

                break;
            }
        }
    }

    private static void Shuffle<T>(IList<T> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}