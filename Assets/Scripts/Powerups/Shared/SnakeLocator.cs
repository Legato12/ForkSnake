using UnityEngine;
using System;
using System.Collections.Generic;

namespace SnakeGame.Powerups
{
    /// <summary>
    /// Attempts to locate the snake root and segment targets without requiring code changes.
    /// </summary>
    public static class SnakeLocator
    {
        private static GameObject _cachedRoot;
        private static Transform[] _cachedTargets;
        private static float _nextRefresh;

        public static GameObject TryFindSnakeRoot()
        {
            if (_cachedRoot != null && _cachedRoot.activeInHierarchy) return _cachedRoot;

            // Priority: tagged Player
            GameObject root = GameObject.FindGameObjectWithTag("Player");

            // Next: component named 'SnakeController' anywhere
            if (root == null)
            {
                Type t = Type.GetType("SnakeController");
                if (t == null)
                {
                    // Try common fully-qualified names if user has namespace
                    t = Type.GetType("Snake.SnakeController");
                    if (t == null) t = Type.GetType("Game.SnakeController");
                }
                if (t != null)
                {
                    Component comp = UnityEngine.Object.FindObjectOfType(t) as Component;
                    if (comp != null) root = comp.gameObject;
                }
            }

            // Fallback: first GO with name containing 'snake'
            if (root == null)
            {
                GameObject[] all = UnityEngine.Object.FindObjectsOfType<GameObject>();
                for (int i = 0; i < all.Length; i++)
                {
                    GameObject go = all[i];
                    if (go.name.ToLowerInvariant().Contains("snake"))
                    {
                        root = go;
                        break;
                    }
                }
            }

            _cachedRoot = root;
            _cachedTargets = null;
            _nextRefresh = 0f;
            return _cachedRoot;
        }

        public static Transform[] GetSnakeSegmentTargets()
        {
            if (Time.unscaledTime < _nextRefresh && _cachedTargets != null) return _cachedTargets;

            GameObject root = TryFindSnakeRoot();
            if (root == null) return new Transform[0];

            List<Transform> list = new List<Transform>();
            // Prefer colliders on children as targets
            Collider2D[] cols = root.GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i] == null) continue;
                list.Add(cols[i].transform);
            }
            if (list.Count == 0)
            {
                // fallback to any child transforms
                Transform[] all = root.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < all.Length; i++) list.Add(all[i]);
            }
            _cachedTargets = list.ToArray();
            _nextRefresh = Time.unscaledTime + 0.5f;
            return _cachedTargets;
        }
    }
}
