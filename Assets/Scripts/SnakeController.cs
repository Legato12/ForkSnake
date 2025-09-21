using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SnakeController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Board board;
    [SerializeField] private AppleSpawner appleSpawner;
    [SerializeField] private PowerupSpawner powerupSpawner;
    [SerializeField] private ObstacleRegistry obstacles;
    [SerializeField] private Transform segmentPrefab;
    [SerializeField] private Transform segmentsParent;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI coinsText;
    [SerializeField] private TextMeshProUGUI chainText;
    [SerializeField] private GameObject gameOverUI;
    [SerializeField] private StatusHUD statusHUD;

    [Header("Speed")]
    [SerializeField] private float stepTimeStart = 0.14f;
    [SerializeField] private float stepTimeMin = 0.08f;
    [SerializeField] private float stepReductionPerChain = 0.012f;
    private float stepTimeStartRuntime;

    [Header("Start")]
    [SerializeField] private int startLength = 4;
    [SerializeField] private int prewarmSegments = 32;

    // grid
    private readonly List<Vector2Int> cells = new List<Vector2Int>(256);
    private readonly HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();
    private Vector2Int dir = Vector2Int.right;
    private Vector2Int pendingDir = Vector2Int.right;
    private float acc;
    private bool alive;

    // visuals / pool
    private readonly List<Transform> segments = new List<Transform>(256);
    private readonly Queue<Transform> pool = new Queue<Transform>(128);
    private readonly List<Vector2> prevWorld = new List<Vector2>(256);
    private readonly List<Vector2> currWorld = new List<Vector2>(256);

    // score
    private int score;
    private int coins;
    private int chainLevel = 1;
    private float lastAppleTime = -999f;
    [SerializeField] private int chainMax = 5;
    [SerializeField] private float chainWindowBase = 1.7f;
    [SerializeField] private float chainWindowPerCell = 0.035f;
    [SerializeField] private float chainWindowCap = 2.6f;
    private float currentChainWindow = 1.7f;

    // Powerups (simple)
    private float shieldExpire, ghostExpire;
    private bool ShieldActive => Time.time < shieldExpire;
    private bool GhostActive  => Time.time < ghostExpire;

    // Growth pulses (delayed tail growth)
    private readonly List<int> pulses = new List<int>(); // each = current segment index of the pulse

    // -------- external compatibility
    public void SetStartStepTime(float t) { stepTimeStart = t; stepTimeStartRuntime = t; }
    public void ApplyPowerupFromStash(PowerupSO def) { if (def != null) ApplyPowerup(def); }
    public void RefreshBodySprites()
    {
        if (!segmentPrefab) return;
        var spr = segmentPrefab.GetComponent<SpriteRenderer>()?.sprite;
        if (!spr) return;
        for (int i = 1; i < segments.Count; i++)
        {
            var sr = segments[i].GetComponent<SpriteRenderer>();
            if (sr) sr.sprite = spr;
        }
    }

    private void Awake() { PrewarmPool(); }
    private void Start()
    {
        stepTimeStartRuntime = stepTimeStart;
        if (!obstacles) obstacles = FindObjectOfType<ObstacleRegistry>();
        if (!board) board = FindObjectOfType<Board>();
        ResetRun();
    }

    private void PrewarmPool()
    {
        for (int i = 0; i < prewarmSegments; i++)
        {
            var seg = Instantiate(segmentPrefab, segmentsParent);
            seg.gameObject.SetActive(false);
            pool.Enqueue(seg);
        }
    }

    private void ResetRun()
    {
        // visuals
        for (int i = segmentsParent.childCount - 1; i >= 0; i--)
        {
            var t = segmentsParent.GetChild(i);
            if (t != this.transform)
            {
                t.gameObject.SetActive(false);
                pool.Enqueue(t as Transform);
            }
        }
        segments.Clear(); prevWorld.Clear(); currWorld.Clear();

        // logic
        alive = true; acc = 0f;
        score = 0; coins = 0; chainLevel = 1;
        dir = pendingDir = Vector2Int.right;
        cells.Clear(); occupied.Clear();
        pulses.Clear();

        var start = new Vector2Int(0, 0);
        for (int i = 0; i < startLength; i++)
        {
            var c = start - new Vector2Int(i, 0);
            cells.Add(c); occupied.Add(c);
        }
        segments.Add(this.transform);
        EnsureWorldBuffersLength(cells.Count);
        for (int i = 0; i < cells.Count; i++)
        {
            var w = (Vector2)board.CellToWorld(cells[i]);
            prevWorld.Add(w); currWorld.Add(w);
        }
        transform.position = board.CellToWorld(cells[0]);
        Render(0f);

        if (appleSpawner != null)
        {
            appleSpawner.Spawn(occupied, cells[0], dir, isGold:false);
            RecomputeChainWindowForCurrentApple();
        }

        UpdateScoreUI(); UpdateCoinsUI(); UpdateChainUI();
        if (gameOverUI) gameOverUI.SetActive(false);
        statusHUD?.HideAll();
        shieldExpire = ghostExpire = 0f;
    }

    private float BaseStepTime() => Mathf.Max(stepTimeMin, stepTimeStartRuntime - (chainLevel - 1) * stepReductionPerChain);
    private float EffectiveStepTime() => BaseStepTime();

    private void Update()
    {
        if (!alive) { if (Input.GetKeyDown(KeyCode.R)) ResetRun(); return; }

        // input
        var want = pendingDir;
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))    want = Vector2Int.up;
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))  want = Vector2Int.down;
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))  want = Vector2Int.left;
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) want = Vector2Int.right;
        if (cells.Count < 2 || want + dir != Vector2Int.zero) pendingDir = want;

        if (chainLevel > 1 && Time.time - lastAppleTime > currentChainWindow)
        { chainLevel = 1; UpdateChainUI(); }

        float stepTimeNow = EffectiveStepTime();
        acc += Time.deltaTime;
        while (acc >= stepTimeNow)
        {
            acc -= stepTimeNow;
            StepLogic();
            stepTimeNow = EffectiveStepTime();
        }
        float t = stepTimeNow <= 0f ? 1f : Mathf.Clamp01(acc / stepTimeNow);
        Render(t);
    }

    private void StepLogic()
    {
        if (cells.Count < 2 || pendingDir + dir != Vector2Int.zero) dir = pendingDir;

        var head = cells[0];
        var next = head + dir;

        // bounds
        if (!board.wrap && !board.InBounds(next)) { Die(); return; }
        if (board.wrap) next = board.Wrap(next);

        // forest obstacles
        if (obstacles != null && obstacles.IsBlocked(next))
        {
            if (GhostActive) { /* pass through */ }
            else if (ShieldActive)
            {
                shieldExpire = 0f;
                if (obstacles.TryConsume(next, out var tr) && tr) Destroy(tr.gameObject);
            }
            else { Die(); return; }
        }

        bool ate = (appleSpawner != null && next == appleSpawner.AppleCell);

        // self collision
        if (occupied.Contains(next) && !SnakePowerupBridge.GhostActive)
        {
            bool isTail = next == cells[cells.Count - 1];
            bool willMoveTail = !ate && pulses.Count == 0; // if a pulse will mature this step, tail may not move
            bool coll = !(isTail && willMoveTail);
            if (coll) { Die(); return; }
        }

        var lastTailBeforeShift = cells[cells.Count - 1];

        // shift body
        for (int i = cells.Count - 1; i > 0; --i) cells[i] = cells[i - 1];
        cells[0] = next;

        // occupancy update
        occupied.Remove(lastTailBeforeShift);
        occupied.Add(next);
// powerup pickup at head cell
if (powerupSpawner != null && powerupSpawner.TryConsumeAt(next, out var powerDef))
{
    var stash = PowerupStash.Instance ?? FindObjectOfType<PowerupStash>();
    if (stash != null) stash.Pickup(powerDef);
    else ApplyPowerup(powerDef);
}


        // apple
        if (ate)
        {
            OnAppleEaten();
            // start a pulse at the head (index 0)
            pulses.Add(0);
            // respawn apple
            bool nextGold = ShouldSpawnGold();
            appleSpawner.Spawn(occupied, cells[0], dir, isGold: nextGold);
            RecomputeChainWindowForCurrentApple();
        }

        // advance pulses, grow when reaching tail
        for (int i = 0; i < pulses.Count; i++) pulses[i]++;
        int matureCount = 0;
        for (int i = pulses.Count - 1; i >= 0; i--)
        {
            if (pulses[i] >= cells.Count - 1)
            {
                matureCount++;
                pulses.RemoveAt(i);
            }
        }
        for (int i = 0; i < matureCount; i++)
        {
            cells.Add(lastTailBeforeShift);
            occupied.Add(lastTailBeforeShift);
        }

        EnsureVisualsLength(cells.Count);
        SyncWorldBuffers();
    }

    private void OnAppleEaten()
    {
        if (lastAppleTime > 0f && Time.time - lastAppleTime <= currentChainWindow)
            chainLevel = Mathf.Min(chainLevel + 1, chainMax);
        else chainLevel = 1;
        lastAppleTime = Time.time;

        score += chainLevel;
        coins += 1 * chainLevel;
        UpdateScoreUI(); UpdateCoinsUI(); UpdateChainUI();
        PulseHead();
    }

    private bool ShouldSpawnGold() => chainLevel >= 5 && Random.value < 0.10f;

    private void RecomputeChainWindowForCurrentApple()
    {
        if (appleSpawner == null) { currentChainWindow = chainWindowBase; return; }
        int dist = Mathf.Abs(cells[0].x - appleSpawner.AppleCell.x) +
                   Mathf.Abs(cells[0].y - appleSpawner.AppleCell.y);
        currentChainWindow = Mathf.Clamp(chainWindowBase + chainWindowPerCell * dist, chainWindowBase, chainWindowCap);
    }

    private void Die()
    {
        alive = false;
        if (gameOverUI) gameOverUI.SetActive(true);
    }

    private void ApplyPowerup(PowerupSO def)
    {
        if (def == null) return;
        switch (def.id)
        {
            case PowerupId.Shield: shieldExpire = Time.time + def.duration; break;
            case PowerupId.Ghost:  ghostExpire  = Time.time + def.duration; break;
            // Magnet/Freeze can be added here
        }
        statusHUD?.Show(def.id, def.duration);
    }

    private void UpdateScoreUI() { if (scoreText) scoreText.text = "Score: " + score; }
    private void UpdateCoinsUI() { if (coinsText) coinsText.text = "Coins: " + coins; }
    private void UpdateChainUI()
    {
        if (!chainText) return;
        chainText.gameObject.SetActive(chainLevel > 1);
        if (chainLevel > 1)
        {
            chainText.text = $"Chain x{chainLevel}";
            chainText.transform.localScale = Vector3.one * 1.12f;
            CancelInvoke(nameof(ResetChainScale));
            Invoke(nameof(ResetChainScale), 0.05f);
        }
        else chainText.text = string.Empty;
    }
    private void ResetChainScale() { if (chainText) chainText.transform.localScale = Vector3.one; }
    public void OnClickRetry() { if (!alive) ResetRun(); }

    
