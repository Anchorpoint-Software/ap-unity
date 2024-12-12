using System;
using System.Diagnostics;
using System.IO;
using Anchorpoint.Logger;
using UnityEngine;

public static class AnchorpointChecker
{
    private const string anchorpointMacAppPath = "/Applications/Anchorpoint.app";
    private const string anchorpointWinAppPath = "Anchorpoint\\Anchorpoint.exe";
    
    public static bool IsAnchorpointInstalled()
    {
        if (Application.platform == RuntimePlatform.WindowsEditor)
        {
            return IsAnchorpointInstalledWindows();
        }
        else if (Application.platform == RuntimePlatform.OSXEditor)
        {
            return IsAnchorpointInstalledMac();
        }

        return false;
    }

    private static bool IsAnchorpointInstalledWindows()
    {
        string cliPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), anchorpointWinAppPath);
        return File.Exists(cliPath);
    }

    private static bool IsAnchorpointInstalledMac()
    {
        return Directory.Exists(anchorpointMacAppPath);
    }

    public static void OpenAnchorpointApplication()
    {
        if (Application.platform == RuntimePlatform.WindowsEditor)
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string exePath = Path.Combine(localAppData, anchorpointWinAppPath);
            if (File.Exists(exePath))
            {
                Process.Start(exePath);
            }
            else
            {
                AnchorpointLogger.LogError("Anchorpoint.exe not found on Windows.");
            }
        }
        else if (Application.platform == RuntimePlatform.OSXEditor)
        {
            if (Directory.Exists(anchorpointMacAppPath))
            {
                Process.Start("open", anchorpointMacAppPath);
            }
            else
            {
                AnchorpointLogger.LogError("Anchorpoint.app not found on macOS.");
            }
        }
        else
        {
            AnchorpointLogger.LogError("Unsupported platform for opening Anchorpoint application.");
        }
    }
}
