using System.Collections.Generic;
using UnityEngine;

/**
 * @file MusicPlayer.cs
 * @brief Singleton music player with biome based tracks and overlays, crossfading, and volume control.
 * @ingroup Sound
 */

[DefaultExecutionOrder(-1000)]
public sealed class MusicPlayer : MonoBehaviour
{
    #region BiomeAudio
    [System.Serializable]
    public sealed class BiomeAudio
    {
        [Tooltip("Id in SetBiome(id).")]
        public string biomeId;

        [Header("Main track (loops)")]
        public AudioClip mainTrack;
        [Range(0f, 1f)] public float mainVolume = 1f;

        [Header("Overlay loops")]
        public List<AudioClip> overlayLoops = new List<AudioClip>();
        [Range(0f, 1f)] public float overlayVolume = 0.6f;
    }
    #endregion

    #region singleton
    private static MusicPlayer _instance;
    private static bool EnsureInstance()
    {
        if (_instance) return true;

        var go = new GameObject("MusicPlayer");
        _instance = go.AddComponent<MusicPlayer>();

        return _instance != null;
    }
    #endregion

    #region inspector
    [SerializeField] private List<BiomeAudio> biomes = new List<BiomeAudio>();
    [SerializeField, Tooltip("Crossfade time when switching biomes.")]
    private float crossfadeSeconds = 1.0f;
    #endregion

    #region params
    private readonly Dictionary<string, BiomeAudio> _byId = new();
    private string _currentBiome;

    private AudioSource _mainA, _mainB;  // for crossfades
    private bool _usingA;

    private readonly List<AudioSource> _overlaySources = new();
    private float _masterVolume = 1f;
    #endregion

    #region api
    public static void SetBiome(string biomeId, float fadeSeconds = -1f)
    {
        if (!EnsureInstance()) return;

        _instance.SetBiomeInternal(biomeId, fadeSeconds < 0f ? _instance.crossfadeSeconds : fadeSeconds);
    }

    public static void SetVolume(float volume01)
    {
        if (!EnsureInstance()) return;

        _instance.SetVolumeInternal(Mathf.Clamp01(volume01));
    }

    public static void Pause(bool pause)
    {
        if (!EnsureInstance()) return;

        _instance.PauseInternal(pause);
    }
    #endregion

    #region cycle
    private void Awake()
    {
        if (_instance)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        if (transform.parent != null)
            transform.SetParent(null, true);

        DontDestroyOnLoad(gameObject);

        foreach (var b in biomes)
            if (!string.IsNullOrEmpty(b.biomeId) && !_byId.ContainsKey(b.biomeId))
                _byId.Add(b.biomeId, b);

        _mainA = gameObject.AddComponent<AudioSource>();
        _mainB = gameObject.AddComponent<AudioSource>();

        InitSource(_mainA, true);
        InitSource(_mainB, true);

        _usingA = true;
    }

    private static void InitSource(AudioSource s, bool loop)
    {
        s.playOnAwake = false;
        s.loop = loop;
        s.spatialBlend = 0f;
        s.ignoreListenerPause = false;
        s.volume = 0f;
    }
    #endregion

    #region helpers
    private void SetBiomeInternal(string biomeId, float fadeSeconds)
    {
        if (string.IsNullOrEmpty(biomeId) || !_byId.TryGetValue(biomeId, out var cfg))
        {
            Debug.LogWarning($"[MusicPlayer] Unknown biome id '{biomeId}'.");
            return;
        }

        if (_currentBiome == biomeId && GetActiveMain().clip == cfg.mainTrack) return;

        _currentBiome = biomeId;

        StartCoroutine(CrossfadeTo(cfg.mainTrack, Mathf.Clamp01(cfg.mainVolume), Mathf.Max(0f, fadeSeconds)));
        RebuildOverlays(cfg);
    }

