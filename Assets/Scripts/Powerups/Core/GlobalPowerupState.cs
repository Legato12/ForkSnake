using UnityEngine;
using System.Collections.Generic;

namespace SnakeGame.Powerups
{
    /// <summary>
    /// Static flags/state accessible across systems.
    /// </summary>
    public static class GlobalPowerupState
    {
        public static bool GhostActive;
        public static bool ShieldActive;
        public static bool MagnetActive;
        public static bool FreezeActive;

        // Freeze tuning
        public static float BaseFixedDeltaTime;
        public static float BaseTimeScale = 1f;

        // For overlay tint mixing
        private static List<Color> _activeTints = new List<Color>();

        public static void ClearTints()
        {
            _activeTints.Clear();
        }

        public static void AddTint(Color c)
        {
            _activeTints.Add(c);
        }

        public static Color MixTint()
        {
            if (_activeTints.Count == 0) return new Color(0f, 0f, 0f, 0f);
            float r = 0f, g = 0f, b = 0f, a = 0f;
            for (int i = 0; i < _activeTints.Count; i++)
            {
                Color c = _activeTints[i];
                r += c.r * c.a;
                g += c.g * c.a;
                b += c.b * c.a;
                a += c.a;
            }
            if (a > 1f) a = 1f;
            // Normalize rgb by total alpha so overlapping colors blend rather than overbrighten
            if (a > 0.001f)
            {
                r /= a;
                g /= a;
                b /= a;
            }
            return new Color(r, g, b, Mathf.Clamp01(a));
        }
    }
}
