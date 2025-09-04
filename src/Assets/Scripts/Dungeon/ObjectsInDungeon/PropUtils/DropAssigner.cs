using System;
using System.Collections.Generic;
using UnityEngine;

/**
 * @file DropAssigner.cs
 * @brief Assigns drops to breakable holders and spawns fallback cursed pickups.
 * @ingroup PropUtils
 *
 * Categories: swords, books (unique), potions (chance), cursed (Skull/Heart/Crown).
 * Ensures guarantees from policy; if any cursed holder is missing, spawns world pickups.
 */

public static class DropAssigner
{
    #region publicEntry
    public static void Assign(PropPopulator.DropPolicy policy, TilemapVisualizer viz, DungeonGrid grid, List<Room> rooms, List<GameObject> spawned, List<BreakableProp> breakables, System.Random rng)
    {
        Debug.Log($"[DropAssigner] Policy allowS:{policy.allowSwords} gS:{policy.guaranteedSwords} " +
          $"allowB:{policy.allowBooks} gB:{policy.guaranteedBooks} " +
          $"allowP:{policy.allowPotions} p%:{policy.potionChance} " +
          $"allowC:{policy.allowCursed} gC:{policy.guaranteedCursed}");

        if (policy.Equals(default(PropPopulator.DropPolicy))) return;

        var ctx = new Ctx(viz, grid, rooms, rng);
        var pools = Pools.From(breakables);

        EnsureGuaranteedHolders(policy, spawned, breakables, ref pools);

        pools.ShuffleAll(rng);

        // One holder gets only one drop
        var used = new HashSet<BreakableProp>();

        AssignSwords(policy, pools, used);
        AssignBooksUnique(policy, pools, used);
        AssignPotions(policy, pools, used, rng);
        AssignCursed(policy, pools, used, ctx);

        Debug.Log($"[DropAssigner] Pools: S={pools.Swords.Count} B={pools.Books.Count} " +
          $"Pots={pools.Potions.Count} Skull={pools.Skull.Count} Heart={pools.Heart.Count} Crown={pools.Crown.Count}");

    }
    #endregion
    #region dataHolders
    private readonly struct Ctx
    {
        public readonly TilemapVisualizer Viz;
        public readonly DungeonGrid Grid;
        public readonly List<Room> Rooms;
        public readonly System.Random Rng;
        public Ctx(TilemapVisualizer viz, DungeonGrid grid, List<Room> rooms, System.Random rng)
        {
            Viz = viz; Grid = grid; Rooms = rooms; Rng = rng;
        }
    }

    private sealed class Pools
    {
        public readonly List<BreakableProp> Swords = new();
        public readonly List<BreakableProp> Books = new();
        public readonly List<BreakableProp> Potions = new();
        public readonly List<BreakableProp> Skull = new();
        public readonly List<BreakableProp> Heart = new();
        public readonly List<BreakableProp> Crown = new();

        public static Pools From(IEnumerable<BreakableProp> breakables)
        {
            var p = new Pools();

            foreach (var bp in breakables)
            {
                if (!bp) continue;

                var t = bp.GetComponent<PropDropTags>();
                if (!t) continue;

                if (t.sword) p.Swords.Add(bp);
                if (t.book) p.Books.Add(bp);
                if (t.potion) p.Potions.Add(bp);
                if (t.skull) p.Skull.Add(bp);
                if (t.heart) p.Heart.Add(bp);
                if (t.crown) p.Crown.Add(bp);
            }

            return p;
        }

        public void ShuffleAll(System.Random rng)
        {
            Shuffle(Swords, rng);
            Shuffle(Books, rng);
            Shuffle(Potions, rng);
            Shuffle(Skull, rng);
            Shuffle(Heart, rng);
            Shuffle(Crown, rng);
        }
    }
    #endregion
    #region helpers
    private static void EnsureGuaranteedHolders(PropPopulator.DropPolicy policy, List<GameObject> spawned, List<BreakableProp> breakables, ref Pools pools)
    {
        if (policy.allowSwords && pools.Swords.Count < policy.guaranteedSwords)
        {
            PromoteByTag(spawned, need: policy.guaranteedSwords - pools.Swords.Count, out var newHolders, want: t => t.sword, breakables);
            pools.Swords.AddRange(newHolders);
        }

        if (policy.allowBooks && pools.Books.Count < policy.guaranteedBooks)
        {
            PromoteByTag(spawned, need: policy.guaranteedBooks - pools.Books.Count, out var newHolders, want: t => t.book, breakables);
            pools.Books.AddRange(newHolders);
        }

        // Potions/cursed use chance/exact placement logic; no "count" guarantee needed here
    }
    #region categoryAssignments
    private static void AssignSwords(PropPopulator.DropPolicy policy, Pools pools, HashSet<BreakableProp> used)
    {
        if (!policy.allowSwords) return;

        int need = Mathf.Max(0, policy.guaranteedSwords);

        for (int i = 0; i < pools.Swords.Count && need > 0; i++)
        {
            var bp = pools.Swords[i];
            if (!bp || used.Contains(bp)) continue;

            bp.ConfigureDrop(Item.ItemType.Sword, 1);
            Debug.Log($"[DropAssigner] Assigned SWORD to '{bp.gameObject.name}'");
            used.Add(bp);
            need--;
        }
    }

