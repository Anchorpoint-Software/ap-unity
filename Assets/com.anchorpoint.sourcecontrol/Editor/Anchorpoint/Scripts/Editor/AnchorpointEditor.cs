using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Anchorpoint.Constants;
using Anchorpoint.Logger;
using Anchorpoint.Parser;
using Anchorpoint.Wrapper;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Anchorpoint.Editor
{
    public class AnchorpointEditor : EditorWindow
    {
        [SerializeField] private VisualTreeAsset m_VisualTreeAsset = default;

        private List<TreeViewItemData<ProjectData>> treeViewItems = new List<TreeViewItemData<ProjectData>>();
        private ProjectData projectRoot; // Root of the project data tree
        private TreeView treeView;
        private TextField commitMessageField;
        private Button commitButton;
        private Button revertButton;

        // Global paths
        private string projectPath;      // Absolute path to the Unity project root
        private const string assetsFolderName = "Assets";
        private string assetsPath;       // Absolute path to the Assets folder
        private string btnStr;
        
        private bool inProcess = false;      // Flag to check is if some commit/revert is in process
        
        private const string anchorPointIcon = "d8e0264a1e3a54b09aaf9e7ac62d4e1f";

        private void OnEnable()
        {
            CLIWrapper.RefreshWindow += OnEditorUpdate;
            CLIWrapper.OnCommandOutputReceived += OnCommandOutputReceived; 
        }

        private void OnDisable()
        {
            CLIWrapper.RefreshWindow -= OnEditorUpdate;
            CLIWrapper.OnCommandOutputReceived -= OnCommandOutputReceived; 
        }

        private void OnEditorUpdate()
        {
            if (!CLIWrapper.isWindowActive) return;
            rootVisualElement.Clear();
            CreateGUI();
        }

        [MenuItem("Window/Anchorpoint")]
        public static void ShowWindow()
        {
            AnchorpointEditor window = GetWindow<AnchorpointEditor>();

            string assetPath = AssetDatabase.GUIDToAssetPath(anchorPointIcon);
            Texture2D icon = (Texture2D)AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            window.titleContent = new GUIContent("Anchorpoint", icon);
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;

            VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
            root.Add(labelFromUXML);

            // Get the commit message text field and commit button
            commitMessageField = root.Q<TextField>("CommitMessageField");
            commitButton = root.Q<Button>("CommitButton");
            commitButton.SetEnabled(false); // Disable the commit button initially
            revertButton = root.Q<Button>("Revert");
            revertButton.SetEnabled(false);

            Label changesLabel = root.Q<Label>("ChangeCountLabel");
            int totalChanges = CalculateTotalChanges();
            changesLabel.text = $"Total Changes: {totalChanges}";

            Button allButton = root.Q<Button>("AllButton");
            Button noneButton = root.Q<Button>("NoneButton");

            treeView = root.Q<TreeView>("TreeView");

            allButton.clickable.clicked += () => { SetAllCheckboxes(true); };
            noneButton.clickable.clicked += () => { SetAllCheckboxes(false); };

            // When the commit button is clicked, gather selected files and commit them
            commitButton.clickable.clicked += () =>
            {
                string commitMessage = commitMessageField.value;
                List<string> filesToCommit = GetSelectedFiles();
                
                if (IsAnyFileSelected())
                {
                    inProcess = true;
                    commitButton.SetEnabled(false);
                    revertButton.SetEnabled(false);
                    commitMessageField.SetEnabled(false);
                    commitButton.text = "Processing changes…";
                    CLIWrapper.Sync(commitMessage, filesToCommit.ToArray());
                }
                else
                {
                    AnchorpointLogger.LogWarning("No files selected for commit.");
                }
            };
            
            revertButton.clickable.clicked += () =>
            {
                List<string> filesToRevert = GetSelectedFiles();
                
                if (IsAnyFileSelected())
                {
                    inProcess = true;
                    commitButton.SetEnabled(false);
                    revertButton.SetEnabled(false);
                    commitMessageField.SetEnabled(false);
                    revertButton.text = "Reverting...";
                    CLIWrapper.Revert(filesToRevert.ToArray());
                }
                else
                {
                    AnchorpointLogger.LogWarning("No files selected for revert.");
                }
            };

            CreateTreeUnity(treeView);
        }

        private void CreateTreeUnity(TreeView treeView)
        {
            if (treeView == null)
            {
                AnchorpointLogger.LogError("TreeView is null!");
                return;
            }

            projectRoot = GetCliProjectStructure();

            if (projectRoot == null || projectRoot.Children.Count == 0)
            {
                AnchorpointLogger.LogWarning("No project structure data found or children are empty.");
                return;
            }
            else
            {
                AnchorpointLogger.Log($"Project Root found: {projectRoot.Name}, Child Count: {projectRoot.Children.Count}");
            }

            treeViewItems.Clear();  // Clear any previous data
            int idCounter = 0;
            PopulateTreeItems(projectRoot, treeViewItems, ref idCounter);

            // Use SetRootItems to assign the tree data
            treeView.SetRootItems(treeViewItems);

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

                    // If the item is a directory, update all its children
                    if (itemData.IsDirectory && itemData.Children != null && itemData.Children.Any())
                    {
                        SetAllCheckboxesRecursive(itemData.Children, evt.newValue);
                    }

                    commitButton.SetEnabled(IsAnyFileSelected()); // Update the commit button state
                    revertButton.SetEnabled(IsAnyFileSelected()); // Update the revert button state
                    commitMessageField.SetEnabled(IsAnyFileSelected());

                    // Refresh all visible items in the tree view
                    treeView.RefreshItems();
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

            treeView.bindItem = (element, index) =>
            {
                var itemData = treeView.GetItemDataForIndex<ProjectData>(index);

                var checkbox = element.Q<Toggle>("checkbox");
                checkbox.userData = itemData;
                checkbox.SetValueWithoutNotify(itemData.IsChecked);

                var icon = element.Q<Image>("icon");
                var nameLabel = element.Q<Label>("name");

                nameLabel.text = itemData.Name;

                // Set the appropriate color based on the status using StyleColor
                switch (itemData.Status)
                {
                    case "A":  // Added files
                        nameLabel.style.color = new StyleColor(Color.green);
                        break;
                    case "M":  // Modified files
                        nameLabel.style.color = new StyleColor(Color.yellow);
                        break;
                    case "D":  // Deleted files
                        nameLabel.style.color = new StyleColor(Color.red);
                        break;
                    default:
                        nameLabel.style.color = new StyleColor(Color.white);
                        break;
                }

                if (itemData.Icon != null)
                {
                    icon.image = itemData.Icon;
                }
                else
                {
                    // Fallback icon if necessary
                    icon.image = EditorGUIUtility.IconContent("Folder Icon").image as Texture2D;
                }

                // Show checkboxes for files, empty folders, and deleted folders
                if (itemData.IsDirectory)
                {
                    if (itemData.IsEmptyDirectory || itemData.Status == "D" || itemData.Status == "A")
                    {
                        checkbox.style.display = DisplayStyle.Flex;
                    }
                    else
                    {
                        checkbox.style.display = DisplayStyle.None;
                    }
                }
                else
                {
                    checkbox.style.display = DisplayStyle.Flex;
                }
            };

            treeView.selectionType = SelectionType.Multiple;
            treeView.Rebuild(); // Rebuild the tree after setting items
            treeView.ExpandAll();
        }

        private void PopulateTreeItems(ProjectData data, List<TreeViewItemData<ProjectData>> items, ref int idCounter)
        {
            List<TreeViewItemData<ProjectData>> childItems = null;

            if (data.Children.Count > 0)
            {
                childItems = new List<TreeViewItemData<ProjectData>>();
                foreach (var child in data.Children)
                {
                    child.Parent = data;  // Set the parent reference for the child
                    PopulateTreeItems(child, childItems, ref idCounter);
                }
            }

            data.Id = idCounter; // Assign the id to the data

            var treeItem = new TreeViewItemData<ProjectData>(idCounter++, data, childItems);
            items.Add(treeItem);
        }

        private Texture2D GetFileIcon(string relativePath)
        {
            // Normalize the path separators to forward slashes
            relativePath = relativePath.Replace('\\', '/');

            // Ensure the path is relative and starts with "Assets/"
            if (!relativePath.StartsWith(assetsFolderName + "/", StringComparison.OrdinalIgnoreCase))
            {
                relativePath = assetsFolderName + "/" + relativePath.TrimStart('/');
            }

            // Convert the relative path to an absolute path for the file system
            string absolutePath = Path.Combine(projectPath, relativePath.Replace('/', Path.DirectorySeparatorChar));

            // Check if the asset exists on disk
            bool assetExists = File.Exists(absolutePath) || Directory.Exists(absolutePath);

            // Load the asset at the given relative path
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(relativePath);

            if (asset != null && assetExists)
            {
                // Get the icon for the loaded asset
                Texture2D icon = AssetPreview.GetMiniThumbnail(asset);
                return icon;
            }
            else
            {
                // Asset doesn't exist on disk or can't be loaded
                // Get the file extension
                string extension = Path.GetExtension(relativePath).ToLowerInvariant();

                // Map file extensions to Unity icon names
                string iconName = GetIconNameForExtension(extension);

                // Get the icon
                GUIContent iconContent = EditorGUIUtility.IconContent(iconName);
                if (iconContent != null && iconContent.image != null)
                {
                    return iconContent.image as Texture2D;
                }
                else
                {
                    // Fallback to default icons
                    if (Directory.Exists(absolutePath))
                    {
                        // It's a folder
                        GUIContent folderIcon = EditorGUIUtility.IconContent("Folder Icon");
                        return folderIcon?.image as Texture2D;
                    }
                    else
                    {
                        // Use generic warning icon
                        GUIContent defaultIcon = EditorGUIUtility.IconContent("console.warnicon");
                        return defaultIcon?.image as Texture2D;
                    }
                }
            }
        }

        private string GetIconNameForExtension(string extension)
        {
            switch (extension)
            {
                case ".cs":
                    return "cs Script Icon";
                case ".js":
                    return "Js Script Icon";
                case ".boo":
                    return "Boo Script Icon";
                case ".shader":
                    return "Shader Icon";
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".tga":
                case ".psd":
                case ".gif":
                    return "Texture Icon";
                case ".mat":
                    return "Material Icon";
                case ".prefab":
                    return "Prefab Icon";
                case ".fbx":
                case ".obj":
                    return "Mesh Icon";
                case ".anim":
                    return "Animation Icon";
                case ".controller":
                    return "AnimatorController Icon";
                case ".ttf":
                case ".otf":
                case ".fon":
                    return "Font Icon";
                case ".txt":
                case ".xml":
                case ".json":
                    return "TextAsset Icon";
                case ".unity":
                    return "SceneAsset Icon";
                case ".asset":
                    return "ScriptableObject Icon";
                case ".wav":
                case ".mp3":
                case ".ogg":
                    return "AudioClip Icon";
                // Add more cases as needed for different file types
                default:
                    return "DefaultAsset Icon";
            }
        }

        private ProjectData GetCliProjectStructure()
        {
            CLIStatus status = DataManager.GetStatus();
            if (status == null)
            {
                AnchorpointLogger.LogError("CLIStatus is null");
                CLIWrapper.Status();
                return new ProjectData { Name = "No CLI Data" };
            }

            projectPath = Directory.GetParent(Application.dataPath).FullName;
            assetsPath = Path.Combine(projectPath, assetsFolderName);

            string projectName = Directory.GetParent(Application.dataPath).Name;
            string rootRelativePath = projectPath.Substring(CLIConstants.WorkingDirectory.Length).TrimStart(Path.DirectorySeparatorChar);

            var projectRoot = new ProjectData
            {
                Name = projectName,
                Path = projectPath,
                IsDirectory = true,
                IsEmptyDirectory = false,
                Status = "Unknown",
                CommitPath = rootRelativePath
            };

            // Function to find or create a node in the project data tree based on the given path
            Func<string, ProjectData, ProjectData> findOrCreatePath = null;
            findOrCreatePath = (path, currentNode) =>
            {
                if (string.IsNullOrEmpty(path))
                    return currentNode;

                string[] parts = path.Split(new char[] { '/', '\\' }, 2);
                string currentPart = parts[0];
                string remainingPath = parts.Length > 1 ? parts[1] : "";

                // Adjusted to include empty directories
                var childNode = currentNode.Children.FirstOrDefault(c => c.Name == currentPart && c.IsDirectory);
                if (childNode == null)
                {
                    string fullPath = currentNode.Path != null ? Path.Combine(currentNode.Path, currentPart) : currentPart;

                    // Calculate the CommitPath
                    string commitPath = !string.IsNullOrEmpty(currentNode.CommitPath)
                        ? Path.Combine(currentNode.CommitPath, currentPart)
                        : currentPart;

                    childNode = new ProjectData(currentPart, fullPath, true)
                    {
                        Parent = currentNode,
                        CommitPath = commitPath // Set the CommitPath for the directory
                    };
                    currentNode.Children.Add(childNode);
                }

                return findOrCreatePath(remainingPath, childNode);
            };

            Action<Dictionary<string, string>, ProjectData> addFilesToStructure = (files, rootNode) =>
            {
                HashSet<string> processedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var file in files)
                {
                    string relativePath = file.Key; // e.g., "Assets/New Folder 1/New Folder/New Folder/One.cs"
                    string statusFlag = file.Value;  // e.g., "D"
                    string fullPath = Path.Combine(CLIConstants.WorkingDirectory, relativePath);

                    if (!fullPath.StartsWith(projectPath, StringComparison.OrdinalIgnoreCase))
                        continue;

                    string projectRelativePath = fullPath.Replace(projectPath, "").TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                    if (relativePath.EndsWith(".meta"))
                    {
                        string baseRelativePath = relativePath.Substring(0, relativePath.Length - 5); // Remove ".meta"
                        string baseFullPath = fullPath.Substring(0, fullPath.Length - 5);
                        string baseProjectRelativePath = baseFullPath.Replace(projectPath, "").TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                        // Check if the base path is a directory
                        bool isDirectory = !Path.HasExtension(baseFullPath);

                        if (isDirectory)
                        {
                            // It's an empty or deleted folder
                            ProjectData directoryNode = findOrCreatePath(Path.GetDirectoryName(baseProjectRelativePath), rootNode);

                            // Avoid adding duplicate folders
                            if (!directoryNode.Children.Any(c => c.Name.Equals(Path.GetFileName(baseFullPath), StringComparison.OrdinalIgnoreCase) && c.IsDirectory))
                            {
                                string folderName = Path.GetFileName(baseFullPath);
                                string folderProjectRelativePath = baseProjectRelativePath;
                                string folderCommitPath = relativePath;

                                ProjectData newItem = new ProjectData
                                {
                                    Name = folderName,
                                    Path = folderProjectRelativePath,
                                    CommitPath = folderCommitPath, // Include ".meta" in CommitPath
                                    IsDirectory = true,
                                    IsEmptyDirectory = true,
                                    Status = statusFlag,
                                    Icon = GetFileIcon(folderProjectRelativePath)
                                };

                                directoryNode.Children.Add(newItem);
                                processedPaths.Add(baseFullPath);
                            }
                        }
                        else
                        {
                            // It's a .meta file for a file (not a directory), skip processing
                            continue;
                        }
                    }
                    else
                    {
                        // Regular file
                        if (processedPaths.Contains(fullPath))
                            continue; // Skip if already processed

                        ProjectData directoryNode = findOrCreatePath(Path.GetDirectoryName(projectRelativePath), rootNode);

                        ProjectData newItem = new ProjectData
                        {
                            Name = Path.GetFileName(fullPath),
                            Path = projectRelativePath,
                            CommitPath = relativePath,
                            IsDirectory = false,
                            Status = statusFlag,
                            Icon = GetFileIcon(projectRelativePath) // Use relative path
                        };

                        directoryNode.Children.Add(newItem);
                        processedPaths.Add(fullPath);
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
                commitButton.SetEnabled(IsAnyFileSelected());
                revertButton.SetEnabled(IsAnyFileSelected());
                commitMessageField.SetEnabled(IsAnyFileSelected());
            }

            treeView = rootVisualElement.Q<TreeView>("TreeView");
            treeView.Rebuild();
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

        // Function to gather selected files, meta files, and handle deleted folders
        private List<string> GetSelectedFiles()
        {
            List<string> selectedFiles = new List<string>();

            // Recursively add files and meta files, even if they are deleted
            AddSelectedFilesRecursive(projectRoot, selectedFiles);

            // Handle deleted folders and their meta files
            HandleDeletedFolders(projectRoot, selectedFiles);

            return selectedFiles;
        }

        private bool IsAnyFileSelected()
        {
            if (inProcess)
            {
                return false;
            }
            else
            {
                return GetSelectedFiles().Count > 0;
            }
        }

        private bool AddSelectedFilesRecursive(ProjectData node, List<string> selectedFiles)
        {
            CLIStatus status = DataManager.GetStatus();
            var notStagedFiles = status.NotStaged;

            bool hasFileSelected = false;

            if (node.IsChecked)
            {
                if (node.IsDirectory)
                {
                    // For empty or deleted directories, the CommitPath includes ".meta"
                    if (notStagedFiles.ContainsKey(node.CommitPath) && !selectedFiles.Contains(node.CommitPath))
                    {
                        selectedFiles.Add(node.CommitPath);
                    }

                    hasFileSelected = true;

                    // If the directory is deleted, include all child items
                    if (node.Status == "D" && node.Children != null)
                    {
                        foreach (var child in node.Children)
                        {
                            child.IsChecked = true; // Mark child as checked
                            AddSelectedFilesRecursive(child, selectedFiles);
                        }
                    }
                }
                else
                {
                    // Node is a file
                    if (notStagedFiles.ContainsKey(node.CommitPath) && !selectedFiles.Contains(node.CommitPath))
                    {
                        selectedFiles.Add(node.CommitPath);

                        // Add the .meta file if it exists in notStagedFiles
                        string metaFilePath = node.CommitPath + ".meta";
                        if (notStagedFiles.ContainsKey(metaFilePath) && !selectedFiles.Contains(metaFilePath))
                        {
                            selectedFiles.Add(metaFilePath);
                        }

                        hasFileSelected = true;
                    }
                }
            }

            // Process children
            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    bool childHasFileSelected = AddSelectedFilesRecursive(child, selectedFiles);
                    if (childHasFileSelected)
                    {
                        hasFileSelected = true;
                    }
                }
            }

            // If any file in this folder is selected, add the folder's .meta file
            if (hasFileSelected && node.IsDirectory && node.CommitPath != node.Parent?.CommitPath)
            {
                if (notStagedFiles.ContainsKey(node.CommitPath) && !selectedFiles.Contains(node.CommitPath))
                {
                    selectedFiles.Add(node.CommitPath);
                }

                // Also process parent folders
                AddUncommittedParentFolders(node.Parent, selectedFiles, notStagedFiles);
            }

            return hasFileSelected;
        }

        private void AddUncommittedParentFolders(ProjectData node, List<string> selectedFiles, Dictionary<string, string> notStagedFiles)
        {
            if (node == null || string.IsNullOrEmpty(node.CommitPath))
            {
                return; // Stop recursion if no parent
            }

            // For directories, CommitPath includes .meta
            if (notStagedFiles.ContainsKey(node.CommitPath) && !selectedFiles.Contains(node.CommitPath))
            {
                selectedFiles.Add(node.CommitPath); // Add the .meta file
            }

            // Recursively add parent folders and their .meta files
            if (node.Parent != null)
            {
                AddUncommittedParentFolders(node.Parent, selectedFiles, notStagedFiles);
            }
        }

        private void HandleDeletedFolders(ProjectData node, List<string> selectedFiles)
        {
            CLIStatus status = DataManager.GetStatus();
            var notStagedFiles = status.NotStaged;

            if (node.IsChecked && node.Status == "D") // Check if the node is checked and deleted
            {
                // Add the node's commit path if it's in notStagedFiles
                if (notStagedFiles.ContainsKey(node.CommitPath) && !selectedFiles.Contains(node.CommitPath))
                {
                    selectedFiles.Add(node.CommitPath);
                }

                // Add the corresponding .meta file
                string metaFilePath = node.CommitPath + ".meta";
                if (notStagedFiles.ContainsKey(metaFilePath) && !selectedFiles.Contains(metaFilePath))
                {
                    selectedFiles.Add(metaFilePath);
                }

                // Recursively process child nodes to include them
                if (node.Children != null)
                {
                    foreach (var child in node.Children)
                    {
                        // Mark the child as checked
                        child.IsChecked = true;

                        // Recursively call HandleDeletedFolders on the child
                        HandleDeletedFolders(child, selectedFiles);
                    }
                }
            }
        }

        private int CalculateTotalChanges()
        {
            CLIStatus status = DataManager.GetStatus();
            int totalChanges = 0;

            // Ensure projectPath is initialized
            if (string.IsNullOrEmpty(projectPath))
            {
                projectPath = Directory.GetParent(Application.dataPath).FullName;
            }

            HashSet<string> processedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (status?.NotStaged != null)
            {
                foreach (var entry in status.NotStaged)
                {
                    string filePath = entry.Key;  // E.g., "Assets/SomeFile.cs"
                    string statusFlag = entry.Value;
                    string fullPath = Path.Combine(CLIConstants.WorkingDirectory, filePath);

                    // Exclude files outside the Unity project
                    if (!fullPath.StartsWith(projectPath, StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Skip if already processed
                    if (processedFiles.Contains(fullPath))
                        continue;

                    if (filePath.EndsWith(".meta"))
                    {
                        string baseFilePath = fullPath.Substring(0, fullPath.Length - 5); // Remove ".meta"

                        // Determine if the base path is a directory or a file
                        if (!Path.HasExtension(baseFilePath))
                        {
                            // It's a folder's .meta file; skip counting it
                            continue;
                        }
                        else
                        {
                            // It's a file's .meta file; proceed
                            if (processedFiles.Contains(baseFilePath))
                            {
                                continue; // Skip if the base file is already processed
                            }
                            else
                            {
                                processedFiles.Add(baseFilePath);
                                totalChanges++;
                            }
                        }
                    }
                    else
                    {
                        // It's a regular file
                        processedFiles.Add(fullPath);
                        totalChanges++;
                    }
                }
            }
            return totalChanges;
        }
        
        private void OnCommandOutputReceived(string output)
        {
            // Update the UI on the main thread
            EditorApplication.delayCall += () =>
            {
                btnStr = output;

                if (commitButton != null && btnStr == "Pushing git changes")
                {
                    commitButton.text = "Commit Changed Files";
                }
                
                if (btnStr == "Status Command Completed")
                {
                    inProcess = false;
                    OnRevertComplete();
                }
            };
        }
        
        private void OnRevertComplete()
        {
            if (revertButton != null)
            {
                revertButton.text = "Revert";
            }
        }
        
        private void RefreshTreeItems(ProjectData itemData)
        {
            var idsToRefresh = new List<int>();
            GetAllItemIds(itemData, idsToRefresh);

            foreach (var id in idsToRefresh)
            {
                treeView.RefreshItem(id);
            }
        }

        private void GetAllItemIds(ProjectData itemData, List<int> ids)
        {
            ids.Add(itemData.Id);

            if (itemData.Children != null && itemData.Children.Any())
            {
                foreach (var child in itemData.Children)
                {
                    GetAllItemIds(child, ids);
                }
            }
        }
        
        private void Update()
        {
            if (this.docked || this.hasFocus)
            {
                CLIWrapper.isWindowActive = true;
            }
        }
    }
}