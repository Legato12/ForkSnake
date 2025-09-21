// Unity 2020.3 LTS compatible. No tuples.
// PowerupStash v17: robust static AddStatic(), best-instance resolver, reuse quickbar children, integrates with SnakePowerupRuntime.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class PowerupStash : MonoBehaviour
{
    private static PowerupStash s_Instance;
    private static readonly List<PowerupStash> s_All = new List<PowerupStash>();

    public static PowerupStash Instance { get { return GetOrCreate(); } }

    [Header("Wiring")]
    [SerializeField] public MonoBehaviour snake; // expects SnakeController
    [SerializeField] private Transform slotsParent = null;
    [SerializeField] private PowerupSlotUI slotPrefab = null;

    [Header("Config")]
    [SerializeField] private int maxSlots = 3;
    [SerializeField] private int stackLimitPerType = 1;
    [SerializeField] private bool useExistingChildrenAsSlots = true;
    [SerializeField] private bool ensureLayoutOnParent = true;

    private Dictionary<PowerupSO, SlotRuntime> slotsByDef = new Dictionary<PowerupSO, SlotRuntime>();
    private List<SlotRuntime> orderedSlots = new List<SlotRuntime>();

    private List<PowerupSlotUI> pooledUIs = new List<PowerupSlotUI>();
    private List<bool> pooledUsed = new List<bool>();

    private class SlotRuntime
    {
        public PowerupSO def;
        public int count;
        public PowerupSlotUI ui;
        public int pooledIndex;
    }

    private void Awake()
    {
        if (!s_All.Contains(this)) s_All.Add(this);
        if (s_Instance == null) s_Instance = this;

        if (snake == null)
        {
            MonoBehaviour[] all = GameObject.FindObjectsOfType<MonoBehaviour>(true);
            for (int i = 0; i < all.Length; i++)
            {
                MonoBehaviour mb = all[i];
                if (mb == null) continue;
                System.Type t = mb.GetType();
                if (t != null && t.Name == "SnakeController")
                {
                    snake = mb;
                    break;
                }
            }
        }

        EnsureUI();
    }

    private void OnDestroy()
    {
        s_All.Remove(this);
        if (s_Instance == this) s_Instance = null;
    }

    // Static robust access
    public static PowerupStash GetOrCreate()
    {
        PowerupStash best = null;
        for (int i = 0; i < s_All.Count; i++)
        {
            var s = s_All[i];
            if (s == null || !s.isActiveAndEnabled) continue;
            if (s.slotsParent != null && s.slotsParent.gameObject.activeInHierarchy) { best = s; break; }
            if (best == null) best = s;
        }
        if (best != null) { s_Instance = best; return best; }

        PowerupStash found = GameObject.FindObjectOfType<PowerupStash>(true);
        if (found != null) { if (!s_All.Contains(found)) s_All.Add(found); s_Instance = found; return found; }

        GameObject go = new GameObject("PowerupStash_AUTO");
        best = go.AddComponent<PowerupStash>();
        s_All.Add(best);
        s_Instance = best;
        return best;
    }

    public static bool AddStatic(PowerupSO def) { PowerupStash s = GetOrCreate(); return s != null && s.Add(def); }

    public bool Add(PowerupSO def)
    {
        if (def == null) return false;

        SlotRuntime slot;
        if (slotsByDef.TryGetValue(def, out slot))
        {
            if (stackLimitPerType > 0 && slot.count >= stackLimitPerType) return false;
            slot.count += 1; UpdateSlotUI(slot); return true;
        }

        if (orderedSlots.Count >= maxSlots) return false;

        PowerupSlotUI ui = CreateSlotUI(out int pooledIndex);
        ui.SetIcon(def.sprite); ui.SetCount(1); ui.SetInteractable(true); ui.onClick = OnSlotClicked;

        slot = new SlotRuntime(); slot.def = def; slot.count = 1; slot.ui = ui; slot.pooledIndex = pooledIndex;
        slotsByDef.Add(def, slot); orderedSlots.Add(slot); return true;
    }

    public bool Pickup(PowerupSO def) { return Add(def); }
    public bool PickupInstance(PowerupSO def) { return Add(def); }

    private void OnSlotClicked(PowerupSlotUI ui)
    {
        SlotRuntime target = null;
        for (int i = 0; i < orderedSlots.Count; i++) { SlotRuntime s = orderedSlots[i]; if (s != null && s.ui == ui) { target = s; break; } }
        if (target == null) return;

        bool activated = ActivateViaRuntime(target.def);
        if (!activated) activated = InvokeSnakeFallback(target.def);
        if (!activated) return;

        if (target.count > 0) target.count -= 1; UpdateSlotUI(target);
        if (target.count <= 0)
        {
            slotsByDef.Remove(target.def); orderedSlots.Remove(target);
            if (target.pooledIndex >= 0 && target.pooledIndex < pooledUsed.Count) { pooledUsed[target.pooledIndex] = false; ClearSlotUI(target.ui); }
            else { if (target.ui != null) GameObject.Destroy(target.ui.gameObject); }
        }
    }

    private bool ActivateViaRuntime(PowerupSO def)
    {
        if (snake == null) return false;
        SnakePowerupRuntime r = ((MonoBehaviour)snake).GetComponent<SnakePowerupRuntime>();
        if (r == null) r = ((MonoBehaviour)snake).gameObject.AddComponent<SnakePowerupRuntime>();
        if (r == null) return false;
        return r.ActivatePowerup(def);
    }

    private System.Reflection.MethodInfo cachedActivate;
    private bool InvokeSnakeFallback(PowerupSO def)
    {
        if (snake == null) return false;
        if (cachedActivate == null)
            cachedActivate = snake.GetType().GetMethod("ActivatePowerup", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (cachedActivate == null) return false;
        try { cachedActivate.Invoke(snake, new object[] { def }); return true; } catch { return false; }
    }

    private void UpdateSlotUI(SlotRuntime slot)
    {
        if (slot == null || slot.ui == null) return;
        slot.ui.SetIcon(slot.def != null ? slot.def.sprite : null);
        slot.ui.SetCount(slot.count);
        slot.ui.SetInteractable(slot.count > 0);
    }

    private void ClearSlotUI(PowerupSlotUI ui)
    {
        if (ui == null) return;
        ui.SetIcon(null); ui.SetCount(0); ui.SetInteractable(false); ui.onClick = OnSlotClicked;
    }

    private void EnsureUI()
    {
        if (EventSystem.current == null)
        {
            GameObject es = new GameObject("EventSystem"); es.AddComponent<EventSystem>(); es.AddComponent<StandaloneInputModule>(); es.hideFlags = HideFlags.DontSave;
        }

        if (slotsParent == null)
        {
            GameObject canvasGO = GameObject.Find("PowerupCanvas_AUTO"); Canvas canvas;
            if (canvasGO == null) { canvasGO = new GameObject("PowerupCanvas_AUTO"); canvas = canvasGO.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvasGO.AddComponent<CanvasScaler>(); canvasGO.AddComponent<GraphicRaycaster>(); }
            else { canvas = canvasGO.GetComponent<Canvas>(); if (canvas == null) canvas = canvasGO.AddComponent<Canvas>(); if (canvasGO.GetComponent<GraphicRaycaster>() == null) canvasGO.AddComponent<GraphicRaycaster>(); }

            GameObject panelGO = new GameObject("PowerupBar_AUTO"); panelGO.transform.SetParent(canvasGO.transform, false);
            RectTransform rt = panelGO.AddComponent<RectTransform>(); rt.anchorMin = new Vector2(0.5f, 0f); rt.anchorMax = new Vector2(0.5f, 0f); rt.pivot = new Vector2(0.5f, 0f); rt.anchoredPosition = new Vector2(0f, 10f); rt.sizeDelta = new Vector2(360f, 64f);
            HorizontalLayoutGroup h = panelGO.AddComponent<HorizontalLayoutGroup>(); h.childAlignment = TextAnchor.MiddleCenter; h.childForceExpandHeight = false; h.childForceExpandWidth = false; h.spacing = 8f;
            ContentSizeFitter fit = panelGO.AddComponent<ContentSizeFitter>(); fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize; fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            CanvasGroup cg = panelGO.AddComponent<CanvasGroup>(); cg.interactable = true; cg.blocksRaycasts = true;
            slotsParent = panelGO.transform;
        }
        else
        {
            if (ensureLayoutOnParent)
            {
                if (slotsParent.GetComponent<HorizontalLayoutGroup>() == null && slotsParent.GetComponent<GridLayoutGroup>() == null)
                { HorizontalLayoutGroup h = slotsParent.gameObject.AddComponent<HorizontalLayoutGroup>(); h.childAlignment = TextAnchor.MiddleCenter; h.childForceExpandHeight = false; h.childForceExpandWidth = false; h.spacing = 8f; }
                if (slotsParent.GetComponent<ContentSizeFitter>() == null)
                { ContentSizeFitter fit = slotsParent.gameObject.AddComponent<ContentSizeFitter>(); fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize; fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize; }
                CanvasGroup cg = slotsParent.GetComponent<CanvasGroup>(); if (cg == null) cg = slotsParent.gameObject.AddComponent<CanvasGroup>(); cg.interactable = true; cg.blocksRaycasts = true;
            }

            pooledUIs.Clear(); pooledUsed.Clear();
            for (int i = 0; i < slotsParent.childCount; i++)
            {
                Transform ch = slotsParent.GetChild(i);
                PowerupSlotUI ui = ch.GetComponent<PowerupSlotUI>();
                if (ui == null)
                {
                    Image icon = ch.GetComponentInChildren<Image>(true);
                    Button btn = ch.GetComponent<Button>(); if (btn == null) btn = ch.gameObject.AddComponent<Button>();
                    Text txt = ch.GetComponentInChildren<Text>(true);
                    if (icon == null) { GameObject iconGO = new GameObject("Icon"); iconGO.transform.SetParent(ch, false); icon = iconGO.AddComponent<Image>(); RectTransform rtI = iconGO.GetComponent<RectTransform>(); if (rtI != null) { rtI.anchorMin = new Vector2(0.1f,0.1f); rtI.anchorMax = new Vector2(0.9f,0.9f); rtI.offsetMin = Vector2.zero; rtI.offsetMax = Vector2.zero; } }
                    if (txt == null) { GameObject countGO = new GameObject("Count"); countGO.transform.SetParent(ch, false); txt = countGO.AddComponent<Text>(); txt.alignment = TextAnchor.LowerRight; txt.fontSize = 14; txt.text = ""; txt.color = new Color(1f,1f,1f,0.95f); RectTransform rtC = countGO.GetComponent<RectTransform>(); if (rtC != null) { rtC.anchorMin = new Vector2(1f,0f); rtC.anchorMax = new Vector2(1f,0f); rtC.pivot = new Vector2(1f,0f); rtC.anchoredPosition = new Vector2(-2f,2f); rtC.sizeDelta = new Vector2(22f,18f); } }
                    ui = ch.gameObject.AddComponent<PowerupSlotUI>(); ui.Bind(icon, txt, btn);
                }
                pooledUIs.Add(ui); pooledUsed.Add(false); ClearSlotUI(ui);
            }
        }

        if (slotPrefab == null && pooledUIs.Count == 0)
        {
            GameObject go = new GameObject("PowerupSlotUI_RUNTIME");
            RectTransform rt = go.AddComponent<RectTransform>(); rt.sizeDelta = new Vector2(56f, 56f);
            Image bg = go.AddComponent<Image>(); bg.raycastTarget = true;
            GameObject iconGO2 = new GameObject("Icon"); iconGO2.transform.SetParent(go.transform, false); RectTransform rtI2 = iconGO2.AddComponent<RectTransform>(); rtI2.anchorMin = new Vector2(0.1f,0.1f); rtI2.anchorMax = new Vector2(0.9f,0.9f); rtI2.offsetMin = Vector2.zero; rtI2.offsetMax = Vector2.zero; Image icon = iconGO2.AddComponent<Image>();
            GameObject countGO2 = new GameObject("Count"); countGO2.transform.SetParent(go.transform, false); RectTransform rtC2 = countGO2.AddComponent<RectTransform>(); rtC2.anchorMin = new Vector2(1f,0f); rtC2.anchorMax = new Vector2(1f,0f); rtC2.pivot = new Vector2(1f,0f); rtC2.anchoredPosition = new Vector2(-2f,2f); rtC2.sizeDelta = new Vector2(22f,18f); Text txt2 = countGO2.AddComponent<Text>(); txt2.alignment = TextAnchor.LowerRight; txt2.fontSize = 14; txt2.text = ""; txt2.color = new Color(1f,1f,1f,0.95f);
            Button btn2 = go.AddComponent<Button>(); PowerupSlotUI ui2 = go.AddComponent<PowerupSlotUI>(); ui2.Bind(icon, txt2, btn2);
            slotPrefab = ui2;
        }
    }

    private PowerupSlotUI CreateSlotUI(out int pooledIndex)
    {
        pooledIndex = -1;
        for (int i = 0; i < pooledUIs.Count; i++) if (!pooledUsed[i]) { pooledUsed[i] = true; pooledIndex = i; PowerupSlotUI ui = pooledUIs[i]; ui.onClick = OnSlotClicked; return ui; }
        PowerupSlotUI uiInst = null;
        if (slotPrefab != null) uiInst = GameObject.Instantiate<PowerupSlotUI>(slotPrefab, slotsParent);
        else { GameObject go = new GameObject("PowerupSlotUI"); go.transform.SetParent(slotsParent, false); uiInst = go.AddComponent<PowerupSlotUI>(); }
        return uiInst;
    }
}
