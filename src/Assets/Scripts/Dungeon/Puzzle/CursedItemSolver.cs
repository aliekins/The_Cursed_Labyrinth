using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class CursedItemsSolver : MonoBehaviour, ISpecialSolver
{
    public event Action OnSolved;

    private KeyCode depositKey = KeyCode.E;

    [Header("State (read-only)")]
    [SerializeField] private bool skullDelivered = false;
    [SerializeField] private bool heartDelivered = false;
    [SerializeField] private bool crownDelivered = false;

    public bool IsSolved => skullDelivered && heartDelivered && crownDelivered;

    private bool playerInside;
    private PlayerInventory inv;

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    private void Update()
    {
        if (!playerInside || !inv) return;
        if (!Input.GetKeyDown(depositKey)) return;
        //if (IsSolved) return;

        if (inv.IsCarrying(Item.ItemType.SkullDiamond) && !skullDelivered)
        {
            inv.DropCarriedSpecial(null, transform.position);
            skullDelivered = true;
            CheckSolved();
            Debug.Log("[CursedItemsSolver] Delivered SKULL");

            return;
        }
        if (inv.IsCarrying(Item.ItemType.HeartDiamond) && !heartDelivered)
        {
            inv.DropCarriedSpecial(null, transform.position);
            heartDelivered = true;
            CheckSolved();
            Debug.Log("[CursedItemsSolver] Delivered HEART");

            return;
        }
        if (inv.IsCarrying(Item.ItemType.Crown) && !crownDelivered)
        {
            inv.DropCarriedSpecial(null, transform.position);
            crownDelivered = true;
            CheckSolved();
            Debug.Log("[CursedItemsSolver] Delivered CROWN");

            return;
        }
    }

    private void CheckSolved()
    {
        if (IsSolved)
        {
            Debug.Log("[CursedItemsSolver] SOLVED");
            OnSolved?.Invoke();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var pi = other.GetComponent<PlayerInventory>();

        if (pi) 
        { 
            playerInside = true;
            inv = pi;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<PlayerInventory>())
        {
            playerInside = false; inv = null;
        }
    }
}