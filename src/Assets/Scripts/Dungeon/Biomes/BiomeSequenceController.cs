using UnityEngine;
using UnityEngine.Tilemaps;
using System;
using System.Collections;
using System.Collections.Generic;

/// \brief Listens for "special room solved" and then regenerates the dungeon with the next biome
public class BiomeSequenceController : MonoBehaviour
{
    [Serializable]
    public class BiomeEntry
    {
        public BiomeSetup_SO biomeProfile;
        public GameObject specialRoomPrefab;     // must contain its own Grid and Tilemaps (Ground/Carpet/Wall)
        //public Vector2Int mapSizeOverride = Vector2Int.zero; // optional
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

    private void Awake()
    {
        if (dungeonController == null) dungeonController = FindAnyObjectByType<DungeonController>();
        if (specialRoomSeeder == null) specialRoomSeeder = FindAnyObjectByType<SpecialRoomSeeder>();
        if (spawnSelector == null) spawnSelector = FindAnyObjectByType<SpawnSelector>();
        if (tilemapClearer == null) tilemapClearer = FindAnyObjectByType<TilemapClearer>();
    }

    private void Start()
    {
        StartFirstBiome();
    }

    public void StartFirstBiome()
    {
        if (biomes.Count == 0)
        {
            Debug.LogError("[BiomeSequence] No biomes configured.");
            return;
        }
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
        restartRequested = true;          // signal we want to restore snapshot after rebuild
        currentBiomeIndex--;              // NextBiome() will ++ it back to this biome
        NextBiome();
    }
    public void NextBiome()
    {
        Debug.Log("[BiomeSequence] Generating next biome...");
        StartCoroutine(NextBiome_Co());
    }

    private IEnumerator NextBiome_Co()
    {
        // Clear previous tiles/props
        tilemapClearer?.ClearAll();
        yield return null;

        // Decide which biome we’re entering
        int nextIndex = currentBiomeIndex + 1;

        // Capture snapshot for the biome we’re about to ENTER, but only if we’re not restarting it
        if (!restartRequested)
        {
            var inv = FindFirstObjectByType<PlayerInventory>(FindObjectsInactive.Include);
            if (inv != null)
                startSnapshots[nextIndex] = inv.GetSnapshot();
        }

        currentBiomeIndex = nextIndex;
        if (currentBiomeIndex >= biomes.Count) { Debug.Log("[BiomeSequence] All biomes complete."); yield break; }

        var entry = biomes[currentBiomeIndex];
        if (entry == null || !entry.biomeProfile || !entry.specialRoomPrefab)
        { Debug.LogError("[BiomeSequence] Invalid biome entry."); yield break; }

        var seedInfo = specialRoomSeeder.Seed(entry.specialRoomPrefab);
        if (seedInfo == null)
        { Debug.LogError("[BiomeSequence] Failed to seed special room."); yield break; }

        dungeonController.Build(entry.biomeProfile, seedInfo);
        Debug.Log("[BiomeSequence] Biome built: " + entry.biomeProfile.name);

        // re-hook special broadcaster as you already do...
        if (_activeBroadcaster != null)
            _activeBroadcaster.OnSolved -= HandleSpecialSolved;
        _activeBroadcaster = seedInfo.broadcaster;
        if (_activeBroadcaster != null)
            _activeBroadcaster.OnSolved += HandleSpecialSolved;
    }

    //private void HandlePlayerSpawned(PlayerInventory inv)
    //{
    //    if (inv == null) return;

    //    if (restartRequested)
    //    {
    //        if (startSnapshots.TryGetValue(currentBiomeIndex, out var snap))
    //            inv.ApplySnapshot(snap);
    //        restartRequested = false;
    //    }
    //}

    private void HandleSpecialSolved()
    {
        Debug.Log("[BiomeSequence] Special solved, generate next biome");
        NextBiome();
    }
}