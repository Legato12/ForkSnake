using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class StatusIcon : MonoBehaviour
{
    public Image bg, ring, glyph, frame;
    CanvasGroup cg; LayoutElement le;
    float endTime, duration; bool active;

    void Awake(){ HideImmediate(); }

    void Ensure()
    {
        if (!cg) cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();
        if (!le) le = GetComponent<LayoutElement>();
        if (!le) le = gameObject.AddComponent<LayoutElement>();
    }

    public void Show(Sprite glyphSprite, Color ringColor, float dur)
    {
        Ensure();
        active = true; duration = Mathf.Max(0.01f, dur); endTime = Time.time + duration;
        cg.alpha = 1f; le.ignoreLayout = false;

        if (bg) bg.enabled = true;
        if (frame) frame.enabled = true;
        if (glyph){ glyph.enabled = true; glyph.sprite = glyphSprite; }
        if (ring)
        {
            ring.enabled = true;
            ring.type = Image.Type.Filled;
            ring.fillMethod = Image.FillMethod.Radial360;
            ring.fillOrigin = 2;
            ring.fillClockwise = false;
            ring.color = ringColor;
            ring.fillAmount = 1f;
        }
    }

    public void HideImmediate()
    {
        Ensure();
        active = false;
        if (ring){ ring.enabled = false; ring.fillAmount = 0f; }
        if (glyph) glyph.enabled = false;
        if (bg) bg.enabled = false;
        if (frame) frame.enabled = false;
        cg.alpha = 0f; le.ignoreLayout = true;
    }

    void Update()
    {
        if (!active || ring == null) return;
        float t = Mathf.Clamp01((endTime - Time.time)/duration);
        ring.fillAmount = t;
        if (t <= 0f) HideImmediate();
    }
}
