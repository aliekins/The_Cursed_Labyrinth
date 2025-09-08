using System.Collections;
using UnityEngine;

public sealed class GhostOnPlayerSpawn : MonoBehaviour
{
    [SerializeField] private string hintTag = "spawn";
    [SerializeField] private bool perBiome = true;

    private int lastBiomeIndex = -1;
    private bool shownThisBiome;

    void OnEnable() => StartCoroutine(WatchSpawn());

    IEnumerator WatchSpawn()
    {
        while (true)
        {
            int biome = 0;
            var seq = FindAnyObjectByType<BiomeSequenceController>();

            if (seq)
                biome = seq.currentBiomeIndex;

            if (perBiome && biome != lastBiomeIndex)
            {
                lastBiomeIndex = biome;
                shownThisBiome = false;
            }

            if (!shownThisBiome)
            {
                var player = FindAnyObjectByType<PlayerInventory>();

                if (player)
                {
                    Debug.Log($"[GhostOnPlayerSpawn] Showing hint '{hintTag}' at player spawn in biome {biome}.");
                    GhostHintController.ShowTaggedAtPlayer(hintTag);
                    shownThisBiome = true;
                }
            }
            yield return new WaitForSeconds(0.2f);
        }
    }
}