using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class UltNoodleBowlSelector : VisualElement
{
    public new class UxmlFactory : UxmlFactory<UltNoodleBowlSelector, VisualElement.UxmlTraits> { }

    // TODO: bowl renaming? or should that be done in the inspector?

    private UltNoodleEditor _editor;

    public UltNoodleBowlSelector()
    {
    }

    public void AttachToEditor(UltNoodleEditor editor)
    {
        _editor = editor;
        _editor.OnBowlsChanged += () =>
        {
            UpdateBowls();
        };

        UpdateBowls();
    }

    private void UpdateBowls()
    {
        Clear();
        IMGUIContainer container = new(() =>
        {
            if (_editor.Bowls.Count == 0)
            {
                EditorGUILayout.LabelField("No Bowls found");
                return;
            }
            foreach (var bowl in _editor.Bowls)
            {
                bool isSelected = bowl == _editor.CurrentBowl;
                GUIStyle style = isSelected ? new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold } : GUI.skin.button;
                if (GUILayout.Button(bowl.SerializedData.BowlName, style))
                {
                    _editor.SelectBowl(bowl);
                }
            }
        });
        Add(container);
    }
}
