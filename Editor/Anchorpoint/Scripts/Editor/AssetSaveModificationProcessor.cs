using System;
using UnityEditor;
using System.Collections.Generic;
using Anchorpoint.Wrapper;
using UnityEngine;
using System.IO;
using Anchorpoint.Constants;
using Anchorpoint.Logger;
using Anchorpoint.Parser;

namespace Anchorpoint.Editor
{
    /// <summary>
    /// Intercepts Unity's asset save pipeline to enforce Anchorpoint file locking.
    /// Prevents users from modifying assets locked by other users and optimistically marks modified assets for UI updates.
    /// Integrates with Anchorpoint's CLI and user data to perform permission checks before saving assets.
    /// </summary>
    public class AssetSaveModificationProcessor : AssetModificationProcessor
    {
        private static string rootRelativePath;
        private static Dictionary<string, string> lockedFiles;

        // This method is automatically called by Unity before assets are saved
        public static string[] OnWillSaveAssets(string[] paths)
        {
            // Refresh the locked files list before checking
            RefreshLockedFiles();

            // Get the current user's email from DataManager
            CLIUser currentUser = DataManager.GetCurrentUser();
                
            // If currentUser is null, the UserList command might not have completed yet
            if (currentUser == null)
            {
                AnchorpointLogger.LogWarning("Current user not retrieved yet.");
            }

            string currentUserEmail = currentUser!=null ? currentUser.Email:"No user found";
            
            // Prepare a list for paths that are allowed to be saved
            List<string> allowedSavePaths = new List<string>();

            foreach (var path in paths)
            {
                // Convert the path dynamically to match the commit path format
                string commitPath = GetCommitPath(path);

                if (lockedFiles != null && lockedFiles.TryGetValue(commitPath, out string lockingUserEmail))
                {
                    // Check if the locking user is not the current user
                    if (!string.Equals(lockingUserEmail, currentUserEmail, StringComparison.OrdinalIgnoreCase))
                    {
                        // Notify the user with a single "OK" button
                        EditorUtility.DisplayDialog("Read-Only File",
                            $"{path} is locked by {GetLockedUserName(lockingUserEmail)} and cannot be saved.", "OK");

                        // Prevent the asset from being saved
                        return new string[] { };  
                    }
                }

                // If the file is not locked or locked by the current user, add it to the allowed list
                allowedSavePaths.Add(path);
                
                // Mark asset as modified in UI this is for the optimistic update for status icon
                AssetStatusIconDrawer.MarkAssetAsModified(commitPath);
            }

            // Return only the paths that are allowed to be saved
            return allowedSavePaths.ToArray();
        }
        
        // Calls CLIWrapper to get the current locked files from Anchorpoint
        private static void RefreshLockedFiles()
        {
            lockedFiles = CLIWrapper.GetLockedFiles();
        }

        // Get locking file user name
        private static string GetLockedUserName(string lockingUserEmail)
        {
            CLIUser cliUser = DataManager.GetUserList().Find(user => user.Email == lockingUserEmail);
            return !string.IsNullOrEmpty(cliUser?.Name) ? cliUser.Name : "Unknown User";
        }

        // Converts Unity asset path to the format used by Anchorpoint for commits
        private static string GetCommitPath(string path)
        {
            // Calculate the root relative path for commit path conversion
            string projectPath = Directory.GetParent(Application.dataPath)?.FullName;
            rootRelativePath = projectPath.Substring(CLIConstants.WorkingDirectory.Length).TrimStart(Path.DirectorySeparatorChar);

            // Combine the root relative path with the original path to create the commit path
            string combinedPath = Path.Combine(rootRelativePath, path);

            // Normalize the combined path by replacing backslashes with forward slashes
            string normalizedPath = combinedPath.Replace("\\", "/");

            // Ensure the path is relative, i.e., doesn't have any leading separators
            return normalizedPath.TrimStart('/');
        }
    }
}
