
#if UNITY_EDITOR && SNAKE_MENUS
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.IO;

public class SnakePowerupSpawnerTuner : EditorWindow
{
    PowerupSpawnScheduler scheduler;
    Object spawnerObj;
    SerializedObject serSch;

    [MenuItem("Tools/Snake/Spawner Tuner")]
    public static void Open()
    {
        GetWindow<SnakePowerupSpawnerTuner>("Spawner Tuner");
    }

    void OnEnable()
    {
        scheduler = FindObjectOfType<PowerupSpawnScheduler>();
        if (!scheduler)
        {
            var spawn = FindObjectsOfType<MonoBehaviour>(true).FirstOrDefault(m => m.GetType().Name=="PowerupSpawner");
            if (spawn) scheduler = (spawn as Component).gameObject.AddComponent<PowerupSpawnScheduler>();
        }
        if (scheduler) { serSch = new SerializedObject(scheduler); spawnerObj = scheduler.spawnerObject; }
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Spawner", EditorStyles.boldLabel);
        spawnerObj = EditorGUILayout.ObjectField("PowerupSpawner", spawnerObj, typeof(Object), true);
        if (scheduler) scheduler.spawnerObject = spawnerObj;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Timing", EditorStyles.boldLabel);
        if (scheduler)
        {
            serSch.Update();
            EditorGUILayout.PropertyField(serSch.FindProperty("minInterval"));
            EditorGUILayout.PropertyField(serSch.FindProperty("maxInterval"));
            EditorGUILayout.PropertyField(serSch.FindProperty("maxOnBoard"));
            EditorGUILayout.PropertyField(serSch.FindProperty("spawnAtStart"));
            serSch.ApplyModifiedProperties();
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Spawn Now")) scheduler?.SpawnNow();
        if (GUILayout.Button("Clear Pickups")) scheduler?.ClearPickups();
        if (GUILayout.Button("Auto-Fill Spawner Lists")) AutoFillSpawner();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Auto-Fill searches:\n• PowerupSO in Assets/Data/Powerups (or name 'PU_*')\n• Prefabs in Assets/Powerups/Prefabs\nOrder is matched by name.", MessageType.Info);
    }

    void AutoFillSpawner()
    {
        var spawn = (spawnerObj as Component) ?? (spawnerObj as GameObject)?.GetComponent<MonoBehaviour>();
        if (!spawn) spawn = FindObjectsOfType<MonoBehaviour>(true).FirstOrDefault(m => m.GetType().Name=="PowerupSpawner");
        if (!spawn) { Debug.LogWarning("PowerupSpawner not found."); return; }

        var so = new SerializedObject(spawn);
        SerializedProperty defs = null, prefs = null;
        var it = so.GetIterator(); bool enter = true;
        while (it.NextVisible(enter))
        {
            enter = false;
            if (it.isArray && it.propertyType == SerializedPropertyType.ArraySize)
            {
                var path = it.propertyPath.ToLower();
                if (path.Contains("def")) defs = so.FindProperty(it.propertyPath);
                if (path.Contains("prefab")) prefs = so.FindProperty(it.propertyPath);
            }
        }

        var defsGuids = AssetDatabase.FindAssets("t:ScriptableObject PU_ Data/Powerups");
        if (defsGuids == null || defsGuids.Length == 0) defsGuids = AssetDatabase.FindAssets("PU_ t:ScriptableObject");
        var defPaths = defsGuids.Select(AssetDatabase.GUIDToAssetPath).ToArray();
        var defsAssets = defPaths.Select(p => AssetDatabase.LoadAssetAtPath<ScriptableObject>(p)).Where(a=>a).ToArray();

        var pfGuids = AssetDatabase.FindAssets("t:Prefab Powerups/Prefabs");
        if (pfGuids == null || pfGuids.Length == 0) pfGuids = AssetDatabase.FindAssets("PU_ t:Prefab");
        var pfPaths = pfGuids.Select(AssetDatabase.GUIDToAssetPath).ToArray();
        var pfAssets = pfPaths.Select(p => AssetDatabase.LoadAssetAtPath<GameObject>(p)).Where(a=>a).ToArray();

        // order by name to match
        defsAssets = defsAssets.OrderBy(a=>a.name).ToArray();
        pfAssets   = pfAssets.OrderBy(a=>a.name).ToArray();

        int n = Mathf.Min(defsAssets.Length, pfAssets.Length);
        if (n == 0) { Debug.LogWarning("No matching PowerupSO/Prefabs found. Create assets first."); return; }

        if (defs != null) { defs.arraySize = n; for (int i=0;i<n;i++) defs.GetArrayElementAtIndex(i).objectReferenceValue = defsAssets[i]; }
        if (prefs != null) { prefs.arraySize = n; for (int i=0;i<n;i++) prefs.GetArrayElementAtIndex(i).objectReferenceValue = pfAssets[i].transform; }
        so.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log($"Filled PowerupSpawner lists with {n} items.");
    }
}
#endif
