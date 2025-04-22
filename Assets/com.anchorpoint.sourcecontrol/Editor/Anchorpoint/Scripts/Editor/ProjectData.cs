using System.Collections.Generic;
using UnityEngine;

namespace Anchorpoint.Editor
{
    /// <summary>
    /// Represents a file or folder in the Anchorpoint Editor hierarchy. 
    /// Contains metadata used for visual representation, commit management, and status tracking within Unity.
    /// </summary>
    public class ProjectData
    {
        public int Id { get; set; }  // Unique ID used for TreeView display and tracking
        public string Name { get; set; }  // Display name of the file or folder
        public string Path { get; set; }  // Relative path used for visual hierarchy
        public string CommitPath { get; set; }  // Full path used for committing to version control
        public Texture2D Icon { get; set; }  // Icon representing the file or folder in the UI
        public bool IsDirectory { get; set; }  // True if the entry is a folder
        public bool IsEmptyDirectory { get; set; }  // True if the folder is empty or marked as such
        public bool IsChecked { get; set; }  // Used for tracking selection in the UI (checkbox state)
        public string Status { get; set; }  // Git status: A = Added, M = Modified, D = Deleted
        public List<ProjectData> Children { get; set; } = new List<ProjectData>();  // List of children if this is a directory
        public ProjectData Parent { get; set; }  // Reference to parent ProjectData (null if root)

        // Default constructor
        public ProjectData() { }

        // Constructor with name, path and directory flag
        public ProjectData(string name, string path, bool isDirectory)
        {
            Name = name;
            Path = path;
            IsDirectory = isDirectory;
        }
    }
}