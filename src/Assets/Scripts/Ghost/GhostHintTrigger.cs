using UnityEngine;

/// @file GhostHintTrigger.cs
/// @brief One shot world trigger that spawns the biome specific ghost prefab and speaks.
/// @ingroup Puzzle
[RequireComponent(typeof(Collider2D))]
public sealed class GhostHintTrigger : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private GhostHintDB hintDB;
    [SerializeField] private GhostPrefabSet prefabSet;

    [Header("Hint selection")]
    [SerializeField] private string hintTag = "";
    [SerializeField] private bool useSpecificOrder = false;
    [SerializeField, Min(0)] private int orderIndex = 0;

    [Header("Spawn")]
    [SerializeField] private Vector2 worldOffset = new(0f, 0.6f);
    [SerializeField] private bool oneShot = true;

    private bool consumed;

    private void Reset()
    {
        var c = GetComponent<Collider2D>();
        if (c) c.isTrigger = true;
    }

    // Called by the placer to wire assets + tag + optional index
    public void Configure(GhostHintDB db, GhostPrefabSet set, string tag = "", int? forcedOrderIndex = null)
    {
        hintDB = db;
        prefabSet = set;
        hintTag = tag ?? "";

        if (forcedOrderIndex.HasValue)
        {
            useSpecificOrder = true;
            orderIndex = Mathf.Max(0, forcedOrderIndex.Value);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed) return;
        if (!other.GetComponent<PlayerInventory>()) return;

        if (!hintDB || !prefabSet)
        {
            Debug.LogWarning("[GhostHintTrigger] Missing DB or PrefabSet.");
            return;
        }

        var seq = FindFirstObjectByType<BiomeSequenceController>();
        var dc = FindFirstObjectByType<DungeonController>();
        int biomeIndex = seq ? seq.currentBiomeIndex : 0;

        GhostHintDB.Hint hint = null;

        if (useSpecificOrder)
            hint = hintDB.GetHintAtTagged(biomeIndex, hintTag, orderIndex);
        if (hint == null)
            hint = hintDB.GetNextHintTagged(biomeIndex, hintTag);
        if (hint == null)
            hint = hintDB.GetNextHint(biomeIndex);

        var ghostPrefab = prefabSet.GetForBiome(biomeIndex);
        if (ghostPrefab == null || hint == null) return;

        Transform parent = (dc && dc.Viz) ? dc.Viz.GridTransform : null;
        var pos = transform.position + (Vector3)worldOffset;

        var ghostGO = Instantiate(ghostPrefab, pos, Quaternion.identity, parent);
        var agent = ghostGO.GetComponent<GhostHintAgent>();

        if (agent)
        {
            agent.AppearAt(parent, pos); 
            agent.Speak(hint.text, hint.voice);
        }

        else
        { 
            Debug.LogWarning("[GhostHintTrigger] Spawned ghost has no GhostHintAgent.");
        }

        if (oneShot) 
        {
            consumed = true;
            gameObject.SetActive(false);
        }
    }
}