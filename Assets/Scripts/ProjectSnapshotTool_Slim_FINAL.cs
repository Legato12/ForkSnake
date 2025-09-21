#if UNITY_EDITOR
// Unity 2020.3+
// Tools → Project Snapshot: Create Snapshot (ZIP) + One-Click: Clean & Auto-Wire
// FINAL: uses System.IO.Compression.ZipArchive with fully-qualified types (no ambiguity with UnityEngine.CompressionLevel).
// Put this file at: Assets/Editor/ProjectSnapshotTool_Slim.cs
using UnityEditor;
using UnityEngine;
using System.IO;

public static class ProjectSnapshotTool_Slim
{
    private const string ZipName = "ProjectSnapshot_{0}.zip";

    [MenuItem("Tools/Project Snapshot/Create Snapshot (ZIP)", false, 100)]
    public static void CreateSnapshotZip()
    {
        string ts = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string zipName = string.Format(ZipName, ts);
        string targetPath = EditorUtility.SaveFilePanel("Save Project Snapshot", "", zipName, "zip");
        if (string.IsNullOrEmpty(targetPath)) return;

        // Prepare temp dir
        string tempDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/psnap"));
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        Directory.CreateDirectory(tempDir);

        // Copy Scripts and Scenes (if present)
        SafeCopyDir(Path.Combine(Application.dataPath, "Scripts"), Path.Combine(tempDir, "Scripts"));
        SafeCopyDir(Path.Combine(Application.dataPath, "Scenes"), Path.Combine(tempDir, "Scenes"));

        // Zip
        if (File.Exists(targetPath)) File.Delete(targetPath);
        CreateZipFromDirectory(tempDir, targetPath, System.IO.Compression.CompressionLevel.Optimal);
        EditorUtility.RevealInFinder(targetPath);
    }

    private static void SafeCopyDir(string src, string dst)
    {
        if (!Directory.Exists(src)) return;
        Directory.CreateDirectory(dst);
        var files = Directory.GetFiles(src, "*", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++)
        {
            string f = files[i];
            string rel = f.Substring(src.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string outPath = Path.Combine(dst, rel);
            string outDir = Path.GetDirectoryName(outPath);
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
            File.Copy(f, outPath, true);
        }
    }

    /// <summary>Zip folder via ZipArchive (no dependency on FileSystem assembly).</summary>
    private static void CreateZipFromDirectory(string sourceDir, string zipPath, System.IO.Compression.CompressionLevel level)
    {
        string basePath = sourceDir.Replace('\\', '/').TrimEnd('/');
        using (var fs = new FileStream(zipPath, FileMode.Create))
        using (var zip = new System.IO.Compression.ZipArchive(fs, System.IO.Compression.ZipArchiveMode.Create))
        {
            var files = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string filePath = files[i];
                string normFile = filePath.Replace('\\', '/');
                string rel = normFile.Substring(basePath.Length + 1);
                var entry = zip.CreateEntry(rel, level);
                using (var entryStream = entry.Open())
                using (var fileStream = File.OpenRead(filePath))
                {
                    fileStream.CopyTo(entryStream);
                }
            }
        }
    }

    [MenuItem("Tools/Project Snapshot/One-Click: Clean & Auto-Wire", false, 0)]
    public static void CleanAndWire()
    {
        // 1) Remove legacy helper scripts (safe list)
        string[] toDelete = new string[] {
            "Assets/Scripts/OneTime_PowerupsSetup.cs",
            "Assets/Scripts/OneClick_Powerups_Create.cs",
            "Assets/Scripts/ProjectSnapshot_AutoFix.cs",
            "Assets/Scripts/README_Powerups_Pulse.txt",
            "Assets/Scripts/README_PowerupSpawnScheduler.txt",
            "Assets/Scripts/README_Scripts_fixed_v6.txt"
        };
        for (int i = 0; i < toDelete.Length; i++)
        {
            if (File.Exists(toDelete[i])) { AssetDatabase.DeleteAsset(toDelete[i]); }
        }

        // 2) Remove auto-created clutter in scene (duplicate canvases/bars)
        var bars = GameObject.FindObjectsOfType<RectTransform>(true);
        for (int i = 0; i < bars.Length; i++)
        {
            if (bars[i] == null) continue;
            string n = bars[i].name;
            if (n == "PowerupCanvas_AUTO" || n == "PowerupBar_AUTO" || n == "PowerupSlotUI_RUNTIME")
            {
                Object.DestroyImmediate(bars[i].gameObject);
            }
        }

        // 3) Ensure one PowerupStash connected to Quickbar if exists; otherwise create minimal
        PowerupStash[] stashes = Object.FindObjectsOfType<PowerupStash>(true);
        PowerupStash keep = null;
        if (stashes != null && stashes.Length > 0)
        {
            for (int i = 0; i < stashes.Length; i++)
            {
                var s = stashes[i];
                if (s == null) continue;
                var so = new UnityEditor.SerializedObject(s);
                var spParent = so.FindProperty("slotsParent");
                Transform tr = spParent != null ? (Transform)spParent.objectReferenceValue : null;
                if (tr != null && tr.childCount > 0) { keep = s; break; }
                if (keep == null) keep = s;
            }
            for (int i = 0; i < stashes.Length; i++) if (stashes[i] != keep) Object.DestroyImmediate(stashes[i].gameObject);
        }
        else
        {
            var go = new GameObject("PowerupStash"); keep = go.AddComponent<PowerupStash>();
        }

        // 4) Ensure SnakePowerupRuntime & tint/HUD exist on snake
        var snakes = Object.FindObjectsOfType<MonoBehaviour>(true);
        MonoBehaviour snake = null;
        for (int i = 0; i < snakes.Length; i++)
        {
            if (snakes[i] != null && snakes[i].GetType().Name == "SnakeController") { snake = snakes[i]; break; }
        }
        if (snake != null)
        {
            if (snake.GetComponent<SnakePowerupRuntime>() == null) snake.gameObject.AddComponent<SnakePowerupRuntime>();
        }
        else
        {
            Debug.LogWarning("One-Click: SnakeController not found in scene. Effects will still work once snake is present.");
        }

        // 5) Ensure PowerupTopHUDMulti and PowerupScreenTintMixer exist
        if (Object.FindObjectOfType<PowerupTopHUDMulti>(true) == null)
        {
            var go = new GameObject("PowerupTopHUDMulti"); go.AddComponent<PowerupTopHUDMulti>();
        }
        if (Object.FindObjectOfType<PowerupScreenTintMixer>(true) == null)
        {
            var go = new GameObject("PowerupScreenTintMixer"); go.AddComponent<PowerupScreenTintMixer>();
        }

        // 6) Ensure spawner exists and table filled from assets
        PowerupSpawner spawner = Object.FindObjectOfType<PowerupSpawner>(true);
        if (spawner == null)
        {
            var go = new GameObject("PowerupSpawner"); spawner = go.AddComponent<PowerupSpawner>();
        }
        var soSpawner = new UnityEditor.SerializedObject(spawner);
        var pTable = soSpawner.FindProperty("table");
        if (pTable != null)
        {
            string[] guids = AssetDatabase.FindAssets("t:PowerupSO");
            pTable.arraySize = guids.Length;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                pTable.GetArrayElementAtIndex(i).objectReferenceValue = asset;
            }
            soSpawner.ApplyModifiedPropertiesWithoutUndo();
        }

        AssetDatabase.SaveAssets();
        Debug.Log("One-Click: Clean & Auto-Wire complete.");
    }
}
#endif
