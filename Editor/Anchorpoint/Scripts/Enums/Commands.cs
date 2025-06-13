namespace Anchorpoint.Enums
{
    /// <summary>
    /// Enum representing the set of CLI commands supported by the Anchorpoint plugin.
    /// These commands are mapped to specific operations handled by the CLI interface.
    /// </summary>
    public enum Command
    {
        Status = 0,       // Checks the status of the repository and returns changed files
        Pull   = 1,       // Pulls the latest changes from the remote repository
        Commit = 2,       // Commits staged changes to the local repository
        Push   = 3,       // Pushes local commits to the remote repository
        Sync   = 4,       // Syncs (pull + commit + push) in one go
        UserList = 5,     // Lists users in the current Anchorpoint project
        LockList = 6,     // Lists currently locked files
        LockCreate = 7,   // Creates a lock for one or more files
        LockRemove = 8,   // Removes the lock on specified files
        LogFile    = 9,   // Retrieves log information for a specific file
        Config     = 10,  // Applies configuration settings via a config file
        Revert = 11       // Reverts changes for selected files

        // Connect = 12 not putting here as it was a complete background process
    }
}