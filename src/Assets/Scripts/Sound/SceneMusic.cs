using UnityEngine;

/**
* @file SceneMusic.cs
* @brief Sets the music biome when the GameObject is enabled.
* @ingroup Sound
*/

public sealed class SceneMusic : MonoBehaviour
{
    [SerializeField] private string biomeId = "menu";
    [SerializeField] private float fadeSeconds = 3.0f;

    void OnEnable()
    {
        MusicPlayer.SetBiome(biomeId, fadeSeconds);
    }
}