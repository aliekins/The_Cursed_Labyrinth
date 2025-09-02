using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public sealed class BiomeTransitionTrigger : MonoBehaviour
{
    [Header("Gate")]
    [SerializeField] private bool requireSolved = true;
    private KeyCode advanceKey = KeyCode.E;


    [Header("Final Exit Portal")]
    [SerializeField] private GameObject portalPrefab;
    //[SerializeField] private ParticleSystem portalFxInPlace; 
    [SerializeField] private string endingSceneName = "EndingCutscene";
    //[SerializeField, Min(0f)] private float loadDelay = 0.5f;

    private BiomeSequenceController sequence;
    private ISpecialSolver solver;
    private bool inside;
    private bool solved;
    private bool advancing;
    private bool isFinalBiome;
    private bool portalActive;
    private bool loading;


    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (col)
            col.isTrigger = true;

        sequence = FindFirstObjectByType<BiomeSequenceController>();

        if (sequence)
            isFinalBiome = (sequence.currentBiomeIndex >= 0 && sequence.currentBiomeIndex == sequence.biomes.Count - 1);

        TryBind();
        SyncSolvedFromSolver();  // if puzzle was already solved
    }

    private void OnDestroy()
    {
        if (solver != null) 
            solver.OnSolved -= HandleSolved;
    }

    private void Update()
    {
        if (solver == null) { TryBind(); }
        if (portalActive) return; // final-portal mode: no E-gating

        if (!inside) return;

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
        if (other.GetComponent<PlayerInventory>()) inside = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<PlayerInventory>()) inside = false;
    }

    private void HandleSolved()
    {
        solved = true;
        if (isFinalBiome) ActivatePortal();
    }

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
        if (solver is LeverRoomSolver lr) return lr.IsSolved;
        if (solver is SwordRoomSolver sr) return sr.IsSolved;
        if (solver is CursedItemsSolver cr) return cr.IsSolved;   

        return false;
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

    #region helpers
    private void ActivatePortal()
    {
        if (portalActive) return;
        portalActive = true;

        if (portalPrefab)
        {
            var go = Instantiate(portalPrefab, transform.position, Quaternion.identity, transform.parent);
            Debug.Log("[BiomeTransition] Spawned portal prefab at trigger.");
        }
        //else if (portalFxInPlace)
        //{
        //    portalFxInPlace.Play(true);
        //    Debug.Log("[BiomeTransition] Played in-place portal particle system at trigger.");
        //}
        else
        {
            Debug.LogWarning("[BiomeTransition] Portal active, but no prefab/ParticleSystem assigned.");
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