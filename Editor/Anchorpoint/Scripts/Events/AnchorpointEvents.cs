using System;
using Anchorpoint.Logger;
using Anchorpoint.Wrapper;

namespace Anchorpoint.Events
{
    /// <summary>
    /// Contains global event definitions and invokers used throughout the Anchorpoint Unity integration.
    /// These events manage status updates, tree refreshes, command outputs, and connection messages.
    /// </summary>
    public static class AnchorpointEvents
    {
        // Flag used to temporarily block event invocation when operations are ongoing
        public static bool inProgress = false;
        
        // Event triggered when the CLI status is updated
        public static event Action OnStatusUpdated;
        // Raises the OnStatusUpdated event if not inProgress
        public static void RaiseStatusUpdated()
        {
            if (inProgress)
                return;
            AnchorpointLogger.Log("Raise Status Updated Called");
            OnStatusUpdated?.Invoke();
        }
        
        // Event triggered to refresh the file tree UI
        public static event Action RefreshTreeWindow;
        // Raises the RefreshTreeWindow event if not inProgress
        public static void RaiseRefreshTreeWindow()
        {
            if (inProgress)
                return;
            AnchorpointLogger.Log("Raise Refresh Window Called");
            RefreshTreeWindow?.Invoke();
        }
        
        // Event triggered when CLI outputs command results
        public static event Action<string> OnCommandOutputReceived;
        // Raises the OnCommandOutputReceived event with CLI output
        public static void RaiseCommandOutputReceived(string str)
        {
            AnchorpointLogger.Log("Raise Command Output Called");
            OnCommandOutputReceived?.Invoke(str);
        }
        
        // Event triggered when a connection message is received from the Anchorpoint process
        public static event Action<ConnectMessage> OnMessageReceived;
        // Raises the OnMessageReceived event with the connection message
        public static void RaiseMessageReceived(ConnectMessage message)
        {
            OnMessageReceived?.Invoke(message);
        }
        
        // Event triggered to refresh the plugin's main UI view
        public static event Action RefreshView;
        // Raises the RefreshView event if not inProgress
        public static void RaiseRefreshView()
        {
            if (inProgress)
                return;   
            AnchorpointLogger.Log("Raise Refresh View Called");
            RefreshView?.Invoke();
        }
    }
}
