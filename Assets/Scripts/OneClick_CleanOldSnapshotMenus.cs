
#if UNITY_EDITOR && SNAKE_MENUS
using UnityEditor;
using UnityEngine;

public class OneClick_CleanOldSnapshotMenus : MonoBehaviour
{
    [MenuItem("Tools/Snake/Clean Old Snapshot Menu Items (One‑Click)")]
    public static void RunFromMenu()
    {
        DeleteIfExists("SnakeProjectSnapshot");   // old single‑scene
        DeleteIfExists("SnakeProjectSnapshotV2"); // all scenes in build
        // Keep V3
        AssetDatabase.Refresh();
        Debug.Log("Cleaned old snapshot menu providers. Only V3 remains.");
    }

    static void DeleteIfExists(string className)
    {
        string[] guids = AssetDatabase.FindAssets("t:MonoScript " + className);
        foreach (var g in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(p);
            if (ms != null && ms.GetClass() != null && ms.GetClass().Name == className)
            {
                AssetDatabase.DeleteAsset(p);
            }
        }
    }
}
#endif
