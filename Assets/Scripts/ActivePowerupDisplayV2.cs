
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ActivePowerupDisplayV2 : MonoBehaviour
{
    public Image bg;
    public Image ring;
    public Image frame;
    public Image icon;

    Coroutine co;

    void Awake(){ Hide(); }

    public void Show(Sprite s, float duration)
    {
        if (icon){ icon.sprite = s; icon.enabled = s != null; }
        if (bg) bg.enabled = true;
        if (frame) frame.enabled = true;
        if (ring)
        {
            ring.enabled = true;
            ring.type = Image.Type.Filled;
            ring.fillMethod = Image.FillMethod.Radial360;
            ring.fillOrigin = (int)Image.Origin360.Top;
            ring.fillClockwise = false;
            ring.fillAmount = 1f;
        }

        if (co != null) StopCoroutine(co);
        co = StartCoroutine(Run(duration));
        gameObject.SetActive(true);
    }

    public void SetProgress(float t){ if (ring) ring.fillAmount = Mathf.Clamp01(1f - t); }

    public void Hide()
    {
        if (co != null) StopCoroutine(co); co = null;
        if (bg) bg.enabled = false;
        if (frame) frame.enabled = false;
        if (icon) icon.enabled = false;
        if (ring) ring.enabled = false;
        gameObject.SetActive(false);
    }

    IEnumerator Run(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            SetProgress(t / Mathf.Max(0.001f, duration));
            yield return null;
        }
        Hide();
    }
}
