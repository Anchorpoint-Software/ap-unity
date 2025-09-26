using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using Anchorpoint.Logger;
using Anchorpoint.Events;
using Anchorpoint.Parser;

namespace Anchorpoint.Wrapper
{
    /// <summary>
    /// Initializes and manages the connection between Unity and the Anchorpoint desktop application.
    /// Handles automatic reconnection, connection state tracking, and message processing from the Anchorpoint CLI.
    /// Provides hooks for Unity Editor state changes and ensures proper communication lifecycle.
    /// </summary>
    [InitializeOnLoad]
    public static class PluginInitializer
    {
        private static ConnectCommandHandler connectHandler;
        private static readonly object handlerLock = new object();
        public static bool IsInitialized => connectHandler != null;
        public static bool IsConnected => connectHandler?.IsConnected() ?? false;
        public static bool IsNotAnchorpointProject { get; private set; } = false;
        public static bool IsCheckingProjectStatus { get; private set; } = true;
        public static bool IsProjectOpen { get; private set; }
        public static bool IsPlaymode { get; private set; }

        private const string WasConnectedKey = "Anchorpoint_WasConnected";
        private const double connectionCheckInterval = 30f;
        private const double projectStatusCheckTimeout = 60f;
        private static double lastConnectionCheckTime;
        private static double projectStatusCheckStartTime;

        public static bool WasConnected
        {
            get => EditorPrefs.GetBool(WasConnectedKey);
            private set => EditorPrefs.SetBool(WasConnectedKey, value);
        }

        static PluginInitializer()
        {
            // Static constructor sets up Unity editor callbacks for connection management and initialization.
            if (!AnchorpointChecker.IsAnchorpointInstalled())
            {
                return;
            }

            EditorApplication.delayCall += Initialize;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += AfterAssemblyReload;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += PeriodicConnectionCheck;
            EditorApplication.update += CheckProjectStatusTimeout;
        }

        private static void Initialize()
        {
            lock (handlerLock)
            {
                // Initializes the connect handler and subscribes to Anchorpoint connection messages.
                if (HasCompilationErrors())
                {
                    StopConnection();
                    AnchorpointLogger.LogError("Compilation errors detected. Disabling Anchorpoint.");
                    return;
                }

                if (connectHandler == null)
                {
                    // Ensure any tracked processes are cleaned up before creating new handler
                    ProcessTracker.KillTrackedProcesses();
                    
                    connectHandler = new ConnectCommandHandler();
                    AnchorpointEvents.OnMessageReceived += OnConnectMessageReceived;

                    // Automatically start checking project status when plugin initializes
                    if (!IsPlaymode)
                    {
                        StartConnection();
                    }
                }
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Automatically reconnects or disconnects based on Unity play mode state.
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                IsPlaymode = false;
                if (WasConnected && !IsConnected)
                {
                    // Reconnect automatically if we were previously connected
                    StartConnection();
                }
                AnchorpointEvents.RaiseRefreshView();
            }
            else if (state == PlayModeStateChange.EnteredPlayMode)
            {
                IsPlaymode = true;
                StopConnection();
                AnchorpointEvents.RaiseRefreshView();
            }
        }

        public static void StartConnection()
        {
            // Sets the connected state and initiates the CLI connection.
            WasConnected = true;
            if (IsCheckingProjectStatus)
            {
                projectStatusCheckStartTime = EditorApplication.timeSinceStartup;
            }
            connectHandler?.StartConnect();
            AnchorpointLogger.Log("Starting Connection");
        }

        public static void TryAgainConnection()
        {
            // Reset states for trying again
            IsNotAnchorpointProject = false;
            IsCheckingProjectStatus = true;
            projectStatusCheckStartTime = EditorApplication.timeSinceStartup;
            AnchorpointEvents.RaiseRefreshView();
            StartConnection();
        }

        private static void StopConnection()
        {
            lock (handlerLock)
            {
                // Stops the CLI connection and logs the action.
                connectHandler?.StopConnect();
                AnchorpointLogger.Log("Connection stopped");
            }
        }

        public static void StopConnectionExt()
        {
            WasConnected = false;
            StopConnection();
        }

