using UnityEngine;

namespace SnakeGame.Powerups
{
    /// <summary>
    /// Component added to collectibles to let them be attracted to the nearest snake segment.
    /// It does not require a SpriteRenderer; works with any Renderer or WorldSpace Canvas.
    /// </summary>
    [DisallowMultipleComponent]
    public class Magnetable : MonoBehaviour
    {
        private Collider2D _col;
        private Rigidbody2D _rb;
        private Vector2 _vel; // simple integrator

        public void Bind(Collider2D c, Rigidbody2D rb)
        {
            _col = c;
            _rb = rb;
        }

        public void TickMagnet(Transform[] targets, float magnetRadius, float pullSpeed, float pullAccel)
        {
            if (!GlobalPowerupState.MagnetActive) return;
            if (_rb == null) return;
            Transform nearest = FindNearest(targets);
            if (nearest == null) return;

            Vector2 pos = _rb.position;
            Vector2 to = (Vector2)nearest.position - pos;
            float dist = to.magnitude;
            if (dist > magnetRadius) return;

            Vector2 dir = dist > 0.001f ? to / dist : Vector2.zero;
            float targetSpeed = pullSpeed * Mathf.Clamp01(1f - (dist / magnetRadius) * 0.5f);
            _vel = Vector2.MoveTowards(_vel, dir * targetSpeed, pullAccel * Time.deltaTime);

            Vector2 next = pos + _vel * Time.deltaTime;
            _rb.MovePosition(next);
        }

        private Transform FindNearest(Transform[] targets)
        {
            Transform best = null;
            float bestD = float.MaxValue;
            Vector3 p = transform.position;
            for (int i = 0; i < targets.Length; i++)
            {
                Transform t = targets[i];
                if (t == null) continue;
                float d = (t.position - p).sqrMagnitude;
                if (d < bestD)
                {
                    bestD = d;
                    best = t;
                }
            }
            return best;
        }
    }
}