    private static void AssignBooksUnique(PropPopulator.DropPolicy policy, Pools pools, HashSet<BreakableProp> used)
    {
        if (!policy.allowBooks) return;

        int need = Mathf.Max(0, policy.guaranteedBooks);
        Item.ItemType[] unique = { Item.ItemType.Book1, Item.ItemType.Book2, Item.ItemType.Book3, Item.ItemType.Book4, Item.ItemType.Book5 };

        int uCount = Mathf.Min(need, unique.Length, pools.Books.Count);

        for (int u = 0, i = 0; i < pools.Books.Count && u < uCount; i++)
        {
            var bp = pools.Books[i];
            if (!bp || used.Contains(bp)) continue;

            bp.ConfigureDrop(unique[u], 1);
            Debug.Log($"[DropAssigner] Assigned BOOK{u + 1} to '{bp.gameObject.name}'");
            used.Add(bp);
            u++;
        }
    }

    private static void AssignPotions(PropPopulator.DropPolicy policy, Pools pools, HashSet<BreakableProp> used, System.Random rng)
    {
        if (!policy.allowPotions || policy.potionChance <= 0f) return;

        for (int i = 0; i < pools.Potions.Count; i++)
        {
            var bp = pools.Potions[i];
            if (!bp || used.Contains(bp)) continue;

            if (rng.NextDouble() < policy.potionChance)
            {
                bp.ConfigureDrop(Item.ItemType.HealthPotion, 1);
                Debug.Log($"[DropAssigner] Assigned POTION to '{bp.gameObject.name}'");
                used.Add(bp);
            }
        }
    }

    private static void AssignCursed(PropPopulator.DropPolicy policy, Pools pools, HashSet<BreakableProp> used, Ctx ctx)
    {
        if (!policy.allowCursed || policy.guaranteedCursed <= 0) return;

        bool sOk = TryAssign(pools.Skull, Item.ItemType.SkullDiamond, used);
        bool hOk = TryAssign(pools.Heart, Item.ItemType.HeartDiamond, used);
        bool cOk = TryAssign(pools.Crown, Item.ItemType.Crown, used);

        if (!sOk || !hOk || !cOk)
            FallbackSpawnCursedOnFloor(ctx, sOk, hOk, cOk);
    }

    #endregion
    #endregion
    #region utils
    private static bool TryAssign(List<BreakableProp> list, Item.ItemType t, HashSet<BreakableProp> used)
    {
        foreach (var bp in list)
        {
            if (!bp || used.Contains(bp)) continue;

            bp.ConfigureDrop(t, 1);
            Debug.Log($"[DropAssigner] Assigned {t} to '{bp.gameObject.name}'");
            used.Add(bp);
            return true;
        }
        return false;
    }

    private static void PromoteByTag(List<GameObject> spawned, int need, out List<BreakableProp> holdersOut, Func<PropDropTags, bool> want, List<BreakableProp> breakables)
    {
        holdersOut = new List<BreakableProp>();
        var candidates = new List<GameObject>();

        foreach (var go in spawned)
        {
            if (!go) continue;
            if (go.GetComponent<BreakableProp>()) continue;

            var tag = go.GetComponent<PropDropTags>();
            if (!tag) continue;

            // must be physically interactive to be promoted
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

    private static void FallbackSpawnCursedOnFloor(Ctx ctx, bool hasSkull, bool hasHeart, bool hasCrown)
    {
        var parent = ctx.Viz.GridTransform ? ctx.Viz.GridTransform : null;
        var missing = new List<Item.ItemType>(3);

        if (!hasSkull)
            missing.Add(Item.ItemType.SkullDiamond);
        if (!hasHeart)
            missing.Add(Item.ItemType.HeartDiamond);
        if (!hasCrown)
            missing.Add(Item.ItemType.Crown);

        foreach (var m in missing)
        {
            // Try a few random inner tiles of random rooms
            for (int tries = 0; tries < 50; tries++)
            {
                var rm = ctx.Rooms[ctx.Rng.Next(0, ctx.Rooms.Count)];
                var b = rm.Bounds;

                int x = ctx.Rng.Next(b.xMin + 1, b.xMax - 1);
                int y = ctx.Rng.Next(b.yMin + 1, b.yMax - 1);

                if (!ctx.Grid.InBounds(x, y)) continue;

                var k = ctx.Grid.Kind[x, y] ?? "";
                if (k == "wall") continue;

                var pos = ctx.Viz.CellCenterLocal(x, y);
                var go = new GameObject($"pickup_{m}");

                go.transform.SetParent(parent, false);
                go.transform.localPosition = pos;

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 2;

                var col = go.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = ctx.Viz.CellSize * 0.3f;

                var pu = go.AddComponent<PickupItem>();
                pu.Type = m;
                pu.Quantity = 1;
                pu.autoPickup = false;
                pu.isSpecial = true;

                CursedItemRespawnManager.RegisterPickup(pu);

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
    #endregion
}