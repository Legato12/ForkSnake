using UnityEngine;
using System.Collections.Generic;

namespace SnakeGame.Powerups
{
    public class MagnetEffect : IRuntimeEffect
    {
        private MagnetRuntime _runtime;

        public void OnStart(PowerupSO so)
        {
            GlobalPowerupState.MagnetActive = true;

            // Ensure runtime driver exists
            GameObject go = GameObject.Find("MagnetRuntime");
            if (go == null)
            {
                go = new GameObject("MagnetRuntime");
                _runtime = go.AddComponent<MagnetRuntime>();
                Object.DontDestroyOnLoad(go);
            }
            else
            {
                _runtime = go.GetComponent<MagnetRuntime>();
                if (_runtime == null) _runtime = go.AddComponent<MagnetRuntime>();
            }
            _runtime.enabled = true;
        }

        public void OnTick(PowerupSO so) { }

        public void OnStop(PowerupSO so)
        {
            GlobalPowerupState.MagnetActive = false;
            if (_runtime != null) _runtime.enabled = false;
        }
    }

    public class MagnetRuntime : MonoBehaviour
    {
        public float scanInterval = 0.5f;
        public float magnetRadius = 8f;
        public float pullSpeed = 8f;
        public float pullAccel = 20f;

        private float _nextScan;

        private static readonly string[] TagCandidates = new string[] { "Food", "Apple", "Collectible" };
        private static readonly int CollectibleLayer = 0; // optional

        private List<Magnetable> _magList = new List<Magnetable>();

        private void OnEnable()
        {
            _nextScan = 0f;
        }

        private void Update()
        {
            if (!GlobalPowerupState.MagnetActive) return;

            if (Time.unscaledTime >= _nextScan)
            {
                ScanSceneForMagnetables();
                _nextScan = Time.unscaledTime + scanInterval;
            }

            Transform[] targets = SnakeLocator.GetSnakeSegmentTargets();
            if (targets == null || targets.Length == 0) return;

            for (int i = 0; i < _magList.Count; i++)
            {
                Magnetable m = _magList[i];
                if (m == null) continue;
                m.TickMagnet(targets, magnetRadius, pullSpeed, pullAccel);
            }
        }

                private void ScanSceneForMagnetables()
        {
            GameObject[] all = GameObject.FindObjectsOfType<GameObject>();
            _magList.Clear();
            int collectibleLayer = LayerMask.NameToLayer("Collectible");

            for (int i = 0; i < all.Length; i++)
            {
                GameObject go = all[i];
                if (!go.activeInHierarchy) continue;

                bool tagged = false;
                for (int t = 0; t < TagCandidates.Length; t++)
                {
                    if (go.CompareTag(TagCandidates[t]))
                    {
                        tagged = true;
                        break;
                    }
                }

                bool layered = (collectibleLayer >= 0) && go.layer == collectibleLayer;

                string lname = go.name.ToLowerInvariant();
                bool named = lname.Contains("apple") || lname.Contains("food") || lname.Contains("fruit") || lname.Contains("pickup") || lname.Contains("collect");

                bool hasRenderer = (go.GetComponent<Renderer>() != null);
                bool hasWsCanvas = false;
                if (!hasRenderer)
                {
                    Canvas cv = go.GetComponent<Canvas>();
                    if (cv != null && cv.renderMode == RenderMode.WorldSpace) hasWsCanvas = true;
                }

                bool candidate = tagged || layered || (named && (hasRenderer || hasWsCanvas));
                if (!candidate) continue;

                Magnetable mag = go.GetComponent<Magnetable>();
                if (mag == null) mag = go.AddComponent<Magnetable>();
                EnsureLight2DCollider(go, mag);
                _magList.Add(mag);
            }
        }


                private void EnsureLight2DCollider(GameObject go, Magnetable mag)
        {
            Collider2D[] all = go.GetComponents<Collider2D>();
            bool hasTrigger = false;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].isTrigger) { hasTrigger = true; break; }
            }
            if (!hasTrigger)
            {
                CircleCollider2D circle = go.AddComponent<CircleCollider2D>();
                circle.isTrigger = true;
                circle.radius = 0.35f;
            }

            Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = go.AddComponent<Rigidbody2D>();
                rb.isKinematic = true;
                rb.gravityScale = 0f;
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            }
            Collider2D main = null;
            Collider2D[] all2 = go.GetComponents<Collider2D>();
            if (all2 != null && all2.Length > 0) main = all2[0];
            mag.Bind(main, rb);
        }

    }
}
