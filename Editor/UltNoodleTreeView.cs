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

    // this is a bit awkward due to the focus function call chain
    public bool IsSearchOpen => _openingSearch || _searchWindow != null;

    public UltNoodleBowl Bowl => _bowl;
    public Vector2 NewNodeSpawnPos => _newNodeSpawnPos;

    private UltNoodleBowl _bowl;
    private UltNoodleSearchWindow _searchWindow;
    private bool _openingSearch;
    private Vector2 _newNodeSpawnPos;

    // TODO: copy/pasting and duplication doesn't work for some reason
    // TODO: zoom to fit all nodes on open
    // TODO: is undo/redo possible?

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

    public void InitializeSearch(EditorWindow baseWindow)
    {
        nodeCreationRequest = (ctx) =>
        {
            Vector2 localPos = baseWindow.rootVisualElement.ChangeCoordinatesTo(
                baseWindow.rootVisualElement.parent,
                ctx.screenMousePosition - baseWindow.position.position
            );
            _newNodeSpawnPos = contentViewContainer.WorldToLocal(localPos);

            _openingSearch = true;
            _searchWindow = UltNoodleSearchWindow.Open(this, ctx.screenMousePosition);
            _openingSearch = false;
        };
    }

    public void ForceCloseSearch()
    {
        if (_searchWindow == null) return;
        _searchWindow.Close();
        _searchWindow = null;
    }

    public void RenderNewNodes()
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
                    {
                        AddElement(parentPort.ConnectTo(childPort));
                        ToggleNodeConstantField(childPort, false);
                    }
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
        return ports.ToList().Where(endPort =>
        {
            if (endPort.direction == startPort.direction) return false;
            if (endPort.node == startPort.node) return false;

            // flow ports
            if (startPort.userData is NoodleFlowOutput fo && endPort.userData is NoodleFlowInput fi)
                return true;
            if (startPort.userData is NoodleFlowInput fi2 && endPort.userData is NoodleFlowOutput fo2)
                return true;

            // data ports
            if (startPort.userData is NoodleDataOutput dout && endPort.userData is NoodleDataInput din)
                return din.Type.Type.IsAssignableFrom(dout.Type.Type);
            if (startPort.userData is NoodleDataInput din2 && endPort.userData is NoodleDataOutput dout2)
                return din2.Type.Type.IsAssignableFrom(dout2.Type.Type);

            return false;
        }).ToList();
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
                            ToggleNodeConstantField(edge.input, true);
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
                        ToggleNodeConstantField(edge.input, false);
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
    
    private void ToggleNodeConstantField(Port port, bool show)
    {
        if (port.direction == Direction.Output) return;

        var container = port.GetFirstAncestorOfType<VisualElement>();
        var field = container?.Q<VisualElement>("ConstantField");
        if (field != null)
            field.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
