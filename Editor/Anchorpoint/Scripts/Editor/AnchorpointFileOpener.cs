using System.Diagnostics;
using Anchorpoint.Constants;
using Anchorpoint.Logger;
using UnityEngine;

namespace Anchorpoint.Editor
{
    /// <summary>
    /// A static helper class for opening files in the Anchorpoint desktop application.
    /// Handles platform-specific process execution (Windows and macOS) with error handling and logging.
    /// </summary>
    public static class AnchorpointFileOpener
    {
        /// <summary>
        /// Opens a file in the Anchorpoint desktop application.
        /// </summary>
        /// <param name="fullPath">The full system path to the file to open.</param>
        public static void OpenInAnchorpoint(string fullPath)
        {
            try
            {
                if (Application.platform == RuntimePlatform.OSXEditor)
                {
                    Process.Start("open", $"-a \"{CLIConstants.AnchorpointExecutablePath}\" \"{fullPath}\"");
                }
                else
                {
                    Process.Start(CLIConstants.AnchorpointExecutablePath, $"\"{fullPath}\"");
                }

                AnchorpointLogger.Log($"Opening in Anchorpoint: {fullPath}");
            }
            catch (System.Exception ex)
            {
                AnchorpointLogger.LogError($"Failed to open in Anchorpoint: {ex.Message}");
            }
        }
    }
}
