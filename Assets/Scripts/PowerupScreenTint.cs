// Unity 2020.3 LTS compatible.
// Full-screen tint for powerup feedback.
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class PowerupScreenTint : MonoBehaviour
{
    private Canvas canvas;
    private Image overlay;
    private float endAt = 0f;

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
        gameObject.SetActive(false);
    }

    public void Show(Color c, float duration)
    {
        overlay.color = c;
        endAt = Time.unscaledTime + duration;
        gameObject.SetActive(true);
        CancelInvoke("HideSelf");
        Invoke("HideSelf", duration);
    }

    private void HideSelf()
    {
        gameObject.SetActive(false);
    }
}
