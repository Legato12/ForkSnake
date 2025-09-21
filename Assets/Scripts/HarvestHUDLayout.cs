using UnityEngine;

[ExecuteAlways]
[DefaultExecutionOrder(-800)]
public class HarvestHUDLayout : MonoBehaviour
{
    [SerializeField] private Board board;
    [SerializeField] private RectTransform topPanel;
    [SerializeField] private RectTransform bottomPanel;
    [SerializeField] private Camera cam;

    [Header("Clamps (pixels if board-projection used)")]
    [SerializeField] private float topMaxPx = 140f;
    [SerializeField] private float bottomMaxPx = 160f;
    [SerializeField] private float topMinPx = 72f;
    [SerializeField] private float bottomMinPx = 72f;

    private void Reset()
    {
        if (!board) board = FindObjectOfType<Board>();
        if (!cam)   cam   = Camera.main;
    }
    private void OnEnable(){ Apply(); }
#if UNITY_EDITOR
    private void OnValidate(){ if (!Application.isPlaying) Apply(); }
#endif
    private void LateUpdate(){ if (Application.isPlaying) Apply(); }

    private void Apply()
    {
        if (!board || !cam || !topPanel || !bottomPanel) return;

        // Prefer anchoring to camera viewport if it's inset (our camera fitter does this)
        var r = cam.rect;
        if (r.x > 0f || r.y > 0f || r.width < 0.999f || r.height < 0.999f)
        {
            AnchorFullWidthTop(topPanel, r.y + r.height, 1f);
            AnchorFullWidthBottom(bottomPanel, 0f, r.y);
            return;
        }

        // Otherwise, project board bounds
        var bl = board.CellToWorld(new Vector2Int(-board.borderX, -board.borderY));
        var tr = board.CellToWorld(new Vector2Int( board.borderX,  board.borderY));

        var vBL = cam.WorldToViewportPoint(bl);
        var vTR = cam.WorldToViewportPoint(tr);

        float yMin = Mathf.Clamp01(Mathf.Min(vBL.y, vTR.y));
        float yMax = Mathf.Clamp01(Mathf.Max(vBL.y, vTR.y));

        float availTop = Mathf.Clamp01(1f - yMax);
        float availBot = Mathf.Clamp01(yMin - 0f);

        float sh = Mathf.Max(1, Screen.height);
        float topH = Mathf.Max(topMinPx / sh, Mathf.Min(availTop, topMaxPx / sh));
        float botH = Mathf.Max(bottomMinPx / sh, Mathf.Min(availBot, bottomMaxPx / sh));

        AnchorFullWidthTop(topPanel, 1f - topH, 1f);
        AnchorFullWidthBottom(bottomPanel, 0f, botH);
    }

    private static void AnchorFullWidthTop(RectTransform rt, float yMin, float yMax)
    {
        rt.anchorMin = new Vector2(0f, yMin);
        rt.anchorMax = new Vector2(1f, yMax);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.pivot     = new Vector2(0.5f, 1f);
    }
    private static void AnchorFullWidthBottom(RectTransform rt, float yMin, float yMax)
    {
        rt.anchorMin = new Vector2(0f, yMin);
        rt.anchorMax = new Vector2(1f, yMax);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.pivot     = new Vector2(0.5f, 0f);
    }
}