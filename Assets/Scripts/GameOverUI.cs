using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI coinsText;
    [SerializeField] private Button retryBtn;
    [SerializeField] private Button doubleBtn;

    public void Bind(int score, int coins, System.Action onRetry, System.Action onDouble)
    {
        if (scoreText) scoreText.text = "Score: " + score;
        if (coinsText) coinsText.text = "Coins: " + coins;
        if (retryBtn != null){ retryBtn.onClick.RemoveAllListeners(); retryBtn.onClick.AddListener(()=>onRetry?.Invoke()); }
        if (doubleBtn != null){ doubleBtn.onClick.RemoveAllListeners(); doubleBtn.onClick.AddListener(()=>onDouble?.Invoke()); }
    }
}
