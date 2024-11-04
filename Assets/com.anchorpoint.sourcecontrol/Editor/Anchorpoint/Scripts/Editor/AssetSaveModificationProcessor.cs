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
    [InitializeOnLoad]
    public class AssetSaveModificationProcessor : AssetModificationProcessor
    {
        private static string rootRelativePath;
        private static Dictionary<string, string> lockedFiles;
        
        static AssetSaveModificationProcessor()
        {
            // Run the UserList command to get the current user
            CLIWrapper.GetCurrentUser();
        }

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
                // Optionally, you can wait or handle this case appropriately
                AnchorpointLogger.LogWarning("Current user not retrieved yet.");
            }

            string currentUserEmail = currentUser.Email;

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
                            $"{path} is locked by another user and cannot be saved.", "OK");

                        // Prevent the asset from being saved
                        return new string[] { };  
                    }
                }

                // If the file is not locked or locked by the current user, add it to the allowed list
                allowedSavePaths.Add(path);
            }

            // Return only the paths that are allowed to be saved
            return allowedSavePaths.ToArray();
        }
        
        // Refresh the locked files list from CLIWrapper
        private static void RefreshLockedFiles()
        {
            lockedFiles = CLIWrapper.GetLockedFiles();
        }

        private static string GetCommitPath(string path)
        {
            // Calculate the root relative path for commit path conversion
            string projectPath = Directory.GetParent(Application.dataPath).FullName;
            rootRelativePath = projectPath.Substring(CLIConstants.WorkingDirectory.Length).TrimStart(Path.DirectorySeparatorChar);

            // Combine the root relative path with the original path to create the commit path
            string combinedPath = Path.Combine(rootRelativePath, path);

            // Normalize the combined path by replacing backslashes with forward slashes
            string normalizedPath = combinedPath.Replace("\\", "/");

            // Ensure the path is relative, i.e., doesn't have any leading separators
            return normalizedPath.TrimStart('/');
        }
        
        // [OnOpenAsset]
        // public static bool OnOpenAsset(int instanceID, int line)
        // {
        //     string path = AssetDatabase.GetAssetPath(instanceID);
        //
        //     // Refresh the locked files list before checking
        //     RefreshLockedFiles();
        //
        //     // Convert the path dynamically to match the commit path format
        //     string commitPath = GetCommitPath(path);
        //
        //     if (lockedFiles != null && lockedFiles.ContainsKey(commitPath))
        //     {
        //         // Notify the user with the original path, as it is user-facing
        //         EditorUtility.DisplayDialog("Read-Only File",
        //             $"{path} is locked by another user and cannot be opened.", "OK");
        //
        //         // Returning true prevents the asset from being opened
        //         return true;
        //     }
        //
        //     // Returning false allows the asset to be opened as usual
        //     return false;
        // }
    }
}
