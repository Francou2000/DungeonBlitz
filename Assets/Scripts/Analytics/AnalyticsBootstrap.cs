using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Analytics;
using Unity.Services.Core;

/// <summary>
/// Bootstraps Unity Gaming Services + Analytics once for the app lifetime.
/// Attach this to a startup scene object (or rely on the RuntimeInitializeOnLoad hook).
/// </summary>
public sealed class AnalyticsBootstrap : MonoBehaviour
{
    public static AnalyticsBootstrap Instance { get; private set; }

    public bool IsReady { get; private set; }
    public bool InitializationAttempted { get; private set; }
    public string LastInitializationError { get; private set; }

    [Tooltip("When true, events queued before UGS init are sent when init completes.")]
    [SerializeField] private bool flushQueuedEventsOnReady = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureBootstrapExists()
    {
        if (Instance != null)
        {
            return;
        }

        var go = new GameObject("AnalyticsBootstrap");
        DontDestroyOnLoad(go);
        go.AddComponent<AnalyticsBootstrap>();

        var adapter = go.AddComponent<AnalyticsGameplayAdapter>();
        adapter.InitializeIfNeeded();
    }

    private async void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        await InitializeUnityServicesAsync();
    }

    public async Task InitializeUnityServicesAsync()
    {
        if (IsReady || InitializationAttempted)
        {
            return;
        }

        InitializationAttempted = true;

        try
        {
            await UnityServices.InitializeAsync();

            // Start data collection for this session.
            AnalyticsService.Instance.StartDataCollection();
            IsReady = true;
            Debug.Log("[AnalyticsBootstrap] UGS Analytics initialized.");
        }
        catch (Exception ex)
        {
            LastInitializationError = ex.Message;
            IsReady = false;
            Debug.LogWarning($"[AnalyticsBootstrap] Failed to initialize analytics safely: {ex}");
        }

        if (IsReady && flushQueuedEventsOnReady)
        {
            GameAnalyticsService.Instance.FlushQueuedEvents();
        }
    }
}
