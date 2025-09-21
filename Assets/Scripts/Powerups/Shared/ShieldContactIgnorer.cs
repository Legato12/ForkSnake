using UnityEngine;
using System.Collections.Generic;

namespace SnakeGame.Powerups
{
    /// <summary>
    /// Temporarily ignores collision pairs while Shield is active, allowing the snake to pass through hazards.
    /// </summary>
    public class ShieldContactIgnorer : MonoBehaviour
    {
        public LayerMask hazardLayers = ~0; // Configure in inspector to relevant hazard layers

        private Dictionary<Collider2D, float> _ignoredUntil = new Dictionary<Collider2D, float>();
        private Collider2D[] _snakeCols = new Collider2D[0];
        private float _ignoreSeconds = 0.5f;

        private void OnEnable()
        {
            _ignoredUntil.Clear();
            CacheSnakeColliders();
        }

        private void OnDisable()
        {
            // restore all
            foreach (var kv in _ignoredUntil)
            {
                Collider2D other = kv.Key;
                RestoreAllPairsWith(other);
            }
            _ignoredUntil.Clear();
        }

        private void CacheSnakeColliders()
        {
            Collider2D[] tmp = GetComponentsInChildren<Collider2D>(true);
            _snakeCols = tmp;
        }

        private void Update()
        {
            if (!GlobalPowerupState.ShieldActive) return;

            float now = Time.time;
            // Re-enable expired pairs
            List<Collider2D> toEnable = new List<Collider2D>();
            foreach (var kv in _ignoredUntil)
            {
                if (now >= kv.Value) toEnable.Add(kv.Key);
            }
            for (int i = 0; i < toEnable.Count; i++)
            {
                RestoreAllPairsWith(toEnable[i]);
                _ignoredUntil.Remove(toEnable[i]);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!GlobalPowerupState.ShieldActive) return;
            if (IsHazard(collision.collider))
            {
                IgnoreAllPairsWith(collision.collider);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!GlobalPowerupState.ShieldActive) return;
            if (IsHazard(other))
            {
                IgnoreAllPairsWith(other);
            }
        }

        private bool IsHazard(Collider2D c)
        {
            int layer = c.gameObject.layer;
            return (hazardLayers.value & (1 << layer)) != 0;
        }

        private void IgnoreAllPairsWith(Collider2D other)
        {
            float until = Time.time + _ignoreSeconds;
            _ignoredUntil[other] = until;
            for (int i = 0; i < _snakeCols.Length; i++)
            {
                Collider2D s = _snakeCols[i];
                if (s == null) continue;
                Physics2D.IgnoreCollision(s, other, true);
            }
        }

        private void RestoreAllPairsWith(Collider2D other)
        {
            for (int i = 0; i < _snakeCols.Length; i++)
            {
                Collider2D s = _snakeCols[i];
                if (s == null) continue;
                Physics2D.IgnoreCollision(s, other, false);
            }
        }
    }
}
