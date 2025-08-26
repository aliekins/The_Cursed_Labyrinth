///// \file IBspSplitPolicy.cs
///// \brief Interface for deciding how a BSP node is split.
//using UnityEngine;

///// <summary>
///// Defines how a BSP node (rect) should be split into two rects or remain a leaf
///// </summary>
//public interface IBspSplitPolicy
//{
//    /// <summary>
//    /// Try to split an <paramref name="area"/> into <paramref name="a"/> and <paramref name="b"/>
//    /// Return <c>false</c> to indicate a leaf
//    /// </summary>
//    /// <param name="area">Current node area</param>
//    /// <param name="minLeafSize">Minimum size per side to allow a splt</param>
//    /// <param name="rng">Random</param>
//    /// <param name="a">First sub-rect if split succeeds</param>
//    /// <param name="b">Second sub-rect if split succeeds</param>
//    /// <returns><c>true</c> if split produced two valid sub-rects; otherwise <c>false</c></returns>
//    bool TrySplit(RectInt area, int minLeafSize, System.Random rng, out RectInt a, out RectInt b);
//}