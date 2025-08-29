using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Collider2D))]
public sealed class BiomeTransitionTrigger : MonoBehaviour
{
    [Header("Gate")]
    [SerializeField] private bool requireSolved = true;
    private KeyCode advanceKey = KeyCode.E;

    private BiomeSequenceController sequence;
    private ISpecialSolver solver;
    private bool inside;
    private bool solved;      // cached, but we’ll always re-check live on key press
    private bool advancing;

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (col)
            col.isTrigger = true;

        sequence = FindFirstObjectByType<BiomeSequenceController>();

        TryBind();
        SyncSolvedFromSolver();  // if puzzle already solved before we bound
    }

    private void OnDestroy()
    {
        if (solver != null) 
            solver.OnSolved -= HandleSolved;
    }

    private void Update()
    {
        if (solver == null) 
        { 
            TryBind();
        }

        if (!inside) return;

        if (Input.GetKeyDown(advanceKey))
        {
            // Re-check live state at the moment of pressing E
            bool solvedNow = solved || IsSolverSolved();

            if (requireSolved && !solvedNow)
            {
                Debug.Log("[BiomeTransition] Blocked: special room not solved yet.");
                return;
            }

            if (!advancing)
            {
                advancing = true;
                sequence?.NextBiome();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerInventory>()) inside = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<PlayerInventory>()) inside = false;
    }

    private void HandleSolved() => solved = true;

    #region building_checks
    private void TryBind()
    {
        var root = FindNearestGridRoot(transform);
        if (!root) return;

        // find an active ISpecialSolver on this special-room clone
        var monos = root.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var m in monos)
        {
            if (m is ISpecialSolver s)
            {
                // unsubscribe old solver (if any)
                if (solver != null) solver.OnSolved -= HandleSolved;

                solver = s;
                solver.OnSolved += HandleSolved;
                break;
            }
        }
    }
    private void SyncSolvedFromSolver()
    {
        if (IsSolverSolved()) solved = true;
    }
    private bool IsSolverSolved()
    {
        if (solver is LeverRoomSolver lr)
            return lr.IsSolved;

        if (solver is SwordRoomSolver sr)
            return sr.IsSolved;

        return false;
    }

    private static Transform FindNearestGridRoot(Transform t)
    {
        // Walk up to the special-room clone root (object that owns Grid/Tilemaps)
        var cur = t;
        while (cur != null)
        {
            if (cur.GetComponent<Grid>() || cur.GetComponentInChildren<Tilemap>(true))
                return cur;
            cur = cur.parent;
        }
        return t ? t.root : null;
    }
    #endregion
}