using UnityEngine;
using UnityEngine.SceneManagement;
/**
 * @file Cutscene_MainStory.cs
 * @brief Simple bootstrap cutscene that immediately loads the "Game" scene.
 * @ingroup Utilities
 */
public class Cutscene_MainStory : MonoBehaviour
{
    private void OnEnable()
    {
        SceneManager.LoadScene("Game", LoadSceneMode.Single);
    }
}