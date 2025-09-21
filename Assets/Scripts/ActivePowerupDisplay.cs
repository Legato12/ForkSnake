using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ActivePowerupDisplay : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Image radialFill;
    [SerializeField] private TextMeshProUGUI timerText;

    private float duration, started;

    public void Show(Sprite iconSprite, float dur)
    {
        if (icon) icon.sprite = iconSprite;
        duration = dur; started = Time.time;
        gameObject.SetActive(true);
        SetProgress(0f);
    }

    public void SetProgress(float t)
    {
        if (radialFill) radialFill.fillAmount = Mathf.Clamp01(1f - t);
        float left = Mathf.Max(0f, started + duration - Time.time);
        if (timerText) timerText.text = Mathf.CeilToInt(left).ToString();
    }

    public void Hide()
    {
        if (icon) icon.sprite = null;
        if (radialFill) radialFill.fillAmount = 0f;
        if (timerText) timerText.text = "";
        gameObject.SetActive(false);
    }
}