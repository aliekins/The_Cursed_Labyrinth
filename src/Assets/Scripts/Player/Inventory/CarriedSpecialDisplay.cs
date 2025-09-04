using UnityEngine;

/**
 * @file CarriedSpecialDisplay.cs
 * @brief Renders a small sprite above the player for the currently carried cursed item.
 * @ingroup PlayerInventory
 */

[RequireComponent(typeof(PlayerInventory))]
public sealed class CarriedSpecialDisplay : MonoBehaviour
{
    [SerializeField] private Transform anchor;       // empty GameObject above the head
    [SerializeField] private Sprite skullSprite;
    [SerializeField] private Sprite heartSprite;
    [SerializeField] private Sprite crownSprite;

    private PlayerInventory inv;
    private SpriteRenderer sr;

    private void Awake()
    {
        inv = GetComponent<PlayerInventory>();
        if (!anchor)
        {
            Debug.Log("[CarriedSpecialDisplay] No anchor assigned, creating one.");

            anchor = new GameObject("CarryAnchor").transform;
            anchor.SetParent(transform, false);
            anchor.localPosition = new Vector3(-10f, 20f, 0f);
        }

        Debug.Log($"[CarriedSpecialDisplay] Anchor position: {anchor.position}", this);

        var go = new GameObject("CarriedSprite");
        go.transform.SetParent(anchor, false);

        sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 10;
        sr.enabled = false;
    }

    private void OnEnable() => inv.CarriedSpecialChanged += OnCarryChanged;
    private void OnDisable() => inv.CarriedSpecialChanged -= OnCarryChanged;

    private void OnCarryChanged(Item.ItemType? t)
    {
        if (!t.HasValue)
        {
            sr.enabled = false;
            return; 
        }

        sr.sprite = t.Value switch
        {
            Item.ItemType.SkullDiamond => skullSprite,
            Item.ItemType.HeartDiamond => heartSprite,
            Item.ItemType.Crown => crownSprite,
            _ => null
        };

        sr.enabled = sr.sprite != null;
    }
}