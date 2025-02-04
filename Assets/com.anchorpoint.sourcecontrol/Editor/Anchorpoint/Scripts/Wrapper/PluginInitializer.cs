using System.IO;
using UnityEditor;
using Anchorpoint.Logger;
using System.Timers;
using Anchorpoint.Events;
using Anchorpoint.Parser;

namespace Anchorpoint.Wrapper
{
    [InitializeOnLoad]
    public static class PluginInitializer
    {
        private static ConnectCommandHandler connectHandler;
        private static Timer statusPollingTimer;
        public static bool IsInitialized => connectHandler != null;
        public static bool IsConnected => connectHandler?.IsConnected() ?? false;
        public static bool IsNotAnchorpointProject { get; private set; }
        public static bool IsProjectOpen { get; private set; }
        public static bool IsPlaymode { get; private set; }
        private const string WasConnectedKey = "Anchorpoint_WasConnected";
        
        private static double lastConnectionCheckTime = 0;
        private const double connectionCheckInterval = 60.0; // Check every 60 seconds

        public static bool WasConnected
        {
            get => EditorPrefs.GetBool(WasConnectedKey);
            private set => EditorPrefs.SetBool(WasConnectedKey, value);
        }

        static PluginInitializer()
        {
            EditorApplication.delayCall += Initialize;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += AfterAssemblyReload;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void Initialize()
        {
            if (HasCompilationErrors())
            {
                StopConnection();
                AnchorpointLogger.LogError("Compilation errors detected. Disabling Anchorpoint.");
                return;
            }
            
            if (connectHandler == null)
            {
                connectHandler = new ConnectCommandHandler();
                AnchorpointEvents.OnMessageReceived += OnConnectMessageReceived;
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // When Unity returns to Edit mode from Play mode, re-check connection state
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                IsPlaymode = false;
                if (WasConnected && !IsConnected)
                {
                    // Reconnect automatically if we were previously connected
                    StartConnection();
                }
            }
            else if (state == PlayModeStateChange.EnteredPlayMode)
            {
                IsPlaymode = true;
                StopConnection();
            }
        }

        public static void StartConnection()
        {
            WasConnected = true;
            connectHandler?.StartConnect();
            AnchorpointLogger.Log("Starting Connection");
        }

        private static void StopConnection()
        {
            connectHandler?.StopConnect();
            StopStatusPolling();
            AnchorpointLogger.Log("Connection stopped");
        }

        public static void StopConnectionExt()
        {
            WasConnected = false;
            StopConnection();
        }

        private static void OnBeforeAssemblyReload()
        {
            StopConnection();
        }

        private static void AfterAssemblyReload()
        {
            if (!IsConnected && WasConnected && !IsPlaymode)
            {
                Initialize();
                StartConnection();
            }
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
            FetchCurrentUser();
            switch (message.type)
            {
                case "files locked":
                    break;
                case "files unlocked":
                    break;
                case "files outdated":
                    break;
                case "files updated":
                    CLIWrapper.Status();    //  There is a conflict so run the Status command
                    break;
                case "project opened":
                    IsProjectOpen = true;
                    SetNoProjectState(false);
                    StopStatusPolling();
                    break;
                case "project closed":
                    IsProjectOpen = false;
                    SetNoProjectState(false);
                    StartStatusPolling();
                    break;
                case "project dirty":
                    CLIWrapper.Status();
                    break;
                case "":
                    //  do nothing
                    break;
                default:
                    SetNoProjectState(true);
                    break;
            }

            AnchorpointEvents.RaiseRefreshView();
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

        private static void FetchCurrentUser()
        {
            if (DataManager.GetCurrentUser() == null)
            {
                CLIWrapper.GetCurrentUser();
            }
        }

        private static bool HasCompilationErrors()
        {
            string editorLogPath = GetEditorLogPath();
    
            if (!File.Exists(editorLogPath)) return false;

            string logContents = File.ReadAllText(editorLogPath);
            return logContents.Contains("error CS"); // Checks for C# compiler errors
        }

        private static string GetEditorLogPath()
        {
            string editorLogPath = "";

#if UNITY_EDITOR_WIN
    editorLogPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "Unity", "Editor", "Editor.log");

#elif UNITY_EDITOR_OSX
            editorLogPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), "Library", "Logs", "Unity", "Editor.log");
#endif

            return editorLogPath;
        }
    }
}
