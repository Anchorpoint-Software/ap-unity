using System.Collections.Generic;

namespace AnchorPoint.Parser
{
    public static class DataManager
    {
        private static CLIStatus _status;
        private static List<CLIError> errors = new List<CLIError>();

        private static Dictionary<string, string> _lockFiles = new();

        public static CLIStatus GetStatus()
        {
            return _status;
        }

        // public static void UpdateCurrentStatus(CLIStatus status)
        // {
        //     currentStatus = status;
        // }

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

        // public static void ResetStatus()
        // {
        //     currentStatus = null;
        // }

        public static void UpdateData<T>(T data) where T : class
        {
            switch (data)
            {
                case CLIStatus staus:
                    _status = staus;
                break;
                case List<Dictionary<string, string>> temp:
                    _lockFiles = new();
                    foreach (Dictionary<string, string> fileData in temp)
                    {
                        foreach (KeyValuePair<string, string> entry in fileData)
                        {
                            _lockFiles.Add(entry.Key, entry.Value);
                        }
                    }
                    break;
            }
        }
        
        public static Dictionary<string, string> GetLockList()
        {
            return _lockFiles;
        }
    }
}