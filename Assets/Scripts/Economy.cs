using UnityEngine;

public static class Economy
{
    public static int TotalCoins { get; private set; }
    public static int BestScore  { get; private set; }

    public static void Load(){ TotalCoins = PlayerPrefs.GetInt("eco.coins",0); BestScore = PlayerPrefs.GetInt("eco.best",0); }
    public static void AddCoins(int d){ TotalCoins = Mathf.Max(0, TotalCoins + d); PlayerPrefs.SetInt("eco.coins", TotalCoins); }
    public static void TrySetBest(int s){ if (s > BestScore){ BestScore = s; PlayerPrefs.SetInt("eco.best", BestScore); } }
}
