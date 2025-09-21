using UnityEngine;
using System.Collections.Generic;

namespace SnakeGame.Powerups
{
    /// <summary>
    /// Registry pattern so Freeze can scale spawner intervals ~x2 easily.
    /// </summary>
    public static class SpawnRateScaler
    {
        private static List<PowerupSpawner> _spawners = new List<PowerupSpawner>();

        public static void Register(PowerupSpawner s)
        {
            if (s == null) return;
            if (!_spawners.Contains(s)) _spawners.Add(s);
            ApplyScale();
        }

        public static void Unregister(PowerupSpawner s)
        {
            if (s == null) return;
            _spawners.Remove(s);
        }

        public static void ApplyScale()
        {
            float scale = GlobalPowerupState.FreezeActive ? 2f : 1f;
            for (int i = 0; i < _spawners.Count; i++)
            {
                if (_spawners[i] != null) _spawners[i].SetIntervalScale(scale);
            }
        }
    }
}
