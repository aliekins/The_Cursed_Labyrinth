using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public sealed class InventoryUI : MonoBehaviour
{
    [SerializeField] private DungeonController controller; 

    [SerializeField] private Image swordIcon;
    [SerializeField] private TMP_Text swordText;
    [SerializeField] private Image swordDimOverlay;

    [SerializeField] private Image potionIcon;
    [SerializeField] private TMP_Text potionText;
    [SerializeField] private Image potionDimOverlay;

    [SerializeField] private Image[] bookSlots = new Image[5];
    [SerializeField] private Color emptyTint = new Color(1, 1, 1, 0.25f);
    [SerializeField] private Color filledTint = Color.white;

    private PlayerInventory inventory; // currently bound inventory

    private void Awake()
    {
        if (!controller) controller = FindFirstObjectByType<DungeonController>(FindObjectsInactive.Exclude);
    }

    private void OnEnable()
    {
        if (controller) controller.PlayerSpawned += OnPlayerSpawned;
        TryBindExistingInventory();
    }

    private void OnDisable()
    {
        if (controller) controller.PlayerSpawned -= OnPlayerSpawned;
        UnhookInventory();
    }

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

    private void OnInvChanged(PlayerInventory.Snapshot s)
    {
        if (swordText) swordText.text = s.swords.ToString();
        if (potionText) potionText.text = s.potions.ToString();

        if (bookSlots != null && bookSlots.Length == 5 && s.books != null)
        {
            for (int i = 0; i < 5; i++)
                if (bookSlots[i]) bookSlots[i].color = s.books[i] ? filledTint : emptyTint;
        }
    }

    private void OnUsePotionClicked()
    {
        if (inventory) inventory.UsePotion();
    }
}