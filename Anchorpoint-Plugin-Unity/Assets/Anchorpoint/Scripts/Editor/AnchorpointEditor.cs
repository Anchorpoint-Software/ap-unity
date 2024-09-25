using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        [MenuItem("Window/Anchorpoint")]
        public static void ShowWindow()
        {
            AnchorpointEditor window = GetWindow<AnchorpointEditor>();
            Texture2D icon = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/UI/Logos/anchorPointLogo.png", typeof(Texture2D));

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
                Debug.LogError("TreeView not found in the UXML hierarchy!");
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
                    Debug.LogError($"Number of files that are included are {filesToCommit.Count}");
                    Debug.LogError($"Path to the file {filesToCommit[0]}");
                    // CLIWrapper.Commit(commitMessage, filesToCommit.ToArray());
                }
                else
                {
                    Debug.LogWarning("No files selected for commit.");
                }
            };

            CreateTreeUnity(treeView);
        }

        private void CreateTreeUnity(TreeView treeView)
        {
            if (treeView == null)
            {
                Debug.LogError("TreeView is null!");
                return;
            }

            projectRoot = GetCliProjectStructure();

            if (projectRoot == null || projectRoot.Children.Count == 0)
            {
                Debug.LogWarning("No project structure data found or children are empty.");
                return;
            }
            else
            {
                Debug.Log($"Project Root found: {projectRoot.Name}, Child Count: {projectRoot.Children.Count}");
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
                    Debug.LogError($"File {itemData.Name} checked: {itemData.IsChecked}");
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

                if (itemData.Icon != null)
                {
                    icon.image = itemData.Icon;
                }
                else
                {
                    icon.image = GetFileIcon(itemData.Path);
                }

                checkbox.style.display = itemData.IsDirectory ? DisplayStyle.None : DisplayStyle.Flex;
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
                Debug.LogError("CLIStatus is null");
                return new ProjectData { Name = "No CLI Data" };
            }

            ProjectData projectRoot = null; // We'll store the actual root folder as the root

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

                    // Set the project root to the first node (i.e., the actual project folder)
                    if (projectRoot == null)
                    {
                        projectRoot = childNode;
                    }
                    currentNode.Children.Add(childNode);
                }

                return findOrCreatePath(remainingPath, childNode);
            };

            // Add files to the correct directory
            Action<Dictionary<string, string>, ProjectData> addFilesToStructure = (files, rootNode) =>
            {
                foreach (var file in files)
                {
                    // Skip meta files
                    if (file.Key.EndsWith(".meta"))
                        continue;

                    string fullPath = file.Key;

                    // Create or find the correct directory node
                    ProjectData directoryNode = findOrCreatePath(Path.GetDirectoryName(fullPath), rootNode);

                    // Add the file and its meta file to the tree
                    directoryNode.Children.Add(new ProjectData(Path.GetFileName(fullPath), fullPath, false)
                    {
                        Icon = GetFileIcon(fullPath)
                    });

                    // Check and add the meta file if it exists
                    string metaFilePath = fullPath + ".meta";
                    if (File.Exists(metaFilePath))
                    {
                        directoryNode.Children.Add(new ProjectData(Path.GetFileName(metaFilePath), metaFilePath, false)
                        {
                            Icon = GetFileIcon(metaFilePath)
                        });
                    }
                }
            };

            // Add not staged files to the structure
            if (status.NotStaged != null)
            {
                addFilesToStructure(status.NotStaged, new ProjectData { Name = "", Path = "", IsDirectory = true });
            }

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
            AddSelectedFilesRecursive(projectRoot, selectedFiles);
            return selectedFiles;
        }

        // Recursive function to collect selected files
        private void AddSelectedFilesRecursive(ProjectData node, List<string> selectedFiles)
        {
            if (node.IsChecked && !node.IsDirectory)
            {
                selectedFiles.Add(node.Path);
                string metaFilePath = node.Path + ".meta";
                if (File.Exists(metaFilePath))
                {
                    selectedFiles.Add(metaFilePath);
                }
            }

            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    AddSelectedFilesRecursive(child, selectedFiles);
                }
            }
        }

        private int CalculateTotalChanges()
        {
            CLIStatus status = DataManager.GetStatus();
            return status?.NotStaged?.Count ?? 0;
        }
    }
}
