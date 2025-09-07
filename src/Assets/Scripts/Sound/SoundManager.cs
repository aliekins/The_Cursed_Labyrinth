using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    public static SoundManager I { get; private set; }
    private void Awake()
    {
        if (I && I != this) 
        {
            Destroy(gameObject); 
            return; 
        }

        I = this;

        DontDestroyOnLoad(gameObject);
        WarmPool();
        LoadVolumes();
    }

    #region config
    [Header("Bank and Mixer")]
    [SerializeField] private SoundBank bank;
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private AudioMixerGroup masterGroup;
    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private AudioMixerGroup uiGroup;

    [Header("Pool")]
    [SerializeField, Min(1)] private int poolSize = 24;

    [Header("Music")]
    [SerializeField] private float musicFadeSeconds = 0.75f;

    private readonly Queue<AudioSource> pool = new();
    private readonly HashSet<AudioSource> inUse = new();

    private readonly Dictionary<string, int> voices = new();
    private readonly Dictionary<string, float> lastPlay = new();

    private AudioSource musicA, musicB;
    private bool musicOnA = true;
    #endregion

    #region pooling
    void WarmPool()
    {
        for (int i = 0; i < poolSize; i++)
            pool.Enqueue(MakePooledSource(sfxGroup));

        musicA = MakePooledSource(musicGroup);
        musicA.loop = true;

        musicB = MakePooledSource(musicGroup);
        musicB.loop = true;
    }

    AudioSource MakePooledSource(AudioMixerGroup grp)
    {
        var go = new GameObject("AudioSource2D (pooled)");
        go.transform.SetParent(transform, false);

        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = 0f;
        src.outputAudioMixerGroup = grp ? grp : masterGroup;

        go.SetActive(false);
        return src;
    }

    AudioSource Rent(AudioMixerGroup grp)
    {
        var src = pool.Count > 0 ? pool.Dequeue() : MakePooledSource(grp);
        src.outputAudioMixerGroup = grp ? grp : masterGroup;
        src.gameObject.SetActive(true);

        inUse.Add(src);

        return src;
    }

    void Return(AudioSource src, string keyUsed = null)
    {
        if (!src) return;

        src.Stop();
        src.clip = null;
        src.transform.SetParent(transform, false);
        src.gameObject.SetActive(false);

        inUse.Remove(src);
        pool.Enqueue(src);

        if (!string.IsNullOrEmpty(keyUsed) && voices.TryGetValue(keyUsed, out var v) && v > 0)
            voices[keyUsed] = v - 1;
    }
    #endregion

    #region API
    public static AudioSource Play(string key) => I?.PlayInternal(key, SoundBus.SFX);
    public static AudioSource PlayUI(string key) => I?.PlayInternal(key, SoundBus.UI);
    public static void Stop(AudioSource s)
    {
        if (s)
            I?.Return(s);
    }
    public static void StopAll()
    {
        if (!I) return;

        foreach (var s in I.inUse) 
            if (s) 
                s.Stop();

        // second pass to avoid modifying during iteration
        var tmp = new List<AudioSource>(I.inUse);
        foreach (var s in tmp) 
            I.Return(s);
    }

    public static void PlayMusic(string key, float? fade = null) => I?.PlayMusicInternal(key, fade ?? I.musicFadeSeconds);
    public static void StopMusic(float? fade = null) => I?.PlayMusicInternal(null, fade ?? I.musicFadeSeconds);

    public static void SetMaster(float v) => I?.SetBus("MasterVol", v);
    public static void SetMusic(float v) => I?.SetBus("MusicVol", v);
    public static void SetSFX(float v) => I?.SetBus("SFXVol", v);
    public static void SetUI(float v) => I?.SetBus("UIVol", v);
    #endregion

    #region helpers
    AudioSource PlayInternal(string key, SoundBus forcedBus)
    {
        if (!bank || !bank.TryGet(key, out var def))
        {
            Debug.LogWarning($"[Audio2D] No sound '{key}' in bank.");
            return null;
        }

        float now = Time.unscaledTime;
        if (def.cooldown > 0f && lastPlay.TryGetValue(key, out var t0) && now - t0 < def.cooldown)
            return null;
        lastPlay[key] = now;

        int v = voices.TryGetValue(key, out var cur) ? cur : 0;
        if (v >= Mathf.Max(1, def.maxVoices))
            return null;
        voices[key] = v + 1;

        var clip = def.PickClip();
        if (!clip)
        {
            voices[key]--;
            return null;
        }

        var bus = forcedBus != SoundBus.SFX ? forcedBus : def.bus;
        var grp = bus switch
        {
            SoundBus.Music => musicGroup,
            SoundBus.UI => uiGroup,
            _ => sfxGroup
        };

        var src = Rent(grp);
        src.clip = clip;
        src.loop = def.loop;
        src.volume = def.volume;
        src.pitch = SemitoneToPitch(Random.Range(-def.pitchJitterSemitones, def.pitchJitterSemitones));
        src.transform.localPosition = Vector3.zero;

        src.Play();
        if (!src.loop) 
            StartCoroutine(ReturnWhenDone(src, key));

        return src;
    }

    System.Collections.IEnumerator ReturnWhenDone(AudioSource src, string key)
    {
        while (src && src.isPlaying)
            yield return null;

        Return(src, key);
    }

    void PlayMusicInternal(string key, float fade)
    {
        var cur = musicOnA ? musicA : musicB;
        var alt = musicOnA ? musicB : musicA;

        if (string.IsNullOrEmpty(key))
        {
            if (cur && cur.isPlaying)
                StartCoroutine(FadeOut(cur, fade));
            return;
        }

        if (!bank || !bank.TryGet(key, out var def))
        { 
            Debug.LogWarning($"[Music2D] No sound '{key}'.");
            return; 
        }

        var clip = def.PickClip();
        if (!clip) return;

        alt.clip = clip;
        alt.loop = true;
        alt.volume = 0f;
        alt.Play();

        StartCoroutine(Crossfade(cur, alt, fade));
        musicOnA = !musicOnA;
    }

    System.Collections.IEnumerator FadeOut(AudioSource src, float t)
    {
        if (!src)
            yield break;

        float v0 = src.volume;
        float e = 0f;
        while (e < t) { e += Time.unscaledDeltaTime; src.volume = Mathf.Lerp(v0, 0f, e / t); yield return null; }
        src.Stop(); src.volume = v0;
    }

    System.Collections.IEnumerator Crossfade(AudioSource from, AudioSource to, float t)
    {
        float a0 = from ? from.volume : 0f;
        float e = 0f;

        while (e < t)
        {
            e += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(e / t);

            if (from) 
                from.volume = Mathf.Lerp(a0, 0f, k);
            if (to)
                to.volume = Mathf.Lerp(0f, 1f, k);

            yield return null;
        }

        if (from) 
        { 
            from.Stop();
            from.volume = a0;
        }

        if (to) 
            to.volume = 1f;
    }

    void SetBus(string exposedParam, float linear01)
    {
        if (!mixer) return;

        float db = Mathf.Log10(Mathf.Clamp(linear01, 0.0001f, 1f)) * 20f;
        mixer.SetFloat(exposedParam, db);

        PlayerPrefs.SetFloat($"vol_{exposedParam}", linear01);
    }

    void LoadVolumes()
    {
        if (!mixer) return;
        foreach (var p in new[] { "MasterVol", "MusicVol", "SFXVol", "UIVol" })
        {
            float v = PlayerPrefs.GetFloat($"vol_{p}", 1f);
            SetBus(p, v);
        }
    }

    static float SemitoneToPitch(float semis) => Mathf.Pow(2f, semis / 12f);
    #endregion
}