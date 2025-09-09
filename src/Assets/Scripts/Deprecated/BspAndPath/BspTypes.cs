///// \file BspTypes.cs
///// \brief Common BSP types: config, tree, and results.
//using System.Collections.Generic;
//using UnityEngine;
//#nullable enable

///// <summary>Configuration for BSP generation</summary>
//public sealed class BspConfig
//{
//    /// <summary>Map area to partition</summary>
//    public RectInt MapArea;
//    /// <summary>Minimum leaf size to stop splitting</summary>
//    public int MinLeafSize = 12;
//    /// <summary>Minimum carved room dimension</summary>
//    public int MinRoomSize = 4;
//    /// <summary>Maximum carved room dimension</summary>
//    public int MaxRoomSize = 10;
//    /// <summary>(Optional) cap to avoid loops - for now, no cap</summary>
//    public int MaxNodes = 0;
//}

///// <summary>BSP node</summary>
//public sealed class BspNode
//{
//    /// <summary>Area covered by this node</summary>
//    public RectInt Area;
//    /// <summary>Left child (first partition)</summary>
//    public BspNode? Left;
//    /// <summary>Right child (second partition)</summary>
//    public BspNode? Right;
//    /// <summary>Carved room inside this leaf; null for non-leaf or uncarved</summary>
//    public RectInt? Room;

//    /// <summary>Whether this node is a leaf</summary>
//    public bool IsLeaf => Left == null && Right == null;

//    public BspNode(RectInt area) { Area = area; }
//}

///// <summary>Result bundle of BSP generation</summary>
//public sealed class BspResult
//{
//    /// <summary>Root of the BSP tree</summary>
//    public BspNode? Root;
//    /// <summary>All leaves (debug)</summary>
//    public List<BspNode> Leaves = new List<BspNode>();
//    /// <summary>All carved rooms (ordered by creation)</summary>
//    public List<Room> Rooms = new List<Room>();
//}