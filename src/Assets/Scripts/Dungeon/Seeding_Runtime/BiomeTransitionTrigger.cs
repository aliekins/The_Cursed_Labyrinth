using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Collider2D))]
public sealed class BiomeTransitionTrigger : MonoBehaviour
{
    [SerializeField] private bool requireSolved = true;
    [SerializeField] private KeyCode advanceKey = KeyCode.E;

    private BiomeSequenceController sequence;
    private ISpecialSolver solver;
    private bool solved;
    private bool inside;
    private bool advancing;

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;

        sequence = FindFirstObjectByType<BiomeSequenceController>();

        // bind now (works because sign and solver are in the same clone)
        TryBindSolver();
        SyncSolvedFromKnownSolvers();
    }

    private void OnDestroy()
    {
        if (solver != null) solver.OnSolved -= HandleSolved;
    }

    private void Update()
    {
        // late-bind safety: if cloning order made us miss it, keep trying
        if (solver == null && !solved) {
            TryBindSolver(); 
            SyncSolvedFromKnownSolvers(); }

        if (!inside) return;

        if (Input.GetKeyDown(advanceKey))
        {
            if (requireSolved && !solved)
            {
                Debug.Log("[BiomeTransition] Blocked: special room not solved yet.");
                return;
            }
            if (!advancing)
            {
                advancing = true; // debounce
                sequence?.NextBiome(); // sequence handles clear/seed/build
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

    private void TryBindSolver()
    {
        var root = FindPrefabRoot(transform);
        if (!root) 
            return;

        var monos = root.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < monos.Length; i++)
        {
            if (monos[i] is ISpecialSolver s)
            {
                solver = s;
                solver.OnSolved += HandleSolved;
                break;
            }
        }
    }

    private void SyncSolvedFromKnownSolvers()
    {
        if (solver == null) return;

        // if your solvers expose IsSolved, reflect it here
        if (solver is LeverRoomSolver lever && lever.IsSolved) { solved = true; return; }
        if (solver is SwordRoomSolver sword && sword.IsSolved) { solved = true; return; }
    }

    private static Transform FindPrefabRoot(Transform t)
    {
        // climb until we reach the clone root that owns Grid/Tilemaps
        Transform cur = t, best = null;
        while (cur != null)
        {
            if (cur.GetComponentInChildren<Tilemap>(true) || cur.GetComponentInChildren<Grid>(true))
                best = cur;
            cur = cur.parent;
        }
        return best ? best : t?.root;
    }
}
