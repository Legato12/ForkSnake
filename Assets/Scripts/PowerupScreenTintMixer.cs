// Unity 2020.3 LTS compatible.
// Mixes multiple active tints and produces a single overlay color (alpha-blended).
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class PowerupScreenTintMixer : MonoBehaviour
{
    private Canvas canvas;
    private Image overlay;

    private class Entry
    {
        public string id;
        public Color color;
        public float endAt;
        public float duration;
    }

    private readonly List<Entry> entries = new List<Entry>();

    private void Awake()
    {
        EnsureCanvas();
        EnsureOverlay();
    }

    private void EnsureCanvas()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            GameObject go = new GameObject("PowerupScreenTint_Canvas");
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
            transform.SetParent(go.transform, false);
        }
        if (EventSystem.current == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
            es.hideFlags = HideFlags.DontSave;
        }
    }

    private void EnsureOverlay()
    {
        RectTransform rt = gameObject.GetComponent<RectTransform>();
        if (rt == null) rt = gameObject.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        overlay = gameObject.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0f);
        overlay.raycastTarget = false;
        gameObject.SetActive(true);
    }

    public void Show(string id, Color c, float duration)
    {
        if (duration <= 0f) duration = 1f;
        Entry e = Find(id);
        if (e == null)
        {
            e = new Entry();
            e.id = id;
            entries.Add(e);
        }
        e.color = c;
        e.duration = duration;
        e.endAt = Time.unscaledTime + duration;
        UpdateOverlay();
    }

    private Entry Find(string id)
    {
        for (int i = 0; i < entries.Count; i++) if (entries[i].id == id) return entries[i];
        return null;
    }

    private void Update()
    {
        bool dirty = false;
        float now = Time.unscaledTime;
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            if (entries[i].endAt <= now) { entries.RemoveAt(i); dirty = true; }
        }
        if (dirty) UpdateOverlay();
    }

    private void UpdateOverlay()
    {
        if (entries.Count == 0) { overlay.color = new Color(0,0,0,0); return; }

        // Combine alphas: a = 1 - Π(1 - ai)
        // Combine colors as weighted average by ai
        float invProd = 1f;
        float r = 0f, g = 0f, b = 0f, aSum = 0f;
        for (int i = 0; i < entries.Count; i++)
        {
            Color c = entries[i].color;
            float a = Mathf.Clamp01(c.a);
            invProd *= (1f - a);
            r += c.r * a;
            g += c.g * a;
            b += c.b * a;
            aSum += a;
        }
        float outA = 1f - invProd;
        if (aSum > 0f)
        {
            r /= aSum; g /= aSum; b /= aSum;
        }
        overlay.color = new Color(r, g, b, outA);
    }
}
