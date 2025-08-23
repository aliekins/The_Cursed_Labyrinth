using System;
using UnityEngine;

public class QuestState : MonoBehaviour
{
    public static QuestState Instance { get; private set; }
    public static bool Has => Instance != null;

    public event Action<int> SwordsChanged;
    public int Swords { get; private set; }
    void Awake() { if (Instance) Destroy(gameObject); else { Instance = this; DontDestroyOnLoad(gameObject); } }

    public void AddSword() { Swords++; SwordsChanged?.Invoke(Swords); }
}