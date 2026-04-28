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
        private const string EnvironmentVariableName = "ANCHORPOINT_ROOT";
        private const string CommandLineArgName = "-anchorpointRoot";
        private const string MacDefaultInstallFolder = "/Applications/Anchorpoint.app/Contents/Frameworks";
        private const string MacDefaultAppBundle = "/Applications/Anchorpoint.app";
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

            string overrideSource;
            string installFolder = GetInstallFolder(out overrideSource);

            if (string.IsNullOrEmpty(installFolder))
            {
                CLIVersion = IsSupportedPlatform() ? "CLI Not Installed!" : "Unsupported OS";
                return;
            }

            string cliExecutableName = (Environment.OSVersion.Platform == PlatformID.Win32NT) ? "ap.exe" : "ap";
            string cliFullPath = Path.Combine(installFolder, cliExecutableName);

            if (!File.Exists(cliFullPath))
            {
                AnchorpointLogger.LogWarning($"Anchorpoint CLI not found at expected path: {cliFullPath}");
                CLIVersion = "CLI Not Installed!";
                return;
            }

            CLIPath = cliFullPath;

            if (overrideSource != null)
            {
                CLIVersion = $"Custom ({overrideSource})";
                AnchorpointLogger.Log($"Using {overrideSource} CLI at: {cliFullPath}");
            }
            else
            {
                CLIVersion = ParseInstalledVersion(installFolder) ?? "Installed";
            }
        }

        // Resolves the install folder containing the Anchorpoint binaries.
        // Resolution order: -anchorpointRoot CLI arg > ANCHORPOINT_ROOT env var > platform default.
        // Returns null if no usable folder is found. The override source (if any) is reported via overrideSource.
        private static string GetInstallFolder(out string overrideSource)
        {
            overrideSource = null;

            string cliRoot = GetCommandLineArgValue(CommandLineArgName);
            if (!string.IsNullOrEmpty(cliRoot))
            {
                if (Directory.Exists(cliRoot))
                {
                    overrideSource = CommandLineArgName;
                    return cliRoot;
                }

                AnchorpointLogger.LogWarning($"{CommandLineArgName} is set to '{cliRoot}' but the directory does not exist; falling back to environment variable / default detection.");
            }

            string envRoot = Environment.GetEnvironmentVariable(EnvironmentVariableName);
            if (!string.IsNullOrEmpty(envRoot))
            {
                if (Directory.Exists(envRoot))
                {
                    overrideSource = EnvironmentVariableName;
                    return envRoot;
                }

                AnchorpointLogger.LogWarning($"{EnvironmentVariableName} is set to '{envRoot}' but the directory does not exist; falling back to default detection.");
            }

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                string basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Anchorpoint");
                if (!Directory.Exists(basePath))
                    return null;

                string pattern = @"app-(\d+\.\d+\.\d+)";
                var versionedDirectories = Directory.GetDirectories(basePath)
                                                    .Where(d => Regex.IsMatch(Path.GetFileName(d), pattern))
                                                    .OrderByDescending(d => Version.Parse(Regex.Match(d, pattern).Groups[1].Value))
                                                    .ToList();
                return versionedDirectories.FirstOrDefault();
            }

            if (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
            {
                return MacDefaultInstallFolder;
            }

            return null;
        }

        // Reads a "-name <value>" pair from the editor's command-line arguments.
        // Unity Hub forwards per-project command-line arguments to the editor; this lets QA
        // pin a CLI/install via Hub settings without touching environment variables.
        private static string GetCommandLineArgValue(string argName)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], argName, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            }
            return null;
        }

        // Parses a version string like "1.18.4" out of an "app-X.Y.Z" install folder name. Returns null if no match.
        private static string ParseInstalledVersion(string installFolder)
        {
            var match = Regex.Match(installFolder ?? string.Empty, @"app-(\d+\.\d+\.\d+)");
            return match.Success ? match.Groups[1].Value : null;
        }

        private static bool IsSupportedPlatform()
        {
            return Environment.OSVersion.Platform == PlatformID.Win32NT
                || Environment.OSVersion.Platform == PlatformID.Unix
                || Environment.OSVersion.Platform == PlatformID.MacOSX;
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
            string overrideSource;
            string installFolder = GetInstallFolder(out overrideSource);

            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                {
                    if (string.IsNullOrEmpty(installFolder))
                    {
                        AnchorpointLogger.LogWarning("Anchorpoint install folder could not be located.");
                        return null;
                    }

                    string exePath = Path.Combine(installFolder, "Anchorpoint.exe");

                    if (!File.Exists(exePath))
                    {
                        AnchorpointLogger.LogWarning($"Anchorpoint.exe not found at expected path: {exePath}");
                        return null;
                    }

                    return exePath;
                }
                case RuntimePlatform.OSXEditor:
                    // The macOS app launch flow expects the .app bundle (used with `open`).
                    // When the install folder is overridden to <bundle>/Contents/Frameworks, walk up two levels to find the bundle.
                    if (overrideSource != null && !string.IsNullOrEmpty(installFolder))
                    {
                        DirectoryInfo dir = new DirectoryInfo(installFolder);
                        if (dir.Name == "Frameworks"
                            && dir.Parent != null && dir.Parent.Name == "Contents"
                            && dir.Parent.Parent != null
                            && string.Equals(dir.Parent.Parent.Extension, ".app", StringComparison.OrdinalIgnoreCase))
                        {
                            return dir.Parent.Parent.FullName;
                        }
                    }
                    return MacDefaultAppBundle;
            }

            return null;
        }
    }
}