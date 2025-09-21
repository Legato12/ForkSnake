using UnityEngine;

/// <summary>Simple obstacle/column that drifts left and occupies a grid cell.</summary>
public class Column : MonoBehaviour
{
    [HideInInspector] public float speed = 3f;
    [HideInInspector] public float despawnX = -20f;
    [SerializeField] private Board board;

    private Vector2Int cell; private bool hasCell;

    private void Awake()
    {
        if (!board) board = FindObjectOfType<Board>();
    }

    private void OnEnable()  { UpdateCell(); }
    private void OnDisable() { if (hasCell) { ObstacleRegistry.SetBlocked(cell, false); hasCell = false; } }

    private void Update()
    {
        transform.position += Vector3.left * speed * Time.deltaTime;
        UpdateCell();
        if (transform.position.x < despawnX) gameObject.SetActive(false);
    }

    private void UpdateCell()
    {
        if (!board) return;
        var c = board.WorldToCell(transform.position);
        if (!hasCell)
        {
            hasCell = true; cell = c;
            ObstacleRegistry.SetBlocked(cell, true);
            return;
        }
        if (c != cell)
        {
            ObstacleRegistry.SetBlocked(cell, false);
            cell = c;
            ObstacleRegistry.SetBlocked(cell, true);
        }
    }
}
