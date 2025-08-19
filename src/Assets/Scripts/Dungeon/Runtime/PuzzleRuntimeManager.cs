///// \file PuzzleRuntimeManager.cs
///// \brief Spawns puzzle prefabs the first time the player enters a room.
//using System.Collections.Generic;
//using UnityEngine;

//public sealed class PuzzleRuntimeManager : MonoBehaviour
//{
//    [System.Serializable] public struct PrefabMap { public PuzzleType type; public GameObject prefab; }

//    [SerializeField] private DungeonController controller;
//    [SerializeField] private List<PrefabMap> prefabs = new();

//    private readonly Dictionary<PuzzleType, GameObject> map = new();
//    private readonly HashSet<int> spawnedRooms = new();

//    private void Awake()
//    {
//        foreach (var p in prefabs)
//        {
//            if (p.prefab)
//            {
//                map[p.type] = p.prefab;
//            }
//        }
//    }
//    private void OnEnable()
//    {
//        if (controller != null)
//            controller.RoomEntered += OnRoomEntered;
//    }
//    private void OnDisable()
//    {
//        if (controller != null)
//            controller.RoomEntered -= OnRoomEntered;
//    }

//    private void OnRoomEntered(int roomId)
//    {
//        if (roomId < 0 || spawnedRooms.Contains(roomId)) return;

//        if (!controller.TryGetPlans(roomId, out var plans) || plans == null) return;

//        foreach (var plan in plans)
//        {
//            if (!map.TryGetValue(plan.type, out var prefab) || prefab == null) continue;

//            var parent = controller.GridTransform;
//            var pos = controller.CellCenterLocal(plan.cell);
//            var go = Instantiate(prefab, parent);
//            go.transform.localPosition = pos;

//            // If the prefab has a runtime driver, pass context in:
//            var driver = go.GetComponent<IPuzzleManager>();
//            driver?.Init(plan, controller);

//            Debug.Log($"[IPuzzleManager] Spawned {plan.type} in room {roomId} at {plan.cell} (biome {plan.biomeKind})");
//        }

//        spawnedRooms.Add(roomId);
//    }
//}