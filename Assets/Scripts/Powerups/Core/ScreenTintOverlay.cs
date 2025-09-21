using UnityEngine;
using UnityEngine.UI;

namespace SnakeGame.Powerups
{
    [RequireComponent(typeof(Image))]
    public class ScreenTintOverlay : MonoBehaviour
    {
        private Image _img;

        private void Awake()
        {
            _img = GetComponent<Image>();
            if (_img != null)
            {
                _img.raycastTarget = false;
                Color c = _img.color;
                c.a = 0f;
                _img.color = c;
            }
        }

        public void SetTint(Color c)
        {
            if (_img != null) _img.color = c;
        }
    }
}
