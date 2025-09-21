#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace SnakeGame.Powerups.EditorTools
{
    public static class PowerupsMenu
    {
        [MenuItem("Tools/Powerups/Clean Legacy", priority = 1)]
        public static void CleanLegacy()
        {
            int removed = 0;
            string[] legacyNames = new string[] {
                "LegacyPowerup", "OldMagnet", "OldFreeze", "OldGhost", "OldShield",
                "MagnetCollectorOld", "PowerupControllerLegacy"
            };

            foreach (var go in GameObject.FindObjectsOfType<GameObject>())
            {
                var comps = go.GetComponents<Component>();
                foreach (var c in comps)
                {
                    if (c == null) continue;
                    string n = c.GetType().Name;
                    for (int i = 0; i < legacyNames.Length; i++)
                    {
                        if (n == legacyNames[i])
                        {
                            Object.DestroyImmediate(c, true);
                            removed++;
                            break;
                        }
                    }
                }
            }
            Debug.Log("Clean Legacy: removed components = " + removed);
        }

        [MenuItem("Tools/Powerups/Apply Ghost Hook", priority = 2)]
        public static void ApplyGhostHook()
        {
            // Minimal hook: ensure a global bypass component exists in scene; user wires their self-collision check to it.
            var hook = Object.FindObjectOfType<GhostHookMarker>();
            if (hook == null)
            {
                var go = new GameObject("GhostHookMarker");
                go.AddComponent<GhostHookMarker>();
                Debug.Log("Created GhostHookMarker. In SnakeController self-collision checks, gate with: if (!GhostHookMarker.ShouldBlockSelfCollision()) { /* death */ }");
            }
            else
            {
                Debug.Log("GhostHookMarker already present.");
            }
        }
    }

    public class GhostHookMarker : MonoBehaviour
    {
        public static bool ShouldBlockSelfCollision()
        {
            // When ghost is active, we do NOT want to die on tail
            return !SnakeGame.Powerups.GlobalPowerupState.GhostActive;
        }
    }
}
#endif
