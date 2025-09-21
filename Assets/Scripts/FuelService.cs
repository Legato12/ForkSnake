using UnityEngine;

public static class FuelService
{
    public static int MaxFuel { get; private set; } = 180;
    public static int Fuel    { get; private set; } = 0;

    public static void Reset(int? maxOverride = null){ MaxFuel = maxOverride ?? MaxFuel; Fuel = 0; }
    public static void Add(int amount){ Fuel = Mathf.Clamp(Fuel + amount, 0, MaxFuel); }
    public static bool TryConsume(int amount)
    {
        if (Fuel < amount) return false;
        Fuel -= amount; return true;
    }
}
