using UnityEngine;

namespace SnakeGame.Powerups
{
    /// <summary>
    /// Ensures tail segments appear immediately at the tail without gaps and normalizes scale/visibility.
    /// Attach to the snake root if you observe 'holes' appearing on growth.
    /// </summary>
    public class TailSegmentFixer : MonoBehaviour
    {
        public Vector3 normalizedScale = new Vector3(1f, 1f, 1f);

        public void OnSegmentSpawned(Transform seg)
        {
            if (seg == null) return;
            SpriteRenderer sr = seg.GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null) sr.enabled = true;
            seg.localScale = normalizedScale;
            // Additional logic may be wired from your SnakeController when creating new segments:
            // - Place directly at last tail position.
            // - Link follow chain immediately.
        }
    }
}