        private static void OnBeforeAssemblyReload()
        {
            AnchorpointLogger.Log("Domain reload starting - cleaning up connections");
            
            StopConnection();
            
            // Kill any tracked ap processes before stopping our connection
            ProcessTracker.KillTrackedProcesses();
            
            // Give extra time for cleanup during domain reload
            System.Threading.Thread.Sleep(500);
        }

        private static void AfterAssemblyReload()
        {
            AnchorpointLogger.Log("Domain reload completed - checking reconnection");
            
            // Small delay to ensure any remaining processes have time to clean up
            EditorApplication.delayCall += () =>
            {
                // Reconnect after Unity assembly reload if conditions are met.
                if (!IsConnected && WasConnected && !IsPlaymode)
                {
                    Initialize();
                    StartConnection();
                }
            };
        }

        private static void SetNoProjectState(bool flag)
        {
            IsNotAnchorpointProject = flag;
            IsCheckingProjectStatus = false;
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
            // Processes messages received from Anchorpoint CLI and updates project state accordingly.
            FetchCurrentUser();
            AnchorpointLogger.LogWarning($"Message type {message.type}");

            switch (message.type)
            {
                case "files locked":
                    break;
                case "files unlocked":
                    break;
                case "files outdated":
                    CLIWrapper.Status();
                    break;
                case "files updated":
                    CLIWrapper.Status();    //  There is a conflict so run the Status command
                    break;
                case "project opened":
                    IsProjectOpen = true;
                    SetNoProjectState(false);
                    break;
                case "project closed":
                    IsProjectOpen = false;
                    SetNoProjectState(false);
                    break;
                case "project dirty":
                    SetNoProjectState(false);
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

        private static void FetchCurrentUser()
        {
            // Fetch current user from CLI if not cached already.
            if (DataManager.GetCurrentUser() == null)
            {
                CLIWrapper.GetCurrentUser();
            }
        }

        private static bool HasCompilationErrors()
        {
            // Reads the last few lines of Editor.log to detect compile-time errors.
            string editorLogPath = GetEditorLogPath();
            if (!File.Exists(editorLogPath)) return false;

            try
            {
                using (FileStream fs = new FileStream(editorLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (StreamReader reader = new StreamReader(fs))
                {
                    // Read last few lines instead of the entire file
                    List<string> recentLines = new List<string>();
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (recentLines.Count > 20) // Keep only the last 20 lines
                            recentLines.RemoveAt(0);
                        recentLines.Add(line);
                    }

                    // Check only recent lines for errors
                    return recentLines.Any(l => l.Contains("error CS"));
                }
            }
            catch (IOException ex)
            {
                AnchorpointLogger.LogError($"Failed to read Editor.log: {ex.Message}");
                return false; // Assume no errors if we can't read the file
            }
        }

        private static void PeriodicConnectionCheck()
        {
            // Periodically checks if Unity is disconnected and tries to reconnect if necessary.
            // Get the current time in seconds
            double currentTime = EditorApplication.timeSinceStartup;

            // Check if the interval has passed
            if (currentTime - lastConnectionCheckTime >= connectionCheckInterval)
            {
                lastConnectionCheckTime = currentTime;

                // Perform the connection check
                if (!IsPlaymode && WasConnected && !IsConnected)
                {
                    AnchorpointLogger.LogWarning("Detected disconnection. Attempting to reconnect...");
                    StartConnection();
                }
            }
        }

        private static void CheckProjectStatusTimeout()
        {
            // Check if we've been in checking status too long
            if (IsCheckingProjectStatus && projectStatusCheckStartTime > 0)
            {
                double currentTime = EditorApplication.timeSinceStartup;
                if (currentTime - projectStatusCheckStartTime >= projectStatusCheckTimeout)
                {
                    AnchorpointLogger.LogWarning("Project status check timed out. Assuming no Anchorpoint project.");
                    SetNoProjectState(true);
                    AnchorpointEvents.RaiseRefreshView();
                }
            }
        }

        private static string GetEditorLogPath()
        {
            string editorLogPath = "";

#if UNITY_EDITOR_WIN
    editorLogPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "Unity", "Editor", "Editor.log");

#elif UNITY_EDITOR_OSX
            editorLogPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), "Library", "Logs", "Unity", "Editor.log");
#endif

            // Returns platform-specific path to the Unity Editor log file.
            return editorLogPath;
        }
    }
}