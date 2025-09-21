using UnityEngine;
// Simple integrity fixer: ensures all child SpriteRenderers of the snake are enabled & visible.
[DisallowMultipleComponent]
public sealed class SnakeTailIntegrityFixer : MonoBehaviour
{
    [Tooltip("Call Check() automatically each frame (low cost).")]
    public bool autoFixEachFrame = true;

    private void Update(){ if (autoFixEachFrame) Check(); }

    [ContextMenu("Check Now")]
    public void Check()
    {
        var srs = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i=0;i<srs.Length;i++)
        {
            var sr = srs[i];
            if (sr == null) continue;
            if (!sr.enabled) sr.enabled = true;
            if (sr.color.a < 0.95f) { var c = sr.color; c.a = 1f; sr.color = c; }
            var t = sr.transform;
            if (Mathf.Abs(t.localScale.x) < 0.01f || Mathf.Abs(t.localScale.y) < 0.01f)
                t.localScale = new Vector3(1f,1f,1f);
        }
    }
}
