using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Anchorpoint.Parser;
using System.IO;
using Anchorpoint.Constants;

namespace Anchorpoint.Editor
{
    [InitializeOnLoad]
    public class AssetStatusIconDrawer
    {
        private static Dictionary<string, string> stagedFiles;
        private static Dictionary<string, string> notStagedFiles;
        private static Dictionary<string, string> lockedFiles;
        private static HashSet<string> outdatedFiles;

        private static Dictionary<string, Texture2D> assetPathToIcon = new Dictionary<string, Texture2D>();

        private const string addIcon = "2354d2ae9e6644355b13247fe7bcf803";
        private const string modifyIcon = "f0d42548e9ac042fe8debb31f645886d";
        private const string lockMeIcon = "2b87ed061126442709ab8f989fdf1783";

        private const string outdatedIcon = "952dff34c8a314eebbaecdab2e41d89e";
        private const string conflictIcon = "5b2f35ebbd6964f6da7714f1096d5ff3";

        private static string rootRelativePath;

        static AssetStatusIconDrawer()
        {
            // Subscribe to the projectWindowItemOnGUI event
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemOnGUI;

            // Subscribe to status updates
            DataManager.OnStatusUpdated += OnStatusUpdated;

            // Initial data fetch
            RefreshStatusData();
        }

        private static void OnProjectWindowItemOnGUI(string guid, Rect selectionRect)
        {
            // Get the asset path from the GUID
            string path = AssetDatabase.GUIDToAssetPath(guid);

            // Normalize the path to match the format in the DataManager
            string commitPath = GetCommitPath(path);

            Texture2D icon = null;

            if (assetPathToIcon.TryGetValue(commitPath, out icon))
            {
                // Icon is already cached
            }
            else
            {
                // Determine the status of the asset
                string status = null;

                if (stagedFiles != null && stagedFiles.TryGetValue(commitPath, out string stagedStatus))
                {
                    status = stagedStatus;
                }
                else if (notStagedFiles != null && notStagedFiles.TryGetValue(commitPath, out string notStagedStatus))
                {
                    status = notStagedStatus;
                }

                if (lockedFiles != null && lockedFiles.TryGetValue(commitPath, out string lockingUserEmail))
                {
                    string currentUserEmail = DataManager.GetCurrentUser()?.Email;

                    if (!string.IsNullOrEmpty(currentUserEmail))
                    {
                        if (string.Equals(lockingUserEmail, currentUserEmail, StringComparison.OrdinalIgnoreCase))
                        {
                            // The file is locked by the current user
                            icon = LoadIcon(lockMeIcon);
                            assetPathToIcon[commitPath] = icon;
                        }
                        else
                        {
                            // The file is locked by someone else
                            // Get the user's picture
                            DataManager.GetUserPicture(lockingUserEmail, (texture) =>
                            {
                                if (texture != null)
                                {
                                    assetPathToIcon[commitPath] = texture;
                                    // Repaint the Project window
                                    EditorApplication.RepaintProjectWindow();
                                }
                                else
                                {
                                    // Use a default icon if picture is not available
                                    Texture2D defaultIcon = LoadIcon(lockMeIcon);
                                    assetPathToIcon[commitPath] = defaultIcon;
                                    EditorApplication.RepaintProjectWindow();
                                }
                            });

                            // Use a placeholder icon until the picture is loaded
                            icon = LoadIcon(lockMeIcon);
                            assetPathToIcon[commitPath] = icon;
                        }
                    }
                }
                else if (outdatedFiles != null && outdatedFiles.Contains(commitPath))
                {
                    // Load the 'Outdated' icon
                    icon = LoadIcon(outdatedIcon);
                    assetPathToIcon[commitPath] = icon;
                }
                else if (status == "A")
                {
                    // Load the 'A' (Added) icon
                    icon = LoadIcon(addIcon);
                    assetPathToIcon[commitPath] = icon;
                }
                else if (status == "M")
                {
                    // Load the 'M' (Modified) icon
                    icon = LoadIcon(modifyIcon);
                    assetPathToIcon[commitPath] = icon;
                }
                else if (status == "C")
                {
                    // Load the 'C' (Conflict) icon
                    icon = LoadIcon(conflictIcon);
                    assetPathToIcon[commitPath] = icon;
                }
            }

            // Draw the icon next to the asset
            if (icon != null)
            {
                float iconSize = 20f;
                float padding = 2f;

                // Adjust the iconRect to be positioned at the top right corner
                Rect iconRect = new Rect(
                    selectionRect.x + selectionRect.width - iconSize - padding,
                    selectionRect.y + padding,  // Positioning at the top of the asset
                    iconSize,
                    iconSize
                );

                GUI.DrawTexture(iconRect, icon);
            }
        }

        private static Texture2D LoadIcon(string GUID)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(GUID);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        private static string GetCommitPath(string path)
        {
            // Calculate the root relative path for commit path conversion
            string projectPath = Directory.GetParent(Application.dataPath).FullName;
            if (string.IsNullOrEmpty(rootRelativePath))
            {
                rootRelativePath = projectPath.Substring(CLIConstants.WorkingDirectory.Length).TrimStart(Path.DirectorySeparatorChar);
            }

            // Combine the root relative path with the original path to create the commit path
            string combinedPath = Path.Combine(rootRelativePath, path);

            // Normalize the combined path by replacing backslashes with forward slashes
            string normalizedPath = combinedPath.Replace("\\", "/");

            // Ensure the path is relative, i.e., doesn't have any leading separators
            return normalizedPath.TrimStart('/');
        }

        private static void RefreshStatusData()
        {
            // Get the latest status data from the DataManager
            CLIStatus status = DataManager.GetStatus();

            if (status != null)
            {
                stagedFiles = status.Staged;
                notStagedFiles = status.NotStaged;
                lockedFiles = status.LockedFiles;
                outdatedFiles = DataManager.GetOutdatedFiles();
            }
            else
            {
                stagedFiles = null;
                notStagedFiles = null;
                lockedFiles = null;
                outdatedFiles = null;
            }
        }

        private static void OnStatusUpdated()
        {
            // Refresh data when status updates
            RefreshStatusData();
            // Repaint the Project window to reflect updated icons
            EditorApplication.RepaintProjectWindow();
        }
    }
}