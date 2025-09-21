
#if UNITY_EDITOR && SNAKE_MENUS
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.IO;

/// Creates 4 PowerupSOs (Freeze, Magnet, Shield, Ghost), generates simple sprites, prefabs,
/// wires PowerupSpawner lists, then self-deletes.
public class OneClick_Powerups_CreateAndWire : MonoBehaviour
{
    [MenuItem("Tools/Snake/Powerups/Create 4 Powerups + Wire Spawner (One‑Click)")]
    public static void RunFromMenu()
    {
        var go = new GameObject("PowerupsCreateWireInstallerTemp");
        var i = go.AddComponent<OneClick_Powerups_CreateAndWire>();
        i.Run();
        Object.DestroyImmediate(go);
    }

    [ContextMenu("Run & Self‑delete")]
    public void Run(){ try { DoRun(); } finally { SelfDelete(); } }

    private void DoRun()
    {
        // folders
        const string dataFolder = "Assets/Data/Powerups";
        const string sprFolder  = "Assets/Powerups/Sprites";
        const string pfFolder   = "Assets/Powerups/Prefabs";
        if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder(dataFolder)) AssetDatabase.CreateFolder("Assets/Data", "Powerups");
        if (!AssetDatabase.IsValidFolder("Assets/Powerups")) AssetDatabase.CreateFolder("Assets", "Powerups");
        if (!AssetDatabase.IsValidFolder(sprFolder)) AssetDatabase.CreateFolder("Assets/Powerups", "Sprites");
        if (!AssetDatabase.IsValidFolder(pfFolder)) AssetDatabase.CreateFolder("Assets/Powerups", "Prefabs");

        // powerup definitions
        var defs = new (string id, string file, float dur, Color col)[] {
            ("freeze","PU_Freeze", 5f, new Color(0.55f,0.75f,1f,1f)),
            ("magnet","PU_Magnet", 6f, new Color(1f,0.55f,0.1f,1f)),
            ("shield","PU_Shield", 8f, new Color(0.5f,1f,0.55f,1f)),
            ("ghost","PU_Ghost",  6f, new Color(0.55f,1f,1f,1f)),
        };

        var soAssets = new Object[defs.Length];
        var pfAssets = new Object[defs.Length];

        for (int i=0;i<defs.Length;i++)
        {
            // create SO
            var so = ScriptableObject.CreateInstance("PowerupSO");
            string soPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(dataFolder, defs[i].file + ".asset"));
            AssetDatabase.CreateAsset(so, soPath);
            var ser = new SerializedObject(so);
            SetStringIfExists(ser, "id", defs[i].id);
            SetFloatIfExists(ser, "duration", defs[i].dur);
            ser.ApplyModifiedPropertiesWithoutUndo();
            soAssets[i] = AssetDatabase.LoadAssetAtPath<Object>(soPath);

            // create sprite png
            string pngPath = Path.Combine(sprFolder, defs[i].file + ".png").Replace("\\","/");
            if (!File.Exists(pngPath))
            {
                var tex = new Texture2D(32,32, TextureFormat.RGBA32, false);
                var col = defs[i].col;
                var arr = tex.GetPixels32();
                for (int p=0;p<arr.Length;p++) arr[p] = new Color(col.r, col.g, col.b, 1f);
                tex.SetPixels32(arr); tex.Apply();
                var bytes = tex.EncodeToPNG();
                File.WriteAllBytes(pngPath, bytes);
                AssetDatabase.ImportAsset(pngPath);
                var ti = (TextureImporter)AssetImporter.GetAtPath(pngPath);
                ti.textureType = TextureImporterType.Sprite;
                ti.SaveAndReimport();
            }
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);

            // create prefab
            var go = new GameObject(defs[i].file);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 12;
            string pfPath = Path.Combine(pfFolder, defs[i].file + ".prefab").Replace("\\","/");
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, pfPath);
            Object.DestroyImmediate(go);
            pfAssets[i] = AssetDatabase.LoadAssetAtPath<Object>(pfPath);

            // assign icon to SO if it has a field named icon/sprite
            ser = new SerializedObject(so);
            SetObjectIfExists(ser, "icon", sprite);
            SetObjectIfExists(ser, "sprite", sprite);
            ser.ApplyModifiedPropertiesWithoutUndo();
        }

        // wire to PowerupSpawner
        var spawner = Object.FindObjectsOfType<MonoBehaviour>(true).FirstOrDefault(m => m.GetType().Name == "PowerupSpawner");
        if (spawner == null) { Debug.LogWarning("PowerupSpawner not found in scene."); return; }

        var spSO = new SerializedObject(spawner);
        SerializedProperty defsProp = null, prefProp = null;

        // heuristically find two arrays: defs and prefabs
        var it = spSO.GetIterator(); bool enter = true;
        while (it.NextVisible(enter))
        {
            enter = false;
            if (it.isArray && it.propertyType == SerializedPropertyType.ArraySize && it.propertyPath.ToLower().Contains("def"))
                defsProp = spSO.FindProperty(it.propertyPath);
            if (it.isArray && it.propertyType == SerializedPropertyType.ArraySize && it.propertyPath.ToLower().Contains("prefab"))
                prefProp = spSO.FindProperty(it.propertyPath);
        }

        if (defsProp != null)
        {
            defsProp.arraySize = soAssets.Length;
            for (int i=0;i<soAssets.Length;i++)
                defsProp.GetArrayElementAtIndex(i).objectReferenceValue = soAssets[i];
        }

        if (prefProp != null)
        {
            prefProp.arraySize = pfAssets.Length;
            for (int i=0;i<pfAssets.Length;i++)
                prefProp.GetArrayElementAtIndex(i).objectReferenceValue = pfAssets[i];
        }

        spSO.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log("Created 4 powerups, generated sprites/prefabs, and wired PowerupSpawner.");
    }

    static void SetStringIfExists(SerializedObject so, string name, string value)
    {
        var p = so.FindProperty(name);
        if (p != null && p.propertyType == SerializedPropertyType.String) p.stringValue = value;
    }
    static void SetFloatIfExists(SerializedObject so, string name, float value)
    {
        var p = so.FindProperty(name);
        if (p != null && p.propertyType == SerializedPropertyType.Float) p.floatValue = value;
    }
    static void SetObjectIfExists(SerializedObject so, string name, Object value)
    {
        var p = so.FindProperty(name);
        if (p != null && p.propertyType == SerializedPropertyType.ObjectReference) p.objectReferenceValue = value;
    }

    private void SelfDelete()
    {
        var thisType = typeof(OneClick_Powerups_CreateAndWire);
        string[] guids = AssetDatabase.FindAssets("t:MonoScript OneClick_Powerups_CreateAndWire");
        foreach (var g in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(p);
            if (ms != null && ms.GetClass() == thisType)
            {
                AssetDatabase.DeleteAsset(p);
                AssetDatabase.Refresh();
                Debug.Log("Removed installer script: " + p);
                break;
            }
        }
    }
}
#endif
