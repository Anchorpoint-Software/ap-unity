using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnchorPoint.Constants;
using AnchorPoint.Logger;
using AnchorPoint.Parser;
using AnchorPoint.Wrapper;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AnchorPoint.Editor
{
    public class AnchorpointEditor : EditorWindow
    {
        [SerializeField] private VisualTreeAsset m_VisualTreeAsset = default;

        private List<TreeViewItemData<ProjectData>> treeViewItems = new List<TreeViewItemData<ProjectData>>();
        private ProjectData projectRoot; // Root of the project data tree
        
        private void OnEnable()
        {
            CLIWrapper.RefreshWindow += OnEditorUpdate;
        }
        
        private void OnDisable()
        {
            CLIWrapper.RefreshWindow -= OnEditorUpdate;
        }
        
        private void OnEditorUpdate()
        {
            // Clear the rootVisualElement to remove all existing UI
            rootVisualElement.Clear();
    
            // Rebuild the entire UI, including the TreeView and labels
            CreateGUI();
    
            // Ensure the window is repainted after the rebuild
            Repaint();
        }

        [MenuItem("Window/Anchorpoint")]
        public static void ShowWindow()
        {
            AnchorpointEditor window = GetWindow<AnchorpointEditor>();
            
            string assetPath = AssetDatabase.GUIDToAssetPath("d8e0264a1e3a54b09aaf9e7ac62d4e1f");
            Texture2D icon = (Texture2D)AssetDatabase.LoadAssetAtPath(assetPath, typeof(Texture2D));
            window.titleContent = new GUIContent(" AnchorPoint", icon);
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;

            VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
            root.Add(labelFromUXML);

            // Get the commit message text field and commit button
            TextField commitMessageField = root.Q<TextField>("CommitMessageField");
            Button commitButton = root.Q<Button>("CommitButton");
            
            Label changesLabel = root.Q<Label>("ChangeCountLabel");
            int totalChanges = CalculateTotalChanges();
            changesLabel.text = $"Total Changes: {totalChanges}";

            Button allButton = root.Q<Button>("AllButton");
            Button noneButton = root.Q<Button>("NoneButton");

            var treeView = root.Q<TreeView>("TreeView");
            if (treeView == null)
            {
                AnchorPointLogger.LogError("TreeView not found in the UXML hierarchy!");
                return;
            }

            allButton.clickable.clicked += () =>
            {
                SetAllCheckboxes(true);
            };

            noneButton.clickable.clicked += () =>
            {
                SetAllCheckboxes(false);
            };
            
            // When the commit button is clicked, gather selected files and commit them
            commitButton.clickable.clicked += () =>
            {
                string commitMessage = commitMessageField.value;
                List<string> filesToCommit = GetSelectedFiles();
                
                if (filesToCommit.Count > 0)
                {
                    CLIWrapper.Commit(commitMessage, filesToCommit.ToArray());
                }
                else
                {
                    AnchorPointLogger.LogWarning("No files selected for commit.");
                }
            };

            CreateTreeUnity(treeView);
        }

        private void CreateTreeUnity(TreeView treeView)
        {
            if (treeView == null)
            {
                AnchorPointLogger.LogError("TreeView is null!");
                return;
            }

            projectRoot = GetCliProjectStructure();

            if (projectRoot == null || projectRoot.Children.Count == 0)
            {
                AnchorPointLogger.LogWarning("No project structure data found or children are empty.");
                return;
            }
            else
            {
                AnchorPointLogger.Log($"Project Root found: {projectRoot.Name}, Child Count: {projectRoot.Children.Count}");
            }

            treeViewItems.Clear();  // Clear any previous data
            int idCounter = 0;
            PopulateTreeItems(projectRoot, treeViewItems, ref idCounter);

            // Use SetRootItems to assign the tree data
            treeView.SetRootItems(treeViewItems);

            // Define how to create each item
            treeView.makeItem = () =>
            {
                var container = new VisualElement { style = { flexDirection = FlexDirection.Row } };

                var checkbox = new Toggle { name = "checkbox" };
                checkbox.style.marginRight = 5;
                checkbox.style.marginTop = 3.5f;

                checkbox.RegisterCallback<ChangeEvent<bool>>(evt =>
                {
                    var itemData = (ProjectData)checkbox.userData;
                    itemData.IsChecked = evt.newValue;
                });

                var icon = new Image { name = "icon" };
                icon.style.width = 16;
                icon.style.height = 16;
                icon.style.marginTop = 3;

                var label = new Label { name = "name" };
                label.style.marginTop = 3;

                container.Add(checkbox);
                container.Add(icon);
                container.Add(label);

                return container;
            };

            // Bind each item in the tree
            treeView.bindItem = (element, index) =>
            {
                var itemData = treeView.GetItemDataForIndex<ProjectData>(index);

                var checkbox = element.Q<Toggle>("checkbox");
                checkbox.userData = itemData;
                checkbox.value = itemData.IsChecked;

                var icon = element.Q<Image>("icon");
                var nameLabel = element.Q<Label>("name");

                nameLabel.text = itemData.Name;

                // Set the appropriate color based on the status using StyleColor
                switch (itemData.Status)
                {
                    case "A":  // Added files
                        nameLabel.style.color = new StyleColor(Color.green);  // Use StyleColor to set color
                        break;

                    case "M":  // Modified files
                        nameLabel.style.color = new StyleColor(Color.yellow);  // Use StyleColor for yellow
                        break;

                    case "D":  // Deleted files
                        nameLabel.style.color = new StyleColor(Color.red);  // Use StyleColor for red
                        break;

                    default:
                        nameLabel.style.color = new StyleColor(Color.white);  // Default to white
                        break;
                }

                if (itemData.Icon != null)
                {
                    icon.image = itemData.Icon;
                }
                else
                {
                    icon.image = GetFileIcon(itemData.Path);
                }

                checkbox.style.display = itemData.IsDirectory ? DisplayStyle.None : DisplayStyle.Flex;
                checkbox.style.display = itemData.IsEmptyDirectory ? DisplayStyle.Flex : checkbox.style.display;
            };

            treeView.selectionType = SelectionType.Multiple;
            treeView.Rebuild(); // Rebuild the tree after setting items
        }

        private void PopulateTreeItems(ProjectData data, List<TreeViewItemData<ProjectData>> items, ref int idCounter)
        {
            var treeItem = new TreeViewItemData<ProjectData>(idCounter++, data);

            if (data.Children.Count > 0)
            {
                var childItems = new List<TreeViewItemData<ProjectData>>();
                foreach (var child in data.Children)
                {
                    PopulateTreeItems(child, childItems, ref idCounter);
                }

                treeItem = new TreeViewItemData<ProjectData>(idCounter++, data, childItems);
            }

            items.Add(treeItem);
        }

        private Texture2D GetFileIcon(string path)
        {
            string iconType = "DefaultAsset Icon";

            if (path.EndsWith(".cs"))
            {
                iconType = "cs Script Icon";
            }
            else if (path.EndsWith(".png") || path.EndsWith(".jpg") || path.EndsWith(".jpeg"))
            {
                iconType = "Texture Icon";
            }
            else if (path.EndsWith(".prefab"))
            {
                iconType = "Prefab Icon";
            }
            else if (path.EndsWith(".unity"))
            {
                iconType = "SceneAsset Icon";
            }
            else if (path.EndsWith(""))
            {
                iconType = "Folder Icon";
            }

            GUIContent iconContent = EditorGUIUtility.IconContent(iconType);
            return iconContent?.image as Texture2D;
        }

        private ProjectData GetCliProjectStructure()
        {
            CLIStatus status = DataManager.GetStatus();
            if (status == null)
            {
                AnchorPointLogger.LogError("CLIStatus is null");
                return new ProjectData { Name = "No CLI Data" };
            }

            var projectRoot = new ProjectData { Name = "Project Root", Path = "", IsDirectory = true, IsEmptyDirectory = false, Status = "Unknown"};

            // Helper method to find or create the path
            Func<string, ProjectData, ProjectData> findOrCreatePath = null;
            findOrCreatePath = (path, currentNode) =>
            {
                if (string.IsNullOrEmpty(path))
                    return currentNode;

                string[] parts = path.Split(new char[] { '/', '\\' }, 2);
                string currentPart = parts[0];
                string remainingPath = parts.Length > 1 ? parts[1] : "";

                var childNode = currentNode.Children.FirstOrDefault(c => c.Name == currentPart && c.IsDirectory);
                if (childNode == null)
                {
                    string fullPath = currentNode.Path != null ? Path.Combine(currentNode.Path, currentPart) : currentPart;
                    childNode = new ProjectData(currentPart, fullPath, true);
                    currentNode.Children.Add(childNode);
                }

                return findOrCreatePath(remainingPath, childNode);
            };

           Action<Dictionary<string, string>, ProjectData> addFilesToStructure = (files, rootNode) =>
           {
                HashSet<string> processedPaths = new HashSet<string>();

                foreach (var file in files)
                {
                    string relativePath = file.Key;
                    string statusFlag = file.Value; // 'A', 'M', 'D'
                    string fullPath = Path.Combine(CLIConstants.WorkingDirectory, relativePath); // Full path from git root
                    string projectRelativePath = fullPath.Replace(CLIConstants.WorkingDirectory, "").TrimStart(Path.DirectorySeparatorChar);

                    // Handle .meta files
                    if (relativePath.EndsWith(".meta"))
                    {
                        string basePath = fullPath.Replace(".meta", "");

                        // Only show .meta files if they belong to deleted files or empty folders
                        if (File.Exists(basePath) || processedPaths.Contains(basePath))
                        {
                            continue; // Skip showing the .meta file if the base file exists or has been processed
                        }

                        // Show orphaned .meta files (deleted assets) or empty folder .meta
                        if (Directory.Exists(basePath))
                        {
                            if (Directory.GetFiles(basePath).Length == 0 && Directory.GetDirectories(basePath).Length == 0)
                            {
                                ProjectData directoryNode = findOrCreatePath(Path.GetDirectoryName(projectRelativePath), rootNode);
                                directoryNode.Children.Add(new ProjectData(Path.GetFileName(basePath), projectRelativePath, true, true, statusFlag)  // <-- Setting status here
                                {
                                    Icon = GetFileIcon(basePath)
                                });
                                processedPaths.Add(basePath); // Mark folder as processed
                            }
                        }
                        else
                        {
                            // Show orphaned meta file for deleted assets
                            ProjectData directoryNode = findOrCreatePath(Path.GetDirectoryName(projectRelativePath), rootNode);
                            directoryNode.Children.Add(new ProjectData(Path.GetFileName(projectRelativePath), projectRelativePath, false, false, statusFlag)  // <-- Setting status here
                            {
                                Icon = GetFileIcon(projectRelativePath)
                            });
                            processedPaths.Add(fullPath); // Mark .meta file as processed
                        }
                    }
                    else
                    {
                        // Handle regular files and non-empty folders
                        if (!processedPaths.Contains(fullPath))
                        {
                            ProjectData directoryNode = findOrCreatePath(Path.GetDirectoryName(projectRelativePath), rootNode);
                            directoryNode.Children.Add(new ProjectData(Path.GetFileName(fullPath), projectRelativePath, false, false, statusFlag)  // <-- Setting status here
                            {
                                Icon = GetFileIcon(fullPath)
                            });
                            processedPaths.Add(fullPath); // Mark the file as processed
                        } 
                    }
                }
           };
           
           if (status.NotStaged != null)
               addFilesToStructure(status.NotStaged, projectRoot);

           return projectRoot;
        }
        
        private void SetAllCheckboxes(bool isChecked)
        {
            if (treeViewItems != null && treeViewItems.Count > 0)
            {
                SetAllCheckboxesRecursive(treeViewItems.Select(x => x.data), isChecked);
            }

            var treeView = rootVisualElement.Q<TreeView>("TreeView");
            treeView.Rebuild(); // Rebuild the tree view to reflect the changes
        }

        private void SetAllCheckboxesRecursive(IEnumerable<ProjectData> dataItems, bool isChecked)
        {
            foreach (var item in dataItems)
            {
                item.IsChecked = isChecked;
                if (item.Children != null && item.Children.Any())
                {
                    SetAllCheckboxesRecursive(item.Children, isChecked);
                }
            }
        }

        // Function to gather selected files and their meta files
        private List<string> GetSelectedFiles()
        {
            List<string> selectedFiles = new List<string>();

            // Recursively add files and meta files, even if they are deleted
            AddSelectedFilesRecursive(projectRoot, selectedFiles);
            return selectedFiles;
        }

        private void AddSelectedFilesRecursive(ProjectData node, List<string> selectedFiles)
        {
            bool hasFileSelected = false;

            // If the node is selected and it's not a directory (i.e., a file)
            if (node.IsChecked && !node.IsDirectory)
            {
                selectedFiles.Add(node.Path); // Add the file itself
                hasFileSelected = true; // Mark that a file is selected

                // If it's not already a .meta file, check for and add its associated .meta file
                if (!node.Path.EndsWith(".meta"))
                {
                    string metaFilePath = node.Path + ".meta";

                    // Add the .meta file if it exists or if it's part of a staged/unstaged list
                    if (File.Exists(metaFilePath) || DataManager.GetStatus().NotStaged.ContainsKey(metaFilePath))
                    {
                        selectedFiles.Add(metaFilePath); // Add the corresponding .meta file
                    }
                }
            }

            // Recursively process child nodes to see if any files are selected
            if (node.Children != null && node.Children.Any())
            {
                foreach (var child in node.Children)
                {
                    AddSelectedFilesRecursive(child, selectedFiles);

                    // If any file in this folder is selected, mark that the folder itself should be included
                    if (child.IsChecked || child.Children.Any(c => c.IsChecked)) // Check if any children are checked
                    {
                        hasFileSelected = true;
                    }
                }
            }

            // If any file in this folder is selected, include the folder and its .meta file
            if (hasFileSelected && node.IsDirectory)
            {
                if (!selectedFiles.Contains(node.Path))
                {
                    selectedFiles.Add(node.Path); // Add the folder itself
                }

                // Add the folder's .meta file if it exists in the system or is staged/unstaged
                string folderMetaPath = node.Path + ".meta";
                if (File.Exists(folderMetaPath) || DataManager.GetStatus().NotStaged.ContainsKey(folderMetaPath))
                {
                    selectedFiles.Add(folderMetaPath); // Add the folder's .meta file
                }
            }

            // If it's an empty directory, directly add the folder and its .meta file
            if (node.IsChecked && node.IsEmptyDirectory)
            {
                selectedFiles.Add(node.Path); // Empty folder

                // Ensure .meta file for the empty folder is added
                string emptyFolderMetaPath = node.Path + ".meta";
                if (File.Exists(emptyFolderMetaPath) || DataManager.GetStatus().NotStaged.ContainsKey(emptyFolderMetaPath))
                {
                    selectedFiles.Add(emptyFolderMetaPath); // Add the .meta file for empty folder
                }
            }
        }
        
        // Calculate the total changes by dynamically checking both files and meta files.
        private int CalculateTotalChanges()
        {
            string gitIgnoreDirectory = CLIConstants.WorkingDirectory;  // Directory with .gitignore

            CLIStatus status = DataManager.GetStatus();
            int totalChanges = 0; // Total changes counter

            HashSet<string> processedFiles = new HashSet<string>(); // To track processed base files and avoid counting .meta files

            if (status?.NotStaged != null)
            {
                foreach (var entry in status.NotStaged)
                {
                    string filePath = entry.Key;
                    string statusFlag = entry.Value;  // 'A', 'M', 'D'
                    string fullPath = Path.Combine(gitIgnoreDirectory, filePath); // Full path from git root

                    // Skip the .meta file if the associated file has already been processed
                    if (filePath.EndsWith(".meta"))
                    {
                        string baseFilePath = fullPath.Replace(".meta", "");

                        // If the base file is already processed, skip counting the .meta file
                        if (processedFiles.Contains(baseFilePath))
                        {
                            continue;
                        }
                    }
                    else
                    {
                        // Mark the base file as processed to skip its .meta file later
                        processedFiles.Add(fullPath);
                    }

                    // Process each status type accordingly
                    switch (statusFlag)
                    {
                        case "A":  // Added files
                        case "M":  // Modified files
                            if (File.Exists(fullPath))  // Ensure the file exists
                            {
                                totalChanges++;  // Count added or modified file
                            }
                            break;

                        case "D":  // Deleted files
                            // Deleted files might not exist in the file system anymore, but we count them
                            totalChanges++;
                            break;

                        default:
                            break;
                    }
                }
            }
            return totalChanges;
        }
    }
}
