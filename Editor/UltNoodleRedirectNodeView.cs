#if UNITY_EDITOR
using UnityEngine;
using System;
using NoodledEvents;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEditor;
using System.Linq;

public class UltNoodleRedirectNodeView : UltNoodleNodeView
{
    public bool IsPendingDelete { get; set; }

    public UltNoodleRedirectNodeView(SerializedNode node) : base(node)
    {
        if (node.NoadType != SerializedNode.NodeType.Redirect)
            throw new ArgumentException("Node is not a Redirect node", nameof(node));

        style.left = node.Position.x;
        style.top = node.Position.y;

        this.capabilities &= ~Capabilities.Copiable;  // cannot copy redirects

        CreateFlowPorts();
        CreateDataPorts();

        RegisterCallback<MouseDownEvent>(OnMouseDown);
        
        titleContainer.RemoveFromHierarchy();
    }

    private void OnMouseDown(MouseDownEvent evt)
    {
        // workaround for OnGraphViewChanged calling drags many times during a move
        if (evt.button == 0)
            Undo.RegisterCompleteObjectUndo(Node.Bowl, "Move Node");
    }

    private void CreateFlowPorts()
    {
        foreach (var fi in Node.FlowInputs)
        {
            var port = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, null);
            port.portName = "";
            port.userData = fi;
            _flowInputs[fi.ID] = port;
            inputContainer.Add(port);
        }

        foreach (var fo in Node.FlowOutputs)
        {
            var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, null);
            port.portName = "";
            port.userData = fo;
            _flowOutputs[fo.ID] = port;
            outputContainer.Add(port);
        }
    }

    private void CreateDataPorts()
    {
        foreach (var di in Node.DataInputs)
        {
            var port = InstantiatePort(
                Orientation.Horizontal,
                Direction.Input,
                Port.Capacity.Single,
                di.Type?.Type ?? typeof(object)
            );
            port.portName = "";
            port.userData = di;
            _dataInputs[di.ID] = port;
            inputContainer.Add(port);
        }

        foreach (var dout in Node.DataOutputs)
        {
            var port = InstantiatePort(
                Orientation.Horizontal,
                Direction.Output,
                Port.Capacity.Multi,
                dout.Type?.Type ?? typeof(object)
            );
            port.portName = "";
            port.userData = dout;
            _dataOutputs[dout.ID] = port;
            outputContainer.Add(port);
        }
    }
}
#endif