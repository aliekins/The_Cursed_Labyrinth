using UnityEngine;

/**
 * @file PlayerInventoryInput.cs
 * @brief Minimal input bridge for inventory actions (drop special, use potion).
 * @ingroup PlayerInventory
 */

[RequireComponent(typeof(PlayerInventory))]
public sealed class PlayerInventoryInput : MonoBehaviour
{
    [SerializeField] private GameObject specialPickupPrefab;
    private KeyCode dropSpecialKey = KeyCode.E;
    private KeyCode usePotionKey = KeyCode.H;

    private PlayerInventory inv;

    private void Awake() => inv = GetComponent<PlayerInventory>();

    private void Update()
    {
        if (!inv) return;

        if (Input.GetKeyDown(dropSpecialKey) && inv.IsCarryingAny)
        {
            // Try a pedestal under/near the player
            var hits = Physics2D.OverlapCircleAll(transform.position, 1.0f);
            foreach (var h in hits)
            {
                var solver = h ? h.GetComponent<CursedItemsSolver>() : null;
                if (solver != null && solver.TryDepositFrom(inv))
                    return;
            }

            // No solver took it - drop to the ground
            inv.DropCarriedSpecial(null, transform.position);
        }

        if (Input.GetKeyDown(usePotionKey)) inv.UsePotion();
    }
}