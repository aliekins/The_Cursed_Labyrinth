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

    public void NextBiome()
    {
        StartCoroutine(NextBiome_Co());
    }

    private IEnumerator NextBiome_Co()
    {
        // Clear previous biome content 
        if (tilemapClearer != null)
            tilemapClearer.ClearAll();

        // Slight delay to ensure all systems released their references
        yield return null;

        currentBiomeIndex++;
        if (currentBiomeIndex >= biomes.Count)
        {
            Debug.Log("[BiomeSequence] All biomes complete.");
            yield break;
        }

        var entry = biomes[currentBiomeIndex];
        if (entry == null || entry.biomeProfile == null || entry.specialRoomPrefab == null)
        {
            Debug.LogError("[BiomeSequence] Invalid biome entry.");
            yield break;
        }

        // Seed the special room prefab instance
        var seedInfo = specialRoomSeeder.Seed(entry.specialRoomPrefab);
        if (seedInfo == null)
        {
            Debug.LogError("[BiomeSequence] Failed to seed special room.");
            yield break;
        }

        // Ask DungeonController to build the dungeon, expanding from the prefab's tilemaps
        dungeonController.Build(entry.biomeProfile, seedInfo);

        // Hook to special-room completion to advance the sequence
        if (_activeBroadcaster != null)
            _activeBroadcaster.OnSolved -= HandleSpecialSolved;

        _activeBroadcaster = seedInfo.broadcaster;
        if (_activeBroadcaster != null)
            _activeBroadcaster.OnSolved += HandleSpecialSolved;
    }

    private void HandleSpecialSolved()
    {
        Debug.Log("[BiomeSequence] Special solved, generate next biome");
        NextBiome();
    }
}