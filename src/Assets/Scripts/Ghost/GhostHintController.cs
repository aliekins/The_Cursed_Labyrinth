using UnityEngine;

[DisallowMultipleComponent]
public sealed class GhostHintController : MonoBehaviour
{
    #region setup
    public static GhostHintController Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private GhostHintDB hintDB;
    [SerializeField] private GhostPrefabSet prefabSet;
    [SerializeField] private Vector2 defaultScreenOffset = new Vector2(10f, 10f);

    private Transform gridParent;

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
        TryCacheGridParent();
    }

    private void TryCacheGridParent()
    {
        var dc = FindAnyObjectByType<DungeonController>(FindObjectsInactive.Include);

        if (dc && dc.Viz) gridParent = dc.Viz.GridTransform;
    }
    #endregion

    #region api
    public static void ShowTaggedAtPlayer(string hintTag)
    {
        var svc = Ensure();

        var player = FindAnyObjectByType<PlayerInventory>(FindObjectsInactive.Include);

        if (!player) return;
        svc.SpawnTagged(hintTag, player.transform.position + (Vector3)svc.defaultScreenOffset);
    }

    public static void ShowTaggedAtPosition(string hintTag, Vector3 worldPos)
    {
        Ensure().SpawnTagged(hintTag, worldPos);
    }
    #endregion

    #region helpers
    private void SpawnTagged(string hintTag, Vector3 worldPos)
    {
        if (!hintDB || !prefabSet)
        {
            Debug.LogWarning("[GhostHintController] Missing DB or PrefabSet.");
            return;
        }

        int biomeIndex = 0;
        var seq = FindAnyObjectByType<BiomeSequenceController>(FindObjectsInactive.Include);
        if (seq) biomeIndex = Mathf.Clamp(seq.currentBiomeIndex, 0, prefabSet.MaxBiomeIndex);

        var hint = hintDB.GetNextHintTagged(biomeIndex, hintTag) ?? hintDB.GetNextHint(biomeIndex);
        if (hint == null)
        {
            return;
        }

        var ghostPrefab = prefabSet.GetForBiome(biomeIndex);
        if (!ghostPrefab)
        {
            return;
        }

        Transform parent = null; 
        var go = Instantiate(ghostPrefab, worldPos, Quaternion.identity, parent);

        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            r.sortingOrder = 6;
        }
        
        var agent = go.GetComponent<GhostHintAgent>()
                 ?? go.GetComponentInChildren<GhostHintAgent>(true);

        if (!agent)
        {
            agent = go.AddComponent<GhostHintAgent>();
        }

        agent.AppearAt(gridParent, worldPos);
        agent.Speak(hint.text, hint.voice);

        Debug.Log($"[GhostHintController] Spawned hint (tag='{hintTag}', biome={biomeIndex}).");
    }


    private static GhostHintController Ensure()
    {
        if (Instance) return Instance;
        var go = new GameObject("GhostHintController");

        return go.AddComponent<GhostHintController>();
    }
    #endregion
}