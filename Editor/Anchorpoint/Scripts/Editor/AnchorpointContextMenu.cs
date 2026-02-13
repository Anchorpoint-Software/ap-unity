using Anchorpoint.Logger;
using UnityEditor;

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
            var selectedGuids = Selection.assetGUIDs;
            if (selectedGuids.Length == 0)
            {
                AnchorpointLogger.LogWarning("No assets selected.");
                return;
            }

            foreach (var guid in selectedGuids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var fullPath = System.IO.Path.GetFullPath(assetPath);
                AnchorpointFileOpener.OpenInAnchorpoint(fullPath);
            }
        }

        // Enables the menu item only if exactly one asset is selected.
        [MenuItem("Assets/Anchorpoint/Show in Anchorpoint", true)]
        private static bool ShowAnchorpointValidation()
        {
            return Selection.assetGUIDs.Length == 1;
        }
    }
}