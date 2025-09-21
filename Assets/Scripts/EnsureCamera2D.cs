using UnityEngine;

[RequireComponent(typeof(Camera))]
[DefaultExecutionOrder(-1000)]
public class EnsureCamera2D : MonoBehaviour
{
    [Tooltip("Camera Z to keep (negative).")]
    public float z = -10f;
    public bool forceOrthographic = true;
    public float near = 0.01f;
    public float far = 1000f;

    void Awake()  { Apply(); }
    void Reset()  { Apply(); }
#if UNITY_EDITOR
    void OnValidate() { if (!Application.isPlaying) Apply(); }
#endif

    private void Apply()
    {
        var cam = GetComponent<Camera>();
        if (forceOrthographic) cam.orthographic = true;
        var t = transform;
        if (Mathf.Abs(t.position.z - z) > 0.0001f)
            t.position = new Vector3(t.position.x, t.position.y, z);
        cam.nearClipPlane = near;
        cam.farClipPlane = far;
    }
}