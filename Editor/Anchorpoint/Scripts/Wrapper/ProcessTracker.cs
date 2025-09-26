using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using Anchorpoint.Logger;

namespace Anchorpoint.Wrapper
{
    /// <summary>
    /// Universal helper class for tracking and managing ap connect processes across domain reloads.
    /// Uses EditorPrefs for persistent tracking.
    /// </summary>
    public static class ProcessTracker
    {
        private const string EDITOR_PREFS_KEY = "AnchorpointProcessTracker_ProcessIds";
        private const char SEPARATOR = ',';
        
        /// <summary>
        /// Registers a new ap connect process for tracking.
        /// </summary>
        /// <param name="processId">The process ID to track</param>
        /// <param name="description">Optional description for logging</param>
        public static void RegisterProcess(int processId, string description = "")
        {
            try
            {
                // Store in EditorPrefs for persistence across Unity restarts
                var existingIds = GetTrackedProcessIds();
                if (!existingIds.Contains(processId))
                {
                    existingIds.Add(processId);
                    SaveTrackedProcessIds(existingIds);
                }
                
                AnchorpointLogger.Log($"ProcessTracker: Registered process {processId} ({description})");
            }
            catch (Exception ex)
            {
                AnchorpointLogger.LogError($"ProcessTracker: Failed to register process {processId}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Unregisters a process from tracking (call when process is properly stopped).
        /// </summary>
        /// <param name="processId">The process ID to unregister</param>
        public static void UnregisterProcess(int processId)
        {
            try
            {
                // Remove from EditorPrefs
                var existingIds = GetTrackedProcessIds();
                if (!existingIds.Remove(processId)) return;
                
                SaveTrackedProcessIds(existingIds);
                AnchorpointLogger.Log($"ProcessTracker: Unregistered process {processId}");
            }
            catch (Exception ex)
            {
                AnchorpointLogger.LogError($"ProcessTracker: Failed to unregister process {processId}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Kills all tracked ap processes that are still running.
        /// This is the universal solution that works on all platforms.
        /// </summary>
        /// <returns>Number of processes killed</returns>
        public static int KillTrackedProcesses()
        {
            var killedCount = 0;
            
            try
            {
                var trackedIds = GetTrackedProcessIds();
                var processesToRemove = new List<int>();
                
                foreach (var processId in trackedIds)
                {
                    try
                    {
                        var process = Process.GetProcessById(processId);
                        
                        // Verify this is actually an ap process (additional safety check)
                        if (process.ProcessName.ToLower().Contains("ap"))
                        {
                            AnchorpointLogger.Log($"ProcessTracker: Killing tracked ap process {processId}");
                            process.Kill();
                            
                            // Wait for the process to exit
                            if (process.WaitForExit(2000))
                            {
                                killedCount++;
                                AnchorpointLogger.Log($"ProcessTracker: Successfully killed process {processId}");
                            }
                            else
                            {
                                AnchorpointLogger.LogWarning($"ProcessTracker: Process {processId} did not exit within timeout");
                            }
                        }
                        else
                        {
                            AnchorpointLogger.LogWarning($"ProcessTracker: Process {processId} is not an ap process (skipping), shouldn't happen");
                        }
                        
                        process.Dispose();
                        processesToRemove.Add(processId);
                    }
                    catch (ArgumentException)
                    {
                        // Process doesn't exist anymore - this is expected and good
                        processesToRemove.Add(processId);
                    }
                    catch (InvalidOperationException)
                    {
                        // Process has already exited
                        processesToRemove.Add(processId);
                    }
                    catch (Exception ex)
                    {
                        AnchorpointLogger.LogError($"ProcessTracker: Error handling process {processId}: {ex.Message}");
                        // Still remove it from tracking since we can't handle it
                        processesToRemove.Add(processId);
                    }
                }
                
                // Clean up tracking for processes we've handled
                foreach (var processId in processesToRemove)
                {
                    UnregisterProcess(processId);
                }
                
                if (killedCount > 0)
                {
                    AnchorpointLogger.Log($"ProcessTracker: Killed {killedCount} tracked processes");
                }
                else if (trackedIds.Count > 0)
                {
                    AnchorpointLogger.Log($"ProcessTracker: Cleaned up {processesToRemove.Count} stale process references");
                }
            }
            catch (Exception ex)
            {
                AnchorpointLogger.LogError($"ProcessTracker: Error during process cleanup: {ex.Message}");
            }
            
            return killedCount;
        }
        
        /// <summary>
        /// Gets all currently tracked process IDs.
        /// </summary>
        /// <returns>List of tracked process IDs</returns>
        private static List<int> GetTrackedProcessIds()
        {
            try
            {
                var idsString = EditorPrefs.GetString(EDITOR_PREFS_KEY, "");
                if (string.IsNullOrEmpty(idsString))
                {
                    return new List<int>();
                }
                
                return idsString.Split(SEPARATOR)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => int.TryParse(s.Trim(), out var id) ? id : -1)
                    .Where(id => id > 0)
                    .ToList();
            }
            catch (Exception ex)
            {
                AnchorpointLogger.LogError($"ProcessTracker: Error reading tracked process IDs: {ex.Message}");
                return new List<int>();
            }
        }
        
        /// <summary>
        /// Saves the list of tracked process IDs to EditorPrefs.
        /// </summary>
        /// <param name="processIds">List of process IDs to save</param>
        private static void SaveTrackedProcessIds(List<int> processIds)
        {
            try
            {
                var idsString = string.Join(SEPARATOR.ToString(), processIds.Select(id => id.ToString()));
                EditorPrefs.SetString(EDITOR_PREFS_KEY, idsString);
            }
            catch (Exception ex)
            {
                AnchorpointLogger.LogError($"ProcessTracker: Error saving tracked process IDs: {ex.Message}");
            }
        }
    }
}
