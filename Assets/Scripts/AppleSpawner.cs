using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AppleSpawner : MonoBehaviour
{
    [SerializeField] private Board board;
    [SerializeField] private BoardPlayArea playArea;
    [SerializeField] private Transform applePrefab;
    [SerializeField] private Transform goldApplePrefab;
    [SerializeField] private int tries = 300;

    public Vector2Int AppleCell { get; private set; }
    public bool IsGold { get; private set; }

    private Transform _current;

    public void Despawn()
    {
        if (_current) Destroy(_current.gameObject);
        _current = null;
        IsGold = false;
        AppleCell = new Vector2Int(9999, 9999);
    }

    public void Spawn(HashSet<Vector2Int> occupied, Vector2Int head, Vector2Int dir, bool isGold = false)
    {
        if (!board) board = FindObjectOfType<Board>();
        if (!playArea) playArea = FindObjectOfType<BoardPlayArea>();

        var forbidden = new HashSet<Vector2Int>(occupied);
        const int safeAhead = 2;
        for (int i = 1; i <= safeAhead; i++)
            forbidden.Add(new Vector2Int(head.x + dir.x * i, head.y + dir.y * i));

        for (int t = 0; t < tries; t++)
        {
            int x = Random.Range(-board.borderX, board.borderX + 1);
            int y = Random.Range(playArea ? playArea.PlayBottomY : -board.borderY,
                                 (playArea ? playArea.PlayTopY : board.borderY) + 1);
            var c = new Vector2Int(x, y);
            if (forbidden.Contains(c)) continue;
            Place(c, isGold);
            return;
        }
        for (int y = playArea ? playArea.PlayBottomY : -board.borderY; y <= (playArea ? playArea.PlayTopY : board.borderY); y++)
        for (int x = -board.borderX; x <= board.borderX; x++)
        {
            var c = new Vector2Int(x, y);
            if (!forbidden.Contains(c)) { Place(c, isGold); return; }
        }
    }

    private void Place(Vector2Int c, bool gold)
    {
        if (_current) Destroy(_current.gameObject);
        var prefab = gold && goldApplePrefab ? goldApplePrefab : applePrefab;
        _current = Instantiate(prefab, board.CellToWorld(c), Quaternion.identity, transform);
        AppleCell = c;
        IsGold = gold;

        // No gold prefab? Tint the apple to gold (yellow square look)
        if (gold && !goldApplePrefab)
        {
            var sr = _current.GetComponentInChildren<SpriteRenderer>(true);
            if (sr) sr.color = new Color(1f, 0.85f, 0.1f, 1f);
            var img = _current.GetComponentInChildren<Image>(true);
            if (img) img.color = new Color(1f, 0.85f, 0.1f, 1f);
        }
    }

}