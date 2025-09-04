using UnityEngine;

/**
 * @file PickupItem.cs
 * @brief World pickup that gives an item to the player on E (or auto pickup).
 * @ingroup PlayerInventory
 */

[RequireComponent(typeof(Collider2D))]
public sealed class PickupItem : MonoBehaviour
{
    #region config
    [Header("What this pickup represents")]
    public Item.ItemType Type;
    public int Quantity = 1;

    [Header("Pickup behaviour")]
    public bool autoPickup = false;
    public bool isSpecial = false;

    [Header("FX")]
    public AudioClip pickupSfx;
    [Range(0f, 1f)] public float pickupVolume = 1f;
    public GameObject pickupFlashPrefab;
    #endregion

    #region cyclr
    private void Reset()
    {
        // make sure collider is trigger
        var c = GetComponent<Collider2D>();
        if (c) c.isTrigger = true;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        var inv = other.GetComponent<PlayerInventory>();
        if (!inv) return;

        if (autoPickup || Input.GetKeyDown(KeyCode.E))
            TryGiveTo(inv);
    }
    #endregion

    #region helpers
    private void TryGiveTo(PlayerInventory inv)
    {
        if (isSpecial)
        {
            bool ok = inv.TryCarrySpecial(Type);
            Debug.Log($"[PickupItem] Special pickup '{Type}' - TryCarrySpecial: {ok}");
            if (!ok) return;
        }
        else
        {
            inv.Add(Type, Mathf.Max(1, Quantity));
            Debug.Log($"[PickupItem] Normal pickup '{Type}' x{Mathf.Max(1, Quantity)} - Add()");
        }

        if (pickupSfx) AudioSource.PlayClipAtPoint(pickupSfx, transform.position, pickupVolume);
        if (pickupFlashPrefab) Instantiate(pickupFlashPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
    #endregion
}