    private AudioSource GetActiveMain() => _usingA ? _mainA : _mainB;
    private AudioSource GetIdleMain() => _usingA ? _mainB : _mainA;

    private System.Collections.IEnumerator CrossfadeTo(AudioClip clip, float targetVol, float seconds)
    {
        var from = GetActiveMain();
        var to = GetIdleMain();

        to.clip = clip;
        to.loop = true;
        to.volume = 0f;

        if (clip) to.Play();

        if (seconds > 0.05f)
            StartCoroutine(DuckOverlays(0.65f, seconds * 0.5f));

        float t = 0f;
        float fromStart = from.volume / Mathf.Max(_masterVolume, 0.0001f);

        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;

            float k = seconds <= 0f ? 1f : Mathf.Clamp01(t / seconds);
            float theta = k * 1.57079633f; // pi/2
            float inGain = Mathf.Sin(theta);
            float outGain = Mathf.Cos(theta);

            to.volume = _masterVolume * (inGain * targetVol);
            from.volume = _masterVolume * (outGain * fromStart);

            yield return null;
        }

        if (clip)
        {
            to.volume = _masterVolume * targetVol;
            from.volume = 0f;
            from.Stop();

            _usingA = !_usingA;
        }
        else
        {
            from.volume = 0f; from.Stop();
        }
    }
    private void RebuildOverlays(BiomeAudio cfg)
    {
        // clear previous
        foreach (var s in _overlaySources)
        {
            if (s)
            {
                s.Stop();
                Destroy(s);
            } 
        }

        _overlaySources.Clear();

        if (cfg.overlayLoops == null) return;

        foreach (var c in cfg.overlayLoops)
        {
            if (!c) continue;

            var s = gameObject.AddComponent<AudioSource>();

            InitSource(s, true);

            s.clip = c;
            s.volume = _masterVolume * Mathf.Clamp01(cfg.overlayVolume);
            s.Play();
            _overlaySources.Add(s);
        }
    }

    private void SetVolumeInternal(float v)
    {
        _masterVolume = v;

        if (_mainA.isPlaying) _mainA.volume = Mathf.Min(_mainA.volume, _masterVolume);
        if (_mainB.isPlaying) _mainB.volume = Mathf.Min(_mainB.volume, _masterVolume);

        float layerBase = GetOverlayBaseVolume();

        foreach (var s in _overlaySources)
            if (s) s.volume = layerBase;
    }
    private void PauseInternal(bool pause)
    {
        if (pause)
        {
            _mainA.Pause();
            _mainB.Pause();

            foreach (var s in _overlaySources)
                s.Pause();
        }
        else
        {
            _mainA.UnPause(); 
            _mainB.UnPause();

            foreach (var s in _overlaySources)
                s.UnPause();
        }
    }

    private System.Collections.IEnumerator DuckOverlays(float scale, float halfTime)
    {
        if (_overlaySources.Count == 0) yield break;

        float start = GetOverlayBaseVolume();
        float down = start * Mathf.Clamp01(scale);

        // down
        float t = 0f;
        while (t < halfTime)
        {
            t += Time.unscaledDeltaTime;
            float v = Mathf.Lerp(start, down, t / Mathf.Max(halfTime, 0.001f));

            foreach (var s in _overlaySources)
                if (s)
                    s.volume = v;

            yield return null;
        }
        // up
        t = 0f;
        while (t < halfTime)
        {
            t += Time.unscaledDeltaTime;
            float v = Mathf.Lerp(down, start, t / Mathf.Max(halfTime, 0.001f));

            foreach (var s in _overlaySources) 
                if (s)
                    s.volume = v;

            yield return null;
        }
    }

    private float GetOverlayBaseVolume()
    {
        if (!string.IsNullOrEmpty(_currentBiome) && _byId.TryGetValue(_currentBiome, out var cfg))
            return _masterVolume * Mathf.Clamp01(cfg.overlayVolume);

        return _masterVolume;
    }
    #endregion
}