using UnityEngine;

[DefaultExecutionOrder(1000)]
public class ScreenBuzz2D : MonoBehaviour
{
    public float maxOffset = 0.12f;
    public float frequency = 48f;

    private float t, dur, amp;
    private Vector3 baseLocalPos;
    private bool buzzing;

    void Awake() { baseLocalPos = transform.localPosition; }

    public void Buzz(float intensity, float duration)
    {
        amp = Mathf.Max(amp, Mathf.Clamp01(intensity));
        dur = Mathf.Max(dur, duration);
        t = 0f; buzzing = true;
    }

    void LateUpdate()
    {
        if (!buzzing) { baseLocalPos = transform.localPosition; return; }

        t += Time.unscaledDeltaTime;
        float u = Mathf.Clamp01(t / Mathf.Max(0.0001f, dur));
        float fall = 1f - u; fall *= fall;
        float a = amp * fall;

        float nx = (Mathf.PerlinNoise(Time.time * frequency, 0f) - 0.5f) * 2f;
        float ny = (Mathf.PerlinNoise(0f, Time.time * frequency) - 0.5f) * 2f;

        transform.localPosition = baseLocalPos + new Vector3(nx, ny, 0f) * (maxOffset * a);

        if (u >= 1f)
        {
            buzzing = false; amp = 0f; dur = 0f; t = 0f;
            transform.localPosition = baseLocalPos;
        }
    }
}