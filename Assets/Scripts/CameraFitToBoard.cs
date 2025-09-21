using UnityEngine;

[ExecuteAlways]
[DefaultExecutionOrder(-900)]
[RequireComponent(typeof(Camera))]
public class CameraFitToBoard : MonoBehaviour
{
    public enum Strategy
    {
        FullWidthIfPossible, // Use full screen width; put leftover height into top/bottom bars
        MatchBoardAspect     // Exact board aspect in viewport; may cause thin side bands
    }

    [SerializeField] private Board board;
    public Strategy strategy = Strategy.FullWidthIfPossible;

    [Header("Minimum HUD padding (pixels)")]
    public int minTopPx = 72;
    public int minBottomPx = 72;
    public int minSidePx = 0;

    [Tooltip("Continuously recompute in Play Mode. Leave ON.")]
    public bool continuous = true;

    private Camera cam;

    private void Reset()
    {
        cam = GetComponent<Camera>();
        if (!board) board = FindObjectOfType<Board>();
        cam.orthographic = true;
    }
    private void OnEnable()  { Apply(true); }
#if UNITY_EDITOR
    private void OnValidate(){ if (!Application.isPlaying) Apply(false); }
#endif
    private void LateUpdate(){ if (Application.isPlaying && continuous) Apply(false); }

    public void Apply(bool forceFind)
    {
        if (forceFind || cam == null) cam = GetComponent<Camera>();
        if (forceFind || board == null) board = FindObjectOfType<Board>();
        if (!cam || !board) return;

        cam.orthographic = true;

        float tile = board.tileWorldSize;
        float worldW = (board.borderX * 2 + 1) * tile;
        float worldH = (board.borderY * 2 + 1) * tile;
        float boardAspect = Mathf.Max(0.0001f, worldW / worldH);

        int sw = Mathf.Max(1, Screen.width);
        int sh = Mathf.Max(1, Screen.height);
        float screenAspect = (float)sw / sh;

        float vx = 0f, vy = 0f, vw = 1f, vh = 1f;

        if (strategy == Strategy.FullWidthIfPossible)
        {
            // Try: use full width, shrink height so effective aspect equals board
            float vhNeeded = Mathf.Clamp01(screenAspect / boardAspect); // <= 1
            float leftover = 1f - vhNeeded;
            float minTB = (float)(minTopPx + minBottomPx) / sh;

            if (leftover + 1e-5f >= minTB)
            {
                // We can keep full width
                float minTop = (float)minTopPx / sh;
                float minBot = (float)minBottomPx / sh;
                float extra = Mathf.Max(0f, leftover - (minTop + minBot));
                vy = minBot + extra * 0.5f;
                vx = 0f; vw = 1f; vh = vhNeeded;
            }
            else
            {
                // Not enough leftover for min top/bottom. Fall back to matching aspect with minTB reserved.
                float vhAvail = Mathf.Clamp01(1f - minTB);
                float vwNeeded = Mathf.Clamp01(vhAvail * boardAspect / screenAspect);
                float minSide = (float)minSidePx / sw;
                vw = Mathf.Clamp01(vwNeeded);
                if (vw > 1f - 2f * minSide) vw = 1f - 2f * minSide;
                vx = (1f - vw) * 0.5f;
                vy = (float)minBottomPx / sh;
                vh = vhAvail;
            }
        }
        else // MatchBoardAspect
        {
            float minTB = (float)(minTopPx + minBottomPx) / sh;
            vh = Mathf.Clamp01(1f - minTB);
            float minSide = (float)minSidePx / sw;
            vw = Mathf.Clamp01(vh * boardAspect / screenAspect);
            if (vw > 1f - 2f * minSide) vw = 1f - 2f * minSide;
            vx = (1f - vw) * 0.5f;
            vy = (float)minBottomPx / sh;
        }

        cam.rect = new Rect(vx, vy, vw, vh);

        // Now fit ortho to CONTAIN the board within this viewport
        var pixel = cam.pixelRect;
        float aspect = Mathf.Max(0.0001f, pixel.width / Mathf.Max(1f, pixel.height));
        float sizeH = worldH * 0.5f;
        float sizeW = (worldW * 0.5f) / aspect;
        cam.orthographicSize = Mathf.Max(sizeH, sizeW);

        // Center on board
        var bl = board.CellToWorld(new Vector2Int(-board.borderX, -board.borderY));
        var tr = board.CellToWorld(new Vector2Int( board.borderX,  board.borderY));
        var ctr = (bl + tr) * 0.5f;
        var t = cam.transform;
        t.position = new Vector3(ctr.x, ctr.y, t.position.z);
    }
}