using UnityEngine;

public enum PowerupId { Shield, Magnet, Ghost, Freeze }

[CreateAssetMenu(menuName="Snake/Powerup")]
public class PowerupSO : ScriptableObject
{
    public PowerupId id;
    public string displayName;
    public Sprite sprite;
    public float duration = 8f;
    public int magnetRadius = 3;
    public float freezeStepMultiplier = 1.25f;
    public int weight = 1;
}
