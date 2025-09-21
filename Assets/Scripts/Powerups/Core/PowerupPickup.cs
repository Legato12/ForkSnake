using UnityEngine;

namespace SnakeGame.Powerups
{
    /// <summary>
    /// Attach to world pickup spawned by PowerupSpawner.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PowerupPickup : MonoBehaviour
    {
        public PowerupSO data;
        public SpriteRenderer targetRenderer;

        private void Reset()
        {
            Collider2D c = GetComponent<Collider2D>();
            c.isTrigger = true;
            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        private void Awake()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<SpriteRenderer>();
            }
            if (data != null && targetRenderer != null && data.sprite != null)
            {
                targetRenderer.sprite = data.sprite;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Basic condition: snake head or root tagged Player or containing SnakeController
            if (other.CompareTag("Player") || other.name.Contains("Head"))
            {
                PowerupSystem instance = PowerupSystem.Instance;
                if (instance != null && data != null)
                {
                    instance.OnPickup(data);
                    Destroy(gameObject);
                }
            }
        }
    }
}
