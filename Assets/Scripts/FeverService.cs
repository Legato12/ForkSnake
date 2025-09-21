using UnityEngine;

public static class FeverService
{
    static int need = 3;       // need 3 "x5" events (пример)
    static int have = 0;
    public static bool Active { get; private set; }
    static float until;

    public static void Reset(){ Active = false; have = 0; until = 0f; }

    public static void OnApple(int chainLevel)
    {
        if (Active) return;
        if (chainLevel >= 5)
        {
            have++;
            if (have >= need){ Active = true; until = Time.time + 8f; have = 0; }
        }
        else have = 0;
    }

    public static void Update(){ if (Active && Time.time >= until) Active = false; }
}
