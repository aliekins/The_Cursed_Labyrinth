using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class SwordDoor : MonoBehaviour
{
    [SerializeField] private SwordRoomSolver solver;
    [SerializeField] private KeyCode useKey = KeyCode.E;
    [SerializeField] private AudioClip insertSfx;
    [SerializeField] private GameObject insertVfx;
    [SerializeField] private float openDelay = 0.15f;

    private Collider2D col;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    private void OnEnable()
    {
        if (solver) solver.OnSolved += OnSolved;
    }
    private void OnDisable()
    {
        if (solver) solver.OnSolved -= OnSolved;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!Input.GetKeyDown(useKey)) return;
        var inv = other.GetComponent<PlayerInventory>();
        if (!inv) return;

        if (inv.RemoveSword(1))
        {
            if (insertSfx) 
                AudioSource.PlayClipAtPoint(insertSfx, transform.position, 1f);
            if (insertVfx)
                Destroy(Instantiate(insertVfx, transform.position, Quaternion.identity), 1.5f);
            if (solver) 
                solver.UseSword();
        }
        else
        {
            // Nothing for now
        }
    }

    private void OnSolved()
    {
        // Open the door
        if (col) col.enabled = false;

        // Optionally destroy the door object after a beat
        if (openDelay > 0f) Destroy(gameObject, openDelay);
        else Destroy(gameObject);
    }
}