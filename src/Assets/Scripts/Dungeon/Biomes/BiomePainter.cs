using UnityEngine;
using UnityEngine.Tilemaps;
using System.Reflection;

public sealed class BiomePainter : MonoBehaviour
{
    private Tilemap overrideGround;
    private Tilemap overrideCarpet;
    private Tilemap overrideWall;

    public void OverrideTilemaps(Tilemap ground, Tilemap carpet, Tilemap wall)
    {
        overrideGround = ground;
        overrideCarpet = carpet;
        overrideWall = wall;

        var viz = FindAnyObjectByType<TilemapVisualizer>();
        if (!viz) 
        { 
            Debug.LogWarning("[BiomePainter] TilemapVisualizer not found to override.");
            return;
        }

        var t = typeof(TilemapVisualizer);
        var fGround = t.GetField("ground", BindingFlags.NonPublic | BindingFlags.Instance);
        var fCarpet = t.GetField("carpet", BindingFlags.NonPublic | BindingFlags.Instance);
        var fWalls = t.GetField("walls", BindingFlags.NonPublic | BindingFlags.Instance);

        if (fGround != null && ground) fGround.SetValue(viz, ground);
        if (fCarpet != null && carpet) fCarpet.SetValue(viz, carpet);
        if (fWalls != null && wall) fWalls.SetValue(viz, wall);
    }
}