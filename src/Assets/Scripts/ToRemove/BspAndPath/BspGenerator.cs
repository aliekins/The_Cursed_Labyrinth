///// \file BspGenerator.cs
///// \brief BSP tree builder that uses policies for splitting and room carving.
//using System;
//using System.Collections.Generic;
//using UnityEngine;

//public static class BspGenerator
//{
//    /// <param name="config">Map/size configuration</param>
//    /// <param name="splitPolicy">Strategy for splitting nodes</param>
//    /// <param name="roomCarver">Strategy for placing rooms in leaves</param>
//    /// <param name="rng">Random</param>
//    /// <returns>Result bundle containing the tree, leaves, and carved rooms</returns>
//    public static BspResult Generate(BspConfig config, IBspSplitPolicy splitPolicy, IRoomCarver roomCarver, System.Random rng)
//    {
//        if (splitPolicy == null) throw new ArgumentNullException(nameof(splitPolicy));
//        if (roomCarver == null) throw new ArgumentNullException(nameof(roomCarver));

//        var result = new BspResult { Root = new BspNode(config.MapArea) };
//        var queue = new Queue<BspNode>();
//        queue.Enqueue(result.Root);

//        int nodeCount = 1;
//        while (queue.Count > 0)
//        {
//            var node = queue.Dequeue();

//            // cap safety
//            if (config.MaxNodes > 0 && nodeCount >= config.MaxNodes)
//            {
//                // force leaf
//                result.Leaves.Add(node);
//                continue;
//            }

//            // Try split according to policy
//            if (splitPolicy.TrySplit(node.Area, config.MinLeafSize, rng, out var a, out var b))
//            {
//                node.Left = new BspNode(a);
//                node.Right = new BspNode(b);
//                queue.Enqueue(node.Left);
//                queue.Enqueue(node.Right);
//                nodeCount += 2;
//            }
//            else
//            {
//                // Leaf
//                result.Leaves.Add(node);
//            }
//        }

//        // Carve rooms in leaves
//        int id = 0;
//        foreach (var leaf in result.Leaves)
//        {
//            var room = roomCarver.CreateRoom(leaf.Area, config.MinRoomSize, config.MaxRoomSize, rng);
//            if (room.HasValue)
//            {
//                leaf.Room = room.Value;
//                result.Rooms.Add(new Room(id++, room.Value));
//            }
//        }

//        return result;
//    }

//    /// <summary>
//    /// Wrapper that returns just rooms
//    /// </summary>
//    public static List<Room> GenerateRooms(RectInt mapArea, int minLeafSize, int minRoomSize, int maxRoomSize, System.Random rng)
//    {
//        var cfg = new BspConfig
//        {
//            MapArea = mapArea,
//            MinLeafSize = minLeafSize,
//            MinRoomSize = minRoomSize,
//            MaxRoomSize = maxRoomSize
//        };
//        var result = Generate(cfg, new AspectBiasedSplitPolicy(), new RandomRoomCarver(), rng);
//        return result.Rooms;
//    }
//}