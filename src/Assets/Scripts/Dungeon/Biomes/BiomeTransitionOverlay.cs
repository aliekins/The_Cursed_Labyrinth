using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class BiomeTransitionOverlay : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private CanvasGroup group;
    [SerializeField] private TMP_Text titleText;

    [Header("Timings (seconds)")]
    [SerializeField] private float fadeIn = 0.20f;
    [SerializeField] private float hold = 1.50f;
    [SerializeField] private float fadeOut = 0.25f;

    public Coroutine Play(string title, float? holdOverride = null)
    {
        if (titleText) titleText.text = title;
        if (holdOverride.HasValue) hold = Mathf.Max(0f, holdOverride.Value);
        return StartCoroutine(PlayCo());
    }

    private IEnumerator PlayCo()
    {
        if (!group) group = GetComponent<CanvasGroup>();
        if (group) { group.alpha = 0f; group.blocksRaycasts = true; }

        // Fade in
        float t = 0f;
        while (t < fadeIn)
        {
            t += Time.unscaledDeltaTime;
            if (group) group.alpha = Mathf.Clamp01(t / fadeIn);
            yield return null;
        }

        // Hold
        yield return new WaitForSecondsRealtime(hold);

        // Fade out
        t = 0f;
        while (t < fadeOut)
        {
            t += Time.unscaledDeltaTime;
            if (group) group.alpha = 1f - Mathf.Clamp01(t / fadeOut);
            yield return null;
        }

        Destroy(gameObject);
    }
}