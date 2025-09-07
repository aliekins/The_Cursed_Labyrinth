using System;
using UnityEngine;

/**
 * @file CursedItemSolver.cs
 * @brief Pedestal logic for the cursed items puzzle (Skull, Heart, Crown).
 * @ingroup Puzzle
 *
 * Each pedestal accepts a single expected item.
 * On correct item: consume and mark global delivered flag.
 * On wrong item: consume and immediately respawn the item elsewhere.
 * Fires OnSolved once when all three items are delivered correctly.
 */

[RequireComponent(typeof(Collider2D))]
public sealed class CursedItemsSolver : MonoBehaviour, ISpecialSolver
{
    #region config
    [Header("This pedestal accepts:")]
    [SerializeField] private Item.ItemType expected = Item.ItemType.SkullDiamond; // set per trigger in Inspector

    [Header("Global state (shared)")]
    [SerializeField] private static bool skullDelivered = false;
    [SerializeField] private static bool heartDelivered = false;
    [SerializeField] private static bool crownDelivered = false;
    private bool playerInside;
    private PlayerInventory inv;
    #endregion

    #region instantiation
    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetRunState()
    {
        skullDelivered = false;
        heartDelivered = false;
        crownDelivered = false;
    }
    #endregion

    #region access
    public event Action OnSolved;
    public bool IsSolved => skullDelivered && heartDelivered && crownDelivered;
    public static void ResetNow() => ResetRunState();

    public bool TryDepositFrom(PlayerInventory who)
    {
        if (!playerInside || !who || who != inv) return false;

        // Which cursed item (if any) is carried?
        Item.ItemType carried;
        if (who.IsCarrying(Item.ItemType.SkullDiamond))
            carried = Item.ItemType.SkullDiamond;
        else if (who.IsCarrying(Item.ItemType.HeartDiamond)) 
            carried = Item.ItemType.HeartDiamond;
        else if (who.IsCarrying(Item.ItemType.Crown))
            carried = Item.ItemType.Crown;
        else return false; // not carrying a cursed item

        // Correct pedestal?
        if (carried == expected)
        {
            // consume + set the right global flag
            who.ConsumeCarriedSpecial();

            switch (carried)
            {
                case Item.ItemType.SkullDiamond:
                    if (!skullDelivered)
                        skullDelivered = true;
                    break;
                case Item.ItemType.HeartDiamond: 
                    if (!heartDelivered)
                        heartDelivered = true;
                    break;
                case Item.ItemType.Crown: 
                    if (!crownDelivered)
                        crownDelivered = true;
                    break;
            }

            if (IsSolved)
            {
                Debug.Log("[CursedItemsSolver] SOLVED");
                OnSolved?.Invoke();
            }

            Debug.Log($"[CursedItemsSolver] Delivered {carried}");
            Debug.Log("[CursedItemsSolver] State: " +
                (skullDelivered ? "SKULL " : "") +
                (heartDelivered ? "HEART " : "") +
                (crownDelivered ? "CROWN " : ""));

            return true;
        }

        // Wrong pedestal - respawn elsewhere
        // Clear carry
        who.ConsumeCarriedSpecial();

        // Spawn a pickup for that item now
        var go = new GameObject($"pickup_{carried}_wrong");
        go.transform.position = transform.position;

        var trig = go.AddComponent<CircleCollider2D>(); trig.isTrigger = true;
        var pu = go.AddComponent<PickupItem>();

        pu.Type = carried;
        pu.Quantity = 1;
        pu.isSpecial = true;
        pu.autoPickup = false;

        if (!CursedItemRespawnManager.ForceRelocateNow(pu))
        {
            // If immediate relocation fails for any reason, fall back to timer-based respawn.
            CursedItemRespawnManager.RegisterPickup(pu);
            Debug.LogWarning("[CursedItemsSolver] Wrong pedestal - queued for respawn (no immediate slot found).");
        }
        else
        {
            // After forced relocation, keep the timer alive so it won’t get stuck later
            CursedItemRespawnManager.RegisterPickup(pu);
            Debug.Log($"[CursedItemsSolver] Wrong pedestal - {carried} respawned elsewhere.");
        }

        return true;
    }
    #endregion

    #region triggers
    private void OnTriggerEnter2D(Collider2D other)
    {
        var pi = other.GetComponent<PlayerInventory>();
        if (pi) { playerInside = true; inv = pi; }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<PlayerInventory>())
        {
            playerInside = false; inv = null;
        }
    }
    #endregion
}