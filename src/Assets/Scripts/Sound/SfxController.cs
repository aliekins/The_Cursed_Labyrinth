using UnityEngine;

public sealed class SfxController : MonoBehaviour
{
    private static SfxController _instance;
    private AudioSource _src;

    // Call from anywhere: Sfx2DController.Play(clip, 0.8f);
    public static void Play(AudioClip clip, float volume = 1f, bool ignoreListenerPause = false)
    {
        if (!clip) return;

        if (_instance == null)
        {
            var go = new GameObject("SfxController");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<SfxController>();
            _instance._src = go.AddComponent<AudioSource>();
            _instance._src.spatialBlend = 0f;
            _instance._src.playOnAwake = false;
        }

        _instance._src.ignoreListenerPause = ignoreListenerPause;
        _instance._src.PlayOneShot(clip, Mathf.Clamp01(volume));
    }
}
