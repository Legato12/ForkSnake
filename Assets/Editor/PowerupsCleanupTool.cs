#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public static class PowerupsCleanupTool
{
    [MenuItem("Tools/Powerups/Clean Legacy Scripts & Scene Clutter", false, 0)]
    public static void Clean()
    {
        // Safe list of assets to delete if present
        string[] maybe = new string[]{
            "Assets/Scripts/OneTime_PowerupsSetup.cs",
            "Assets/Scripts/OneClick_Powerups_Create.cs",
            "Assets/Scripts/ProjectSnapshot_AutoFix.cs",
            "Assets/Scripts/README_Powerups_Pulse.txt",
            "Assets/Scripts/README_PowerupSpawnScheduler.txt",
            "Assets/Scripts/README_Scripts_fixed_v6.txt",
            "Assets/Editor/ProjectSnapshotTool_Slim_FIXED.cs",
            "Assets/Editor/ProjectSnapshotTool_Slim_FIXED_v2.cs",
            "Assets/Scripts/PowerupSpawnScheduler.cs"
        };
        for (int i=0;i<maybe.Length;i++)
        {
            if (File.Exists(maybe[i]))
            {
                Debug.Log("Cleanup: delete " + maybe[i]);
                AssetDatabase.DeleteAsset(maybe[i]);
            }
        }

        // Remove duplicate runtime-created UI objects if they linger
        var all = GameObject.FindObjectsOfType<RectTransform>(true);
        for (int i=0;i<all.Length;i++)
        {
            var rt = all[i]; if (rt == null) continue;
            string n = rt.name;
            if (n == "PowerupCanvas_AUTO" || n == "PowerupBar_AUTO" || n == "PowerupSlotUI_RUNTIME")
            {
                Object.DestroyImmediate(rt.gameObject);
                Debug.Log("Cleanup: removed scene auto object " + n);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Cleanup complete.");
    }
}
#endif
