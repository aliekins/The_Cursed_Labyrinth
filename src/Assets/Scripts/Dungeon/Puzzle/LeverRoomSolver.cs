using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public sealed class LeverRoomSolver : MonoBehaviour, ISpecialSolver
{
    public event Action OnSolved;

    [Header("Levers (any order; colors decide)")]
    public LeverListener[] levers;

    [Header("Rules")]
    [Tooltip("Seconds allowed from the first correct pull to finish the sequence.")]
    public float timeLimitSeconds = 20f;
    [Tooltip("If true, levers pop back up on fail.")]
    public bool autoResetOnFail = true;

    [Header("UI (optional)")]
    public TMP_Text countdownText; // optional
    public bool IsSolved => solved;

    // Required order by book colors
    private readonly LeverListener.BookColor[] order =
    {
        LeverListener.BookColor.Blue,
        LeverListener.BookColor.Green,
        LeverListener.BookColor.Red,
        LeverListener.BookColor.Orange,
        LeverListener.BookColor.Purple
    };

    private Dictionary<LeverListener.BookColor, LeverListener> byColor;
    private int progressIndex;
    private float timeRemaining;
    private bool timerRunning;
    private bool solved;

    private void Awake()
    {
        byColor = new Dictionary<LeverListener.BookColor, LeverListener>(levers?.Length ?? 0);

        if (levers != null)
        {
            foreach (var lv in levers)
            {
                if (!lv) continue;
                if (byColor.ContainsKey(lv.color))
                    Debug.LogWarning($"[LeverRoomSolver] Duplicate color {lv.color} on {lv.name}");
                byColor[lv.color] = lv;
                lv.PulledDown += OnLeverPulled;
            }
        }

        ResetState(hard: true);
        UpdateUI();
    }

    private void OnDestroy()
    {
        if (levers != null)
            foreach (var lv in levers)
                if (lv) lv.PulledDown -= OnLeverPulled;
    }

    private void Update()
    {
        if (solved || !timerRunning) return;

        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0f)
        {
            FailReset("Timer expired");
        }
        UpdateUI();
    }

    private void OnLeverPulled(LeverListener lever)
    {
        if (solved) return;

        // Start timer on the first correct pull
        if (!timerRunning && progressIndex == 0)
        {
            timerRunning = true;
            timeRemaining = Mathf.Max(0.5f, timeLimitSeconds);
        }

        var expected = order[Mathf.Clamp(progressIndex, 0, order.Length - 1)];
        if (lever.color != expected)
        {
            FailReset($"Wrong lever. Expected {expected}, got {lever.color}");
            return;
        }

        progressIndex++;

        if (progressIndex >= order.Length)
        {
            solved = true;
            timerRunning = false;
            UpdateUI();

            // Lock all levers to prevent further changes
            foreach (var lv in levers) if (lv) lv.LockInteraction(true);

            Debug.Log("[LeverRoomSolver] SOLVED");
            OnSolved?.Invoke();
        }
    }

    private void FailReset(string reason)
    {
        Debug.Log($"[LeverRoomSolver] Reset: {reason}");
        ResetState(hard: autoResetOnFail);
        UpdateUI();
    }

    private void ResetState(bool hard)
    {
        solved = false;
        timerRunning = false;
        progressIndex = 0;
        timeRemaining = timeLimitSeconds;

        if (hard && levers != null)
            foreach (var lv in levers) if (lv) lv.ResetUp();
    }

    private void UpdateUI()
    {
        if (!countdownText) return;

        if (solved) { countdownText.text = "Solved!"; }
        else if (!timerRunning && progressIndex == 0)
            countdownText.text = $"Order: B  G  R  O  P\nTime: {timeLimitSeconds:0}s";
        else
            countdownText.text = $"{Mathf.CeilToInt(Mathf.Max(0f, timeRemaining))}s";
    }
}