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
        
        private VisualElement connectAnchorpointView;
        private VisualElement connectTryAgainView;
        private VisualElement connectedView;
        private VisualElement reconnectView;
        private VisualElement root;

        private List<TreeViewItemData<ProjectData>> treeViewItems = new List<TreeViewItemData<ProjectData>>();
        private ProjectData projectRoot; // Root of the project data tree
        private TreeView treeView;
        private TextField commitMessageField;
        private Label changesLabel;
        private Label noticeLable;
        private Button commitButton;
        private Button revertButton;
        private Button allButton;
        private Button noneButton;
        private Button refreshButton;
        private Button disconnectButton;
        private Button helpConnectedWinButton;
        private VisualElement loadingImg;
        private VisualElement refreshImg;
        
        
        //  Connect to Anchorpoint window
        private Label descriptionConnectWin;
        private Button connectToAnchorpoint;
        private Button helpConeectWindButton;
        
        //  Try again Anchorpoint window
        private Label descriptionTryAgainWin;
        private Button openAnchorpointButton;
        private Button trAgainAnchorpointButton;
        private Button helpTryAgainWindowButton;
        
        //  Pause window
        private Button reConnectToAnchorpoint;
        
        // Global paths
        private string projectPath;      // Absolute path to the Unity project root
        private string btnStr;
        private const string assetsFolderName = "Assets";
        private const string anchorPointIcon = "d8e0264a1e3a54b09aaf9e7ac62d4e1f";
        private const string helpUrl = "https://docs.anchorpoint.app/docs/version-control/first-steps/unity/";
        private const string noProjectErrorDescription = "This Unity project is not maintained by Anchorpoint. You will need to create a project first.\n\nCheck the documentation for help.";
        private const string validatingDescription = "Anchorpoint’s desktop application is not available.\n\nCheck the documentation for help.";
        
        private bool inProcess = false;      // Flag to check is if some commit/revert is in process
        
        private void OnEnable()
        {
            CLIWrapper.RefreshWindow += OnEditorUpdate;
            CLIWrapper.OnCommandOutputReceived += OnCommandOutputReceived;
            PluginInitializer.RefreshView += RefreshView;
        }

        private void OnDisable()
        {
            CLIWrapper.RefreshWindow -= OnEditorUpdate;
            CLIWrapper.OnCommandOutputReceived -= OnCommandOutputReceived;
            PluginInitializer.RefreshView -= RefreshView;
        }

        private void OnEditorUpdate()
        {
            AnchorpointLogger.LogWarning( CLIWrapper.isWindowActive.ToString());
            
            if (!CLIWrapper.isWindowActive) return;
            
            AnchorpointLogger.LogWarning( CLIWrapper.isWindowActive.ToString());
            rootVisualElement.Clear();
            CreateGUI();
        }

        [MenuItem("Window/Anchorpoint")]
        public static void ShowWindow()
        {
            AnchorpointEditor window = GetWindow<AnchorpointEditor>();

            string assetPath = AssetDatabase.GUIDToAssetPath(anchorPointIcon);
            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            window.titleContent = new GUIContent("Anchorpoint", icon);
        }

        public void CreateGUI()
        {
            root = rootVisualElement;
            
            VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
            root.Add(labelFromUXML);
            
            //  Getting three windows in the flow
            connectAnchorpointView = root.Q<VisualElement>("ConnectAnchorpoint");
            connectTryAgainView = root.Q<VisualElement>("ConnectTryAgain");
            connectedView = root.Q<VisualElement>("Connected");
            reconnectView = root.Q<VisualElement>("PauseAnchorpoint");

            RefreshView();
        }

        private void RefreshView()
        {
            if (PluginInitializer.IsNotAnchorpointProject)
            {
                ShowNoProjectError();
            }
            else if (PluginInitializer.IsInitialized && PluginInitializer.IsConnected)
            {
                ShowConnectedWindow();
            }
            else if (PluginInitializer.IsPlaymode && PluginInitializer.WasConnected)
            {
                ShowReconnectWindow();
            }
            else
            {
                ShowConnectWindow();
            }
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
                    if (inProcess)
                    {
                        // Revert the toggle to the old state
                        checkbox.SetValueWithoutNotify(!evt.newValue);
                        return;
                    }
                    
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
                AnchorpointLogger.LogWarning("CLIStatus is null");
                CLIWrapper.Status();
                return new ProjectData { Name = "No CLI Data" };
            }

            projectPath = Directory.GetParent(Application.dataPath)?.FullName;

            string projectName = Directory.GetParent(Application.dataPath)?.Name;
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
            
            Dictionary<string, string> combined = CombineStagedAndUnstaged(status.Staged, status.NotStaged);
            addFilesToStructure(combined, projectRoot);
            
            return projectRoot;
        }
        
        private Dictionary<string, string> CombineStagedAndUnstaged(Dictionary<string, string> stagedFiles, Dictionary<string, string> notStagedFiles)
        {
            Dictionary<string, string> combined = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Merge staged files
            if (stagedFiles != null)
            {
                foreach (var kvp in stagedFiles)
                {
                    combined[kvp.Key] = kvp.Value;
                }
            }

            // Merge not staged files
            if (notStagedFiles != null)
            {
                foreach (var kvp in notStagedFiles)
                {
                    if (!combined.ContainsKey(kvp.Key))
                    {
                        // Not in combined yet; just add it
                        combined[kvp.Key] = kvp.Value;
                    }
                    else
                    {
                        // It's in both dictionaries. Resolve the final status
                        string existingStatus = combined[kvp.Key];
                        string newStatus = kvp.Value;
                        combined[kvp.Key] = ResolveUnionStatus(existingStatus, newStatus);
                    }
                }
            }

            return combined;
        }
        
        /// <summary>
        /// Merges two status strings for the same file and returns a unified status.
        ///  resolving conflicts via ResolveUnionStatus when a file appears in both.
        ///  - "A" + "M" => "M"
        ///  - "M" + "M" => "M"
        ///  - "A" + "A" => "A"
        ///  - "D" (deleted) has highest priority
        ///  - "M" (modified) is next
        ///  - "A" (added) is lowest
        /// </summary>
        private string ResolveUnionStatus(string existingStatus, string newStatus)
        {
            // Put the statuses in a small set for easy checks
            var statuses = new HashSet<string> { existingStatus, newStatus };

            // If either state is "D" (Deleted), that usually takes precedence
            if (statuses.Contains("D"))
                return "D";

            // If either state is "M" (Modified), we unify as "M"
            if (statuses.Contains("M"))
                return "M";

            // If either state is "A" (Added), unify as "A"
            if (statuses.Contains("A"))
                return "A";

            // Otherwise, fall back to the existing status 
            // (or newStatus, or even "Unknown" if you prefer)
            return existingStatus;
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
            if (status == null) return false;

            // 1. Merge them
            Dictionary<string, string> mergedFiles = CombineStagedAndUnstaged(status.Staged, status.NotStaged);

            bool hasFileSelected = false;

            if (node.IsChecked)
            {
                if (node.IsDirectory)
                {
                    // For empty or deleted directories, the CommitPath includes ".meta"
                    if (mergedFiles.ContainsKey(node.CommitPath) && !selectedFiles.Contains(node.CommitPath))
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
                    if (mergedFiles.ContainsKey(node.CommitPath) && !selectedFiles.Contains(node.CommitPath))
                    {
                        selectedFiles.Add(node.CommitPath);

                        // Check for .meta file if you want
                        string metaFilePath = node.CommitPath + ".meta";
                        if (mergedFiles.ContainsKey(metaFilePath) && !selectedFiles.Contains(metaFilePath))
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
                if (mergedFiles.ContainsKey(node.CommitPath) && !selectedFiles.Contains(node.CommitPath))
                {
                    selectedFiles.Add(node.CommitPath);
                }
                // Also process parent folders...
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

            if (status == null)
            {
                return 0;
            }

            // Merge staged & not staged into a single dictionary
            Dictionary<string, string> merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            // Add staged files first
            if (status.Staged != null)
            {
                foreach (var kvp in status.Staged)
                {
                    merged[kvp.Key] = kvp.Value;
                }
            }

            // Merge not staged
            if (status.NotStaged != null)
            {
                foreach (var kvp in status.NotStaged)
                {
                    if (!merged.ContainsKey(kvp.Key))
                    {
                        // Not in merged yet, just add it
                        merged[kvp.Key] = kvp.Value;
                    }
                    else
                    {
                        // It's in both. Decide how to unify statuses:
                        string existingStatus = merged[kvp.Key];
                        string newStatus = kvp.Value;
                        merged[kvp.Key] = ResolveUnionStatus(existingStatus, newStatus);
                    }
                }
            }

            // Now count unique files from the merged dictionary
            HashSet<string> processedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in merged)
            {
                string filePath = entry.Key;    // e.g., "Assets/SomeFile.cs"
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
                    // It's a .meta file
                    string baseFilePath = fullPath.Substring(0, fullPath.Length - 5); // remove ".meta"

                    // If no extension => it's a folder .meta => skip
                    if (!Path.HasExtension(baseFilePath))
                        continue;

                    // It's a file's .meta => count it if we haven't processed the base file
                    if (!processedFiles.Contains(baseFilePath))
                    {
                        processedFiles.Add(baseFilePath);
                        totalChanges++;
                    }
                }
                else
                {
                    // It's a regular file
                    processedFiles.Add(fullPath);
                    totalChanges++;
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
                    ChangingUIInProgress(true);
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
        
        private void Update()
        {
            if (this.docked || this.hasFocus)
            {
                CLIWrapper.isWindowActive = true;
            }
        }

        private void ShowConnectWindow()
        {
            AnchorpointLogger.Log("ShowConnectWindow Shown");
            
            connectAnchorpointView.style.display = DisplayStyle.Flex;
            connectTryAgainView.style.display = DisplayStyle.None;
            connectedView.style.display = DisplayStyle.None;
            reconnectView.style.display = DisplayStyle.None;
            
            //  Getting the Connect to Anchorpoint window
            descriptionConnectWin = root.Q<Label>("DescriptionConnectWindow");
            connectToAnchorpoint = root.Q<Button>("ConnectToAnchorpoint");
            helpConeectWindButton = root.Q<Button>("HelpConnectWindow");
            
            connectToAnchorpoint.text = "Connect to Anchorpoint";
            descriptionConnectWin.text = "Connect to Anchorpoint to commit and view the status of files from within Unity.";
            
            connectToAnchorpoint.SetEnabled(true);
            
            connectToAnchorpoint.clickable.clicked -= ConnectToAnchorPoint;
            helpConeectWindButton.clickable.clicked -= Help;
            
            connectToAnchorpoint.clickable.clicked += ConnectToAnchorPoint;
            helpConeectWindButton.clickable.clicked += Help;
        }
        
        private void ShowNoProjectError()
        {
            AnchorpointLogger.Log("ShowNoProjectError Shown");
            
            connectAnchorpointView.style.display = DisplayStyle.None;
            connectTryAgainView.style.display = DisplayStyle.Flex;
            connectedView.style.display = DisplayStyle.None;
            reconnectView.style.display = DisplayStyle.None;
            
            descriptionTryAgainWin = root.Q<Label>("DescriptionTryAgainWindow");
            trAgainAnchorpointButton = root.Q<Button>("TryAgain");
            openAnchorpointButton = root.Q<Button>("OpenAnchorpoint");
            helpTryAgainWindowButton = root.Q<Button>("HelpTryAgainWindow");

            descriptionTryAgainWin.text = noProjectErrorDescription;

            trAgainAnchorpointButton.clickable.clicked -= PluginInitializer.StartConnection;
            openAnchorpointButton.clickable.clicked -= AnchorpointChecker.OpenAnchorpointApplication;
            helpTryAgainWindowButton.clickable.clicked -= Help;
           
            trAgainAnchorpointButton.clickable.clicked += PluginInitializer.StartConnection;
            openAnchorpointButton.clickable.clicked += AnchorpointChecker.OpenAnchorpointApplication;
            helpTryAgainWindowButton.clickable.clicked += Help;
        }
        
        private void ShowConnectedWindow()
        {
            connectAnchorpointView.style.display = DisplayStyle.None;
            connectTryAgainView.style.display = DisplayStyle.None;
            connectedView.style.display = DisplayStyle.Flex;
            reconnectView.style.display = DisplayStyle.None;
            
            // Get the commit message text field and commit button
            commitMessageField = root.Q<TextField>("CommitMessageField");
            commitButton = root.Q<Button>("CommitButton");
            commitButton.SetEnabled(false); // Disable the commit button initially
            revertButton = root.Q<Button>("Revert");
            revertButton.SetEnabled(false);

            refreshButton = root.Q<Button>("Refresh");
            loadingImg = root.Q<VisualElement>("LoadingImg");
            refreshImg = root.Q<VisualElement>("RefreshImg");
            
            disconnectButton = root.Q<Button>("Disconnect");
            helpConnectedWinButton = root.Q<Button>("ConnectedHelp");

            changesLabel = root.Q<Label>("ChangeCountLabel");
            int totalChanges = CalculateTotalChanges();
            changesLabel.text = $"Total Changes: {totalChanges}";

            allButton = root.Q<Button>("AllButton");
            noneButton = root.Q<Button>("NoneButton");

            treeView = root.Q<TreeView>("TreeView");

            noticeLable = root.Q<Label>("Notice");
            noticeLable.style.display = PluginInitializer.IsProjectOpen ? DisplayStyle.None : DisplayStyle.Flex;
            
            // When the commit button is clicked, gather selected files and commit them
            commitButton.clickable.clicked += () =>
            {
                string commitMessage = commitMessageField.value;
                List<string> filesToCommit = GetSelectedFiles();
                
                if (IsAnyFileSelected())
                {
                    inProcess = true;
                    ChangingUIInProgress(false);
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
                    ChangingUIInProgress(false);
                    revertButton.text = "Reverting...";
                    CLIWrapper.Revert(filesToRevert.ToArray());
                }
                else
                {
                    AnchorpointLogger.LogWarning("No files selected for revert.");
                }
            };

            loadingImg.style.display = DisplayStyle.None;
            refreshImg.style.display = DisplayStyle.Flex;
            
            allButton.clickable.clicked += () => { SetAllCheckboxes(true); };
            noneButton.clickable.clicked += () => { SetAllCheckboxes(false); };
            
            refreshButton.clickable.clicked -= Refresh;
            disconnectButton.clickable.clicked -= Disconnect;
            helpConnectedWinButton.clickable.clicked -= Help;
            
            refreshButton.clickable.clicked += Refresh;
            disconnectButton.clickable.clicked += Disconnect;
            helpConnectedWinButton.clickable.clicked += Help;
            
            CreateTreeUnity(treeView);
        }

        private void ShowReconnectWindow()
        {
            AnchorpointLogger.Log("ShowReconnectWindow Shown");
            
            connectAnchorpointView.style.display = DisplayStyle.None;
            connectTryAgainView.style.display = DisplayStyle.None;
            connectedView.style.display = DisplayStyle.None;
            reconnectView.style.display = DisplayStyle.Flex;
            
            reConnectToAnchorpoint = root.Q<Button>("ReconnectToAnchorpoint");
            
            reConnectToAnchorpoint.SetEnabled(true);
            
            reConnectToAnchorpoint.clickable.clicked -= PluginInitializer.StartConnection;
            reConnectToAnchorpoint.clickable.clicked += () => {
                PluginInitializer.StartConnection();
                reConnectToAnchorpoint.SetEnabled(false);
            };
        }

        private void Refresh()
        {
            loadingImg.style.display = DisplayStyle.Flex;
            refreshImg.style.display = DisplayStyle.None;
            CLIWrapper.Status();
            ChangingUIInProgress(false);
        }

        private void Disconnect()
        {
            PluginInitializer.StopConnectionExt();
            ShowConnectWindow();
        }

        private void Help()
        {
            Application.OpenURL(helpUrl);
        }
        
        private void ConnectToAnchorPoint()
        {
            AnchorpointLogger.Log("ConnectToAnchorPoint");

            if (!ValidatingAnchorPoint())
            {
                return;
            }

            if (PluginInitializer.IsConnected)
            {
                AnchorpointLogger.LogWarning("Already connected.");
            }
            else
            {
                PluginInitializer.StartConnection();
                connectToAnchorpoint.text = "Connecting...";
                connectToAnchorpoint.SetEnabled(false);
            }
        }

        private bool ValidatingAnchorPoint()
        {
            if (AnchorpointChecker.IsAnchorpointInstalled())
            {
                return true;
            }

            descriptionConnectWin.text = validatingDescription;

            return false;
        }

        private void ChangingUIInProgress(bool flag)
        {
            allButton.SetEnabled(flag);
            noneButton.SetEnabled(flag);
            refreshButton.SetEnabled(flag);
            disconnectButton.SetEnabled(flag);
            helpConnectedWinButton.SetEnabled(flag);
            commitMessageField.SetEnabled(flag);
            commitButton.SetEnabled(flag);
            revertButton.SetEnabled(flag);
        }
    }
}