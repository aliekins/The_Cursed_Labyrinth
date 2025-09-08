using UnityEngine;

[DisallowMultipleComponent]
public sealed class GhostHintController : MonoBehaviour
{
    #region config
    public static GhostHintController Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private GhostHintDB hintDB;
    [SerializeField] private GhostPrefabSet prefabSet;
    [SerializeField] private Vector2 defaultScreenOffset = new Vector2(0.6f, 1.0f);

    [Header("Visibility")]
    [SerializeField] private string unityLayerName = "Default";

    [Header("Sorting (2D)")]
    [SerializeField] private int sortingOrder = 6;
    #endregion

    #region cycle
    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!hintDB) hintDB = Resources.Load<GhostHintDB>("GhostHintDB");
        if (!prefabSet) prefabSet = Resources.Load<GhostPrefabSet>("GhostPrefabSet");
    }
    #endregion

    #region api
    public static bool ShowTaggedAtPlayer(string hintTag)
    {
        var svc = Ensure();
        var player = FindAnyObjectByType<PlayerInventory>();

        if (!player)
        {
            Debug.LogWarning("[GhostHints] No PlayerInventory found."); 
            return false; 
        }

        var pos = new Vector3(player.transform.position.x, player.transform.position.y, 0f) + (Vector3)svc.defaultScreenOffset;

        return svc.SpawnTagged(hintTag, pos);
    }

    public static bool ShowTaggedAtPosition(string hintTag, Vector3 worldPos)
    {
        return Ensure().SpawnTagged(hintTag, worldPos);
    }
    #endregion

    #region orchestration
    private bool SpawnTagged(string hintTag, Vector3 worldPos)
    {
        if (!ValidateConfig(out string error))
        {
            Debug.LogWarning(error);
            return false;
        }

        int biome = GetBiomeIndex();
        var hint = ResolveHint(biome, hintTag);

        if (hint == null)
        {
            Debug.Log($"[GhostHints] No line for tag '{hintTag}' and no generic line for biome {biome}.");
            return false;
        }

        if (!TryGetGhostPrefab(biome, out var prefab))
        {
            Debug.Log($"[GhostHints] No ghost prefab for biome {biome} in PrefabSet.");
            return false;
        }

        worldPos = WithZ(worldPos, 0f);

        var go = InstantiateTopLevel(prefab, worldPos);
        ApplySortingAndLayer(go);

        var agent = GetOrAddAgent(go);
        RunAgent(agent, worldPos, hint);

        Debug.Log($"[GhostHints] Spawned hint tag='{hintTag}' biome={biome} at {worldPos}.");
        return true;
    }
    #endregion

    #region helpers
    private bool ValidateConfig(out string error)
    {
        if (!hintDB) 
        {
            error = "[GhostHints] Missing GhostHintDB."; 
            return false;
        }

        if (!prefabSet) 
        {
            error = "[GhostHints] Missing GhostPrefabSet.";
            return false;
        }

        error = null;
        return true;
    }

    private int GetBiomeIndex()
    {
        var seq = FindAnyObjectByType<BiomeSequenceController>();
        if (!seq) return 0;

        return Mathf.Clamp(seq.currentBiomeIndex, 0, prefabSet.MaxBiomeIndex);
    }

    private GhostHintDB.Hint ResolveHint(int biome, string tag)
    {
        var h = hintDB.GetNextHintTagged(biome, tag);
        return h ?? hintDB.GetNextHint(biome);
    }

    private bool TryGetGhostPrefab(int biome, out GameObject prefab)
    {
        prefab = prefabSet.GetForBiome(biome);
        return prefab != null;
    }

    private static Vector3 WithZ(Vector3 p, float z) => new Vector3(p.x, p.y, z);

    private static GameObject InstantiateTopLevel(GameObject prefab, Vector3 pos)
    {
        return Instantiate(prefab, pos, Quaternion.identity, null);
    }
    private void ApplySortingAndLayer(GameObject root)
    {
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            r.sortingOrder = sortingOrder;
        }

        var cam = GetActiveCamera();
        foreach (var cv in root.GetComponentsInChildren<Canvas>(true))
        {
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.overrideSorting = true;
            cv.sortingOrder = sortingOrder;
            cv.worldCamera = cam;
        }

        int layer = LayerMask.NameToLayer(unityLayerName);
        if (layer >= 0) 
            SetLayerRecursively(root, layer);
    }

    private static GhostHintAgent GetOrAddAgent(GameObject root)
    {
        var agent = root.GetComponent<GhostHintAgent>() ??
                    root.GetComponentInChildren<GhostHintAgent>(true);

        if (!agent)
        {
            agent = root.AddComponent<GhostHintAgent>();
            Debug.Log("[GhostHints] Ghost prefab had no GhostHintAgent; added one at runtime.");
        }
        return agent;
    }

    private static void RunAgent(GhostHintAgent agent, Vector3 worldPos, GhostHintDB.Hint hint)
    {
        agent.AppearAt(null, worldPos);
        agent.Speak(hint.text, hint.voice);
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        foreach (Transform t in root.transform)
            SetLayerRecursively(t.gameObject, layer);
    }

    private static Camera GetActiveCamera()
    {
        var cam = Camera.main;

        if (cam && cam.enabled) return cam;

        foreach (var c in Camera.allCameras)
            if (c && c.enabled) return c;

        return null;
    }

    private static GhostHintController Ensure()
    {
        if (Instance) return Instance;

        var go = new GameObject("GhostHintController");
        return go.AddComponent<GhostHintController>();
    }
    #endregion
}