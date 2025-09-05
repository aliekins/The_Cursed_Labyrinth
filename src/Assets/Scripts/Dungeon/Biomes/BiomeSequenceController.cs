using UnityEngine;
using UnityEngine.Tilemaps;
using System;
using System.Collections;
using System.Collections.Generic;
/**
 * @file BiomeSequenceController.cs
 * @brief Drives progression through biomes and rebuilds the dungeon each step.
 * @ingroup Biomes
 */

/**
 * @class BiomeSequenceController
 * @brief Listens for "special room solved" and builds the next biome; keeps simple run snapshots.
 */
public class BiomeSequenceController : MonoBehaviour
{
    #region Config
    /**
     * @struct BiomeEntry
     * @brief A single step in the sequence: biome profile + its special-room prefab.
     */
    [Serializable]
    public class BiomeEntry
    {
        public BiomeSetup_SO biomeProfile;
        public GameObject specialRoomPrefab;     // must contain its own Grid and Tilemaps (Ground/Carpet/Wall)
    }

    [Header("Sequence")]
    public List<BiomeEntry> biomes = new List<BiomeEntry>();

    [Header("References")]
    public DungeonController dungeonController;     
    public SpecialRoomSeeder specialRoomSeeder;     // seeds prefab and exposes its Tilemaps and occupied cells
    public SpawnSelector spawnSelector;             // chooses spawn farthest from special
    public TilemapClearer tilemapClearer;          // clears all tilemaps & props between biomes

    [Header("Runtime")]
    public int currentBiomeIndex = -1;

    private SpecialRoomCompleteBroadcaster _activeBroadcaster;
    private readonly Dictionary<int, PlayerInventory.Snapshot> startSnapshots = new();
    private bool restartRequested = false;
    /// @brief Rooms of the current build
    public List<Room> CurrentRooms { get; private set; }
    #endregion

    #region lifecycle
    private void Awake()
    {
        if (dungeonController == null) dungeonController = FindAnyObjectByType<DungeonController>();
        if (specialRoomSeeder == null) specialRoomSeeder = FindAnyObjectByType<SpecialRoomSeeder>();
        if (spawnSelector == null) spawnSelector = FindAnyObjectByType<SpawnSelector>();
        if (tilemapClearer == null) tilemapClearer = FindAnyObjectByType<TilemapClearer>();
        if (dungeonController) dungeonController.PlayerSpawned += OnPlayerSpawned;
    }


    private void Start()
    {
        StartFirstBiome();
    }
    #endregion

    #region flow
    /**
     * @brief Begin the sequence from biome 0.
     *
     * Resets @ref currentBiomeIndex then advances to the first biome.
     */
    public void StartFirstBiome()
    {
        if (biomes.Count == 0)
        {
            Debug.LogError("[BiomeSequence] No biomes configured.");
            return;
        }
        startSnapshots.Clear();
        currentBiomeIndex = -1;
        NextBiome();
    }

    public void TryRestartCurrentBiome()
    {
        if (currentBiomeIndex < 0 || currentBiomeIndex >= biomes.Count)
        {
            Debug.LogError("[BiomeSequence] No current biome to restart.");
            return;
        }

        restartRequested = true;
        currentBiomeIndex--;
        NextBiome();
    }

    /// @brief Kick off building the next biome.
    public void NextBiome()
    {
        Debug.Log("[BiomeSequence] Generating next biome...");
        StartCoroutine(NextBiome_Co());
    }

    /**
     * @brief Coroutine that clears old content, seeds special room, and rebuilds the dungeon.
     *
     * Captures a lightweight inventory snapshot on entry (unless restarting), then
     * builds the biome and subscribes to its special-room completion.
     */
    private IEnumerator NextBiome_Co()
    {
        tilemapClearer?.ClearAll();
        yield return null;

        int nextIndex = currentBiomeIndex + 1;

        currentBiomeIndex = nextIndex;
        if (currentBiomeIndex >= biomes.Count)
        {
            Debug.Log("[BiomeSequence] All biomes complete.");
            yield break;
        }

        var entry = biomes[currentBiomeIndex];
        if (entry == null || !entry.biomeProfile || !entry.specialRoomPrefab)
        {
            Debug.LogError("[BiomeSequence] Invalid biome entry.");
            yield break;
        }

        var seedInfo = specialRoomSeeder.Seed(entry.specialRoomPrefab);
        if (seedInfo == null)
        {
            Debug.LogError("[BiomeSequence] Failed to seed special room.");
            yield break;
        }

        dungeonController.Build(entry.biomeProfile, seedInfo);
        Debug.Log("[BiomeSequence] Biome built: " + entry.biomeProfile.name);

        if (!restartRequested)
        {
            var inv = FindFirstObjectByType<PlayerInventory>(FindObjectsInactive.Include);
            if (inv != null)
            {
                startSnapshots[currentBiomeIndex] = inv.GetSnapshot();
                Debug.Log("[BiomeSequence] Captured baseline for biome " + currentBiomeIndex);
            }
        }

        if (_activeBroadcaster != null)
            _activeBroadcaster.OnSolved -= HandleSpecialSolved;
        _activeBroadcaster = seedInfo.broadcaster;
        if (_activeBroadcaster != null)
            _activeBroadcaster.OnSolved += HandleSpecialSolved;
    }

    /**
     * @brief React to special room completion.
     *
     * If not final biome, builds the next one. On the final biome, keeps the world
     * so the portal can appear and the player can exit.
     */
    private void HandleSpecialSolved()
    {
        if (currentBiomeIndex < biomes.Count - 1)
        {
            Debug.Log("[BiomeSequence] Special solved, generate next biome");
            NextBiome();
            return;
        }

        Debug.Log("[BiomeSequence] Final biome solved — waiting for portal/exit.");
    }
    private void OnPlayerSpawned(PlayerInventory inv)
    {
        if (!inv) return;

        if (restartRequested)
        {
            if (startSnapshots.TryGetValue(currentBiomeIndex, out var snap))
            {
                Debug.Log("[BiomeSequence] Restoring inventory snapshot for biome " + currentBiomeIndex);

                inv.ApplySnapshot(snap);
                StartCoroutine(ApplySnapshotEndOfFrame(inv, snap)); // defeats late auto-pickups
            }
            restartRequested = false;
            return;
        }
    }

    private System.Collections.IEnumerator ApplySnapshotEndOfFrame(PlayerInventory inv, PlayerInventory.Snapshot snap)
    {
        yield return null;
        inv.ApplySnapshot(snap);
    }

    private void OnDestroy()
    {
        if (dungeonController)
            dungeonController.PlayerSpawned -= OnPlayerSpawned;
    }
    #endregion
}