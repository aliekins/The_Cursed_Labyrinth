using UnityEngine;
using TMPro;
using System.Collections;
/**
 * @file GhostHintAgent.cs
 * @brief Controlls the spawn and destruction of a single ghost hint instance.
 * @ingroup Ghost
 */

public sealed class GhostHintAgent : MonoBehaviour
{
    #region config
    [Header("Wiring")]
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource voice;
    [SerializeField] private TMP_Text bubble;

    [Header("Lifetime")]
    [SerializeField] private float minLifetime = 4.0f;
    [SerializeField] private float extraHoldAfterVoice = 0.25f;
    [SerializeField] private bool autoDestroy = true;

    private static readonly int AppearHash = Animator.StringToHash("appear");
    public event System.Action Completed;
    #endregion

    #region cycle
    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>(true);
        if (!voice) voice = GetComponent<AudioSource>();
        if (!bubble) bubble = GetComponentInChildren<TMP_Text>(true);

        if (bubble)
        {
            var canvas = bubble.GetComponentInParent<Canvas>(true);
            if (canvas)
            {
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = Camera.main;
                canvas.overrideSorting = true;
            }

            var cg = bubble.GetComponentInParent<CanvasGroup>(true);
            if (cg) { cg.alpha = 1f; cg.interactable = false; cg.blocksRaycasts = false; }
        }
    }
    #endregion

    #region api
    public void AppearAt(Transform parent, Vector3 worldPos)
    {
        if (parent)
            transform.SetParent(parent, true);

        transform.position = new Vector3(worldPos.x, worldPos.y, 0f);
        if (transform.localScale == Vector3.zero)
            transform.localScale = Vector3.one;

        gameObject.SetActive(true);

        Debug.Log("[GhostHintAgent] AppearAt " + transform.position);

        if (animator && HasParam(animator, AppearHash))
            animator.SetTrigger(AppearHash);
    }

    public void Speak(string text, AudioClip clip)
    {
        if (bubble)
        {
            bubble.gameObject.SetActive(true);
            bubble.text = text ?? string.Empty;

            var c = bubble.color; c.a = 1f; bubble.color = c;
        }
        else
        {
            Debug.LogWarning("[GhostHintAgent] No TMP_Text bubble found on ghost prefab.");
        }

        if (clip)
        {
            if (!voice) voice = gameObject.AddComponent<AudioSource>();
            voice.spatialBlend = 0f;
            voice.playOnAwake = false;
            voice.clip = clip;
            voice.Play();
        }

        Debug.Log("[GhostHintAgent] Speak: " + (text ?? "(empty)"));

        if (autoDestroy)
            StartCoroutine(WaitUntilTypewriterAndVoiceThenDestroy());
    }
    #endregion

    #region helpers
    private IEnumerator WaitUntilTypewriterAndVoiceThenDestroy()
    {
        float hardMin = Time.time + minLifetime;

        var tw = bubble ? bubble.GetComponent<TypeWriterEffect>() : null;

        if (tw && bubble)
        {
            while (true)
            {
                bubble.ForceMeshUpdate();
                int total = bubble.textInfo.characterCount;
                if (total <= 0) { yield return null; continue; }

                if (bubble.maxVisibleCharacters >= total - 1)
                    break;

                yield return null;
            }
        }

        if (voice && voice.clip)
        {
            while (voice.isPlaying)
                yield return null;

            if (extraHoldAfterVoice > 0f)
                yield return new WaitForSeconds(extraHoldAfterVoice);
        }

        while (Time.time < hardMin)
            yield return null;

        Completed?.Invoke();

        Destroy(gameObject);
    }

    private static bool HasParam(Animator a, int nameHash)
    {
        if (!a) return false;
        var ps = a.parameters;

        for (int i = 0; i < ps.Length; i++)
            if (ps[i].nameHash == nameHash)
                return true;

        return false;
    }
    #endregion
}