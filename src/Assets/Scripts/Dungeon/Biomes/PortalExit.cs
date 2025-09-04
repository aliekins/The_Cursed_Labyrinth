using UnityEngine;
using UnityEngine.SceneManagement;

/**
 * @file PortalExit.cs
 * @brief Simple exit portal that loads a scene when the player enters.
 * @ingroup Biomes
 */

/**
 * @class PortalExit
 * @brief Loads the configured ending scene after (an optional) delay.
 */

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class PortalExit : MonoBehaviour
{
    #region Config
    [SerializeField] private string endingSceneName = "EndingCutscene";
    [SerializeField, Min(0f)] private float loadDelay = 2f;

    private bool armed = true;
    #endregion

    #region Lifecycle
    private void Reset()
    {
        var c = GetComponent<Collider2D>();

        if (c) c.isTrigger = true;
    }

    public void SetScene(string sceneName) => endingSceneName = sceneName;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!armed) return;

        if (other.GetComponent<PlayerInventory>())
        {
            armed = false;

            Debug.Log("[PortalExit] Player entered portal, loading scene: " + endingSceneName);
            Invoke(nameof(LoadEnding), loadDelay);
        }
    }

    private void LoadEnding()
    {
        if (!string.IsNullOrEmpty(endingSceneName))
            SceneManager.LoadScene(endingSceneName, LoadSceneMode.Single);
        else
            Debug.LogError("[PortalExit] endingSceneName is empty.");
    }
    #endregion
}