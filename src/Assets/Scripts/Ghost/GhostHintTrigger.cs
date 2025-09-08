using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class GhostHintTrigger : MonoBehaviour
{
    [SerializeField] private string hintTag = "inside";
    [SerializeField] private Vector2 localOffset = new Vector2(10f, 10f);
    [SerializeField] private bool oneShot = true;

    private bool fired;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (fired && oneShot) return;
        if (!other.TryGetComponent<PlayerInventory>(out _)) return;

        fired = true;
        Debug.Log($"[GhostHintTrigger] Player entered trigger, showing hint '{hintTag}' at offset {localOffset}.");
        GhostHintController.ShowTaggedAtPosition(hintTag, transform.position + (Vector3)localOffset);

        if (oneShot) 
            Destroy(gameObject);
    }
}