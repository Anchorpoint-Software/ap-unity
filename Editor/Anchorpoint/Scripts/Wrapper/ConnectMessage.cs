using System;
using System.Collections.Generic;

namespace Anchorpoint.Wrapper
{
    /// <summary>
    /// Represents the message received from the Anchorpoint CLI during connection,
    /// including an ID, type, and a list of related files.
    /// </summary>
    [Serializable]
    public class ConnectMessage
    {
        /// Unique identifier for the connection message.
        public string id;

        /// The type/category of the message (e.g., status, info, error).
        public string type;

        /// A list of file paths relevant to this message.
        public List<string> files;
    }
}