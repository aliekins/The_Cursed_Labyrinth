using UnityEngine;
using UnityEngine.SceneManagement;
/**
 * @file SceneLoader.cs
 * @brief Small helper to load a specific scene (used by UI buttons).
 * @ingroup Utilities
 */
public class SceneLoader : MonoBehaviour
{
    public void LoadScene()
    {
        SceneManager.LoadScene("Cutscene_MainStory");
    }
}
