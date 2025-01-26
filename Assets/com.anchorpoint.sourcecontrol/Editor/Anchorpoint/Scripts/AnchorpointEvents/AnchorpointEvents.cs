using System;
using Anchorpoint.Wrapper;

namespace Anchorpoint.Events
{
    public static class AnchorpointEvents
    {
        // Event to notify when status is updated
        public static event Action OnStatusUpdated;
        public static void RaiseStatusUpdated()
        {
            OnStatusUpdated?.Invoke();
        }
        
        public static event Action RefreshWindow;
        public static void RaiseRefreshWindow()
        {
            RefreshWindow?.Invoke();
        }
        
        public static event Action<string> OnCommandOutputReceived;
        public static void RaiseCommandOutputReceived(string str)
        {
            OnCommandOutputReceived?.Invoke(str);
        }
        
        public static event Action<ConnectMessage> OnMessageReceived;
        public static void RaiseMessageReceived(ConnectMessage message)
        {
            OnMessageReceived?.Invoke(message);
        }
    }
}
