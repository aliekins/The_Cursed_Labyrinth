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
    [SerializeField] private string endingSceneName = "Cutscene_Ending";

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
        if (solver == null)  TryBind();

        if (!inside) return;

        // When portal is active, E should take you to the ending scene
        if (portalActive)
        {
            if (!loading && Input.GetKeyDown(advanceKey))
            {
                loading = true;
                LoadEnding();
            }
            return;
        }

        // Normal “next biome” gate
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

    #region binding + status
    // BiomeTransitionTrigger.cs  (inside TryBind)
    private void TryBind()
    {
        var root = FindNearestGridRoot(transform);
        if (!root) return;

        var monos = root.GetComponentsInChildren<MonoBehaviour>(true);

        foreach (var m in monos)
        {
            if (m is ISpecialSolver s)
            {
                if (solver != null)
                    solver.OnSolved -= HandleSolved;

                solver = s;
                solver.OnSolved += HandleSolved;

                UpdateIsFinalBiome();
                solved = IsSolverSolved();

                if (isFinalBiome && solved)
                    ActivatePortal();

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