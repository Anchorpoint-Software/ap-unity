using System.IO;
using UnityEngine;
using System.Linq;
using UnityEditor;
using Anchorpoint.Constants;

public static class CLIConfig
{
    private static string FormattedDirectory => CLIConstants.WorkingDirectory.Replace("\\", "\\\\");
    private static string PersistentDataPath {get; set;}

    [InitializeOnLoadMethod]
    private static void SetPersistentDataPath()
    {
        PersistentDataPath = Application.persistentDataPath;
    }

    private static string CreateConfig()
    {
        string filePath = Path.Combine(PersistentDataPath, "config.ini");

        if(File.Exists(filePath))
            File.Delete(filePath);

        return filePath;
    }

    public static string StatusConfig()
    {
        string filePath = CreateConfig();
        using StreamWriter writer = new(filePath);
        writer.WriteLine($"cwd=\"{FormattedDirectory}\"");
        writer.WriteLine("json=true");
        writer.WriteLine("apiVersion=1");
        writer.WriteLine("[status]");
        return filePath;
    }

    public static string CommitConfig(string message, params string[] files)
    {
        string filePath = CreateConfig();
        using StreamWriter writer = new(filePath);
        writer.WriteLine($"cwd=\"{FormattedDirectory}\"");
        writer.WriteLine("json=true");
        writer.WriteLine("apiVersion=1");
        writer.WriteLine("[commit]");
        writer.WriteLine($"message=\"{message}\"");
        string joinedFiles = string.Join(", ", files.Select(f => $"\"{f}\""));
        writer.WriteLine($"files=[{joinedFiles}]");
        return filePath;
    }

    public static string SyncConfig(string message, params string[] files)
    {
        string filePath = CreateConfig();
        using StreamWriter writer = new(filePath);
        writer.WriteLine($"cwd=\"{FormattedDirectory}\"");
        writer.WriteLine("json=true");
        writer.WriteLine("apiVersion=1");
        writer.WriteLine("[sync]");
        writer.WriteLine($"message=\"{message}\"");
        string joinedFiles = string.Join(", ", files.Select(f => $"\"{f}\""));
        writer.WriteLine($"files=[{joinedFiles}]");
        return filePath;
    }

    public static string LockCreateConfig(bool keep, params string[] files)
    {
        string filePath = CreateConfig();
        using StreamWriter writer = new(filePath);
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

    public static string LockRemoveConfig(params string[] files)
    {
        string filePath = CreateConfig();

        using StreamWriter writer = new(filePath);
        writer.WriteLine($"cwd=\"{FormattedDirectory}\"");
        writer.WriteLine("json=true");
        writer.WriteLine("apiVersion=1");
        writer.WriteLine("[lock]");
        writer.WriteLine("[lock.remove]");
        string joinedFiles = string.Join(", ", files.Select(f => $"\"{f}\""));
        writer.WriteLine($"files=[{joinedFiles}]");

        return filePath;
    }
    
    public static string RevertConfig(params string[] files)
    {
        string filePath = CreateConfig();
        using StreamWriter writer = new(filePath);
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
