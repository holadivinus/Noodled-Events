using System;
using UnityEditor;
using UnityEngine.UIElements;

// TODO: do something with this or remove it
public class UltNoodleInspectorView : VisualElement
{
    public new class UxmlFactory : UxmlFactory<UltNoodleInspectorView, VisualElement.UxmlTraits> { }

    public UltNoodleInspectorView()
    {
    }

    internal void UpdateSelection(UltNoodleNodeView nodeView)
    {
        Clear();
    }
}
