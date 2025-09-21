using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

namespace SnakeGame.Powerups
{
    public class PowerupSystem : MonoBehaviour
    {
        public static PowerupSystem Instance;

        [Header("Stash (bottom panel)")]
        public PowerupStash stash;

        [Header("Active Bar (top panel)")]
        public Transform activeContainer;
        public GameObject activeItemPrefab;

        [Header("Overlay")]
        public ScreenTintOverlay screenOverlay;

        private List<RunningEffect> _running = new List<RunningEffect>();

        private class RunningEffect
        {
            public PowerupSO data;
            public float endTime;
            public IRuntimeEffect runtime;
            public ActivePowerupUIItem uiItem;
        }

        private void Awake()
        {
            Instance = this;
            if (GlobalPowerupState.BaseFixedDeltaTime <= 0f)
            {
                GlobalPowerupState.BaseFixedDeltaTime = Time.fixedDeltaTime;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void OnPickup(PowerupSO so)
        {
            if (stash != null)
            {
                stash.Add(so);
            }
        }

        public void ActivateFromStash(PowerupSO so)
        {
            if (so == null) return;
            if (stash != null) stash.RemoveOne(so);
            StartEffect(so);
        }

        private void StartEffect(PowerupSO so)
        {
            IRuntimeEffect runtime = CreateRuntime(so.type);
            if (runtime == null) return;

            runtime.OnStart(so);

            RunningEffect re = new RunningEffect();
            re.data = so;
            re.endTime = Time.unscaledTime + so.duration;
            re.runtime = runtime;

            if (activeContainer != null && activeItemPrefab != null)
            {
                GameObject go = GameObject.Instantiate(activeItemPrefab, activeContainer);
                ActivePowerupUIItem item = go.GetComponent<ActivePowerupUIItem>();
                if (item == null) item = go.AddComponent<ActivePowerupUIItem>();
                item.Bind(so, re.endTime);
                re.uiItem = item;
            }

            _running.Add(re);
            UpdateOverlayTint();
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            for (int i = _running.Count - 1; i >= 0; i--)
            {
                RunningEffect re = _running[i];
                if (now >= re.endTime)
                {
                    re.runtime.OnStop(re.data);
                    if (re.uiItem != null) Destroy(re.uiItem.gameObject);
                    _running.RemoveAt(i);
                }
                else
                {
                    re.runtime.OnTick(re.data);
                }
            }
            UpdateOverlayTint();
        }

        private void UpdateOverlayTint()
        {
            GlobalPowerupState.ClearTints();
            for (int i = 0; i < _running.Count; i++)
            {
                PowerupSO d = _running[i].data;
                if (d != null) GlobalPowerupState.AddTint(d.overlayTint);
            }
            if (screenOverlay != null) screenOverlay.SetTint(GlobalPowerupState.MixTint());
        }

        private IRuntimeEffect CreateRuntime(PowerupType t)
        {
            switch (t)
            {
                case PowerupType.Freeze: return new FreezeEffect();
                case PowerupType.Ghost:  return new GhostEffect();
                case PowerupType.Shield: return new ShieldEffect();
                case PowerupType.Magnet: return new MagnetEffect();
                default: return null;
            }
        }
    }

    public interface IRuntimeEffect
    {
        void OnStart(PowerupSO so);
        void OnTick(PowerupSO so);
        void OnStop(PowerupSO so);
    }
}
