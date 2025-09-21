using UnityEngine;
using System.Collections.Generic;

namespace SnakeGame.Powerups
{
    public class GhostEffect : IRuntimeEffect
    {
        private List<Collider2D> _snakeCols = new List<Collider2D>();

        public void OnStart(PowerupSO so)
        {
            GlobalPowerupState.GhostActive = true;

            GameObject root = SnakeLocator.TryFindSnakeRoot();
            if (root != null)
            {
                _snakeCols.Clear();
                Collider2D[] cols = root.GetComponentsInChildren<Collider2D>(true);
                _snakeCols.Clear();
                for (int i = 0; i < cols.Length; i++) _snakeCols.Add(cols[i]);
                for (int i = 0; i < _snakeCols.Count; i++)
                {
                    Collider2D c = _snakeCols[i];
                    if (c != null) c.isTrigger = true;
                }
            }
        }

        public void OnTick(PowerupSO so) { }

        public void OnStop(PowerupSO so)
        {
            for (int i = 0; i < _snakeCols.Count; i++)
            {
                Collider2D c = _snakeCols[i];
                if (c != null) c.isTrigger = false;
            }
            _snakeCols.Clear();
            GlobalPowerupState.GhostActive = false;
        }
    }
}
