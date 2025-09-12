using UltEvents;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class UltNoodleInspectorView : VisualElement
{
    public new class UxmlFactory : UxmlFactory<UltNoodleInspectorView, VisualElement.UxmlTraits> { }

    public UltNoodleInspectorView()
    {
    }

    internal void UpdateSelection(UltNoodleNodeView nodeView)
    {
        Clear();

        // TODO: this ui is awful
        var container = new IMGUIContainer(() =>
        {
            foreach (var input in nodeView.Node.DataInputs)
            {
                UnityEditor.EditorGUILayout.LabelField(input.Name);

                if (input.Type.Type == typeof(object))
                {
                    UnityEditor.EditorGUI.BeginChangeCheck();
                    var newType = (PersistentArgumentType)UnityEditor.EditorGUILayout.EnumPopup("Type", input.ConstInput);
                    if (UnityEditor.EditorGUI.EndChangeCheck())
                    {
                        input.ConstInput = newType;
                        nodeView.RebuildConstantField(input);
                    }
                }
            }
        });

        Add(container);
    }
}
