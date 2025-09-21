
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

public class QuickbarCircleUI : MonoBehaviour
{
    [System.Serializable]
    public class Slot
    {
        public Button button;
        public Image  bg;
        public Image  ring;
        public Image  frame;
        public Image  icon;
        [HideInInspector] public PowerupSO def;
        [HideInInspector] public int count;
        public void Clear()
        {
            def = null; count = 0;
            if (icon) icon.enabled = false;
            if (ring) ring.enabled = false;
        }
        public void Set(PowerupSO d)
        {
            def = d; count += 1;
            if (icon)
            {
                icon.sprite = ExtractIcon(d);
                icon.enabled = icon.sprite != null;
            }
            if (ring) ring.enabled = false;
        }
        static Sprite ExtractIcon(PowerupSO d)
        {
            if (!d) return null;
            var t = d.GetType();
            try{
                var f = t.GetField("icon", BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
                var s = f!=null ? f.GetValue(d) as Sprite : null;
                if (s) return s;
                f = t.GetField("sprite", BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
                return f!=null ? f.GetValue(d) as Sprite : null;
            }catch{} return null;
        }
    }

    public Slot slot0, slot1;
    public Component snake;                // SnakeController (any assembly)
    public ActivePowerupDisplayV2 activeDisplay;

    MethodInfo miActivate, miApplyStash;   // ActivatePowerup(def) or ApplyPowerupFromStash(def)

    void Awake()
    {
        if (!snake)
        {
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                try{
                    var tp = System.Array.Find(asm.GetTypes(), t => t.Name=="SnakeController");
                    if (tp!=null) { snake = FindObjectOfType(tp) as Component; break; }
                }catch{}
            }
        }
        if (snake)
        {
            var t = snake.GetType();
            miActivate  = t.GetMethod("ActivatePowerup", BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
            miApplyStash= t.GetMethod("ApplyPowerupFromStash", BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
        }

        if (slot0?.button) slot0.button.onClick.AddListener(()=>OnClick(0));
        if (slot1?.button) slot1.button.onClick.AddListener(()=>OnClick(1));
        slot0?.Clear(); slot1?.Clear();
    }

    public bool TryStash(PowerupSO def)
    {
        if (!def) return false;
        if (slot0!=null && slot0.def && Same(slot0.def, def)) { slot0.Set(def); return true; }
        if (slot1!=null && slot1.def && Same(slot1.def, def)) { slot1.Set(def); return true; }
        if (slot0!=null && !slot0.def) { slot0.Set(def); return true; }
        if (slot1!=null && !slot1.def) { slot1.Set(def); return true; }
        return false;
    }

    // Allows SendMessage("OnPowerupCollected", def)
    public void OnPowerupCollected(PowerupSO def) => TryStash(def);

    void OnClick(int idx)
    {
        var s = (idx==0) ? slot0 : slot1;
        if (s==null || s.def==null) return;
        Activate(s.def);
        s.count -= 1;
        if (s.count <= 0) s.Clear();
        if (activeDisplay) activeDisplay.Show(s.icon ? s.icon.sprite : null, s.def.duration);
    }

    void Activate(PowerupSO def)
    {
        if (!snake) return;
        if (miActivate != null) { miActivate.Invoke(snake, new object[]{def}); return; }
        if (miApplyStash != null) { miApplyStash.Invoke(snake, new object[]{def}); return; }
        snake.SendMessage("ActivatePowerup", def, SendMessageOptions.DontRequireReceiver);
        snake.SendMessage("ApplyPowerupFromStash", def, SendMessageOptions.DontRequireReceiver);
    }

    bool Same(PowerupSO a, PowerupSO b)
    {
        if (a==b) return true; if (!a||!b) return false;
        try{
            var fa = a.GetType().GetField("id", BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
            var fb = b.GetType().GetField("id", BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
            var sa = fa!=null ? (fa.GetValue(a) as string) : null;
            var sb = fb!=null ? (fb.GetValue(b) as string) : null;
            if (!string.IsNullOrEmpty(sa) && !string.IsNullOrEmpty(sb)) return sa==sb;
        }catch{}
        return a.name == b.name;
    }
}
