using UnityEngine;
using TMPro;

public class ForestModeBootstrap : MonoBehaviour
{
    [SerializeField] private SnakeController snake;
    [SerializeField] private Board board;
    [SerializeField] private TextMeshProUGUI distanceText;
    [SerializeField] private float startStepTime = 0.18f;   // slower start
    [SerializeField] private float distancePerSecond = 4f;

    private float distance;

    private void Start()
    {
        if (!snake) snake = FindObjectOfType<SnakeController>();
        if (!board) board = FindObjectOfType<Board>();

        if (board) board.wrap = false;                    // no wrap in Forest
        if (snake) snake.SetStartStepTime(startStepTime); // slow start

        if (snake && board)
        {
            var pos = snake.transform.position;
            pos.x = -board.borderX * board.tileWorldSize * 0.25f;
            snake.transform.position = pos;
        }
    }

    private void Update()
    {
        distance += distancePerSecond * Time.deltaTime;
        if (distanceText) distanceText.text = $"Dist: {Mathf.FloorToInt(distance)}";
    }
}
