using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PuzzleTokenPickup : MonoBehaviour
{
    public TokenType type;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (!PuzzleInventory.I) new GameObject("PuzzleInventory").AddComponent<PuzzleInventory>();
        PuzzleInventory.I.Add(type, 1);
        // TODO: SFX/VFX
        Destroy(gameObject);
    }
}