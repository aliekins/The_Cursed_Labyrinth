/// \file PlayerHealth.cs
/// \brief Player HP model with change and death events and damage/heal.
/// \ingroup PlayerHP
using UnityEngine;
using System;

[DisallowMultipleComponent]
public sealed class PlayerHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHP = 100;
    [SerializeField] private int startHP = 100;

    public int Max => maxHP;
    public int Current { get; private set; }

    public event Action<int, int> Changed; // (current, max)
    public event Action Died;

    private void Awake()
    {
        Current = Mathf.Clamp(startHP, 0, maxHP);
        Changed?.Invoke(Current, maxHP);
    }

    public void Damage(int amount)
    {
        if (amount <= 0 || Current <= 0) return;
        Current = Mathf.Max(0, Current - amount);
        Debug.Log($"[HP] Damage({amount}) -> {Current}/{maxHP}", this);
        Changed?.Invoke(Current, maxHP);

        if (Current == 0) Died?.Invoke();
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || Current <= 0) return;

        Current = Mathf.Min(maxHP, Current + amount);
        Debug.Log($"[HP] Heal({amount}) -> {Current}/{maxHP}", this);
        Changed?.Invoke(Current, maxHP);
    }

    public void ResetToFull()
    {
        Current = Max;
        Changed?.Invoke(Current, Max);
    }
}