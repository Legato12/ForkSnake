using UnityEngine;

public class Board : MonoBehaviour
{
    [Header("Grid bounds (centered at 0,0)")]
    public int borderX = 9;             // X: -borderX .. +borderX
    public int borderY = 5;             // Y: -borderY .. +borderY
    public float tileWorldSize = 1f;
    public bool wrap = true;

    [Header("Spawn safety (top HUD rows)")]
    public int safeTopRows = 2;          // do not spawn in top rows (space for HUD)
    public int PlayTopY => borderY - safeTopRows;

    public Vector3 CellToWorld(Vector2Int c)
    {
        var local = new Vector3(c.x * tileWorldSize, c.y * tileWorldSize, 0f);
        var w = transform.TransformPoint(local);
        w.z = 0f;
        return w;
    }

    public Vector2Int WorldToCell(Vector3 w)
    {
        var local = transform.InverseTransformPoint(w);
        int x = Mathf.RoundToInt(local.x / tileWorldSize);
        int y = Mathf.RoundToInt(local.y / tileWorldSize);
        return new Vector2Int(x, y);
    }

    public bool InBounds(Vector2Int c)
        => c.x >= -borderX && c.x <= borderX && c.y >= -borderY && c.y <= borderY;

    public Vector2Int Wrap(Vector2Int c)
    {
        if (c.x < -borderX) c.x = borderX; else if (c.x > borderX) c.x = -borderX;
        if (c.y < -borderY) c.y = borderY; else if (c.y > borderY) c.y = -borderY;
        return c;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        float w = (borderX * 2 + 1) * tileWorldSize;
        float h = (borderY * 2 + 1) * tileWorldSize;
        Gizmos.DrawWireCube(transform.position, new Vector3(w, h, 0.1f));
    }
#endif
}
