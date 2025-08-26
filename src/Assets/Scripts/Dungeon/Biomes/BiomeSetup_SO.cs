using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Dungeon/Biome Setup", fileName = "BiomeSetup")]
public class BiomeSetup_SO : ScriptableObject
{
    [Header("Map Overrides (optional)")]
    public bool overrideSize;
    public int width = 64;
    public int height = 48;

    [Header("Floor & Corridors")]
    [Tooltip("Kind used when carving rooms in this biome (e.g., floor_entry, floor_quarry, floor_grove)")]
    public string floorKind = "floor_entry";
    [Tooltip("Kind used for corridors in this biome (usually floor_corridor)")]
    public string corridorKind = "floor_corridor";

    [Header("Start/Special selection")]
    [Tooltip("Min path distance (in room graph cells) between start and special room")]
    public int minPathCells = 100;

    //[Header("Special Room")]
    //public GameObject specialRoomPrefab;
    //public SpecialPort specialEntrance = SpecialPort.Bottom;   // where corridor should enter the special prefab

    //[Header("Props / Traps")]
    //public ScriptableObject propRules;
    //public bool keepSpikeTraps = true;

}