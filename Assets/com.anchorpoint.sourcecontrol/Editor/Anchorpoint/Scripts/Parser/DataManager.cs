using System.Collections.Generic;
using Anchorpoint.Logger;

namespace Anchorpoint.Parser
{
    public static class DataManager
    {
        private static CLIStatus _status;
        private static List<CLIError> errors = new List<CLIError>();

        private static Dictionary<string, string> _lockFiles = new();
        
        private static CLIUser currentUser;

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
                    }
                    else
                    {
                        _lockFiles.Clear();
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
            AnchorpointLogger.LogError("The current user is: " +currentUser);
        }

        public static CLIUser GetCurrentUser()
        {
            return currentUser;
        }
    }
}