private void EnsureVisualsLength(int len)
{
    var bodySprite = segmentPrefab ? segmentPrefab.GetComponent<SpriteRenderer>()?.sprite : null;
    // Grow visuals to match logical length
    while (segments.Count < len)
    {
        var seg = pool.Count > 0 ? pool.Dequeue() : Instantiate(segmentPrefab, segmentsParent);
        seg.gameObject.SetActive(true);
        var sr = seg.GetComponent<SpriteRenderer>();
        if (sr != null && bodySprite != null) sr.sprite = bodySprite;
        seg.name = "Segment_" + segments.Count;

        // Place exactly at the current tail cell (no center pop)
        Vector3 tailPos = transform.position;
        if (board != null && cells.Count > 0)
        {
            // Always use the last logical cell (tail), not segments.Count - 1 which may lag
            var tailCell = cells[cells.Count - 1];
            tailPos = board.CellToWorld(tailCell);
        }
        seg.position = tailPos;
        segments.Add(seg);

        // Make sure the world buffers are initialized for this new index (prevents 1‑frame drag)
        EnsureWorldBuffersLength(segments.Count);
        int idx = segments.Count - 1;
        if (idx >= 0 && idx < prevWorld.Count && idx < currWorld.Count)
        {
            Vector2 w = new Vector2(tailPos.x, tailPos.y);
            prevWorld[idx] = w;
            currWorld[idx] = w;
        }
    }

    // Shrink visuals if they exceed logical length
    while (segments.Count > len)
    {
        var t = segments[segments.Count - 1];
        segments.RemoveAt(segments.Count - 1);
        if (t != this.transform)
        {
            t.gameObject.SetActive(false);
            t.SetParent(segmentsParent, false);
            pool.Enqueue(t);
        }
    }
}


    private void EnsureWorldBuffersLength(int len)
{
    while (prevWorld.Count < len) prevWorld.Add(Vector2.zero);
    while (currWorld.Count < len) currWorld.Add(Vector2.zero);
    int n = Mathf.Min(len, cells.Count);
    for (int i = 0; i < n; i++)
    {
        if (prevWorld[i] == Vector2.zero && currWorld[i] == Vector2.zero && board != null)
        {
            Vector2 w = (Vector2)board.CellToWorld(cells[i]);
            prevWorld[i] = w; currWorld[i] = w;
        }
    }
}


    private void SyncWorldBuffers()
    {
        EnsureWorldBuffersLength(cells.Count);
        for (int i = 0; i < cells.Count; i++)
        {
            prevWorld[i] = currWorld[i];
            currWorld[i] = (Vector2)board.CellToWorld(cells[i]);
        }
    }

    private void Render(float t)
    {
        int n = Mathf.Min(segments.Count, currWorld.Count);
        for (int i = 0; i < n; i++)
        {
            Vector2 pos = Vector2.Lerp(prevWorld[i], currWorld[i], t);
            var tr = segments[i];
            tr.position = new Vector3(pos.x, pos.y, 0f);
            // reset scale
            tr.localScale = Vector3.one;
        }
        // simple pulse visualization
        for (int i = 0; i < pulses.Count; i++)
        {
            int idx = Mathf.Clamp(pulses[i], 0, n - 1);
            if (idx >= 0 && idx < segments.Count) segments[idx].localScale = Vector3.one * 1.10f;
        }
    }

    private void PulseHead()
    {
        var head = segments[0];
        head.localScale = Vector3.one * 1.10f;
        CancelInvoke(nameof(ResetHeadScale));
        Invoke(nameof(ResetHeadScale), 0.06f);
    }
    private void ResetHeadScale() { segments[0].localScale = Vector3.one; }

// Compatibility shims
public void ActivatePowerup(PowerupSO def) { ApplyPowerup(def); }
public int CurrentChain => chainLevel;
public float ChainTimeLeft()
{
    if (lastAppleTime <= 0f) return 0f;
    float left = currentChainWindow - (Time.time - lastAppleTime);
    return left > 0f ? left : 0f;
}
public void OnMenuButton(string action)
{
    if (string.IsNullOrEmpty(action)) return;
    action = action.ToLowerInvariant();
    if (action.Contains("retry")) { OnClickRetry(); return; }
}
}
