using UnityEngine;

/**
 * @file PopulatorTypes.cs
 * @brief Per prop tags used by DropAssigner to find eligible holders.
 * @ingroup PropUtils
 */
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