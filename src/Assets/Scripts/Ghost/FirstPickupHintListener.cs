using UnityEngine;
using System;

[DisallowMultipleComponent]
public sealed class FirstPickupHintListener : MonoBehaviour
{
    [Header("Tags in GhostHintDB")]
    [SerializeField] private string firstSwordTag = "first_sword";
    [SerializeField] private string firstBookTag = "first_book";
    [SerializeField] private string firstCursedTag = "first_cursed";

    private PlayerInventory inv;

    private int lastSwordCount;
    private bool hadAnyBook;

    private bool hintedSword;
    private bool hintedBook;
    private bool hintedCursed;

    private static readonly Item.ItemType[] BookTypes = new[]
    {
        Item.ItemType.Book1,
        Item.ItemType.Book2,
        Item.ItemType.Book3,
        Item.ItemType.Book4,
        Item.ItemType.Book5
    };

    void Awake()
    {
        inv = GetComponent<PlayerInventory>();
        Snapshot();
    }

    void OnEnable()
    {
        if (inv != null) inv.CarriedSpecialChanged += OnCarriedSpecialChanged;
    }

    void OnDisable()
    {
        if (inv != null) inv.CarriedSpecialChanged -= OnCarriedSpecialChanged;
    }

    void Update()
    {
        if (!inv) return;

        if (!hintedSword && lastSwordCount == 0 && inv.Swords > 0)
        {
            hintedSword = true;
            Debug.Log("[FirstPickupHintListener] First sword acquired, showing hint.");
            GhostHintController.ShowTaggedAtPlayer(firstSwordTag);
        }

        if (!hintedBook)
        {
            bool anyBookNow = HasAnyBook(inv);
            if (!hadAnyBook && anyBookNow)
            {
                hintedBook = true;
                Debug.Log("[FirstPickupHintListener] First book acquired, showing hint.");
                GhostHintController.ShowTaggedAtPlayer(firstBookTag);
            }
            hadAnyBook = anyBookNow;
        }

        lastSwordCount = inv.Swords;
    }

    private void OnCarriedSpecialChanged(Nullable<Item.ItemType> carried)
    {
        if (!hintedCursed && carried.HasValue)
        {
            hintedCursed = true;
            Debug.Log("[FirstPickupHintListener] First special item carried, showing cursed hint.");
            GhostHintController.ShowTaggedAtPlayer(firstCursedTag);
        }
    }

    private void Snapshot()
    {
        lastSwordCount = inv ? inv.Swords : 0;
        hadAnyBook = inv && HasAnyBook(inv);
    }

    private static bool HasAnyBook(PlayerInventory inv)
    {
        foreach (var bt in BookTypes)
        {
            if (inv.HasBook(bt))
            {
                return true; 
            }
        }

        return false;
    }
}