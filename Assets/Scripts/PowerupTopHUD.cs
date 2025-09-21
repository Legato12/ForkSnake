// Unity 2020.3 LTS compatible.
// PowerupTopHUD v16: use Resources sprites if present; else fall back to Unity built-in circular sprites.
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class PowerupTopHUD : MonoBehaviour
{
    [Header("Sprites (auto-loaded from Resources/PowerupHUD; fallback to built-in UI sprites)")]
    [SerializeField] private string bgPath = "PowerupHUD/ui_status_bg_96";
    [SerializeField] private string progressPath = "PowerupHUD/ui_status_progress_96";
    [SerializeField] private string framePath = "PowerupHUD/ui_status_frame_96";

    [Header("Layout")]
    [SerializeField] private Vector2 anchoredPos = new Vector2(0f, -32f);
    [SerializeField] private float size = 96f;

    private Canvas canvas;
    private Image bg, ring, frame, icon;

    private float endAt = 0f;
    private float duration = 0f;

    private void Awake()
    {
        EnsureCanvas();
        BuildUI();
    }

    private void EnsureCanvas()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            GameObject go = new GameObject("PowerupTopHUD_Canvas");
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

    private Sprite LoadOrBuiltin(string path, string builtin)
    {
        Sprite s = Resources.Load<Sprite>(path);
        if (s != null) return s;
        // Fallback to built-in skin sprite
        try { s = Resources.GetBuiltinResource<Sprite>(builtin); } catch { s = null; }
        return s;
    }

    private void BuildUI()
    {
        Sprite bgSprite = LoadOrBuiltin(bgPath, "UI/Skin/Background.psd");
        Sprite progressSprite = LoadOrBuiltin(progressPath, "UI/Skin/Knob.psd");
        Sprite frameSprite = LoadOrBuiltin(framePath, "UI/Skin/UISprite.psd");

        RectTransform rootRT = gameObject.GetComponent<RectTransform>();
        if (rootRT == null) rootRT = gameObject.AddComponent<RectTransform>();
        rootRT.anchorMin = new Vector2(0.5f, 1f);
        rootRT.anchorMax = new Vector2(0.5f, 1f);
        rootRT.pivot = new Vector2(0.5f, 1f);
        rootRT.anchoredPosition = anchoredPos;
        rootRT.sizeDelta = new Vector2(size, size);

        // bg
        GameObject bgGO = new GameObject("BG");
        bgGO.transform.SetParent(transform, false);
        RectTransform rtBG = bgGO.AddComponent<RectTransform>();
        rtBG.sizeDelta = new Vector2(size, size);
        bg = bgGO.AddComponent<Image>();
        if (bgSprite != null) { bg.sprite = bgSprite; bg.type = Image.Type.Simple; }
        bg.raycastTarget = false;

        // progress ring
        GameObject ringGO = new GameObject("Progress");
        ringGO.transform.SetParent(transform, false);
        RectTransform rtR = ringGO.AddComponent<RectTransform>();
        rtR.sizeDelta = new Vector2(size, size);
        ring = ringGO.AddComponent<Image>();
        if (progressSprite != null) ring.sprite = progressSprite;
        ring.type = Image.Type.Filled;
        ring.fillMethod = Image.FillMethod.Radial360;
        ring.fillOrigin = (int)Image.Origin360.Top;
        ring.fillClockwise = false;
        ring.fillAmount = 0f;
        ring.color = Color.white;
        ring.raycastTarget = false;

        // icon
        GameObject iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(transform, false);
        RectTransform rtI = iconGO.AddComponent<RectTransform>();
        rtI.sizeDelta = new Vector2(size * 0.60f, size * 0.60f);
        icon = iconGO.AddComponent<Image>();
        icon.raycastTarget = false;

        // frame
        GameObject frameGO = new GameObject("Frame");
        frameGO.transform.SetParent(transform, false);
        RectTransform rtF = frameGO.AddComponent<RectTransform>();
        rtF.sizeDelta = new Vector2(size, size);
        frame = frameGO.AddComponent<Image>();
        if (frameSprite != null) frame.sprite = frameSprite;
        frame.raycastTarget = false;

        gameObject.SetActive(false);
    }

    public void Show(Sprite s, float dur)
    {
        icon.sprite = s;
        duration = dur > 0f ? dur : 1f;
        endAt = Time.unscaledTime + duration;
        ring.fillAmount = 1f;
        gameObject.SetActive(true);
        CancelInvoke("HideSelf");
        Invoke("HideSelf", duration);
    }

    private void HideSelf()
    {
        gameObject.SetActive(false);
        ring.fillAmount = 0f;
        endAt = 0f;
    }

    private void Update()
    {
        if (endAt <= 0f || !gameObject.activeSelf) return;
        float remaining = endAt - Time.unscaledTime;
        if (remaining <= 0f)
        {
            ring.fillAmount = 0f;
            endAt = 0f;
        }
        else
        {
            float frac = remaining / duration;
            if (frac < 0f) frac = 0f;
            ring.fillAmount = frac;
        }
    }
}
