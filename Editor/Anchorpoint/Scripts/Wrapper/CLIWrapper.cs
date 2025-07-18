using System;
using Anchorpoint.Enums;
using System.Diagnostics;
using Anchorpoint.Parser;
using Anchorpoint.Constants;
using System.Collections.Generic;
using System.Threading;
using Anchorpoint.Events;
using Anchorpoint.Logger;
using UnityEditor;

namespace Anchorpoint.Wrapper
{
    /// <summary>
    /// Static wrapper class that manages CLI communication between Unity and the Anchorpoint application.
    /// Handles queuing, command execution, and parsing of results.
    /// </summary>
    public delegate void Callback();

    public static class CLIWrapper
    {
        // Accumulates the output received from CLI command executions
        private static string Output { get; set; }
        private static CommandQueue _commandQueue = new CommandQueue();

        // Determines whether the Status command should be queued after another CLI operation
        public static bool isStatusQueuedAfterCommand = false;  // Track when Status should be queued
        private static bool isRefreshing = false;                // Flag for refresh control
        private static readonly Queue<Action> refreshQueue = new Queue<Action>();  // Queue to manage refresh actions

        public static bool isWindowActive = false;

        // Logs the CLI executable path used by Anchorpoint
        public static void CLIPath() => AnchorpointLogger.Log(CLIConstants.CLIPath);

        // Issues the status command to update the file tracking state from the CLI
        public static void Status()
        {
            if (isStatusQueuedAfterCommand)
            {
                // Queue status to run after other commands complete
                _commandQueue.EnqueueCommand(RunStatusCommandOnBackgroundThread);
            }
            else
            {
                RunStatusCommandOnBackgroundThread();
            }
        }

        private static void RunStatusCommandOnBackgroundThread()
        {
            Thread statusThread = new Thread(() =>
                {
                    RunCommand(Command.Status, CLIConstants.Status);
                })
            { IsBackground = true };

            statusThread.Start();
        }

        public static void GetCurrentUser()
        {
            EditorApplication.update += RunUserListCommandOnMainThread;
        }

        private static void RunUserListCommandOnMainThread()
        {
            // Ensure this runs once
            EditorApplication.update -= RunUserListCommandOnMainThread;

            // Execute the UserList command on the main thread
            RunCommand(Command.UserList, CLIConstants.UserList);
        }

        public static void Pull() => EnqueueCommand(Command.Pull, CLIConstants.Pull, true);

        public static void CommitAll(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                Output = string.Empty;
                AnchorpointLogger.LogWarning("Commit Message empty!");
                AddOutput($"\n\n<color=red>Commit Message empty!</color>");
            }
            else
            {
                EnqueueCommand(Command.Commit, CLIConstants.CommitAll(message), true);
            }
        }

        public static void Commit(string message, params string[] files)
        {
            if (string.IsNullOrEmpty(message))
            {
                Output = string.Empty;
                AnchorpointLogger.LogWarning("Commit Message empty!");
                AddOutput($"\n\n<color=red>Commit Message empty!</color>");
            }
            else
            {
                EnqueueCommand(Command.Commit, CLIConstants.CommitFiles(message, files), true);
            }
        }

        public static void Push() => EnqueueCommand(Command.Push, CLIConstants.Push, true);

