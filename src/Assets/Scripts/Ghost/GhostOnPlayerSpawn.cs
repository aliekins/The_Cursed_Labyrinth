using System.Collections;
using UnityEngine;

public sealed class GhostOnPlayerSpawn : MonoBehaviour
{
    [SerializeField] private string hintTag = "spawn";
    [SerializeField] private bool perBiome = true;

    [Header("Transition Overlay")]
    [SerializeField] private bool muteDuringOverlay = true;   // <-- NEW: respect the overlay
    [SerializeField] private float postOverlayDelay = 0.15f;  // small delay after overlay clears

    [Header("Polling")]
    [SerializeField] private float pollInterval = 0.2f;

    private int lastBiomeIndex = -1;
    private bool shownThisBiome;

    // overlay edge detection
    private bool wasOverlayActive;
    private float overlayClearedAt;

    void OnEnable() => StartCoroutine(WatchSpawn());

    IEnumerator WatchSpawn()
    {
        while (true)
        {
            int biome = 0;
            var seq = FindAnyObjectByType<BiomeSequenceController>();

            if (seq) biome = seq.currentBiomeIndex;

            if (perBiome && biome != lastBiomeIndex)
            {
                lastBiomeIndex = biome;
                shownThisBiome = false;
            }

            bool overlayActive = false;
            if (muteDuringOverlay)
            {
                overlayActive = BiomeTransitionOverlay.IsActive;

                if (!overlayActive && wasOverlayActive)
                    overlayClearedAt = Time.time;

                wasOverlayActive = overlayActive;
            }

            if (!shownThisBiome)
            {
                if (muteDuringOverlay && (overlayActive || (Time.time - overlayClearedAt) < postOverlayDelay))
                {
                    yield return new WaitForSeconds(pollInterval);
                    continue;
                }

                var player = FindAnyObjectByType<PlayerInventory>();
                if (player)
                {
                    bool ok = GhostHintController.ShowTaggedAtPlayer(hintTag);

                    if (ok) 
                        shownThisBiome = true;
                }
            }

            yield return new WaitForSeconds(pollInterval);
        }
    }
}