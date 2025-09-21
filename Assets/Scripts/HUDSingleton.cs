using UnityEngine;

/// <summary>Keeps only one HUD/TopBar instance to avoid duplicate text after reload/shake.</summary>
public class HUDSingleton : MonoBehaviour
{
    private static HUDSingleton _instance;
    private void Awake()
    {
        if (_instance == null) { _instance = this; DontDestroyOnLoad(gameObject); }
        else if (_instance != this) Destroy(gameObject);
    }
}
