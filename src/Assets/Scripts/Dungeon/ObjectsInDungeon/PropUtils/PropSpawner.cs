using UnityEngine;

public static class PropSpawner
{
    private const int SortingOrder = 2; // above floor, below walls

    public static GameObject Spawn(TilemapVisualizer viz, PropStrategyBase.SimpleProp rule, Vector2Int cell, PlacementMods mods, bool fitToCell)
    {
        var parent = viz.GridTransform ? viz.GridTransform : null;
        GameObject go;

        if (rule.prefab)
        {
            go = Object.Instantiate(rule.prefab, parent, false);
            go.transform.localPosition = viz.CellCenterLocal(cell.x, cell.y) + (Vector3)mods.offset;
        }
        else
        {
            go = new GameObject($"prop_{cell.x}_{cell.y}");

            go.transform.SetParent(parent, false);
            go.transform.localPosition = viz.CellCenterLocal(cell.x, cell.y) + (Vector3)mods.offset;

            var sr = go.AddComponent<SpriteRenderer>();

            sr.sprite = rule.sprite;
            sr.sortingOrder = SortingOrder;

            if (fitToCell && rule.sprite && mods.scaleToCell)
            {
                float cs = viz.CellSize;
                var s = rule.sprite.bounds.size;

                if (s.x > 0.0001f && s.y > 0.0001f)
                    go.transform.localScale = new Vector3(cs / s.x, cs / s.y, 1f);
            }
        }
        EnsureCollider(go, viz, rule);
        AddTags(go, rule);

        return go;
    }

    private static void EnsureCollider(GameObject go, TilemapVisualizer viz, PropStrategyBase.SimpleProp rule)
    {
        if (rule.colliderMode == PropStrategyBase.SimpleProp.ColliderMode.None) return;
        if (go.GetComponent<Collider2D>()) return;

        float cell = viz.CellSize;
        Vector2 size = new Vector2(cell * rule.colliderSizeScale.x, cell * rule.colliderSizeScale.y);
        Vector2 offset = rule.colliderOffset;

        switch (rule.colliderMode)
        {
            case PropStrategyBase.SimpleProp.ColliderMode.Box:
                var bc = go.AddComponent<BoxCollider2D>();
                bc.isTrigger = rule.colliderIsTrigger;
                bc.size = size;
                bc.offset = offset;
                break;
            case PropStrategyBase.SimpleProp.ColliderMode.Circle:
                var cc = go.AddComponent<CircleCollider2D>();
                cc.isTrigger = rule.colliderIsTrigger;
                cc.radius = Mathf.Min(size.x, size.y) * 0.5f;
                cc.offset = offset;
                break;
            case PropStrategyBase.SimpleProp.ColliderMode.Capsule:
                var cap = go.AddComponent<CapsuleCollider2D>(); 
                cap.isTrigger = rule.colliderIsTrigger;
                cap.size = size;
                cap.direction = (size.x >= size.y) ? CapsuleDirection2D.Horizontal : CapsuleDirection2D.Vertical; 
                cap.offset = offset;
                break;
        }
    }

    private static void AddTags(GameObject go, PropStrategyBase.SimpleProp rule)
    {
        var tag = go.GetComponent<PropDropTags>() ?? go.AddComponent<PropDropTags>();

        tag.sword = rule.holdsSwords;
        tag.book = rule.holdsBooks;
        tag.potion = rule.holdsPotions;

        tag.skull = rule.holdsSkull;
        tag.heart = rule.holdsHeart;
        tag.crown = rule.holdsCrown;

        Debug.Log($"[PropSpawner] Tagged prop '{go.name}' " +
                  $"S:{tag.sword} B:{tag.book} P:{tag.potion} " +
                  $"Skull:{tag.skull} Heart:{tag.heart} Crown:{tag.crown}");
    }
}