        public static void SyncAll(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                Output = string.Empty;
                AnchorpointLogger.LogWarning("Commit Message empty!");
                AddOutput($"\n\n<color=red>Commit Message empty!</color>");
            }
            else
            {
                EnqueueCommand(Command.Sync, CLIConstants.SyncAll(message), true);
            }
        }

        public static void Sync(string message, params string[] files)
        {
            if (string.IsNullOrEmpty(message))
            {
                Output = string.Empty;
                AnchorpointLogger.LogWarning("Commit Message empty!");
                AddOutput($"\n\n<color=red>Commit Message empty!</color>");
                message = "Commit Message empty!";
            }

            EnqueueCommand(Command.Sync, CLIConstants.SyncFiles(message, files), true);
        }

        public static void Revert(Callback callback = null, params string[] files)
        {
            EnqueueCommand(Command.Revert, CLIConstants.RevertFiles(files), true, callback);
        }

        // public static void LockList() => EnqueueCommand(Command.LockList, CLIConstants.LockList);

        public static void LockCreate(bool keep, params string[] files)
        {
            RunCommand(Command.LockList, CLIConstants.LockList, false, () =>
            {
                List<string> fileList = new(files);

                foreach (string file in files)
                {
                    if (DataManager.GetLockList().ContainsKey(file))
                    {
                        AnchorpointLogger.Log($"File {file} was already locked!");
                        fileList.Remove(file);
                    }
                }

                if (fileList.Count > 0)
                    RunCommand(Command.LockCreate, CLIConstants.LockCreate(keep, fileList.ToArray()));
                else
                    AnchorpointLogger.Log("No files to Lock!");
            });
        }

        public static void LockRemove(params string[] files)
        {
            RunCommand(Command.LockList, CLIConstants.LockList, false, () =>
            {
                List<string> fileList = new();

                foreach (string file in files)
                {
                    if (DataManager.GetLockList().ContainsKey(file))
                    {
                        fileList.Add(file);
                    }
                }

                if (fileList.Count > 0)
                    RunCommand(Command.LockRemove, CLIConstants.LockRemove(fileList.ToArray()));
                else
                    AnchorpointLogger.Log("No files to Unlock!");
            });
        }

        public static Dictionary<string, string> GetLockedFiles()
        {
            return DataManager.GetLockList();
        }

        public static void LogFile(string file, int numberOfCommits = 5) => RunCommand(Command.LogFile, CLIConstants.LogFile(file, numberOfCommits));

        private static void AddOutput(string data) => Output += data;

        private static void EnqueueCommand(Command command, string commandText, bool sequential = false, Callback callback = null)
        {
            // Flag to queue Status command only after all commands complete
            isStatusQueuedAfterCommand = true;
            _commandQueue.EnqueueCommand(() =>
            {
                RunCommand(command, commandText, sequential, callback);
            });
        }

        // The main method that runs the command, now excluding the Status command from threaded logic
        private static void RunCommand(Command command, string commandText, bool sequential = false, Callback callback = null)
        {
            Output = string.Empty;
            AnchorpointLogger.Log($"Running Command: {commandText}");
            AddOutput($"<color=green>Running Command: {commandText}</color>");

            // Prepare and launch the CLI process with appropriate command arguments
            try
            {
                ProcessStartInfo startInfo = new()
                {
                    FileName = CLIConstants.CLIPath,
                    Arguments = commandText,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // On Windows, use Windows-1252 encoding to properly handle special characters (ä, ö, ü, etc.)
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    startInfo.StandardOutputEncoding = System.Text.Encoding.GetEncoding(1252);
                    startInfo.StandardErrorEncoding = System.Text.Encoding.GetEncoding(1252);
                }

                using Process process = new() { StartInfo = startInfo };
                process.Start();

                // Decide between async (sequential) and sync error reading
                if (sequential)
                {
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            AnchorpointLogger.Log($"Output: {e.Data}");
                            AnchorpointEvents.RaiseCommandOutputReceived($"{e.Data}");
                            AddOutput($"\n\nOutput:\n{e.Data}");
                        }
                    };
                    process.BeginErrorReadLine();
                }
                else
                {
                    string errorOutput = process.StandardError.ReadToEnd();
                    if (!string.IsNullOrEmpty(errorOutput))
                    {
                        AnchorpointLogger.Log($"Output: {errorOutput}");
                        AnchorpointEvents.RaiseCommandOutputReceived($"Output: {errorOutput}");
                        AddOutput($"\n\nOutput:\n{errorOutput}");
                        ProcessOutput(command, errorOutput, callback);
                    }
                }

                process.WaitForExit();
                AnchorpointLogger.Log($"{command} Command Completed");
                AnchorpointEvents.RaiseCommandOutputReceived($"{command} Command Completed");
                AddOutput($"\n\n{command} Command Completed");
                
                if (sequential)
                {
                    callback?.Invoke();
                }

                QueueRefresh(command);  // Handle RefreshWindow logic here
            }
            catch (Exception ex)
            {
                AnchorpointLogger.LogError($"Error running command: {ex.Message}");
            }
        }

        // Queues a refresh call to be executed after a command finishes
        private static void QueueRefresh(Command command)
        {
            // Add the refresh action to the queue
            refreshQueue.Enqueue(() =>
            {
                EditorApplication.delayCall += () =>
                {
                    if (command == Command.Status)
                    {
                        AnchorpointEvents.RaiseRefreshTreeWindow();
                    }
                };
            });

            // If no refresh is currently running, process the next one
            if (!isRefreshing)
            {
                ProcessNextRefresh();
            }
        }

        // Executes refresh calls sequentially with delay to avoid overlapping updates
        private static void ProcessNextRefresh()
        {
            if (refreshQueue.Count == 0)
            {
                isRefreshing = false;
                return;
            }

            isRefreshing = true;

            // Dequeue and run the next refresh action
            var nextRefresh = refreshQueue.Dequeue();
            if (nextRefresh != null)
            {
                nextRefresh.Invoke();
            }

            // Set a delay before allowing the next refresh to be processed
            EditorApplication.delayCall += () =>
            {
                isRefreshing = false;
                ProcessNextRefresh();
            };
        }

        // Parses the structured JSON output returned by the CLI based on the command type
        private static void ProcessOutput(Command command, string jsonOutput, Callback callback)
        {
            if (jsonOutput.Contains("\"error\":\"No Project\""))
            {
                AnchorpointLogger.LogError("No project found on Anchorpoint");
                return;
            }
            switch (command)
            {
                case Command.Status:
                    CLIStatus status = CLIJsonParser.ParseJson<CLIStatus>(jsonOutput);
                    if (status != null)
                    {
                        DataManager.UpdateData(status);
                        callback?.Invoke();
                    }
                    else
                    {
                        AnchorpointLogger.LogError("Failed to parse output as CLI Status or output was empty.");
                    }
                    break;

                case Command.LockList:
                    List<Dictionary<string, string>> fileDataList = CLIJsonParser.ParseJson<List<Dictionary<string, string>>>(jsonOutput);
                    if (fileDataList != null)
                    {
                        DataManager.UpdateData(fileDataList);
                        callback?.Invoke();
                    }
                    else
                    {
                        AnchorpointLogger.LogError("Failed to parse output as CLILockFile or output was empty.");
                    }
                    break;

                case Command.UserList:
                    List<CLIUser> users = CLIJsonParser.ParseJson<List<CLIUser>>(jsonOutput);
                    if (users != null)
                    {
                        DataManager.UpdateUserList(users);
                        CLIUser currentUser = users.Find(user => user.Current == "1");
                        if (currentUser != null)
                        {
                            DataManager.UpdateCurrentUser(currentUser);
                            callback?.Invoke();
                        }
                        else
                        {
                            AnchorpointLogger.LogError("No current user found in user list.");
                        }
                    }
                    else
                    {
                        AnchorpointLogger.LogError("Failed to parse output as CLIUser or output was empty.");
                    }
                    break;
            }
        }
    }
}