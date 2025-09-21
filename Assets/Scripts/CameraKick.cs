using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraKick : MonoBehaviour
{
    [SerializeField] private float kickPercent = 0.03f;
    [SerializeField] private float duration = 0.15f;

    private Camera cam;
    private Coroutine routine;

    private void Awake(){ cam = GetComponent<Camera>(); }

    public void Kick()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(KickRoutine());
    }

    private IEnumerator KickRoutine()
    {
        float baseSize = cam.orthographicSize;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;
            cam.orthographicSize = baseSize * (1f - kickPercent * Mathf.Sin(p * Mathf.PI));
            yield return null;
        }
        cam.orthographicSize = baseSize;
    }
}
