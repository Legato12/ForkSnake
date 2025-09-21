
#if UNITY_EDITOR && SNAKE_MENUS
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Text;
using System.Linq;
using System.IO;
using System.Collections.Generic;

/// Deep snapshot: enumerates all scenes (Build Settings + all .unity under Assets),
/// full hierarchy, components, plus a complete script list. Writes multiple files
/// under Assets/_Snapshot to keep things readable.
public static class SnakeProjectSnapshotV3
{
    [MenuItem("Tools/Snake/Export Project Snapshot V3 (Deep)")]
    public static void Export()
    {
        const string outFolder = "Assets/_Snapshot";
        if (!AssetDatabase.IsValidFolder(outFolder)) AssetDatabase.CreateFolder("Assets", "_Snapshot");

        string mdSummary = Path.Combine(outFolder, "SNAKE_SNAPSHOT_V3.md");
        string scriptsMd = Path.Combine(outFolder, "SNAKE_SCRIPTS_LIST.md");
        string assetsMd  = Path.Combine(outFolder, "SNAKE_ASSETS_SUMMARY.md");

        var sb = new StringBuilder();
        sb.AppendLine("# Snake Project Snapshot V3");
        sb.AppendLine();

        // --- Gather scenes
        var sceneGuids = new HashSet<string>(AssetDatabase.FindAssets("t:Scene"));
        var scenePaths = sceneGuids.Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p).ToList();

        string opened = SceneManager.GetActiveScene().path;

        int sceneIndex = 0;
        foreach (var scenePath in scenePaths)
        {
            var s = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            string file = Path.Combine(outFolder, $"{s.name}_Hierarchy.md");
            File.WriteAllText(file, DumpSceneHierarchy(s));
            AssetDatabase.ImportAsset(file);

            sb.AppendLine($"## Scene: **{s.name}**");
            sb.AppendLine($"- Path: `{scenePath}`");
            sb.AppendLine($"- Root count: {s.rootCount}");
            sb.AppendLine($"- Hierarchy: `{Path.GetFileName(file)}`");
            AppendSceneSummary(sb, s);
            sb.AppendLine();
            sceneIndex++;
        }

        // reopen previous scene if any
        if (!string.IsNullOrEmpty(opened) && File.Exists(opened))
            EditorSceneManager.OpenScene(opened, OpenSceneMode.Single);

        File.WriteAllText(mdSummary, sb.ToString());
        AssetDatabase.ImportAsset(mdSummary);

        // --- Scripts list
        File.WriteAllText(scriptsMd, DumpScriptsList());
        AssetDatabase.ImportAsset(scriptsMd);

        // --- Assets summary
        File.WriteAllText(assetsMd, DumpAssetsSummary());
        AssetDatabase.ImportAsset(assetsMd);

        Debug.Log("Snapshot V3 exported to: " + outFolder);
    }

    private static string DumpSceneHierarchy(Scene s)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Hierarchy — {s.name}");
        foreach (var go in s.GetRootGameObjects())
            DumpGO(sb, go.transform, 0);
        return sb.ToString();
    }

    private static void DumpGO(StringBuilder sb, Transform t, int depth)
    {
        string indent = new string(' ', depth * 2);
        var comps = t.GetComponents<Component>().Select(c => c ? c.GetType().Name : "(Missing)").ToArray();
        sb.AppendLine($"  - {t.name}: {string.Join(", ", comps)}");
        for (int i = 0; i < t.childCount; i++)
            DumpGO(sb, t.GetChild(i), depth + 1);
    }

    private static void AppendSceneSummary(StringBuilder sb, Scene s)
    {
        // Board
        var bd = Object.FindObjectsOfType<MonoBehaviour>(true).FirstOrDefault(m => m.GetType().Name=="Board");
        if (bd)
        {
            var so = new SerializedObject(bd);
            int bx = so.FindProperty("borderX")?.intValue ?? 0;
            int by = so.FindProperty("borderY")?.intValue ?? 0;
            float tile = so.FindProperty("tileWorldSize")?.floatValue ?? 1f;
            bool wrap = so.FindProperty("wrap")?.boolValue ?? false;
            sb.AppendLine($"- Board: borderX={bx}, borderY={by}, tile={tile:0.###}, wrap={wrap}");
        }

        // Camera
        var cam = Camera.main ?? Object.FindObjectOfType<Camera>();
        if (cam) sb.AppendLine($"- Camera: OrthoSize={cam.orthographicSize:0.###}, Viewport={cam.rect.xMin:0.###},{cam.rect.yMin:0.###},{cam.rect.width:0.###},{cam.rect.height:0.###}");

        // Spawners
        var apple = Object.FindObjectsOfType<MonoBehaviour>(true).FirstOrDefault(m => m.GetType().Name=="AppleSpawner");
        if (apple)
        {
            var so = new SerializedObject(apple);
            var gold = so.FindProperty("goldApplePrefab");
            sb.AppendLine($"- AppleSpawner: {(gold!=null && gold.objectReferenceValue ? "gold prefab set" : "no gold prefab")}");
        }

        var power = Object.FindObjectsOfType<MonoBehaviour>(true).FirstOrDefault(m => m.GetType().Name=="PowerupSpawner");
        if (power)
        {
            var so = new SerializedObject(power);
            int defs = CountArray(so, "def");
            int pref = CountArray(so, "prefab");
            sb.AppendLine($"- PowerupSpawner: defs={defs}, prefabs={pref}");
        }
    }

    private static int CountArray(SerializedObject so, string nameHint)
    {
        int count = 0;
        var it = so.GetIterator();
        bool enter = true;
        while (it.NextVisible(enter))
        {
            enter = false;
            if (it.isArray && it.propertyType == SerializedPropertyType.ArraySize && it.displayName.ToLower().Contains("size"))
            {
                if (it.propertyPath.ToLower().Contains(nameHint)) { count = it.intValue; break; }
            }
        }
        return count;
    }

    private static string DumpScriptsList()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Scripts in Project");
        var guids = AssetDatabase.FindAssets("t:MonoScript");
        foreach (var g in guids.OrderBy(x => AssetDatabase.GUIDToAssetPath(x)))
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            var cls = ms != null ? (ms.GetClass() != null ? ms.GetClass().FullName : "(no class)") : "(missing)";
            sb.AppendLine($"- {path}  —  {cls}");
        }
        return sb.ToString();
    }

    private static string DumpAssetsSummary()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Assets Summary");
        sb.AppendLine("## Prefabs");
        foreach (var p in AssetDatabase.FindAssets("t:Prefab").OrderBy(x => AssetDatabase.GUIDToAssetPath(x)))
        {
            string path = AssetDatabase.GUIDToAssetPath(p);
            sb.AppendLine($"- {path}");
        }
        sb.AppendLine();
        sb.AppendLine("## ScriptableObjects");
        foreach (var p in AssetDatabase.FindAssets("t:ScriptableObject").OrderBy(x => AssetDatabase.GUIDToAssetPath(x)))
        {
            string path = AssetDatabase.GUIDToAssetPath(p);
            var obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            string type = obj ? obj.GetType().FullName : "(missing)";
            sb.AppendLine($"- {path}  —  {type}");
        }
        return sb.ToString();
    }
}
#endif
