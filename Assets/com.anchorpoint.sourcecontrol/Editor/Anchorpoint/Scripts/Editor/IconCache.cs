using System.Collections.Generic;
using UnityEngine;

namespace Anchorpoint.Editor
{ 
    /// <summary>
    /// A static utility class for managing and caching icons in the Anchorpoint Unity Editor.
    /// Stores icon references in a dictionary for quick lookup and maintains a persistent list to avoid garbage collection.
    /// </summary>
    public static class IconCache
    {
        // Dictionary to cache icons by file path or identifier to avoid repeated loading.
        public static Dictionary<string, Texture2D> Icons = new Dictionary<string, Texture2D>();
        // Holds strong references to icons to prevent them from being unloaded by Unity's GC.
        public static List<Texture2D> PersistentReferences = new List<Texture2D>();
    }
}
