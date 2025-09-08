using UnityEngine;

/**
 * @file SwordDoor.cs
 * @brief Door in the sword room: consumes swords and opens when the puzzle is solved.
 * @ingroup Puzzle
 *
 * Press E while inside: if enough swords were used to complete the puzzle,
 * the door opens; otherwise it consumes one sword and progresses the solver.
 */

[RequireComponent(typeof(Collider2D))]
public sealed class SwordDoor : MonoBehaviour
{
    private KeyCode useKey = KeyCode.Q;

    [Header("Visuals / SFX (optional)")]
    [SerializeField] private AudioClip insertSfx;
    [SerializeField, Range(0, 1)] private float insertVolume = 1f;

    private SwordRoomSolver solver;
    private bool open;

    // trigger state
    private bool inside;
    private PlayerInventory currentInv;

    void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        solver = FindSolverOnClone();
        if (!solver)
            Debug.LogError("[SwordDoor] SwordRoomSolver not found under this special room prefab!");

        if (solver) solver.OnSolved += HandleSolved;
    }

    void OnDestroy()
    {
        if (solver) 
            solver.OnSolved -= HandleSolved;
    }

    void Update()
    {
        if (!inside || open) return;
        if (!Input.GetKeyDown(useKey)) return;
        if (!currentInv) return;

        if (solver && solver.IsSolved)
        {
            HandleSolved();
            return;
        }

        if (currentInv.RemoveSword(1))
        {
            solver?.UseSword();

            if (insertSfx)
                AudioSource.PlayClipAtPoint(insertSfx, transform.position, insertVolume);

            if (solver != null && solver.IsSolved)
                HandleSolved();
        }
        else
        {
            Debug.Log("[SwordDoor] You need more swords.");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var inv = other.GetComponent<PlayerInventory>();
        if (inv)
        {
            inside = true;
            currentInv = inv;

            Debug.Log("[SwordDoor] Player entered door area.");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<PlayerInventory>())
        {
            inside = false;
            currentInv = null;

            Debug.Log("[SwordDoor] Player left door area.");
        }
    }

    private void HandleSolved()
    {
        if (open) return;
        open = true;

        var col = GetComponent<Collider2D>();
        if (col) col.enabled = false;

        //if (animator) animator.SetBool("Open", true);

        Debug.Log("[SwordDoor] Door opened. (Puzzle solved)");
    }

    private SwordRoomSolver FindSolverOnClone()
    {
        Transform root = FindPrefabRoot(transform);

        return root ? root.GetComponentInChildren<SwordRoomSolver>(true) : null;
    }

    private static Transform FindPrefabRoot(Transform t)
    {
        Transform cur = t;
        Transform best = null;

        while (cur != null)
        {
            if (cur.GetComponentInChildren<Grid>(true)) 
                best = cur;
            cur = cur.parent;
        }

        return best ? best : t.root;
    }
}