using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class PickupItem : MonoBehaviour
{
    [Header("Pickup")]
    public Item.ItemType type = Item.ItemType.Sword;
    public int quantity = 1;

    [Header("FX")]
    [SerializeField] private AudioSource pickupSfx;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!Input.GetKeyDown(KeyCode.E)) return; 

        var inv = other.GetComponent<PlayerInventory>();
        if (!inv) return;

        inv.Add(type, Mathf.Max(1, quantity));
        if (pickupSfx) pickupSfx.Play();

        // destroy after pickup
        Destroy(gameObject);
    }
}