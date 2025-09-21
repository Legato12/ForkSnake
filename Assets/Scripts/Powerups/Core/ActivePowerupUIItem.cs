using UnityEngine;
using UnityEngine.UI;

namespace SnakeGame.Powerups
{
    public class ActivePowerupUIItem : MonoBehaviour
    {
        public Image icon;
        public Image radialFill;
        private float _endTime;
        private float _duration;

        public void Bind(PowerupSO so, float endTime)
        {
            _endTime = endTime;
            _duration = Mathf.Max(0.01f, endTime - Time.unscaledTime);
            if (icon == null) icon = GetComponentInChildren<Image>();
            if (icon != null && so != null) icon.sprite = so.sprite;
        }

        private void Update()
        {
            if (radialFill != null)
            {
                float remain = Mathf.Max(0f, _endTime - Time.unscaledTime);
                float t = _duration > 0.0001f ? (remain / _duration) : 0f;
                radialFill.fillAmount = Mathf.Clamp01(t);
            }
        }
    }
}
