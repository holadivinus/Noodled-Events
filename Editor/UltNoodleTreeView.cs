using System;
using System.Collections.Generic;
using System.Linq;
using NoodledEvents;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class UltNoodleTreeView : GraphView
{
    public Action<UltNoodleNodeView> OnNodeSelected;
    public new class UxmlFactory : UxmlFactory<UltNoodleTreeView, GraphView.UxmlTraits> { }

    public UltNoodleBowl Bowl => _bowl;
    public UltNoodleSearchProvider Search => _searchWindow;

    private UltNoodleBowl _bowl;
    private UltNoodleSearchProvider _searchWindow;

    public UltNoodleTreeView()
    {
        Insert(0, new GridBackground());

        this.AddManipulator(new ContentZoomer());
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>($"{UltNoodleEditor.EditorFolder}/Styles/UltNoodleEditor.uss");
        styleSheets.Add(styleSheet);
    }

    public void InitializeSearch(EditorWindow editorWindow)
    {
        _searchWindow = ScriptableObject.CreateInstance<UltNoodleSearchProvider>();
        _searchWindow.Initialize(editorWindow, this);

        nodeCreationRequest = (ctx) =>
        {
            SearchWindow.Open(new SearchWindowContext(ctx.screenMousePosition), _searchWindow);
        };
    }

    public void UpdateNodes()
    {
        foreach (var node in _bowl.SerializedData.NodeDatas)
        {
            if (FindNodeView(node) != null) continue; // already exists

            var nodeView = new UltNoodleNodeView(node);
            nodeView.OnNodeSelected = OnNodeSelected;
            AddElement(nodeView);
        }
    }

    internal void PopulateView(UltNoodleBowl bowl)
    {
        _bowl = bowl;

        graphViewChanged -= OnGraphViewChanged;
        DeleteElements(graphElements);
        graphViewChanged += OnGraphViewChanged;

        DisplayAllNodes();
        DisplayAllConnections();
    }

    private UltNoodleNodeView FindNodeView(SerializedNode node)
    {
        return GetNodeByGuid(node.ID) as UltNoodleNodeView;
    }

    private void DisplayAllConnections()
    {
        foreach (var node in _bowl.SerializedData.NodeDatas)
        {
            var parentView = FindNodeView(node);

            foreach (var fo in node.FlowOutputs)
            {
                if (fo.Target != null)
                {
                    var childView = FindNodeView(fo.Target.Node);
                    var parentPort = parentView.GetPortForFlowOutput(fo);
                    var childPort = childView.GetPortForFlowInput(fo.Target);
                    if (parentPort != null && childPort != null)
                        AddElement(parentPort.ConnectTo(childPort));
                }
            }

            foreach (var dout in node.DataOutputs)
            {
                foreach (var target in dout.Targets)
                {
                    var childView = FindNodeView(target.Node);
                    var parentPort = parentView.GetPortForDataOutput(dout);
                    var childPort = childView.GetPortForDataInput(target);
                    if (parentPort != null && childPort != null)
                        AddElement(parentPort.ConnectTo(childPort));
                }
            }
        }
    }


    private void DisplayAllNodes()
    {
        foreach (var node in _bowl.SerializedData.NodeDatas)
        {
            var nodeView = new UltNoodleNodeView(node);
            nodeView.OnNodeSelected = OnNodeSelected;
            AddElement(nodeView);
        }
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        // TODO: compatibility logic
        return ports.ToList().Where(endPort => endPort.direction != startPort.direction && endPort.node != startPort.node).ToList();
    }

    private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
    {
        if (graphViewChange.elementsToRemove != null)
        {
            foreach (var element in graphViewChange.elementsToRemove)
            {
                if (element is UltNoodleNodeView nodeView)
                {
                    _bowl.SerializedData.NodeDatas.Remove(nodeView.Node);
                }

                if (element is Edge edge)
                {
                    var parentView = edge.output.node as UltNoodleNodeView;
                    var childView = edge.input.node as UltNoodleNodeView;

                    if (parentView != null && childView != null)
                    {
                        if (edge.output.userData is NoodleFlowOutput fo &&
                            edge.input.userData is NoodleFlowInput fi)
                        {
                            fo.Connect(null);
                        }

                        if (edge.output.userData is NoodleDataOutput dout &&
                            edge.input.userData is NoodleDataInput din)
                        {
                            din.Connect(null);
                        }
                    }
                }
            }
        }

        if (graphViewChange.edgesToCreate != null)
        {
            foreach (var edge in graphViewChange.edgesToCreate)
            {
                var parentView = edge.output.node as UltNoodleNodeView;
                var childView = edge.input.node as UltNoodleNodeView;

                if (parentView != null && childView != null)
                {
                    if (edge.output.userData is NoodleFlowOutput fo &&
                        edge.input.userData is NoodleFlowInput fi)
                    {
                        fi.Connect(fo);
                    }

                    if (edge.output.userData is NoodleDataOutput dout &&
                        edge.input.userData is NoodleDataInput din)
                    {
                        din.Connect(dout);
                    }
                }
            }
        }

        if (graphViewChange.movedElements != null)
        {
            foreach (var element in graphViewChange.movedElements)
            {
                if (element is UltNoodleNodeView view)
                {
                    view.Node.Position = view.GetPosition().position;
                }
            }
        }

        return graphViewChange;
    }
}
