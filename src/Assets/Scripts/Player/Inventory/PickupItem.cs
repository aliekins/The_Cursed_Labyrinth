using UnityEngine;

[DisallowMultipleComponent]
public sealed class PickupItem : MonoBehaviour
{
    [Header("Pickup")]
    public Item.ItemType type = Item.ItemType.Sword;
    public int quantity = 1;

    [Header("FX")]
    [SerializeField] private AudioClip pickupSfx;
    [SerializeField, Range(0, 1)] private float volume = 1f;

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (!col) col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!Input.GetKeyDown(KeyCode.E)) return;

        var inv = other.GetComponent<PlayerInventory>();
        if (!inv) return;

        inv.Add(type, Mathf.Max(1, quantity));
        if (pickupSfx) AudioSource.PlayClipAtPoint(pickupSfx, transform.position, volume);
        Destroy(gameObject);
    }
}