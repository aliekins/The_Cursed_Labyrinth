using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerInventory : MonoBehaviour
{
    [Header("Healing")]
    [SerializeField, Min(1)] private int potionHealAmount = 20;

    public int Swords { get; private set; }
    public int Potions { get; private set; }

    // fixed-order book slots
    private readonly bool[] books = new bool[5];

    // events
    public struct Snapshot
    {
        public int swords;
        public int potions;
        public bool[] books;
    }
    public event Action<Snapshot> Changed;

    private PlayerHealth health; // to consume potions

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
                if (idx >= 0 && idx < books.Length)
                    books[idx] = true;       // placed into its fixed slot
                break;
        }
        PushChanged();
    }

    public bool HasBook(Item.ItemType t) => books[BookIndex(t)];
    public bool AllBooksFilled()
    {
        for (int i = 0; i < books.Length; i++) if (!books[i]) return false;
        return true;
    }

    public bool UsePotion()
    {
        if (Potions <= 0) return false;
        Potions--;
        if (health) health.Heal(potionHealAmount);
        PushChanged();
        return true;
    }
    public bool RemoveSword(int amount = 1)
    {
        if (Swords < amount) return false;
        Swords -= amount;
        PushChanged();
        return true;
    }

    public Snapshot GetSnapshot() => new Snapshot
    {
        swords = Swords,
        potions = Potions,
        books = (bool[])books.Clone()
    };
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
    // H for consuming potion
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
            UsePotion();
    }
    #endregion
}