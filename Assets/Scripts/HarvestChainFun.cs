using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class HarvestChainFun : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SnakeController snake;
    [SerializeField] private ScreenBuzz2D shaker;
    [SerializeField] private TextMeshProUGUI chainText;   // shows "Chain xN  0.00"
    [SerializeField] private TextMeshProUGUI popupText;   // centered "MAX SPEED!"
    [SerializeField] private Image flashOverlay;          // gold flash over gameplay viewport

    [Header("Tuning")]
    [SerializeField] private int maxLevel = 10;
    [SerializeField] private float buzzBase = 0.35f;
    [SerializeField] private float buzzDuration = 0.16f;
    [SerializeField] private Color chainNormal = Color.white;
    [SerializeField] private Color chainFlash = new Color(1f, 0.85f, 0.1f, 1f);  // gold
    [SerializeField] private string maxSpeedMessage = "MAX SPEED!";
    [SerializeField] private Color popupColor = new Color(1f, 0.92f, 0.2f, 1f);

    private bool guaranteedGoldPending = false;
    private bool maxShownThisStreak = false;

    private Coroutine chainPopCo, flashCo, popupCo;

    public void WireIfNull()
    {
        if (!snake)  snake  = FindObjectOfType<SnakeController>();
        if (!shaker && Camera.main)
        {
            shaker = Camera.main.GetComponent<ScreenBuzz2D>();
            if (!shaker) shaker = Camera.main.gameObject.AddComponent<ScreenBuzz2D>();
        }
    }

    private void Awake()
    {
        WireIfNull();
        if (popupText) { popupText.gameObject.SetActive(false); popupText.alpha = 0f; }
        if (chainText)
        {
            chainText.text = "Chain x1  0.00";
            chainText.color = chainNormal;
            chainText.rectTransform.localScale = Vector3.one;
            chainText.gameObject.SetActive(false); // hidden until x2
        }
        if (flashOverlay)
        {
            var c = flashOverlay.color;
            flashOverlay.color = new Color(c.r, c.g, c.b, 0f);
            flashOverlay.raycastTarget = false;
        }
    }

    private void Update()
    {
        if (!snake || !chainText) return;
        int lvl = Mathf.Clamp(snake.CurrentChain, 1, maxLevel);
        if (lvl < 2) { chainText.gameObject.SetActive(false); return; }

        float left = snake.ChainTimeLeft();
        chainText.gameObject.SetActive(true);
        chainText.text = $"Chain x{lvl}  {left:0.00}";
    }

    // Called by SnakeController on every apple
    public void OnAppleEaten(bool wasGold, int chainLevel)
    {
        WireIfNull();
        int lvl = Mathf.Clamp(chainLevel, 1, maxLevel);

        // pop + flash + shake only if visible (>=2)
        if (lvl >= 2 && chainText)
        {
            float t = (lvl - 1) / Mathf.Max(1f, (maxLevel - 1)); // 0..1
            if (chainPopCo != null) StopCoroutine(chainPopCo);
            chainPopCo = StartCoroutine(PopChain(t));

            if (flashOverlay)
            {
                if (flashCo != null) StopCoroutine(flashCo);
                flashCo = StartCoroutine(FlashOverlay(Mathf.Lerp(0.10f, 0.40f, t)));
            }

            shaker?.Buzz(Mathf.Lerp(0.15f, 1f, t) * buzzBase,
                         Mathf.Lerp(buzzDuration * 0.8f, buzzDuration * 1.3f, t));
        }

        // MAX event once per streak
        if (lvl >= maxLevel && !maxShownThisStreak)
        {
            maxShownThisStreak = true;
            guaranteedGoldPending = true;

            if (popupCo != null) StopCoroutine(popupCo);
            popupCo = StartCoroutine(ShowMaxPopup());

            shaker?.Buzz(1f, 0.35f);
            if (flashOverlay)
            {
                if (flashCo != null) StopCoroutine(flashCo);
                flashCo = StartCoroutine(FlashOverlay(0.55f));
            }
        }
    }

    public void OnChainReset()
    {
        maxShownThisStreak = false;
        if (chainText)
        {
            chainText.text = "Chain x1  0.00";
            chainText.color = chainNormal;
            chainText.rectTransform.localScale = Vector3.one;
            chainText.gameObject.SetActive(false);
        }
        if (popupText) { popupText.gameObject.SetActive(false); popupText.alpha = 0f; }
    }

    public bool ConsumeGuaranteedGold()
    {
        bool v = guaranteedGoldPending;
        guaranteedGoldPending = false;
        return v;
    }

    private IEnumerator PopChain(float t)
    {
        var rt = chainText.rectTransform;
        chainText.color = chainFlash;

        float s0 = 1f;
        float s1 = 1f + Mathf.Lerp(0.10f, 0.26f, t);
        float up = 0.08f, down = 0.12f;

        float x = 0f;
        while (x < up) { x += Time.unscaledDeltaTime; rt.localScale = Vector3.one * Mathf.Lerp(s0, s1, x / up); yield return null; }
        x = 0f;
        while (x < down){ x += Time.unscaledDeltaTime; rt.localScale = Vector3.one * Mathf.Lerp(s1, 1f, x / down); yield return null; }

        chainText.color = Color.Lerp(chainFlash, chainNormal, 0.6f);
    }

    private IEnumerator ShowMaxPopup()
    {
        if (!popupText) yield break;
        popupText.gameObject.SetActive(true);
        popupText.text = maxSpeedMessage;
        popupText.color = popupColor;
        var rt = popupText.rectTransform;

        float total = 1.1f;
        int pulses = 4;
        float amp = 0.32f;

        float t = 0f;
        while (t < total)
        {
            t += Time.unscaledDeltaTime;
            float phase = (t / total) * pulses * Mathf.PI;
            float s = 1f + Mathf.Abs(Mathf.Sin(phase)) * amp;
            rt.localScale = Vector3.one * s;

            float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.18f)) *
                      Mathf.SmoothStep(1f, 0f, Mathf.Clamp01((t - 0.75f) / (total - 0.75f)));
            popupText.alpha = a;
            yield return null;
        }
        popupText.gameObject.SetActive(false);
        popupText.alpha = 0f;
    }

    private IEnumerator FlashOverlay(float intensity)
    {
        var col = flashOverlay.color;
        float a1 = Mathf.Clamp01(intensity);
        float t = 0f, dur = 0.24f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float u = t / dur;
            float a = Mathf.Lerp(a1, 0f, u * u);
            flashOverlay.color = new Color(col.r, col.g, col.b, a);
            yield return null;
        }
        flashOverlay.color = new Color(col.r, col.g, col.b, 0f);
    }
}