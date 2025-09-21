// Unity 2020.3 LTS compatible.
// Multi-effect top HUD: several circular indicators with per-effect radial timers.
// Uses sprites from Resources/PowerupHUD if present; otherwise falls back to built-in UI sprites.
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

[DisallowMultipleComponent]
public sealed class PowerupTopHUDMulti : MonoBehaviour
{
    [Header("Sprites (auto-loaded from Resources/PowerupHUD; fallback to built-in UI sprites)")]
    [SerializeField] private string bgPath = "PowerupHUD/ui_status_bg_96";
    [SerializeField] private string progressPath = "PowerupHUD/ui_status_progress_96";
    [SerializeField] private string framePath = "PowerupHUD/ui_status_frame_96";

    [Header("Layout")]
    [SerializeField] private Vector2 anchoredPos = new Vector2(0f, -32f);
    [SerializeField] private float size = 96f;
    [SerializeField] private float spacing = 8f;
    [SerializeField] private TextAnchor alignment = TextAnchor.UpperCenter;
    [SerializeField] private Color progressColor = new Color(0.25f, 0.85f, 1f, 1f);

    private Canvas canvas;
    private HorizontalLayoutGroup hgroup;
    private ContentSizeFitter fitter;

    private Sprite bgSprite;
    private Sprite progressSprite;
    private Sprite frameSprite;

    private class Item
    {
        public string id;
        public GameObject root;
        public Image ring;
        public Image icon;
        public float endAt;
        public float duration;
    }

    private readonly List<Item> items = new List<Item>();

    private void Awake()
    {
        EnsureCanvasLayout();
        LoadSprites();
        gameObject.SetActive(true);
    }

    private void EnsureCanvasLayout()
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

        RectTransform rootRT = gameObject.GetComponent<RectTransform>();
        if (rootRT == null) rootRT = gameObject.AddComponent<RectTransform>();
        rootRT.anchorMin = new Vector2(0.5f, 1f);
        rootRT.anchorMax = new Vector2(0.5f, 1f);
        rootRT.pivot = new Vector2(0.5f, 1f);
        rootRT.anchoredPosition = anchoredPos;

        hgroup = GetComponent<HorizontalLayoutGroup>();
        if (hgroup == null) hgroup = gameObject.AddComponent<HorizontalLayoutGroup>();
        hgroup.childAlignment = alignment;
        hgroup.spacing = spacing;
        hgroup.childControlWidth = false;
        hgroup.childControlHeight = false;
        hgroup.childForceExpandWidth = false;
        hgroup.childForceExpandHeight = false;

        fitter = GetComponent<ContentSizeFitter>();
        if (fitter == null) fitter = gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private Sprite LoadOrBuiltin(string path, string builtin)
    {
        Sprite s = Resources.Load<Sprite>(path);
        if (s != null) return s;
        try { s = Resources.GetBuiltinResource<Sprite>(builtin); } catch { s = null; }
        return s;
    }

    private void LoadSprites()
    {
        bgSprite = LoadOrBuiltin(bgPath, "UI/Skin/Background.psd");
        progressSprite = LoadOrBuiltin(progressPath, "UI/Skin/Knob.psd");
        frameSprite = LoadOrBuiltin(framePath, "UI/Skin/UISprite.psd");
    }

    // Public API: show or refresh an effect indicator by id
    public void Show(Sprite iconSprite, float duration, string id)
    {
        if (duration <= 0f) duration = 1f;
        Item it = FindItem(id);
        if (it == null)
        {
            it = CreateItem(id);
            items.Add(it);
        }
        it.icon.sprite = iconSprite;
        it.duration = duration;
        it.endAt = Time.unscaledTime + duration;
        it.ring.fillAmount = 1f;
        it.root.SetActive(true);
    }

    private Item FindItem(string id)
    {
        for (int i = 0; i < items.Count; i++) if (items[i].id == id) return items[i];
        return null;
    }

    private Item CreateItem(string id)
    {
        GameObject root = new GameObject("Item_" + id);
        root.transform.SetParent(transform, false);
        RectTransform r = root.AddComponent<RectTransform>();
        r.sizeDelta = new Vector2(size, size);

        // bg
        GameObject bgGO = new GameObject("BG");
        bgGO.transform.SetParent(root.transform, false);
        RectTransform rtBG = bgGO.AddComponent<RectTransform>();
        rtBG.sizeDelta = new Vector2(size, size);
        Image bg = bgGO.AddComponent<Image>(); if (bgSprite != null) bg.sprite = bgSprite;
        bg.raycastTarget = false;

        // progress ring
        GameObject ringGO = new GameObject("Progress");
        ringGO.transform.SetParent(root.transform, false);
        RectTransform rtR = ringGO.AddComponent<RectTransform>();
        rtR.sizeDelta = new Vector2(size, size);
        Image ring = ringGO.AddComponent<Image>(); if (progressSprite != null) ring.sprite = progressSprite;
        ring.type = Image.Type.Filled;
        ring.fillMethod = Image.FillMethod.Radial360;
        ring.fillOrigin = (int)Image.Origin360.Top;
        ring.fillClockwise = false;
        ring.fillAmount = 0f;
        ring.color = progressColor;
        ring.raycastTarget = false;

        // icon
        GameObject iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(root.transform, false);
        RectTransform rtI = iconGO.AddComponent<RectTransform>();
        rtI.sizeDelta = new Vector2(size * 0.60f, size * 0.60f);
        Image icon = iconGO.AddComponent<Image>();
        icon.raycastTarget = false;

        // frame
        GameObject frameGO = new GameObject("Frame");
        frameGO.transform.SetParent(root.transform, false);
        RectTransform rtF = frameGO.AddComponent<RectTransform>();
        rtF.sizeDelta = new Vector2(size, size);
        Image frame = frameGO.AddComponent<Image>(); if (frameSprite != null) frame.sprite = frameSprite;
        frame.raycastTarget = false;

        Item it = new Item();
        it.id = id;
        it.root = root;
        it.ring = ring;
        it.icon = icon;
        return it;
    }

    private void Update()
    {
        float now = Time.unscaledTime;
        for (int i = items.Count - 1; i >= 0; i--)
        {
            Item it = items[i];
            if (it.endAt <= 0f || !it.root.activeSelf) continue;
            float remaining = it.endAt - now;
            if (remaining <= 0f)
            {
                GameObject.Destroy(it.root);
                items.RemoveAt(i);
                continue;
            }
            float frac = remaining / it.duration;
            if (frac < 0f) frac = 0f;
            it.ring.fillAmount = frac;
        }
    }

    // Editor-time helpers
    public void SetAnchoredPosition(Vector2 pos) { anchoredPos = pos; var rt = GetComponent<RectTransform>(); if (rt != null) rt.anchoredPosition = pos; }
    public void SetSize(float s) { size = s; }
    public void SetSpacing(float s) { spacing = s; if (hgroup != null) hgroup.spacing = s; }
}
