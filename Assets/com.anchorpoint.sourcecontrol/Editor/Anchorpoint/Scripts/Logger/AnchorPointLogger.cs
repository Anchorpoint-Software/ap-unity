using System.Runtime.CompilerServices;
using UnityEngine;

namespace Anchorpoint.Logger
{
    /// <summary>
    /// Centralized logging utility for the Anchorpoint Unity plugin. Logs messages to the Unity Console
    /// with contextual metadata (file, method, line number) when logging is enabled.
    /// </summary>
    public static class AnchorpointLogger
    {
        /// Global toggle to enable or disable logging throughout the Anchorpoint plugin.
        public static bool EnableLogging = false; // You can toggle this based on environment

        /// Logs a standard debug message to the Unity console with contextual source information.
        public static void Log(string message,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0,
            [CallerMemberName] string methodName = "")
        {
            if (EnableLogging)
            {
                string fileName = System.IO.Path.GetFileName(filePath);
                Debug.Log($"[{fileName}:{lineNumber}] {methodName}: {message}");
            }
        }

        /// Logs a warning message to the Unity console with contextual source information.
        public static void LogWarning(string message,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0,
            [CallerMemberName] string methodName = "")
        {
            if (EnableLogging)
            {
                string fileName = System.IO.Path.GetFileName(filePath);
                Debug.LogWarning($"[{fileName}:{lineNumber}] {methodName}: {message}");
            }
        }

        /// Logs an error message to the Unity console with contextual source information.
        public static void LogError(string message,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0,
            [CallerMemberName] string methodName = "")
        {
            if (EnableLogging)
            {
                string fileName = System.IO.Path.GetFileName(filePath);
                Debug.LogError($"[{fileName}:{lineNumber}] {methodName}: {message}");
            }
        }
    }
}