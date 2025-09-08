using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class GhostHintTrigger : MonoBehaviour
{
    [SerializeField] private string hintTag = "inside";
    [SerializeField] private Vector2 localOffset = new Vector2(0.6f, 1.0f);
    [SerializeField] private bool oneShot = true;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Accept child colliders on the player, too
        if (!other.GetComponentInParent<PlayerInventory>()) return;

        var pos = transform.position + (Vector3)localOffset;
        bool ok = GhostHintController.ShowTaggedAtPosition(hintTag, pos);

        if (ok)
        {
            if (oneShot) Destroy(gameObject);
        }
        else
        {
            Debug.LogWarning($"[GhostHints] Trigger '{name}' failed to spawn (tag='{hintTag}'). Keeping trigger so you can retry.");
        }
    }
}