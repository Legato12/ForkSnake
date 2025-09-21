using UnityEngine;
using System;

public class AdService : MonoBehaviour
{
    public static AdService Instance;
    private void Awake()
    {
        if (Instance == null){ Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    public void ShowRewarded(Action onSuccess)
    {
        Debug.Log("[AdService] Simulated rewarded ad.");
        onSuccess?.Invoke();
    }
}
