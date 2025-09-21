// UI element for a single stash slot (Unity 2020.3 compatible).
// Keeps icon, count text and click forwarding to PowerupStash.
using UnityEngine;
using UnityEngine.UI;
using System;

public sealed class PowerupSlotUI : MonoBehaviour
{
    [SerializeField] private Image icon = null;
    [SerializeField] private Text countText = null;
    [SerializeField] private Button button = null;

    [NonSerialized] public Action<PowerupSlotUI> onClick;

    public void Bind(Image iconImage, Text count, Button btn)
    {
        icon = iconImage;
        countText = count;
        button = btn;
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }
    }

    private void Awake()
    {
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }
    }

    private void OnClick()
    {
        if (onClick != null) onClick(this);
    }

    public void SetIcon(Sprite s)
    {
        if (icon != null) icon.sprite = s;
    }

    public void SetCount(int count)
    {
        if (countText == null) return;
        if (count <= 1) countText.text = "";
        else countText.text = count.ToString();
    }

    public void SetInteractable(bool canUse)
    {
        if (button != null) button.interactable = canUse;
    }
}
