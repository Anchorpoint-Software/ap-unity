using System.Collections.Generic;
using UnityEngine;

namespace AnchorPoint.Editor
{
    public class ProjectData
    {
        public int Id { get; set; }
        public string Name { get; set; }            // Name of the file/folder
        public string Path { get; set; }             // For visual hierarchy
        public string CommitPath { get; set; }      // For commit path
        public Texture2D Icon { get; set; }         // Icon representing file/folder
        public bool IsDirectory { get; set; }       // Is it a directory?
        public bool IsEmptyDirectory { get; set; }  // Is it an empty directory?
        public bool IsChecked { get; set; }         // Checkbox state in the tree
        public string Status { get; set; }          // File status ('A', 'M', 'D')
        public List<ProjectData> Children { get; set; } = new List<ProjectData>(); // Children of the folder
        public ProjectData Parent { get; set; }     // Parent folder (new field)

        // Constructors
        public ProjectData() { }

        public ProjectData(string name, string path, bool isDirectory)
        {
            Name = name;
            Path = path;
            IsDirectory = isDirectory;
        }
    }
}