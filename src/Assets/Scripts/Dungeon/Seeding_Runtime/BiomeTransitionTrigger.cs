using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;

/**
 * @file BiomeTransitionTrigger.cs
 * @brief Sign trigger that advances to the next biome or, on the final biome, spawns a portal and loads the ending.
 * @ingroup SeedingRT
 */

[RequireComponent(typeof(Collider2D))]
public sealed class BiomeTransitionTrigger : MonoBehaviour
{
    #region config
    [Header("Gate")]
    [SerializeField] private bool requireSolved = true;
    private KeyCode advanceKey = KeyCode.E;

    [Header("Final Exit Portal")]
    [SerializeField] private GameObject portalPrefab;
    [SerializeField] private string endingSceneName = "Cutscene_Ending";

    private BiomeSequenceController sequence;
    private ISpecialSolver solver;
    private bool inside;
    private bool solved;
    private bool advancing;
    private bool isFinalBiome;
    private bool portalActive;
    private bool loading;
    #endregion

    #region lifecycle
    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;

        sequence = FindFirstObjectByType<BiomeSequenceController>();

        UpdateIsFinalBiome();

        TryBind();
        SyncSolvedFromSolver();

        if (isFinalBiome && solved)
            ActivatePortal();
    }

    private void OnDestroy()
    {
        if (solver != null) solver.OnSolved -= HandleSolved;
    }

    private void Update()
    {
        if (solver == null) TryBind();

        UpdateIsFinalBiome();
        if (!portalActive && isFinalBiome && (solved || IsSolverSolved()))
        {
            ActivatePortal();
        }

        if (!inside) return;

        if (portalActive)
        {
            if (!loading && Input.GetKeyDown(advanceKey))
            {
                loading = true;
                LoadEnding();
            }
            return;
        }

        if (Input.GetKeyDown(advanceKey))
        {
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
        if (other.GetComponent<PlayerInventory>()) 
            inside = true;
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<PlayerInventory>()) 
            inside = false;
    }

    private void HandleSolved()
    {
        solved = true;
        UpdateIsFinalBiome();

        if (isFinalBiome)
            ActivatePortal();
    }
    #endregion

    #region binding + status
    private void TryBind()
    {
        var root = FindNearestGridRoot(transform);
        if (!root) return;

        var monos = root.GetComponentsInChildren<MonoBehaviour>(true);

        ISpecialSolver best = null;
        float bestSqr = float.PositiveInfinity;

        foreach (var m in monos)
        {
            if (m is ISpecialSolver s && s is Component c && c.gameObject.activeInHierarchy)
            {
                float sqr = (c.transform.position - transform.position).sqrMagnitude;

                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = s; 
                }
            }
        }

        if (best == null) return;
        if (!ReferenceEquals(best, solver))
        {
            if (solver != null) 
                solver.OnSolved -= HandleSolved;

            solver = best;
            solver.OnSolved += HandleSolved;

            solved = IsSolverSolved();
            UpdateIsFinalBiome();

            Debug.Log($"[BiomeTransition] Bound to solver '{solver.GetType().Name}' at ~{Mathf.Sqrt(bestSqr):0.0} units. Solved={solved}");

            if (isFinalBiome && solved) 
                ActivatePortal();
        }
        else
        {
            bool wasSolved = solved;
            solved = solved || IsSolverSolved();

            if (!portalActive && isFinalBiome && solved && !wasSolved)
                ActivatePortal();
        }
    }

    private void SyncSolvedFromSolver()
    {
        if (IsSolverSolved()) solved = true;
    }

    private bool IsSolverSolved()
    {
        if (solver is LeverRoomSolver lr) return lr.IsSolved;
        if (solver is SwordRoomSolver sr) return sr.IsSolved;
        if (solver is CursedItemsSolver cr) return cr.IsSolved;  

        return false;
    }

    private void UpdateIsFinalBiome()
    {
        if (!sequence)
        { 
            isFinalBiome = false;
            return; 
        }

        isFinalBiome = (sequence.currentBiomeIndex >= 0 && sequence.currentBiomeIndex == sequence.biomes.Count - 1);
    }

    private static Transform FindNearestGridRoot(Transform t)
    {
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

    #region portal
    private void ActivatePortal()
    {
        if (portalActive) return;
        portalActive = true;

        if (portalPrefab)
        {
            var portal = Instantiate(portalPrefab, transform.position, Quaternion.identity, transform.parent);
            var exit = portal.GetComponent<PortalExit>();

            if (exit) 
                exit.SetScene(endingSceneName);

            Debug.Log("[BiomeTransition] Spawned portal prefab at trigger.");
        }
        else
        {
            Debug.LogWarning("[BiomeTransition] Portal active, but no prefab assigned.");
        }
    }

    private void LoadEnding()
    {
        if (string.IsNullOrEmpty(endingSceneName))
        {
            Debug.LogError("[BiomeTransition] endingSceneName is empty.");
            return;
        }
        SceneManager.LoadScene(endingSceneName, LoadSceneMode.Single);
    }
    #endregion
}