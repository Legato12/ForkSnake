using UnityEngine;

namespace SnakeGame.Powerups
{
    [CreateAssetMenu(fileName = "PowerupSO", menuName = "Snake/Powerups/Powerup SO", order = 0)]
    public class PowerupSO : ScriptableObject
    {
        public PowerupType type = PowerupType.None;
        [Min(0.1f)] public float duration = 5f;
        public Sprite sprite;
        public Color overlayTint = new Color(1f, 1f, 1f, 0.25f);
        public AudioClip pickupSfx;
        public AudioClip activateSfx;
    }
}
