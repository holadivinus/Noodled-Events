#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class UltNoodleBowlSelector : VisualElement
{
    public new class UxmlFactory : UxmlFactory<UltNoodleBowlSelector, VisualElement.UxmlTraits> { }

    private UltNoodleEditor _editor;

    public UltNoodleBowlSelector() { }

    public void AttachToEditor(UltNoodleEditor editor)
    {
        _editor = editor;
        _editor.OnBowlsChanged += UpdateBowls;
        UpdateBowls();
    }

    private void UpdateBowls()
    {
        Clear();

        if (_editor.Bowls.Count == 0)
        {
            Add(new Label("No Bowls found"));
            return;
        }

        var scroll = new ScrollView();
        scroll.style.flexGrow = 1;
        Add(scroll);

        foreach (var bowl in _editor.Bowls)
        {
            bool isSelected = bowl == _editor.CurrentBowl;

            var box = new VisualElement();
            box.style.flexDirection = FlexDirection.Column;
            box.style.paddingTop = box.style.paddingBottom = 4;
            box.style.paddingLeft = box.style.paddingRight = box.style.marginBottom = 6;
            box.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
            box.style.borderTopWidth = box.style.borderBottomWidth = 1;
            box.style.borderLeftWidth = box.style.borderRightWidth = 1;
            box.style.borderTopColor = box.style.borderBottomColor = box.style.borderLeftColor = box.style.borderRightColor = new Color(0.25f, 0.25f, 0.25f);

            var nameField = new TextField(bowl.SerializedData.gameObject.name);
            nameField.labelElement.style.color = Color.white;
            nameField.labelElement.style.fontSize = 13;
            nameField.value = bowl.SerializedData.BowlName;
            if (isSelected)
                nameField.style.unityFontStyleAndWeight = FontStyle.Bold;

            nameField.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue != bowl.SerializedData.BowlName)
                    bowl.SerializedData.BowlName = evt.newValue;
            });
            box.Add(nameField);

            var pathLabel = new Label(bowl.SerializedData.EventHolder.GetType().Name + "." + bowl.SerializedData.EventFieldPath);
            pathLabel.style.fontSize = 11;
            pathLabel.style.color = Color.white;
            pathLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            pathLabel.style.marginTop = 2;
            box.Add(pathLabel);

            VisualElement buttonRow = new();
            buttonRow.style.flexDirection = FlexDirection.Row;

            var selectButton = new Button(() => _editor.SelectBowl(bowl))
            {
                text = isSelected ? "Selected" : "Select"
            };
            selectButton.style.flexGrow = 1;
            selectButton.style.marginRight = 0;
            if (isSelected)
            {
                selectButton.style.unityFontStyleAndWeight = FontStyle.Bold;
                selectButton.SetEnabled(false);
            }
            buttonRow.Add(selectButton);

            var gameObjectButton = new Button(() => EditorGUIUtility.PingObject(bowl.Component))
            {
                tooltip = "Highlight Bowl in Hierarchy",
            };
            gameObjectButton.style.width = 20;
            gameObjectButton.style.backgroundImage = new StyleBackground((Texture2D)EditorGUIUtility.IconContent("d_GameObject Icon").image);

            gameObjectButton.style.marginLeft = 0;

            buttonRow.Add(gameObjectButton);
            box.Add(buttonRow);
            scroll.Add(box);
        }
    }
}
#endif