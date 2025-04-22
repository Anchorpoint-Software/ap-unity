using System.Collections.Generic;
using Newtonsoft.Json;

namespace Anchorpoint.Parser
{
    /// <summary>
    /// Represents the status response from the Anchorpoint CLI.
    /// </summary>
    [System.Serializable]
    public class CLIStatus
    {
        [JsonProperty("current_branch")]
        public string CurrentBranch; // Name of the current git branch

        [JsonProperty("staged")]
        public Dictionary<string, string> Staged; // Files staged for commit

        [JsonProperty("not_staged")]
        public Dictionary<string, string> NotStaged; // Files not yet staged

        [JsonProperty("locked_files")]
        public Dictionary<string, string> LockedFiles; // Files currently locked and the user who locked them

        [JsonProperty("outdated_files")]
        public List<string> OutdatedFiles; // Files that are outdated and need sync
    }

    /// <summary>
    /// Represents the progress status during a long-running CLI operation.
    /// </summary>
    [System.Serializable]
    public class CLIProgressStatus
    {
        public string ProgressText; // Description of the current progress
    }

    /// <summary>
    /// Represents a user returned from the CLI.
    /// </summary>
    [System.Serializable]
    public class CLIUser
    {
        [JsonProperty("id")]
        public string Id;

        [JsonProperty("email")]
        public string Email;

        [JsonProperty("name")]
        public string Name;

        [JsonProperty("picture")]
        public string Picture;

        [JsonProperty("level")]
        public string Level;

        [JsonProperty("pending")]
        public string Pending;

        [JsonProperty("current")]
        public string Current;
    }

    /// <summary>
    /// Represents an individual locked file and its associated locker email.
    /// </summary>
    [System.Serializable]
    public class CLILockFile
    {
        public string filePath; // Path of the locked file
        public string email; // Email of the user who locked the file
    }

    /// <summary>
    /// Represents a log entry from git history.
    /// </summary>
    [System.Serializable]
    public class CLILogFile
    {
        public string CommitHash; // Hash of the commit
        public string Author; // Author of the commit
        public long Date; // Timestamp of the commit
        public string Message; // Commit message
    }

    /// <summary>
    /// Represents an error response from the CLI.
    /// </summary>
    [System.Serializable]
    public class CLIError
    {
        public string Error; // Error message
    }
}