using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using Anchorpoint.Constants;
using Anchorpoint.Logger;
using Newtonsoft.Json;
using UnityEditor;

namespace Anchorpoint.Wrapper
{
    public class ConnectCommandHandler
    {
        private Process connectProcess;
        private Thread connectThread;
        private volatile bool isRunning;
        private bool previousConnectionState;
        
        // Thread-safe queue to store actions to be executed on the main thread
        private static readonly Queue<Action> mainThreadActions = new Queue<Action>();
        private static readonly object queueLock = new object();

        public event Action<ConnectMessage> OnMessageReceived;
        public event Action<bool> ConnectionStateChanged; // Notify UI about connection state

        private StringBuilder jsonBuffer = new StringBuilder();
        private int braceCount = 0;

        public void StartConnect()
        {
            if (isRunning)
                return;

            isRunning = true;
            connectThread = new Thread(RunConnectProcess) { IsBackground = true };
            connectThread.Start();

            EditorApplication.update += OnEditorUpdate; // Subscribe to tick updates
        }

        public void StopConnect()
        {
            isRunning = false;

            if (connectProcess != null)
            {
                try
                {
                    // If the process is still running, kill and dispose it
                    if (!connectProcess.HasExited)
                    {
                        connectProcess.Kill();
                    }
                }
                catch (InvalidOperationException)
                {
                    // Process might have already exited or is invalid,
                    // just ignore and proceed
                }
                finally
                {
                    // Dispose the process in all cases
                    connectProcess.Dispose();
                    connectProcess = null;
                }
            }

            if (connectThread != null && connectThread.IsAlive)
            {
                connectThread.Join();
                connectThread = null;
            }

            EditorApplication.update -= OnEditorUpdate;
        }

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

        private void OnConnectDataReceived(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
                return;

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
                            mainThreadActions.Enqueue(() => OnMessageReceived?.Invoke(message));
                        }
                    }
                }
                catch (JsonException ex)
                {
                    AnchorpointLogger.LogError($"JSON parsing error: {ex.Message}\nData: {completeJson}");
                }
            }
        }

        private void OnEditorUpdate()
        {
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
                    ConnectionStateChanged?.Invoke(currentConnectionState);
                    previousConnectionState = currentConnectionState;
                }
            }
        }
        
        public bool IsConnected()
        {
            return isRunning && connectProcess != null && !connectProcess.HasExited;
        }
        
        // public bool IsConnected()
        // {
        //     // If connectProcess is null or has been disposed, you cannot check HasExited
        //     if (connectProcess == null) return false;
        //
        //     // If the process was killed or stopped, handle that gracefully
        //     try
        //     {
        //         return isRunning && connectProcess != null && !connectProcess.HasExited;
        //     }
        //     catch (InvalidOperationException)
        //     {
        //         // The process is no longer associated, so not connected
        //         return false;
        //     }
        // }
    }

    [Serializable]
    public class ConnectMessage
    {
        public string id;
        public string type;
        public List<string> files;
    }
}