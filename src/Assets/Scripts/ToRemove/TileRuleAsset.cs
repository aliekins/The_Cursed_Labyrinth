///// \file TileRuleAsset.cs
///// \brief Rule definition.
//using System;
//using UnityEngine;

//public enum SpritePickMode { First, Random, Hash }
//public enum ColliderKind { None, Box, Capsule, Circle }

//[Serializable]
//public sealed class ColliderParams
//{
//    public ColliderKind type = ColliderKind.None;
//    public Vector2 size = Vector2.one;
//    public bool isTrigger = false;
//}

///// <summary>Defines how a tile kind should look and collide</summary>
//[CreateAssetMenu(fileName = "TileRule", menuName = "Dungeon/Tile Rule", order = 10)]
//public sealed class TileRuleAsset : ScriptableObject
//{
//    public Sprite[] sprites = Array.Empty<Sprite>();
//    public SpritePickMode pick = SpritePickMode.First;
//    public Vector2 offset = Vector2.zero;
//    public float rotation = 0f;
//    public float scale = 1f;
//    public float z = 0f;
//    public ColliderParams collider = new ColliderParams();
//}