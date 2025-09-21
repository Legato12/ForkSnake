
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;

[DefaultExecutionOrder(360)]
public class PulseOnAppleBridge : MonoBehaviour
{
    SnakePulseFX pulse;
    Component snake;
    FieldInfo fiApples;
    int last = -1;

    void Awake()
    {
        pulse = GetComponent<SnakePulseFX>() ?? gameObject.AddComponent<SnakePulseFX>();

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try{
                var tp = asm.GetTypes().FirstOrDefault(t => t.Name=="SnakeController");
                if (tp!=null) { snake = GetComponent(tp) as Component; break; }
            }catch{}
        }
        if (snake != null)
            fiApples = snake.GetType().GetField("apples", BindingFlags.NonPublic|BindingFlags.Instance);
    }

    void Update()
    {
        if (snake == null || fiApples == null) return;
        int a = (int)fiApples.GetValue(snake);
        if (last < 0) last = a;
        else if (a > last){ last = a; pulse?.Trigger(); }
    }
}
