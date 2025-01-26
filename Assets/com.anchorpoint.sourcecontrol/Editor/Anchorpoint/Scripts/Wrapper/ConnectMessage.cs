using System;
using System.Collections.Generic;

namespace Anchorpoint.Wrapper
{
    [Serializable]
    public class ConnectMessage
    {
        public string id;
        public string type;
        public List<string> files;
    }
}