using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerInventory : MonoBehaviour
{
    [Header("Healing")]
    [SerializeField, Min(1)] private int potionHealAmount = 20;

    [Header("Biome 3")]
    [SerializeField] private Item.ItemType? carriedSpecial = null;

    public int Swords { get; private set; }
    public int Potions { get; private set; }

    private readonly bool[] books = new bool[5];

    public Item.ItemType? CarriedSpecial { get; private set; } = null;

    public struct Snapshot
    {
        public int swords;
        public int potions;
        public bool[] books;
        public Item.ItemType? carried;
    }
    public event Action<Snapshot> Changed;

    private PlayerHealth health;
    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
        PushChanged();
    }

    #region API
    public void Add(Item.ItemType type, int amount = 1)
    {
        if (amount <= 0) return;

        switch (type)
        {
            case Item.ItemType.Sword:
                Swords += amount;
                Debug.Log($"[Inventory] Added {amount} sword(s), now have {Swords}", this);
                break;

            case Item.ItemType.HealthPotion:
                Potions += amount;
                break;

            case Item.ItemType.Book1:
            case Item.ItemType.Book2:
            case Item.ItemType.Book3:
            case Item.ItemType.Book4:
            case Item.ItemType.Book5:
                int idx = BookIndex(type);
                if (idx >= 0) 
                    books[idx] = true;
                break;
        }
        PushChanged();
    }

    public event System.Action<Item.ItemType?> CarriedSpecialChanged;

    /// Try to place a special item into the carry slot. Returns false if already carrying something else.
    public bool TryCarrySpecial(Item.ItemType t)
    {
        if (carriedSpecial.HasValue) return false;

        carriedSpecial = t;
        CarriedSpecialChanged?.Invoke(carriedSpecial);
        return true;
    }

    /// Drop the carried special. If dropPrefab is provided, it will be instantiated at 'pos'
    public void DropCarriedSpecial(GameObject dropPrefab, Vector3 pos)
    {
        if (!carriedSpecial.HasValue) return;

        GameObject go;
        if (dropPrefab)
        {
            go = Instantiate(dropPrefab, pos, Quaternion.identity);
        }
        else
        {
            go = new GameObject($"pickup_{carriedSpecial.Value}");
            go.transform.position = pos;
            var c = go.AddComponent<CircleCollider2D>();
            c.isTrigger = true;
        }

        var pi = go.GetComponent<PickupItem>() ?? go.AddComponent<PickupItem>();
        // Set fields BEFORE registering so it's recognized as cursed
        pi.Type = carriedSpecial.Value;
        pi.Quantity = 1;
        pi.isSpecial = true;
        pi.autoPickup = false;

        // Robust registration
        CursedItemRespawnManager.RegisterPickup(pi);

        carriedSpecial = null;
        CarriedSpecialChanged?.Invoke(null);
    }


    public void ConsumeCarriedSpecial()
    {
        if (carriedSpecial == null) return;
        var t = carriedSpecial.Value;
        carriedSpecial = null;
        CarriedSpecialChanged?.Invoke(null); // keep UI in sync if present
        Debug.Log($"[PlayerInventory] Consumed special: {t}");
    }

    public bool IsCarryingAny => carriedSpecial.HasValue;
    public bool IsCarrying(Item.ItemType t) => carriedSpecial.HasValue && carriedSpecial.Value == t;


    public bool RemoveSword(int amount = 1)
    {
        if (amount <= 0 || Swords < amount) return false;

        Swords -= amount;
        PushChanged();
        return true;
    }

    public bool UsePotion()
    {
        if (Potions <= 0) return false;
        Potions--;

        if (health)
        {
            health.Heal(potionHealAmount);
            Debug.Log($"[Inventory] Used potion to heal {potionHealAmount} HP.", this);
        }

        PushChanged();
        return true;
    }

    public bool HasBook(Item.ItemType t)
    {
        int i = BookIndex(t);
        return i >= 0 && books[i];
    }

    public bool AllBooksFilled()
    {
        for (int i = 0; i < books.Length; i++) if (!books[i]) return false;
        return true;
    }
    #endregion

    #region specialCarry
    public bool TryPickupSpecial(Item.ItemType t)
    {
        if (CarriedSpecial != null) return false;
        if (t != Item.ItemType.SkullDiamond && t != Item.ItemType.HeartDiamond && t != Item.ItemType.Crown)
            return false;

        CarriedSpecial = t;
        PushChanged();
        return true;
    }
    #endregion

    #region snapshots
    public Snapshot GetSnapshot() => new Snapshot
    {
        swords = Swords,
        potions = Potions,
        books = (bool[])books.Clone(),
        carried = CarriedSpecial
    };

    public void ApplySnapshot(Snapshot s)
    {
        Swords = s.swords;
        Potions = s.potions;
        if (s.books != null && s.books.Length == books.Length)
            Array.Copy(s.books, books, books.Length);
        CarriedSpecial = s.carried;
        PushChanged();
    }
    #endregion

    #region helpers

    private static int BookIndex(Item.ItemType t) => t switch
    {
        Item.ItemType.Book1 => 0,
        Item.ItemType.Book2 => 1,
        Item.ItemType.Book3 => 2,
        Item.ItemType.Book4 => 3,
        Item.ItemType.Book5 => 4,
        _ => -1
    };

    private void PushChanged() => Changed?.Invoke(GetSnapshot());
    #endregion
}