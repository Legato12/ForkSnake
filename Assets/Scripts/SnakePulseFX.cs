
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

/// Visual "apple traveling" pulse that moves from head to tail at the snake's step cadence.
/// Includes optional color tint.
[DefaultExecutionOrder(350)]
public class SnakePulseFX : MonoBehaviour
{
    [Header("Look")]
    [SerializeField] private float pulseScale = 1.18f;    // peak scale factor (1.10–1.25 nice)
    [SerializeField] private float pulseWidth = 0.45f;    // width in segments (0.3–0.6)
    [SerializeField] private float tailPopScale = 1.22f;  // tail "birth" pop scale
    [SerializeField] private float tailPopTime  = 0.12f;  // tail pop duration

    [Header("Color Pulse")]
    [SerializeField] private bool tintSegments = true;
    [SerializeField] private Color pulseColor = new Color(1f, 0.92f, 0.2f, 1f); // golden
    [SerializeField] private float colorIntensity = 0.9f;

    [Header("Fallback timing (if reflection fails)")]
    [SerializeField] private float fallbackStepTimeStart = 0.14f;
    [SerializeField] private float fallbackStepTimeMin   = 0.08f;
    [SerializeField] private float fallbackStepPerChain  = 0.012f;

    // reflection targets
    private Component snake;
    private FieldInfo fiSegments, fiStepTimeStart, fiStepTimeMin, fiStepPerChain;
    private PropertyInfo piCurrentChain;
    private List<Transform> segments;
    private List<SpriteRenderer> renderers = new List<SpriteRenderer>();

    // pulse state
    private readonly Queue<float> pulses = new Queue<float>();
    private float currentPos = -1f;
    private float tailPopUntil = -1f;

    void Awake()
    {
        Type snakeType = null;
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try { snakeType = asm.GetTypes().FirstOrDefault(t => t.Name == "SnakeController"); if (snakeType != null) break; }
            catch {}
        }

        if (snakeType != null) snake = GetComponent(snakeType);
        if (snake == null)     snake = GetComponent("SnakeController"); // string fallback

        if (snake == null)
        {
            enabled = false;
            Debug.LogWarning("SnakePulseFX: SnakeController not found on this GameObject.");
            return;
        }

        var tSnake = snake.GetType();
        fiSegments = tSnake.GetField("segments", BindingFlags.NonPublic | BindingFlags.Instance);
        fiStepTimeStart = tSnake.GetField("stepTimeStart", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        fiStepTimeMin   = tSnake.GetField("stepTimeMin",   BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        fiStepPerChain  = tSnake.GetField("stepPerChain",  BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        piCurrentChain  = tSnake.GetProperty("CurrentChain", BindingFlags.Public | BindingFlags.Instance);

        segments = fiSegments?.GetValue(snake) as List<Transform>;
        RefreshRenderers();
    }

    void RefreshRenderers()
    {
        renderers.Clear();
        if (segments != null)
        {
            foreach (var tr in segments)
            {
                if (!tr) continue;
                var sr = tr.GetComponent<SpriteRenderer>();
                if (sr) renderers.Add(sr);
            }
        }
        var headSR = GetComponent<SpriteRenderer>();
        if (headSR && (renderers.Count == 0 || renderers[0] != headSR)) renderers.Insert(0, headSR);
    }

    public void Trigger()
    {
        if (!enabled) return;
        if (currentPos < 0f) currentPos = -0.001f;
        else pulses.Enqueue(-0.001f);
    }

    void Update()
    {
        if (snake == null || fiSegments == null) return;
        var segs = fiSegments.GetValue(snake) as List<Transform>;
        if (segs != null && segs != segments) { segments = segs; RefreshRenderers(); }
        if (segments == null || segments.Count == 0) return;

        if (currentPos >= 0f)
        {
            float stepNow = GetStepNow();
            if (stepNow <= 0.0001f) stepNow = 0.1f;
            currentPos += Time.deltaTime / stepNow;

            int lastIndex = segments.Count - 1;
            if (currentPos >= lastIndex + 0.25f)
            {
                currentPos = -1f;
                tailPopUntil = Time.unscaledTime + tailPopTime;
                if (pulses.Count > 0) currentPos = pulses.Dequeue();
            }
        }

        for (int i = 0; i < segments.Count; i++)
        {
            float s = 1f;
            float k = 0f;

            if (currentPos >= 0f)
            {
                float d = Mathf.Abs(i - currentPos);
                k = Mathf.Clamp01(1f - d / Mathf.Max(0.0001f, pulseWidth));
                k = k * k * (3f - 2f * k);
                s = Mathf.Lerp(1f, pulseScale, k);
            }

            if (Time.unscaledTime < tailPopUntil && i >= segments.Count - 1)
            {
                float u = 1f - (tailPopUntil - Time.unscaledTime) / Mathf.Max(0.0001f, tailPopTime);
                float e = 1f + (tailPopScale - 1f) * Mathf.Sin(u * Mathf.PI);
                s *= e;
            }

            var tr = segments[i];
            if (tr) tr.localScale = new Vector3(s, s, 1f);

            if (tintSegments && i < renderers.Count && renderers[i])
            {
                var sr = renderers[i];
                Color baseCol = Color.white;
                Color target  = pulseColor;
                float a = colorIntensity * k;
                sr.color = Color.Lerp(baseCol, target, a);
            }
        }

        if (!tintSegments)
        {
            // ensure restored to white if user toggled it off while running
            foreach (var sr in renderers) if (sr) sr.color = Color.white;
        }
    }

    private float GetStepNow()
    {
        float start = fallbackStepTimeStart, min = fallbackStepTimeMin, per = fallbackStepPerChain;

        try
        {
            if (fiStepTimeStart != null) start = (float)fiStepTimeStart.GetValue(snake);
            if (fiStepTimeMin   != null) min   = (float)fiStepTimeMin.GetValue(snake);
            if (fiStepPerChain  != null) per   = (float)fiStepPerChain.GetValue(snake);
        } catch {}

        int chain = 1;
        try { if (piCurrentChain != null) chain = (int)piCurrentChain.GetValue(snake); } catch {}

        return Mathf.Max(min, start - (chain - 1) * per);
    }
}
