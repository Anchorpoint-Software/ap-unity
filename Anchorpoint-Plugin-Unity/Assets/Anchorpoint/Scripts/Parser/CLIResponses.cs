using System.Collections.Generic;

namespace AnchorPoint.Parser
{
    [System.Serializable]
    public class CLIStatus
    {
        public string CurrentBranch;
        public Dictionary<string, string> Staged;
        public Dictionary<string, string> NotStaged;
        public Dictionary<string, string> LockedFiles;
        public List<string> OutdatedFiles;
    }

    [System.Serializable]
    public class CLIProgressStatus
    {
        public string ProgressText;
    }

    [System.Serializable]
    public class CLIUser
    {
        public string Id;
        public string Email;
        public string Name;
        public string Picture;
        public string Level;
        public string Pending;
    }

    [System.Serializable]
    public class CLILockFile
    {
        public string filePath;
        public string email;
    }

    [System.Serializable]
    public class CLILogFile
    {
        public string CommitHash;
        public string Author;
        public long Date;
        public string Message;
    }

    [System.Serializable]
    public class CLIError
    {
        public string Error;
    }
}