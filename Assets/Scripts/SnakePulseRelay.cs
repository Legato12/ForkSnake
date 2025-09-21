using UnityEngine;

[DefaultExecutionOrder(351)]
public class SnakePulseRelay : MonoBehaviour
{
    [SerializeField] private SnakePulseFX pulse;

    void Awake()
    {
        if (!pulse) pulse = GetComponent<SnakePulseFX>();
    }

    // Called by your game when an apple is eaten.
    public void OnApplePulse()
    {
        if (!pulse) return;

        // FIX: don’t call Kick(); forward the message the FX listens for.
        pulse.SendMessage("OnApplePulse", SendMessageOptions.DontRequireReceiver);
    }
}
