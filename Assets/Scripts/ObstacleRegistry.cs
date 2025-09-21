using UnityEngine;
using System.Collections.Generic;

public class ObstacleRegistry : MonoBehaviour
{
    // Optional singleton to support static wrappers.
    public static ObstacleRegistry I { get; private set; }

    private readonly HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
    private readonly Dictionary<Vector2Int, Transform> map = new Dictionary<Vector2Int, Transform>();

    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
    }

    // -------- Instance API --------
    public bool IsBlocked(Vector2Int c) => cells.Contains(c);

    public void Register(Vector2Int c, Transform tr)
    {
        cells.Add(c);
        map[c] = tr;
    }

    public bool TryConsume(Vector2Int c, out Transform tr)
    {
        if (cells.Remove(c) && map.TryGetValue(c, out tr))
        {
            map.Remove(c);
            return true;
        }
        tr = null;
        return false;
    }

    public void Clear()
    {
        cells.Clear();
        map.Clear();
    }

    // -------- Static convenience (compat) --------
    public static void SetBlocked(Vector2Int c, bool value)
    {
        if (I == null) return;
        if (value) { I.cells.Add(c); }
        else { I.cells.Remove(c); I.map.Remove(c); }
    }
}
