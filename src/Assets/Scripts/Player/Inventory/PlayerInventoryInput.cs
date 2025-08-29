using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
public sealed class PlayerInventoryInput : MonoBehaviour
{
    private KeyCode dropSpecialKey = KeyCode.E;
    private KeyCode usePotionKey = KeyCode.H;

    private PlayerInventory inv;

    private void Awake() => inv = GetComponent<PlayerInventory>();

    private void Update()
    {
        if (!inv) return;

        if (Input.GetKeyDown(dropSpecialKey) && inv.CarriedSpecial != null)
        {
            inv.DropCarriedSpecial(null, transform.position);
        }

        if (Input.GetKeyDown(usePotionKey))
        {
            inv.UsePotion();
        }
    }
}