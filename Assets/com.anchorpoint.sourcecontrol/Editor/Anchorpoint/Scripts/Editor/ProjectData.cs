using System.Collections.Generic;
using UnityEngine;

namespace AnchorPoint.Editor
{
    public class ProjectData
    {
        public string Name { get; set; }            // Name of the file/folder
        public string Path { get; set; }            // Full path of the file/folder
        public Texture2D Icon { get; set; }         // Icon representing file/folder
        public bool IsDirectory { get; set; }       // Is it a directory?
        public bool IsEmptyDirectory { get; set; }  // Is it an empty directory?
        public bool IsChecked { get; set; }         // Checkbox state in the tree
        public string Status { get; set; }          // File status ('A', 'M', 'D')
        public List<ProjectData> Children { get; set; } = new List<ProjectData>(); // Children of the folder

        // Default constructor
        public ProjectData() { }

        // Constructor for non-empty directory
        public ProjectData(string name, string path, bool isDirectory)
        {
            Name = name;
            Path = path;
            IsDirectory = isDirectory;
        }

        // Constructor for empty directory
        public ProjectData(string name, string path, bool isDirectory, bool isEmptyDirectory)
        {
            Name = name;
            Path = path;
            IsDirectory = isDirectory;
            IsEmptyDirectory = isEmptyDirectory;
        }

        // Constructor that includes status (A/M/D)
        public ProjectData(string name, string path, bool isDirectory, bool isEmptyDirectory, string status)
        {
            Name = name;
            Path = path;
            IsDirectory = isDirectory;
            IsEmptyDirectory = isEmptyDirectory;
            Status = status;
        }
    }
}