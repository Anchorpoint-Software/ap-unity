    using System.Collections.Generic;
    using System.IO;
    using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class Anchorpoint : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    [MenuItem("Window/UI Toolkit/Anchorpoint")]
    public static void ShowExample()
    {
        Anchorpoint wnd = GetWindow<Anchorpoint>();
        wnd.titleContent = new GUIContent("Anchorpoint");
    }

    public void CreateGUI()
    {
        var allObjectGuids = AssetDatabase.FindAssets("");
        var allObjectsNames = new List<string>();
        foreach (var obj in allObjectGuids)
        {
            var name = Path.GetFileName(AssetDatabase.GUIDToAssetPath(obj));
            allObjectsNames.Add(name);
            Debug.Log(name);
        }
        
        // 2 buttons
        var buttonContainer = new VisualElement();
        buttonContainer.style.flexDirection = FlexDirection.Row;
        buttonContainer.style.minHeight = 30;
        buttonContainer.style.maxHeight = 30;
        buttonContainer.style.marginTop = 20;
        buttonContainer.style.bottom = 10;
        buttonContainer.style.left = 13;

        var allButton = new Button();
        allButton.text = "All";
        // allButton.style.marginTop = 10;
        // allButton.style.bottom = 10;
        // allButton.style.right = 15;
        // allButton.style.left = 7;
        buttonContainer.Add(allButton);
        
        var noneButton = new Button();
        noneButton.text = "None";
        // noneButton.style.marginTop = 10;
        // noneButton.style.bottom = 10;
        buttonContainer.Add(noneButton);
        
        rootVisualElement.Add(buttonContainer);
        
        var dataContainer = new VisualElement();

        var listView = new ListView();
        dataContainer.Add(listView);
        rootVisualElement.Add(dataContainer);

        
        
        listView.makeItem = () =>
        {
            // Create a new container that holds the Toggle and Label
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;

            // Create the checkbox
            var toggle = new Toggle();
            toggle.style.marginRight = 5; // Add some space between the checkbox and the label
            container.Add(toggle);

            // Create the label
            var label = new Label();
            container.Add(label);

            return container;
        };

        listView.bindItem = (item, index) =>
        {
            // Bind the label text
            var container = item as VisualElement;
            var label = container.ElementAt(1) as Label;
            label.text = allObjectsNames[index];
        };

        listView.itemsSource = allObjectsNames;

    }
}
