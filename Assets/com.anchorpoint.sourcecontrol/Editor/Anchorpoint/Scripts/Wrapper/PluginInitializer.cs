using System;
using UnityEditor;
using Anchorpoint.Wrapper;
using Anchorpoint.Logger;
using System.Timers;

[InitializeOnLoad]
public static class PluginInitializer
{
    private static ConnectCommandHandler connectHandler;
    private static Timer statusPollingTimer;
    public static bool IsInitialized => connectHandler != null;
    public static bool IsConnected => connectHandler?.IsConnected() ?? false;
    public static bool IsNotAnchorpointProject { get; private set; }
    public static event Action RefreshView;

    private const string WasConnectedKey = "Anchorpoint_WasConnected";

    static PluginInitializer()
    {
        EditorApplication.delayCall += Initialize;
        AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void Initialize()
    {
        if (connectHandler == null)
        {
            connectHandler = new ConnectCommandHandler();
            connectHandler.OnMessageReceived += OnConnectMessageReceived;
        }

        // After reloading assemblies, if we were previously connected, reconnect
        // bool wasConnected = EditorPrefs.GetBool(WasConnectedKey, false);
        // if (wasConnected && !IsConnected)
        // {
        //     StartConnection();
        // }
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // When Unity returns to Edit mode from Play mode, re-check connection state
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            bool wasConnected = EditorPrefs.GetBool(WasConnectedKey, false);
            if (wasConnected && !IsConnected)
            {
                // Reconnect automatically if we were previously connected
                StartConnection();
            }
        }
    }

    public static void StartConnection()
    {
        connectHandler.StartConnect();
        AnchorpointLogger.Log("Start Connection");
        EditorPrefs.SetBool(WasConnectedKey, true);
    }

    private static void StopConnection()
    {
        connectHandler?.StopConnect();
        StopStatusPolling();
        AnchorpointLogger.Log("Connection stopped");
    }

    public static void StopConnectionExt()
    {
        StopConnection();
        EditorPrefs.SetBool(WasConnectedKey, false);
    }

    private static void OnBeforeAssemblyReload()
    {
        StopConnection();
    }

    private static void SetNoProjectState(bool flag)
    {
        IsNotAnchorpointProject = flag;
        if (flag)
        {
            StopConnection();
        }
    }

    private static void OnConnectMessageReceived(ConnectMessage message)
    {
        HandleConnectMessage(message);
    }

    private static void HandleConnectMessage(ConnectMessage message)
    {
        switch (message.type)
        {
            case "files locked":
                break;
            case "files unlocked":
                break;
            case "files outdated":
                break;
            case "files updated":
                break;
            case "project opened":
                SetNoProjectState(false);
                StopStatusPolling();
                break;
            case "project closed":
                SetNoProjectState(false);
                StartStatusPolling();
                break;
            case "project dirty":
                CLIWrapper.Status();
                break;
            default:
                AnchorpointLogger.LogError("Default state has been called");
                SetNoProjectState(true);
                break;
        }
        RefreshView?.Invoke();
        EditorApplication.RepaintProjectWindow();
    }

    private static void StartStatusPolling()
    {
        if (statusPollingTimer == null)
        {
            statusPollingTimer = new Timer(5 * 60 * 1000); // 5 minutes
            statusPollingTimer.Elapsed += (sender, e) => CLIWrapper.Status();
        }
        statusPollingTimer.Start();
    }

    private static void StopStatusPolling()
    {
        statusPollingTimer?.Stop();
    }
}
