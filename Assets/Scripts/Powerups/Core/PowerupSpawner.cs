using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SnakeGame.Powerups
{
    public class PowerupSpawner : MonoBehaviour
    {
        public PowerupSO[] table;
        public GameObject pickupFallbackPrefab;
        [Min(0.25f)] public float spawnInterval = 8f;
        public Transform[] spawnPoints;

        private Coroutine _co;
        private float _intervalScale = 1f;

        private void OnEnable()
        {
            _co = StartCoroutine(SpawnLoop());
            SpawnRateScaler.Register(this);
        }

        private void OnDisable()
        {
            if (_co != null) StopCoroutine(_co);
            SpawnRateScaler.Unregister(this);
        }

        public void SetIntervalScale(float s)
        {
            if (s < 0.1f) s = 0.1f;
            _intervalScale = s;
        }

        private IEnumerator SpawnLoop()
        {
            WaitForSeconds w = new WaitForSeconds(0.5f);
            yield return w; // small delay
            while (enabled)
            {
                float wait = spawnInterval * _intervalScale;
                if (wait < 0.1f) wait = 0.1f;
                yield return new WaitForSeconds(wait);
                SpawnOne();
            }
        }

        private void SpawnOne()
        {
            if (table == null || table.Length == 0 || pickupFallbackPrefab == null) return;
            PowerupSO chosen = table[Random.Range(0, table.Length)];
            if (chosen == null) return;

            Transform p = transform;
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                p = spawnPoints[Random.Range(0, spawnPoints.Length)];
            }

            GameObject go = GameObject.Instantiate(pickupFallbackPrefab, p.position, Quaternion.identity);
            PowerupPickup pu = go.GetComponent<PowerupPickup>();
            if (pu == null) pu = go.AddComponent<PowerupPickup>();
            pu.data = chosen;

            // Ensure visible sprite
            if (pu.targetRenderer == null) pu.targetRenderer = go.GetComponentInChildren<SpriteRenderer>();
            if (pu.targetRenderer != null && chosen.sprite != null)
            {
                pu.targetRenderer.sprite = chosen.sprite;
            }
        }
    }
}
