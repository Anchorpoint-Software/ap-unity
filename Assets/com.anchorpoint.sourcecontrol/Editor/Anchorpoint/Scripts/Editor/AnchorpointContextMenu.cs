using Anchorpoint.Constants;
using Anchorpoint.Logger;
using UnityEditor;
using UnityEngine;
using System.Diagnostics;

namespace Anchorpoint.Editor
{
    public static class AnchorpointContextMenu
    {
        [MenuItem("Assets/Anchorpoint/Show in Anchorpoint", false, 1000)]
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
                        Process.Start(ReturnPath(), fullPath);
                    }

                    AnchorpointLogger.Log($"Opening file in Anchorpoint: {fullPath}");
                }
                catch (System.Exception ex)
                {
                    AnchorpointLogger.LogError($"Failed to open file in Anchorpoint: {ex.Message}");
                }
            }
        }

        [MenuItem("Assets/Anchorpoint/Show in Anchorpoint", true)]
        private static bool ShowAnchorpointValidation()
        {
            return Selection.assetGUIDs.Length == 1;
        }

        private static string ReturnPath()
        {
            return CLIConstants.AnchorpointExecutablePath;
        }
    }
}