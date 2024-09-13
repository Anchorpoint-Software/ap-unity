using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AnchorPoint.Editor
{
    public class AnchorpointEditor : EditorWindow
    {
        [SerializeField] private VisualTreeAsset m_VisualTreeAsset = default;
        
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

            CreateTreeUnity(root.Q<TreeView>());
        }

        private void CreateTreeUnity(TreeView treeView)
        {
            var projectRoot = GetProjectStructure(Application.dataPath);

            var items = new List<TreeViewItemData<ProjectData>>();

            int idCounter = 0;
            PopulateTreeItems(projectRoot, items, ref idCounter);

            Func<VisualElement> makeItem = () =>
            {
                var container = new VisualElement();
                container.style.flexDirection = FlexDirection.Row;

                var checkbox = new Toggle() { name = "checkbox" };
                checkbox.style.marginRight = 5;
                checkbox.style.marginTop = 3.5f;

                var icon = new Image() { name = "icon" };
                icon.style.width = 16;
                icon.style.height = 16;
                icon.style.marginTop = 3;

                var label = new Label() { name = "name" };
                label.style.marginTop = 3;

                container.Add(checkbox);
                container.Add(icon);
                container.Add(label);

                return container;
            };

            Action<VisualElement, int> bindItem = (element, index) =>
            {
                var itemData = treeView.GetItemDataForIndex<ProjectData>(index);
                var checkbox = element.Q<Toggle>("checkbox");
                var icon = element.Q<Image>("icon");
                var nameLabel = element.Q<Label>("name");

                nameLabel.text = itemData.Name;

                // Try to fetch the icon
                if (itemData.Icon != null)
                {
                    icon.image = itemData.Icon;
                }
                else
                {
                    // Use a fallback icon for directories and files
                    if (itemData.Children.Count > 0)
                    {
                        icon.image = EditorGUIUtility.IconContent("Folder Icon").image as Texture2D;
                    }
                    else
                    {
                        // Fallback for files - assign generic or specific file type icons
                        icon.image = GetFileIcon(itemData.Path);
                    }
                }

                // If it's a directory, hide the checkbox
                checkbox.style.display = itemData.Children.Count > 0 ? DisplayStyle.None : DisplayStyle.Flex;
            };

            treeView.SetRootItems(items);
            treeView.makeItem = makeItem;
            treeView.bindItem = bindItem;
            treeView.selectionType = SelectionType.Multiple;
            treeView.Rebuild();
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
            // Here we are checking for specific file types and assigning relevant icons
            if (path.EndsWith(".cs")) 
            {
                return EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D;
            }
            else if (path.EndsWith(".png") || path.EndsWith(".jpg") || path.EndsWith(".jpeg"))
            {
                return EditorGUIUtility.IconContent("Texture Icon").image as Texture2D;
            }
            else if (path.EndsWith(".prefab"))
            {
                return EditorGUIUtility.IconContent("Prefab Icon").image as Texture2D;
            }
            else if (path.EndsWith(".unity"))
            {
                return EditorGUIUtility.IconContent("SceneAsset Icon").image as Texture2D;
            }
            // Add more conditions for other types of assets (like Audio, Materials, etc.)

            // Default file icon if no specific type is found
            return EditorGUIUtility.IconContent("DefaultAsset Icon").image as Texture2D;
        }

        private ProjectData GetProjectStructure(string path)
        {
            // Retrieve the icon for the file or folder
            var icon = AssetDatabase.GetCachedIcon(path) as Texture2D;

            var projectData = new ProjectData
            {
                Name = Path.GetFileName(path),
                Path = path,
                Icon = icon
            };

            foreach (var directory in Directory.GetDirectories(path))
            {
                projectData.Children.Add(GetProjectStructure(directory));
            }

            foreach (var file in Directory.GetFiles(path))
            {
                if (!file.EndsWith(".meta")) // Skip .meta files
                {
                    var fileIcon = AssetDatabase.GetCachedIcon(file) as Texture2D;
                    projectData.Children.Add(new ProjectData
                    {
                        Name = Path.GetFileName(file),
                        Path = file,
                        Icon = fileIcon
                    });
                }
            }

            return projectData;
        }
    }

    public class ProjectData
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public Texture2D Icon { get; set; }
        public List<ProjectData> Children { get; set; } = new List<ProjectData>();
    }
}