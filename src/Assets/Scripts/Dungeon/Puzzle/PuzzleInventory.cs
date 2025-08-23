using System;
using System.Collections.Generic;
using UnityEngine;

public enum TokenType { Feather, Skull, Gem, Leaf } // use whatever four you like

public class PuzzleInventory : MonoBehaviour
{
    public static PuzzleInventory I { get; private set; }
    public event Action<TokenType, int> Changed;

    private readonly Dictionary<TokenType, int> counts = new();

    void Awake()
    {
        if (I) { Destroy(gameObject); return; }
        I = this; DontDestroyOnLoad(gameObject);
        foreach (TokenType t in Enum.GetValues(typeof(TokenType))) counts[t] = 0;
    }

    public int Count(TokenType t) => counts.TryGetValue(t, out var n) ? n : 0;

    public void Add(TokenType t, int n = 1)
    {
        counts[t] = Count(t) + n;
        Changed?.Invoke(t, counts[t]);
    }

    public bool Consume(TokenType t, int n = 1)
    {
        int have = Count(t);
        if (have < n) return false;
        counts[t] = have - n;
        Changed?.Invoke(t, counts[t]);
        return true;
    }
}