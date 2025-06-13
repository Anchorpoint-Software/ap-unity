using System;
using System.Collections;
using System.Collections.Generic;
using Anchorpoint.Logger;
using UnityEngine;
using Unity.EditorCoroutines.Editor;
using UnityEngine.Networking;
using Anchorpoint.Events;

namespace Anchorpoint.Parser
{
    /// <summary>
    /// Manages CLI data such as status, lock files, current user, and user pictures.
    /// Handles caching and updating logic for UI and system sync with CLI responses.
    /// </summary>
    public static class DataManager
    {
        private static CLIStatus _status;
        private static List<CLIError> errors = new List<CLIError>();
        private static Dictionary<string, string> _lockFiles = new();
        private static CLIUser currentUser;
        private static HashSet<string> outdatedFiles = new HashSet<string>();
        
        private static Dictionary<string, string> emailToPictureUrl = new Dictionary<string, string>();
        private static Dictionary<string, Texture2D> emailToPictureTexture = new Dictionary<string, Texture2D>();
        
        private static List<CLIUser> userList;

        // Returns the latest CLI status information
        public static CLIStatus GetStatus()
        {
            return _status;
        }

        // Adds a new error to the list of CLI errors
        public static void AddError(CLIError error)
        {
            errors.Add(error);
        }

        public static List<CLIError> GetErrors()
        {
            return errors;
        }

        // Clears all stored CLI errors
        public static void ClearErrors()
        {
            errors.Clear();
        }

        // Updates internal data based on the given type, primarily CLIStatus
        public static void UpdateData<T>(T data) where T : class
        {
            switch (data)
            {
                case CLIStatus status:
                    // Update the CLI status
                    _status = status;

                    // Copy locked files if available, otherwise clear the list
                    if (_status.LockedFiles != null)
                    {
                        _lockFiles = new Dictionary<string, string>(_status.LockedFiles);
                        AnchorpointEvents.RaiseStatusUpdated();
                    }
                    else
                    {
                        _lockFiles.Clear();
                    }
                    
                    // Copy outdated files if available, otherwise clear the list
                    if (_status.OutdatedFiles != null)
                    {
                        outdatedFiles = new HashSet<string>(_status.OutdatedFiles);
                        AnchorpointEvents.RaiseStatusUpdated();
                    }
                    else
                    {
                        outdatedFiles.Clear();
                    }
                    
                    break;

                default:
                    AnchorpointLogger.LogError("Unsupported data type in UpdateData.");
                    break;
            }
        }

        // Returns a dictionary of files that are currently locked
        public static Dictionary<string, string> GetLockList()
        {
            return _lockFiles;
        }

        // Sets the current CLI user
        public static void UpdateCurrentUser(CLIUser user)
        {
            currentUser = user;
        }

        // Returns the current CLI user
        public static CLIUser GetCurrentUser()
        {
            return currentUser;
        }
        
        // Stores the user list and maps email addresses to profile picture URLs
        public static void UpdateUserList(List<CLIUser> users)
        {
            userList = users;
            foreach (var user in users)
            {
                if (!string.IsNullOrEmpty(user.Email))
                {
                    emailToPictureUrl[user.Email] = user.Picture;
                }
            }
        }
        
        // Retrieves or downloads the user profile picture from cache or URL
        public static void GetUserPicture(string email, Action<Texture2D> callback)
        {
            if (emailToPictureTexture.TryGetValue(email, out var texture))
            {
                // Picture is already downloaded
                callback(texture);
                return;
            }

            if (emailToPictureUrl.TryGetValue(email, out var url))
            {
                // Start downloading the picture
                EditorCoroutineUtility.StartCoroutineOwnerless(DownloadUserPictureCoroutine(email, url, callback));
            }
            else
            {
                // No picture URL available
                callback(null);
            }
        }
        
        // Coroutine that downloads a user profile picture from the provided URL
        private static IEnumerator DownloadUserPictureCoroutine(string email, string url, Action<Texture2D> callback)
        {
            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(request);
                    emailToPictureTexture[email] = texture;
                    callback(texture);
                }
                else
                {
                    AnchorpointLogger.LogError($"Failed to download picture for {email}: {request.error}");
                    callback(null);
                }
            }
        }

        // Returns the cached list of CLI users
        public static List<CLIUser> GetUserList()
        {
            return userList;
        }
        
        // Returns the list of outdated files detected in the last CLI status check
        public static HashSet<string> GetOutdatedFiles()
        {
            return outdatedFiles;
        }
    }
}