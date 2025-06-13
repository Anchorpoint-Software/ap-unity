using System.IO;
using UnityEngine;
using System.Linq;
using UnityEditor;
using Anchorpoint.Constants;

namespace Anchorpoint.Scripts.Config
{
    /// <summary>
    /// Provides methods to generate temporary configuration files (.ini) used for invoking
    /// Anchorpoint CLI commands from Unity. Handles file creation and structured writing
    /// based on the requested operation (status, commit, sync, lock, revert).
    /// </summary>
    public static class CLIConfig
    {
        // Escaped directory path to ensure valid formatting in .ini files
        private static string FormattedDirectory => CLIConstants.WorkingDirectory.Replace("\\", "\\\\");
        private static string PersistentDataPath {get; set;}

        // Initialize the path used to store temporary .ini config files
        [InitializeOnLoadMethod]
        private static void SetPersistentDataPath()
        {
            PersistentDataPath = Application.persistentDataPath;
        }

        // Creates or resets the temporary config file path
        private static string CreateConfig()
        {
            string filePath = Path.Combine(PersistentDataPath, "config.ini");

            if(File.Exists(filePath))
                File.Delete(filePath);

            return filePath;
        }

        // Generates config for status operation
        public static string StatusConfig()
        {
            string filePath = CreateConfig();
            using StreamWriter writer = new(filePath);
            // Write necessary parameters to the config file
            writer.WriteLine($"cwd=\"{FormattedDirectory}\"");
            writer.WriteLine("json=true");
            writer.WriteLine("apiVersion=1");
            writer.WriteLine("[status]");
            return filePath;
        }

        // Generates config for commit operation
        public static string CommitConfig(string message, params string[] files)
        {
            string filePath = CreateConfig();
            using StreamWriter writer = new(filePath);
            // Write necessary parameters to the config file
            writer.WriteLine($"cwd=\"{FormattedDirectory}\"");
            writer.WriteLine("json=true");
            writer.WriteLine("apiVersion=1");
            writer.WriteLine("[commit]");
            writer.WriteLine($"message=\"{message}\"");
            string joinedFiles = string.Join(", ", files.Select(f => $"\"{f}\""));
            writer.WriteLine($"files=[{joinedFiles}]");
            return filePath;
        }

        // Generates config for sync operation
        public static string SyncConfig(string message, params string[] files)
        {
            string filePath = CreateConfig();
            using StreamWriter writer = new(filePath);
            // Write necessary parameters to the config file
            writer.WriteLine($"cwd=\"{FormattedDirectory}\"");
            writer.WriteLine("json=true");
            writer.WriteLine("apiVersion=1");
            writer.WriteLine("[sync]");
            writer.WriteLine($"message=\"{message}\"");
            string joinedFiles = string.Join(", ", files.Select(f => $"\"{f}\""));
            writer.WriteLine($"files=[{joinedFiles}]");
            return filePath;
        }

        // Generates config for lock operation
        public static string LockCreateConfig(bool keep, params string[] files)
        {
            string filePath = CreateConfig();
            using StreamWriter writer = new(filePath);
            // Write necessary parameters to the config file
            writer.WriteLine($"cwd=\"{FormattedDirectory}\"");
            writer.WriteLine("json=true");
            writer.WriteLine("apiVersion=1");
            writer.WriteLine("[lock]");
            writer.WriteLine("[lock.create]");
            string joinedFiles = string.Join(", ", files.Select(f => $"\"{f}\""));
            writer.WriteLine($"files=[{joinedFiles}]");
            writer.WriteLine("git=true");
            
            if(keep)
                writer.WriteLine("keep=true");

            return filePath;
        }

        // Generates config for lock remove operation
        public static string LockRemoveConfig(params string[] files)
        {
            string filePath = CreateConfig();

            using StreamWriter writer = new(filePath);
            // Write necessary parameters to the config file
            writer.WriteLine($"cwd=\"{FormattedDirectory}\"");
            writer.WriteLine("json=true");
            writer.WriteLine("apiVersion=1");
            writer.WriteLine("[lock]");
            writer.WriteLine("[lock.remove]");
            string joinedFiles = string.Join(", ", files.Select(f => $"\"{f}\""));
            writer.WriteLine($"files=[{joinedFiles}]");

            return filePath;
        }
        
        // Generates config for revert operation
        public static string RevertConfig(params string[] files)
        {
            string filePath = CreateConfig();
            using StreamWriter writer = new(filePath);
            // Write necessary parameters to the config file
            writer.WriteLine($"cwd=\"{FormattedDirectory}\"");
            writer.WriteLine("json=true");
            writer.WriteLine("apiVersion=1");
            writer.WriteLine("[revert]");

            if (files.Length > 0)
            {
                string joinedFiles = string.Join(", ", files.Select(f => $"\"{f}\""));
                writer.WriteLine($"files=[{joinedFiles}]");
            }
            return filePath;
        }
    }
}