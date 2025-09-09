using UnityEngine;
using UnityEngine.Playables;
using System.Collections;

/**
 * @file QuitAfterCutscene.cs
 * @brief Quits the application after a PlayableDirector cutscene ends, with (optional) delay and hard timeout.
 * @ingroup Utilities
 */

public class QuitAfterCutscene : MonoBehaviour
{
    [SerializeField] private PlayableDirector director;
    [SerializeField] private float delayAfterEnd = 2f;
    [SerializeField] private float hardTimeout = -1f;

    private void Awake()
    {
        if (!director)
            director = GetComponent<PlayableDirector>();
    }

    private void OnEnable()
    {
        if (director)
        {
            director.stopped += OnDirectorStopped;
            director.played += OnDirectorPlayed;
        }
        if (hardTimeout > 0f)
            StartCoroutine(HardTimeoutQuit(hardTimeout));
    }

    private void OnDisable()
    {
        if (director)
        {
            director.stopped -= OnDirectorStopped;
            director.played -= OnDirectorPlayed;
        }
    }

    private void OnDirectorPlayed(PlayableDirector d)
    {
        StopAllCoroutines();

        if (hardTimeout > 0f)
            StartCoroutine(HardTimeoutQuit(hardTimeout));
    }

    private void OnDirectorStopped(PlayableDirector d)
    {
        StartCoroutine(QuitAfterDelay());
    }

    private IEnumerator QuitAfterDelay()
    {
        yield return new WaitForSecondsRealtime(delayAfterEnd);
        Quit();
    }

    private IEnumerator HardTimeoutQuit(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        Quit();
    }

    private void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}