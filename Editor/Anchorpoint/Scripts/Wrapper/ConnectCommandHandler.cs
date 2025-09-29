using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using Anchorpoint.Constants;
using Anchorpoint.Events;
using Anchorpoint.Logger;
using Newtonsoft.Json;
using UnityEditor;

namespace Anchorpoint.Wrapper
{
    /// <summary>
    /// Handles communication with the Anchorpoint CLI for maintaining a live connection from Unity.
    /// Continuously listens for messages and dispatches them to the main thread for UI-safe processing.
    /// </summary>
    public class ConnectCommandHandler
    {
        private Process connectProcess;
        private Thread connectThread;
        private volatile bool isRunning;
        private bool previousConnectionState;
        private static readonly object processLock = new object();
        
        // Queue to pass CLI messages from the background thread to the main Unity thread.
        private static readonly Queue<Action> mainThreadActions = new Queue<Action>();
        private static readonly object queueLock = new object();

        private StringBuilder jsonBuffer = new StringBuilder();
        private int braceCount = 0;

        // Starts the background thread to establish and maintain connection with Anchorpoint CLI.
        public void StartConnect()
        {
            lock (processLock)
            {
                if (isRunning)
                    return;
                
                // Ensure any previous process is fully cleaned up before starting a new one
                if (connectProcess != null)
                {
                    AnchorpointLogger.LogWarning("Previous process still exists during StartConnect, cleaning up...");
                    StopConnect();
                }
                
                connectThread = new Thread(RunConnectProcess) { IsBackground = true };
                connectThread.Start();

                EditorApplication.update += OnEditorUpdate; // Subscribe to tick updates
            }
        }

        // Gracefully stops the background thread and cleans up the process.
        public void StopConnect()
        {
            lock (processLock)
            {
                isRunning = false;

                if (connectProcess != null)
                {
                    int processId = -1;
                    try
                    {
                        processId = connectProcess.Id;
                        
                        // If the process is still running, kill and dispose it
                        if (!connectProcess.HasExited)
                        {
                            AnchorpointLogger.Log($"Terminating ap process with ID: {processId}");
                            connectProcess.Kill();
                            // Give the process time to actually terminate
                            if (!connectProcess.WaitForExit(2000))
                            {
                                AnchorpointLogger.LogWarning("Process did not terminate within 2 seconds");
                            }
                        }
                        
                        // Unregister from tracker since we're cleaning up properly
                        ProcessTracker.UnregisterProcess(processId);
                    }
                    catch (InvalidOperationException ex)
                    {
                        // Process might have already exited or is invalid
                        AnchorpointLogger.Log($"Process cleanup exception (expected): {ex.Message}");
                        // Still try to unregister if we got the process ID
                        if (processId > 0)
                        {
                            ProcessTracker.UnregisterProcess(processId);
                        }
                    }
                    catch (Exception ex)
                    {
                        AnchorpointLogger.LogError($"Unexpected error during process cleanup: {ex.Message}");
                        // Still try to unregister if we got the process ID
                        if (processId > 0)
                        {
                            ProcessTracker.UnregisterProcess(processId);
                        }
                    }
                    finally
                    {
                        // Dispose the process in all cases
                        try
                        {
                            connectProcess.Dispose();
                        }
                        catch (Exception ex)
                        {
                            AnchorpointLogger.LogError($"Error disposing process: {ex.Message}");
                        }
                        connectProcess = null;
                    }
                }

                if (connectThread != null && connectThread.IsAlive)
                {
                    try
                    {
                        // Give the thread a reasonable time to finish
                        if (!connectThread.Join(3000))
                        {
                            AnchorpointLogger.LogWarning("Background thread did not terminate within 3 seconds");
                        }
                    }
                    catch (Exception ex)
                    {
                        AnchorpointLogger.LogError($"Error joining background thread: {ex.Message}");
                    }
                    finally
                    {
                        connectThread = null;
                    }
                }

                EditorApplication.update -= OnEditorUpdate;
            }
        }

        // Runs the Anchorpoint CLI connect process and listens to output streams.
        private void RunConnectProcess()
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = CLIConstants.CLIPath,
                    Arguments = $"--cwd \"{CLIConstants.WorkingDirectory}\" connect --name \"unity\" --pretty",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                connectProcess = new Process { StartInfo = startInfo };
                connectProcess.OutputDataReceived += (sender, e) => OnConnectDataReceived(e.Data);
                connectProcess.ErrorDataReceived += (sender, e) => OnConnectDataReceived(e.Data);

                connectProcess.Start();
                
                // Register the process with our tracker
                ProcessTracker.RegisterProcess(connectProcess.Id, "Unity connect process");
                
                connectProcess.BeginOutputReadLine();
                connectProcess.BeginErrorReadLine();

                connectProcess.WaitForExit();
            }
            catch (Exception ex)
            {
                AnchorpointLogger.LogError($"Error starting connect process: {ex.Message}");
            }
            finally
            {
                isRunning = false;
            }
        }

        // Reconstruct JSON from stream chunks and dispatch parsed messages.
        private void OnConnectDataReceived(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
                return;
            
            isRunning = true;
            jsonBuffer.AppendLine(data);
            foreach (char c in data)
            {
                if (c == '{') braceCount++;
                else if (c == '}') braceCount--;
            }

            if (braceCount == 0 && jsonBuffer.Length > 0)
            {
                string completeJson = jsonBuffer.ToString().Trim();
                jsonBuffer.Clear();

                try
                {
                    var message = JsonConvert.DeserializeObject<ConnectMessage>(completeJson);
                    if (message != null)
                    {
                        lock (queueLock)
                        {
                            mainThreadActions.Enqueue(() => AnchorpointEvents.RaiseMessageReceived(message));
                        }
                    }
                }
                catch (JsonException ex)
                {
                    AnchorpointLogger.LogError($"JSON parsing error: {ex.Message}\nData: {completeJson}");
                }
            }
        }

        // Called every editor frame; executes queued main thread actions and monitors connection state.
        private void OnEditorUpdate()
        {
            if (!isRunning)
                return;

            // Execute queued actions
            lock (queueLock)
            {
                while (mainThreadActions.Count > 0)
                {
                    mainThreadActions.Dequeue()?.Invoke();
                }
            }

            // Check connection state periodically
            if (connectProcess != null)
            {
                bool currentConnectionState = !connectProcess.HasExited;
                if (currentConnectionState != previousConnectionState)
                {
                    previousConnectionState = currentConnectionState;
                }
            }
        }
        
        public bool IsConnected()
        {
            return isRunning && connectProcess != null && !connectProcess.HasExited;
        }
        
    }
}