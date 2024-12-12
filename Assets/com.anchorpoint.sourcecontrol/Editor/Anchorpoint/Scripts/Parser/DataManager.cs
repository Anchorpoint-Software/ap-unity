using System;
using System.Collections;
using System.Collections.Generic;
using Anchorpoint.Logger;
using UnityEngine;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine.Networking;

namespace Anchorpoint.Parser
{
    public static class DataManager
    {
        private static CLIStatus _status;
        private static List<CLIError> errors = new List<CLIError>();
        private static Dictionary<string, string> _lockFiles = new();
        private static CLIUser currentUser;
        private static HashSet<string> outdatedFiles = new HashSet<string>();
        
        private static Dictionary<string, string> emailToPictureUrl = new Dictionary<string, string>();
        private static Dictionary<string, Texture2D> emailToPictureTexture = new Dictionary<string, Texture2D>();
        
        // Event to notify when status is updated
        public static event Action OnStatusUpdated;
        private static List<CLIUser> userList;

        public static CLIStatus GetStatus()
        {
            return _status;
        }

        public static void AddError(CLIError error)
        {
            errors.Add(error);
        }

        public static List<CLIError> GetErrors()
        {
            return errors;
        }

        public static void ClearErrors()
        {
            errors.Clear();
        }

        public static void UpdateData<T>(T data) where T : class
        {
            switch (data)
            {
                case CLIStatus status:
                    // Update the CLI status
                    _status = status;

                    // Update the lock files from the CLI status
                    if (_status.LockedFiles != null)
                    {
                        _lockFiles = new Dictionary<string, string>(_status.LockedFiles);
                        OnStatusUpdated?.Invoke();
                    }
                    else
                    {
                        _lockFiles.Clear();
                    }
                    
                    // Update outdated files
                    if (_status.OutdatedFiles != null)
                    {
                        outdatedFiles = new HashSet<string>(_status.OutdatedFiles);
                        OnStatusUpdated?.Invoke();
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
        
        public static void UpdateLockList(List<Dictionary<string, string>> lockList)
        {
            _lockFiles.Clear();
            foreach (var lockInfo in lockList)
            {
                if (lockInfo.TryGetValue("filePath", out string filePath) &&
                    lockInfo.TryGetValue("email", out string email))
                {
                    _lockFiles[filePath] = email;
                }
            }
        }

        public static Dictionary<string, string> GetLockList()
        {
            return _lockFiles;
        }

        public static void UpdateCurrentUser(CLIUser user)
        {
            currentUser = user;
        }

        public static CLIUser GetCurrentUser()
        {
            return currentUser;
        }
        
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

        public static List<CLIUser> GetUserList()
        {
            return userList;
        }
        
        public static HashSet<string> GetOutdatedFiles()
        {
            return outdatedFiles;
        }
    }
}