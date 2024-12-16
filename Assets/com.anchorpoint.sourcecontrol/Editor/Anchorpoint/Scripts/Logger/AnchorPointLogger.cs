using System.Runtime.CompilerServices;
using UnityEngine;

namespace Anchorpoint.Logger
{
    public static class AnchorpointLogger
    {
        private static bool EnableLogging = true; // You can toggle this based on environment

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