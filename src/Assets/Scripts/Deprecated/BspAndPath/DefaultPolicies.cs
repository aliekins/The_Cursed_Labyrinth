///// \file DefaultPolicies.cs
///// \brief Default implementations for split and room placement.
//using System;
//using UnityEngine;

///// <summary>
///// Split policy: 
///// prefers splitting along the longer axis; ties - broken randomly
///// </summary>
//public sealed class AspectBiasedSplitPolicy : IBspSplitPolicy
//{
//    public bool TrySplit(RectInt area, int minLeafSize, System.Random rng, out RectInt a, out RectInt b)
//    {
//        a = default; b = default;

//        bool canSplitHorizontally = area.width >= minLeafSize * 2;
//        bool canSplitVertically = area.height >= minLeafSize * 2;

//        if (!canSplitHorizontally && !canSplitVertically) return false;

//        bool splitHorizontally;
//        if (canSplitHorizontally && !canSplitVertically) splitHorizontally = true;
//        else if (!canSplitHorizontally && canSplitVertically) splitHorizontally = false;
//        else
//        {
//            if (area.width > area.height) splitHorizontally = true;
//            else if (area.width < area.height) splitHorizontally = false;
//            else splitHorizontally = rng.NextDouble() < 0.5;
//        }

//        if (splitHorizontally)
//        {
//            int cut = rng.Next(minLeafSize, area.width - minLeafSize + 1);
//            a = new RectInt(area.x, area.y, cut, area.height);
//            b = new RectInt(area.x + cut, area.y, area.width - cut, area.height);
//        }
//        else
//        {
//            int cut = rng.Next(minLeafSize, area.height - minLeafSize + 1);
//            a = new RectInt(area.x, area.y, area.width, cut);
//            b = new RectInt(area.x, area.y + cut, area.width, area.height - cut);
//        }
//        return true;
//    }
//}

///// <summary>
///// Room placement policy: 
///// picks a random size in range and a random offset so it stays within the leaf
///// </summary>
//public sealed class RandomRoomCarver : IRoomCarver
//{
//    public RectInt? CreateRoom(RectInt leafArea, int minRoomSize, int maxRoomSize, System.Random rng)
//    {
//        int rw = Mathf.Clamp(rng.Next(minRoomSize, maxRoomSize + 1), minRoomSize, leafArea.width);
//        int rh = Mathf.Clamp(rng.Next(minRoomSize, maxRoomSize + 1), minRoomSize, leafArea.height);

//        // If the leaf is smaller than min room size on either axis - skip
//        if (leafArea.width < minRoomSize || leafArea.height < minRoomSize)
//            return null;

//        int rx = leafArea.x + rng.Next(0, Math.Max(1, leafArea.width - rw + 1));
//        int ry = leafArea.y + rng.Next(0, Math.Max(1, leafArea.height - rh + 1));

//        return new RectInt(rx, ry, rw, rh);
//    }
//}

//public sealed class PaddedRoomCarver : IRoomCarver
//{
//    private readonly int pad;
//    public PaddedRoomCarver(int padding) { pad = Mathf.Max(0, padding); }

//    public RectInt? CreateRoom(RectInt leafArea, int minRoomSize, int maxRoomSize, System.Random rng)
//    {
//        // Shrink the leaf on all sides
//        var inner = new RectInt(leafArea.x + pad, leafArea.y + pad, leafArea.width - 2 * pad, leafArea.height - 2 * pad);

//        if (inner.width < minRoomSize || inner.height < minRoomSize) return null;

//        int rw = Mathf.Clamp(rng.Next(minRoomSize, maxRoomSize + 1), minRoomSize, inner.width);
//        int rh = Mathf.Clamp(rng.Next(minRoomSize, maxRoomSize + 1), minRoomSize, inner.height);

//        int rx = inner.x + rng.Next(0, inner.width - rw + 1);
//        int ry = inner.y + rng.Next(0, inner.height - rh + 1);

//        return new RectInt(rx, ry, rw, rh);
//    }
//}
