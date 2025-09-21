// Unity 2020.3 LTS compatible. No tuples.
// v21: tougher magnet + broader kind detection + fallback snap-collect + richer debug.
//      Ghost remains runtime + bridge; Freeze/Shield unchanged from v20.
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

[DisallowMultipleComponent]
public sealed class SnakePowerupRuntime : MonoBehaviour
{
    [Header("Optional wiring")]
    [SerializeField] private MonoBehaviour snake; // SnakeController (ищется сам)

    [Header("Magnet")]
    [SerializeField] private float magnetRadius = 7f;
    [SerializeField] private float magnetPullSpeed = 24f;
    [SerializeField] private float magnetCaptureDistance = 0.28f;
    [SerializeField] private bool snapCollectIfNotMovable = true; // если объект "не двигается" — просто коллекти его

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private PowerupTopHUDMulti hudMulti;
    private PowerupScreenTintMixer tintMixer;

    private readonly List<Collider2D> snakeCols = new List<Collider2D>();
    private readonly List<bool> snakeColsTriggerOrig = new List<bool>();

    private float freezeUntil, ghostUntil, shieldUntil, magnetUntil;
    private bool timeScaleOwned; private float prevTimeScale = 1f; private float prevFixedDelta = 0.02f;

    private bool fieldsScaled; private readonly List<FieldScale> scaled = new List<FieldScale>();
    private struct FieldScale { public Object target; public FieldInfo field; public PropertyInfo prop; public float original; }

    private PowerupSpawner powerupSpawner; private float spMin = -1f, spMax = -1f;
    private readonly List<BoolToggle> toggled = new List<BoolToggle>();
    private struct BoolToggle { public Object target; public FieldInfo field; public bool original; }

    private Transform snakeRoot;
    private readonly List<Transform> magnetCandidates = new List<Transform>(); private float nextRefreshAt;

    private void Log(string msg) { if (debugLogs) Debug.Log("[Powerups] " + msg); }

    private void Awake()
    {
        if (snake == null)
        {
            MonoBehaviour[] all = GetComponents<MonoBehaviour>();
            for (int i = 0; i < all.Length; i++)
            {
                MonoBehaviour mb = all[i];
                if (mb == null) continue;
                System.Type t = mb.GetType();
                if (t != null && t.Name == "SnakeController") { snake = mb; break; }
            }
        }

        snakeRoot = (snake != null) ? ((MonoBehaviour)snake).transform : transform;

        GetComponentsInChildren<Collider2D>(true, snakeCols);
        for (int i = 0; i < snakeCols.Count; i++) snakeColsTriggerOrig.Add(snakeCols[i].isTrigger);

        powerupSpawner = GameObject.FindObjectOfType<PowerupSpawner>(true);

        EnsureHUD();
        EnsureTint();
    }

    private void EnsureHUD()
    {
        hudMulti = GameObject.FindObjectOfType<PowerupTopHUDMulti>(true);
        if (hudMulti == null) { GameObject go = new GameObject("PowerupTopHUDMulti_AUTO"); hudMulti = go.AddComponent<PowerupTopHUDMulti>(); }
    }
    private void EnsureTint()
    {
        tintMixer = GameObject.FindObjectOfType<PowerupScreenTintMixer>(true);
        if (tintMixer == null) { GameObject go = new GameObject("PowerupScreenTintMixer_AUTO"); tintMixer = go.AddComponent<PowerupScreenTintMixer>(); }
    }

    private void Update()
    {
        float now = Time.unscaledTime;

        if (timeScaleOwned && now >= freezeUntil) { Time.timeScale = prevTimeScale; Time.fixedDeltaTime = prevFixedDelta; timeScaleOwned = false; }
        if (fieldsScaled && now >= freezeUntil) { RestoreScaledFields(); fieldsScaled = false; }
        if (spMin >= 0f && now >= freezeUntil) { RestoreSpawnerIntervals(); }

        if (ghostUntil > 0f && now >= ghostUntil) { RestoreGhost(); ghostUntil = 0f; }
        if (shieldUntil > 0f && now >= shieldUntil) { SnakePowerupBridge.ShieldActive = false; shieldUntil = 0f; }

        if (magnetUntil > now) { MagnetTick(); } else { SnakePowerupBridge.MagnetActive = false; }
    }

