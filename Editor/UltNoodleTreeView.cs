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
    public Port PendingEdgeOriginPort { get => _pendingEdgeOriginPort; internal set => _pendingEdgeOriginPort = value; }
    public Vector2 NewNodeSpawnPos { get => _newNodeSpawnPos; internal set => _newNodeSpawnPos = value; }

    private UltNoodleBowl _bowl;

    private Port _pendingEdgeOriginPort;
    private Vector2 _newNodeSpawnPos;

    // TODO: copy/pasting and duplication doesn't work for some reason
    // TODO: zoom to fit all nodes on open

    public UltNoodleTreeView()
    {
        Insert(0, new GridBackground());

        this.AddManipulator(new ContentZoomer());
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>($"{UltNoodleEditor.EditorFolder}/Styles/UltNoodleEditor.uss");
        styleSheets.Add(styleSheet);

        Undo.undoRedoPerformed += OnUndoRedo;
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

            UltNoodleSearchWindow.Open(this, ctx.screenMousePosition);
        };
    }

    public void OnUndoRedo()
    {
        if (_bowl != null)
            PopulateView(_bowl);
    }

    public void RenderNewNodes()
    {
        foreach (var node in _bowl.SerializedData.NodeDatas)
        {
            if (FindNodeView(node) != null) continue; // already exists

            var nodeView = new UltNoodleNodeView(node);
            nodeView.OnNodeSelected = OnNodeSelected;
            AddElement(nodeView);

            if (PendingEdgeOriginPort != null)
            {
                var originPort = PendingEdgeOriginPort; // cache it, it might get cleared while we're in this function
                List<Port> targetPorts = InternalGetCompatiblePorts(originPort, nodeView.GetAllPorts());
                if (targetPorts.Count == 0)
                {
                    Debug.LogWarning("No compatible ports found when creating new node, not connecting");
                    continue;
                }

                if (targetPorts.Count > 1)
                {
                    GenericMenu menu = new();
                    foreach (var port in targetPorts)
                    {
                        Port localPort = port;
                        menu.AddItem(new GUIContent(localPort.portName), false, () =>
                        {
                            var edge = originPort.ConnectTo(localPort);
                            AddElement(edge);
                            HandleEdgeCreation(edge);
                        });
                    }
                    menu.ShowAsContext();
                    return; // wait for user to pick from menu
                }

                Port targetPort = targetPorts.First();
                AddElement(originPort.ConnectTo(targetPort));
                HandleEdgeCreation(originPort.connections.First());
            }
        }
    }

    internal void PopulateView(UltNoodleBowl bowl)
    {
        if (bowl == null) // clear the view
        {
            _bowl = null;

            graphViewChanged -= OnGraphViewChanged;
            DeleteElements(graphElements);
            graphViewChanged += OnGraphViewChanged;
            return;
        }

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

            foreach (var fi in node.FlowInputs)
            {
                foreach (var fo in fi.Sources)
                {
                    var parentPort = parentView.GetPortForFlowInput(fi);
                    var childView = FindNodeView(fo.Node);
                    var childPort = childView.GetPortForFlowOutput(fo);
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
        return InternalGetCompatiblePorts(startPort, ports);
    }

    private List<Port> InternalGetCompatiblePorts(Port startPort, IEnumerable<Port> ports)
    {
        return ports.Where(endPort =>
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
                    Undo.RecordObject(_bowl.SerializedData, "Delete Node");

                    // manually removing edges due to how data input constants are implemented
                    foreach (var port in nodeView.GetAllPorts())
                    {
                        foreach (var portEdge in port.connections.ToList())
                            HandleEdgeRemoval(portEdge);
                    }
                    _bowl.SerializedData.NodeDatas.Remove(nodeView.Node);
                    EditorUtility.SetDirty(_bowl.SerializedData);
                }

                if (element is Edge edge)
                {
                    Undo.RecordObject(_bowl.SerializedData, "Remove Connection");
                    HandleEdgeRemoval(edge);
                    EditorUtility.SetDirty(_bowl.SerializedData);
                }
            }
        }

        if (graphViewChange.edgesToCreate != null)
        {
            foreach (var edge in graphViewChange.edgesToCreate)
            {
                Undo.RecordObject(_bowl.SerializedData, "Create Connection");
                HandleEdgeCreation(edge);
                EditorUtility.SetDirty(_bowl.SerializedData);
            }
        }

        if (graphViewChange.movedElements != null)
        {
            foreach (var element in graphViewChange.movedElements)
            {
                if (element is UltNoodleNodeView view)
                {
                    view.Node.Position = view.GetPosition().position;
                    EditorUtility.SetDirty(_bowl.SerializedData);
                }
            }
        }

        return graphViewChange;
    }

    private void HandleEdgeCreation(Edge edge)
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

    private void HandleEdgeRemoval(Edge edge)
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

        edge.input?.Disconnect(edge);
        edge.output?.Disconnect(edge);
        edge.parent?.Remove(edge);
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
