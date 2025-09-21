using UnityEngine;

[DefaultExecutionOrder(-50)]
public class BoardPlayArea : MonoBehaviour
{
    [SerializeField] private Board board;
    [Tooltip("Rows at the very top that are reserved for the HUD (non-playable).")]
    public int topRows = 2;
    [Tooltip("Rows at the very bottom that are reserved for the HUD (non-playable).")]
    public int bottomRows = 2;

    [Header("Wrap within play area")]
    [Tooltip("If ON, moving above PlayTopY wraps to PlayBottomY (and vice versa). Panels stay non-playable.")]
    public bool verticalWrapInPlayArea = true;

    // --- Gizmos ---
    [Header("Gizmos")]
    public bool showGizmos = true;
    public Color boardOutline = new Color(1f, 0.95f, 0.2f, 1f);
    public Color playOutline  = new Color(0.25f, 1f, 0.5f, 1f);
    public Color topTint      = new Color(1f, 0.35f, 0.35f, 0.25f);
    public Color bottomTint   = new Color(1f, 0.6f, 0.2f, 0.25f);
    public bool showWrapChevrons = true;

    public int PlayBottomY => -board.borderY + bottomRows;
    public int PlayTopY    =>  board.borderY - topRows;

    public bool IsInPlayArea(Vector2Int c)
    {
        if (c.x < -board.borderX || c.x > board.borderX) return false;
        if (c.y <  PlayBottomY  || c.y > PlayTopY)       return false;
        return true;
    }

    public Vector2Int WorldToCell(Vector3 worldPos)
    {
        var local = board.transform.InverseTransformPoint(worldPos);
        int cx = Mathf.RoundToInt(local.x / board.tileWorldSize);
        int cy = Mathf.RoundToInt(local.y / board.tileWorldSize);
        return new Vector2Int(cx, cy);
    }

    public Vector3 CellToWorld(Vector2Int c) => board.CellToWorld(c);

    private void Reset()
    {
        if (!board) board = FindObjectOfType<Board>();
    }

    // ---------- Gizmos ----------
    private void OnDrawGizmos()
    {
        if (!showGizmos || board == null) return;

        // Board rect (full)
        var minB = board.CellToWorld(new Vector2Int(-board.borderX, -board.borderY));
        var maxB = board.CellToWorld(new Vector2Int( board.borderX,  board.borderY));
        var sizeB = new Vector3((maxB.x - minB.x) + board.tileWorldSize, (maxB.y - minB.y) + board.tileWorldSize, 0.01f);
        var ctrB  = (minB + maxB) * 0.5f;

        // Play rect
        var minP = board.CellToWorld(new Vector2Int(-board.borderX, PlayBottomY));
        var maxP = board.CellToWorld(new Vector2Int( board.borderX,  PlayTopY));
        var sizeP = new Vector3((maxP.x - minP.x) + board.tileWorldSize, (maxP.y - minP.y) + board.tileWorldSize, 0.01f);
        var ctrP  = (minP + maxP) * 0.5f;

        Gizmos.color = boardOutline;
        Gizmos.DrawWireCube(ctrB, sizeB);

        Gizmos.color = playOutline;
        Gizmos.DrawWireCube(ctrP, sizeP);

        // Top reserved
        if (topRows > 0)
        {
            var minT = board.CellToWorld(new Vector2Int(-board.borderX, PlayTopY + 1));
            var maxT = board.CellToWorld(new Vector2Int( board.borderX,  board.borderY));
            var sizeT = new Vector3((maxT.x - minT.x) + board.tileWorldSize, (maxT.y - minT.y) + board.tileWorldSize, 0.01f);
            var ctrT  = (minT + maxT) * 0.5f;
            Gizmos.color = topTint;
            Gizmos.DrawCube(ctrT, sizeT);
        }

        // Bottom reserved
        if (bottomRows > 0)
        {
            var minBo = board.CellToWorld(new Vector2Int(-board.borderX, -board.borderY));
            var maxBo = board.CellToWorld(new Vector2Int( board.borderX,  PlayBottomY - 1));
            var sizeBo = new Vector3((maxBo.x - minBo.x) + board.tileWorldSize, (maxBo.y - minBo.y) + board.tileWorldSize, 0.01f);
            var ctrBo  = (minBo + maxBo) * 0.5f;
            Gizmos.color = bottomTint;
            Gizmos.DrawCube(ctrBo, sizeBo);
        }

        if (showWrapChevrons)
            DrawWrapChevrons(ctrP, sizeP);
    }

    private void DrawWrapChevrons(Vector3 playCtr, Vector3 playSize)
    {
        float tile = board.tileWorldSize;
        float w = playSize.x;
        float h = playSize.y;
        float leftX  = playCtr.x - w * 0.5f;
        float rightX = playCtr.x + w * 0.5f;
        float bottomY = playCtr.y - h * 0.5f;
        float topY    = playCtr.y + h * 0.5f;

        float stepY = Mathf.Max(tile * 2.2f, h / 5f);
        float stepX = Mathf.Max(tile * 2.2f, w / 8f);
        float inset = tile * 0.25f;
        float arm   = tile * 0.5f;

        Gizmos.color = new Color(1f, 1f, 1f, 0.85f);

        // left/right chevrons
        for (float y = bottomY + tile * 1.2f; y <= topY - tile * 1.2f; y += stepY)
        {
            Vector3 A = new Vector3(leftX + inset, y - arm * 0.6f, 0f);
            Vector3 B = new Vector3(leftX + inset + arm, y, 0f);
            Vector3 C = new Vector3(leftX + inset, y + arm * 0.6f, 0f);
            Gizmos.DrawLine(A, B); Gizmos.DrawLine(B, C);

            Vector3 A2 = new Vector3(rightX - inset, y - arm * 0.6f, 0f);
            Vector3 B2 = new Vector3(rightX - inset - arm, y, 0f);
            Vector3 C2 = new Vector3(rightX - inset, y + arm * 0.6f, 0f);
            Gizmos.DrawLine(A2, B2); Gizmos.DrawLine(B2, C2);
        }

        // top/bottom chevrons (vertical wrap)
        if (verticalWrapInPlayArea)
        {
            for (float x = leftX + tile * 1.2f; x <= rightX - tile * 1.2f; x += stepX)
            {
                // bottom '^'
                Vector3 BA = new Vector3(x - arm * 0.6f, bottomY + inset + arm, 0f);
                Vector3 BB = new Vector3(x,               bottomY + inset,      0f);
                Vector3 BC = new Vector3(x + arm * 0.6f, bottomY + inset + arm, 0f);
                Gizmos.DrawLine(BA, BB); Gizmos.DrawLine(BB, BC);

                // top 'v'
                Vector3 TA = new Vector3(x - arm * 0.6f, topY - inset - arm, 0f);
                Vector3 TB = new Vector3(x,               topY - inset,      0f);
                Vector3 TC = new Vector3(x + arm * 0.6f, topY - inset - arm, 0f);
                Gizmos.DrawLine(TA, TB); Gizmos.DrawLine(TB, TC);
            }
        }
    }
}