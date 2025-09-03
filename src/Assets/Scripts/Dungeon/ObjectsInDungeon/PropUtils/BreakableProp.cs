using UnityEngine;
using UnityEngine.InputSystem;

/// Breakable prop controlled by proximity:
/// - Press I while within interactRadius to break this prop (not global)
/// - On break: SFX/VFX, drop item
///   - If player within autoPickupRadius: auto-add to inventory
///   - Else: spawn guaranteed collectible PickupItem at this position
[DisallowMultipleComponent]
public sealed class BreakableProp : MonoBehaviour
{
    [Header("Interact")]
    [SerializeField, Min(0f)] private float interactRadius = 1f;
    [SerializeField, Min(0f)] private float autoPickupRadius = 1f;

    [Header("Break FX")]
    [SerializeField] private AudioClip breakSfx;
    [SerializeField, Range(0f, 1f)] private float breakSfxVolume = 1f;
    [SerializeField] private GameObject breakVfxPrefab;
    [SerializeField, Min(0f)] private float destroyDelay = 0f;

    [Header("Drop (auto-filled by spawner)")]
    [SerializeField] private bool dropOnBreak = false;
    [SerializeField] private Item.ItemType dropItem = Item.ItemType.Sword;
    [SerializeField, Min(1)] private int dropQty = 1;
    [SerializeField] private bool autoPickupIfNear = true;
    [SerializeField] private AudioClip pickupSfx;
    [SerializeField] private GameObject pickupFlashPrefab;
    [SerializeField] private GameObject pickupPrefab; // optional prefab with PickupItem

    private Collider2D selfCollider;
    private KeyCode interactKey = KeyCode.E;

    void Awake()
    {
        selfCollider = GetComponent<Collider2D>(); // may be null; we null-guard below
    }

    // configured by PropPopulator
    public void ConfigureDrop(bool enable, Item.ItemType item, int qty, bool autoPickup, AudioClip pickSfx, GameObject flashPrefab, GameObject pickupObjPrefab)
    {
        dropOnBreak = enable;
        dropItem = item;
        dropQty = Mathf.Max(1, qty);
        autoPickupIfNear = autoPickup;
        pickupSfx = pickSfx;
        pickupFlashPrefab = flashPrefab;
        pickupPrefab = pickupObjPrefab;
    }

    public void Configure(AudioClip sfx, float volume, GameObject vfx, float delay = 0f)
    {
        interactKey = KeyCode.E;
        breakSfx = sfx;
        breakSfxVolume = Mathf.Clamp01(volume);
        breakVfxPrefab = vfx;
        destroyDelay = Mathf.Max(0f, delay);
    }

    public void ConfigureDrop(Item.ItemType item, int qty)
    {
        ConfigureDrop(
            enable: true,
            item: item,
            qty: Mathf.Max(1, qty),
            autoPickup: true,
            pickSfx: null,
            flashPrefab: null,
            pickupObjPrefab: null
        );
    }

    void Update()
    {
        if (!Input.GetKeyDown(interactKey)) return;

        var inv = FindPlayerInventory();
        if (!inv) return;

        // Must be close enough to THIS prop to break it
        float dist2 = (inv.transform.position - transform.position).sqrMagnitude;
        if (dist2 > interactRadius * interactRadius) return;

        // Decide if we can auto-pickup or need to spawn a pickup object
        PlayerInventory invForPickup = (autoPickupIfNear && dist2 <= autoPickupRadius * autoPickupRadius) ? inv : null;

        Break(invForPickup);
    }

    private void Break(PlayerInventory invInRange)
    {
        // disable only this prop’s interaction
        enabled = false;
        if (selfCollider) selfCollider.enabled = false;

        if (breakSfx) AudioSource.PlayClipAtPoint(breakSfx, transform.position, breakSfxVolume);
        if (breakVfxPrefab) Destroy(Instantiate(breakVfxPrefab, transform.position, Quaternion.identity), 1.25f);

        if (dropOnBreak)
        {
            bool isCursed = dropItem == Item.ItemType.SkullDiamond
                         || dropItem == Item.ItemType.HeartDiamond
                         || dropItem == Item.ItemType.Crown;

            if (invInRange)
            {
                if (isCursed)
                {
                    bool ok = invInRange.TryCarrySpecial(dropItem); // use carry slot for cursed
                    Debug.Log($"[BreakableProp] TryCarrySpecial({dropItem}) - {ok}");
                    if (!ok)
                    {
                        // player already carrying one - leave special pickup in world
                        SpawnFallbackPickup(dropItem, dropQty, true);
                    }
                    else
                    {
                        if (pickupSfx) AudioSource.PlayClipAtPoint(pickupSfx, transform.position, 1f);
                        if (pickupFlashPrefab) Destroy(Instantiate(pickupFlashPrefab, transform.position, Quaternion.identity), 1.0f);
                    }
                }
                else
                {
                    invInRange.Add(dropItem, dropQty); // normal inventory items
                    if (pickupSfx) AudioSource.PlayClipAtPoint(pickupSfx, transform.position, 1f);
                    if (pickupFlashPrefab) Destroy(Instantiate(pickupFlashPrefab, transform.position, Quaternion.identity), 1.0f);
                }
            }
            else
            {
                // player not in auto-pickup radius - spawn pickup (flag special if cursed)
                SpawnFallbackPickup(dropItem, dropQty, isCursed);
            }
        }

        if (destroyDelay > 0f) Destroy(gameObject, destroyDelay);
        else Destroy(gameObject);
    }

    private void SpawnFallbackPickup(Item.ItemType type, int qty, bool forceSpecial)
    {
        var parent = transform.parent;
        var go = pickupPrefab ? Instantiate(pickupPrefab, parent) : new GameObject($"pickup_{type}");
        go.transform.position = transform.position;

        var c = go.GetComponent<Collider2D>();
        if (!c) c = go.AddComponent<CircleCollider2D>();
        c.isTrigger = true;

        var pi = go.GetComponent<PickupItem>() ?? go.AddComponent<PickupItem>();
        pi.Type = type;
        pi.Quantity = Mathf.Max(1, qty);

        // mark cursed ones so PlayerInventory uses the carry slot on pickup
        pi.isSpecial = forceSpecial;

        //Debug.Log($"[BreakableProp] Fallback pickup spawned: {type} special={pi.isSpecial} at {transform.position}");
    }

    private static PlayerInventory FindPlayerInventory()
    {
        return FindFirstObjectByType<PlayerInventory>();
    }
}