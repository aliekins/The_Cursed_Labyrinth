using UnityEngine;

/**
 * @file PickupItem.cs
 * @brief World pickup that shows a sprite and can be picked with E (or auto pickup).
 * @ingroup PlayerInventory
 */
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
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

    [Header("Visuals")]
    [Tooltip("Optional DB that maps ItemType to Sprite.")]
    [SerializeField] private ItemVisualDB visuals;
    [Tooltip("If set, overrides any DB sprite.")]
    [SerializeField] private Sprite overrideSprite;
    #endregion

    #region state
    private SpriteRenderer sr;
    private PlayerInventory playerInRange;

    private void Reset()
    {
        var c = GetComponent<Collider2D>();

        if (c) 
            c.isTrigger = true;
    }

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        if (!sr)
            sr = gameObject.AddComponent<SpriteRenderer>();

        sr.enabled = true;
        ApplySprite();
    }

    private void OnValidate()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr && Application.isEditor && !Application.isPlaying)
            ApplySprite();
    }

    private void ApplySprite()
    {
        Sprite sprite = overrideSprite;

        if (!sprite && visuals)
            sprite = visuals.GetSprite(Type);
        if (!sprite && visuals) 
            sprite = visuals.fallback;

        sr.sprite = sprite;

        if (!sprite)
            Debug.LogWarning($"[PickupItem] No sprite found for '{Type}'. Assign Override Sprite or add it to ItemVisualDB.", this);
    }
    #endregion

    #region trigger
    private void OnTriggerEnter2D(Collider2D other)
    {
        var inv = other.GetComponent<PlayerInventory>();
        if (!inv) return;

        if (autoPickup)
        {
            TryGiveTo(inv);
            return;
        }

        playerInRange = inv;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<PlayerInventory>() == playerInRange)
            playerInRange = null;
    }

    private void Update()
    {
        if (!playerInRange) return;

        if (Input.GetKeyDown(KeyCode.E))
            TryGiveTo(playerInRange);
    }
    #endregion

    #region give
    private void TryGiveTo(PlayerInventory inv)
    {
        if (!inv) return;

        if (isSpecial)
        {
            bool ok = inv.TryCarrySpecial(Type);
            Debug.Log($"[PickupItem] Special pickup '{Type}' - TryCarrySpecial: {ok}");

            if (!ok) return; // already carrying something else
        }
        else
        {
            inv.Add(Type, Mathf.Max(1, Quantity));
            Debug.Log($"[PickupItem] Normal pickup '{Type}' x{Mathf.Max(1, Quantity)} - Add()");
        }

        if (pickupSfx) AudioSource.PlayClipAtPoint(pickupSfx, transform.position, pickupVolume);
        if (pickupFlashPrefab) Instantiate(pickupFlashPrefab, transform.position, Quaternion.identity);

        // tidy up any respawn tracking
        var resp = FindFirstObjectByType<CursedItemRespawnManager>();
        resp?.Unregister(this);

        Destroy(gameObject);
    }
    #endregion
}