    // ===== Activator =====
    public bool ActivatePowerup(PowerupSO def)
    {
        if (def == null) return false;
        string id = ResolveKind(def);
        float dur = ResolveDuration(def);

        if (hudMulti != null) hudMulti.Show(def.sprite, dur, id);

        if (id == "freeze") { StartFreeze(dur); if (tintMixer != null) tintMixer.Show("freeze", new Color(0.55f, 0.75f, 1f, 0.18f), dur); }
        else if (id == "ghost") { StartGhost(dur); if (tintMixer != null) tintMixer.Show("ghost", new Color(0.80f, 0.70f, 1f, 0.16f), dur); }
        else if (id == "shield") { StartShield(dur); if (tintMixer != null) tintMixer.Show("shield", new Color(1f, 0.95f, 0.40f, 0.12f), dur); }
        else { StartMagnet(dur); if (tintMixer != null) tintMixer.Show("magnet", new Color(1f, 1f, 1f, 0.06f), dur); }

        Log("Activate: " + id + " " + dur + "s");
        return true;
    }

    // ===== Effects =====
    private void StartFreeze(float duration)
    {
        if (duration <= 0f) duration = 5f;
        if (!timeScaleOwned) { prevTimeScale = Time.timeScale; prevFixedDelta = Time.fixedDeltaTime; timeScaleOwned = true; }
        Time.timeScale = 0.5f;
        Time.fixedDeltaTime = prevFixedDelta * 0.5f;
        SnakePowerupBridge.FreezeMultiplier = 0.5f;
        freezeUntil = Time.unscaledTime + duration;
        if (!fieldsScaled) { fieldsScaled = TryScaleSpeeds(2f); }
        if (powerupSpawner != null && spMin < 0f)
        {
            System.Type t = powerupSpawner.GetType();
            FieldInfo fMin = t.GetField("spawnIntervalMin", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            FieldInfo fMax = t.GetField("spawnIntervalMax", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fMin != null && fMax != null)
            {
                spMin = (float)fMin.GetValue(powerupSpawner);
                spMax = (float)fMax.GetValue(powerupSpawner);
                fMin.SetValue(powerupSpawner, spMin * 2f);
                fMax.SetValue(powerupSpawner, spMax * 2f);
            }
        }
        MonoBehaviour[] all = GameObject.FindObjectsOfType<MonoBehaviour>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null) continue;
            string n = all[i].GetType().Name;
            if (n.Contains("Spawner") && all[i] != (MonoBehaviour)powerupSpawner)
            {
                ScaleFloatFields(all[i], 2f, new string[] { "interval", "cooldown", "delay", "period", "spawn" });
            }
        }
    }
    private bool TryScaleSpeeds(float factor)
    {
        bool any = false;
        if (snake != null) any |= ScaleFloatFields(snake, factor, new string[] { "step", "tick", "interval", "period", "delay", "move", "speed" });
        return any;
    }
    private bool ScaleFloatFields(Object target, float factor, string[] keys)
    {
        bool any = false;
        System.Type st = target.GetType();
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        FieldInfo[] fields = st.GetFields(flags);
        for (int i = 0; i < fields.Length; i++)
        {
            FieldInfo f = fields[i];
            if (f.FieldType == typeof(float))
            {
                string nm = f.Name.ToLowerInvariant();
                bool hit = false; for (int k = 0; k < keys.Length; k++) if (nm.Contains(keys[k])) { hit = true; break; }
                if (!hit) continue;
                float val = (float)f.GetValue(target);
                FieldScale rec; rec.target = target; rec.field = f; rec.prop = null; rec.original = val; scaled.Add(rec);
                f.SetValue(target, val * factor); any = true;
            }
        }
        PropertyInfo[] props = st.GetProperties(flags);
        for (int i = 0; i < props.Length; i++)
        {
            PropertyInfo p = props[i];
            if (!p.CanRead || !p.CanWrite || p.PropertyType != typeof(float)) continue;
            string nm = p.Name.ToLowerInvariant();
            bool hit = false; for (int k = 0; k < keys.Length; k++) if (nm.Contains(keys[k])) { hit = true; break; }
            if (!hit) continue;
            float val = (float)p.GetValue(target, null);
            FieldScale rec; rec.target = target; rec.field = null; rec.prop = p; rec.original = val; scaled.Add(rec);
            p.SetValue(target, val * factor, null); any = true;
        }
        return any;
    }
    private void RestoreScaledFields()
    {
        for (int i = 0; i < scaled.Count; i++)
        {
            FieldScale rec = scaled[i];
            if (rec.field != null) rec.field.SetValue(rec.target, rec.original);
            else if (rec.prop != null) rec.prop.SetValue(rec.target, rec.original, null);
        }
        scaled.Clear();
        SnakePowerupBridge.FreezeMultiplier = 1f;
    }
    private void RestoreSpawnerIntervals()
    {
        if (powerupSpawner == null) { spMin = spMax = -1f; return; }
        System.Type t = powerupSpawner.GetType();
        FieldInfo fMin = t.GetField("spawnIntervalMin", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        FieldInfo fMax = t.GetField("spawnIntervalMax", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (fMin != null && fMax != null) { fMin.SetValue(powerupSpawner, spMin); fMax.SetValue(powerupSpawner, spMax); }
        spMin = spMax = -1f;
    }

    private void StartGhost(float duration)
    {
        if (duration <= 0f) duration = 5f;
        for (int i = 0; i < snakeCols.Count; i++) if (snakeCols[i] != null) snakeCols[i].isTrigger = true;
        ToggleSnakeBools(true, new string[] { "ignore", "noclip", "ghost", "pass", "through", "invulner", "shield" });
        ghostUntil = Time.unscaledTime + duration;
        SnakePowerupBridge.GhostActive = true;
        Log("Ghost ON");
    }
    private void RestoreGhost()
    {
        for (int i = 0; i < snakeCols.Count; i++)
        {
            Collider2D c = snakeCols[i]; if (c == null) continue;
            bool orig = (i < snakeColsTriggerOrig.Count) ? snakeColsTriggerOrig[i] : false;
            c.isTrigger = orig;
        }
        for (int i = 0; i < toggled.Count; i++)
        {
            BoolToggle t = toggled[i]; if (t.field != null && t.target != null) t.field.SetValue(t.target, t.original);
        }
        toggled.Clear();
        SnakePowerupBridge.GhostActive = false;
        Log("Ghost OFF");
    }
    private void ToggleSnakeBools(bool value, string[] keys)
    {
        if (snake == null) return;
        System.Type st = snake.GetType();
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        FieldInfo[] fields = st.GetFields(flags);
        for (int i = 0; i < fields.Length; i++)
        {
            FieldInfo f = fields[i];
            if (f.FieldType != typeof(bool)) continue;
            string nm = f.Name.ToLowerInvariant();
            bool hit = false;
            for (int k = 0; k < keys.Length; k++) if (nm.Contains(keys[k])) { hit = true; break; }
            if (!hit) continue;
            BoolToggle rec; rec.target = snake; rec.field = f; rec.original = (bool)f.GetValue(snake);
            toggled.Add(rec);
            f.SetValue(snake, value);
        }
    }

    private void StartShield(float duration)
    {
        if (duration <= 0f) duration = 5f;
        shieldUntil = Time.unscaledTime + duration;
        SnakePowerupBridge.ShieldActive = true;
        Log("Shield ON");
    }

    private void OnCollisionEnter2D(Collision2D collision) { if (shieldUntil > Time.unscaledTime) TryIgnoreCollision(collision.collider); }
    private void OnTriggerEnter2D(Collider2D other) { if (shieldUntil > Time.unscaledTime) TryIgnoreCollision(other); }
    private void TryIgnoreCollision(Collider2D other)
    {
        if (other == null) return;
        for (int i = 0; i < snakeCols.Count; i++) { Collider2D mine = snakeCols[i]; if (mine != null) Physics2D.IgnoreCollision(mine, other, true); }
        StartCoroutine(ReenableCollision(other, 0.2f));
    }
    private System.Collections.IEnumerator ReenableCollision(Collider2D other, float delay)
    {
        float t = Time.unscaledTime + delay;
        while (Time.unscaledTime < t) yield return null;
        for (int i = 0; i < snakeCols.Count; i++) { Collider2D mine = snakeCols[i]; if (mine != null && other != null) Physics2D.IgnoreCollision(mine, other, false); }
    }

    // ===== Magnet =====
    private static readonly Collider2D[] _buf = new Collider2D[256];

    private void StartMagnet(float duration)
    {
        if (duration <= 0f) duration = 5f;
        magnetUntil = Time.unscaledTime + duration;
        nextRefreshAt = 0f;
        SnakePowerupBridge.MagnetActive = true;
        Log("Magnet ON");
    }

    private void MagnetTick()
    {
        Vector3 targetPos = snakeRoot != null ? snakeRoot.position : transform.position;
        if (Time.unscaledTime >= nextRefreshAt) { RefreshMagnetCandidates(targetPos); nextRefreshAt = Time.unscaledTime + 0.15f; }

        float step = magnetPullSpeed * Time.unscaledDeltaTime;
        int moved = 0, snapped = 0;
        for (int i = magnetCandidates.Count - 1; i >= 0; i--)
        {
            Transform t = magnetCandidates[i];
            if (t == null) { magnetCandidates.RemoveAt(i); continue; }
            if (IsSnakeOwned(t)) { magnetCandidates.RemoveAt(i); continue; }

            Vector3 p = NearestPointOnSnake(t.position);

            // Если объект практически не двигается (не меняет позицию из-за родителя/тайлмапы), просто "собираем"
            Vector3 before = t.position;
            t.position = Vector3.MoveTowards(t.position, p, step);
            if ((t.position - before).sqrMagnitude < 1e-6f && snapCollectIfNotMovable)
            {
                if ((t.position - p).sqrMagnitude <= magnetRadius * magnetRadius)
                {
                    TryCollectTransform(t, out bool wasPowerup, out bool wasApple);
                    snapped++;
                    magnetCandidates.RemoveAt(i);
                    continue;
                }
            }
            else moved++;

            if ((t.position - p).sqrMagnitude <= magnetCaptureDistance * magnetCaptureDistance)
            {
                TryCollectTransform(t, out bool wasPowerup2, out bool wasApple2);
                magnetCandidates.RemoveAt(i);
            }
        }
        if (moved + snapped == 0) Log("Magnet: no candidates");
    }

    private void RefreshMagnetCandidates(Vector3 center)
    {
        magnetCandidates.Clear();

        int n = Physics2D.OverlapCircleNonAlloc(new Vector2(center.x, center.y), magnetRadius, _buf, ~0);
        for (int i = 0; i < n; i++)
        {
            Collider2D c = _buf[i]; if (c == null) continue; if (IsSnakeCollider(c)) continue;
            if (!IsUI(c.transform)) magnetCandidates.Add(c.transform);
        }

        SpriteRenderer[] all = GameObject.FindObjectsOfType<SpriteRenderer>(true);
        for (int i = 0; i < all.Length; i++)
        {
            SpriteRenderer sr = all[i]; if (sr == null) continue;
            Transform t = sr.transform; if (t == null) continue;
            if (IsUI(t) || IsSnakeOwned(t)) continue;
            if ((t.position - center).sqrMagnitude > magnetRadius * magnetRadius) continue;

            EnsureTempTrigger(t.gameObject);
            if (!magnetCandidates.Contains(t)) magnetCandidates.Add(t);
        }
        Log("Magnet candidates: " + magnetCandidates.Count);
    }

    private void EnsureTempTrigger(GameObject go)
    {
        if (go.GetComponent<Collider2D>() == null)
        {
            CircleCollider2D cc = go.AddComponent<CircleCollider2D>(); cc.isTrigger = true; cc.radius = 0.3f;
        }
        Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
        if (rb == null) { rb = go.AddComponent<Rigidbody2D>(); rb.isKinematic = true; rb.simulated = false; }
    }

    private void TryCollectTransform(Transform t, out bool wasPowerup, out bool wasApple)
    {
        wasPowerup = false; wasApple = false;
        if (t == null) return;

        PowerupPickupItem pui = t.GetComponent<PowerupPickupItem>();
        if (pui != null && pui.spawner != null)
        {
            PowerupSO taken; pui.spawner.ConsumeFromItem(pui, out taken);
            wasPowerup = true;
            return;
        }
        CollectAsApple(t.gameObject);
        wasApple = true;
    }

    private Vector3 NearestPointOnSnake(Vector3 from)
    {
        if (snakeRoot == null) return transform.position;
        float best = float.MaxValue; Vector3 bestPos = snakeRoot.position;
        Transform[] parts = snakeRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < parts.Length; i++)
        {
            Transform seg = parts[i]; if (seg == null || seg == snakeRoot) continue;
            if (seg.GetComponent<SpriteRenderer>() == null && seg.GetComponent<Collider2D>() == null) continue;
            float d = (seg.position - from).sqrMagnitude;
            if (d < best) { best = d; bestPos = seg.position; }
        }
        return bestPos;
    }

    private bool IsSnakeCollider(Collider2D c) { for (int i = 0; i < snakeCols.Count; i++) if (snakeCols[i] == c) return true; return false; }
    private bool IsUI(Transform t) { Transform cur = t; while (cur != null) { if (cur.GetComponent<Canvas>() != null) return true; cur = cur.parent; } return false; }
    private bool IsSnakeOwned(Transform t) { return (snakeRoot != null) && (t == snakeRoot || t.IsChildOf(snakeRoot)); }

    private void CollectAsApple(GameObject go)
    {
        if (go != null) GameObject.Destroy(go);
        if (snake == null) return;
        System.Type st = snake.GetType();
        string[] methodNames = new string[] { "OnAppleCollected", "OnAppleEaten", "CollectApple", "EatApple", "HandleApplePickup", "HandleApple", "FoodEaten" };
        for (int i = 0; i < methodNames.Length; i++)
        {
            MethodInfo mi = st.GetMethod(methodNames[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (mi != null && mi.GetParameters().Length == 0) { try { mi.Invoke(snake, null); return; } catch { } }
        }
        MethodInfo mg = st.GetMethod("Grow", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (mg != null && mg.GetParameters().Length == 0) { try { mg.Invoke(snake, null); return; } catch { } }
        MethodInfo[] mis = st.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        for (int i = 0; i < mis.Length; i++)
        {
            if (mis[i].Name.ToLowerInvariant().Contains("grow"))
            {
                var ps = mis[i].GetParameters();
                if (ps.Length == 1 && (ps[0].ParameterType == typeof(int) || ps[0].ParameterType.IsPrimitive))
                { try { mis[i].Invoke(snake, new object[] { 1 }); return; } catch { } }
            }
        }
    }

    // ===== Helpers =====
    private string ResolveKind(PowerupSO def)
    {
        string key = null; System.Type t = def.GetType();
        FieldInfo f = t.GetField("id"); if (f != null) { object v = f.GetValue(def); key = v != null ? v.ToString() : null; }
        if (string.IsNullOrEmpty(key)) { PropertyInfo p = t.GetProperty("Id"); if (p != null) { object v = p.GetValue(def, null); key = v != null ? v.ToString() : null; } }
        if (string.IsNullOrEmpty(key)) { PropertyInfo p2 = t.GetProperty("Kind"); if (p2 != null) { object v = p2.GetValue(def, null); key = v != null ? v.ToString() : null; } }
        if (string.IsNullOrEmpty(key)) key = def.name;
        string spriteName = def.sprite != null ? def.sprite.name : "";
        string idSrc = (key + " " + spriteName).ToLowerInvariant();
        if (idSrc.Contains("freeze") || idSrc.Contains("frost") || idSrc.Contains("замор") || idSrc.Contains("slow")) return "freeze";
        if (idSrc.Contains("ghost") || idSrc.Contains("phase") || idSrc.Contains("noclip") || idSrc.Contains("призрак")) return "ghost";
        if (idSrc.Contains("shield") || idSrc.Contains("guard") || idSrc.Contains("protect") || idSrc.Contains("щит")) return "shield";
        if (idSrc.Contains("magnet") || idSrc.Contains("vacuum") || idSrc.Contains("магнит")) return "magnet";
        return "magnet";
    }
    private float ResolveDuration(PowerupSO def)
    {
        float d = 5f; System.Type t = def.GetType();
        FieldInfo f = t.GetField("duration"); if (f != null) { object v = f.GetValue(def); if (v is float) d = (float)v; }
        else { PropertyInfo p = t.GetProperty("Duration"); if (p != null) { object v = p.GetValue(def, null); if (v is float) d = (float)v; } }
        return d > 0f ? d : 5f;
    }
}
