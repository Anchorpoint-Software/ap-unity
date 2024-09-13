using System.Threading;
using AnchorPoint.Enums;
using System.Diagnostics;
using AnchorPoint.Parser;
using AnchorPoint.Constants;
using Debug = UnityEngine.Debug;
using System.Collections.Generic;

namespace AnchorPoint.Wrapper
{
    public delegate void Callback();

    public static class CLIWrapper
    {   
        public static string Output{get; set;}

        public static void CLIPath() => Debug.Log(CLIConstants.CLIPath);

        public static void Status() => RunCommand(Command.Status, CLIConstants.Status);

        public static void Pull() => RunCommand(Command.Pull, CLIConstants.Pull, true);

        public static void CommitAll(string message)
        {
            if(string.IsNullOrEmpty(message))
            {
                Output = string.Empty;
                Debug.LogWarning("Commit Message empty!");
                AddOutput($"\n\n<color=red>Commit Message empty!</color>");
            }
            else
                RunCommand(Command.Commit, CLIConstants.CommitAll(message), true);
        }

        public static void Push() => RunCommand(Command.Push, CLIConstants.Push, true);
        
        public static void SyncAll(string message)
        {
            if(string.IsNullOrEmpty(message))
            {
                Output = string.Empty;
                Debug.LogWarning("Commit Message empty!");
                AddOutput($"\n\n<color=red>Commit Message empty!</color>");
            }   
            else
                RunCommand(Command.Sync, CLIConstants.SyncAll(message), true);
        }

        public static void UserList() => RunCommand(Command.UserList, CLIConstants.UserList);

        public static void LockList() => RunCommand(Command.LockList, CLIConstants.LockList);

        public static void LockCreate(bool keep, params string[] files)
        {
            RunCommand(Command.LockList, CLIConstants.LockList, false,()=>{
                
                List<string> fileList = new(files);

                foreach (string file in files)
                {
                    if (DataManager.GetLockList().ContainsKey(file))
                    {
                        Debug.Log($"File {file} was already locked!");
                        fileList.Remove(file);
                    }
                }

                if(fileList.Count > 0)
                    RunCommand(Command.LockCreate, CLIConstants.LockCreate(keep, fileList.ToArray()));
                else
                    Debug.Log("No files to Lock!");
            });
        }

        public static void LockRemove(params string[] files)
        {
            RunCommand(Command.LockList, CLIConstants.LockList, false,()=>{
                
                List<string> fileList = new();

                foreach (string file in files)
                {
                    if (DataManager.GetLockList().ContainsKey(file))
                    {
                        fileList.Add(file);
                    }
                }

                if(fileList.Count > 0)
                    RunCommand(Command.LockRemove, CLIConstants.LockRemove(fileList.ToArray()));
                else
                    Debug.Log("No files to Unlock!");
            });
        }

        public static void LogFile(string file, int numberOfCommits = 5) => RunCommand(Command.LogFile, CLIConstants.LogFile(file, numberOfCommits));   

        private static void AddOutput(string data) => Output += data;

        private static void RunCommand(Command command, string commandText, bool sequential = false, Callback callback = null)
        {
            Output = string.Empty;
            Debug.Log($"Running Command: {commandText}");
            AddOutput($"<color=green>Running Command: {commandText}</color>");

            // Run the command in a separate thread
            Thread commandThread = new(() =>
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

                using Process process = new();
                process.StartInfo = startInfo;

                if(sequential)
                {
                    process.ErrorDataReceived += (sender, e) => 
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            Debug.Log($"Output: {e.Data}");
                            AddOutput($"\n\nOutput:\n{e.Data}");
                        }
                    };

                    process.Start();
                    process.BeginErrorReadLine();
                    process.WaitForExit();
                }
                else
                {
                    process.Start();
                    string errorOutput = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(errorOutput))
                    {
                        Debug.Log($"Output: {errorOutput}");
                        AddOutput($"\n\nOutput:\n{errorOutput}");
                        ProcessOutput(command, errorOutput, callback);
                    }
                }

                Debug.Log($"{command} Command Completed");
                AddOutput($"\n\n{command} Command Completed");
            });

            commandThread.Start();
        }

        private static void ProcessOutput(Command command, string jsonOutput, Callback callback)
        {
            switch(command)
            {
                case Command.Status:
                    CLIStatus status = CLIJsonParser.ParseJson<CLIStatus>(jsonOutput);

                    if(status != null)
                    {
                        DataManager.UpdateData(status);
                        callback?.Invoke();
                    }
                    else
                    {
                        Debug.LogError("Failed to parse output as CLI Status or output was empty.");
                    }
                    break;
                case Command.LockList:
                    List<Dictionary<string, string>> fileDataList = CLIJsonParser.ParseJson<List<Dictionary<string, string>>>(jsonOutput);

                    if(fileDataList != null)
                    {   
                        DataManager.UpdateData(fileDataList);
                        callback?.Invoke();
                    }
                    else
                    {
                        Debug.LogError("Failed to parse output as CLILockFile or output was empty.");
                    }
                    break;
            }
        }
    }
}
