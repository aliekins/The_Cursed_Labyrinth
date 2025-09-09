using UnityEngine;
/**
 * @file QuitButton.cs
 * @brief Small helper to quit from scene (used by UI button).
 * @ingroup Utilities
 */
public class QuitButton : MonoBehaviour
{
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}