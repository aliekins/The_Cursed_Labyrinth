using UnityEngine;
using UnityEngine.Audio;
using System;
using System.Collections.Generic;

public enum SoundBus { SFX, Music, UI }

[Serializable]
public class SoundDef
{
    public string key;
    public SoundBus bus = SoundBus.SFX;
    public AudioClip[] clips;

    [Range(0f, 1f)] public float volume = 1f;
    [Range(-24f, 24f)] public float pitchJitterSemitones = 0f;

    public bool loop = false;
    public int maxVoices = 8;
    public float cooldown = 0f;

    public AudioClip PickClip() => (clips != null && clips.Length > 0) ? clips[UnityEngine.Random.Range(0, clips.Length)] : null;
}

[CreateAssetMenu(menuName = "Dungeon/Audio/Sound Bank")]
public class SoundBank : ScriptableObject
{
    public List<SoundDef> sounds = new();
    private Dictionary<string, SoundDef> map;

    private void OnEnable() => Build();
    private void OnValidate() => Build();

    private void Build()
    {
        map = new(StringComparer.OrdinalIgnoreCase);
        if (sounds == null) return;
        foreach (var s in sounds)
        {
            if (s != null && !string.IsNullOrWhiteSpace(s.key))
                map[s.key] = s;
        }
    }

    public bool TryGet(string key, out SoundDef def)
    {
        if (map == null)
        {
            def = null;
            return false;
        }
        return map.TryGetValue(key, out def);
    }
}