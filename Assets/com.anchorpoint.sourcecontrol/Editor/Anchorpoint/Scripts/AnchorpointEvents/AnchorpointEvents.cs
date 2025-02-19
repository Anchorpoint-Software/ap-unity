using System;
using Anchorpoint.Logger;
using Anchorpoint.Wrapper;

namespace Anchorpoint.Events
{
    public static class AnchorpointEvents
    {
        public static bool inProgress = false;
        
        public static event Action OnStatusUpdated;
        public static void RaiseStatusUpdated()
        {
            if (inProgress)
                return;
            AnchorpointLogger.Log("Raise Status Updated Called");
            OnStatusUpdated?.Invoke();
        }
        
        public static event Action RefreshTreeWindow;
        public static void RaiseRefreshTreeWindow()
        {
            if (inProgress)
                return;
            AnchorpointLogger.Log("Raise Refresh Window Called");
            RefreshTreeWindow?.Invoke();
        }
        
        public static event Action<string> OnCommandOutputReceived;
        public static void RaiseCommandOutputReceived(string str)
        {
            AnchorpointLogger.Log("Raise Command Output Called");
            OnCommandOutputReceived?.Invoke(str);
        }
        
        public static event Action<ConnectMessage> OnMessageReceived;
        public static void RaiseMessageReceived(ConnectMessage message)
        {
            OnMessageReceived?.Invoke(message);
        }
        
        public static event Action RefreshView;
        public static void RaiseRefreshView()
        {
            if (inProgress)
                return;   
            AnchorpointLogger.Log("Raise Refresh View Called");
            RefreshView?.Invoke();
        }
    }
}
