using UnityEngine;
using TMPro;
using System.Collections;

/**
 * @file GhostHintAgent.cs
 * @brief In-world ghost that appears at a cell, plays an animation, speaks a line, then despawns.
 * @ingroup Ghost
 */
[DisallowMultipleComponent]
public sealed class GhostHintAgent : MonoBehaviour
{
    #region config
    [Header("Optional refs")]
    [SerializeField] private Animator animator;
    [SerializeField] private TMP_Text bubble;
    [SerializeField] private AudioSource voice;

    [Header("Timing")]
    [SerializeField, Min(0.05f)] private float secondsPerChar = 0.05f;
    [SerializeField, Min(1f)] private float minHoldSeconds = 2.5f;
    [SerializeField, Min(1f)] private float maxHoldSeconds = 6f;
    [SerializeField, Min(0f)] private float extraHoldSeconds = 0.0f;

    private Coroutine lifeCo;
    #endregion

    #region cycle
    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>(true);
        if (!voice) voice = GetComponent<AudioSource>();
    }

    public void AppearAt(Transform parent, Vector3 worldPos)
    {
        if (parent) 
            transform.SetParent(parent, worldPositionStays: true);

        transform.position = worldPos;
    }

    public void Speak(string line, AudioClip clip = null)
    {
        if (bubble)
            bubble.text = line ?? "";

        if (clip)
        {
            if (!voice)
                voice = gameObject.AddComponent<AudioSource>();
            voice.clip = clip;
            voice.Play();
        }

        float hold = Mathf.Clamp((line?.Length ?? 0) * secondsPerChar + extraHoldSeconds, minHoldSeconds, maxHoldSeconds);

        if (lifeCo != null) 
            StopCoroutine(lifeCo);

        lifeCo = StartCoroutine(Life_Co(hold));
    }
    public void Despawn(float delay = 0.75f)
    {
        if (lifeCo != null)
        {
            StopCoroutine(lifeCo);
            lifeCo = null; 
        }

        Destroy(gameObject, delay);
    }
    #endregion

    #region helpers
    private IEnumerator Life_Co(float hold)
    {
        yield return new WaitForSeconds(hold);
        Despawn();
    }
    #endregion
}