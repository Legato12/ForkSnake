using UnityEngine;
using UnityEngine.UI;

public class HarvestQuickbar2 : MonoBehaviour
{
    [SerializeField] private SnakeController snake;
    [SerializeField] private Image slot0;
    [SerializeField] private Image slot1;
    [SerializeField] private Button btn0;
    [SerializeField] private Button btn1;
    [SerializeField] private Sprite emptySprite;

    private PowerupSO[] slots = new PowerupSO[2];

    private void Awake()
    {
        if (btn0) btn0.onClick.AddListener(() => Use(0));
        if (btn1) btn1.onClick.AddListener(() => Use(1));
        Refresh();
    }

    public bool TryStash(PowerupSO def)
    {
        if (!def) return false;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                slots[i] = def;
                Refresh();
                return true;
            }
        }
        return false; // full
    }

    private void Use(int i)
    {
        if (!snake) snake = FindObjectOfType<SnakeController>();
        var def = slots[i];
        if (def == null) return;
        slots[i] = null; Refresh();
        snake.ActivatePowerup(def);
    }

    private void Refresh()
    {
        if (slot0) { slot0.sprite = emptySprite; slot0.color = slots[0] ? Color.white : new Color(1,1,1,0.4f); }
        if (slot1) { slot1.sprite = emptySprite; slot1.color = slots[1] ? Color.white : new Color(1,1,1,0.4f); }
    }
}