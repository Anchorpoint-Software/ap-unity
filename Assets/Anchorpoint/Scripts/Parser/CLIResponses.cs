using System.Collections.Generic;
using Newtonsoft.Json;

namespace AnchorPoint.Parser
{
    [System.Serializable]
    public class CLIStatus
    {
        [JsonProperty("current_branch")]
        public string CurrentBranch;

        [JsonProperty("staged")]
        public Dictionary<string, string> Staged;

        [JsonProperty("not_staged")]
        public Dictionary<string, string> NotStaged;

        [JsonProperty("locked_files")]
        public Dictionary<string, string> LockedFiles;

        [JsonProperty("outdated_files")]
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