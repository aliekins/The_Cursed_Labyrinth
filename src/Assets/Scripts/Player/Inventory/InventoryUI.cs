using UnityEngine;
using UnityEngine.UI;
using TMPro;

/**
 * @file InventoryUI.cs
 * @brief Small HUD that mirrors PlayerInventory counts.
 * @ingroup PlayerInventory
 */

[DisallowMultipleComponent]
public sealed class InventoryUI : MonoBehaviour
{
    #region config
    [SerializeField] private DungeonController controller; 

    [SerializeField] private Image swordIcon;
    [SerializeField] private TMP_Text swordText;

    [SerializeField] private Image potionIcon;
    [SerializeField] private TMP_Text potionText;

    [SerializeField] private Image[] bookSlots = new Image[5];

    [SerializeField] private Color notFoundTint = new Color(1, 1, 1, 0.25f);
    [SerializeField] private Color emptyTint = new Color(1, 1, 1, 0.25f);
    [SerializeField] private Color filledTint = Color.white;

    private PlayerInventory inventory;
    #endregion

    #region cycle
    private void Awake()
    {
        if (!controller)
            controller = FindFirstObjectByType<DungeonController>(FindObjectsInactive.Exclude);
    }

    private void OnEnable()
    {
        if (controller)
            controller.PlayerSpawned += OnPlayerSpawned;

        TryBindExistingInventory();
    }

    private void OnDisable()
    {
        if (controller)
            controller.PlayerSpawned -= OnPlayerSpawned;

        UnhookInventory();
    }
    #endregion

    #region (un)hooking inventory
    private void HookInventory(PlayerInventory inv)
    {
        if (!inv) return;

        inventory = inv;
        inventory.Changed += OnInvChanged;

        OnInvChanged(inventory.GetSnapshot());
    }

    private void UnhookInventory()
    {
        if (!inventory) return;

        inventory.Changed -= OnInvChanged;
        inventory = null;
    }
    #endregion

    #region helpers
    private void OnPlayerSpawned(PlayerInventory inv)
    {
        UnhookInventory();
        HookInventory(inv);
    }

    private void TryBindExistingInventory()
    {
        if (inventory) return;

        var inv = FindFirstObjectByType<PlayerInventory>(FindObjectsInactive.Exclude);

        if (inv)
            HookInventory(inv);
    }

    private void OnInvChanged(PlayerInventory.Snapshot s)
    {
        bool hasSword = s.swords > 0;
        if (swordText)
            swordText.text = s.swords.ToString();
        if (swordIcon)
            swordIcon.color = hasSword ? filledTint : notFoundTint;

        bool hasPotion = s.potions > 0;
        if (potionText)
            potionText.text = s.potions.ToString();
        if (potionIcon)
            potionIcon.color = hasPotion ? filledTint : notFoundTint;

        if (bookSlots != null && s.books != null && s.books.Length == 5)
        {
            for (int i = 0; i < 5; i++)
            {
                bool filled = s.books[i];
                if (bookSlots[i])
                    bookSlots[i].color = filled ? filledTint : emptyTint;
            }
        }
    }
    #endregion
}