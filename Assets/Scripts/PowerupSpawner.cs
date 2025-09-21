// Unity 2020.3 LTS compatible (no tuples, no target-typed new()).
// PowerupSpawner with built-in auto-spawn and pickup-item integration.
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PowerupSpawner : MonoBehaviour
{
    private static PowerupSpawner s_Instance;

    [Header("Definitions")]
    [SerializeField] private PowerupSO[] table = null;

    [Header("Auto-Spawn")]
    [SerializeField] private bool autoSpawn = true;
    [SerializeField] private float spawnIntervalMin = 6f;
    [SerializeField] private float spawnIntervalMax = 10f;
    [SerializeField] private int maxActive = 1;

    [Header("Spawn Area (grid)")]
    [SerializeField] private RectInt spawnArea = new RectInt(-8, -6, 16, 12);
    [SerializeField] private Vector3 worldOrigin = Vector3.zero;
    [SerializeField] private float cellSize = 1f;

    [Header("Avoid Overlaps (optional)")]
    [SerializeField] private LayerMask avoidMask = 0;
    [SerializeField] private float avoidProbeRadius = 0.4f;

    [Header("Pickup Prefab / Container")]
    [SerializeField] private GameObject pickupPrefab = null;
    [SerializeField] private Transform container = null;

    private readonly Dictionary<Vector2Int, ActiveEntry> active =
        new Dictionary<Vector2Int, ActiveEntry>();

    private struct ActiveEntry
    {
        public Vector2Int cell;
        public GameObject go;
        public PowerupSO def;
    }

    private float nextSpawnTimer = -1f;

    private void Awake()
    {
        if (s_Instance != null && s_Instance != this)
        {
            Debug.LogWarning("PowerupSpawner: duplicate instance found; disabling this one (" + name + ").", this);
            enabled = false;
            return;
        }
        s_Instance = this;
        if (container == null) container = transform;
        ResetSpawnTimer();
    }

    private void OnDestroy()
    {
        if (s_Instance == this) s_Instance = null;
    }

    private void Update()
    {
        if (!autoSpawn) return;
        if (table == null || table.Length == 0) return;
        if (maxActive > 0 && active.Count >= maxActive) return;

        nextSpawnTimer -= Time.deltaTime;
        if (nextSpawnTimer <= 0f)
        {
            Vector2Int cell;
            if (TryFindFreeCell(out cell))
            {
                PowerupSO def = PickRandomDef();
                if (def != null)
                    Spawn(cell, def);
            }
            ResetSpawnTimer();
        }
    }

    private void ResetSpawnTimer()
    {
        float min = (spawnIntervalMin <= 0f) ? 1f : spawnIntervalMin;
        float max = (spawnIntervalMax < min) ? min : spawnIntervalMax;
        nextSpawnTimer = Random.Range(min, max);
    }

    private PowerupSO PickRandomDef()
    {
        if (table == null || table.Length == 0) return null;
        int idx = Random.Range(0, table.Length);
        if (idx < 0 || idx >= table.Length) idx = 0;
        return table[idx];
    }

    private bool TryFindFreeCell(out Vector2Int cell)
    {
        for (int attempt = 0; attempt < 50; attempt++)
        {
            int x = Random.Range(spawnArea.xMin, spawnArea.xMax);
            int y = Random.Range(spawnArea.yMin, spawnArea.yMax);
            Vector2Int c = new Vector2Int(x, y);
            if (active.ContainsKey(c))
                continue;

            if (avoidMask != 0)
            {
                Vector3 w = CellToWorld(c);
                Collider2D hit = Physics2D.OverlapCircle(w, avoidProbeRadius, avoidMask);
                if (hit != null)
                    continue;
            }

            cell = c;
            return true;
        }
        cell = default(Vector2Int);
        return false;
    }

    // ---- Public API --------------------------------------------------------

    public bool TryConsumeAt(Vector2Int cell, out PowerupSO def)
    {
        ActiveEntry e;
        if (active.TryGetValue(cell, out e))
        {
            def = e.def;
            if (e.go != null)
            {
                Destroy(e.go);
            }
            active.Remove(cell);
            return true;
        }
        def = null;
        return false;
    }

    /// <summary>
    /// Called from PowerupPickupItem when the snake collides with a pickup.
    /// Removes the entry by its cell and destroys the GO.
    /// </summary>
    public bool ConsumeFromItem(PowerupPickupItem item, out PowerupSO def)
    {
        def = null;
        if (item == null) return false;
        ActiveEntry e;
        if (active.TryGetValue(item.cell, out e))
        {
            def = e.def;
            active.Remove(item.cell);
        }
        if (item.gameObject != null) Destroy(item.gameObject);
        return def != null;
    }

    public bool Spawn(Vector2Int cell, PowerupSO def)
    {
        if (def == null) return false;
        if (maxActive > 0 && active.Count >= maxActive) return false;
        if (active.ContainsKey(cell)) return false;

        EnsurePickupPrefab();

        GameObject go = GameObject.Instantiate(pickupPrefab, CellToWorld(cell), Quaternion.identity, container);

        // Attach runtime marker behaviour to handle trigger pickup
        PowerupPickupItem marker = go.GetComponent<PowerupPickupItem>();
        if (marker == null) marker = go.AddComponent<PowerupPickupItem>();
        marker.spawner = this;
        marker.def = def;
        marker.cell = cell;

        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sprite = def.sprite;

        ActiveEntry e = new ActiveEntry();
        e.cell = cell;
        e.go = go;
        e.def = def;
        active.Add(cell, e);

        return true;
    }

    public bool SpawnRandom()
    {
        Vector2Int cell;
        if (!TryFindFreeCell(out cell))
            return false;
        PowerupSO def = PickRandomDef();
        if (def == null) return false;
        return Spawn(cell, def);
    }

    public bool IsOccupied(Vector2Int cell)
    {
        return active.ContainsKey(cell);
    }

    public void ClearAll()
    {
        List<Vector2Int> keys = new List<Vector2Int>();
        foreach (KeyValuePair<Vector2Int, ActiveEntry> kv in active)
            keys.Add(kv.Key);

        for (int i = 0; i < keys.Count; i++)
        {
            ActiveEntry e;
            if (active.TryGetValue(keys[i], out e))
            {
                if (e.go != null) Destroy(e.go);
            }
            active.Remove(keys[i]);
        }
    }

    private void EnsurePickupPrefab()
    {
        if (pickupPrefab != null)
        {
            // ensure it has required components
            if (pickupPrefab.GetComponent<Collider2D>() == null)
            {
                var c = pickupPrefab.AddComponent<CircleCollider2D>();
                c.isTrigger = true;
                c.radius = 0.45f;
            }
            if (pickupPrefab.GetComponent<SpriteRenderer>() == null)
            {
                pickupPrefab.AddComponent<SpriteRenderer>();
            }
            if (pickupPrefab.GetComponent<PowerupPickupItem>() == null)
            {
                pickupPrefab.AddComponent<PowerupPickupItem>();
            }
            return;
        }

        GameObject proto = new GameObject("Powerup_Pickup_RUNTIME");
        proto.hideFlags = HideFlags.HideAndDontSave;

        SpriteRenderer sr = proto.AddComponent<SpriteRenderer>();

        CircleCollider2D c2d = proto.AddComponent<CircleCollider2D>();
        c2d.isTrigger = true;
        c2d.radius = 0.45f;

        proto.AddComponent<PowerupPickupItem>();

        pickupPrefab = proto;
    }

    private Vector3 CellToWorld(Vector2Int cell)
    {
        return worldOrigin + new Vector3(cell.x * cellSize, cell.y * cellSize, 0f);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (spawnIntervalMin < 0f) spawnIntervalMin = 0f;
        if (spawnIntervalMax < 0f) spawnIntervalMax = 0f;
        if (cellSize <= 0f) cellSize = 1f;
        if (maxActive < 0) maxActive = 0;
        if (spawnArea.width < 1) spawnArea.width = 1;
        if (spawnArea.height < 1) spawnArea.height = 1;
    }
#endif
}
