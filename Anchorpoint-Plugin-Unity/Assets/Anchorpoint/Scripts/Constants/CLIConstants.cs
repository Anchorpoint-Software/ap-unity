using System;
using System.IO;
using System.Linq;
using UnityEditor;
using System.Text.RegularExpressions;

namespace AnchorPoint.Constants
{
    public static class CLIConstants
    {
        private const string APIVersion = "--apiVersion 1";
        public static string CLIPath { get; private set; } = null;
        public static string CLIVersion { get; private set; } = null;

        /// <summary>
        /// Originally the current working directory will be the Unity project parent directory so this is implemented as that to get the CWD
        /// But in development stage we are using a empty Dummy Unity Project cloned in Anchorpoint so wherever that exists the path for that is manually passed here as the CWD. 
        /// </summary>
        // private static string WorkingDirectory => $"--cwd \"{System.IO.Directory.GetParent(Application.dataPath).FullName}\"";
        // public static string WorkingDirectory => "C:\\Users\\Op\\Documents\\_GitHub\\Unity Project";
        public static string WorkingDirectory => Directory.GetCurrentDirectory();

        private static string CWD => $"--cwd \"{WorkingDirectory}\"";

        public static string Status => $"{CWD} --json {APIVersion} status";

        public static string Pull => $"{CWD} --json {APIVersion} pull";

        public static string CommitAll(string message) => $"{CWD} --json {APIVersion} commit -m \"{message}\"";

        public static string CommitFiles(string message, params string[] files)
        {
            if (files.Length > 5)
                return Config(CLIConfig.CommitConfig(message, files));
            else
            {
                string joinedFiles = string.Join(" ", files.Select(f => $"\"{f}\""));
                return $"{CWD} --json {APIVersion} commit -m \"{message}\" -f{joinedFiles}";
            }
        }

        public static string Push => $"{CWD} --json {APIVersion} push";

        public static string SyncAll(string message) => $"{CWD} --json {APIVersion} sync -m \"{message}\"";

        public static string SyncFiles(string message, params string[] files)
        {
            if (files.Length > 5)
                return Config(CLIConfig.SyncConfig(message, files));
            else
            {
                string joinedFiles = string.Join(" ", files.Select(f => $"\"{f}\""));
                return $"{CWD} --json {APIVersion} sync -m \"{message}\" -f{joinedFiles}";
            }
        }

        public static string UserList => $"{CWD} --json {APIVersion} user list";

        public static string LockList => $"{CWD} --json {APIVersion} lock list";

        public static string LockCreate(bool keep, params string[] files)
        {
            if (files.Length > 5)
            {
                return Config(CLIConfig.LockCreateConfig(keep, files));
            }
            else
            {
                string joinedFiles = string.Join(" ", files.Select(f => $"\"{f}\""));
                return $"{CWD} --json {APIVersion} lock create --git -f {joinedFiles} {(keep ? "--keep" : null)}";
            }
        }

        public static string LockRemove(params string[] files)
        {
            if (files.Length > 5)
            {
                return Config(CLIConfig.LockRemoveConfig(files));
            }
            else
            {
                string joinedFiles = string.Join(" ", files.Select(f => $"\"{f}\""));
                return $"{CWD} --json {APIVersion} lock remove -f {joinedFiles}";
            }
        }

        public static string LogFile(string file, int numberOfCommits) => $"{CWD} --json {APIVersion} log -f \"{file}\" -n {numberOfCommits}";

        public static string Config(string configPath) => $"--config \"{configPath}\"";

        [InitializeOnLoadMethod]
        private static void GetCLIPath()
        {
            CLIPath = null;
            CLIVersion = null;

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                string basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Anchorpoint");
                string pattern = @"app-(\d+\.\d+\.\d+)";
                string cliExecutableName = "ap.exe";

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
                string cliPath = "/Applications/Anchorpoint.app/Contents/Frameworks/ap";

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
    }
}
