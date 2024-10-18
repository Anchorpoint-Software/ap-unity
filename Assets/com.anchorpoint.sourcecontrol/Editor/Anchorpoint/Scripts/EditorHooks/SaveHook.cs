using AnchorPoint.Logger;
using AnchorPoint.Wrapper;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace AnchorPoint.EditorHooks
{
    [InitializeOnLoad]
    public class SaveHook
    {
        static SaveHook()
        {
            // Register event handlers
            EditorApplication.projectChanged += OnProjectChanged;
            EditorApplication.focusChanged += OnProjectChanged;
            EditorSceneManager.sceneSaved += OnProjectChanged;

            // Optionally deregister on assembly reload or domain unload
            EditorApplication.quitting += DeregisterEvents;
        }

        private static void OnProjectChanged(Scene scene)
        {
            if (!CLIWrapper.isWindowActive) return;
            CLIWrapper.isWindowActive = false;
            CLIWrapper.Status();
            AnchorPointLogger.Log("Project changed: Status command triggered.");
        }

        private static void OnProjectChanged(bool obj)
        {
            if (!CLIWrapper.isWindowActive) return;
            CLIWrapper.isWindowActive = false;
            CLIWrapper.Status();
            AnchorPointLogger.Log("Project changed: Status command triggered.");
        }

        private static void OnProjectChanged()
        {
            if (!CLIWrapper.isWindowActive) return;
            CLIWrapper.isWindowActive = false;
            CLIWrapper.Status();
            AnchorPointLogger.Log("Project changed: Status command triggered.");
        }

        private static void DeregisterEvents()
        {
            // Deregister event handlers
            EditorApplication.projectChanged -= OnProjectChanged;
            EditorApplication.focusChanged -= OnProjectChanged;
            EditorSceneManager.sceneSaved -= OnProjectChanged;

            AnchorPointLogger.Log("Events deregistered.");
        }
    }
}