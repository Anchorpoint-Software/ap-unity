using System;
using System.Diagnostics;
using System.IO;
using Anchorpoint.Constants;
using Anchorpoint.Logger;
using UnityEngine;

namespace Anchorpoint.Wrapper
{
    /// <summary>
    /// Provides utility methods to check the installation status of the Anchorpoint application
    /// and open it based on the current platform (Windows or macOS).
    /// </summary>
    public static class AnchorpointChecker
    {
        private static readonly string anchorpointExecutablePath = CLIConstants.AnchorpointExecutablePath;

        /// <summary>
        /// Determines whether Anchorpoint is installed on the current operating system.
        /// </summary>
        public static bool IsAnchorpointInstalled()
        {
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                return IsAnchorpointInstalledWindows();
            }
            else if (Application.platform == RuntimePlatform.OSXEditor)
            {
                return IsAnchorpointInstalledMac();
            }

            return false;
        }

        /// <summary>
        /// Checks if Anchorpoint is installed on Windows by verifying the executable path.
        /// </summary>
        private static bool IsAnchorpointInstalledWindows()
        {
            return File.Exists(anchorpointExecutablePath);
        }

        /// <summary>
        /// Checks if Anchorpoint is installed on macOS by verifying the application directory.
        /// </summary>
        private static bool IsAnchorpointInstalledMac()
        {
            return Directory.Exists(anchorpointExecutablePath);
        }

        /// <summary>
        /// Attempts to open the Anchorpoint application based on the platform.
        /// Logs an error if the executable or directory is not found.
        /// </summary>
        public static void OpenAnchorpointApplication()
        {
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                // Construct the expected executable path for Windows
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string exePath = Path.Combine(localAppData, anchorpointExecutablePath);
                if (File.Exists(exePath))
                {
                    Process.Start(exePath);
                }
                else
                {
                    AnchorpointLogger.LogError("Anchorpoint.exe not found on Windows.");
                }
            }
            else if (Application.platform == RuntimePlatform.OSXEditor)
            {
                if (Directory.Exists(anchorpointExecutablePath))
                {
                    // Open the Anchorpoint application using the 'open' command on macOS
                    Process.Start("open", anchorpointExecutablePath);
                }
                else
                {
                    AnchorpointLogger.LogError("Anchorpoint.app not found on macOS.");
                }
            }
            else
            {
                // Log an error for unsupported platforms
                AnchorpointLogger.LogError("Unsupported platform for opening Anchorpoint application.");
            }
        }
    }
}