
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

/// New "eat pulse" from scratch:
/// When an apple is eaten, each body segment scales up then down one-by-one,
/// marching from head to tail at the snake's step cadence.
[DefaultExecutionOrder(350)]
public class SnakeEatPulse : MonoBehaviour
{
    [Header("Look")]
    [Tooltip("How much each segment grows at the peak (e.g., 0.18 = +18%).")]
    [SerializeField] private float pulseAmount = 0.18f;
    [Tooltip("How long each segment takes for a full up-then-down bump.")]
    [SerializeField] private float bumpDuration = 0.12f;

    [Header("Timing fallback (if we can't read SnakeController fields)")]
    [SerializeField] private float fallbackStepStart = 0.14f;
    [SerializeField] private float fallbackStepMin   = 0.08f;
    [SerializeField] private float fallbackStepPerChain = 0.012f;

    // reflection hooks
    private Component snake;
    private FieldInfo fiSegments;               // List<Transform>
    private FieldInfo fiApples;                 // int
    private FieldInfo fiStepStart, fiStepMin, fiStepPerChain;
    private PropertyInfo piCurrentChain;        // int
    private List<Transform> segments;

    // pulse state
    private bool active;
    private int nextIndex;
    private float nextAt; // when to trigger next bump
    private readonly Dictionary<Transform, float> bumpStart = new Dictionary<Transform, float>(); // start times

    private int lastAppleCount = -1;

    void Awake()
    {
        // locate SnakeController by name to avoid hard coupling
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var t = asm.GetTypes().FirstOrDefault(x => x.Name == "SnakeController");
                if (t != null) { snake = GetComponent(t); break; }
            }
            catch { /* some editor assemblies may throw */ }
        }
        if (!snake)
        {
            enabled = false;
            Debug.LogWarning("SnakeEatPulse: SnakeController not found on this GameObject.");
            return;
        }

        var st = snake.GetType();
        fiSegments = st.GetField("segments", BindingFlags.NonPublic | BindingFlags.Instance);
        fiApples   = st.GetField("apples", BindingFlags.NonPublic | BindingFlags.Instance);
        fiStepStart = st.GetField("stepTimeStart", BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
        fiStepMin   = st.GetField("stepTimeMin",   BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
        fiStepPerChain = st.GetField("stepPerChain", BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
        piCurrentChain = st.GetProperty("CurrentChain", BindingFlags.Public|BindingFlags.Instance);

        segments = fiSegments?.GetValue(snake) as List<Transform>;
    }

    void OnDisable()
    {
        // ensure all scales are restored
        if (segments != null)
            foreach (var tr in segments) if (tr) tr.localScale = Vector3.one;
        bumpStart.Clear();
        active = false;
    }

    public void Trigger()
    {
        RefreshSegments();
        if (segments == null || segments.Count == 0) return;

        active = true;
        nextIndex = 0;
        nextAt = Time.time; // first bump immediately
    }

    void Update()
    {
        if (snake == null) return;

        // Watch apples count to auto-trigger
        if (fiApples != null)
        {
            int a = (int)fiApples.GetValue(snake);
            if (lastAppleCount < 0) lastAppleCount = a;
            else if (a > lastAppleCount)
            {
                lastAppleCount = a;
                Trigger();
            }
        }

        if (!active) return;

        // fire bumps at "snake speed"
        float step = GetStepNow();
        if (Time.time >= nextAt)
        {
            if (nextIndex < (segments?.Count ?? 0))
            {
                var tr = segments[nextIndex];
                if (tr) bumpStart[tr] = Time.time;
                nextIndex++;
                nextAt += Mathf.Max(0.04f, step); // step time controls pace
            }
            else
            {
                active = false; // finished
            }
        }

        // animate all "bumped" segments
        // scale = 1 + pulseAmount * sin(pi * (t / bumpDuration)) for 0..dur, then restore
        var keys = new List<Transform>(bumpStart.Keys);
        foreach (var tr in keys)
        {
            float t = Time.time - bumpStart[tr];
            if (t >= bumpDuration || !tr)
            {
                if (tr) tr.localScale = Vector3.one;
                bumpStart.Remove(tr);
                continue;
            }
            float u = Mathf.Clamp01(t / Mathf.Max(0.0001f, bumpDuration));
            float s = 1f + pulseAmount * Mathf.Sin(u * Mathf.PI); // up then down
            tr.localScale = new Vector3(s, s, 1f);
        }
    }

    private void RefreshSegments()
    {
        if (fiSegments == null) return;
        var list = fiSegments.GetValue(snake) as List<Transform>;
        if (list != segments) segments = list;
    }

    private float GetStepNow()
    {
        float start = fallbackStepStart;
        float min   = fallbackStepMin;
        float per   = fallbackStepPerChain;
        try
        {
            if (fiStepStart != null) start = (float)fiStepStart.GetValue(snake);
            if (fiStepMin   != null) min   = (float)fiStepMin.GetValue(snake);
            if (fiStepPerChain != null) per = (float)fiStepPerChain.GetValue(snake);
        } catch {}

        int chain = 1;
        try { if (piCurrentChain != null) chain = (int)piCurrentChain.GetValue(snake); } catch {}

        return Mathf.Max(min, start - (chain - 1) * per);
    }
}
