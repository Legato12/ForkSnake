// Unity 2020.3 compatible.
// Robust pickup: on snake contact, consumes from spawner and forwards to Stash.AddStatic(def).
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class PowerupPickupItem : MonoBehaviour
{
    [System.NonSerialized] public PowerupSpawner spawner;
    [System.NonSerialized] public PowerupSO def;
    [System.NonSerialized] public Vector2Int cell;

    private void Reset()
    {
        var c = GetComponent<Collider2D>();
        if (c != null) c.isTrigger = true;
    }

    private static bool LooksLikeSnake(Transform t)
    {
        if (t == null) return false;
        var all = t.GetComponentsInParent<MonoBehaviour>(true);
        for (int i = 0; i < all.Length; i++)
        {
            var mb = all[i]; if (mb == null) continue;
            var tt = mb.GetType();
            if (tt != null && tt.Name == "SnakeController") return true;
        }
        if (t.CompareTag("Player")) return true;
        string n = t.name != null ? t.name.ToLowerInvariant() : "";
        if (n.Contains("snake") || n.Contains("head")) return true;
        return false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (spawner == null || def == null) return;
        if (!LooksLikeSnake(other.transform)) return;
        PowerupSO taken;
        if (spawner.ConsumeFromItem(this, out taken))
        {
            PowerupStash.AddStatic(taken);
        }
    }
}
