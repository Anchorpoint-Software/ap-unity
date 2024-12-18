using Anchorpoint.Logger;
using Anchorpoint.Wrapper;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Anchorpoint.EditorHooks
{
    [InitializeOnLoad]
    public class SaveHook
    {
        static SaveHook()
        {
            EditorSceneManager.sceneSaved += OnProjectChanged;
            EditorApplication.projectChanged += OnProjectAssetsSaved;
            // Optionally deregister on assembly reload or domain unload
            EditorApplication.quitting += DeregisterEvents;
        }

        private static void OnProjectAssetsSaved()
        { 
            // if (!PluginInitializer.IsConnected)
            //     return;
            // CLIWrapper.isWindowActive = false;
            // CLIWrapper.Status();
            // AnchorpointLogger.Log("Project Assets Saved: Status command triggered.");
        }

        private static void OnProjectChanged(Scene scene)
        {
            // if (!PluginInitializer.IsConnected)
            //     return;
            // CLIWrapper.isWindowActive = false;
            // CLIWrapper.Status();
            // AnchorpointLogger.Log("Project changed: Status command triggered.");
        }

        private static void DeregisterEvents()
        { 
            if(!PluginInitializer.IsConnected)
                return;
            EditorSceneManager.sceneSaved -= OnProjectChanged;
            EditorApplication.projectChanged -= OnProjectAssetsSaved;

            AnchorpointLogger.Log("Events deregistered.");
        }
    }
}