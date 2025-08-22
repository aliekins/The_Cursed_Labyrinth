using UnityEngine;
using UnityEngine.SceneManagement;

public class Cutscene_MainStory : MonoBehaviour
{
    private void OnEnable()
    {
        SceneManager.LoadScene("Game", LoadSceneMode.Single);
    }
}
