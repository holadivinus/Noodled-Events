#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using NoodledEvents;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;
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

    // port from original
    // TODO: node method selection menu
    // TODO: dragging objects into graph (requires above)
    // TODO: varman variable selection menu
    // TODO: quick node delete button

    // general improvements
    // TODO: copy node default values
    // TODO: remember bowl when switching between show all and show selected

    // extras
    // TODO: grouping
    // TODO: handle multi selection and show child bowls w/ show selected bowls only
    // TODO: try to replicate the shadergraph redirect node
    // TODO: bookmarks window/popout list

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
        serializeGraphElements = SerializeGraphElementsImpl;
        unserializeAndPaste = UnserializeAndPasteImpl;
        canPasteSerializedData = CanPasteSerializedDataImpl;
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
                if (originPort.capacity == Port.Capacity.Single && originPort.connected)
                {
                    // we are already connected and can't handle multiple, let's disconnect first
                    var existingEdge = originPort.connections.First();
                    HandleEdgeRemoval(existingEdge);
                    RemoveElement(existingEdge);
                }

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
        DisplayAllNotes();
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

    private void DisplayAllNotes()
    {
        foreach (var note in _bowl.SerializedData.NoteDatas)
        {
            var noteView = new UltNoodleNoteView(note, _bowl.SerializedData);
            AddElement(noteView);
        }
    }

    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        if (_bowl == null) return;

        Vector2 localMousePos = contentViewContainer.WorldToLocal(evt.mousePosition);

        evt.menu.AppendAction("Add Note", (a) =>
        {
            Undo.RecordObject(_bowl.SerializedData, "Add Note");

            var note = new UltNoodleNoteData("", localMousePos);
            _bowl.SerializedData.NoteDatas.Add(note);
            EditorUtility.SetDirty(_bowl.SerializedData);

            var noteView = new UltNoodleNoteView(note, _bowl.SerializedData);
            AddElement(noteView);
        });

        base.BuildContextualMenu(evt);
    }

    private void OnUndoRedo()
    {
        if (_bowl != null)
            PopulateView(_bowl);
    }

    private string SerializeGraphElementsImpl(IEnumerable<GraphElement> elements)
    {
        var nodes = elements.OfType<UltNoodleNodeView>().Select(nv => new NodeData()
        {
            id = nv.Node.ID,
            cookBookName = nv.Node.Book.GetType().FullName,
            bookTag = nv.Node.BookTag,
            position = nv.GetPosition().position
        }).ToList();

        var edges = elements.OfType<Edge>()
            .Select(e =>
            {
                if (e.output?.node is UltNoodleNodeView fromNode &&
                    e.input?.node is UltNoodleNodeView toNode)
                {
                    return new EdgeData
                    {
                        fromNodeId = fromNode.Node.ID,
                        fromPortName = e.output.portName,
                        toNodeId = toNode.Node.ID,
                        toPortName = e.input.portName
                    };
                }
                return null;
            })
            .Where(ed => ed != null)
            .ToList();

        var wrapper = new NodeWrapper() { nodes = nodes, edges = edges };
        return JsonUtility.ToJson(wrapper);
    }

    private bool CanPasteSerializedDataImpl(string data)
    {
        try
        {
            var wrapper = JsonUtility.FromJson<NodeWrapper>(data);
            return wrapper != null && wrapper.nodes != null && wrapper.nodes.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    private void UnserializeAndPasteImpl(string operationName, string data)
    {
        var wrapper = JsonUtility.FromJson<NodeWrapper>(data);
        if (wrapper == null || wrapper.nodes == null || wrapper.nodes.Count == 0) return;

        Undo.RecordObject(_bowl.SerializedData, operationName);

        Dictionary<string, string> oldToNewIds = new();

        Vector2 avg = Vector2.zero;
        foreach (var nodeData in wrapper.nodes)
            avg += nodeData.position;
        avg /= wrapper.nodes.Count;

        Vector2 viewCenter = contentViewContainer.WorldToLocal(layout.center);

        ClearSelection();
        foreach (var nodeData in wrapper.nodes)
        {
            var book = UltNoodleEditor.AllBooks.FirstOrDefault(b => b.GetType().FullName == nodeData.cookBookName);
            if (book == null)
            {
                Debug.LogWarning($"Could not find cook book {nodeData.cookBookName} when pasting nodes, skipping");
                continue;
            }

            var def = UltNoodleEditor.AllNodeDefs.FirstOrDefault(d => d.BookTag == nodeData.bookTag && d.CookBook == book);
            if (def == null)
            {
                Debug.LogWarning($"Could not find node def {nodeData.id} in book {book.name} when pasting nodes, skipping");
                continue;
            }

            UltNoodleBowl bowl = UltNoodleEditor.Editor.CurrentBowl;
            if (bowl == null) return;
            var nod = bowl.AddNode(def.Name, book).MatchDef(def);
            nod.BookTag = def.BookTag != string.Empty ? def.BookTag : def.Name;

            if (operationName == "Duplicate")
                nod.Position = nodeData.position + new Vector2(20, 20); // offset a bit so user can see the duplicate
            else
                nod.Position = nodeData.position + viewCenter - avg; // keep relative positions, center around view

            bowl.Validate();
            UltNoodleEditor.Editor.TreeView.RenderNewNodes();
            oldToNewIds[nodeData.id] = nod.ID;

            AddToSelection(FindNodeView(nod));
        }

        foreach (var edgeData in wrapper.edges)
        {
            if (!oldToNewIds.TryGetValue(edgeData.fromNodeId, out var newFromId)) continue;
            if (!oldToNewIds.TryGetValue(edgeData.toNodeId, out var newToId)) continue;

            var fromNode = _bowl.SerializedData.NodeDatas.FirstOrDefault(n => n.ID == newFromId);
            var toNode = _bowl.SerializedData.NodeDatas.FirstOrDefault(n => n.ID == newToId);
            if (fromNode == null || toNode == null) continue;

            var fromView = FindNodeView(fromNode);
            var toView = FindNodeView(toNode);
            if (fromView == null || toView == null) continue;

            var fromPort = fromView.GetPortByName(edgeData.fromPortName, Direction.Output);
            var toPort = toView.GetPortByName(edgeData.toPortName, Direction.Input);
            if (fromPort == null || toPort == null) continue;

            var edge = fromPort.ConnectTo(toPort);
            AddElement(edge);
            HandleEdgeCreation(edge);
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
                return fo.CanConnectTo(fi);
            if (startPort.userData is NoodleFlowInput fi2 && endPort.userData is NoodleFlowOutput fo2)
                return fo2.CanConnectTo(fi2);

            // data ports
            if (startPort.userData is NoodleDataOutput dout && endPort.userData is NoodleDataInput din)
                return dout.Type.Type.IsAssignableFrom(din.Type.Type) || din.Type.Type.IsAssignableFrom(dout.Type.Type);
            if (startPort.userData is NoodleDataInput din2 && endPort.userData is NoodleDataOutput dout2)
                return dout2.Type.Type.IsAssignableFrom(din2.Type.Type) || din2.Type.Type.IsAssignableFrom(dout2.Type.Type);

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

    [Serializable]
    private class NodeWrapper
    {
        public List<NodeData> nodes;
        public List<EdgeData> edges;
    }

    [Serializable]
    private class NodeData
    {
        public string id;
        public string cookBookName;
        public string bookTag;
        public Vector2 position;
    }

    [Serializable]
    private class EdgeData
    {
        public string fromNodeId;
        public string fromPortName;
        public string toNodeId;
        public string toPortName;
    }
}
#endif