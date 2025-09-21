using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class BoardBackdrop : MonoBehaviour
{
    public enum FitTarget { CameraViewport, FullBoard }

    [Header("Fit")]
    public FitTarget fit = FitTarget.CameraViewport;
    public Camera cam;
    public Board board;
    [Tooltip("Grow the backdrop by this world amount to hide tiny seams.")]
    public float overscan = 0.05f;
    [Tooltip("Z offset relative to camera/board center.")]
    public float z = 5f;
    [Tooltip("Force Simple draw mode (prevents sprite tiling warnings).")]
    public bool forceSimpleDrawMode = true;
    [Tooltip("Update every frame while in Play Mode.")]
    public bool continuous = true;

    private SpriteRenderer sr;

    void Reset()
    {
        sr = GetComponent<SpriteRenderer>();
        if (!cam) cam = Camera.main;
        if (!board) board = FindObjectOfType<Board>();
    }
    void OnEnable(){ if (!sr) sr = GetComponent<SpriteRenderer>(); Apply(); }
#if UNITY_EDITOR
    void OnValidate(){ if (!Application.isPlaying) Apply(); }
#endif
    void LateUpdate(){ if (Application.isPlaying && continuous) Apply(); }

    public void Apply()
    {
        if (!sr) sr = GetComponent<SpriteRenderer>();
        if (forceSimpleDrawMode && sr.drawMode != SpriteDrawMode.Simple) sr.drawMode = SpriteDrawMode.Simple;

        if (fit == FitTarget.CameraViewport) FitToCamera();
        else FitToBoard();
    }

    private void FitToCamera()
    {
        if (!cam) cam = Camera.main;
        if (!cam || !sr || sr.sprite == null) return;

        var pixel = cam.pixelRect;
        float aspect = Mathf.Max(0.0001f, pixel.width / Mathf.Max(1f, pixel.height));
        float halfH = cam.orthographicSize;
        float halfW = halfH * aspect;
        float w = halfW * 2f + overscan;
        float h = halfH * 2f + overscan;

        Vector3 ctr = cam.transform.position;
        ctr.z += z;

        FitSprite(w, h);
        transform.position = new Vector3(ctr.x, ctr.y, ctr.z);
        sr.sortingOrder = -100;
    }

    private void FitToBoard()
    {
        if (!board || !sr || sr.sprite == null) return;

        float tile = board.tileWorldSize;
        float worldW = (board.borderX * 2 + 1) * tile + overscan;
        float worldH = (board.borderY * 2 + 1) * tile + overscan;

        var bl = board.CellToWorld(new Vector2Int(-board.borderX, -board.borderY));
        var tr = board.CellToWorld(new Vector2Int( board.borderX,  board.borderY));
        var ctr = (bl + tr) * 0.5f;
        ctr.z = (cam ? cam.transform.position.z : transform.position.z) + z;

        FitSprite(worldW, worldH);
        transform.position = ctr;
        sr.sortingOrder = -100;
    }

    private void FitSprite(float w, float h)
    {
        var sprite = sr.sprite;
        if (sprite == null) return;
        var sz = sprite.bounds.size;
        if (sz.x <= 0.0001f || sz.y <= 0.0001f) return;
        transform.localScale = new Vector3(w / sz.x, h / sz.y, 1f);
    }
}