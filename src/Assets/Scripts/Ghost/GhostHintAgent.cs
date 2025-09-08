using TMPro;
using UnityEngine;

public sealed class GhostHintAgent : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource voice;
    [SerializeField] private TMP_Text bubble;

    static readonly int AppearHash = Animator.StringToHash("appear");

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
        }
    }

    public void AppearAt(Transform parent, Vector3 worldPos)
    {
        if (parent) transform.SetParent(parent, true);

        transform.position = new Vector3(worldPos.x, worldPos.y, 0f);

        gameObject.SetActive(true);
        if (transform.localScale == Vector3.zero)
            transform.localScale = Vector3.one;

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
            if (!voice) 
                voice = gameObject.AddComponent<AudioSource>();

            voice.spatialBlend = 0f; 
            voice.playOnAwake = false;
            voice.clip = clip; voice.Play();
        }

        Debug.Log("[GhostHintAgent] Speak: " + (text ?? "(empty)"));
    }

    static bool HasParam(Animator a, int nameHash)
    {
        if (!a) return false;

        var ps = a.parameters;

        for (int i = 0; i < ps.Length; i++)
            if (ps[i].nameHash == nameHash) return true;

        return false;
    }
}