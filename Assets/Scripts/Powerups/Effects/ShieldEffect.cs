using UnityEngine;
using System.Collections.Generic;

namespace SnakeGame.Powerups
{
    public class ShieldEffect : IRuntimeEffect
    {
        private ShieldContactIgnorer _ignorer;

        public void OnStart(PowerupSO so)
        {
            GlobalPowerupState.ShieldActive = true;
            GameObject root = SnakeLocator.TryFindSnakeRoot();
            if (root != null)
            {
                _ignorer = root.GetComponentInChildren<ShieldContactIgnorer>(true);
                if (_ignorer == null) _ignorer = root.AddComponent<ShieldContactIgnorer>();
                _ignorer.enabled = true;
            }
        }

        public void OnTick(PowerupSO so) { }

        public void OnStop(PowerupSO so)
        {
            if (_ignorer != null) _ignorer.enabled = false;
            GlobalPowerupState.ShieldActive = false;
        }
    }
}
