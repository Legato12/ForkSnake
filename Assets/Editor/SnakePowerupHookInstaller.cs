#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;

public static class SnakePowerupHookInstaller
{
    [MenuItem("Tools/Powerups/Apply Ghost Hook (Self-Collision Bypass)", false, 1)]
    public static void ApplyHook()
    {
        string[] guids = AssetDatabase.FindAssets("t:Script SnakeController");
        if (guids == null || guids.Length == 0) { Debug.LogWarning("Ghost Hook: SnakeController.cs not found."); return; }
        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        string text = File.ReadAllText(path);

        if (text.Contains("SnakePowerupBridge.GhostActive")) { Debug.Log("Ghost Hook: already patched."); return; }

        bool changed = false;

        string pat1 = @"if\s*\(\s*occupied\s*\.\s*Contains\s*\(\s*next\s*\)\s*\)";
        string rep1 = "if (occupied.Contains(next) && !SnakePowerupBridge.GhostActive)";
        if (Regex.IsMatch(text, pat1)) { text = Regex.Replace(text, pat1, rep1); changed = true; }

        string pat2 = @"if\s*\(\s*IsSelfCollision\s*\(\s*next\s*\)\s*\)";
        string rep2 = "if (IsSelfCollision(next) && !SnakePowerupBridge.GhostActive)";
        if (Regex.IsMatch(text, pat2)) { text = Regex.Replace(text, pat2, rep2); changed = true; }

        if (!changed)
        {
            string pat3 = @"if\s*\(\s*(occupied|cells)\s*\.\s*Contains\s*\(\s*next\s*\)\s*\)";
            string rep3 = "if ($1.Contains(next) && !SnakePowerupBridge.GhostActive)";
            if (Regex.IsMatch(text, pat3)) { text = Regex.Replace(text, pat3, rep3); changed = true; }
        }

        if (!changed) { Debug.LogWarning("Ghost Hook: couldn't find collision check. Patch manually: add '&& !SnakePowerupBridge.GhostActive' to the self-collision if()."); return; }

        if (!File.Exists("Assets/Scripts/SnakePowerupBridge.cs"))
        {
            File.WriteAllText("Assets/Scripts/SnakePowerupBridge.cs", "public static class SnakePowerupBridge{public static bool GhostActive=false;public static bool ShieldActive=false;public static float FreezeMultiplier=1f;public static bool MagnetActive=false;}");
            AssetDatabase.ImportAsset("Assets/Scripts/SnakePowerupBridge.cs");
        }

        File.WriteAllText(path, text);
        AssetDatabase.ImportAsset(path);
        Debug.Log("Ghost Hook: patched -> " + path);
    }
}
#endif
