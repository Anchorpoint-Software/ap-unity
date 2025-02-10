using System.Collections.Generic;
using UnityEngine;

namespace Anchorpoint.Editor
{ 
    public static class IconCache
    {
        public static Dictionary<string, Texture2D> Icons = new Dictionary<string, Texture2D>();
        public static List<Texture2D> PersistentReferences = new List<Texture2D>();
    }
}
