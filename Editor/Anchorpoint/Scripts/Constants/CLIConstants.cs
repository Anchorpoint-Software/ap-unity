using System;
using System.IO;
using System.Linq;
using UnityEditor;
using System.Text.RegularExpressions;
using Anchorpoint.Logger;
using UnityEngine;
using Anchorpoint.Scripts.Config;

namespace Anchorpoint.Constants
{
    /// <summary>
    /// Provides CLI command templates and utility methods for interacting with the Anchorpoint CLI within Unity.
    /// This includes file commit, revert, lock, and sync operations, as well as dynamic detection of CLI path and executable.
    /// </summary>
    public static class CLIConstants
    {
        private const string APIVersion = "--apiVersion 1";
        private const string cliPath = "/Applications/Anchorpoint.app/Contents/Frameworks/ap";
        public static string CLIPath {get; private set;} = null;
        private static string CLIVersion {get; set;} = null;

        public static string AnchorpointExecutablePath => GetAnchorpointExecutablePath();

        // Dynamically determines the working directory by locating the nearest .gitignore file
        /// <summary>
        /// Originally the current working directory will be the Unity project parent directory so this is implemented as that to get the CWD 
        /// </summary>
        public static string WorkingDirectory => FindGitIgnore(Directory.GetParent(Application.dataPath).FullName);

        private static string CWD => $"--cwd \"{WorkingDirectory}\"";

        // Constructs a CLI command for status, optionally using a config file for large file sets
        public static string Status => $"{CWD} --json {APIVersion} status";

        // Constructs a CLI command for pull, optionally using a config file for large file sets
        public static string Pull => $"{CWD} --json {APIVersion} pull";

        // Constructs a CLI command for commit all, optionally using a config file for large file sets
        public static string CommitAll(string message) => $"{CWD} --json {APIVersion} commit -m \"{message}\"";

        // Constructs a CLI command for commit files, optionally using a config file for large file sets
        public static string CommitFiles(string message, params string[] files)
        {
            if(files.Length > 5)
                return Config(CLIConfig.CommitConfig(message, files));
            else
            {
                string joinedFiles = string.Join(" ", files.Select(f => $"\"{f}\""));
                return $"{CWD} --json {APIVersion} commit -m \"{message}\" -f{joinedFiles}";
            } 
        }

        // Constructs a CLI command for push, optionally using a config file for large file sets
        public static string Push => $"{CWD} --json {APIVersion} push";

        // Constructs a CLI command for sync all, optionally using a config file for large file sets
        public static string SyncAll(string message) => $"{CWD} --json {APIVersion} sync -m \"{message}\"";

        // Constructs a CLI command for sync files, optionally using a config file for large file sets
        public static string SyncFiles(string message, params string[] files)
        {
            if(files.Length > 5)
                return Config(CLIConfig.SyncConfig(message, files));
            else
            {
                string joinedFiles = string.Join(" ", files.Select(f => $"\"{f}\""));
                return $"{CWD} --json {APIVersion} sync -m \"{message}\" -f{joinedFiles}";
            } 
        }
        
        // Constructs a CLI command for reverting files, optionally using a config file for large file sets
        public static string RevertFiles(params string[] files)
        {
            switch (files.Length)
            {
                case 0:
                    // Revert all files in case of no file is selected
                    return $"{CWD} --json {APIVersion} revert";
                case > 5:
                    // Use config file for large number of files
                    return Config(CLIConfig.RevertConfig(files));
                default:
                {
                    // Revert specified files
                    string joinedFiles = string.Join(" ", files.Select(f => $"\"{f}\""));
                    return $"{CWD} --json {APIVersion} revert --files {joinedFiles}";
                }
            }
        }

        // CLI command to retrieve user list
        public static string UserList => $"{CWD} --json {APIVersion} user list";

        // CLI command to retrieve lock list
        public static string LockList => $"{CWD} --json {APIVersion} lock list";

        // Constructs a CLI command for creating locks, optionally using a config file for large file sets
        public static string LockCreate(bool keep, params string[] files)
        {
            if(files.Length > 5)
            {
                return Config(CLIConfig.LockCreateConfig(keep, files));
            }
            else
            {
                string joinedFiles = string.Join(" ", files.Select(f => $"\"{f}\""));
                return $"{CWD} --json {APIVersion} lock create --git -f {joinedFiles} {(keep ? "--keep" : null)}";
            }
        }

        // Constructs a CLI command for removing locks, optionally using a config file for large file sets
        public static string LockRemove(params string[] files)
        {
            if(files.Length > 5)
            {
                return Config(CLIConfig.LockRemoveConfig(files));
            }
            else
            {
                string joinedFiles = string.Join(" ", files.Select(f => $"\"{f}\""));
                return $"{CWD} --json {APIVersion} lock remove -f {joinedFiles}";
            }
        }

