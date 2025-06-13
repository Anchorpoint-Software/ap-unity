using Anchorpoint.Constants;
using Anchorpoint.Logger;
using UnityEditor;
using UnityEngine;
using System.Diagnostics;

namespace Anchorpoint.Editor
{
    /// <summary>
    /// Provides context menu functionality for showing selected Unity assets directly in Anchorpoint.
    /// Provides Unity Editor context menu integrations for Anchorpoint.
    /// Adds "Show in Anchorpoint" options under the Assets menu to allow users to open selected files
    /// directly in the Anchorpoint desktop application. Handles platform-specific execution and logging.
    /// </summary>
    public static class AnchorpointContextMenu
    {
        // Opens the selected asset(s) in the Anchorpoint desktop application using the system shell.
        [MenuItem("Assets/Show in Anchorpoint", false, 1000)]
        private static void ShowInAnchorpoint()
        {
            string[] selectedGuids = Selection.assetGUIDs;
            if (selectedGuids.Length == 0)
            {
                AnchorpointLogger.LogWarning("No assets selected.");
                return;
            }

            foreach (string guid in selectedGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string fullPath = System.IO.Path.GetFullPath(assetPath);

                try
                {
                    if (Application.platform == RuntimePlatform.OSXEditor)
                    {
                        Process.Start("open", $"-a \"{ReturnPath()}\" \"{fullPath}\"");
                    }
                    else
                    {
                        Process.Start(ReturnPath(), $"\"{fullPath}\"");
                    }

                    AnchorpointLogger.Log($"Opening file in Anchorpoint: {fullPath}");
                }
                catch (System.Exception ex)
                {
                    AnchorpointLogger.LogError($"Failed to open file in Anchorpoint: {ex.Message}");
                }
            }
        }

        // Enables the menu item only if exactly one asset is selected.
        [MenuItem("Assets/Anchorpoint/Show in Anchorpoint", true)]
        private static bool ShowAnchorpointValidation()
        {
            return Selection.assetGUIDs.Length == 1;
        }

        // Returns the system path to the Anchorpoint executable as defined in CLIConstants.
        private static string ReturnPath()
        {
            return CLIConstants.AnchorpointExecutablePath;
        }
    }
}