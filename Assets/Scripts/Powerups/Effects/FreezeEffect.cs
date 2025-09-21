using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

namespace SnakeGame.Powerups
{
    public class FreezeEffect : IRuntimeEffect
    {
        private Dictionary<Object, List<PowerupReflectionUtils.ScaledField>> _scaledSnake = new Dictionary<Object, List<PowerupReflectionUtils.ScaledField>>();
        private List<PowerupReflectionUtils.ScaledField> _scaledSpawners = new List<PowerupReflectionUtils.ScaledField>();

        public void OnStart(PowerupSO so)
        {
            GlobalPowerupState.FreezeActive = true;

            if (GlobalPowerupState.BaseFixedDeltaTime <= 0f)
                GlobalPowerupState.BaseFixedDeltaTime = Time.fixedDeltaTime;
            GlobalPowerupState.BaseTimeScale = Time.timeScale;

            Time.timeScale = 0.5f;
            Time.fixedDeltaTime = GlobalPowerupState.BaseFixedDeltaTime * 0.5f;

            // Slightly speed up snake internals (so overall it doesn't crawl too slow)
            // Try scaling common speed fields by 1.5
            float snakeMul = 1.5f;
            GameObject snake = SnakeLocator.TryFindSnakeRoot();
            if (snake != null)
            {
                Component[] comps = snake.GetComponentsInChildren<Component>(true);
                for (int i = 0; i < comps.Length; i++)
                {
                    Component c = comps[i];
                    if (c == null) continue;
                    List<PowerupReflectionUtils.ScaledField> list = PowerupReflectionUtils.ScaleFloatFieldsIfNameContains(c, new string[] { "speed", "move", "tick" }, snakeMul);
                    if (list != null && list.Count > 0)
                    {
                        _scaledSnake[c] = list;
                    }
                }
            }

            // Slow spawners intervals by ~x2
            PowerupSpawner[] spawners = GameObject.FindObjectsOfType<PowerupSpawner>();
            for (int i = 0; i < spawners.Length; i++)
            {
                spawners[i].SetIntervalScale(2f);
            }

            // Generic: also reflect common "spawnInterval" fields in other spawners
            MonoBehaviour[] all = GameObject.FindObjectsOfType<MonoBehaviour>();
            for (int i = 0; i < all.Length; i++)
            {
                MonoBehaviour m = all[i];
                if (m == null) continue;
                List<PowerupReflectionUtils.ScaledField> list = PowerupReflectionUtils.ScaleFloatFieldsIfNameMatches(m, new string[] { "spawninterval", "interval", "minspawninterval", "maxspawninterval" }, 2f);
                if (list != null && list.Count > 0)
                {
                    _scaledSpawners.AddRange(list);
                }
            }
        }

        public void OnTick(PowerupSO so) { }

        public void OnStop(PowerupSO so)
        {
            // restore
            Time.timeScale = GlobalPowerupState.BaseTimeScale <= 0f ? 1f : GlobalPowerupState.BaseTimeScale;
            Time.fixedDeltaTime = GlobalPowerupState.BaseFixedDeltaTime <= 0f ? 0.02f : GlobalPowerupState.BaseFixedDeltaTime;

            foreach (var kv in _scaledSnake)
            {
                List<PowerupReflectionUtils.ScaledField> list = kv.Value;
                for (int i = 0; i < list.Count; i++) PowerupReflectionUtils.Revert(list[i]);
            }
            _scaledSnake.Clear();

            for (int i = 0; i < _scaledSpawners.Count; i++) PowerupReflectionUtils.Revert(_scaledSpawners[i]);
            _scaledSpawners.Clear();

            PowerupSpawner[] sp = GameObject.FindObjectsOfType<PowerupSpawner>();
            for (int i = 0; i < sp.Length; i++) sp[i].SetIntervalScale(1f);

            GlobalPowerupState.FreezeActive = false;
        }
    }
}
