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
    // TODO: quick node delete button

    // general improvements
    // TODO: copy node default values
    // TODO: remember bowl when switching between show all and show selected

    // extras
    // TODO: grouping
    // TODO: handle multi selection and show child bowls w/ show selected bowls only
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

    public void Validate()
    {
        if (_bowl == null) return;
        foreach (var node in _bowl.SerializedData.NodeDatas)
        {
            var view = FindNodeView(node);
            view?.Validate(true);
        }
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
                            HandleEdgeCreation(originPort, localPort, true);
                        });
                    }
                    menu.ShowAsContext();
                    return; // wait for user to pick from menu
                }

                Port targetPort = targetPorts.First();
                HandleEdgeCreation(originPort, targetPort, true);
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
                    {
                        HandleEdgeCreation(childPort, parentPort, false);
                    }
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
                        HandleEdgeCreation(parentPort, childPort, false);
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
            if (node.NoadType == SerializedNode.NodeType.Redirect)
            {
                var redirectView = new UltNoodleRedirectNodeView(node);
                redirectView.OnNodeSelected = OnNodeSelected;
                AddElement(redirectView);
                continue;
            }
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

            HandleEdgeCreation(fromPort, toPort, true);
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
        void HandleDelete(GraphElement element, List<GraphElement> extrasToRemove, bool forceUpdate = false)
        {
            if (element is UltNoodleNodeView nodeView)
            {
                Undo.RecordObject(_bowl.SerializedData, "Delete Node");

                if (nodeView is UltNoodleRedirectNodeView)
                {
                    // TODO: try to reconnect nodes that were connected to this redirect node
                    // the below seemed to work fine for flow ports, but data ports acted weird
                    /* var inEdges = graphViewChange.elementsToRemove
                        .OfType<Edge>()
                        .Where(e => e.input.node == nodeView)
                        .ToList();
                    var outEdges = graphViewChange.elementsToRemove
                        .OfType<Edge>()
                        .Where(e => e.output.node == nodeView)
                        .ToList();

                    foreach (var inEdge in inEdges)
                    {
                        foreach (var outEdge in outEdges)
                        {
                            // make sure we aren't connecting to a node that is also being deleted
                            if (!graphViewChange.elementsToRemove.Contains(inEdge.output.node)
                                && !graphViewChange.elementsToRemove.Contains(outEdge.input.node))
                                HandleEdgeCreation(inEdge.output, outEdge.input, true);
                        }
                    } */
                }
                else
                {
                    // manually removing edges due to how data input constants are implemented
                    foreach (var port in nodeView.GetAllPorts())
                    {
                        foreach (var portEdge in port.connections.ToList())
                            HandleEdgeRemoval(portEdge);
                    }
                }

                _bowl.SerializedData.NodeDatas.Remove(nodeView.Node);
                if (forceUpdate)
                    DeleteElements(new GraphElement[] { nodeView });

                EditorUtility.SetDirty(_bowl.SerializedData);
            }

            if (element is Edge edge)
            {
                Undo.RecordObject(_bowl.SerializedData, "Remove Connection");
                HandleEdgeRemoval(edge);

                if (edge.input.node is UltNoodleRedirectNodeView redirectView)
                {
                    // if both of our inputs are disconnected, delete this node
                    if (extrasToRemove != null && redirectView.GetAllPorts().All(p => !p.connected))
                        extrasToRemove.Add(redirectView);
                }

                if (edge.output.node is UltNoodleRedirectNodeView redirectView2)
                {
                    // if both of our inputs are disconnected, delete this node
                    if (extrasToRemove != null && redirectView2.GetAllPorts().All(p => !p.connected))
                        extrasToRemove.Add(redirectView2);
                }

                if (forceUpdate)
                    DeleteElements(new GraphElement[] { edge });

                EditorUtility.SetDirty(_bowl.SerializedData);
            }
        }

        List<GraphElement> extrasToRemove = new();
        if (graphViewChange.elementsToRemove != null)
        {
            foreach (var element in graphViewChange.elementsToRemove)
            {
                HandleDelete(element, extrasToRemove);
            }
        }

        if (extrasToRemove != null)
        {
            foreach (var extra in extrasToRemove)
            {
                HandleDelete(extra, null, true);
            }
        }

        if (graphViewChange.edgesToCreate != null)
        {
            foreach (var edge in graphViewChange.edgesToCreate)
            {
                Undo.RecordObject(_bowl.SerializedData, "Create Connection");
                HandleEdgeCreation(edge, true);
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

    private void HandleEdgeCreation(Port fromPort, Port toPort, bool connect)
    {
        Edge edge = fromPort.ConnectTo(toPort);
        AddElement(edge);
        HandleEdgeCreation(edge, connect);
    }

    private void HandleEdgeCreation(Edge edge, bool connect)
    {
        var fromPort = edge.output;
        var toPort = edge.input;

        var parentView = fromPort.node as UltNoodleNodeView;
        var childView = toPort.node as UltNoodleNodeView;

        if (connect && parentView != null && childView != null)
        {
            if (fromPort.userData is NoodleFlowOutput fo &&
                toPort.userData is NoodleFlowInput fi)
            {
                fi.Connect(fo);
            }

            if (fromPort.userData is NoodleDataOutput dout &&
                toPort.userData is NoodleDataInput din)
            {
                din.Connect(dout);
                ToggleNodeConstantField(toPort, false);
            }
        }

        // double click edge to create redirect node
        edge.RegisterCallback<MouseDownEvent>(evt =>
        {
            if (evt.clickCount != 2 || evt.button != 0) // double left click
                return;
            
            RemoveElement(edge);
            foreach (var portEdge in edge.output.connections.ToList())
                HandleEdgeRemoval(portEdge);

            var node = _bowl.AddNode("Redirector", UltNoodleEditor.AllBooks.First(c => c is CommonsCookBook));
            node.NoadType = SerializedNode.NodeType.Redirect;
            node.Position = contentViewContainer.WorldToLocal(evt.mousePosition) - new Vector2(36.5f, 20.5f); // center of node
            node.BookTag = "redirect";

            if (fromPort.userData is NoodleFlowOutput || fromPort.userData is NoodleFlowInput)
            {
                node.AddFlowIn("");
                node.AddFlowOut("");
            }
            else if (fromPort.userData is NoodleDataOutput dOut && toPort.userData is NoodleDataInput dIn)
            {
                var type = dOut.Type.Type.IsAssignableFrom(dIn.Type.Type) ? dIn.Type : dOut.Type;
                node.AddDataIn("", type);
                node.AddDataOut("", type);
            }
            else
            {
                Debug.LogWarning("Could not determine port types when creating redirect node");
                return;
            }

            evt.StopPropagation();

            var rerouteView = new UltNoodleRedirectNodeView(node);
            AddElement(rerouteView);

            var parentPort = edge.output;
            var rerouteInput = rerouteView.inputContainer[0] as Port;
            HandleEdgeCreation(parentPort, rerouteInput, true);

            var rerouteOutput = rerouteView.outputContainer[0] as Port;
            var childPort = edge.input;
            HandleEdgeCreation(rerouteOutput, childPort, true);
        });
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
        var dropdown = container?.Q<DropdownField>("VarManDropdown");
        if (dropdown != null)
            dropdown.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
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