using AnchorPoint.Wrapper;
using UnityEditor;
using UnityEngine;

namespace AnchorPoint.EditorHooks
{
    [InitializeOnLoad]
    public class SaveHook
    {
        static SaveHook()
        {
            // Register a callback for asset changes (saves, imports, deletions)
            EditorApplication.projectChanged += OnProjectChanged;
            EditorApplication.focusChanged += OnProjectChanged;
        }

        private static void OnProjectChanged(bool obj)
        {
            CLIWrapper.Status();
            Debug.Log("Project changed: Status command triggered.");
        }

        private static void OnProjectChanged()
        {
            CLIWrapper.Status();
            Debug.Log("Project changed: Status command triggered.");
        }
    }
}