using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class UIFitToCameraViewport : MonoBehaviour
{
    public Camera cam;
    public bool fitToViewport = true;  // if false → full screen

    RectTransform rt;

    void OnEnable() { rt = GetComponent<RectTransform>(); if (!cam) cam = Camera.main; Apply(); }
#if UNITY_EDITOR
    void OnValidate(){ if (!Application.isPlaying) { if (!rt) rt = GetComponent<RectTransform>(); if (!cam) cam = Camera.main; Apply(); } }
#endif
    void LateUpdate(){ if (Application.isPlaying) Apply(); }

    void Apply()
    {
        if (!rt || !cam) return;
        if (!fitToViewport)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return;
        }
        var r = cam.rect;
        rt.anchorMin = new Vector2(r.xMin, r.yMin);
        rt.anchorMax = new Vector2(r.xMax, r.yMax);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}