using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class ChainDotsUI : MonoBehaviour
{
    [Header("Layout")]
    public int maxDots = 10;
    public float dotSize = 22f;
    public float spacing = 8f;
    public float timerHeight = 6f;
    public Vector2 anchoredOffset = new Vector2(0f, -18f);

    [Header("Style")]
    public Color inactiveColor = new Color(1f, 1f, 1f, 0.25f);
    public Color activeColor   = new Color(1f, 0.85f, 0.1f, 1f);
    public Sprite dotSprite; // null -> built-in round sprite
    public bool useFilledTimer = true;

    private RectTransform rt;
    private readonly List<Image> dots = new List<Image>();
    private Image timerBar;
    private int lastShown = 0;

    void OnEnable(){ Build(); }
#if UNITY_EDITOR
    void OnValidate(){ if (!Application.isPlaying) Build(); }
#endif

    void Build()
    {
        rt = GetComponent<RectTransform>();
        if (!rt) return;

        // anchor top-center
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.anchoredPosition = anchoredOffset;

        // clear children (editor-safe)
        var toRemove = new List<GameObject>();
        foreach (Transform c in transform) toRemove.Add(c.gameObject);
        foreach (var go in toRemove) { if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
        dots.Clear();
        timerBar = null;

        float totalWidth = maxDots * dotSize + (maxDots - 1) * spacing;
        rt.sizeDelta = new Vector2(totalWidth, dotSize + 8f + timerHeight);

        // create dots
        for (int i = 0; i < maxDots; i++)
        {
            var go = new GameObject("Dot" + (i+1), typeof(RectTransform));
            var dtr = go.GetComponent<RectTransform>();
            dtr.SetParent(rt, false);
            dtr.sizeDelta = new Vector2(dotSize, dotSize);
            dtr.anchorMin = dtr.anchorMax = new Vector2(0f, 1f);
            float x = i * (dotSize + spacing);
            dtr.anchoredPosition = new Vector2(x - totalWidth * 0.5f + dotSize * 0.5f, -2f);

            var img = go.AddComponent<Image>();
            img.color = inactiveColor;
            img.sprite = dotSprite;
            img.type = Image.Type.Simple;
            img.raycastTarget = false;
            dots.Add(img);
        }

        // timer bar under dots
        var tgo = new GameObject("TimerBar", typeof(RectTransform));
        var tr = tgo.GetComponent<RectTransform>();
        tr.SetParent(rt, false);
        tr.anchorMin = new Vector2(0.5f, 0f);
        tr.anchorMax = new Vector2(0.5f, 0f);
        tr.pivot     = new Vector2(0.5f, 0f);
        tr.sizeDelta = new Vector2(totalWidth, timerHeight);
        tr.anchoredPosition = new Vector2(0f, 0f);

        timerBar = tgo.AddComponent<Image>();
        timerBar.color = new Color(activeColor.r, activeColor.g, activeColor.b, 0.9f);
        timerBar.raycastTarget = false;
        if (useFilledTimer)
        {
            timerBar.type = Image.Type.Filled;
            timerBar.fillMethod = Image.FillMethod.Horizontal;
            timerBar.fillOrigin = (int)Image.OriginHorizontal.Left;
            timerBar.fillAmount = 0f;
        }
    }

    public void UpdateDisplay(int chain, float timeLeft, float timeWindow)
    {
        if (dots.Count == 0) return;
        int c = Mathf.Clamp(chain, 0, maxDots);
        for (int i = 0; i < dots.Count; i++)
        {
            dots[i].color = i < c ? activeColor : inactiveColor;
        }

        if (timerBar)
        {
            float f = (timeWindow <= 0.0001f) ? 0f : Mathf.Clamp01(timeLeft / timeWindow);
            if (useFilledTimer) timerBar.fillAmount = f;
            else timerBar.rectTransform.sizeDelta = new Vector2(rt.sizeDelta.x * f, timerBar.rectTransform.sizeDelta.y);
        }

        if (c > lastShown) PopDot(c - 1, Mathf.InverseLerp(1, maxDots, c));
        lastShown = c;
    }

    public void ResetUI()
    {
        lastShown = 0;
        UpdateDisplay(0, 0f, 1f);
        transform.localScale = Vector3.one;
    }

    public void PopDot(int index, float t01)
    {
        if (index < 0 || index >= dots.Count) return;
        var img = dots[index];
        var tr = img.rectTransform;
        if (Application.isPlaying) StartCoroutine(PopCo(tr, t01));
        else tr.localScale = Vector3.one;
    }

    IEnumerator PopCo(RectTransform tr, float t01)
    {
        float s0 = 1f;
        float s1 = 1f + Mathf.Lerp(0.08f, 0.22f, t01);
        float up = 0.08f, down = 0.12f;
        float x = 0f;
        while (x < up) { x += Time.unscaledDeltaTime; tr.localScale = Vector3.one * Mathf.Lerp(s0, s1, x / up); yield return null; }
        x = 0f;
        while (x < down){ x += Time.unscaledDeltaTime; tr.localScale = Vector3.one * Mathf.Lerp(s1, 1f, x / down); yield return null; }
        tr.localScale = Vector3.one;
    }
}