        // Constructs a CLI command for retrieving log file information
        public static string LogFile(string file, int numberOfCommits) => $"{CWD} --json {APIVersion} log -f \"{file}\" -n {numberOfCommits}";

        // Wraps the given config path in CLI --config syntax
        private static string Config(string configPath) => $"--config \"{configPath}\"";

        // Detects and caches the Anchorpoint CLI executable path based on the OS and installation directory
        [InitializeOnLoadMethod]
        private static void GetCLIPath()
        {
            CLIPath    = null;
            CLIVersion = null;

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                string basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Anchorpoint");
                string pattern = @"app-(\d+\.\d+\.\d+)";
                string cliExecutableName = "ap.exe";

                // Get the most recent version of the CLI by sorting versioned subdirectories
                var versionedDirectories = Directory.GetDirectories(basePath)
                                                    .Where(d => Regex.IsMatch(Path.GetFileName(d), pattern)) // Filter directories by the pattern
                                                    .OrderByDescending(d => Version.Parse(Regex.Match(d, pattern).Groups[1].Value)) // Sort by parsed version
                                                    .ToList();

                if (versionedDirectories.Any())
                {
                    string latestVersionPath = versionedDirectories.First();
                    string latestVersion = Regex.Match(latestVersionPath, pattern).Groups[1].Value;
                    string cliPath = Path.Combine(latestVersionPath, cliExecutableName);

                    if (File.Exists(cliPath))
                    {
                        CLIPath = cliPath;
                        CLIVersion = latestVersion;
                    }
                    else
                    {
                        CLIVersion = "CLI Not Installed!";
                    }
                }
            }
            else if (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
            {
                if (File.Exists(cliPath))
                {
                    CLIPath = cliPath;
                    CLIVersion = "macOS CLI Installed";
                }
                else
                {
                    CLIVersion = "CLI Not Installed!";
                }
            }
            else
            {
                CLIVersion = "Unsupported OS";
            }
        }
        
        private static string FindGitIgnore(string startPath)
        {
            // Check for .gitignore in the Unity root, then in parent directories
            // Check the starting directory (Unity project root)
            if (File.Exists(Path.Combine(startPath, ".gitignore")))
            {
                return startPath;
            }

            // Go up one level and check
            string oneLevelUp = Directory.GetParent(startPath)?.FullName;
            if (!string.IsNullOrEmpty(oneLevelUp) && File.Exists(Path.Combine(oneLevelUp, ".gitignore")))
            {
                return oneLevelUp;
            }

            // Go up another level and check
            string twoLevelsUp = Directory.GetParent(oneLevelUp)?.FullName;
            if (!string.IsNullOrEmpty(twoLevelsUp) && File.Exists(Path.Combine(twoLevelsUp, ".gitignore")))
            {
                return twoLevelsUp;
            }

            return null;
        }
        
        // Identify available versions of Anchorpoint from subdirectories and return the latest executable path
        private static string GetAnchorpointExecutablePath()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                {
                    string basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Anchorpoint");

                    // Ensure the directory exists before proceeding
                    if (!Directory.Exists(basePath))
                    {
                        AnchorpointLogger.LogWarning($"Anchorpoint directory not found: {basePath}");
                        return null;
                    }

                    string pattern = @"app-(\d+\.\d+\.\d+)";
                    string executableName = "Anchorpoint.exe";

                    // Identify available versions of Anchorpoint from subdirectories and return the latest executable path
                    var versionedDirectories = Directory.GetDirectories(basePath)
                        .Where(d => Regex.IsMatch(Path.GetFileName(d), pattern))
                        .OrderByDescending(d => Version.Parse(Regex.Match(d, pattern).Groups[1].Value))
                        .ToList();

                    if (!versionedDirectories.Any())
                    {
                        AnchorpointLogger.LogWarning("No Anchorpoint versions found in: " + basePath);
                        return null;
                    }

                    string latestVersionPath = versionedDirectories.First();
                    string latestVersion = Regex.Match(latestVersionPath, pattern).Groups[1].Value;
                    string exePath = Path.Combine(latestVersionPath, executableName);

                    if (!File.Exists(exePath))
                    {
                        AnchorpointLogger.LogWarning($"Anchorpoint.exe not found at expected path: {exePath}");
                        return null;
                    }

                    return exePath;
                }
                case RuntimePlatform.OSXEditor:
                    return "/Applications/Anchorpoint.app";
            }

            return null;
        }
    }
}