using System.Collections.Generic;
using NoodledEvents;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

// TODO: replace this with a custom search window; SearchWindowProvider doesn't work well with the scale we are working at
public class UltNoodleSearchProvider : ScriptableObject, ISearchWindowProvider
{
    private EditorWindow _editorWindow;
    private UltNoodleTreeView _graphView;

    public void Initialize(EditorWindow window, UltNoodleTreeView graphView)
    {
        _editorWindow = window;
        _graphView = graphView;
    }

    public int MaxResults = 100;
    public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
    {
        Debug.Log("CreateSearchTree called");
        Dictionary<CookBook, SearchTreeGroupEntry> cookBookGroups = new();
        int results = 0;
        var tree = new List<SearchTreeEntry>
        {
            new SearchTreeGroupEntry(new GUIContent("Create Node"), 0),
        };

        foreach (var node in UltNoodleEditor.AllNodeDefs)
        {
            if (results >= MaxResults)
                break;

            if (!cookBookGroups.ContainsKey(node.CookBook))
            {
                var group = new SearchTreeGroupEntry(new GUIContent(node.CookBook.Name), 1);
                cookBookGroups[node.CookBook] = group;
                tree.Add(group);
            }
            var entry = new SearchTreeEntry(new GUIContent(node.Name))
            {
                level = 2,
                userData = node
            };
            tree.Add(entry);
            results++;
        }

        if (results >= MaxResults)
        {
            tree.Add(new SearchTreeEntry(new GUIContent($"… Showing first {MaxResults} results")) { level = 1 });
        }

        return tree;
    }

    public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
    {
        Vector2 mousePos = _editorWindow.rootVisualElement.ChangeCoordinatesTo(
            _graphView.contentViewContainer,
            context.screenMousePosition - _editorWindow.position.position
        );

        if (entry.userData is CookBook.NodeDef nodeDef)
        {
            if (_graphView.Bowl == null) return false;

            var node = _graphView.Bowl.AddNode(nodeDef.Name, nodeDef.CookBook).MatchDef(nodeDef);
            node.BookTag = nodeDef.BookTag != string.Empty ? nodeDef.BookTag : nodeDef.Name;
            node.Position = mousePos;
            _graphView.Bowl.Validate();
            _graphView.UpdateNodes();

            return true;
        }

        return false;
    }
}