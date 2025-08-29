using UnityEngine;

/// Tags set on spawned props so the drop-assigner can find eligible holders.
public sealed class PropDropTags : MonoBehaviour
{
    public bool potion;
    public bool sword;
    public bool book;

    // biome 3
    public bool skull;
    public bool heart;
    public bool crown;
}
