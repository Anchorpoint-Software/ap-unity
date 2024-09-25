using System.Collections.Generic;
using UnityEngine;

namespace AnchorPoint.Editor
{
    public class ProjectData
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public Texture2D Icon { get; set; }
        public bool IsDirectory { get; set; }
        
        public bool IsChecked { get; set; }
        public List<ProjectData> Children { get; set; } = new List<ProjectData>();

        public ProjectData() { }

        public ProjectData(string name, string path, bool isDirectory)
        {
            Name = name;
            Path = path;
            IsDirectory = isDirectory;
        }
    }
}

